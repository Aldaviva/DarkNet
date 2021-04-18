using System;
using darknet;

namespace DarkNet {

    public interface DarkNet<in TWindow> {

        /// <summary>
        ///     <para>Allow windows in your app to use dark mode.</para>
        ///     <para>Call this when your process starts, before you show any windows. It is recommended to call this from <c>App_OnStartup</c> in <c>App.xaml.cs</c>, or whatever method you use to handle the <c>Application.Startup</c> event. Note that you can't use <c>Application.StartupUri</c> because that shows the window before you can call this method.</para>
        ///     <para>This method doesn't actually enable dark mode for your windows, it is a prerequisite for calling <see cref="SetModeForWindow" /> for each of your windows once you have created them.</para>
        /// </summary>
        /// <param name="isDarkModeAllowed"><c>true</c> to allow dark mode, <c>false</c> to not allow dark mode (the default).</param>
        /// <exception cref="InvalidOperationException">If this method was called after creating or showing any windows in your app. It has to be called before that, e.g. as the first statement in <c>App_OnStartup</c>.</exception>
        void SetModeForCurrentProcess(Mode mode);

        void SetModeForWindow(Mode mode, TWindow window);

    }

}