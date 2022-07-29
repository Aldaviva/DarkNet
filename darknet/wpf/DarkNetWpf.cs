using System.Windows;

#nullable enable

namespace DarkNet.WPF {

    public interface DarkNetWpf: DarkNet<Window> {

        /// <summary>
        ///     <para>Allow windows in your app to use dark mode.</para>
        ///     <para>Call this when your process starts, before you show any windows. It is recommended to call this from <c>App_OnStartup</c> in <c>App.xaml.cs</c>, or whatever method you use to handle the <see cref="Application.Startup" /> event. Note that you can't use <see cref="Application.StartupUri" /> because that shows the window before you can call this method.</para>
        ///     <para>This method doesn't actually enable dark mode for your windows, it is a prerequisite for calling <see cref="DarkNetWpfImpl.SetDarkModeAllowedForWindow" /> for each of your windows once you have created them.</para>
        /// </summary>
        /// <param name="isDarkModeAllowed"><c>true</c> to allow dark mode, <c>false</c> to not allow dark mode (the default).</param>
        /// <exception cref="InvalidOperationException">If this method was called after creating or showing any windows in your app. It has to be called before that, e.g. as the first statement in <c>App_OnStartup</c>.</exception>
        // void SetDarkModeAllowedForProcess(Mode mode);

        /// <summary>
        ///     <para>Turn on dark mode for a window.</para>
        ///     <para>You must have already called <see cref="DarkNetWpfImpl.SetDarkModeAllowedForProcess" /> before creating this window.</para>
        ///     <para>You must call this method in your window's <see cref="Window.SourceInitialized" /> event handler.</para>
        /// </summary>
        /// <remarks>The correct time to call this method is when the window has already been constructed, it has an HWND, but it has not yet been shown (i.e. its Win32 window style must not be visible yet). A handler for the <see cref="Window.SourceInitialized" /> event will be fired at the correct point in the window lifecycle to call this method.</remarks>
        /// <param name="window">A WPF window which has been constructed and is being SourceInitialized, but has not yet been shown.</param>
        /// <param name="isDarkModeAllowed"><c>true</c> to make the title bar dark, or <c>false</c> to leave the title bar light (the default).</param>
        /// <exception cref="InvalidOperationException">If this method was called too early (such as right after the Window constructor), or too late (such as after <see cref="Window.Show" /> returns).</exception>
        // void SetDarkModeAllowedForWindow(Window window, Mode mode);

    }

}