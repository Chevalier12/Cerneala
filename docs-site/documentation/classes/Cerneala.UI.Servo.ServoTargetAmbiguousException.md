# ServoTargetAmbiguousException Class

## Definition
Namespace: `Cerneala.UI.Servo`
Assembly/Project: `Cerneala`
Source: `UI/Servo/ServoException.cs`

Indicates that a Servo operation requiring one target found multiple matches.

```csharp
public sealed class ServoTargetAmbiguousException : ServoException
```

## Examples

```csharp
await Assert.ThrowsAsync<ServoTargetAmbiguousException>(
    () => servo.FindAsync(ServoTarget.ByRole(SemanticsRole.Button)));
```

## Remarks

Use a more specific target or `FindAllAsync` when multiple matches are expected.

## Constructors

| Name | Description |
| --- | --- |
| `ServoTargetAmbiguousException(string)` | Creates the exception with a diagnostic message. |

## Applies to

Servo operations requiring exactly one target.
