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
public partial class App : Application
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<Workspace>();
    }
}
```

## Remarks
`Current` identifies the installed application during startup and runtime. Lifecycle operations and window properties must be accessed from the owning UI thread.

`Windows` and `ActiveWindow` are views of the attached window runtime; the application does not maintain a second window collection. `MainWindow` can be reassigned without closing the previous window. The selected `ShutdownMode` is evaluated only after a window closes successfully.

`Shutdown(int)` is idempotent. Its first call closes remaining windows, raises `Exit` once, disposes the published service provider when it implements `IDisposable`, and clears `Current`.

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
| `ShutdownMode` | Policy evaluated after a successful window close. |

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
