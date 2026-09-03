# ServoTargetNotActionableException Class

## Definition
Namespace: `Cerneala.UI.Servo`
Assembly/Project: `Cerneala`
Source: `UI/Servo/ServoException.cs`

Indicates that a resolved target cannot perform the requested Servo action.

```csharp
public sealed class ServoTargetNotActionableException : ServoException
```

## Examples

```csharp
ServoException failure = new ServoTargetNotActionableException("Target is hidden.");
```

## Remarks

Actionability checks are owned by Servo actions rather than by query snapshots.

## Constructors

| Name | Description |
| --- | --- |
| `ServoTargetNotActionableException(string)` | Creates the exception with a diagnostic message. |

## Applies to

Servo input and capture actions.
