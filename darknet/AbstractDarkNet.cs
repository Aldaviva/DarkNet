using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using DarkNet;
using Microsoft.Win32;

namespace darknet {

    public abstract class AbstractDarkNet<TWindow>: DarkNet<TWindow> {

        private bool  _preferredSystemDarkModeCached;
        private Mode? _preferredAppMode;

        private readonly ConcurrentDictionary<IntPtr, Mode> _preferredWindowModes = new();

        public bool IsSystemDarkMode => IsSystemModeDark();
        public event IsSystemDarkModeChangedEventHandler? IsSystemDarkModeChanged;

        public abstract void SetModeForCurrentProcess(Mode mode);
        public abstract void SetModeForWindow(Mode         mode, TWindow window);

        /// <summary>call this before showing any windows in your app</summary>
        /// <param name="isDarkModeAllowed"></param>
        internal void SetModeForProcess(Mode mode) {
            _preferredAppMode = mode;
            try {
                Win32.SetPreferredAppMode(AppMode.AllowDark);
            } catch (Exception e1) when (!(e1 is OutOfMemoryException)) {
                try {
                    Win32.AllowDarkModeForApp(true);
                } catch (Exception e2) when (!(e2 is OutOfMemoryException)) {
                    throw new Exception("Failed to set dark mode for process", e1); //TODO throw a different class
                }
            }

            // refreshImmersiveColorPolicyState();
        }

        internal void RefreshTitleBarThemeColor(IntPtr window) {
            if (!_preferredWindowModes.TryGetValue(window, out Mode windowMode)) {
                windowMode = Mode.Auto;
            }

            if (windowMode == Mode.Auto) {
                windowMode = _preferredAppMode ?? Mode.Auto;
            }

            if (windowMode == Mode.Auto) {
                windowMode = IsSystemModeDark() ? Mode.Dark : Mode.Light;
            }

            if (!Win32.IsDarkModeAllowedForWindow(window)) {
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

                Win32.SetWindowCompositionAttribute(window, ref windowCompositionAttributeData);

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
                try {
                    Win32.SetProp(window, "UseImmersiveDarkModeColors", new IntPtr(Convert.ToInt64(isDarkMode)));
                } catch (Exception exception) when (!(exception is OutOfMemoryException)) {
                    throw new Exception("Failed to set dark mode for process", exception); //TODO throw a different class
                }
            }
        }

        internal static bool IsHighContrast() {
            const int getHighContrast = 0x42;
            const int highContrastOn  = 0x1;

            var highContrastData = new HighContrastData(null);
            if (Win32.SystemParametersInfo(getHighContrast, highContrastData.size, ref highContrastData, 0)) {
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
        internal void SetModeForWindow(IntPtr windowHandle, Mode windowMode) {
            bool isNewWindow = false;

            _preferredWindowModes.AddOrUpdate(windowHandle, _ => {
                isNewWindow = true;
                return windowMode;
            }, (_, _) => {
                isNewWindow = false;
                return windowMode;
            });

            if (isNewWindow) {
                ListenForSystemModeChanges(windowHandle);
            }

            Win32.AllowDarkModeForWindow(windowHandle, windowMode != Mode.Light);
            RefreshTitleBarThemeColor(windowHandle);
        }

        internal void OnWindowClosing(IntPtr windowHandle) {
            _preferredWindowModes.TryRemove(windowHandle, out _);
        }

        private void ListenForSystemModeChanges(IntPtr windowHandle) {
            SystemEvents.UserPreferenceChanged += OnSettingsChanged;

            void OnSettingsChanged(object _, UserPreferenceChangedEventArgs args) {
                if (!_preferredWindowModes.ContainsKey(windowHandle)) {
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
        private bool IsSystemModeDark() {
            bool oldValue = _preferredSystemDarkModeCached;
            _preferredSystemDarkModeCached = Win32.ShouldAppsUseDarkMode();
            if (_preferredSystemDarkModeCached != oldValue) {
                IsSystemDarkModeChanged?.Invoke(this, _preferredSystemDarkModeCached);
            }

            return _preferredSystemDarkModeCached;
        }

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

    public delegate void IsSystemDarkModeChangedEventHandler(object sender, bool isSystemDarkMode);

    // public delegate IsSystemDarkModeChangedEventArgs: EventArgs {
    //
    // }

    // public class IsSystemDarkModeChangedEventArgs: EventArgs {
    //
    //     public bool IsSystemDarkMode { get; }
    //
    //     public IsSystemDarkModeChangedEventArgs(bool isSystemDarkMode) {
    //         IsSystemDarkMode = isSystemDarkMode;
    //     }
    //
    // }

}