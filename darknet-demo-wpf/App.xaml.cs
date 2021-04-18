using System;
using System.Windows;
using darknet;
using darknet.wpf;

#nullable enable

namespace darknet_demo_wpf {

    public partial class App {

        private void App_OnStartup(object sender, StartupEventArgs e) {
            DarkNetWpf darkNet = new DarkNetWpfImpl();
            darkNet.SetModeForCurrentProcess(Mode.Auto);

            var window = new MainWindow();
            darkNet.SetModeForWindow(Mode.Auto, window);
            // window.SourceInitialized += (_, _) => {
            // };

            window.Show();

            Console.WriteLine($"System is in {(darkNet.IsSystemDarkMode ? "Dark" : "Light")} mode");
            darkNet.IsSystemDarkModeChanged += (_, isSystemDarkMode) => Console.WriteLine($"System changed to {(isSystemDarkMode ? "Dark" : "Light")} mode");
        }

    }

}