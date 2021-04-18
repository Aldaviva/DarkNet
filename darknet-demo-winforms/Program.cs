using System;
using System.Windows.Forms;
using darknet;
using darknet.forms;

#nullable enable

namespace darknet_demo_winforms {

    internal static class Program {

        [STAThread]
        private static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            DarkNetForms darkNet = new DarkNetFormsImpl();
            darkNet.SetModeForCurrentProcess(Mode.Auto);

            Form mainForm = new Form1();
            darkNet.SetModeForWindow(Mode.Auto, mainForm);
            mainForm.Show();

            Application.Run(mainForm);
        }

    }

}