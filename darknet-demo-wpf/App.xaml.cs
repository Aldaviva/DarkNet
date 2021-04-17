using System.Windows;
using darknet.wpf;

#nullable enable

namespace darknet_demo_wpf {

    public partial class App {

        private void App_OnStartup(object sender, StartupEventArgs e) {
            DarkNet.SetDarkModeAllowedForProcess(true);

            var window = new MainWindow();
            window.SourceInitialized += (_, _) => { DarkNet.SetDarkModeAllowedForWindow(window, true); };

            window.Show();
        }

    }

}