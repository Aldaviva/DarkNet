using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using Microsoft.Win32;
using Application = System.Windows.Application;

namespace Dark.Net;

public class DarkNet: IDarkNet {

    private static readonly Lazy<DarkNet> LazyInstance = new(LazyThreadSafetyMode.PublicationOnly);
    public static IDarkNet Instance => LazyInstance.Value;

    private readonly ConcurrentDictionary<IntPtr, Theme> _preferredWindowModes = new();

    private bool   _preferredSystemDarkModeCached;
    private Theme? _preferredAppMode;

    /// <inheritdoc />
    public bool IsSystemDarkTheme => IsSystemModeDark();

    /// <inheritdoc />
    public event EventHandler<bool>? SystemDarkThemeChanged;

    /// <inheritdoc />
    public void SetCurrentProcessTheme(Theme theme) {
        if (Application.Current?.MainWindow != null || Application.Current?.Windows.Count > 0 || System.Windows.Forms.Application.OpenForms.Count > 0) {
            //doesn't help if other windows were already opened and closed before calling this
            throw new InvalidOperationException($"Called {nameof(SetCurrentProcessTheme)}() too late, call it before any calls to Form.Show(), Window.Show(), Application.Run(), " +
                $"{nameof(IDarkNet)}.{nameof(SetFormsWindowTheme)}(), or {nameof(IDarkNet)}.{nameof(SetWpfWindowTheme)}()");
        }

        _preferredAppMode = theme;

        try {
            // Windows 10 1903 and later
            Win32.SetPreferredAppMode(AppMode.AllowDark);
        } catch (Exception e1) when (e1 is not OutOfMemoryException) {
            try {
                // Windows 10 1809 only
                Win32.AllowDarkModeForApp(true);
            } catch (Exception e2) when (e2 is not OutOfMemoryException) {
                throw new Exception("Failed to set dark mode for process", e1); //TODO throw a different class
            }
        }
    }

    /// <inheritdoc />
    // Not overloading one method to cover both WPF and Forms so that consumers don't have to add references to both PresentationFramework and System.Windows.Forms just to use one overloaded variant
    public void SetWpfWindowTheme(Window window, Theme theme) {
        bool isWindowInitialized = PresentationSource.FromVisual(window) != null;
        if (!isWindowInitialized) {
            window.SourceInitialized += OnSourceInitialized;

            void OnSourceInitialized(object? _, EventArgs eventArgs) {
                window.SourceInitialized -= OnSourceInitialized;
                SetWpfWindowTheme(window, theme);
            }

            return;
        }

        IntPtr windowHandle = new WindowInteropHelper(window).Handle;
        if (IsWindowVisible(windowHandle)) {
            throw new InvalidOperationException($"Called {nameof(SetWpfWindowTheme)}() too late, call it in OnSourceInitialized or the Window subclass's constructor");
        }

        SetModeForWindow(windowHandle, theme);

        void OnClosing(object _, CancelEventArgs args) {
            window.Closing -= OnClosing;
            OnWindowClosing(windowHandle);
        }

        window.Closing += OnClosing;
    }

    /// <inheritdoc />
    public void SetFormsWindowTheme(Form window, Theme theme) {
        if (IsWindowVisible(window.Handle)) {
            throw new InvalidOperationException($"Called {nameof(SetFormsWindowTheme)}() too late, call it before Form.Show() or Application.Run(), and after " +
                $"{nameof(IDarkNet)}.{nameof(SetCurrentProcessTheme)}()");
        }

        SetModeForWindow(window.Handle, theme);

        void OnClosing(object _, CancelEventArgs args) {
            window.Closing -= OnClosing;
            OnWindowClosing(window.Handle);
        }

        window.Closing += OnClosing;
    }

    private static bool IsWindowVisible(IntPtr windowHandle) {
        WindowInfo windowInfo = new(null);
        Win32.GetWindowInfo(windowHandle, ref windowInfo);
        return (windowInfo.dwStyle & WindowStyles.WsVisible) != 0;
    }

