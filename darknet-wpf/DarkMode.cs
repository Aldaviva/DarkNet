﻿using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace darknet_wpf {

    public static class DarkMode {

        /// <summary>
        ///     call this before showing any windows in your app
        /// </summary>
        /// <param name="isDarkModeAllowed"></param>
        public static void setDarkModeAllowedForProcess(bool isDarkModeAllowed) {
            try {
                bool wasDarkMode = setPreferredAppMode(isDarkModeAllowed ? AppMode.ALLOW_DARK : AppMode.DEFAULT);
                Console.WriteLine($"setPreferredAppMode [wasDarkMode={wasDarkMode}, last error={Marshal.GetLastWin32Error()}]");
            } catch (Exception e1) when (!(e1 is OutOfMemoryException)) {
                try {
                    Console.WriteLine(e1);
                    bool success = allowDarkModeForApp(isDarkModeAllowed);
                    Console.WriteLine($"allowDarkModeForApp [success={success}]");
                } catch (Exception e2) when (!(e2 is OutOfMemoryException)) {
                    throw new Exception("Failed to set dark mode for process", e1);
                }
            }

            refreshImmersiveColorPolicyState();
            Console.WriteLine("refreshImmersiveColorPolicyState");
        }

        internal static void refreshTitleBarThemeColor(IntPtr window) {
            // bool isDarkMode = isDarkModeAllowedForWindow(window) && shouldAppsUseDarkMode() && !isHighContrast();
            bool isDarkMode = true;
            if (!isDarkModeAllowedForWindow(window)) {
                Console.WriteLine("dark mode is not allowed for window");
                isDarkMode = false;
            } else if (!shouldSystemUseDarkMode()) {
                Console.WriteLine("system should not use dark mode");
                isDarkMode = false;
            } else if (isHighContrast()) {
                Console.WriteLine("high contrast enabled");
                isDarkMode = false;
            }

            Console.WriteLine($"dark mode is {(isDarkMode ? "on" : "off")}");

            try {
                // Windows 10 1903 and later
                //might need to pass pointer to struct instead of struct itself
                IntPtr attributeValueBuffer = Marshal.AllocCoTaskMem(Marshal.SizeOf<int>());
                Marshal.WriteInt32(attributeValueBuffer, Convert.ToInt32(true));

                var    windowCompositionAttributeData   = new WindowCompositionAttributeData(WindowCompositionAttribute.WCA_USEDARKMODECOLORS, attributeValueBuffer);
                IntPtr windowCompositionAttributeBuffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(windowCompositionAttributeData));
                Marshal.StructureToPtr(windowCompositionAttributeData, windowCompositionAttributeBuffer, false);
                bool success = setWindowCompositionAttribute(window, windowCompositionAttributeBuffer);
                // setWindowCompositionAttribute(window, windowCompositionAttributeData);
                Console.WriteLine($"setWindowCompositionAttribute [success={success}, lastError={Marshal.GetLastWin32Error()}]");

                // const int WIN10_20H1_BUILD = 19041;
                // DwmWindowAttribute useImmersiveDarkMode = Environment.OSVersion.Version.Build < WIN10_20H1_BUILD
                //     ? DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1
                //     : DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE;
                // int result = dwmSetWindowAttribute(window, useImmersiveDarkMode, attributeValueBuffer, Marshal.SizeOf<int>());
                // Console.WriteLine($"dwmSetWindowAttribute [result={Marshal.GetExceptionForHR(result)}]");
                Marshal.FreeCoTaskMem(attributeValueBuffer);
                Marshal.FreeCoTaskMem(windowCompositionAttributeBuffer);

            } catch (Exception e) when (!(e is OutOfMemoryException)) {
                Console.WriteLine(e);
                // Windows 10 1809 only
                //not sure if the actual address of the bool is needed, or to just make a pointer to address 1 or 0
                bool success = setProp(window, "UseImmersiveDarkModeColors", new IntPtr(Convert.ToInt64(isDarkMode)));
                Console.WriteLine($"setProp [success={success}]");
            }
        }

        private static bool isHighContrast() => SystemParameters.HighContrast;

        /// <summary>
        ///     call this after showing a window
        /// </summary>
        /// <param name="window"></param>
        /// <param name="isDarkModeAllowed"></param>
        public static void setDarkModeAllowedForWindow(Window window, bool isDarkModeAllowed) {
            if (!window.IsVisible) {
                throw new InvalidOperationException("Make sure the window is visible before calling this method. Try calling Window.Show() first.");
            }

            IntPtr windowHandle = new WindowInteropHelper(window).Handle;

            bool wasDarkModeAlreadyAllowed = allowDarkModeForWindow(windowHandle, isDarkModeAllowed);
            Console.WriteLine($"allowDarkModeForWindow [wasDarkModeAlreadyAllowed={wasDarkModeAlreadyAllowed}]");
            // setWindowTheme(windowHandle, isDarkModeAllowed ? "DarkMode_Explorer" : "", null);
            // uint isHighContrast = getIsImmersiveColorUsingHighContrast(ImmersiveHighContrastCacheMode.IHCM_REFRESH);
            // Console.WriteLine($"getIsImmersiveColorUsingHighContrast={isHighContrast}");
            // flushMenuThemes();
            refreshTitleBarThemeColor(windowHandle);
            // flushMenuThemes();
            // SynchronizationContext.Current.Post(state => {
            //     Thread.Sleep(100);
            //     refreshTitleBarThemeColor(windowHandle);
            //
            // invalidateRect(windowHandle, IntPtr.Zero, true);
            // updateWindow(windowHandle);
            //     Thread.Sleep(100);
            // bool success = redrawWindow(windowHandle, IntPtr.Zero, IntPtr.Zero,
            //     RedrawWindowFlags.Frame | RedrawWindowFlags.Invalidate | RedrawWindowFlags.UpdateNow | RedrawWindowFlags.AllChildren | RedrawWindowFlags.EraseNow);
            //     Console.WriteLine($"redrawWindow [success={success}]");
            // }, null);

            // window.UpdateLayout();
            // window.InvalidateVisual();
            // window.InvalidateArrange();
            // window.InvalidateMeasure();
            // window.InvalidateProperty(FrameworkElement.WidthProperty);
            // window.InvalidateProperty(FrameworkElement.HeightProperty);
            //TODO there must be a better way to invalidate DWM's non-client area of this window and force it to repaint
            Visibility originalVisibility = window.Visibility;
            window.Visibility = Visibility.Hidden;
            window.Visibility = originalVisibility;
            // double originalHeight = window.Height;
            // window.Height--;
            // window.Height = originalHeight;
        }

        //these ordinals seem to be decimal
        [DllImport("uxtheme.dll", EntryPoint = "#104")]
        private static extern void refreshImmersiveColorPolicyState();

        [DllImport("uxtheme.dll", EntryPoint = "#132", SetLastError = true)]
        private static extern bool shouldAppsUseDarkMode();

        [DllImport("uxtheme.dll", EntryPoint = "#133", SetLastError = true)]
        private static extern bool allowDarkModeForWindow(IntPtr window, bool isDarkModeAllowed);

        /// <remarks>Available in Windows 10 build 1903 (May 2019 Update) and later</remarks>
        [DllImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true)]
        private static extern bool setPreferredAppMode(AppMode preferredAppMode);

        /// <remarks>Available only in Windows 10 build 1809 (October 2018 Update)</remarks>
        [DllImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true)]
        private static extern bool allowDarkModeForApp(bool isDarkModeAllowed);

        [DllImport("uxtheme.dll", EntryPoint = "#136")]
        private static extern void flushMenuThemes();

        [DllImport("uxtheme.dll", EntryPoint = "#137", SetLastError = true)]
        private static extern bool isDarkModeAllowedForWindow(IntPtr window);

        [DllImport("uxtheme.dll", EntryPoint = "#138", SetLastError = true)]
        private static extern bool shouldSystemUseDarkMode();

        [DllImport("uxtheme.dll", EntryPoint = "#139", SetLastError = true)]
        private static extern bool isDarkModeAllowedForApp();

        [DllImport("uxtheme.dll", EntryPoint = "#106", SetLastError = true)]
        private static extern uint getIsImmersiveColorUsingHighContrast(ImmersiveHighContrastCacheMode cacheMode);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowCompositionAttribute")]
        private static extern bool setWindowCompositionAttribute(IntPtr window, IntPtr windowCompositionAttribute);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowCompositionAttribute")]
        private static extern bool setWindowCompositionAttribute(IntPtr window, WindowCompositionAttributeData windowCompositionAttribute);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "SetProp")]
        private static extern bool setProp(IntPtr window, string propertyName, IntPtr propertyValue);

        [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute", SetLastError = false)]
        private static extern int dwmSetWindowAttribute(IntPtr window, DwmWindowAttribute attribute, IntPtr valuePointer, int valuePointerSize);

        [DllImport("user32.dll", EntryPoint = "InvalidateRect")]
        private static extern bool invalidateRect(IntPtr window, IntPtr rectangle, bool erase);

        [DllImport("user32.dll", EntryPoint = "UpdateWindow")]
        private static extern bool updateWindow(IntPtr window);

        [DllImport("user32.dll", EntryPoint = "RedrawWindow")]
        private static extern bool redrawWindow(IntPtr window, IntPtr lprcUpdate, IntPtr hrgnUpdate, RedrawWindowFlags flags);

        [DllImport("uxtheme.dll", EntryPoint = "SetWindowTheme", CharSet = CharSet.Auto)]
        private static extern uint setWindowTheme(IntPtr window, string applicationName, string clsidNames);

        // [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        // private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        //
        // [DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
        // private static extern IntPtr GetProcAddress(IntPtr hModule, IntPtr ordinal);

    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal readonly struct WindowCompositionAttributeData {

        internal readonly WindowCompositionAttribute attribute;
        internal readonly IntPtr                     data;
        internal readonly int                        size;

        public WindowCompositionAttributeData(WindowCompositionAttribute attribute, IntPtr data) {
            this.attribute = attribute;
            this.data      = data;
            size           = Marshal.SizeOf<int>();
        }

    }

    internal enum WindowCompositionAttribute: uint {

        WCA_UNDEFINED                     = 0,
        WCA_NCRENDERING_ENABLED           = 1,
        WCA_NCRENDERING_POLICY            = 2,
        WCA_TRANSITIONS_FORCEDISABLED     = 3,
        WCA_ALLOW_NCPAINT                 = 4,
        WCA_CAPTION_BUTTON_BOUNDS         = 5,
        WCA_NONCLIENT_RTL_LAYOUT          = 6,
        WCA_FORCE_ICONIC_REPRESENTATION   = 7,
        WCA_EXTENDED_FRAME_BOUNDS         = 8,
        WCA_HAS_ICONIC_BITMAP             = 9,
        WCA_THEME_ATTRIBUTES              = 10,
        WCA_NCRENDERING_EXILED            = 11,
        WCA_NCADORNMENTINFO               = 12,
        WCA_EXCLUDED_FROM_LIVEPREVIEW     = 13,
        WCA_VIDEO_OVERLAY_ACTIVE          = 14,
        WCA_FORCE_ACTIVEWINDOW_APPEARANCE = 15,
        WCA_DISALLOW_PEEK                 = 16,
        WCA_CLOAK                         = 17,
        WCA_CLOAKED                       = 18,
        WCA_ACCENT_POLICY                 = 19,
        WCA_FREEZE_REPRESENTATION         = 20,
        WCA_EVER_UNCLOAKED                = 21,
        WCA_VISUAL_OWNER                  = 22,
        WCA_HOLOGRAPHIC                   = 23,
        WCA_EXCLUDED_FROM_DDA             = 24,
        WCA_PASSIVEUPDATEMODE             = 25,
        WCA_USEDARKMODECOLORS             = 26,
        WCA_LAST                          = 27

    }

    internal enum DwmWindowAttribute {

        DWMWA_NCRENDERING_ENABLED,
        DWMWA_NCRENDERING_POLICY,
        DWMWA_TRANSITIONS_FORCEDISABLED,
        DWMWA_ALLOW_NCPAINT,
        DWMWA_CAPTION_BUTTON_BOUNDS,
        DWMWA_NONCLIENT_RTL_LAYOUT,
        DWMWA_FORCE_ICONIC_REPRESENTATION,
        DWMWA_FLIP3D_POLICY,
        DWMWA_EXTENDED_FRAME_BOUNDS,
        DWMWA_HAS_ICONIC_BITMAP,
        DWMWA_DISALLOW_PEEK,
        DWMWA_EXCLUDED_FROM_PEEK,
        DWMWA_CLOAK,
        DWMWA_CLOAKED,
        DWMWA_FREEZE_REPRESENTATION,
        DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19,
        DWMWA_USE_IMMERSIVE_DARK_MODE             = 20

    }

    internal enum AppMode {

        DEFAULT,
        ALLOW_DARK,
        FORCE_DARK,
        FORCE_LIGHT,
        MAX

    }

    [Flags]
    internal enum RedrawWindowFlags: uint {

        /// <summary>
        ///     Invalidates the rectangle or region that you specify in lprcUpdate or hrgnUpdate.
        ///     You can set only one of these parameters to a non-NULL value. If both are NULL, RDW_INVALIDATE invalidates the entire window.
        /// </summary>
        Invalidate = 0x1,

        /// <summary>Causes the OS to post a WM_PAINT message to the window regardless of whether a portion of the window is invalid.</summary>
        InternalPaint = 0x2,

        /// <summary>
        ///     Causes the window to receive a WM_ERASEBKGND message when the window is repainted.
        ///     Specify this value in combination with the RDW_INVALIDATE value; otherwise, RDW_ERASE has no effect.
        /// </summary>
        Erase = 0x4,

        /// <summary>
        ///     Validates the rectangle or region that you specify in lprcUpdate or hrgnUpdate.
        ///     You can set only one of these parameters to a non-NULL value. If both are NULL, RDW_VALIDATE validates the entire window.
        ///     This value does not affect internal WM_PAINT messages.
        /// </summary>
        Validate = 0x8,

        NoInternalPaint = 0x10,

        /// <summary>Suppresses any pending WM_ERASEBKGND messages.</summary>
        NoErase = 0x20,

        /// <summary>Excludes child windows, if any, from the repainting operation.</summary>
        NoChildren = 0x40,

        /// <summary>Includes child windows, if any, in the repainting operation.</summary>
        AllChildren = 0x80,

        /// <summary>Causes the affected windows, which you specify by setting the RDW_ALLCHILDREN and RDW_NOCHILDREN values, to receive WM_ERASEBKGND and WM_PAINT messages before the RedrawWindow returns, if necessary.</summary>
        UpdateNow = 0x100,

        /// <summary>
        ///     Causes the affected windows, which you specify by setting the RDW_ALLCHILDREN and RDW_NOCHILDREN values, to receive WM_ERASEBKGND messages before RedrawWindow returns, if necessary.
        ///     The affected windows receive WM_PAINT messages at the ordinary time.
        /// </summary>
        EraseNow = 0x200,

        Frame = 0x400,

        NoFrame = 0x800

    }

    internal enum ImmersiveHighContrastCacheMode {

        IHCM_USE_CACHED_VALUE,
        IHCM_REFRESH

    }

}