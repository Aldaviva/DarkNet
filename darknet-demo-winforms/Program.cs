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

        DarkNet.Instance.SetCurrentProcessTheme(Theme.Auto);

        Form mainForm = new Form1();
        DarkNet.Instance.SetFormsWindowTheme(mainForm, Theme.Auto);

        Console.WriteLine($"System is in {(DarkNet.Instance.IsSystemDarkTheme ? "Dark" : "Light")} mode");
        DarkNet.Instance.SystemDarkThemeChanged += (_, isSystemDarkTheme) => Console.WriteLine($"System changed to {(isSystemDarkTheme ? "Dark" : "Light")} mode");

        Application.Run(mainForm);
    }

}