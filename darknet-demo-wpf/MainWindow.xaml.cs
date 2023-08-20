using System;
using System.Windows;
using Dark.Net;
using Dark.Net.Wpf;

namespace darknet_demo_wpf;

public partial class MainWindow {

    public MainWindow() {
        InitializeComponent();

        const Theme windowTheme = Theme.Auto;
        DarkNet.Instance.SetWindowThemeWpf(this, windowTheme);
        Console.WriteLine($"Window theme is {windowTheme}");

        SkinManager skinManager = (SkinManager) FindResource("skinManager");
        skinManager.RegisterSkins(new Uri("Skins/Skin.Light.xaml", UriKind.Relative), new Uri("Skins/Skin.Dark.xaml", UriKind.Relative), this);
    }

    private void onDarkModeCheckboxChanged(object sender, RoutedEventArgs e) {
        Theme theme = darkModeCheckbox.IsChecked switch {
            true  => Theme.Dark,
            false => Theme.Light,
            null  => Theme.Auto
        };
        DarkNet.Instance.SetCurrentProcessTheme(theme);
        Console.WriteLine($"Process theme is {theme}");
    }

}