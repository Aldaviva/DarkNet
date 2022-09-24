using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using Microsoft.Win32;
using Application = System.Windows.Application;

namespace Dark.Net;

/// <summary>
/// <para>Implementation of the DarkNet library. Used for making title bars of your windows dark in Windows 10 1809 and later.</para>
/// <para>Usage:</para>
/// <list type="number">
/// <item><description>Construct a new instance with <c>new DarkNet()</c>, or use the shared singleton <see cref="Instance"/>.</description></item>
/// <item><description>Optionally, call <see cref="SetCurrentProcessTheme"/> before showing any windows in your process, such as in a <see cref="System.Windows.Application.Startup"/> event handler for your WPF program, or at the beginning of <c>Main</c> in your Forms program.</description></item>
/// <item><description>Call <see cref="SetWindowThemeWpf"/> or <see cref="SetWindowThemeForms"/> for each window before you show it. For WPF, you should do this in <see cref="Window.SourceInitialized"/>. For Forms, you should do this after constructing the <see cref="Form"/> instance.</description></item>
/// </list>
/// </summary>
public class DarkNet: IDarkNet {

    private const string PersonalizeKey = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    private static readonly Lazy<DarkNet> LazyInstance = new(LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// <para>Shared singleton instance of the <see cref="DarkNet"/> class that you can use without constructing your own instance. Created lazily the first time it is accessed.</para>
    /// <para>You may want to construct your own instance using <c>new DarkNet()</c> in order to manage the memory lifecycle and dispose of it manually to avoid a <see cref="SystemEvents.UserPreferenceChanged"/> memory leak, or insulate yourself from other consumers that may try to dispose of <see cref="Instance"/>.</para>
    /// </summary>
    public static IDarkNet Instance => LazyInstance.Value;

    private readonly ConcurrentDictionary<IntPtr, Theme> _preferredWindowModes = new();

    private bool?  _userDefaultAppThemeIsDark;
    private bool?  _userTaskbarThemeIsDark;
    private Theme? _preferredAppMode;
    private int    _processThemeChanged; // int instead of bool to support Interlocked atomic operations

    /// <inheritdoc />
    public event EventHandler<bool>? UserDefaultAppThemeIsDarkChanged;

    /// <inheritdoc />
    public event EventHandler<bool>? UserTaskbarThemeIsDarkChanged;

    /// <summary>
    /// <para>Create a new instance of the DarkNet library class. Alternatively, you can use the static singleton <see cref="Instance"/>.</para>
    /// <para>Useful if you want to manage the memory lifecycle and dispose of it manually to avoid a <see cref="SystemEvents.UserPreferenceChanged"/> memory leak, or insulate yourself from other consumers that may try to dispose of <see cref="Instance"/>.</para>
    /// </summary>
    public DarkNet() {
        SystemEvents.UserPreferenceChanged += OnSettingsChanged;
    }

    /// <inheritdoc />
    public void SetCurrentProcessTheme(Theme theme) {
        bool isFirstCall = Interlocked.CompareExchange(ref _processThemeChanged, 1, 0) == 0;
        if (isFirstCall) {
            if ((Application.Current?.Windows.Cast<Window>().Any(window => window.IsVisible) ?? false) || System.Windows.Forms.Application.OpenForms.Count > 0) {
                //doesn't help if other windows were already opened and closed before calling this
                throw new InvalidOperationException($"Called {nameof(SetCurrentProcessTheme)}() too late, call it before any calls to Form.Show(), Window.Show(), Application.Run(), " +
                    $"{nameof(IDarkNet)}.{nameof(SetWindowThemeForms)}(), or {nameof(IDarkNet)}.{nameof(SetWindowThemeWpf)}()");
            }

            try {
                // Windows 10 1903 and later
                Win32.SetPreferredAppMode(AppMode.AllowDark);
            } catch (Exception e1) when (e1 is not OutOfMemoryException) {
                try {
                    // Windows 10 1809 only
                    Win32.AllowDarkModeForApp(true);
                } catch (Exception e2) when (e2 is not OutOfMemoryException) {
                    Trace.TraceWarning("Failed to set dark mode for process: {0}", e1.Message);
                }
            }
        }

        _preferredAppMode = theme;
        RefreshTitleBarThemeColor();
    }

    /// <inheritdoc />
    // Not overloading one method to cover both WPF and Forms so that consumers don't have to add references to both PresentationFramework and System.Windows.Forms just to use one overloaded variant
    public void SetWindowThemeWpf(Window window, Theme theme) {
        bool isWindowInitialized = PresentationSource.FromVisual(window) != null;
        if (!isWindowInitialized) {
            ImplicitlySetProcessThemeIfFirstCall(theme);
            window.SourceInitialized += OnSourceInitialized;

            void OnSourceInitialized(object? _, EventArgs eventArgs) {
                window.SourceInitialized -= OnSourceInitialized;
                SetWindowThemeWpf(window, theme);
            }
        } else {
            IntPtr windowHandle = new WindowInteropHelper(window).Handle;
            if (IsWindowVisible(windowHandle)) {
                throw new InvalidOperationException($"Called {nameof(SetWindowThemeWpf)}() too late, call it in OnSourceInitialized or the Window subclass's constructor");
            }

            ImplicitlySetProcessThemeIfFirstCall(theme);
            SetModeForWindow(windowHandle, theme);

            void OnClosing(object _, CancelEventArgs args) {
                window.Closing -= OnClosing;
                OnWindowClosing(windowHandle);
            }

            window.Closing += OnClosing;
        }
    }

    /// <inheritdoc />
    public void SetWindowThemeForms(Form window, Theme theme) {
        if (IsWindowVisible(window.Handle)) {
            throw new InvalidOperationException($"Called {nameof(SetWindowThemeForms)}() too late, call it before Form.Show() or Application.Run(), and after " +
                $"{nameof(IDarkNet)}.{nameof(SetCurrentProcessTheme)}()");
        }

        ImplicitlySetProcessThemeIfFirstCall(theme);
        SetModeForWindow(window.Handle, theme);

        void OnClosing(object _, CancelEventArgs args) {
            window.Closing -= OnClosing;
            OnWindowClosing(window.Handle);
        }

        window.Closing += OnClosing;
    }

    /// <inheritdoc />
    public void SetWindowThemeRaw(IntPtr windowHandle, Theme theme) {
        if (IsWindowVisible(windowHandle)) {
            throw new InvalidOperationException($"Called {nameof(SetWindowThemeRaw)}() too late, call it before the window is visible.");
        }

        ImplicitlySetProcessThemeIfFirstCall(theme);
        SetModeForWindow(windowHandle, theme);
    }

    private static bool IsWindowVisible(IntPtr windowHandle) {
        WindowInfo windowInfo = new(null);
        return Win32.GetWindowInfo(windowHandle, ref windowInfo) && (windowInfo.dwStyle & WindowStyles.WsVisible) != 0;
    }

    private void ImplicitlySetProcessThemeIfFirstCall(Theme theme) {
        if (Interlocked.CompareExchange(ref _processThemeChanged, 1, 0) == 0) {
            SetCurrentProcessTheme(theme);
        }
    }

    /// <summary>
    ///     <para>call this after creating but before showing a window, such as WPF's Window.OnSourceInitialized or Forms' Form.Load</para>
    ///     <para>if window.Visibility==VISIBLE and WindowPlacement.ShowCmd == SW_HIDE (or whatever), it was definitely called too early </para>
    ///     <para>if GetWindowInfo().style.WS_VISIBLE == true then it was called too late</para>
    /// </summary>
    private void SetModeForWindow(IntPtr windowHandle, Theme windowTheme) {
        _preferredWindowModes[windowHandle] = windowTheme;

        Win32.AllowDarkModeForWindow(windowHandle, windowTheme != Theme.Light);
        RefreshTitleBarThemeColor(windowHandle);
    }

    private void OnWindowClosing(IntPtr windowHandle) {
        _preferredWindowModes.TryRemove(windowHandle, out _);
    }

    private void OnSettingsChanged(object sender, UserPreferenceChangedEventArgs args) {
        if (args.Category == UserPreferenceCategory.General && (_userDefaultAppThemeIsDark != UserDefaultAppThemeIsDark || _userTaskbarThemeIsDark != UserTaskbarThemeIsDark)) {
            RefreshTitleBarThemeColor();
        }
    }

    private void RefreshTitleBarThemeColor() {
        foreach (IntPtr trackedWindow in _preferredWindowModes.Keys) {
            RefreshTitleBarThemeColor(trackedWindow);
        }
    }

    private void RefreshTitleBarThemeColor(IntPtr windowHandle) {
        if (!_preferredWindowModes.TryGetValue(windowHandle, out Theme windowMode)) {
            windowMode = Theme.Auto;
        }

        if (windowMode == Theme.Auto) {
            windowMode = _preferredAppMode ?? Theme.Auto;
        }

        if (windowMode == Theme.Auto) {
            windowMode = UserDefaultAppThemeIsDark ? Theme.Dark : Theme.Light;
        }

        if (!Win32.IsDarkModeAllowedForWindow(windowHandle) || IsHighContrast()) {
            windowMode = Theme.Light;
        }

        bool   isDarkMode               = windowMode == Theme.Dark;
        int    attributeValueBufferSize = Marshal.SizeOf(typeof(bool));
        IntPtr attributeValueBuffer     = Marshal.AllocCoTaskMem(attributeValueBufferSize);
        Marshal.WriteInt32(attributeValueBuffer, Convert.ToInt32(isDarkMode));

        try {
            // Windows 10 1903 and later
            WindowCompositionAttributeData windowCompositionAttributeData = new(WindowCompositionAttribute.WcaUsedarkmodecolors, attributeValueBuffer, attributeValueBufferSize);
            Win32.SetWindowCompositionAttribute(windowHandle, ref windowCompositionAttributeData);
        } catch (Exception e1) when (e1 is not OutOfMemoryException) {
            try {
                // Windows 10 1809 only
                Win32.SetProp(windowHandle, "UseImmersiveDarkModeColors", new IntPtr(Convert.ToInt64(isDarkMode)));
            } catch (Exception e2) when (e2 is not OutOfMemoryException) {
                throw new ApplicationException("Failed to set dark mode for process", e2); //TODO throw a different class
            }
        } finally {
            Marshal.FreeCoTaskMem(attributeValueBuffer);
        }
    }

    /// <inheritdoc />
    public bool UserDefaultAppThemeIsDark {
        get {
            bool? oldValue = _userDefaultAppThemeIsDark;
            // Unfortunately, the corresponding undocumented uxtheme.dll function (#132) always returns Dark in .NET Core runtimes for some reason, so we check the registry instead.
            // Verified on Windows 10 21H2 and Windows 11 21H2.
            _userDefaultAppThemeIsDark = !Convert.ToBoolean(Registry.GetValue(PersonalizeKey, "AppsUseLightTheme", 1));
            if (oldValue is not null && _userDefaultAppThemeIsDark != oldValue) {
                UserDefaultAppThemeIsDarkChanged?.Invoke(this, _userDefaultAppThemeIsDark.Value);
            }

            return _userDefaultAppThemeIsDark.Value;
        }
    }

    /// <inheritdoc />
    public bool UserTaskbarThemeIsDark {
        get {
            bool? oldValue = _userTaskbarThemeIsDark;
            // In Windows 10 1809 and Server 2019, the taskbar is always dark, and this registry value does not exist.
            _userTaskbarThemeIsDark = !Convert.ToBoolean(Registry.GetValue(PersonalizeKey, "SystemUsesLightTheme", 0));
            if (oldValue is not null && _userTaskbarThemeIsDark != oldValue) {
                UserTaskbarThemeIsDarkChanged?.Invoke(this, _userTaskbarThemeIsDark.Value);
            }

            return _userTaskbarThemeIsDark.Value;
        }
    }

    private static bool IsHighContrast() {
        const int getHighContrast = 0x42;
        const int highContrastOn  = 0x1;

        HighContrastData highContrastData = new(null);
        return Win32.SystemParametersInfo(getHighContrast, highContrastData.size, ref highContrastData, 0) && (highContrastData.flags & highContrastOn) != 0;
    }

    /// <inheritdoc />
    public void Dispose() {
        SystemEvents.UserPreferenceChanged -= OnSettingsChanged;
    }

}