using System;
using System.Windows;
using System.Windows.Interop;

namespace darknet_wpf {

    public partial class App {

        private void App_OnStartup(object sender, StartupEventArgs e) {
            setDarkModeAllowedForProcess(true); //you can tell if this worked because the title bar's context menu will be dark

            var window = new MainWindow();
            // setDarkModeAllowedForWindow(window, true);
            window.SourceInitialized += (source, args) => {
                // Console.WriteLine("window initialized = " + window.IsInitialized);
                setDarkModeAllowedForWindow(window, true);
            };

            // window.onBeforeShow();
            window.Show();
            // setDarkModeAllowedForWindow(window, true);
            // Thread.Sleep(200);
            // window.onShow();
            //
            // Task.Delay(200).ContinueWith(task => { Dispatcher.InvokeAsync(() => window.onShow()); });

        }

        private static void setDarkModeAllowedForProcess(bool isDarkModeAllowed) {
            if (Current.MainWindow != null || Current.Windows.Count > 0) { //doesn't help if other windows were already opened and closed before calling this
                throw new InvalidOperationException("Called too late, call this before showing any windows");
            }

            DarkMode.setDarkModeAllowedForProcess(isDarkModeAllowed);
        }

        internal static void setDarkModeAllowedForWindow(Window window, bool isDarkModeAllowed) {
            bool isWindowInitialized = PresentationSource.FromVisual(window) != null;
            if (!isWindowInitialized) {
                throw new InvalidOperationException("Called too early, call this during OnSourceInitialized");
            }

            IntPtr windowHandle = new WindowInteropHelper(window).Handle;
            var    windowInfo   = new WINDOWINFO(null);
            Win32.GetWindowInfo(windowHandle, ref windowInfo);

            bool isWindowVisible = (windowInfo.dwStyle & WindowStyles.WS_VISIBLE) != 0;
            if (isWindowVisible) {
                throw new InvalidOperationException("Called too late, call this during OnSourceInitialized");
            }

            DarkMode.setDarkModeAllowedForWindow(windowHandle, isDarkModeAllowed);
        }

    }

}