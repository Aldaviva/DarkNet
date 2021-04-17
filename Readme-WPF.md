This is a .NET library that you can use to enable Windows 10's dark mode for your application's title bars and system context menus, similar to the dark title bar in Command Prompt.

[![WPF window with dark title bar](https://i.imgur.com/yWWyS0Jl.png)](https://i.imgur.com/yWWyS0J.png)

- [Requirements](#requirements)
- [Installation](#installation)
    - [WPF](#wpf)
    - [Windows Forms](#windows-forms)
- [Usage](#usage)
    - [On application startup](#on-application-startup)
    - [Before showing a new window](#before-showing-a-new-window)
    - [Complete example](#complete-example)
- [Demo](#demo)
- [Limitations](#limitations)
- [Acknowledgements](#acknowledgements)

## Requirements

- .NET Framework 4.7.2 or later, or .NET Core 3.1 or later
- Windows 10 version 1809 (October 2018 Update) or later for dark mode to work
    - You can run your app on earlier Windows versions, but the title bar won't turn dark
- WPF or Windows Forms

## Installation

There are two packages available, one for WPF and one for Windows Forms. You can install the one that corresponds to the GUI technology that your program uses.

### WPF
```ps1
dotnet add package DarkNet-WPF
```
[See **DarkNet-WPF** on NuGet](https://www.nuget.org/packages/DarkNet-WPF/)

### Windows Forms
```ps1
dotnet add package DarkNet-Forms
```
[See **DarkNet-Forms** on NuGet](https://www.nuget.org/packages/DarkNet-Forms/)

## Usage

You must do both of the following steps.

### On application startup

Before showing **any** windows in your application, you must call
```cs
darknet.wpf.DarkNet.SetDarkModeAllowedForProcess(true);
```

A good place to call this is in an event handler for your Application's `Startup` event.

By default, WPF apps have an `App.xaml` file that specifies the first `Window` to show in the `StartupUri` attribute:
```xaml
<Application StartupUri="MainWindow.xaml">
```

This does not allow you to run custom code before showing the window, so remove `StartupUri` and add `Startup`, which specifies an event handler method.
```xaml
<Application Startup="App_OnStartup">
```

In `App.xaml.cs`, create a method to handle this event and call the aforementioned DarkNet method.
```cs
public partial class App: Application {

    private void App_OnStartup(object sender, StartupEventArgs e) {
        DarkNet.SetDarkModeAllowedForProcess(true);

        // Incomplete example; see below for complete example including showing your first window.
    }
}
```

### Before showing a new window

Before showing each window in your application, you have to enable dark mode for that window.

```cs
darknet.wpf.DarkNet.SetDarkModeAllowedForWindow(window, true);
```

You must do this when the `SourceInitialized` event is fired for the window you are about to show. If you do it before this (such as right before calling the constructor), the Win32 HWND will not exist yet, so the native DLL calls will fail. If you call it too late (such as after the window is shown), the DLL calls will have no effect on Windows.

Add an event handler for the `SourceInitialized` event and call the aforementioned DarkNet method.

```cs
window.SourceInitialized += (_, _) => { DarkNet.SetDarkModeAllowedForWindow(window, true); };
```

You must also perform this step for all subsequent windows you show in your application, not just the first window.

### Complete example

```xaml
<Application x:Class="my_namespace.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Startup="App_OnStartup">
</Application>
```

```cs
public partial class App: Application {

    private void App_OnStartup(object sender, StartupEventArgs e) {
        DarkNet.SetDarkModeAllowedForProcess(true);

        Window window = new MainWindow();
        window.SourceInitialized += (_, _) => { DarkNet.SetDarkModeAllowedForWindow(window, true); };

        window.Show();
    }

}
```

## Demo

Download and run `darknet-demo-wpf.exe` from the [latest release](https://github.com/Aldaviva/DarkNet/releases).

[![WPF window with dark title bar](https://i.imgur.com/yWWyS0Jl.png)](https://i.imgur.com/yWWyS0J.png)

You can also clone this repository and build the `darknet-demo-wpf` project yourself using Visual Studio Community 2019.

## Limitations
- This library currently requires a fairly recent version of Windows and the .NET runtime. This could be relaxed in a future version to allow inclusion in applications that run in a wider range of environments.
- This library currently does not expose whether the active Windows app mode is set to Dark or Light. This may be possible to add in a future version to allow you to implement a "follow Windows app mode" strategy in your application.
- This library only changes the theme of the title bar/window chrome/non-client area, as well as the system context menu (the menu that appears when you right click on the title bar, or left click on the title bar icon, or hit `Alt`+`Space`). It does not change the theme of the client area of your window. It is up to you to make that look different when dark mode is enabled.
- This library currently does not help you persist a user choice for the mode they want your application to use. You can expose an option and persist that yourself, then pass the desired value to the methods in this library (*e.g.* call `DarkNet.SetDarkModeAllowedForProcess(false)` for light mode, or just don't call it at all).

## Acknowledgements

- [Milan Burda](https://github.com/miniak) for explaining how to add this in Electron ([electron/electron #23479: [Windows] Title bar does not respect dark mode](https://github.com/electron/electron/issues/23479))
- [dyasta](https://stackoverflow.com/users/191514/dyasta) for an [exaplanation on Stack Overflow](https://stackoverflow.com/a/58547831/979493)
- [Richard Yu](https://github.com/ysc3839) for [implementing this in a C++ library](https://github.com/ysc3839/win32-darkmode)
- [Berrysoft](https://github.com/Berrysoft) for [implementing this in Mintty, the Cygwin Terminal emulator](https://github.com/mintty/mintty/issues/983) ([further discussion](https://github.com/mintty/mintty/pull/984))