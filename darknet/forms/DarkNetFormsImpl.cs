using System;
using System.ComponentModel;
using System.Windows.Forms;

#nullable enable

namespace DarkNet.Forms {

    /// <summary>
    ///     <para>Apply Windows 10's dark mode to the title bars and system context menus of Windows Forms windows.</para>
    ///     <para>Be sure to call both <see cref="SetCurrentProcessTheme" /> before showing any windows in your app, and then call <see cref="SetWindowTheme" /> before showing each window.</para>
    /// </summary>
    /// <remarks>Requires Windows 10 version 1809 or later.</remarks>
    public class DarkNetFormsImpl: AbstractDarkNet<Form>, DarkNetForms {

        private static readonly Lazy<DarkNetFormsImpl> LazyInstance = new();
        public static DarkNetForms Instance => LazyInstance.Value;

        /// <summary>
        ///     <para>Allow windows in your app to use dark mode.</para>
        ///     <para>Call this when your process starts, before you show any windows. It is recommended to call this early in <c>Main()</c>.</para>
        ///     <para>This method doesn't actually enable dark mode for your windows, it is a prerequisite for calling <see cref="SetWindowTheme" /> for each of your windows once you have created them.</para>
        /// </summary>
        /// <param name="theme"></param>
        /// <param name="isDarkModeAllowed"><c>true</c> to allow dark mode, <c>false</c> to not allow dark mode (the default).</param>
        /// <exception cref="InvalidOperationException">If this method was called after creating or showing any windows in your app. It has to be called before that, e.g. as the first statement in <c>Main()</c>.</exception>
        public override void SetCurrentProcessTheme(Theme theme) {
            if (Application.OpenForms.Count > 0) { //doesn't help if other windows were already opened and closed before calling this
                throw new InvalidOperationException("Called SetDarkModeAllowedForProcess too late, call it before any calls to Form.Show(), Application.Run(), or " +
                    "DarkNetForms.SetDarkModeAllowedForWindow()");
            }

            SetModeForProcess(theme);
        }

        /// <summary>
        ///     <para>Turn on dark mode for a window.</para>
        ///     <para>You must have already called <see cref="SetCurrentProcessTheme" /> before creating this window.</para>
        ///     <para>You must call this method before calling <see cref="Form.Show" />.</para>
        /// </summary>
        /// <remarks>The correct time to call this method is when the window has already been constructed, but it has not yet been shown (i.e. its Win32 window style must not be visible yet).</remarks>
        /// <param name="window">A Windows Forms window which has been constructed but has not yet been shown.</param>
        /// <param name="theme"></param>
        /// <param name="isDarkModeAllowed"><c>true</c> to make the title bar dark, or <c>false</c> to leave the title bar light (the default).</param>
        /// <exception cref="InvalidOperationException">If this method was called too late (such as after calling <see cref="Form.Show" /> returns).</exception>
        public override void SetWindowTheme(Form window, Theme theme) {
            var windowInfo = new WindowInfo(null);
            Win32.GetWindowInfo(window.Handle, ref windowInfo);
            bool isWindowVisible = (windowInfo.dwStyle & WindowStyles.WsVisible) != 0;
            if (isWindowVisible) {
                throw new InvalidOperationException("Called SetDarkModeAllowedForWindow too late, call it before Form.Show() or Application.Run(), and before " +
                    "DarkNetForms.SetDarkModeAllowedForProcess().");
            }

            SetModeForWindow(window.Handle, theme);

            void OnWindowOnClosing(object sender, CancelEventArgs args) {
                window.Closing -= OnWindowOnClosing;
                OnWindowClosing(window.Handle);
            }

            window.Closing += OnWindowOnClosing;
        }

    }

}