# ServoOptions Class

## Definition
Namespace: `Cerneala.UI.Servo`
Assembly/Project: `Cerneala`
Source: `UI/Servo/ServoOptions.cs`

Configures time limits for Servo operations.

```csharp
public sealed class ServoOptions
```

## Examples

```csharp
var servo = new Servo(window, new ServoOptions
{
    DefaultTimeout = TimeSpan.FromSeconds(10)
});
```

## Remarks

Servo copies the supplied options when constructed. The timeout must be positive; zero and `Timeout.InfiniteTimeSpan` are rejected. The timeout applies to queries, input actions, waits, and screenshots. Expiration throws `ServoTimeoutException`, while cancellation from the caller remains `OperationCanceledException`.

## Properties

| Name | Description |
| --- | --- |
| `DefaultTimeout` | Gets or sets the default operation timeout. The default is five seconds. |

## Applies to

Servo operations that use the configured default timeout.
