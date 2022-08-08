using Dark.Net;

namespace darknet_demo_wpf;

public partial class MainWindow {

    public MainWindow() {
        InitializeComponent();
        DarkNet.Instance.SetWindowThemeWpf(this, Theme.Auto);
    }

}