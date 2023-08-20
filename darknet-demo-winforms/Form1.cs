#nullable enable

using System;
using System.Drawing;
using System.Windows.Forms;
using Dark.Net;

namespace darknet_demo_winforms;

public partial class Form1: Form {

    public Form1() {
        InitializeComponent();

        DarkNet.Instance.EffectiveCurrentProcessThemeIsDarkChanged += (_, isDarkTheme) => RenderTheme(isDarkTheme);
        RenderTheme(DarkNet.Instance.EffectiveCurrentProcessThemeIsDark);
    }

    private void RenderTheme(bool isDarkTheme) {
        BackColor = isDarkTheme ? Color.FromArgb(19, 19, 19) : Color.White;
        ForeColor = isDarkTheme ? Color.White : Color.Black;
    }

    private void onDarkModeCheckboxChanged(object sender, EventArgs e) {
        Theme theme = darkModeCheckbox.CheckState switch {
            CheckState.Unchecked     => Theme.Light,
            CheckState.Checked       => Theme.Dark,
            CheckState.Indeterminate => Theme.Auto,
            _                        => Theme.Auto
        };
        DarkNet.Instance.SetCurrentProcessTheme(theme, Program.ThemeOptions);
        Console.WriteLine($"Process theme is {theme}");
    }

}