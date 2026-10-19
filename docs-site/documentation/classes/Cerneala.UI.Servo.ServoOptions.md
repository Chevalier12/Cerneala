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

Every `Servo` operation also has an overload with a `TimeSpan timeout` parameter right before the `CancellationToken`. That timeout replaces `DefaultTimeout` for that one call; other calls keep `DefaultTimeout`. For example, with `DefaultTimeout` of 5 s, `await servo.WaitForIdleAsync(TimeSpan.FromSeconds(30))` may wait 30 s, and the next `await servo.WaitForIdleAsync()` times out after 5 s. The same validation applies: zero, negative values, and `Timeout.InfiniteTimeSpan` throw `ArgumentOutOfRangeException`.

## Properties

| Name | Description |
| --- | --- |
| `DefaultTimeout` | Gets or sets the default operation timeout. The default is five seconds. |

## Applies to

Servo operations that do not pass their own timeout.
