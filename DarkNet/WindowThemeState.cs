using System;
using System.Windows;
using System.Windows.Forms;

namespace Dark.Net;

internal class WindowThemeState {

    internal Theme PreferredTheme { get; set; }
    internal ThemeOptions? Options { get; set; }
    internal Window? WpfWindow { get; set; }
    internal Form? FormsWindow { get; set; }
    internal IntPtr WindowHandle { get; set; }

    private bool _effectiveThemeIsDark;

    internal event WindowThemeStateEventHandler? EffectiveThemeChanged;

    internal WindowThemeState(IntPtr windowHandle, Theme preferredTheme, ThemeOptions? options = null) {
        WindowHandle   = windowHandle;
        PreferredTheme = preferredTheme;
        Options        = options;
    }

    internal bool EffectiveThemeIsDark {
        get => _effectiveThemeIsDark;
        set {
            if (value != _effectiveThemeIsDark) {
                _effectiveThemeIsDark = value;
                EffectiveThemeChanged?.Invoke(this, value);
            }
        }
    }

    internal delegate void WindowThemeStateEventHandler(WindowThemeState windowThemeState, bool isDarkMode);

}