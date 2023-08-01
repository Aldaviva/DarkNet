![DarkNet logo](https://raw.githubusercontent.com/Aldaviva/DarkNet/master/.github/images/readme-logo.png) DarkNet
===

[![Nuget](https://img.shields.io/nuget/v/DarkNet?logo=nuget)](https://www.nuget.org/packages/DarkNet/) [![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/Aldaviva/darknet/dotnetpackage.yml?branch=master&logo=github)](https://github.com/Aldaviva/DarkNet/actions/workflows/dotnetpackage.yml)

Enable native Windows dark mode for your WPF and Windows Forms title bars.

<!-- Absolute URL so the URL resolves when viewed not on GitHub, for example, on NuGet Gallery -->
![WPF window with dark title bar](https://raw.githubusercontent.com/Aldaviva/DarkNet/master/.github/images/demo-wpf2.png)

<!-- MarkdownTOC autolink="true" bracket="round" autoanchor="false" levels="1,2,3,4" -->

- [Requirements](#requirements)
- [Installation](#installation)
- [Usage](#usage)
    - [Basics](#basics)
        - [Entry point](#entry-point)
        - [Methods](#methods)
        - [Themes](#themes)
    - [WPF](#wpf)
        - [On application startup](#on-application-startup)
        - [Before showing a new window](#before-showing-a-new-window)
        - [After showing a window](#after-showing-a-window)
        - [Client area](#client-area)
        - [Complete example](#complete-example)
    - [Windows Forms](#windows-forms)
        - [On application startup](#on-application-startup-1)
        - [Before showing a new window](#before-showing-a-new-window-1)
        - [After showing a window](#after-showing-a-window-1)
        - [Complete example](#complete-example-1)
    - [HWND](#hwnd)
    - [Effective application theme](#effective-application-theme)
    - [Taskbar theme](#taskbar-theme)
    - [Custom colors](#custom-colors)
- [Demos](#demos)
    - [WPF](#wpf-1)
    - [Windows Forms](#windows-forms-1)
- [Limitations](#limitations)
- [Acknowledgements](#acknowledgements)

<!-- /MarkdownTOC -->

## Requirements

- .NET runtime
    - .NET 5.0 or later
    - .NET Core 3.1 or later
    - .NET Framework 4.5.2 or later
- Windows
    - Windows 11 or later
    - Windows 10 version 1809 (October 2018 Update) or later
    - You can still run your program on earlier Windows versions as well, but the title bar won't turn dark
- Windows Presentation Foundation, Windows Forms, or access to the native window handle of other windows in your process

## Installation

[DarkNet is available in NuGet Gallery.](https://www.nuget.org/packages/DarkNet/)

```ps1
dotnet add package DarkNet
```
```ps1
Install-Package DarkNet
```

## Usage

### Basics

#### Entry point

The top-level interface of this library is **`Dark.Net.IDarkNet`**, which is implemented by the **`DarkNet`** class. A shared instance of this class is available from **`DarkNet.Instance`**, or you can construct a new instance with `new DarkNet()`.

#### Methods

1. First, you may optionally call **`SetCurrentProcessTheme(Theme)`** to define a default theme for your windows, although it doesn't actually apply the theme to any windows on its own.

    This method also controls the theme of the application's context menu, which appears when you right-click the title bar of any of its windows. This means all windows in your app will have the same context menu theme, even if you set their window themes differently.
    
    If you don't call this method, any window on which you call `SetWindowTheme*(myWindow, Theme.Auto)` will inherit its theme from the operating system's default app theme, skipping this app-level default.
2. Next, you must call one of the **`SetWindowTheme*`** methods to actually apply a theme to each window. There are three methods to choose from, depending on what kind of window you have:
    - **WPF:** `SetWindowThemeWpf(Window, Theme, ThemeOptions?)`
    - **Forms:** `SetWindowThemeForms(Form, Theme, ThemeOptions?)`
    - **HWND:** `SetWindowThemeRaw(IntPtr, Theme, ThemeOptions?)`

    If you don't call one of these methods on a given window, that window will always use the light theme, even if you called `SetCurrentProcessTheme` and set the OS default app mode to dark.

    *These are three separate methods instead of one overloaded method in order to prevent apps from having to depend on **both** WPF and Windows Forms when they only intend to use one, because an overloaded method signature can't be resolved when any of the parameters' types are missing.*

#### Themes

This library uses the `Theme` enum to differentiate **`Dark`** mode from **`Light`** mode. You can set any window in your application to use whichever theme you want, they don't all have to be the same theme.

There is also an **`Auto`** theme value that allows the theme to be inherited from a higher level, falling back from the **window** to the **process** to the user account **default app theme**:

1. When a **window's** theme is `Dark` or `Light`, it uses that theme directly, set by `SetWindowTheme*`.
2. When a **window's** theme is `Auto`, it inherits from the **process's** theme set by `SetCurrentProcessTheme`.
3. When a **window and its process's** themes are both `Auto`, they inherit from the local user's operating system **default app theme** preferences (Settings › Personalization › Colors › Choose your default app mode).

##### Live updates

If your app is running with the `Auto` theme at both the window and process levels, and the user changes their OS default app mode in Settings, then your app will automatically render with the new theme, without you having to handle any events or restart the app.

Try the [demo apps](#demos) to see this behavior in action.

### WPF

#### On application startup

Before showing **any** windows in your application, you may optionally call
```cs
IDarkNet.SetCurrentProcessTheme(theme);
```

A good place to call this is in an event handler for your Application's **`Startup`** event, or in its overridden **`OnStartup`** method.

By default, WPF apps have an `App.xaml.cs` class that inherits from `Application`. You can override `OnStartup` to initialize DarkNet:

```cs
// App.xaml.cs
using Dark.Net;

public partial class App: Application {

    protected override void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);
        DarkNet.Instance.SetCurrentProcessTheme(Theme.Auto);
    }
}
```

#### Before showing a new window

Before showing **each** window in your application, you have to set the theme for that window.

```cs
IDarkNet.SetWindowThemeWpf(window, theme);
```

A good place to call this is in the window's constructor **after the call to `InitializeComponent`**, or in an event handler for the window's **`SourceInitialized`** event.

If you call it too late (such as after the window is shown), the theme will not change.

```cs
// MainWindow.xaml.cs
using Dark.Net;

public partial class MainWindow {

    public MainWindow() {
        InitializeComponent();
        DarkNet.Instance.SetWindowThemeWpf(this, Theme.Auto);
    }

}
```

You must perform this step for **every** window you show in your application, not just the first one.

#### After showing a window

After calling `SetWindowThemeWpf` for the first time and showing a new window, you may optionally make additional calls to `SetCurrentProcessTheme` or `SetWindowThemeWpf` multiple times to change the theme later. This is useful if you let users choose a theme in your app's settings.

Try the [demo apps](#demos) to see this behavior in action.

#### Client area

DarkNet does not give controls in the client area of your windows a dark skin. It only changes the theme of the title bar and system context menu. It is up to you to make the inside of your windows dark.

However, this library can help you switch your WPF application resource dictionaries to apply different styles when the process title bar theme changes.

This limited class currently only handles process theme changes, and does not handle individual windows using different themes in the same process. For more fine-grained skin management, see [pull request #8](https://github.com/Aldaviva/DarkNet/pull/8).

This requires you to create two resource dictionary XAML files, one for the light theme and one for dark. To tell DarkNet to switch between the resource dictionaries when the process theme changes, register them with [**`SkinManager`**](https://github.com/Aldaviva/DarkNet/blob/master/DarkNet/Wpf/SkinManager.cs):

```cs
new Dark.Net.Wpf.SkinManager().RegisterSkins(
    lightThemeResources: new Uri("Skins/Skin.Light.xaml", UriKind.Relative),
    darkThemeResources:  new Uri("Skins/Skin.Dark.xaml", UriKind.Relative));
```

After you register the skin URIs, `SkinManager` will change the `Source` property of the resource dictionary to point to the correct skin, once immediately when you call `RegisterSkins` and again whenever the current process theme changes in the future.

In your skin resource dictionaries, you can define resources, such as a `Color`, `Brush`, or `BitmapImage`, that are referred to using a `DynamicResource` binding somewhere in your window. You can also create a `Style` that applies to a `TargetType`, or that you refer to with a `DynamicResource`. These can apply a `ControlTemplate` to change the default appearance of controls like `CheckBox`, which unfortunately has property triggers that must be completely replaced in order to be restyled, instead of using template bindings or attached properties. Try to use `DynamicResource` instead of `StaticResource` so that the style can update when the skin is changed.

To get started overriding a control's default styles and control template, right-click the control in the Visual Studio XAML Designer, select Edit Template › Edit a Copy…, then define the resource in your skin's resource dictionary. 

See [`darknet-demo-wpf`](https://github.com/Aldaviva/DarkNet/tree/master/darknet-demo-wpf) for an example.
- [`Skins/`](https://github.com/Aldaviva/DarkNet/tree/master/darknet-demo-wpf/Skins) contains the resource dictionaries for [light](https://github.com/Aldaviva/DarkNet/blob/master/darknet-demo-wpf/Skins/Skin.Light.xaml) and [dark](https://github.com/Aldaviva/DarkNet/blob/master/darknet-demo-wpf/Skins/Skin.Dark.xaml) skins, as well as [common](https://github.com/Aldaviva/DarkNet/blob/master/darknet-demo-wpf/Skins/Skin.Common.xaml) resources that are shared by both skins.
    - The dark skin in `Skin.Dark.xaml` overrides the styles and control template for the `CheckBox` to get rid of the light background, including when the mouse pointer is hovering over or clicking on it.
- [`App.xaml.cs`](https://github.com/Aldaviva/DarkNet/blob/master/darknet-demo-wpf/App.xaml.cs) registers the skins with `SkinManager`.
    - When referring to XAML files in other assemblies, or when processing assemblies with tools like [`ILRepack`](https://github.com/ravibpatel/ILRepack.Lib.MSBuild.Task), the skin URIs can change. You may need to [try multiple fallback URIs](https://github.com/Aldaviva/RemoteDesktopServicesCertificateSelector/blob/4beb7893ac99ef0a3bc512feb93398ea8993c9a6/RemoteDesktopServicesCertificateSelector/App.xaml.cs#L44-L59) depending on whether the app was built in a Debug or Release configuration.
- [`App.xaml`](https://github.com/Aldaviva/DarkNet/blob/master/darknet-demo-wpf/App.xaml) can optionally point to either the dark or light skin in its merged resource dictionary, which will be replaced at runtime by `SkinManager`.
    - The value you set here is useful for previewing during Visual Studio XAML authoring. If you choose not to add a resource dictionary here, `SkinManager` will add one for you automatically at runtime, but the skin will be missing from XAML authoring previews and they will look broken in Visual Studio.
    - The app's merged dictionary can additionally refer to your other unrelated resource dictionaries and resources, which won't be touched by `SkinManager`.
- [`MainWindow.xaml`](https://github.com/Aldaviva/DarkNet/blob/master/darknet-demo-wpf/MainWindow.xaml) binds the `Window` `Style` property to a `DynamicResource` pointing to the `windowStyle` resource, which is defined twice in both the light and dark skin resource dictionaries. You need to explicitly bind `Window` styles like this instead of using a `TargetType` on your `Style` because `TargetType` does not apply to superclasses, and the concrete class here is `darknet_demo_wpf.MainWindow` instead of `System.Windows.Window`.

Additional examples of WPF skins for light and dark themes:
- [Aldaviva/Trainers](https://github.com/Aldaviva/Trainers/tree/master/TrainerCommon/App/Skins)
- [Aldaviva/RemoteDesktopServicesCertificateSelector](https://github.com/Aldaviva/RemoteDesktopServicesCertificateSelector/tree/master/RemoteDesktopServicesCertificateSelector/Views/Skins)

#### Complete example

See the demo [`App.xaml.cs`](https://github.com/Aldaviva/DarkNet/blob/master/darknet-demo-wpf/App.xaml.cs) and [`MainWindow.xaml.cs`](https://github.com/Aldaviva/DarkNet/blob/master/darknet-demo-wpf/MainWindow.xaml.cs).

### Windows Forms

#### On application startup

Before showing **any** windows in your application, you may optionally call
```cs
IDarkNet.SetCurrentProcessTheme(theme);
```

A good place to call this is at the **start of the `Main()` method** of your application.

```cs
// Program.cs
using Dark.Net;

internal static class Program {

    [STAThread]
    private static void Main() {
        DarkNet.Instance.SetCurrentProcessTheme(Theme.Auto);

        // Incomplete example; see below for complete example including showing your first window.
    }
}
```

#### Before showing a new window

Before showing **each** window in your application, you have to set the theme for that window.

```cs
IDarkNet.SetWindowThemeForms(window, theme);
```

You must do this **before calling `Show()` or `Application.Run()`** to show the window. If you call it too late (such as after the window is shown), the theme will not change.

```cs
Form mainForm = new Form1();
DarkNet.Instance.SetWindowThemeForms(mainForm, Theme.Auto);
```

You must perform this step for **every** window you show in your application, not just the first one.

#### After showing a window

After calling `SetWindowThemeForms` for the first time and showing a new window, you may optionally make additional calls to `SetCurrentProcessTheme` or `SetWindowThemeForms` multiple times to change the theme later. This is useful if you let users choose a theme in your app's settings.

Try the [demo apps](#demos) to see this behavior in action.

#### Complete example

```cs
// Program.cs
using Dark.Net;

internal static class Program {

    [STAThread]
    private static void Main() {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        DarkNet.Instance.SetCurrentProcessTheme(Theme.Auto);

        Form mainForm = new Form1();
        DarkNet.Instance.SetWindowThemeForms(mainForm, Theme.Auto);

        Application.Run(mainForm);
    }

}
```

See also the demo [`Program.cs`](https://github.com/Aldaviva/DarkNet/blob/master/darknet-demo-winforms/Program.cs) and [`Form1.cs`](https://github.com/Aldaviva/DarkNet/blob/master/darknet-demo-winforms/Form1.cs).

### HWND

If you want to change the theme of a window in your application that was not created with WPF or Windows Forms, you can also just pass the raw HWND of the window to `SetWindowThemeRaw(IntPtr, Theme)`.

### Effective application theme

If you want to know which theme was rendered as a result of setting your app's theme to `Auto` using `SetCurrentProcessTheme`, you can get **`EffectiveCurrentProcessThemeIsDark`**. This will return whether the actual title bar theme is dark or light, and it reflects the theme you set, the user's OS color settings, and the high contrast setting. Changes are emitted from the **`EffectiveCurrentProcessThemeIsDarkChanged`** event.

This can be useful if you want to set the theme to `Auto` and then skin your app's client area based on the Windows default app mode setting. It also helps you keep a single, authoritative copy of this state instead of having to maintain a second one and keep them in sync.

### Taskbar theme

Windows introduced a preference to choose a dark or light taskbar in Windows 10 version 1903. This is controlled by Settings › Personalization › Colors › Choose your default Windows mode.

DarkNet exposes the value of this preference with the **`UserTaskbarThemeIsDark`** property, as well as the change event **`UserTaskbarThemeIsDarkChanged`**. You can use these to render a tray icon in the notification area that matches the taskbar's theme, and re-render it when the user preference changes.

### Custom colors

Windows 11 introduced the ability to override colors in the non-client area of individual windows. You can change the title bar's text color and background color, as well as the window's border color.

To specify custom colors for a WPF, Forms, or HWND window, pass the optional parameter `ThemeOptions? options` to one of the `SetWindowTheme*()` methods. For example, this invocation gives a WPF window a blue title bar theme.

```cs
DarkNet.Instance.SetWindowThemeWpf(this, Theme.Dark, new ThemeOptions {
    TitleBarTextColor       = Color.MidnightBlue,
    TitleBarBackgroundColor = Color.PowderBlue,
    WindowBorderColor       = Color.DarkBlue
});
```

![custom colors](https://raw.githubusercontent.com/Aldaviva/DarkNet/master/.github/images/demo-wpf-customcolors.png)

You can pass any or all of the three properties `TitleBarTextColor`, `TitleBarBackgroundColor`, and `WindowBorderColor`. You can pass a custom RGB value using `Color.FromArgb(red: 255, green: 127, blue: 0)`. Alpha values are ignored. Properties that you omit or leave `null` will remain unchanged from their previous appearance, which would be either the last value you set, or the OS default color for your `Theme` if you never set it for the window.

By default, Windows will automatically change the color of the title bar text and minimize, maximize/restore, and close button icons to light or dark in order to provide maximal contrast with `TitleBarBackgroundColor`. If you set `TitleBarTextColor`, Windows will use it for the title text, and will only change the button icon color automatically.

To apply the same custom colors to all of the windows in your process, you may instead pass the `ThemeOptions` to `SetCurrentProcessTheme(Theme, ThemeOptions?)`, then omit the `options` parameter when you call `SetWindowTheme*(window, theme, options)`. Alternatively, you may set some of the properties at the process level and set others at the window level. You may also set a property at both the process and window level, and the window level value will take precedence.

To remove the window border entirely, set `WindowBorderColor` to `ThemeOptions.NoWindowBorder`.

If you previously set any of these properties to a custom color, and want to revert it to the standard OS color for your chosen `Theme`, set the property to `ThemeOptions.DefaultColor`.

On Windows 10 and earlier versions, these options will have no effect.

## Demos

You can download the following precompiled demos, or clone this repository and build the demo projects yourself using Visual Studio Community 2022.

### WPF

Download and run `darknet-demo-wpf.exe` from the [latest release](https://github.com/Aldaviva/DarkNet/releases), or [view source](https://github.com/Aldaviva/DarkNet/blob/master/darknet-demo-wpf/App.xaml.cs).

Requires [.NET Desktop Runtime 6 x64](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) or later.

![WPF window with dark title bar](https://raw.githubusercontent.com/Aldaviva/DarkNet/master/.github/images/demo-wpf2.png)

### Windows Forms

Download and run `darknet-demo-winforms.exe` from the [latest release](https://github.com/Aldaviva/DarkNet/releases), or [view source](https://github.com/Aldaviva/DarkNet/blob/master/darknet-demo-winforms/Program.cs).

Requires [.NET Framework 4.8](https://dotnet.microsoft.com/en-us/download/dotnet-framework) or later.

![Windows Forms window with dark title bar](https://raw.githubusercontent.com/Aldaviva/DarkNet/master/.github/images/demo-winforms2.png)

## Limitations
- This library only changes the theme of the title bar/window chrome/non-client area, as well as the system context menu (the menu that appears when you right click on the title bar, or left click on the title bar icon, or hit `Alt`+`Space`). It does not change the theme of the client area of your window. It is up to you to make that look different when dark mode is enabled. This is difficult with Windows Forms, [particularly in .NET Core](https://github.com/gitextensions/gitextensions/issues/9191).
    - For simple examples of automatically switching WPF resource dictionaries using [`SkinManager`](#client-area) and writing dark and light mode skins, see [Aldaviva/Trainers](https://github.com/Aldaviva/Trainers/blob/master/TrainerCommon/App/CommonApp.cs) and [Aldaviva/RemoteDesktopServicesCertificateSelector](https://github.com/Aldaviva/RemoteDesktopServicesCertificateSelector/blob/master/RemoteDesktopServicesCertificateSelector/App.xaml.cs).
- This library currently does not help you persist a user's choice for the mode they want your application to use across separate process executions. You can expose an option and persist that yourself, then pass the desired `Theme` value to the methods in this library.

## Acknowledgements

- [Milan Burda](https://github.com/miniak) for [explaining how to add this in Electron](https://github.com/electron/electron/issues/23479)
- [dyasta](https://stackoverflow.com/users/191514/dyasta) for [an explanation on Stack Overflow](https://stackoverflow.com/a/58547831/979493)
- [Richard Yu](https://github.com/ysc3839) for [implementing this in a C++ library](https://github.com/ysc3839/win32-darkmode)
- [Berrysoft](https://github.com/Berrysoft) for [implementing this in Mintty, the Cygwin Terminal emulator](https://github.com/mintty/mintty/issues/983) ([further discussion](https://github.com/mintty/mintty/pull/984))
- [Pavel Yosifovich](https://twitter.com/zodiacon) for [showing how to set custom title bar colors in Windows 11](https://twitter.com/zodiacon/status/1416734060278341633)