using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using Dark.Net.Events;
using Microsoft.Win32;
using SystemColors = System.Drawing.SystemColors;

namespace Dark.Net;

/// <summary>
/// <para>Implementation of the DarkNet library. Used for making title bars and context menus of your windows dark in Windows 10 1809 and later.</para>
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
    /// <para>You may want to construct your own instance using <c>new DarkNet()</c> in order to manage the memory lifecycle and dispose of it manually to insulate yourself from other consumers that may try to dispose of <see cref="Instance"/>.</para>
    /// </summary>
    public static IDarkNet Instance => LazyInstance.Value;

    /// <summary>
    /// Mapping from a window handle to the most recently set <see cref="Theme"/> for that window, used to correctly reapply themes when a parent theme (process or OS) changes.
    /// </summary>
    private readonly ConcurrentDictionary<IntPtr, WindowThemeState> _windowStates = new();

    /// <summary>
    /// The most recently set theme for this process, used to correctly reapply themes to this process's windows when the user changes their OS settings.
    /// </summary>
    private Theme? _preferredAppTheme;

    /// <summary>
    /// Most recent value for whether the process's theme is dark, after taking into account high contrast mode. Null if never set. Used to back the <see cref="EffectiveCurrentProcessThemeIsDark"/> property and fire <see cref="EffectiveCurrentProcessThemeIsDarkChanged"/> events.
    /// </summary>
    private bool? _effectiveProcessThemeIsDark;

    private bool?         _userDefaultAppThemeIsDark;
    private bool?         _userTaskbarThemeIsDark;
    private ThemeOptions? _processThemeOptions;

    private volatile int _processThemeChanged; // int instead of bool to support Interlocked atomic operations

    /// <inheritdoc />
    public event EventHandler<bool>? UserDefaultAppThemeIsDarkChanged;

    /// <inheritdoc />
    public event EventHandler<bool>? UserTaskbarThemeIsDarkChanged;

    /// <inheritdoc />
    public event EventHandler<bool>? EffectiveCurrentProcessThemeIsDarkChanged;

    public event EventHandler<WindowThemeChangedEventArgs>? EffectiveWindowThemeIsDarkChanged;

    /// <summary>
    /// <para>Create a new instance of the DarkNet library class. Alternatively, you can use the static singleton <see cref="Instance"/>.</para>
    /// <para>Useful if you want to manage the memory lifecycle and dispose of it manually to insulate yourself from other consumers that may try to dispose of <see cref="Instance"/>.</para>
    /// </summary>
    public DarkNet() {
        try {
            SystemEvents.UserPreferenceChanged += OnSettingsChanged;
        } catch (ExternalException) { }
    }

    /// <inheritdoc />
    public virtual void SetCurrentProcessTheme(Theme theme, ThemeOptions? options = null) {
        _processThemeChanged = 1;
        _processThemeOptions = options;

        try {
            // Windows 10 1903 and later
            Win32.SetPreferredAppMode(theme switch {
                Theme.Light => AppMode.Default,
                Theme.Auto  => AppMode.AllowDark,
                Theme.Dark  => AppMode.ForceDark,
                _           => AppMode.Default
            });
        } catch (Exception e1) when (e1 is not OutOfMemoryException) {
            try {
                // Windows 10 1809 only
                Win32.AllowDarkModeForApp(true);
            } catch (Exception e2) when (e2 is not OutOfMemoryException) {
                Trace.TraceWarning("Failed to set dark mode for process: {0}", e1.Message);
                return;
            }
        }

        _preferredAppTheme = theme;
        RefreshTitleBarThemeColor();

        if (_effectiveProcessThemeIsDark == null) {
            EffectiveCurrentProcessThemeIsDark = _preferredAppTheme switch {
                Theme.Auto  => UserDefaultAppThemeIsDark && !IsHighContrast(),
                Theme.Light => false,
                Theme.Dark  => !IsHighContrast(),
                _           => false
            };
        }
    }

    /// <inheritdoc />
    // Not overloading one method to cover both WPF and Forms so that consumers don't have to add references to both PresentationFramework and System.Windows.Forms just to use one overloaded variant
    public virtual void SetWindowThemeWpf(Window window, Theme theme, ThemeOptions? options = null) {
        bool isWindowInitialized = PresentationSource.FromVisual(window) != null;
        if (!isWindowInitialized) {
            ImplicitlySetProcessThemeIfFirstCall(theme);
            window.SourceInitialized += OnSourceInitialized;

            void OnSourceInitialized(object? _, EventArgs eventArgs) {
                window.SourceInitialized -= OnSourceInitialized;
                SetWindowThemeWpf(window, theme, options);
            }
        } else {
            IntPtr windowHandle = new WindowInteropHelper(window).Handle;

            WindowThemeState windowThemeState;
            try {
                windowThemeState           = SetModeForWindow(windowHandle, theme, options);
                windowThemeState.WpfWindow = window;
            } catch (DarkNetException.LifecycleException) {
                throw new InvalidOperationException($"Called {nameof(SetWindowThemeWpf)}() too late, call it in OnSourceInitialized or the Window subclass's constructor");
            }

            window.Closing                         += OnClosing;
            windowThemeState.EffectiveThemeChanged += OnWindowEffectiveThemeChanged;
            OnWindowEffectiveThemeChanged(windowThemeState, windowThemeState.EffectiveThemeIsDark);

            void OnClosing(object _, CancelEventArgs args) {
                window.Closing                         -= OnClosing;
                windowThemeState.EffectiveThemeChanged -= OnWindowEffectiveThemeChanged;
                OnWindowClosing(windowHandle);
            }
        }
    }

    /// <inheritdoc />
    public virtual void SetWindowThemeForms(Form window, Theme theme, ThemeOptions? options = null) {
        WindowThemeState windowThemeState;
        try {
            windowThemeState             = SetModeForWindow(window.Handle, theme, options);
            windowThemeState.FormsWindow = window;
        } catch (DarkNetException.LifecycleException) {
            throw new InvalidOperationException($"Called {nameof(SetWindowThemeForms)}() too late, call it before Form.Show() or Application.Run(), and after " +
                $"{nameof(IDarkNet)}.{nameof(SetCurrentProcessTheme)}()");
        }

        window.Closing                         += OnClosing;
        windowThemeState.EffectiveThemeChanged += OnWindowEffectiveThemeChanged;
        OnWindowEffectiveThemeChanged(windowThemeState, windowThemeState.EffectiveThemeIsDark);

        void OnClosing(object _, CancelEventArgs args) {
            window.Closing                         -= OnClosing;
            windowThemeState.EffectiveThemeChanged -= OnWindowEffectiveThemeChanged;
            OnWindowClosing(window.Handle);
        }
    }

    /// <inheritdoc />
    public virtual void SetWindowThemeRaw(IntPtr windowHandle, Theme theme, ThemeOptions? options = null) {
        try {
            SetModeForWindow(windowHandle, theme, options);
        } catch (DarkNetException.LifecycleException) {
            throw new InvalidOperationException($"Called {nameof(SetWindowThemeRaw)}() too late, call it before the window is visible.");
        }
    }

    private void OnWindowEffectiveThemeChanged(WindowThemeState windowThemeState, bool isDarkMode) {
        WindowThemeChangedEventArgs? eventArgs = windowThemeState switch {
            { WpfWindow: { } wpfWindow }     => new WpfWindowThemeChangedEventArgs(wpfWindow, isDarkMode),
            { FormsWindow: { } formsWindow } => new FormsWindowThemeChangedEventArgs(formsWindow, isDarkMode),
            _                                => null
        };

        if (eventArgs != null) {
            EffectiveWindowThemeIsDarkChanged?.Invoke(this, eventArgs);
        }
    }

    public bool? GetWindowEffectiveThemeIsDarkWpf(Window window) {
        return GetWindowEffectiveThemeIsDarkRaw(new WindowInteropHelper(window).Handle);
    }

    public bool? GetWindowEffectiveThemeIsDarkForms(Form window) {
        return GetWindowEffectiveThemeIsDarkRaw(window.Handle);
    }

    public bool? GetWindowEffectiveThemeIsDarkRaw(IntPtr window) {
        return _windowStates.TryGetValue(window, out WindowThemeState? windowThemeState) ? windowThemeState.EffectiveThemeIsDark : null;
    }

    private void ImplicitlySetProcessThemeIfFirstCall(Theme theme) {
        if (_processThemeChanged == 0) {
            SetCurrentProcessTheme(theme);
        }
    }

    /// <summary>
    ///     <para>call this after creating but before showing a window, such as the WPF method Window.OnSourceInitialized or Forms' Form.Load</para>
    ///     <para>if window.Visibility==VISIBLE and WindowPlacement.ShowCmd == SW_HIDE (or whatever), it was definitely called too early </para>
    ///     <para>if GetWindowInfo().style.WS_VISIBLE == true then it was called too late</para>
    /// </summary>
    /// <returns>The new state of this window's theming</returns>
    /// <exception cref="DarkNetException.LifecycleException">if it is called too late</exception>
    private WindowThemeState SetModeForWindow(IntPtr windowHandle, Theme windowTheme, ThemeOptions? options = null) {
        ImplicitlySetProcessThemeIfFirstCall(windowTheme);

        bool isFirstRunForWindow = true;
        WindowThemeState windowState = _windowStates.AddOrUpdate(windowHandle, hwnd => new WindowThemeState(windowHandle, windowTheme, options), (_, oldState) => {
            isFirstRunForWindow     = false;
            oldState.PreferredTheme = windowTheme;
            oldState.Options        = options;
            return oldState;
        });

        if (isFirstRunForWindow && IsWindowVisible(windowHandle)) {
            throw new DarkNetException.LifecycleException("Called too late, call it before the window is visible.");
        }

        try {
            Win32.AllowDarkModeForWindow(windowHandle, windowTheme != Theme.Light);
        } catch (EntryPointNotFoundException) {
            // #9: possibly Wine, do nothing
            return windowState;
        }

        RefreshTitleBarThemeColor(windowHandle, windowState);

        return windowState;
    }

    /// <summary>
    /// Fired when a WPF or Forms window is about to close, so that we can release the entry in the <see cref="_windowStates"/> map and free its memory.
    /// </summary>
    /// <param name="windowHandle"></param>
    private void OnWindowClosing(IntPtr windowHandle) {
        _windowStates.TryRemove(windowHandle, out _);
    }

    private void OnSettingsChanged(object sender, UserPreferenceChangedEventArgs args) {
        if (args.Category == UserPreferenceCategory.General && (_userDefaultAppThemeIsDark != UserDefaultAppThemeIsDark || _userTaskbarThemeIsDark != UserTaskbarThemeIsDark)) {
            RefreshTitleBarThemeColor();
        }
    }

    private void RefreshTitleBarThemeColor() {
        foreach (KeyValuePair<IntPtr, WindowThemeState> trackedWindow in _windowStates) {
            RefreshTitleBarThemeColor(trackedWindow.Key, trackedWindow.Value);
        }
    }

    /// <summary>
    /// Apply all of the theme fallback/override logic and call the OS methods to apply the window theme. Handles the window theme, app theme, OS theme, high contrast, different Windows versions, Windows 11 colors, repainting visible windows, and updating context menus.
    /// </summary>
    /// <param name="windowHandle">A pointer to the window to update</param>
    /// <param name="windowThemeState">Current values and optional extra parameters for this window's theme</param>
    private void RefreshTitleBarThemeColor(IntPtr windowHandle, WindowThemeState windowThemeState) {
        try {
            Theme windowTheme = windowThemeState.PreferredTheme;
            Theme appTheme    = _preferredAppTheme ?? Theme.Auto;

            if (appTheme == Theme.Auto) {
                appTheme = UserDefaultAppThemeIsDark ? Theme.Dark : Theme.Light;
            }

            if (windowTheme == Theme.Auto) {
                windowTheme = appTheme;
            }

            if (IsHighContrast()) {
                windowTheme = Theme.Light;
                appTheme    = Theme.Light;
            } else if (!Win32.IsDarkModeAllowedForWindow(windowHandle)) {
                windowTheme = Theme.Light;
            }

            EffectiveCurrentProcessThemeIsDark = appTheme == Theme.Dark;

            bool   isDarkTheme              = windowTheme == Theme.Dark;
            int    attributeValueBufferSize = Marshal.SizeOf(typeof(bool));
            IntPtr attributeValueBuffer     = Marshal.AllocHGlobal(attributeValueBufferSize);
            Marshal.WriteInt32(attributeValueBuffer, Convert.ToInt32(isDarkTheme));

            try {
                // Windows 10 1903 and later
                WindowCompositionAttributeData windowCompositionAttributeData = new(WindowCompositionAttribute.WcaUsedarkmodecolors, attributeValueBuffer, attributeValueBufferSize);
                Win32.SetWindowCompositionAttribute(windowHandle, ref windowCompositionAttributeData);
            } catch (Exception e1) when (e1 is not OutOfMemoryException) {
                try {
                    // Windows 10 1809 only
                    Win32.SetProp(windowHandle, "UseImmersiveDarkModeColors", new IntPtr(Convert.ToInt64(isDarkTheme)));
                } catch (Exception e2) when (e2 is not OutOfMemoryException) {
                    Trace.TraceWarning("Failed to set dark mode for window: {0}", e1.Message);
                    return;
                }
            } finally {
                Marshal.FreeHGlobal(attributeValueBuffer);
            }

            windowThemeState.EffectiveThemeIsDark = isDarkTheme;

            ApplyCustomTitleBarColors(windowHandle, windowThemeState);
            RepaintTitleBar(windowHandle);
            Win32.FlushMenuThemes(); // Needed to make the context menu theme change when you change the app theme after showing a window.
            ApplyThemeToFormsControls(windowHandle, windowThemeState);
        } catch (EntryPointNotFoundException) {
            // #9: possibly Wine, do nothing
        }
    }

    private void ApplyCustomTitleBarColors(IntPtr windowHandle, WindowThemeState windowThemeState) {
        if ((windowThemeState.Options?.TitleBarBackgroundColor ?? _processThemeOptions?.TitleBarBackgroundColor) is { } titleBarBackgroundColor) {
            SetDwmWindowColor(windowHandle, DwmWindowAttribute.DwmwaCaptionColor, titleBarBackgroundColor);
        }

        if ((windowThemeState.Options?.TitleBarTextColor ?? _processThemeOptions?.TitleBarTextColor) is { } titleBarTextColor) {
            SetDwmWindowColor(windowHandle, DwmWindowAttribute.DwmwaTextColor, titleBarTextColor);
        }

        if ((windowThemeState.Options?.WindowBorderColor ?? _processThemeOptions?.WindowBorderColor) is { } windowBorderColor) {
            SetDwmWindowColor(windowHandle, DwmWindowAttribute.DwmwaBorderColor, windowBorderColor);
        }
    }

    /// <summary>
    /// <para>Needed for subsequent (after the window has already been shown) theme changes, otherwise the title bar will only update after you later hide, blur, or resize the window.</para>
    /// <para>Not needed when changing the theme for the first time, before the window has ever been shown.</para>
    /// <para>Windows 11 does not need this. Windows 10 needs this (1809, 22H2, and likely every other version).</para>
    /// <para>Neither RedrawWindow() nor UpdateWindow() fix this.</para>
    /// <para>https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-ncactivate</para>
    /// </summary>
    private static void RepaintTitleBar(IntPtr windowHandle) {
        const uint activateNonClientArea = 0x86;
        bool       isWindowActive        = windowHandle == Win32.GetForegroundWindow();
        Win32.SendMessage(windowHandle, activateNonClientArea, new IntPtr(isWindowActive ? 0 : 1), IntPtr.Zero);
        Win32.SendMessage(windowHandle, activateNonClientArea, new IntPtr(isWindowActive ? 1 : 0), IntPtr.Zero);
    }

    /// <summary>
    /// Optionally theme Windows Forms scrollbars
    /// </summary>
    private void ApplyThemeToFormsControls(IntPtr windowHandle, WindowThemeState windowThemeState) {
        bool isDarkTheme = windowThemeState.EffectiveThemeIsDark;
        if ((windowThemeState.Options?.ApplyThemeToDescendentFormsScrollbars ?? _processThemeOptions?.ApplyThemeToDescendentFormsScrollbars ?? false) &&
            Control.FromHandle(windowHandle) is { } formsWindow) {

            foreach (Control control in formsWindow.Controls.Cast<Control>()
                         .Where(control => control is HScrollBar or VScrollBar or ScrollableControl { AutoScroll: true } or MdiClient or TreeView)) {

                if (control is TreeView treeView && treeView.ForeColor == GetTreeViewColor(!isDarkTheme, true) && treeView.BackColor == GetTreeViewColor(!isDarkTheme, false)) {
                    if (isDarkTheme) {
                        treeView.ForeColor = GetTreeViewColor(isDarkTheme, true);
                        treeView.BackColor = GetTreeViewColor(isDarkTheme, false);
                    } else {
                        treeView.ResetForeColor();
                        treeView.ResetBackColor();
                    }
                }

                Win32.SetWindowTheme(control.Handle, isDarkTheme ? "DarkMode_Explorer" : null, null);

                // Fix scrollbar corners and TreeView borders not repainting with the new theme. Neither Invalidate() nor Refresh() fix this issue, but hiding and showing the control fixes it.
                if (control.Visible) {
                    control.Visible = false;
                    control.Visible = true;
                }
            }
        }

        static Color GetTreeViewColor(bool isDarkMode, bool isForeground) {
            if (isForeground) {
                return isDarkMode ? Color.White : SystemColors.WindowText;
            } else {
                return isDarkMode ? Color.FromArgb(25, 25, 25) : SystemColors.Window;
            }
        }
    }

    // Windows 11 and later
    private static int SetDwmWindowColor(IntPtr windowHandle, DwmWindowAttribute attribute, Color color) {
        int      attributeValueBufferSize = Marshal.SizeOf<ColorRef>();
        IntPtr   attributeValueBuffer     = Marshal.AllocHGlobal(attributeValueBufferSize);
        ColorRef colorRef                 = new(color, ThemeOptions.DefaultColor.Equals(color) || (attribute == DwmWindowAttribute.DwmwaBorderColor && ThemeOptions.NoWindowBorder.Equals(color)));

        Marshal.StructureToPtr(colorRef, attributeValueBuffer, false);
        try {
            return Win32.DwmSetWindowAttribute(windowHandle, attribute, attributeValueBuffer, attributeValueBufferSize);
        } catch (Exception e) when (e is not OutOfMemoryException) {
            Trace.TraceInformation("Failed to set custom title bar color for window: {0}", e.Message);
            return 1;
        } finally {
            Marshal.FreeHGlobal(attributeValueBuffer);
        }
    }

    /// <inheritdoc />
    public virtual bool UserDefaultAppThemeIsDark {
        get {
            try {
                bool? oldValue = _userDefaultAppThemeIsDark;
                // Unfortunately, the corresponding undocumented uxtheme.dll function (#132) always returns Dark in .NET Core runtimes for some reason, so we check the registry instead.
                // Verified on Windows 10 21H2 and Windows 11 21H2.
                _userDefaultAppThemeIsDark = !Convert.ToBoolean(Registry.GetValue(PersonalizeKey, "AppsUseLightTheme", 1) ?? 1);
                if (oldValue is not null && _userDefaultAppThemeIsDark != oldValue) {
                    UserDefaultAppThemeIsDarkChanged?.Invoke(this, _userDefaultAppThemeIsDark.Value);
                }
            } catch (SecurityException) { } catch (IOException) { } catch (FormatException) { }

            return _userDefaultAppThemeIsDark ?? false;
        }
    }

    /// <inheritdoc />
    public virtual bool UserTaskbarThemeIsDark {
        get {
            try {
                bool? oldValue = _userTaskbarThemeIsDark;
                // In Windows 10 1809 and Server 2019, the taskbar is always dark, and this registry value does not exist.
                _userTaskbarThemeIsDark = !Convert.ToBoolean(Registry.GetValue(PersonalizeKey, "SystemUsesLightTheme", 0) ?? 0);
                if (oldValue is not null && _userTaskbarThemeIsDark != oldValue) {
                    UserTaskbarThemeIsDarkChanged?.Invoke(this, _userTaskbarThemeIsDark.Value);
                }
            } catch (SecurityException) { } catch (IOException) { } catch (FormatException) { }

            return _userTaskbarThemeIsDark ?? true;
        }
    }

    /// <inheritdoc />
    public virtual bool EffectiveCurrentProcessThemeIsDark {
        get => _effectiveProcessThemeIsDark ?? false;
        private set {
            if (value != _effectiveProcessThemeIsDark) {
                _effectiveProcessThemeIsDark = value;
                EffectiveCurrentProcessThemeIsDarkChanged?.Invoke(this, value);
            }

        }
    }

    private static bool IsWindowVisible(IntPtr windowHandle) {
        WindowInfo windowInfo = new(null);
        return Win32.GetWindowInfo(windowHandle, ref windowInfo) && (windowInfo.dwStyle & WindowStyles.WsVisible) != 0;
    }

    private static bool IsHighContrast() {
        const uint getHighContrast = 0x42;
        const uint highContrastOn  = 0x1;

        HighContrastData highContrastData = new(null);
        return Win32.SystemParametersInfo(getHighContrast, highContrastData.size, ref highContrastData, 0) && (highContrastData.flags & highContrastOn) != 0;
    }

    /// <inheritdoc />
    public void Dispose() {
        try {
            SystemEvents.UserPreferenceChanged -= OnSettingsChanged;
        } catch (ExternalException) { }

        GC.SuppressFinalize(this);
    }

}