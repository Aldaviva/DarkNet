using System;
using System.Windows.Forms;
using darknet_wpf;

namespace darknet_winforms {

    internal static class Program {

        [STAThread]
        private static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            setDarkModeAllowedForProcess(true);

            var mainForm = new Form1();
            setDarkModeAllowedForWindow(mainForm, true);
            mainForm.Load += (sender, args) => {
                logInitializationState("on load", mainForm);
                // setDarkModeAllowedForWindow(mainForm, true);
            };
            mainForm.Show();
            // setDarkModeAllowedForWindow(mainForm, true);

            Application.Run(mainForm);
        }

        private static void setDarkModeAllowedForProcess(bool isDarkModeAllowed) {
            if (Application.OpenForms.Count > 0) { //doesn't help if other windows were already opened and closed before calling this
                throw new InvalidOperationException("Call this before opening any windows in your program.");
            }

            DarkMode.setDarkModeAllowedForProcess(isDarkModeAllowed);
        }

        internal static void setDarkModeAllowedForWindow(Form window, bool isDarkModeAllowed) {
            var windowInfo = new WINDOWINFO(null);
            Win32.GetWindowInfo(window.Handle, ref windowInfo);
            bool isWindowVisible = (windowInfo.dwStyle & WindowStyles.WS_VISIBLE) != 0;
            if (isWindowVisible) {
                throw new InvalidOperationException("Called too late, call this during OnSourceInitialized");
            }

            DarkMode.setDarkModeAllowedForWindow(window.Handle, isDarkModeAllowed);
        }

        private static void logInitializationState(string caller, Form window) {

            var windowInfo = new WINDOWINFO(null);
            Win32.GetWindowInfo(window.Handle, ref windowInfo);

            bool isWindowVisible = (windowInfo.dwStyle & WindowStyles.WS_VISIBLE) != 0;

            Console.WriteLine($"{caller}, form visible={window.Visible}, window style visible={isWindowVisible}");
        }

    }

}