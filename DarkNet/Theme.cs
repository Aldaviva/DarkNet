namespace Dark.Net;

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