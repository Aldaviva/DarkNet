using System;
using System.Windows;
using Dark.Net;
using Dark.Net.Wpf;

namespace darknet_demo_wpf_2;

public partial class App {

    protected override void OnStartup(StartupEventArgs e) {
        const Theme processTheme = Theme.Auto;
        IDarkNet    darkNet      = DarkNet.Instance;
        darkNet.SetCurrentProcessTheme(processTheme);
        Console.WriteLine($"Process theme is {processTheme}");
        Console.WriteLine($"System theme is {(darkNet.UserDefaultAppThemeIsDark ? "Dark" : "Light")}");
        Console.WriteLine($"Taskbar theme {(darkNet.UserTaskbarThemeIsDark ? "Dark" : "Light")}");

        darkNet.UserDefaultAppThemeIsDarkChanged += (_, isSystemDarkTheme) => { Console.WriteLine($"System theme is {(isSystemDarkTheme ? "Dark" : "Light")}"); };
        darkNet.UserTaskbarThemeIsDarkChanged    += (_, isTaskbarDarkTheme) => { Console.WriteLine($"Taskbar theme is {(isTaskbarDarkTheme ? "Dark" : "Light")}"); };

        base.OnStartup(e);

        var w1 = new MainWindow();
        var w2 = new MainWindow();
        w1.Show();
        w2.Show();

    }

}
