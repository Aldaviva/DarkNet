using System;
using System.Runtime.InteropServices;

namespace darknet {

    internal static class DarkMode {

        /// <summary>call this before showing any windows in your app</summary>
        /// <param name="isDarkModeAllowed"></param>
        public static void SetDarkModeAllowedForProcess(bool isDarkModeAllowed) {
            try {
                bool wasDarkMode = setPreferredAppMode(isDarkModeAllowed ? AppMode.AllowDark : AppMode.Default);
                // Console.WriteLine($"setPreferredAppMode [wasDarkMode={wasDarkMode}, last error={Marshal.GetLastWin32Error()}]");
            } catch (Exception e1) when (!(e1 is OutOfMemoryException)) {
                try {
                    // Console.WriteLine(e1);
                    bool success = allowDarkModeForApp(isDarkModeAllowed);
                    // Console.WriteLine($"allowDarkModeForApp [success={success}]");
                } catch (Exception e2) when (!(e2 is OutOfMemoryException)) {
                    throw new Exception("Failed to set dark mode for process", e1); //TODO throw a different class
                }
            }

            refreshImmersiveColorPolicyState();
            // Console.WriteLine("refreshImmersiveColorPolicyState");
        }

        private static void RefreshTitleBarThemeColor(IntPtr window) {
            // bool isDarkMode = isDarkModeAllowedForWindow(window) && shouldAppsUseDarkMode() && !isHighContrast();
            bool isDarkMode = true;
            if (!isDarkModeAllowedForWindow(window)) {
                // Trace.WriteLine("dark mode is not allowed for window");
                isDarkMode = false;
            } else if (!shouldAppsUseDarkMode()) {
                // Trace.WriteLine("system should not use dark mode");
                isDarkMode = false;
            } else if (isHighContrast()) {
                // Trace.WriteLine("high contrast enabled");
                isDarkMode = false;
            }

            try {
                // Windows 10 1903 and later
                int    attributeValueBufferSize = Marshal.SizeOf<bool>();
                IntPtr attributeValueBuffer     = Marshal.AllocCoTaskMem(attributeValueBufferSize);
                Marshal.WriteInt32(attributeValueBuffer, Convert.ToInt32(isDarkMode));

                var windowCompositionAttributeData = new WindowCompositionAttributeData(WindowCompositionAttribute.WcaUsedarkmodecolors, attributeValueBuffer, attributeValueBufferSize);

                bool success = setWindowCompositionAttribute(window, ref windowCompositionAttributeData);
                // Trace.WriteLine($"setWindowCompositionAttribute [success={success}, lastError={Marshal.GetLastWin32Error()}]");

                // const int WIN10_20H1_BUILD = 19041;
                // DwmWindowAttribute useImmersiveDarkMode = Environment.OSVersion.Version.Build < WIN10_20H1_BUILD
                //     ? DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1
                //     : DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE;
                // int result = dwmSetWindowAttribute(window, useImmersiveDarkMode, attributeValueBuffer, Marshal.SizeOf<bool>());
                // Console.WriteLine($"dwmSetWindowAttribute [result={Marshal.GetExceptionForHR(result)}]");
                Marshal.FreeCoTaskMem(attributeValueBuffer);
                // Marshal.FreeCoTaskMem(windowCompositionAttributeBuffer);

            } catch (Exception e) when (!(e is OutOfMemoryException)) {
                // Windows 10 1809 only
                // Console.WriteLine(e);
                /*bool success = */
                setProp(window, "UseImmersiveDarkModeColors", new IntPtr(Convert.ToInt64(isDarkMode)));
                // Console.WriteLine($"setProp [success={success}]");
            }
        }

        private static bool isHighContrast() {
            const int getHighContrast = 0x42;
            const int highContrastOn  = 0x1;

            var highContrastData = new HighContrastData(null);
            if (systemParametersInfo(getHighContrast, highContrastData.size, ref highContrastData, 0)) {
                return (highContrastData.flags & highContrastOn) != 0;
            }

            return false;
        }

        /// <summary>
        ///     <para>call this after creating but before showing a window, such as WPF's Window.OnSourceInitialized or Forms' Form.Load</para>
        ///     <para>if window.Visibility==VISIBLE and WindowPlacement.ShowCmd == SW_HIDE (or whatever), it was definitely called too early </para>
        ///     <para>if GetWindowInfo().style.WS_VISIBLE == true then it was called too late</para>
        /// </summary>
        /// <param name="windowHandle"></param>
        /// <param name="isDarkModeAllowed"></param>
        public static void SetDarkModeAllowedForWindow(IntPtr windowHandle, bool isDarkModeAllowed) {
            allowDarkModeForWindow(windowHandle, isDarkModeAllowed);
            RefreshTitleBarThemeColor(windowHandle);
        }

        //these ordinals are decimal
        [DllImport("uxtheme.dll", EntryPoint = "#104")]
        private static extern void refreshImmersiveColorPolicyState();

        [DllImport("uxtheme.dll", EntryPoint = "#132", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool shouldAppsUseDarkMode();

        [DllImport("uxtheme.dll", EntryPoint = "#133", SetLastError = true)]
        // [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool allowDarkModeForWindow(IntPtr window, bool isDarkModeAllowed);

        /// <remarks>Available in Windows 10 build 1903 (May 2019 Update) and later</remarks>
        [DllImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true)]
        // [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool setPreferredAppMode(AppMode preferredAppMode);

        /// <remarks>Available only in Windows 10 build 1809 (October 2018 Update)</remarks>
        [DllImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true)]
        // [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool allowDarkModeForApp(bool isDarkModeAllowed);

        [DllImport("uxtheme.dll", EntryPoint = "#137", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool isDarkModeAllowedForWindow(IntPtr window);

        [DllImport("uxtheme.dll", EntryPoint = "#138", SetLastError = true)]
        // [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool shouldSystemUseDarkMode();

        [DllImport("uxtheme.dll", EntryPoint = "#139", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool isDarkModeAllowedForApp();

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowCompositionAttribute")]
        // [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool setWindowCompositionAttribute(IntPtr window, ref WindowCompositionAttributeData windowCompositionAttribute);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "SetProp")]
        // [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool setProp(IntPtr window, string propertyName, IntPtr propertyValue);

        [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute", SetLastError = false)]
        private static extern int dwmSetWindowAttribute(IntPtr window, DwmWindowAttribute attribute, IntPtr valuePointer, int valuePointerSize);

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfo", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool systemParametersInfo(uint uiAction, uint uiParam, ref HighContrastData callback, uint fwinini);

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

    internal enum AppMode {

        Default,
        AllowDark,
        ForceDark,
        ForceLight,
        Max

    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct HighContrastData {

        internal readonly uint   size;
        internal readonly uint   flags;
        internal readonly IntPtr schemeNamePointer;

        public HighContrastData(object? _ = null) {
            size              = (uint) Marshal.SizeOf<HighContrastData>();
            flags             = 0;
            schemeNamePointer = IntPtr.Zero;
        }

    }

}