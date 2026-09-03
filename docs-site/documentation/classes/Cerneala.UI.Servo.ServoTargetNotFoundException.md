# ServoTargetNotFoundException Class

## Definition
Namespace: `Cerneala.UI.Servo`
Assembly/Project: `Cerneala`
Source: `UI/Servo/ServoException.cs`

Indicates that a Servo operation requiring one target found no matches.

```csharp
public sealed class ServoTargetNotFoundException : ServoException
```

## Examples

```csharp
await Assert.ThrowsAsync<ServoTargetNotFoundException>(
    () => servo.FindAsync(ServoTarget.ById("missing")));
```

## Remarks

`FindAllAsync` returns an empty list and `ExistsAsync` returns `false` for the same cardinality instead of throwing this exception.

## Constructors

| Name | Description |
| --- | --- |
| `ServoTargetNotFoundException(string)` | Creates the exception with a diagnostic message. |

## Applies to

Servo operations requiring exactly one target.
