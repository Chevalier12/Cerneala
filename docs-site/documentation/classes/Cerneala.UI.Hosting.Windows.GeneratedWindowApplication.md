# GeneratedWindowApplication Class

## Definition
Namespace: `Cerneala.UI.Hosting.Windows`  
Assembly/Project: `Cerneala`  
Source: `UI/Hosting/Windows/GeneratedWindowApplication.cs`

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
Only one distinct startup descriptor may be registered. Descriptors generated from `App.cui.xml` include an application factory; legacy descriptors retain the main-window-only path for compatibility.

For application-aware descriptors, `Run` constructs and installs `Application`, configures and publishes services, raises startup, resolves and shows the declarative startup window, pumps until shutdown, and returns the first exit code supplied to `Application.Shutdown(int)`. Cleanup disposes the runtime and application service provider and clears `Application.Current`.

Hosted pumping and reset methods are internal. Hosted startup receives an empty argument list because an external host, rather than a process entry point, owns the pump.

Startup failures retain their original exception instance. The exception `Data` dictionary includes `Cerneala.StartupStage` and `Cerneala.StartupTarget`; a cleanup failure is retained under `Cerneala.CleanupFailure` when another startup exception is already in flight.

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `RegisterStartup` | `ArgumentNullException` | The descriptor is `null`. |
| `RegisterStartup` | `InvalidOperationException` | A different generated descriptor is already registered. |
| `Run` | `ArgumentNullException` | The descriptor or explicit argument list is `null`. |

## Applies to
Windows desktop application hosting.

## See also
- `Application`
- `GeneratedWindowStartupDescriptor`
