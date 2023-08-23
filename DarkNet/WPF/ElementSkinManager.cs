using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Dark.Net.Wpf;

public class ElementSkinManager {

    private          ResourceDictionary? _skinResources;
    private          Uri?                _lightThemeResources;
    private          Uri?                _darkThemeResources;
    private readonly IDarkNet            _darkNet;
    private readonly FrameworkElement      element;

    /// <summary>
    /// Create a new instance that uses the default <see cref="DarkNet"/> instance.
    /// </summary>
    public ElementSkinManager(FrameworkElement element): this(DarkNet.Instance, element) { }

    /// <summary>
    /// Create a new instance that uses a custom <see cref="IDarkNet"/> instance.
    /// </summary>
    public ElementSkinManager(IDarkNet darkNet, FrameworkElement element) {
        _darkNet = darkNet;
        this.element = element;
    }

    public virtual void RegisterSkins(Uri lightThemeResources, Uri darkThemeResources) {
        _darkThemeResources  = darkThemeResources;
        _lightThemeResources = lightThemeResources;

        if (_skinResources == null) {
            Collection<ResourceDictionary> windowResources = element.Resources.MergedDictionaries;
            _skinResources = windowResources.FirstOrDefault(r => r.Source.Equals(lightThemeResources) || r.Source.Equals(darkThemeResources));

            if (_skinResources == null) {
                _skinResources = new ResourceDictionary();
                windowResources.Add(_skinResources);
            }

            UpdateTheme(_darkNet.EffectiveCurrentProcessThemeIsDark);
        }
    }

    /// <summary>
    /// <para>Change the <see cref="FrameworkElement"/>'s <see cref="ResourceDictionary"/>'s <see cref="ResourceDictionary.Source"/> between the light and dark URIs which were set by
    /// <see cref="RegisterSkins"/>.</para>
    /// <para>This is called once by <see cref="RegisterSkins"/>, and again anytime you want to switch between themes for the control.</para>
    /// </summary>
    /// <param name="isDarkTheme"><see langword="true" /> to set dark theme, or <see langword="false"/> to set light theme.</param>
    public virtual void UpdateTheme(bool isDarkTheme) {
        if (_skinResources != null) {
            _skinResources.Source = isDarkTheme ? _darkThemeResources : _lightThemeResources;
        }
    }

    /// <summary>
    /// <para>Change the <see cref="FrameworkElement"/>'s <see cref="ResourceDictionary"/>'s <see cref="ResourceDictionary.Source"/> between the light and dark URIs which were set by
    /// <see cref="RegisterSkins"/>.</para>
    /// <para>This is called once by <see cref="RegisterSkins"/>, and again anytime you want to switch between themes for the control.</para>
    /// </summary>
    /// <param name="theme"><see cref="Theme.Dark"/> to set dark theme, <see cref="Theme.Light"/> to set light theme, or <see cref="Theme.Auto"/> to set current process theme</param>
    public virtual void UpdateTheme(Theme theme)
    {
        var isDarkTheme = theme switch
        {
            Theme.Auto => _darkNet.EffectiveCurrentProcessThemeIsDark,
            Theme.Light => false,
            Theme.Dark => true,
            _ => throw new ArgumentOutOfRangeException(nameof(theme), theme, null)
        };
        UpdateTheme(isDarkTheme);
    }

}
