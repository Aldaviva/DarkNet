using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace Dark.Net.Events;

public abstract class WindowThemeChangedEventArgs: EventArgs {

    public IntPtr WindowHandle { get; }
    public bool EffectiveWindowThemeIsDark { get; }

    protected WindowThemeChangedEventArgs(IntPtr windowHandle, bool effectiveWindowThemeIsDark) {
        WindowHandle               = windowHandle;
        EffectiveWindowThemeIsDark = effectiveWindowThemeIsDark;
    }

}

public class WpfWindowThemeChangedEventArgs: WindowThemeChangedEventArgs {

    public Window Window { get; }

    public WpfWindowThemeChangedEventArgs(Window window, bool effectiveWindowThemeIsDark): base(new WindowInteropHelper(window).Handle, effectiveWindowThemeIsDark) {
        Window = window;
    }

}

public class FormsWindowThemeChangedEventArgs: WindowThemeChangedEventArgs {

    public Form Window { get; }

    public FormsWindowThemeChangedEventArgs(Form window, bool isEffectiveWindowThemeDark): base(window.Handle, isEffectiveWindowThemeDark) {
        Window = window;
    }

}