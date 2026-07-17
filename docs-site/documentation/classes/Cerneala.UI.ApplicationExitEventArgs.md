# ApplicationExitEventArgs Class

## Definition
Namespace: `Cerneala.UI`
Assembly/Project: `Cerneala`
Source: `UI/ApplicationExitEventArgs.cs`

Provides the final application exit code.

```csharp
public sealed class ApplicationExitEventArgs : EventArgs
```

## Examples
```csharp
app.Exit += (_, args) => Console.WriteLine($"Exit code: {args.ExitCode}");
```

## Properties
| Name | Description |
| --- | --- |
| `ExitCode` | Exit code selected by the first shutdown request. |

## Remarks
The exit event is raised once. Repeated shutdown requests do not replace the first exit code.

## Applies to
`Application.Exit` and `Application.OnExit`.
