# ApplicationStartupEventArgs Class

## Definition
Namespace: `Cerneala.UI`
Assembly/Project: `Cerneala`
Source: `UI/ApplicationStartupEventArgs.cs`

Provides command-line arguments for application startup.

```csharp
public sealed class ApplicationStartupEventArgs : EventArgs
```

## Examples
```csharp
app.Startup += (_, args) => Console.WriteLine(string.Join(" ", args.Args));
```

## Properties
| Name | Description |
| --- | --- |
| `Args` | Read-only command-line arguments supplied by the application host. |

## Remarks
Standalone startup supplies process arguments. The hosted argument contract is defined by the hosting integration.

## Applies to
`Application.Startup` and `Application.OnStartup`.
