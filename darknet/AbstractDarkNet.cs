using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Microsoft.Win32;

#nullable enable

namespace DarkNet {

    public abstract class AbstractDarkNet<TWindow>: DarkNet<TWindow> {

        private bool   _preferredSystemDarkModeCached;
        private Theme? _preferredAppMode;

        private readonly ConcurrentDictionary<IntPtr, Theme> _preferredWindowModes = new();

        public bool IsSystemDarkTheme => IsSystemModeDark();
        public event IsSystemDarkThemeChangedEventHandler? IsSystemDarkThemeChanged;

        public abstract void SetCurrentProcessTheme(Theme theme);
        public abstract void SetWindowTheme(TWindow       window, Theme theme);

        /// <summary>call this before showing any windows in your app</summary>
        /// <param name="isDarkModeAllowed"></param>
        internal void SetModeForProcess(Theme theme) {
            _preferredAppMode = theme;
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
            if (!_preferredWindowModes.TryGetValue(window, out Theme windowMode)) {
                windowMode = Theme.Auto;
            }

            if (windowMode == Theme.Auto) {
                windowMode = _preferredAppMode ?? Theme.Auto;
            }

            if (windowMode == Theme.Auto) {
                windowMode = IsSystemModeDark() ? Theme.Dark : Theme.Light;
            }

            if (!Win32.IsDarkModeAllowedForWindow(window)) {
                windowMode = Theme.Light;
            } else if (IsHighContrast()) {
                windowMode = Theme.Light;
            }

            bool isDarkMode = windowMode == Theme.Dark;

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
        internal void SetModeForWindow(IntPtr windowHandle, Theme windowTheme) {
            bool isNewWindow = false;

            _preferredWindowModes.AddOrUpdate(windowHandle, _ => {
                isNewWindow = true;
                return windowTheme;
            }, (_, _) => {
                isNewWindow = false;
                return windowTheme;
            });

            if (isNewWindow) {
                ListenForSystemModeChanges(windowHandle);
            }

            Win32.AllowDarkModeForWindow(windowHandle, windowTheme != Theme.Light);
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
                IsSystemDarkThemeChanged?.Invoke(this, _preferredSystemDarkModeCached);
            }

            return _preferredSystemDarkModeCached;
        }

    }

}