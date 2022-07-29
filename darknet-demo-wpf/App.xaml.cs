using System;
using System.Windows;
using DarkNet;
using DarkNet.WPF;

#nullable enable

namespace darknet_demo_wpf {

    public partial class App {

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);

            DarkNetWpf darkNet = DarkNetWpfImpl.Instance;
            darkNet.SetCurrentProcessTheme(Theme.Auto);

            Console.WriteLine($"System is in {(darkNet.IsSystemDarkTheme ? "Dark" : "Light")} mode");
            darkNet.IsSystemDarkThemeChanged += (_, isSystemDarkMode) => Console.WriteLine($"System changed to {(isSystemDarkMode ? "Dark" : "Light")} mode");
        }

    }

}