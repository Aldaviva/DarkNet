using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace darknet {

    internal class DarkMode {

        private bool  _preferredSystemDarkModeCached;
        private Mode? _preferredAppMode;

        private readonly ConcurrentDictionary<IntPtr, Mode> PreferredWindowModes = new();

        public bool IsSystemDarkMode => IsSystemModeDark();

        /// <summary>call this before showing any windows in your app</summary>
        /// <param name="isDarkModeAllowed"></param>
        public void SetModeForProcess(Mode mode) {
            _preferredAppMode = mode;
            try {
                bool wasDarkMode = SetPreferredAppMode(AppMode.AllowDark);
                // Console.WriteLine($"setPreferredAppMode [wasDarkMode={wasDarkMode}, last error={Marshal.GetLastWin32Error()}]");
            } catch (Exception e1) when (!(e1 is OutOfMemoryException)) {
                try {
                    bool success = AllowDarkModeForApp(true);
                    // Console.WriteLine($"allowDarkModeForApp [success={success}]");
                } catch (Exception e2) when (!(e2 is OutOfMemoryException)) {
                    throw new Exception("Failed to set dark mode for process", e1); //TODO throw a different class
                }
            }

            // refreshImmersiveColorPolicyState();
        }

        internal void RefreshTitleBarThemeColor(IntPtr window) {
            if (!PreferredWindowModes.TryGetValue(window, out Mode windowMode)) {
                windowMode = Mode.Auto;
            }

            if (windowMode == Mode.Auto) {
                windowMode = _preferredAppMode ?? Mode.Auto;
            }

            if (windowMode == Mode.Auto) {
                windowMode = IsSystemModeDark() ? Mode.Dark : Mode.Light;
            }

            if (!IsDarkModeAllowedForWindow(window)) {
                windowMode = Mode.Light;
            } else if (IsHighContrast()) {
                windowMode = Mode.Light;
            }

            bool isDarkMode = windowMode == Mode.Dark;

            try {
                // Windows 10 1903 and later
                int    attributeValueBufferSize = Marshal.SizeOf(typeof(bool));
                IntPtr attributeValueBuffer     = Marshal.AllocCoTaskMem(attributeValueBufferSize);
                Marshal.WriteInt32(attributeValueBuffer, Convert.ToInt32(isDarkMode));

                var windowCompositionAttributeData = new WindowCompositionAttributeData(WindowCompositionAttribute.WcaUsedarkmodecolors, attributeValueBuffer, attributeValueBufferSize);

                bool success = SetWindowCompositionAttribute(window, ref windowCompositionAttributeData);
                Console.WriteLine($"setWindowCompositionAttribute [success={success}, lastError={Marshal.GetLastWin32Error()}]");

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
                try {
                    SetProp(window, "UseImmersiveDarkModeColors", new IntPtr(Convert.ToInt64(isDarkMode)));
                } catch (Exception exception) when (!(exception is OutOfMemoryException)) {
                    throw new Exception("Failed to set dark mode for process", exception); //TODO throw a different class
                }

                // Console.WriteLine($"setProp [success={success}]");
            }
        }

        internal static bool IsHighContrast() {
            const int getHighContrast = 0x42;
            const int highContrastOn  = 0x1;

            var highContrastData = new HighContrastData(null);
            if (SystemParametersInfo(getHighContrast, highContrastData.size, ref highContrastData, 0)) {
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
        public void SetModeForWindow(IntPtr windowHandle, Mode windowMode) {
            bool isNewWindow = false;

            PreferredWindowModes.AddOrUpdate(windowHandle, _ => {
                isNewWindow = true;
                return windowMode;
            }, (_, _) => {
                isNewWindow = false;
                return windowMode;
            });

            if (isNewWindow) {
                ListenForSystemModeChanges(windowHandle);
            }

            AllowDarkModeForWindow(windowHandle, windowMode != Mode.Light);
            RefreshTitleBarThemeColor(windowHandle);
        }

        internal void OnWindowClosed(IntPtr windowHandle) {
            PreferredWindowModes.TryRemove(windowHandle, out _);
        }

        private void ListenForSystemModeChanges(IntPtr windowHandle) {
            SystemEvents.UserPreferenceChanged += OnSettingsChanged;

            void OnSettingsChanged(object _, UserPreferenceChangedEventArgs args) {
                if (!PreferredWindowModes.ContainsKey(windowHandle)) {
                    SystemEvents.UserPreferenceChanged -= OnSettingsChanged;
                } else if (args.Category == UserPreferenceCategory.General && _preferredSystemDarkModeCached != IsSystemModeDark()) {
                    RefreshTitleBarThemeColor(windowHandle);
                }
            }
        }

        /// <summary>
        ///     <para>This reflects the user's preference in Settings › Personalization › Colors › Choose Your Default App Mode.</para>
        ///     <para>This calls <see cref="ShouldAppsUseDarkMode" /> and caches the result for future change comparisons.</para>
        /// </summary>
        /// <returns><c>true</c> if the system's Default App Mode is Dark, or <c>false</c> if it is Light.</returns>
        private bool IsSystemModeDark() => _preferredSystemDarkModeCached = ShouldAppsUseDarkMode();

        //these ordinals are decimal
        [DllImport("uxtheme.dll", EntryPoint = "#104")]
        private static extern void RefreshImmersiveColorPolicyState();

        [DllImport("uxtheme.dll", EntryPoint = "#132", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShouldAppsUseDarkMode();

        [DllImport("uxtheme.dll", EntryPoint = "#133", SetLastError = true)]
        // [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllowDarkModeForWindow(IntPtr window, bool isDarkModeAllowed);

        /// <remarks>Available in Windows 10 build 1903 (May 2019 Update) and later</remarks>
        [DllImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true)]
        private static extern bool SetPreferredAppMode(AppMode preferredAppMode);

        /// <remarks>Available only in Windows 10 build 1809 (October 2018 Update)</remarks>
        [DllImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true)]
        private static extern bool AllowDarkModeForApp(bool isDarkModeAllowed);

        [DllImport("uxtheme.dll", EntryPoint = "#137", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsDarkModeAllowedForWindow(IntPtr window);

        [Obsolete("Use shouldAppsUseDarkMode() instead")]
        [DllImport("uxtheme.dll", EntryPoint = "#138", SetLastError = true)]
        private static extern bool ShouldSystemUseDarkMode();

        [DllImport("uxtheme.dll", EntryPoint = "#139", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsDarkModeAllowedForApp();

        [DllImport("user32.dll", SetLastError = true)]
        // [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowCompositionAttribute(IntPtr window, ref WindowCompositionAttributeData windowCompositionAttribute);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        // [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetProp(IntPtr window, string propertyName, IntPtr propertyValue);

        [DllImport("dwmapi.dll", SetLastError = false)]
        private static extern int DwmSetWindowAttribute(IntPtr window, DwmWindowAttribute attribute, IntPtr valuePointer, int valuePointerSize);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref HighContrastData callback, uint fwinini);

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
            size              = (uint) Marshal.SizeOf(typeof(HighContrastData));
            flags             = 0;
            schemeNamePointer = IntPtr.Zero;
        }

    }

}