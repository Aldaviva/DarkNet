DarkNet
===

[![Nuget](https://img.shields.io/nuget/v/DarkNet?logo=nuget)](https://www.nuget.org/packages/DarkNet/) [![GitHub Workflow Status](https://img.shields.io/github/workflow/status/Aldaviva/DarkNet/.NET?logo=github)](https://github.com/Aldaviva/DarkNet/actions/workflows/dotnetpackage.yml)

Enable native Windows dark mode for your WPF and Windows Forms title bars.

![WPF window with dark title bar](https://raw.githubusercontent.com/Aldaviva/DarkNet/master/.github/images/demo-wpf.png)

<!-- MarkdownTOC autolink="true" bracket="round" autoanchor="true" levels="1,2,3" -->

- [Requirements](#requirements)
- [Installation](#installation)
- [Usage](#usage)
    - [Basics](#basics)
    - [WPF](#wpf)
    - [Windows Forms](#windows-forms)
    - [Taskbar theme](#taskbar-theme)
- [Demos](#demos)
- [Limitations](#limitations)
- [Acknowledgements](#acknowledgements)

<!-- /MarkdownTOC -->

<a id="requirements"></a>
## Requirements

- .NET runtime
    - .NET 5.0 or later
    - .NET Core 3.1 or later
    - .NET Framework 4.5.2 or later
- Windows
    - Windows 10 version 1809 (October 2018 Update) or later
    - Windows 11 or later
    - You can also run your app on earlier Windows versions as well, but the title bar won't turn dark.
- Windows Presentation Foundation or Windows Forms

<a id="installation"></a>
## Installation

[DarkNet is available on NuGet Gallery](https://www.nuget.org/packages/DarkNet/).

```ps1
dotnet add package DarkNet
```
```ps1
Install-Package DarkNet
```

<a id="usage"></a>
## Usage

<a id="basics"></a>
### Basics

#### Entry point

The top-level interface of this library is <code>Dark.Net.<strong>IDarkNet</strong></code>, which is implemented by the **`DarkNet`** class. A shared instance of this class is available from **`DarkNet.Instance`**, or you can construct a new instance with `new DarkNet()`.

#### Methods

You must call **both** of the methods below to make a window use the dark theme. See the following sections for specific examples.

1. First, call **`SetCurrentProcessTheme()`** to allow your process to change the themes of its windows, although it doesn't apply any themes on its own.
    
    If you don't call this method, all windows in your application will use the light theme, even if you call `SetWindowTheme*`.
2. Next, call **`SetWindowTheme*()`** to actually apply the theme to each window. There are 3 methods to handle WPF, Forms, and raw `HWND` handles.
    
    If you don't call one of these methods on a given window, that window will use the light theme, even if you called `SetCurrentProcessTheme`.

#### Themes

This library uses the `Theme` enum to differentiate **`Dark`** mode from **`Light`** mode. You can set any window in your application to use whichever theme you want, they don't all have to be the same theme. There is also an **`Auto`** value that allows the theme to be inherited from a higher level, falling back from the window to the process to the user account:

1. When a **window's** theme is `Dark` or `Light`, it uses that theme directly (`SetWindowTheme*`).
2. When a **window's** theme is `Auto`, it inherits from the **process's** theme set by `SetCurrentProcessTheme`.
3. When a **window and its process's** themes are both `Auto`, they inherit from the local user's operating system **default app theme** preferences (Settings › Personalization › Colors › Choose your default app mode).

##### Live updates

If the user changes their default app mode while your app is running with the `Auto` theme, then your app will automatically update and render the new theme without having to handle any events or restart.

Try the [demo apps](#demos) to see this behavior in action.

<a id="wpf"></a>
### WPF

You must do both of the following steps.

<a id="on-application-startup"></a>
#### On application startup

Before showing **any** windows in your application, you must call
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

<a id="before-showing-a-new-window"></a>
#### Before showing a new window

Before showing each window in your application, you have to set the theme for that window.

```cs
IDarkNet.SetWindowThemeWpf(window, theme);
```

A good place to call this is in the window's constructor **after the call to `InitializeComponent`**, or in an event handler for the window's **`SourceInitialized`** event.

If you call it too late (such as after the window is shown), the calls will have no effect on Windows.

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

<a id="windows-forms"></a>
### Windows Forms

You must do both of the following steps.

<a id="on-application-startup-1"></a>
#### On application startup

Before showing **any** windows in your application, you must call
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

<a id="before-showing-a-new-window-1"></a>
#### Before showing a new window

Before showing each window in your application, you have to set the theme for that window.

```cs
IDarkNet.SetWindowThemeForms(window, theme);
```

You must do this **before calling `Show()` or `Application.Run()`** to show the window. If you call it too late (such as after the window is shown), the calls will have no effect on Windows.

```cs
Form mainForm = new Form1();
DarkNet.Instance.SetWindowThemeForms(mainForm, Theme.Auto);
```

You must perform this step for **every** window you show in your application, not just the first one.

<a id="complete-example"></a>
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

<a id="taskbar-theme"></a>
### Taskbar theme

Windows introduced a preference to choose a dark or light taskbar in Windows 10 version 1903. This is controlled by Settings › Personalization › Colors › Choose your default Windows mode.

DarkNet exposes the value of this preference with the <code>IDarkNet.<strong>UserTaskbarThemeIsDark</strong></code> property, as well as the change event <code>IDarkNet.<strong>UserTaskbarThemeIsDarkChanged</strong></code>. You can use these to render a tray icon in the notification area that matches the taskbar's theme, and re-render it when the user preference changes.

<a id="demos"></a>
## Demos

You can download the following precompiled demos, or clone this repository and build the demo projects yourself using Visual Studio Community 2022.

<a id="wpf-1"></a>
#### WPF

Download and run `darknet-demo-wpf.exe` from the [latest release](https://github.com/Aldaviva/DarkNet/releases).

![WPF window with dark title bar](https://raw.githubusercontent.com/Aldaviva/DarkNet/master/.github/images/demo-wpf.png)

<a id="windows-forms-1"></a>
#### Windows Forms

Download and run `darknet-demo-winforms.exe` from the [latest release](https://github.com/Aldaviva/DarkNet/releases).

![Windows Forms window with dark title bar](https://raw.githubusercontent.com/Aldaviva/DarkNet/master/.github/images/demo-winforms.png)

<a id="limitations"></a>
## Limitations
- This library only changes the theme of the title bar/window chrome/non-client area, as well as the system context menu (the menu that appears when you right click on the title bar, or left click on the title bar icon, or hit `Alt`+`Space`). It does not change the theme of the client area of your window. It is up to you to make that look different when dark mode is enabled.
- This library currently does not help you persist a user choice for the mode they want your application to use across separate process executions. You can expose an option and persist that yourself, then pass the desired `Theme` value to the methods in this library.

<a id="acknowledgements"></a>
## Acknowledgements

- [Milan Burda](https://github.com/miniak) for explaining how to add this in Electron ([electron/electron #23479: [Windows] Title bar does not respect dark mode](https://github.com/electron/electron/issues/23479))
- [dyasta](https://stackoverflow.com/users/191514/dyasta) for [an explanation on Stack Overflow](https://stackoverflow.com/a/58547831/979493)
- [Richard Yu](https://github.com/ysc3839) for [implementing this in a C++ library](https://github.com/ysc3839/win32-darkmode)
- [Berrysoft](https://github.com/Berrysoft) for [implementing this in Mintty, the Cygwin Terminal emulator](https://github.com/mintty/mintty/issues/983) ([further discussion](https://github.com/mintty/mintty/pull/984))