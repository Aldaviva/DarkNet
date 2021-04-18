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
        }

    }

}