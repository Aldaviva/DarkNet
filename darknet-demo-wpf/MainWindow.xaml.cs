using DarkNet;
using DarkNet.WPF;

#nullable enable

namespace darknet_demo_wpf {

    public partial class MainWindow {

        public MainWindow() {
            InitializeComponent();
            DarkNetWpfImpl.Instance.SetWindowTheme(this, Theme.Auto);
        }

    }

}