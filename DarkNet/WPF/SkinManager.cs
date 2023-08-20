using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Dark.Net.Events;

namespace Dark.Net.Wpf;

/// <summary>
/// Automatically swap the current WPF <see cref="Application"/> between a light and dark <see cref="ResourceDictionary"/> when the effective process theme changes between <see cref="Theme.Light"/>
/// and <see cref="Theme.Dark"/>.
/// </summary>
public interface ISkinManager: IDisposable {

    /// <summary>
    /// <para>Specify the URIs to <see cref="ResourceDictionary"/> XAML files that should be applied to the <see cref="Application"/> when the effective process theme is <see cref="Theme.Light"/> or
    /// <see cref="Theme.Dark"/>.</para>
    /// <para>If either <paramref name="lightThemeResources"/> or <paramref name="darkThemeResources"/> is already present in the <see cref="Application"/>'s
    /// <see cref="ResourceDictionary.MergedDictionaries"/>, then that existing <see cref="ResourceDictionary"/> will be reused and its <see cref="ResourceDictionary.Source"/> replaced with the
    /// correct URI based on the process's effective theme. This can aid in WYSIWYG XAML authoring because the XAML editor will have a theme to render for you, and you can manually switch it around in
    /// <c>App.xaml</c> to preview and edit different themes.</para>
    /// <para>If no such <see cref="ResourceDictionary"/> exists already, a new one will be automatically created and added to the <see cref="Application"/>.</para>
    /// <para>This method applies the correct theme immediately, and also sets up an event handler to listen for future updates to <see cref="IDarkNet.EffectiveCurrentProcessThemeIsDark"/> and reapply the
    /// correct theme when it changes.</para>
    /// <para>If you call this method multiple times, it will replace the specified light and dark theme URIs used by the callback, but will not leak event handlers or
    /// <see cref="ResourceDictionary"/> instances.</para>
    /// </summary>
    /// <param name="lightThemeResources"><see cref="Uri"/> of a <see cref="ResourceDictionary"/> to load when the process's effective theme is <see cref="Theme.Light"/>, e.g.
    /// <c>new Uri("/Skins/Skin.Light.xaml", UriKind.Relative)S</c></param>
    /// <param name="darkThemeResources"><see cref="Uri"/> of a <see cref="ResourceDictionary"/> to load when the process's effective theme is <see cref="Theme.Dark"/>, e.g.
    /// <c>new Uri("/Skins/Skin.Dark.xaml", UriKind.Relative)S</c></param>
    void RegisterSkins(Uri lightThemeResources, Uri darkThemeResources);

}

/// <inheritdoc />
public class SkinManager: ISkinManager {

    private Skin? _appSkin;

    private readonly IDarkNet                  _darkNet;
    private readonly IDictionary<Window, Skin> _windowSkins = new Dictionary<Window, Skin>();

    /// <summary>
    /// Create a new instance that uses the default <see cref="DarkNet"/> instance.
    /// </summary>
    public SkinManager(): this(DarkNet.Instance) { }

    /// <summary>
    /// Create a new instance that uses a custom <see cref="IDarkNet"/> instance.
    /// </summary>
    /// <param name="darkNet"></param>
    public SkinManager(IDarkNet darkNet) {
        _darkNet = darkNet;

        _darkNet.EffectiveCurrentProcessThemeIsDarkChanged += UpdateResource;
        _darkNet.EffectiveWindowThemeIsDarkChanged         += UpdateWindowResource;
    }

    /// <inheritdoc />
    public virtual void RegisterSkins(Uri lightThemeResources, Uri darkThemeResources) {
        if (_appSkin == null) {
            Collection<ResourceDictionary> appResources     = Application.Current.Resources.MergedDictionaries;
            ResourceDictionary?            appSkinResources = appResources.FirstOrDefault(r => r.Source.Equals(lightThemeResources) || r.Source.Equals(darkThemeResources));

            if (appSkinResources == null) {
                appSkinResources = new ResourceDictionary();
                appResources.Add(appSkinResources);
            }

            _appSkin = new Skin(lightThemeResources, darkThemeResources, appSkinResources);
        }

        UpdateResource(null, _darkNet.EffectiveCurrentProcessThemeIsDark);
    }

    public virtual void RegisterSkins(Uri lightThemeResources, Uri darkThemeResources, Window window) {
        // not thread safe
        _windowSkins.TryGetValue(window, out Skin? skin);

        if (skin == null) {
            Collection<ResourceDictionary> windowResources     = window.Resources.MergedDictionaries;
            ResourceDictionary?            windowSkinResources = windowResources.FirstOrDefault(r => r.Source.Equals(lightThemeResources) || r.Source.Equals(darkThemeResources));

            if (windowSkinResources == null) {
                windowSkinResources = new ResourceDictionary();
                windowResources.Add(windowSkinResources);
            }

            skin = new Skin(lightThemeResources, darkThemeResources, windowSkinResources);

            _windowSkins[window] = skin;

            window.Closing += OnCloseWindow;
        } else {
            skin.DarkThemeResources  = darkThemeResources;
            skin.LightThemeResources = lightThemeResources;
        }

        UpdateWindowResource(null, new WpfWindowThemeChangedEventArgs(window, _darkNet.GetWindowEffectiveThemeIsDarkWpf(window) ?? false));
    }

    private void OnCloseWindow(object sender, CancelEventArgs e) {
        Window window = (Window) sender;
        _windowSkins.Remove(window);
    }

    /// <summary>
    /// <para>Change the <see cref="Application"/>'s <see cref="ResourceDictionary"/>'s <see cref="ResourceDictionary.Source"/> between the light and dark URIs which were set by
    /// <see cref="RegisterSkins"/>.</para>
    /// <para>This is called once by <see cref="RegisterSkins"/>, and again multiple times when <see cref="IDarkNet.EffectiveCurrentProcessThemeIsDark"/> changes.</para>
    /// </summary>
    /// <param name="eventSource">unused</param>
    /// <param name="isDarkTheme"><see langword="true" /> if the process is set to <see cref="Theme.Dark"/>, or <see langword="false"/> if it is set to <see cref="Theme.Light"/>.</param>
    protected virtual void UpdateResource(object? eventSource, bool isDarkTheme) {
        if (_appSkin != null) {
            _appSkin.WindowSkinResources.Source = isDarkTheme ? _appSkin.DarkThemeResources : _appSkin.LightThemeResources;
        }
    }

    private void UpdateWindowResource(object? sender, WindowThemeChangedEventArgs e) {
        if (e is WpfWindowThemeChangedEventArgs wpfArgs) {
            if (_windowSkins.TryGetValue(wpfArgs.Window, out Skin? skin)) {
                skin.WindowSkinResources.Source = e.EffectiveWindowThemeIsDark ? skin.DarkThemeResources : skin.LightThemeResources;
            }
        }
    }

    /// <inheritdoc cref="IDisposable.Dispose" />
    protected virtual void Dispose(bool disposing) {
        if (disposing) {
            _darkNet.EffectiveCurrentProcessThemeIsDarkChanged -= UpdateResource;
            _darkNet.EffectiveWindowThemeIsDarkChanged         -= UpdateWindowResource;
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private class Skin {

        public          Uri                LightThemeResources;
        public          Uri                DarkThemeResources;
        public readonly ResourceDictionary WindowSkinResources;

        public Skin(Uri lightThemeResources, Uri darkThemeResources, ResourceDictionary windowSkinResources) {
            WindowSkinResources = windowSkinResources;
            LightThemeResources = lightThemeResources;
            DarkThemeResources  = darkThemeResources;
        }

    }

}