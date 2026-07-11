# GeneratedWindowStartupDescriptor Class

## Definition
Namespace: `Cerneala.UI.Hosting.Windows`  
Assembly/Project: `Cerneala`  
Source: `UI/Hosting/Windows/GeneratedWindowApplication.cs`

Connects generated application startup with service registration and main-window creation.

```csharp
public sealed class GeneratedWindowStartupDescriptor
```

## Examples
```csharp
var descriptor = new GeneratedWindowStartupDescriptor(
    services => services.AddSingleton<AppState>(),
    provider => new MainWindow());
GeneratedWindowApplication.Run(descriptor);
```

## Constructors
| Name | Description |
| --- | --- |
| `GeneratedWindowStartupDescriptor(Action<IServiceCollection>, Func<IServiceProvider, Window>)` | Creates startup callbacks. |

## Remarks
Callbacks are retained by generated startup infrastructure. `null` callbacks are rejected.

## Applies to
Windows generated-window hosting.
