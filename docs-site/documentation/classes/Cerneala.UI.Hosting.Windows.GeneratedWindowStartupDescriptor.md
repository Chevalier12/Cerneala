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
    () => new App(),
    services => services.AddSingleton<AppState>(),
    provider => provider.GetRequiredService<ShellWindow>(),
    "Sample.ShellWindow");
int exitCode = GeneratedWindowApplication.Run(descriptor, args);
```

## Constructors
| Name | Description |
| --- | --- |
| `GeneratedWindowStartupDescriptor(Func<Application>, Action<IServiceCollection>, Func<IServiceProvider, Window>, string?)` | Creates the application-aware startup callbacks emitted for `App.cui.xml`; the startup type name is optional diagnostic context. |
| `GeneratedWindowStartupDescriptor(Action<IServiceCollection>, Func<IServiceProvider, Window>)` | Creates the legacy main-window callbacks used when no application definition exists. |

## Remarks
Callbacks are retained by generated startup infrastructure. `null` callbacks are rejected. Application-aware descriptors construct the application separately from the declarative startup window so lifecycle setup can complete before window resolution. A missing or whitespace startup type name is represented as `<startup Window>` in failure context.

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| Constructors | `ArgumentNullException` | A required callback is `null`. |

## Applies to
Windows generated-window hosting.

## See also
- `Application`
- `GeneratedWindowApplication`
