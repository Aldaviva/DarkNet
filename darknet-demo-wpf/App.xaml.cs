using System;
using System.Windows;
using Dark.Net;
using Dark.Net.Wpf;

namespace darknet_demo_wpf;

public partial class App {

    protected override void OnStartup(StartupEventArgs e) {
        const Theme processTheme = Theme.Auto;
        IDarkNet    darkNet      = DarkNet.Instance;
        darkNet.SetCurrentProcessTheme(processTheme);
        Console.WriteLine($"Process theme is {processTheme}");
        Console.WriteLine($"System theme is {(darkNet.UserDefaultAppThemeIsDark ? "Dark" : "Light")}");
        Console.WriteLine($"Taskbar theme {(darkNet.UserTaskbarThemeIsDark ? "Dark" : "Light")}");

        new SkinManager().RegisterSkins(new Uri("Skins/Skin.Light.xaml", UriKind.Relative), new Uri("Skins/Skin.Dark.xaml", UriKind.Relative));

        darkNet.UserDefaultAppThemeIsDarkChanged += (_, isSystemDarkTheme) => { Console.WriteLine($"System theme is {(isSystemDarkTheme ? "Dark" : "Light")}"); };
        darkNet.UserTaskbarThemeIsDarkChanged    += (_, isTaskbarDarkTheme) => { Console.WriteLine($"Taskbar theme is {(isTaskbarDarkTheme ? "Dark" : "Light")}"); };

        base.OnStartup(e);
    }

}