    /// <summary>
    ///     <para>call this after creating but before showing a window, such as WPF's Window.OnSourceInitialized or Forms' Form.Load</para>
    ///     <para>if window.Visibility==VISIBLE and WindowPlacement.ShowCmd == SW_HIDE (or whatever), it was definitely called too early </para>
    ///     <para>if GetWindowInfo().style.WS_VISIBLE == true then it was called too late</para>
    /// </summary>
    private void SetModeForWindow(IntPtr windowHandle, Theme windowTheme) {
        bool isNewWindow = false;

        _preferredWindowModes.AddOrUpdate(windowHandle, _ => {
            isNewWindow = true;
            return windowTheme;
        }, (_, _) => {
            isNewWindow = false;
            return windowTheme;
        });

        if (isNewWindow) {
            ListenForSystemModeChanges(windowHandle);
        }

        Win32.AllowDarkModeForWindow(windowHandle, windowTheme != Theme.Light);
        RefreshTitleBarThemeColor(windowHandle);
    }

    private void OnWindowClosing(IntPtr windowHandle) {
        _preferredWindowModes.TryRemove(windowHandle, out _);
    }

    private void ListenForSystemModeChanges(IntPtr windowHandle) {
        SystemEvents.UserPreferenceChanged += OnSettingsChanged;

        void OnSettingsChanged(object _, UserPreferenceChangedEventArgs args) {
            if (!_preferredWindowModes.ContainsKey(windowHandle)) {
                SystemEvents.UserPreferenceChanged -= OnSettingsChanged;
            } else if (args.Category == UserPreferenceCategory.General && _preferredSystemDarkModeCached != IsSystemModeDark()) {
                RefreshTitleBarThemeColor(windowHandle);
            }
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
            windowMode = IsSystemModeDark() ? Theme.Dark : Theme.Light;
        }

        if (!Win32.IsDarkModeAllowedForWindow(windowHandle) || IsHighContrast()) {
            windowMode = Theme.Light;
        }

        bool isDarkMode = windowMode == Theme.Dark;

        try {
            // Windows 10 1903 and later
            int    attributeValueBufferSize = Marshal.SizeOf(typeof(bool));
            IntPtr attributeValueBuffer     = Marshal.AllocCoTaskMem(attributeValueBufferSize);
            Marshal.WriteInt32(attributeValueBuffer, Convert.ToInt32(isDarkMode));
            WindowCompositionAttributeData windowCompositionAttributeData = new(WindowCompositionAttribute.WcaUsedarkmodecolors, attributeValueBuffer, attributeValueBufferSize);

            Win32.SetWindowCompositionAttribute(windowHandle, ref windowCompositionAttributeData);

            Marshal.FreeCoTaskMem(attributeValueBuffer);
        } catch (Exception e1) when (e1 is not OutOfMemoryException) {
            try {
                // Windows 10 1809 only
                Win32.SetProp(windowHandle, "UseImmersiveDarkModeColors", new IntPtr(Convert.ToInt64(isDarkMode)));
            } catch (Exception e2) when (e2 is not OutOfMemoryException) {
                throw new ApplicationException("Failed to set dark mode for process", e2); //TODO throw a different class
            }
        }
    }

    /// <summary>
    ///     <para>This reflects the user's preference in Settings › Personalization › Colors › Choose Your Default App Mode.</para>
    ///     <para>This calls <see cref="Win32.ShouldAppsUseDarkMode" /> and caches the result for future change comparisons.</para>
    /// </summary>
    /// <returns><c>true</c> if the system's Default App Mode is Dark, or <c>false</c> if it is Light.</returns>
    private bool IsSystemModeDark() {
        bool oldValue = _preferredSystemDarkModeCached;
        _preferredSystemDarkModeCached = Win32.ShouldAppsUseDarkMode();
        if (_preferredSystemDarkModeCached != oldValue) {
            SystemDarkThemeChanged?.Invoke(this, _preferredSystemDarkModeCached);
        }

        return _preferredSystemDarkModeCached;
    }

    private static bool IsHighContrast() {
        const int getHighContrast = 0x42;
        const int highContrastOn  = 0x1;

        HighContrastData highContrastData = new(null);
        if (Win32.SystemParametersInfo(getHighContrast, highContrastData.size, ref highContrastData, 0)) {
            return (highContrastData.flags & highContrastOn) != 0;
        }

        return false;
    }

}