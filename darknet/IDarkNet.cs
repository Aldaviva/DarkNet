using System;
using System.Windows;
using System.Windows.Forms;

namespace Dark.Net;

/// <summary>
/// <para>Interface of the DarkNet library. Used for making title bars of your windows dark in Windows 10 1809 and later.</para>
/// <para>Usage:</para>
/// <list type="number">
/// <item><description>Construct a new instance with <c>new DarkNet()</c>, or use the shared singleton <see cref="DarkNet.Instance"/>.</description></item>
/// <item><description>Optionally, call <see cref="SetCurrentProcessTheme"/> before showing any windows in your process, such as in a <see cref="System.Windows.Application.Startup"/> event handler for your WPF program, or at the beginning of <c>Main</c> in your Forms program.</description></item>
/// <item><description>Call <see cref="SetWindowThemeWpf"/> or <see cref="SetWindowThemeForms"/> for each window before you show it. For WPF, you should do this in <see cref="Window.SourceInitialized"/>. For Forms, you should do this after constructing the <see cref="Form"/> instance.</description></item>
/// </list>
/// </summary>
public interface IDarkNet: IDisposable {

    /// <summary>
    ///     <para>Allow windows in your app to use dark mode.</para>
    ///     <para>You may optionally call this when your process starts, before you show any windows.</para>
    ///     <para>For WPF, it is recommended to call this in an overridden <see cref="System.Windows.Application.OnStartup"/> in <c>App.xaml.cs</c>, or in an event handler for the <see cref="System.Windows.Application.Startup"/> event.</para>
    ///     <para>For Forms, it is recommended to call this near the beginning of <c>Main</c>.</para>
    ///     <para>This method doesn't actually make your title bars dark. It defines the default theme to use if you set a window's theme to <see cref="Theme.Auto"/> using <see cref="SetWindowThemeWpf" />/<see cref="SetWindowThemeForms"/>.</para>
    /// </summary>
    /// <param name="theme">The theme that windows of your process should use. This theme overrides the user's settings and is overridden by the window theme you set later, unless you set the theme to <see cref="Theme.Auto"/>, in which case it inherits from the user's settings.</param>
    /// <exception cref="InvalidOperationException">If this method was called for the first time after creating or showing any windows in your app. It has to be called before that, e.g. as the first statement in <c>OnStartup</c> or <c>Main</c>.</exception>
    void SetCurrentProcessTheme(Theme theme);

    /// <summary>
    ///     <para>Turn on dark mode for a window.</para>
    ///     <para>You must call this method in your window's <see cref="Window.SourceInitialized" /> event handler.</para>
    /// </summary>
    /// <remarks>The correct time to call this method is when the window has already been constructed, it has an HWND, but it has not yet been shown (i.e. its Win32 window style must not be visible yet). You can call this directly after the call to <c>Window.InitializeComponent</c> in the Window's constructor. Alternatively, a handler for the <see cref="Window.SourceInitialized" /> event will be fired at the correct point in the window lifecycle to call this method.</remarks>
    /// <param name="window">A WPF window which has been constructed and is being SourceInitialized, but has not yet been shown.</param>
    /// <param name="theme">The theme to use for this window. Can be <see cref="Theme.Auto"/> to inherit from the app (defined by the theme passed to <see cref="SetCurrentProcessTheme"/>), or from the user's default app settings if you also set the app to <see cref="Theme.Auto"/> (defined in Settings › Personalization › Colors).</param>
    /// <exception cref="InvalidOperationException">If this method was called too early (such as right after the Window constructor), or too late (such as after <see cref="Window.Show" /> returns).</exception>
    void SetWindowThemeWpf(Window window, Theme theme);

    /// <summary>
    ///     <para>Turn on dark mode for a window.</para>
    ///     <para>You must call this method before calling <see cref="Control.Show()" /> or <see cref="System.Windows.Forms.Application.Run()"/>.</para>
    /// </summary>
    /// <remarks>The correct time to call this method is when the window has already been constructed, but it has not yet been shown (i.e. its Win32 window style must not be visible yet). You can call this after the <see cref="Form"/> constructor returns, but before <see cref="Control.Show" />.</remarks>
    /// <param name="window">A Windows Forms window which has been constructed but has not yet been shown.</param>
    /// <param name="theme">The theme to use for this window. Can be <see cref="Theme.Auto"/> to inherit from the app (defined by the theme passed to <see cref="SetCurrentProcessTheme"/>), or from the user's default app settings if you also set the app to <see cref="Theme.Auto"/> (defined in Settings › Personalization › Colors).</param>
    /// <exception cref="InvalidOperationException">If this method was called too late (such as after calling <see cref="Control.Show" /> returns).</exception>
    void SetWindowThemeForms(Form window, Theme theme);

