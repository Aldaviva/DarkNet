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

        IDarkNet darkNet = DarkNet.Instance;
        darkNet.SetCurrentProcessTheme(Theme.Auto);

        Form mainForm = new Form1();
        darkNet.SetWindowThemeForms(mainForm, Theme.Auto);

        Console.WriteLine($"System theme is {(darkNet.UserDefaultAppThemeIsDark ? "Dark" : "Light")}");
        Console.WriteLine($"Taskbar theme is {(darkNet.UserTaskbarThemeIsDark ? "Dark" : "Light")}");

        darkNet.UserDefaultAppThemeIsDarkChanged += (_, isSystemDarkTheme) => Console.WriteLine($"System theme is {(isSystemDarkTheme ? "Dark" : "Light")}");
        darkNet.UserTaskbarThemeIsDarkChanged    += (_, isTaskbarDarkTheme) => Console.WriteLine($"Taskbar theme is {(isTaskbarDarkTheme ? "Dark" : "Light")}");

        Application.Run(mainForm);
    }

}