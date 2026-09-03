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

## Constructors

| Name | Description |
| --- | --- |
| `ServoTimeoutException(string)` | Creates the exception with a diagnostic message. |

## Applies to

Timed Servo operations.
