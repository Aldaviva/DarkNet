using System;
using System.Windows;
using Dark.Net;
using Dark.Net.Wpf;

namespace darknet_demo_wpf_2;

public partial class MainWindow {
    private readonly Theme windowTheme;
    private readonly ElementSkinManager skinManager;
    private readonly ElementSkinManager panelSkinManager;

    public MainWindow() {
        InitializeComponent();

        windowTheme = Theme.Auto;
        skinManager = new ElementSkinManager(this);
        panelSkinManager = new ElementSkinManager(this.SpecialPanel);
        DarkNet.Instance.SetWindowThemeWpf(this, windowTheme);
        skinManager.RegisterSkins(new Uri("Skins/Skin.Light.xaml", UriKind.Relative), new Uri("Skins/Skin.Dark.xaml", UriKind.Relative));
        skinManager.UpdateTheme(windowTheme);
        panelSkinManager.RegisterSkins(new Uri("Skins/Panel.Light.xaml", UriKind.Relative), new Uri("Skins/Panel.Dark.xaml", UriKind.Relative));
        panelSkinManager.UpdateTheme(windowTheme);
        Console.WriteLine($"Window theme is {windowTheme}");
    }

    private void onDarkModeCheckboxChanged(object sender, RoutedEventArgs e) {
        Theme theme = darkModeCheckbox.IsChecked switch {
            true  => Theme.Dark,
            false => Theme.Light,
            null  => Theme.Auto
        };
        DarkNet.Instance.SetWindowThemeWpf(this, theme);
        skinManager.UpdateTheme(theme);
    }

    private void onDarkModePanelCheckboxChanged(object sender, RoutedEventArgs e) {
        Theme theme = darkModePanelCheckbox.IsChecked switch {
            true  => Theme.Dark,
            false => Theme.Light,
            null  => Theme.Auto
        };
        panelSkinManager.UpdateTheme(theme);
    }

}
