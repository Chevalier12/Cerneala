# Application Markup

## Standard Pair

A Windows executable can declare its application without `Program.cs` by pairing
these files:

```text
App.cui.xml
App.cui.xml.cs
```

The markup root must be `Application`. The companion must declare one concrete
partial class with the matching file name, derive from `Cerneala.UI.Application`,
and omit a user-declared constructor.

## Minimal Complete Example

`App.cui.xml`:

```xml
<Application StartupWindow="ShellWindow"
             ShutdownMode="OnLastWindowClose">
    <Application.Resources>
        <SolidColorBrush Name="AccentBrush" Color="#FF4DF0FF" />
    </Application.Resources>
</Application>
```

`App.cui.xml.cs`:

```csharp
using Cerneala.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Sample;

public partial class App : Application
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<AppState>();
    }
}
```

`ShellWindow.cui.xml`:

```xml
<Window Title="Sample" Width="800" Height="600">
    <Border Background="$AccentBrush" />
</Window>
```

`ShellWindow.cui.xml.cs`:

```csharp
using Cerneala.UI.Controls;

namespace Sample;

public partial class ShellWindow : Window
{
}
```

The project includes the generator as an analyzer and the markup as additional
files:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Cerneala.csproj" />
    <ProjectReference Include="..\Cerneala.SourceGen\Cerneala.SourceGen.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
    <AdditionalFiles Include="**\*.cui.xml" Exclude="bin\**;obj\**" />
  </ItemGroup>
</Project>
```

## Startup Window

`StartupWindow` is required. The generator resolves it through Roslyn in the
companion class scope. Simple imported names and fully qualified names are
accepted. The result must be accessible, concrete, and derived from `Window`.
There is no reserved `MainWindow` class name and no runtime URI loading.

The generated standalone entry point passes process arguments to
`Application.OnStartup` and returns the exit code selected by
`Application.Shutdown(int)`. Hosted startup receives an empty argument list
because the external host owns the pump.

## Shutdown Mode

| Value | Behavior |
| --- | --- |
| `OnLastWindowClose` | Shuts down after the final open window closes successfully. This is the default. |
| `OnMainWindowClose` | Shuts down when the window currently assigned to `Application.MainWindow` closes successfully. |
| `OnExplicitShutdown` | Window closure does not stop the application; call `Shutdown()` or `Shutdown(int)`. |

A canceled `Closing` event does not trigger shutdown.

## Application Resources

`Application.Resources` accepts the existing resource syntax for
`SolidColorBrush`, `Aspect`, `Tween`, and `Spring`. These definitions are
available at compile time to paired `Window` and `UserControl` documents and at
runtime to every application window.

```xml
<Application.Resources>
    <SolidColorBrush Name="AccentBrush" Color="#FF4DF0FF" />
    <Tween Name="Quick" Duration="150ms" Easing="EaseOut" />
    <Spring Name="Soft" Stiffness="420" Damping="32" Mass="1" />
</Application.Resources>
```

Resource lookup proceeds from the nearest scope outward:

```text
element -> ancestors / control / window -> application -> theme/default
```

A local resource with the same name shadows the application resource. Replacing
an observable application resource updates dependent consumers in every
attached root; unrelated consumers remain idle, and windows opened later see
the latest value.

Application-scope `Tween` and `Spring` values are reusable Motion specs.
`MotionClip` is rejected in `Application.Resources` because an application has
no visual tree or global namescope. Keep clips that target named elements in the
owning Window or UserControl resources.

## Application Lifecycle

The generated lifecycle is:

```text
construct App and initialize resources
-> install Application.Current
-> ConfigureServices
-> publish Services
-> OnStartup / Startup
-> resolve and show StartupWindow
-> pump
-> shutdown
-> OnExit / Exit
-> dispose services
-> clear Application.Current
```

Calling `Shutdown` during startup skips declarative window creation. `Exit` is
raised once for installed applications, including startup failures.

## Invalid Application Content

`Application` does not own a visual tree. It rejects visual children, `Name`,
`DataType`, element event handlers, and direct `@when`, `@if`, `@set`, or
`@animate` directives. Put visual behavior in a Window, UserControl, Aspect, or
local MotionClip instead.

An executable may contain at most one paired Application definition.

## Legacy Compatibility

When no paired `Application` document exists, a single paired class named
`MainWindow` continues to generate the previous startup path. The legacy static
`App.ConfigureServices(IServiceCollection)` hook is available only on that
fallback path. New applications should use the App pair and the protected
instance override.

## Deliberate Limits

Application markup does not provide `StartupUri`, runtime XML loading, merged
resource dictionaries, navigation, multiple application instances, or global
visual namescopes.
