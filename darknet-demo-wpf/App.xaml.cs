using System;
using System.Windows;
using Dark.Net;

namespace darknet_demo_wpf;

public partial class App {

    protected override void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);

        IDarkNet darkNet = DarkNet.Instance;
        darkNet.SetCurrentProcessTheme(Theme.Auto);

        Console.WriteLine($"System is in {(darkNet.UserDefaultAppThemeIsDark ? "Dark" : "Light")} mode");
        Console.WriteLine($"Taskbar is in {(darkNet.UserTaskbarThemeIsDark ? "Dark" : "Light")} mode");

        darkNet.UserDefaultAppThemeIsDarkChanged += (_, isSystemDarkMode) => { Console.WriteLine($"System changed to {(isSystemDarkMode ? "Dark" : "Light")} mode"); };
        darkNet.UserTaskbarThemeIsDarkChanged    += (_, isTaskbarDarkMode) => { Console.WriteLine($"Taskbar changed to {(isTaskbarDarkMode ? "Dark" : "Light")} mode"); };
    }

}