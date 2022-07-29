using System;
using System.Runtime.InteropServices;

namespace Dark.Net;

internal static class Win32 {

    private const string User32  = "user32.dll";
    private const string UxTheme = "uxtheme.dll";
    private const string DwmApi  = "dwmapi.dll";

    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport(User32, SetLastError = true)]
    public static extern bool GetWindowInfo(IntPtr hwnd, ref WindowInfo pwi);

    //these EntryPoint ordinals are decimal, not hexadecimal
    [DllImport(UxTheme, EntryPoint = "#104")]
    internal static extern void RefreshImmersiveColorPolicyState();

    [DllImport(UxTheme, EntryPoint = "#132", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ShouldAppsUseDarkMode();

    [DllImport(UxTheme, EntryPoint = "#133", SetLastError = true)]
    // [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool AllowDarkModeForWindow(IntPtr window, bool isDarkModeAllowed);

    /// <remarks>Available in Windows 10 build 1903 (May 2019 Update) and later</remarks>
    [DllImport(UxTheme, EntryPoint = "#135", SetLastError = true)]
    internal static extern bool SetPreferredAppMode(AppMode preferredAppMode);

    /// <remarks>Available only in Windows 10 build 1809 (October 2018 Update)</remarks>
    [DllImport(UxTheme, EntryPoint = "#135", SetLastError = true)]
    internal static extern bool AllowDarkModeForApp(bool isDarkModeAllowed);

    [DllImport(UxTheme, EntryPoint = "#137", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsDarkModeAllowedForWindow(IntPtr window);

    [Obsolete("Use shouldAppsUseDarkMode() instead")]
    [DllImport(UxTheme, EntryPoint = "#138", SetLastError = true)]
    internal static extern bool ShouldSystemUseDarkMode();

    [DllImport(UxTheme, EntryPoint = "#139", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsDarkModeAllowedForApp();

    [DllImport(User32, SetLastError = true)]
    // [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetWindowCompositionAttribute(IntPtr window, ref WindowCompositionAttributeData windowCompositionAttribute);

    [DllImport(User32, SetLastError = true, CharSet = CharSet.Auto)]
    // [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetProp(IntPtr window, string propertyName, IntPtr propertyValue);

    [DllImport(DwmApi, SetLastError = false)]
    internal static extern int DwmSetWindowAttribute(IntPtr window, DwmWindowAttribute attribute, IntPtr valuePointer, int valuePointerSize);

    [DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true)]
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

    public WindowInfo(bool? _): this() {
        // Allows automatic initialization of "cbSize" with "new WINDOWINFO(null/true/false)".
        cbSize = (uint) Marshal.SizeOf<WindowInfo>();
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

[StructLayout(LayoutKind.Sequential)]
internal readonly struct HighContrastData {

    internal readonly uint   size;
    internal readonly uint   flags;
    internal readonly IntPtr schemeNamePointer;

    public HighContrastData(object? _ = null) {
        size              = (uint) Marshal.SizeOf(typeof(HighContrastData));
        flags             = 0;
        schemeNamePointer = IntPtr.Zero;
    }

}

internal enum AppMode {

    Default,
    AllowDark,
    ForceDark,
    ForceLight,
    Max

}

internal enum DwmWindowAttribute {

    DwmwaNcrenderingEnabled,
    DwmwaNcrenderingPolicy,
    DwmwaTransitionsForcedisabled,
    DwmwaAllowNcpaint,
    DwmwaCaptionButtonBounds,
    DwmwaNonclientRtlLayout,
    DwmwaForceIconicRepresentation,
    DwmwaFlip3DPolicy,
    DwmwaExtendedFrameBounds,
    DwmwaHasIconicBitmap,
    DwmwaDisallowPeek,
    DwmwaExcludedFromPeek,
    DwmwaCloak,
    DwmwaCloaked,
    DwmwaFreezeRepresentation,
    DwmwaUseImmersiveDarkModeBefore20H1 = 19,
    DwmwaUseImmersiveDarkMode           = 20

}

internal enum WindowCompositionAttribute: uint {

    WcaUndefined                   = 0,
    WcaNcrenderingEnabled          = 1,
    WcaNcrenderingPolicy           = 2,
    WcaTransitionsForcedisabled    = 3,
    WcaAllowNcpaint                = 4,
    WcaCaptionButtonBounds         = 5,
    WcaNonclientRtlLayout          = 6,
    WcaForceIconicRepresentation   = 7,
    WcaExtendedFrameBounds         = 8,
    WcaHasIconicBitmap             = 9,
    WcaThemeAttributes             = 10,
    WcaNcrenderingExiled           = 11,
    WcaNcadornmentinfo             = 12,
    WcaExcludedFromLivepreview     = 13,
    WcaVideoOverlayActive          = 14,
    WcaForceActivewindowAppearance = 15,
    WcaDisallowPeek                = 16,
    WcaCloak                       = 17,
    WcaCloaked                     = 18,
    WcaAccentPolicy                = 19,
    WcaFreezeRepresentation        = 20,
    WcaEverUncloaked               = 21,
    WcaVisualOwner                 = 22,
    WcaHolographic                 = 23,
    WcaExcludedFromDda             = 24,
    WcaPassiveupdatemode           = 25,
    WcaUsedarkmodecolors           = 26,
    WcaLast                        = 27

}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct WindowCompositionAttributeData {

    internal readonly WindowCompositionAttribute attribute;
    internal readonly IntPtr                     data;
    internal readonly int                        size;

    public WindowCompositionAttributeData(WindowCompositionAttribute attribute, IntPtr data, int size) {
        this.attribute = attribute;
        this.data      = data;
        this.size      = size;
    }

}