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
ServoException failure = new ServoTimeoutException("The target did not become visible before timeout.");
```

## Remarks

External cancellation remains an `OperationCanceledException` and is distinct from a Servo timeout.

When a `WaitForAsync` with `Visible`, `Hidden`, `Enabled`, `Disabled`, or `Focused` times out and its last evaluation still found multiple matches, the message appends the match count, for example `The Servo operation exceeded its 00:00:05 timeout. The Servo target still matched 2 elements; exactly one was required.`

## Constructors

| Name | Description |
| --- | --- |
| `ServoTimeoutException(string)` | Creates the exception with a diagnostic message. |

## Applies to

Timed Servo operations.