    /// <summary>
    ///     <para>Turn on dark mode for a window.</para>
    ///     <para>This method is a lower-level alternative to <see cref="SetWindowThemeWpf"/> and <see cref="SetWindowThemeForms"/> for use when one of the windows in your application was created neither by WPF nor Windows Forms, but you still want to make its title bar dark.</para>
    ///     <para>You must call this method before the window is visible.</para>
    /// </summary>
    /// <remarks>The correct time to call this method is when the window has already been constructed, but it has not yet been shown (i.e. its Win32 window style must not be visible yet).</remarks>
    /// <param name="windowHandle">A <c>HWND</c> handle to a Win32 window, which has been constructed but has not yet been shown.</param>
    /// <param name="theme">The theme to use for this window. Can be <see cref="Theme.Auto"/> to inherit from the app (defined by the theme passed to <see cref="SetCurrentProcessTheme"/>), or from the user's default app settings if you also set the app to <see cref="Theme.Auto"/> (defined in Settings › Personalization › Colors).</param>
    /// <exception cref="InvalidOperationException">If this method was called too late.</exception>
    void SetWindowThemeRaw(IntPtr windowHandle, Theme theme);

    /// <summary>
    ///     <para>Whether windows which follow the user's default operating system theme, such as Windows Explorer, Command Prompt, and Settings, will use dark mode in their title bars, context menus, and other themed areas. Also known as "app mode" or "default app mode".</para>
    ///     <para>This reflects the user's preference in Settings › Personalization › Colors › Choose your default app mode.</para>
    ///     <para>Not affected by the taskbar theme, see <seealso cref="UserTaskbarThemeIsDark"/>.</para>
    /// </summary>
    /// <returns><c>true</c> if the user's Default App Mode is Dark, or <c>false</c> if it is Light.</returns>
    bool UserDefaultAppThemeIsDark { get; }

    /// <summary>
    ///     <para>Whether the taskbar and Start Menu will use dark mode. Also known as "system mode" (although it doesn't apply to the entire system, just the current user) and "Windows mode" (although it doesn't apply to most windows in Windows, such as Explorer and Command Prompt).</para>
    ///     <para>This reflects the user's preference in Settings › Personalization › Colors › Choose your default Windows mode.</para>
    ///     <para>Not affected by the default app theme, see <seealso cref="UserTaskbarThemeIsDark"/>.</para>
    /// </summary>
    /// <returns><c>true</c> if the user's Windows Mode is Dark, or <c>false</c> if it is Light.</returns>
    bool UserTaskbarThemeIsDark { get; }

    /// <summary>
    /// <para>Fired when the value of <seealso cref="UserDefaultAppThemeIsDark"/> changes.</para>
    /// <para>If you set your process and window themes to <see cref="Theme.Auto"/>, it will react automatically and you don't have to handle this event for your windows to use the new default theme.</para>
    /// </summary>
    event EventHandler<bool>? UserDefaultAppThemeIsDarkChanged;

    /// <summary>
    /// <para>Fired when the value of <seealso cref="UserTaskbarThemeIsDark"/> changes.</para>
    /// <para>You may choose to handle this event if, for example, you want to show a tray icon in the notification area that depends on the taskbar theme.</para>
    /// </summary>
    event EventHandler<bool>? UserTaskbarThemeIsDarkChanged;

}

/// <summary>
/// Windows visual appearance, which can be used to make the title bar and context menu of a window dark or light.
/// </summary>
public enum Theme {

    /// <summary>
    /// <para>Inherit the theme from a higher level.</para>
    /// <para>When a window's theme is set to <see cref="Auto"/> using <see cref="IDarkNet.SetWindowThemeWpf"/>/<see cref="IDarkNet.SetWindowThemeForms"/>, the window will use the theme that was set on the current process using <see cref="IDarkNet.SetCurrentProcessTheme"/>.</para>
    /// <para>When the process' theme is set to <see cref="Auto"/> using <see cref="IDarkNet.SetCurrentProcessTheme"/>, any windows that also have their theme set to <see cref="Auto"/> will use the user-level settings defined in Settings › Personalization › Colors › Choose your default app mode.</para>
    /// </summary>
    Auto,

    /// <summary>
    /// Light mode, a white background with black text and icons
    /// </summary>
    Light,

    /// <summary>
    /// Dark mode, a black background with white text and icons
    /// </summary>
    Dark

}