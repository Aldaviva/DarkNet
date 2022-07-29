using System;
using System.Windows.Forms;
using DarkNet;
using DarkNet.Forms;

#nullable enable

namespace darknet_demo_winforms {

    internal static class Program {

        [STAThread]
        private static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            DarkNetForms darkNet = DarkNetFormsImpl.Instance;
            darkNet.SetCurrentProcessTheme(Theme.Auto);

            Form mainForm = new Form1();
            darkNet.SetWindowTheme(mainForm, Theme.Auto);

            Console.WriteLine($"System is in {(darkNet.IsSystemDarkTheme ? "Dark" : "Light")} mode");
            darkNet.IsSystemDarkThemeChanged += (_, isSystemDarkTheme) => Console.WriteLine($"System changed to {(isSystemDarkTheme ? "Dark" : "Light")} mode");

            Application.Run(mainForm);
        }

    }

}