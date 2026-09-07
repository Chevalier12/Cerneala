# Getting Started

Cerneala is a retained realtime UI framework for .NET applications and complete
2D games. Traditional controls and realtime game rendering use the same UI
tree, frame scheduler, input system, and rendering pipeline.

The project is currently in **Developer Preview**. There is no polished project
template or stable package-based onboarding path yet. The reliable way to try
Cerneala today is to build the repository and start from one of its working
applications.

## Requirements

- Git
- PowerShell 7 (`pwsh`) for repository scripts and the native-package contract test
- The .NET SDK pinned by [`global.json`](../global.json)
- A supported native graphics stack for the backend you select

The runtime and sample projects shown here target .NET 8 while the repository
pins the SDK used to build and test them.

## Build From Source

Clone the repository, then run these commands from its root:

```powershell
dotnet tool restore
dotnet restore ./Cerneala.slnx
dotnet build ./Cerneala.slnx -c Release --no-restore
$env:CERNEALA_SDL_NATIVE_TESTS = '1'
dotnet test ./Cerneala.slnx -c Release --no-build --no-restore -m:1
```

`Graphix.Native 3.4.16-graphix.3` and `Graphix-CS 3.4.16.1` restore directly from
NuGet.org. No GitHub authentication, temporary Actions artifact or local package
feed is required. See the [SDL backend guide](sdl-desktop-backend.md) for package
provenance and the unchanged managed/native ownership boundaries.

Native input tests share the interactive desktop, so the full native run
serializes test projects with `-m:1`. Keep that desktop available to the tests;
unrelated windows or user input can invalidate their focus preconditions.

That is the full Windows verification path used by CI. SDL3 GPU also has native
smoke and contract coverage on Windows, Linux, and macOS.

## Run A Working Application

The playground is the fastest way to inspect controls, drawing, input, layout,
and backend behavior. It currently targets Windows.

Run it with SDL3 GPU:

```powershell
dotnet run --project ./Playground/Cerneala.Playground/Cerneala.Playground.csproj
```

SDL3 + SDL_GPU is the sole maintained desktop composition. MonoGame and WindowsDX
have been removed without a compatibility facade. No backend-selection build
property is required by these samples.

`CernealaPresentation` is the larger end-to-end showcase:

```powershell
dotnet run --project ./CernealaPresentation/CernealaPresentation.csproj
```

## The Current Desktop Application Model

A generated desktop application normally has these pieces:

```text
App.crn
App.crn.cs
MainWindow.crn
MainWindow.crn.cs
BackendRegistration.cs
YourApplication.csproj
```

The checked-in `CernealaPresentation` project is the current reference for this
shape. Copy contracts from a working project instead of guessing missing
package or generator setup.

### Declare The Application

`App.crn` selects the startup window, shutdown policy, and application-level
resources:

```xml
<Application
    StartupWindow="MainWindow"
    ShutdownMode="OnMainWindowClose">
    <Application.Resources>
        <SolidColorBrush Name="AccentBrush" Color="#FF4DF0FF" />
    </Application.Resources>
</Application>
```

Its companion is a partial `Application` class:

```csharp
using Cerneala.UI;

namespace MyApp;

public partial class App : Application
{
}
```

The source generator produces the process entry point. A separate `Program.cs`
is not required for this path.

See [Application Markup](application-markup.md) for startup, services,
resources, lifecycle, and shutdown contracts.

### Declare A Window

`MainWindow.crn` contains the retained UI tree:

```xml
<Window
    Title="My Cerneala App"
    Width="960"
    Height="640"
    MinWidth="720"
    MinHeight="480"
    WindowStartupLocation="CenterScreen"
    Background="#FF101418">
    <Grid>
        <TextBlock
            Text="Hello from Cerneala"
            FontSize="28"
            HorizontalAlignment="Center"
            VerticalAlignment="Center" />
    </Grid>
</Window>
```

The companion class must match the file name and root type:

```csharp
using Cerneala.UI.Controls;

namespace MyApp;

public partial class MainWindow : Window
{
}
```

Do not add a constructor to a generated `Window` or `UserControl` companion.
The generator owns construction.

