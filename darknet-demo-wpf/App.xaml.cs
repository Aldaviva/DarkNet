using System;
using System.Windows;
using Dark.Net;

namespace darknet_demo_wpf;

public partial class App {

    protected override void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);

        const Theme processTheme = Theme.Auto;
        IDarkNet    darkNet      = DarkNet.Instance;
        darkNet.SetCurrentProcessTheme(processTheme);
        Console.WriteLine($"Process theme is {processTheme}");
        Console.WriteLine($"System theme is {(darkNet.UserDefaultAppThemeIsDark ? "Dark" : "Light")}");
        Console.WriteLine($"Taskbar theme {(darkNet.UserTaskbarThemeIsDark ? "Dark" : "Light")}");

        darkNet.UserDefaultAppThemeIsDarkChanged += (_, isSystemDarkTheme) => { Console.WriteLine($"System theme is {(isSystemDarkTheme ? "Dark" : "Light")}"); };
        darkNet.UserTaskbarThemeIsDarkChanged    += (_, isTaskbarDarkTheme) => { Console.WriteLine($"Taskbar theme is {(isTaskbarDarkTheme ? "Dark" : "Light")}"); };
    }

    protected override void OnExit(ExitEventArgs e) {
        DarkNet.Instance.Dispose();
        base.OnExit(e);
    }

}