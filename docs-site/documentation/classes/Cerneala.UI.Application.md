# Application Class

## Definition
Namespace: `Cerneala.UI`
Assembly/Project: `Cerneala`
Source: `UI/Application.cs`

Represents the UI-thread application, its global resources, windows, services, and desktop lifecycle.

```csharp
public class Application
```

## Examples
```csharp
// Element audio: scoped to the element's attachment lifecycle.
TimbrePlayback click = button.Timbre.Play(new TimbreSound("audio/click.wav"));

// Application audio: lives until the application exits.
TimbrePlayback music = Application.Current!.Timbre.Play(
    new TimbreSound("audio/music.ogg", loading: TimbreLoading.Streaming),
    start => start.Loop = true);
```

```csharp
public partial class App : Application
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<Workspace>();
    }
}
```

Application markup can enable multisampling before the windowing backend is created:

```xml
<Application
    StartupWindow="MainWindow"
    UseMultisampling="True" />
```

## Remarks
`Current` identifies the installed application during startup and runtime. Lifecycle operations and window properties must be accessed from the owning UI thread.

`Windows` and `ActiveWindow` are views of the attached window runtime; the application does not maintain a second window collection. `MainWindow` can be reassigned without closing the previous window. The selected `ShutdownMode` is evaluated only after a window closes successfully.

`UseMultisampling` controls whether the windowing backend requests multisampled render targets. It defaults to `false` and is read when the default runtime is created. Changing it after runtime creation does not recreate graphics resources.

`TimbreRuntime` is the [TimbreRuntime](Cerneala.Timbre.TimbreRuntime.md) shared by every window root of the application. It is created on first use without opening an audio device. Its output is the shared audio output of the window platform the application is installed on, bound when the first playback needs it, so the runtime may be created before startup installs the application. With [SdlGpuApplicationBackend](Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend.md) that output is the SDL3 default playback device; closing a window does not close it, and the platform releases it before SDL shuts down. A platform without audio provides no output, so playbacks fail with `DeviceUnavailable`, as does a missing or failing device. Assign a configured runtime before first use to supply another output; assigning after first use throws `InvalidOperationException`. `Timbre` is an application-scoped [TimbreScope](Cerneala.Timbre.TimbreScope.md) for audio that is not owned by an element; element audio uses `UIElement.Timbre`.

`Shutdown(int)` is idempotent. Its first call closes remaining windows, raises `Exit` once, disposes the published service provider when it implements `IDisposable`, disposes `Timbre` (canceling its playbacks), disposes a `TimbreRuntime` the application created itself — an assigned runtime stays caller-owned — and clears `Current`. Afterwards `Timbre` and `TimbreRuntime` throw `ObjectDisposedException`.

## Constructors
| Name | Description |
| --- | --- |
| `Application()` | Creates an application with an empty observable resource dictionary. |

## Properties
| Name | Description |
| --- | --- |
| `Current` | Installed application, or `null` outside its lifecycle. |
| `Resources` | Application-scope resources shared by attached windows. |
| `Services` | Published service provider; unavailable before service configuration completes. |
| `MainWindow` | Window currently designated as the main window. |
| `Windows` | Read-only view of runtime-owned windows. |
| `ActiveWindow` | Currently active runtime window, if any. |
| `UseMultisampling` | Whether the windowing backend requests multisampled render targets. The default is `false`. |
| `ShutdownMode` | Policy evaluated after a successful window close. |
| `TimbreRuntime` | Audio runtime shared by the application's windows; lazy, assignable before first use. |
| `Timbre` | Application-scoped sound scope, disposed on exit. |

## Methods
| Name | Description |
| --- | --- |
| `Shutdown()` | Requests shutdown with exit code `0`. |
| `Shutdown(int)` | Requests shutdown with the specified process exit code. |
| `ConfigureServices(IServiceCollection)` | Configures application services before startup. |
| `OnStartup(ApplicationStartupEventArgs)` | Raises or customizes startup behavior. |
| `OnExit(ApplicationExitEventArgs)` | Raises or customizes exit behavior. |

## Events
| Name | Description |
| --- | --- |
| `Startup` | Raised after services are published and before the declarative startup window is created. |
| `Exit` | Raised exactly once when the installed application exits. |

## Applies to
Windows desktop standalone and hosted application lifecycles.

## See also
- `ApplicationShutdownMode`
- `Window`
- `ResourceDictionary`
- [TimbreRuntime](Cerneala.Timbre.TimbreRuntime.md)
