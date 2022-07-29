using System;
using System.Windows;
using Dark.Net;

namespace darknet_demo_wpf;

public partial class App {

    protected override void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);

        DarkNet.Instance.SetCurrentProcessTheme(Theme.Auto);

        Console.WriteLine($"System is in {(DarkNet.Instance.IsSystemDarkTheme ? "Dark" : "Light")} mode");
        DarkNet.Instance.SystemDarkThemeChanged += (_, isSystemDarkMode) => Console.WriteLine($"System changed to {(isSystemDarkMode ? "Dark" : "Light")} mode");
    }

}