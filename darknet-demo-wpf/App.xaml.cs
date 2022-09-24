using System;
using System.Windows;
using Dark.Net;

namespace darknet_demo_wpf;

public partial class App {

    protected override void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);

        IDarkNet darkNet = DarkNet.Instance;

        Console.WriteLine($"System is in {(darkNet.UserDefaultAppThemeIsDark ? "Dark" : "Light")} mode");
        Console.WriteLine($"Taskbar is in {(darkNet.UserTaskbarThemeIsDark ? "Dark" : "Light")} mode");

        darkNet.UserDefaultAppThemeIsDarkChanged += (_, isSystemDarkTheme) => { Console.WriteLine($"System changed to {(isSystemDarkTheme ? "Dark" : "Light")} theme"); };
        darkNet.UserTaskbarThemeIsDarkChanged    += (_, isTaskbarDarkTheme) => { Console.WriteLine($"Taskbar changed to {(isTaskbarDarkTheme ? "Dark" : "Light")} theme"); };
    }

}