### Select A Backend

Current desktop projects select their backend explicitly through an assembly
attribute. The checked-in desktop projects select the SDL composition:

```csharp
[assembly: Cerneala.UI.Hosting.Windowing.ApplicationBackend(
    typeof(Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend))]
```

The project file must reference the selected backend and include `.crn` files as
Roslyn additional files. Use
[`CernealaPresentation.csproj`](../CernealaPresentation/CernealaPresentation.csproj)
as the current working reference.

## Markup Is Compiled, Not Loaded At Runtime

`.crn` resembles XML because it is convenient for trees. It is not general
XAML compatibility.

The build path is:

```text
.crn source
    -> Cerneala.Language syntax and semantics
    -> Cerneala.SourceGen
    -> typed C# UI tree
```

Unknown elements, properties, resources, bindings, and directives produce
diagnostics instead of falling back to runtime interpretation.

Use the [Cerneala Markup Guide](CernealaMarkupGuide.md) for layout, resources,
bindings, Aspect, Motion, Prism, events, and validation rules. The Visual Studio
extension setup is documented in
[Visual Studio Community Extension](visual-studio-community.md).

## Code-First UI Is Also Supported

Markup is an authoring option, not the runtime architecture. The same controls
and typed property system can be used directly from C#:

```csharp
ObservableValue<string> statusText = new("Ready");

TextBlock status = new();
status.Bindings.Add(
    BindingOperations.BindOneWay(
        status,
        TextBlock.TextProperty,
        statusText));

Button runButton = new()
{
    Content = "Run"
};
```

For data binding contracts, see [Markup Data Bindings](markup-data-bindings.md)
and the canonical API pages under the documentation site.

## Put A Game In The UI Tree

`RenderSurface2D` is a `ContentControl`. It draws a managed 2D surface while its
normal retained content can provide a HUD or other interactive overlay.

```csharp
RenderSurface2D gameView = new()
{
    ClearColor = new Color(8, 11, 17),
    Content = new TextBlock
    {
        Text = "Score: 1200"
    }
};

gameView.Draw += (_, frame) =>
{
    frame.DrawSprite(player, playerBounds, Color.White);
};
```

The default redraw mode is continuous. For static or infrequently changing
content, use `RenderSurface2DRedrawMode.OnDemand` and call `InvalidateFrame()`
when application state changes.

See the canonical
[`RenderSurface2D` documentation](../docs-site/documentation/classes/Cerneala.UI.Controls.RenderSurface2D.md)
for drawing order, retained reuse, image invalidation, and lifecycle behavior.

## Understand The Retained Contract

The generated desktop runtime owns the host loop. Application code creates the
tree once and mutates state. Cerneala processes invalidated work through its
frame phases.

The important invariant is:

```text
state, resource, input, or time change
    -> invalidation
    -> retained frame phases
    -> cached root command list
    -> backend submission
```

An unchanged tree should not remeasure, rearrange, or regenerate local drawing
commands merely because another frame is presented. Draw submission is not an
allowed backdoor for mutating retained state.

`UiHost` and `UIRoot` remain public integration layers for custom hosts and
tests. They are not the recommended first step for a generated desktop app.

## Current Limits

Developer Preview means the project is useful but not stable. In particular:

- public contracts can still change;
- no stable compatibility promise exists for WPF or Avalonia XAML;
- package distribution and project templates are not finished;
- native accessibility is not complete;
- full IME, multiline editing, and rich text remain incomplete;
- backend maturity is uneven;
- outstanding desktop migration verification is recorded in the
  [removal audit](audits/2026-09-05-monogame-removal.md).

Do not infer support from a familiar type name. Check the implementation,
canonical API documentation, tests, and working examples.

## Where To Go Next

- [Cerneala website](https://chevalier12.github.io/Cerneala/)
- [API reference](https://chevalier12.github.io/Cerneala/documentation.html)
- [Cerneala Markup Guide](CernealaMarkupGuide.md)
- [Architecture](../architecture.md)
- [Roadmap](../ROADMAP.md)
- [Discord](https://discord.gg/p6SbqByd59)
