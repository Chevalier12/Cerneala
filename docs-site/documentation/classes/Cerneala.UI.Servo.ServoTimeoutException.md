# ServoTimeoutException Class

## Definition
Namespace: `Cerneala.UI.Servo`
Assembly/Project: `Cerneala`
Source: `UI/Servo/ServoException.cs`

Indicates that a Servo operation exceeded its configured timeout.

```csharp
public sealed class ServoTimeoutException : ServoException
```

## Examples

```csharp
using Cerneala.UI.Servo;

// The "save" button stays disabled for the whole second.
var servo = new Servo(window, new ServoOptions { DefaultTimeout = TimeSpan.FromSeconds(1) });
try
{
    await servo.ClickAsync(ServoTarget.ById("save"));
}
catch (ServoTimeoutException timeout)
{
    // timeout.Message:
    //   "The Servo operation exceeded its 00:00:01 timeout. Last failure: The Servo target is disabled."
    // timeout.InnerException: the ServoTargetNotActionableException
    //   with Message "The Servo target is disabled."
}
```

## Remarks

External cancellation remains an `OperationCanceledException` and is distinct from a Servo timeout.

Target actions (`ClickAsync`, `HoverAsync`, `DragAsync`, `ScrollAsync`, `TypeIntoAsync`, `ReplaceTextAsync`) wait for their target to exist and be actionable. When the timeout ends that wait, the message ends with `Last failure: ` and the message of the last failed attempt, and `InnerException` is that failure: a `ServoTargetNotFoundException` or a `ServoTargetNotActionableException`. A timeout that ended no such wait has a `null` `InnerException`.

When a `WaitForAsync` with `Visible`, `Hidden`, `Enabled`, `Disabled`, or `Focused` times out and its last evaluation still found multiple matches, the message appends the match count, for example `The Servo operation exceeded its 00:00:05 timeout. The Servo target still matched 2 elements; exactly one was required.` Its `InnerException` is `null`.

## Constructors

| Name | Description |
| --- | --- |
| `ServoTimeoutException(string)` | Creates the exception with a diagnostic message. |
| `ServoTimeoutException(string, Exception?)` | Creates the exception with a diagnostic message and the failure that was still happening when the timeout expired. |

## Applies to

Timed Servo operations.
