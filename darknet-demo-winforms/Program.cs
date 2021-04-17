using System;
using System.Windows.Forms;
using darknet.forms;

#nullable enable

namespace darknet_demo_winforms {

    internal static class Program {

        [STAThread]
        private static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            DarkNet.SetDarkModeAllowedForProcess(true);

            Form mainForm = new Form1();
            DarkNet.SetDarkModeAllowedForWindow(mainForm, true);
            mainForm.Show();

            Application.Run(mainForm);
        }

    }

}