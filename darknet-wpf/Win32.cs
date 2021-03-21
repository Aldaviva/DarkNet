using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace darknet_wpf {

    public class Win32 {

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool GetWindowRect(IntPtr hwnd, out RECT result);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);

        internal static string flagsToString<E>(E flags) where E: Enum {
            Type     enumType   = flags.GetType();
            string[] enumNames  = enumType.GetEnumNames();
            Array    enumValues = enumType.GetEnumValues();

            var  stringBuilder   = new StringBuilder();
            bool foundFirstValue = false;
            for (int i = 0; i < enumNames.Length; i++) {
                string name  = enumNames[i];
                object value = enumValues.GetValue(i);

                if (flags.HasFlag((E) value)) {
                    if (foundFirstValue) {
                        stringBuilder.Append('|');
                    }

                    stringBuilder.Append(name);
                    foundFirstValue = true;
                }
            }

            return stringBuilder.ToString();
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWINFO {

        public readonly uint                 cbSize;
        public readonly RECT                 rcWindow;
        public readonly RECT                 rcClient;
        public readonly WindowStyles         dwStyle;
        public readonly ExtendedWindowStyles dwExStyle;
        public readonly uint                 dwWindowStatus;
        public readonly uint                 cxWindowBorders;
        public readonly uint                 cyWindowBorders;
        public readonly ushort               atomWindowType;
        public readonly ushort               wCreatorVersion;

        public WINDOWINFO(bool? filler): this() // Allows automatic initialization of "cbSize" with "new WINDOWINFO(null/true/false)".
        {
            cbSize = (uint) Marshal.SizeOf(typeof(WINDOWINFO));
        }

    }

    /// <summary>
    ///     Contains information about the placement of a window on the screen.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct WINDOWPLACEMENT {

        /// <summary>
        ///     The length of the structure, in bytes. Before calling the GetWindowPlacement or SetWindowPlacement functions, set this member to sizeof(WINDOWPLACEMENT).
        ///     <para>
        ///         GetWindowPlacement and SetWindowPlacement fail if this member is not set correctly.
        ///     </para>
        /// </summary>
        public int Length;

        /// <summary>
        ///     Specifies flags that control the position of the minimized window and the method by which the window is restored.
        /// </summary>
        public int Flags;

        /// <summary>
        ///     The current show state of the window.
        /// </summary>
        public ShowWindowCommands ShowCmd;

        /// <summary>
        ///     The coordinates of the window's upper-left corner when the window is minimized.
        /// </summary>
        public POINT MinPosition;

        /// <summary>
        ///     The coordinates of the window's upper-left corner when the window is maximized.
        /// </summary>
        public POINT MaxPosition;

        /// <summary>
        ///     The window's coordinates when the window is in the restored position.
        /// </summary>
        public RECT NormalPosition;

        /// <summary>
        ///     Gets the default (empty) value.
        /// </summary>
        public static WINDOWPLACEMENT Default {
            get {
                var result = new WINDOWPLACEMENT();
                result.Length = Marshal.SizeOf(result);
                return result;
            }
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT {

        public int X;
        public int Y;

        public POINT(int x, int y) {
            X = x;
            Y = y;
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {

        public int Left, Top, Right, Bottom;

        public RECT(int left, int top, int right, int bottom) {
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

        public static bool operator ==(RECT r1, RECT r2) {
            return r1.Equals(r2);
        }

        public static bool operator !=(RECT r1, RECT r2) {
            return !r1.Equals(r2);
        }

        public bool Equals(RECT r) {
            return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
        }

        public override bool Equals(object obj) {
            if (obj is RECT) {
                return Equals((RECT) obj);
            }

            return false;
        }

        public override string ToString() {
            return string.Format(CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
        }

    }

    [Flags]
    public enum ExtendedWindowStyles: uint {

        WS_EX_DLGMODALFRAME       = 0x00000001,
        WS_EX_NOPARENTNOTIFY      = 0x00000004,
        WS_EX_TOPMOST             = 0x00000008,
        WS_EX_ACCEPTFILES         = 0x00000010,
        WS_EX_TRANSPARENT         = 0x00000020,
        WS_EX_MDICHILD            = 0x00000040,
        WS_EX_TOOLWINDOW          = 0x00000080,
        WS_EX_WINDOWEDGE          = 0x00000100,
        WS_EX_CLIENTEDGE          = 0x00000200,
        WS_EX_CONTEXTHELP         = 0x00000400,
        WS_EX_RIGHT               = 0x00001000,
        WS_EX_LEFT                = 0x00000000,
        WS_EX_RTLREADING          = 0x00002000,
        WS_EX_LTRREADING          = 0x00000000,
        WS_EX_LEFTSCROLLBAR       = 0x00004000,
        WS_EX_RIGHTSCROLLBAR      = 0x00000000,
        WS_EX_CONTROLPARENT       = 0x00010000,
        WS_EX_STATICEDGE          = 0x00020000,
        WS_EX_APPWINDOW           = 0x00040000,
        WS_EX_LAYERED             = 0x00080000,
        WS_EX_NOINHERITLAYOUT     = 0x00100000,
        WS_EX_NOREDIRECTIONBITMAP = 0x00200000,
        WS_EX_LAYOUTRTL           = 0x00400000,
        WS_EX_COMPOSITED          = 0x02000000,
        WS_EX_NOACTIVATE          = 0x08000000

    }

    [Flags]
    public enum WindowStyles: uint {

        WS_OVERLAPPED   = 0x00000000,
        WS_POPUP        = 0x80000000,
        WS_CHILD        = 0x40000000,
        WS_MINIMIZE     = 0x20000000,
        WS_VISIBLE      = 0x10000000,
        WS_DISABLED     = 0x08000000,
        WS_CLIPSIBLINGS = 0x04000000,
        WS_CLIPCHILDREN = 0x02000000,
        WS_MAXIMIZE     = 0x01000000,
        WS_CAPTION      = 0x00C00000,
        WS_BORDER       = 0x00800000,
        WS_DLGFRAME     = 0x00400000,
        WS_VSCROLL      = 0x00200000,
        WS_HSCROLL      = 0x00100000,
        WS_SYSMENU      = 0x00080000,
        WS_THICKFRAME   = 0x00040000,
        WS_GROUP        = 0x00020000,
        WS_TABSTOP      = 0x00010000,
        WS_MINIMIZEBOX  = 0x00020000,
        WS_MAXIMIZEBOX  = 0x00010000

    }

    [Flags]
    internal enum ShowWindowCommands {

        SW_HIDE            = 0,
        SW_SHOWNORMAL      = 1,
        SW_SHOWMINIMIZED   = 2,
        SW_SHOWMAXIMIZED   = 3,
        SW_SHOWNOACTIVATE  = 4,
        SW_SHOW            = 5,
        SW_MINIMIZE        = 6,
        SW_SHOWMINNOACTIVE = 7,
        SW_SHOWNA          = 8,
        SW_RESTORE         = 9

    }

}