#nullable enable

using System;
using System.Windows.Forms;
using Dark.Net;

namespace darknet_demo_winforms;

public partial class Form1: Form {

    public Form1() {
        InitializeComponent();
    }

    private void onDarkModeCheckboxChanged(object sender, EventArgs e) {
        DarkNet.Instance.SetCurrentProcessTheme(darkModeCheckbox.CheckState switch {
            CheckState.Unchecked     => Theme.Light,
            CheckState.Checked       => Theme.Dark,
            CheckState.Indeterminate => Theme.Auto,
            _                        => Theme.Auto
        });
    }

}