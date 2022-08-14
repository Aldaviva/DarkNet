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
        // darkNet.SetCurrentProcessTheme(Theme.Auto);

        Form mainForm = new Form1();
        darkNet.SetWindowThemeForms(mainForm, Theme.Auto);

        Console.WriteLine($"System is in {(darkNet.UserDefaultAppThemeIsDark ? "Dark" : "Light")} mode");
        Console.WriteLine($"Taskbar is in {(darkNet.UserTaskbarThemeIsDark ? "Dark" : "Light")} mode");

        darkNet.UserDefaultAppThemeIsDarkChanged += (_, isSystemDarkTheme) => Console.WriteLine($"System changed to {(isSystemDarkTheme ? "Dark" : "Light")} mode");
        darkNet.UserTaskbarThemeIsDarkChanged    += (_, isTaskbarDarkTheme) => Console.WriteLine($"Taskbar changed to {(isTaskbarDarkTheme ? "Dark" : "Light")} mode");

        Application.Run(mainForm);
    }

}