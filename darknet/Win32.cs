using System;
using System.Runtime.InteropServices;

namespace darknet {

    internal static class Win32 {

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowInfo(IntPtr hwnd, ref WindowInfo pwi);

        //these ordinals are decimal
        [DllImport("uxtheme.dll", EntryPoint = "#104")]
        internal static extern void RefreshImmersiveColorPolicyState();

        [DllImport("uxtheme.dll", EntryPoint = "#132", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ShouldAppsUseDarkMode();

        [DllImport("uxtheme.dll", EntryPoint = "#133", SetLastError = true)]
        // [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AllowDarkModeForWindow(IntPtr window, bool isDarkModeAllowed);

        /// <remarks>Available in Windows 10 build 1903 (May 2019 Update) and later</remarks>
        [DllImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true)]
        internal static extern bool SetPreferredAppMode(AppMode preferredAppMode);

        /// <remarks>Available only in Windows 10 build 1809 (October 2018 Update)</remarks>
        [DllImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true)]
        internal static extern bool AllowDarkModeForApp(bool isDarkModeAllowed);

        [DllImport("uxtheme.dll", EntryPoint = "#137", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsDarkModeAllowedForWindow(IntPtr window);

        [Obsolete("Use shouldAppsUseDarkMode() instead")]
        [DllImport("uxtheme.dll", EntryPoint = "#138", SetLastError = true)]
        internal static extern bool ShouldSystemUseDarkMode();

        [DllImport("uxtheme.dll", EntryPoint = "#139", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsDarkModeAllowedForApp();

        [DllImport("user32.dll", SetLastError = true)]
        // [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetWindowCompositionAttribute(IntPtr window, ref WindowCompositionAttributeData windowCompositionAttribute);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        // [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetProp(IntPtr window, string propertyName, IntPtr propertyValue);

        [DllImport("dwmapi.dll", SetLastError = false)]
        internal static extern int DwmSetWindowAttribute(IntPtr window, DwmWindowAttribute attribute, IntPtr valuePointer, int valuePointerSize);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref HighContrastData callback, uint fwinini);

    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct WindowInfo {

        public readonly uint                 cbSize;
        public readonly Rect                 rcWindow;
        public readonly Rect                 rcClient;
        public readonly WindowStyles         dwStyle;
        public readonly ExtendedWindowStyles dwExStyle;
        public readonly uint                 dwWindowStatus;
        public readonly uint                 cxWindowBorders;
        public readonly uint                 cyWindowBorders;
        public readonly ushort               atomWindowType;
        public readonly ushort               wCreatorVersion;

        public WindowInfo(bool? _): this() // Allows automatic initialization of "cbSize" with "new WINDOWINFO(null/true/false)".
        {
            cbSize = (uint) Marshal.SizeOf(typeof(WindowInfo));
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect {

        private int Left;
        private int Top;
        private int Right;
        private int Bottom;

        public Rect(int left, int top, int right, int bottom) {
            Left   = left;
            Top    = top;
            Right  = right;
            Bottom = bottom;
        }

        public int X {
            get => Left;
            set {
                Right -= Left - value;
                Left  =  value;
            }
        }

        public int Y {
            get => Top;
            set {
                Bottom -= Top - value;
                Top    =  value;
            }
        }

        public int Height {
            get => Bottom - Top;
            set => Bottom = value + Top;
        }

        public int Width {
            get => Right - Left;
            set => Right = value + Left;
        }

    }

    [Flags]
    internal enum ExtendedWindowStyles: uint {

        WsExDlgmodalframe       = 0x00000001,
        WsExNoparentnotify      = 0x00000004,
        WsExTopmost             = 0x00000008,
        WsExAcceptfiles         = 0x00000010,
        WsExTransparent         = 0x00000020,
        WsExMdichild            = 0x00000040,
        WsExToolwindow          = 0x00000080,
        WsExWindowedge          = 0x00000100,
        WsExClientedge          = 0x00000200,
        WsExContexthelp         = 0x00000400,
        WsExRight               = 0x00001000,
        WsExLeft                = 0x00000000,
        WsExRtlreading          = 0x00002000,
        WsExLtrreading          = 0x00000000,
        WsExLeftscrollbar       = 0x00004000,
        WsExRightscrollbar      = 0x00000000,
        WsExControlparent       = 0x00010000,
        WsExStaticedge          = 0x00020000,
        WsExAppwindow           = 0x00040000,
        WsExLayered             = 0x00080000,
        WsExNoinheritlayout     = 0x00100000,
        WsExNoredirectionbitmap = 0x00200000,
        WsExLayoutrtl           = 0x00400000,
        WsExComposited          = 0x02000000,
        WsExNoactivate          = 0x08000000

    }

    [Flags]
    internal enum WindowStyles: uint {

        WsOverlapped   = 0x00000000,
        WsPopup        = 0x80000000,
        WsChild        = 0x40000000,
        WsMinimize     = 0x20000000,
        WsVisible      = 0x10000000,
        WsDisabled     = 0x08000000,
        WsClipsiblings = 0x04000000,
        WsClipchildren = 0x02000000,
        WsMaximize     = 0x01000000,
        WsCaption      = 0x00C00000,
        WsBorder       = 0x00800000,
        WsDlgframe     = 0x00400000,
        WsVscroll      = 0x00200000,
        WsHscroll      = 0x00100000,
        WsSysmenu      = 0x00080000,
        WsThickframe   = 0x00040000,
        WsGroup        = 0x00020000,
        WsTabstop      = 0x00010000,
        WsMinimizebox  = 0x00020000,
        WsMaximizebox  = 0x00010000

    }

}