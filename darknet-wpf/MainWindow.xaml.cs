using System;
using System.Windows;
using System.Windows.Interop;

namespace darknet_wpf {

    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow: Window {

        public override void BeginInit() {
            logInitializationState("before BeginInit()");
            base.BeginInit();
            logInitializationState("after BeginInit()");
        }

        protected override void OnInitialized(EventArgs e) {
            logInitializationState("before OnInitialized()");
            base.OnInitialized(e);
            logInitializationState("after OnInitialized()");
        }

        public override void EndInit() {
            logInitializationState("before EndInit()");
            base.EndInit();
            logInitializationState("after EndInit()");
            // App.setDarkModeAllowedForWindow(this, true);
        }

        protected override void OnSourceInitialized(EventArgs e) {
            logInitializationState("before OnSourceInitialized()");
            base.OnSourceInitialized(e);
            logInitializationState("after OnSourceInitialized()");
        }

        public virtual void onBeforeShow() {
            logInitializationState("before Show()");
        }

        public virtual void onShow() {
            logInitializationState("after Show()");
        }

        private void logInitializationState(string caller) {
            bool presentationSourceExists = PresentationSource.FromVisual(this) != null;

            IntPtr windowHandle = new WindowInteropHelper(this).Handle;
            Win32.GetWindowRect(windowHandle, out RECT windowRect);

            var windowPlacement = WINDOWPLACEMENT.Default;
            Win32.GetWindowPlacement(windowHandle, ref windowPlacement);

            var windowInfo = new WINDOWINFO(null);
            Win32.GetWindowInfo(windowHandle, ref windowInfo);

            Console.WriteLine(
                $"{caller}, presentation source {(presentationSourceExists ? "exists" : "does not exist")}, and window.IsInitialized={IsInitialized}, window rect top = {windowRect.Top}, window state = {windowPlacement.ShowCmd}, flags={windowPlacement.Flags}, info.style={Win32.flagsToString(windowInfo.dwStyle)}, info.exStyle={Win32.flagsToString(windowInfo.dwExStyle)}, info.status={windowInfo.dwWindowStatus}");
        }

        public MainWindow() {
            InitializeComponent();
        }

    }

}