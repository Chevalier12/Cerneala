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

Servo's message names the target with `ServoTarget.ToString()`, for example `The Servo target Id="missing" did not match any element.` For a target with two or more criteria (ID, role, name, each `Where` predicate, `Within`), the message also counts near misses: elements that fail exactly one criterion. A toolbar holds a button named `Open`, and two buttons named `Save` are outside it; `FindAsync(ServoTarget.ByRole(SemanticsRole.Button).WithName("Save").Within(ServoTarget.ById("toolbar")))` throws with `The Servo target Role=Button, Name="Save" within (Id="toolbar") did not match any element. Near misses: 2 matched every criterion except within (Id="toolbar"); 1 matched every criterion except Name="Save".` For a target with a position, elements that match every criterion but that the position skips are counted too: `Near misses: 2 matched every criterion, but Nth(5) selects none of them.` Near misses are computed only when this exception is thrown.

## Constructors

| Name | Description |
| --- | --- |
| `ServoTargetNotFoundException(string)` | Creates the exception with a diagnostic message. |

## Applies to

Servo operations requiring exactly one target.
