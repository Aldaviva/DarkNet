using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;

namespace darknet.wpf {

    /// <summary>
    ///     <para>Apply Windows 10's dark mode to the title bars and system context menus of WPF windows.</para>
    ///     <para>Be sure to call both <see cref="SetModeForCurrentProcess" /> before showing any windows in your app, and then call <see cref="SetModeForWindow" /> in each of your windows' <see cref="Window.SourceInitialized" /> events.</para>
    /// </summary>
    /// <remarks>Requires Windows 10 version 1809 or later.</remarks>
    public class DarkNetWpfImpl: DarkNetWpf {

        private readonly DarkMode _darkMode = new();

        /// <summary>
        ///     <para>Allow windows in your app to use dark mode.</para>
        ///     <para>Call this when your process starts, before you show any windows. It is recommended to call this from <c>App_OnStartup</c> in <c>App.xaml.cs</c>, or whatever method you use to handle the <see cref="Application.Startup" /> event. Note that you can't use <see cref="Application.StartupUri" /> because that shows the window before you can call this method.</para>
        ///     <para>This method doesn't actually enable dark mode for your windows, it is a prerequisite for calling <see cref="SetModeForWindow" /> for each of your windows once you have created them.</para>
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="isDarkModeAllowed"><c>true</c> to allow dark mode, <c>false</c> to not allow dark mode (the default).</param>
        /// <exception cref="InvalidOperationException">If this method was called after creating or showing any windows in your app. It has to be called before that, e.g. as the first statement in <c>App_OnStartup</c>.</exception>
        public void SetModeForCurrentProcess(Mode mode) {
            if (Application.Current.MainWindow != null || Application.Current.Windows.Count > 0) { //doesn't help if other windows were already opened and closed before calling this
                throw new InvalidOperationException("Called too late, call this before showing any windows");
            }

            _darkMode.SetModeForProcess(mode);
        }

        /// <summary>
        ///     <para>Turn on dark mode for a window.</para>
        ///     <para>You must have already called <see cref="SetModeForCurrentProcess" /> before creating this window.</para>
        ///     <para>You must call this method in your window's <see cref="Window.SourceInitialized" /> event handler.</para>
        /// </summary>
        /// <remarks>The correct time to call this method is when the window has already been constructed, it has an HWND, but it has not yet been shown (i.e. its Win32 window style must not be visible yet). A handler for the <see cref="Window.SourceInitialized" /> event will be fired at the correct point in the window lifecycle to call this method.</remarks>
        /// <param name="mode"></param>
        /// <param name="window">A WPF window which has been constructed and is being SourceInitialized, but has not yet been shown.</param>
        /// <param name="isDarkModeAllowed"><c>true</c> to make the title bar dark, or <c>false</c> to leave the title bar light (the default).</param>
        /// <exception cref="InvalidOperationException">If this method was called too early (such as right after the Window constructor), or too late (such as after <see cref="Window.Show" /> returns).</exception>
        public void SetModeForWindow(Mode mode, Window window) {
            bool isWindowInitialized = PresentationSource.FromVisual(window) != null;
            if (!isWindowInitialized) {
                window.SourceInitialized += OnSourceInitialized;

                void OnSourceInitialized(object? o, EventArgs eventArgs) {
                    window.SourceInitialized -= OnSourceInitialized;
                    SetModeForWindow(mode, window);
                }

                return;
            }

            IntPtr windowHandle = new WindowInteropHelper(window).Handle;
            var    windowInfo   = new WindowInfo(null);
            Win32.GetWindowInfo(windowHandle, ref windowInfo);

            bool isWindowVisible = (windowInfo.dwStyle & WindowStyles.WsVisible) != 0;
            if (isWindowVisible) {
                throw new InvalidOperationException("Called too late, call this during OnSourceInitialized");
            }

            _darkMode.SetModeForWindow(windowHandle, mode);

            // CancellationTokenSource debounce = new();

            // DarkMode.ListenForSystemModeChanges(windowHandle);

            void OnWindowOnClosing(object sender, CancelEventArgs args) {
                window.Closing -= OnWindowOnClosing;
                _darkMode.OnWindowClosed(windowHandle);
            }

            window.Closing += OnWindowOnClosing;
        }

    }

}