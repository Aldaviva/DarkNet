#nullable enable

using System;
using System.Windows.Forms;
using Dark.Net;

namespace darknet_demo_winforms;

internal static class Program {

    [STAThread]
    private static void Main() {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        IDarkNet darkNet      = DarkNet.Instance;
        Theme    processTheme = Theme.Auto;
        darkNet.SetCurrentProcessTheme(processTheme);
        Console.WriteLine($"Process theme is {processTheme}");

        Form  mainForm    = new Form1();
        Theme windowTheme = Theme.Auto;
        darkNet.SetWindowThemeForms(mainForm, windowTheme);
        Console.WriteLine($"Window theme is {windowTheme}");

        Console.WriteLine($"System theme is {(darkNet.UserDefaultAppThemeIsDark ? "Dark" : "Light")}");
        Console.WriteLine($"Taskbar theme is {(darkNet.UserTaskbarThemeIsDark ? "Dark" : "Light")}");

        darkNet.UserDefaultAppThemeIsDarkChanged += (_, isSystemDarkTheme) => Console.WriteLine($"System theme is {(isSystemDarkTheme ? "Dark" : "Light")}");
        darkNet.UserTaskbarThemeIsDarkChanged    += (_, isTaskbarDarkTheme) => Console.WriteLine($"Taskbar theme is {(isTaskbarDarkTheme ? "Dark" : "Light")}");

        Application.Run(mainForm);
    }

}