# GeneratedWindowApplication Class

## Definition
Namespace: `Cerneala.UI.Hosting.Windowing`
Assembly/Project: `Cerneala`  
Source: `UI/Hosting/Windowing/GeneratedWindowApplication.cs`

Runs or hosts the application and startup window described by generated startup metadata.

```csharp
public static class GeneratedWindowApplication
```

## Examples
```csharp
int exitCode = GeneratedWindowApplication.Run(descriptor, args);
```

## Methods
| Name | Description |
| --- | --- |
| `RegisterStartup` | Registers the one generated startup descriptor. |
| `Run(GeneratedWindowStartupDescriptor)` | Runs standalone using the process command-line arguments and returns the application exit code. |
| `Run(GeneratedWindowStartupDescriptor, IReadOnlyList<string>)` | Runs standalone using the supplied startup arguments and returns the application exit code. |

## Remarks
Only one distinct startup descriptor may be registered. Descriptors generated from `App.crn` include an application factory; legacy descriptors retain the main-window-only path for compatibility.

For application-aware descriptors, `Run` constructs and installs `Application`, configures and publishes services, raises startup, resolves and shows the declarative startup window, pumps until shutdown, and returns the first exit code supplied to `Application.Shutdown(int)`. Cleanup disposes the runtime and application service provider and clears `Application.Current`.

Hosted pumping and reset methods are internal. Hosted startup receives an empty argument list because an external host, rather than a process entry point, owns the pump.

Startup failures retain their original exception instance. The exception `Data` dictionary includes `Cerneala.StartupStage` and `Cerneala.StartupTarget` on the application-aware and legacy paths, standalone and hosted. `Cerneala.StartupStage` names the failing stage, for example `"create runtime"` when the windowing backend cannot create its platform, or `"resolve and create startup Window"` when the startup window factory throws. `Cerneala.StartupTarget` is the startup window type name, or `"<legacy MainWindow>"` for a legacy descriptor. A failure after startup, while the window loop runs, carries neither entry. A cleanup failure is retained under `Cerneala.CleanupFailure` when another startup exception is already in flight.

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `RegisterStartup` | `ArgumentNullException` | The descriptor is `null`. |
| `RegisterStartup` | `InvalidOperationException` | A different generated descriptor is already registered. |
| `Run` | `ArgumentNullException` | The descriptor or explicit argument list is `null`. |

## Applies to
Backend-agnostic desktop application hosting. A registered windowing backend supplies the native window and graphics implementation.

## See also
- `Application`
- `GeneratedWindowStartupDescriptor`
