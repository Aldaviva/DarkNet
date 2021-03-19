using System.Windows;

namespace darknet_wpf {

    public partial class App {

        private void App_OnStartup(object sender, StartupEventArgs e) {
            DarkMode.setDarkModeAllowedForProcess(true); //you can tell if this worked because the title bar's context menu will be dark

            var window = new MainWindow();
            window.Show();
            DarkMode.setDarkModeAllowedForWindow(window, true); //do this before or after showing?
        }

    }

}