# ServoTarget Class

## Definition
Namespace: `Cerneala.UI.Servo`
Assembly/Project: `Cerneala`
Source: `UI/Servo/ServoTarget.cs`

Describes a reusable immutable Servo query.

```csharp
public sealed class ServoTarget
```

## Examples

```csharp
ServoTarget save = ServoTarget.ByRole(SemanticsRole.Button)
    .WithName("Save")
    .Within(ServoTarget.ById("editor-toolbar"));

ServoElement current = await servo.FindAsync(save);
```

## Remarks

ID and semantic-name comparisons are exact and ordinal. `Within` evaluates semantic ancestry each time the target is resolved; it does not retain a live container.

## Methods

| Name | Description |
| --- | --- |
| `ById(string)` | Creates a target requiring an exact Servo ID. |
| `ByName(string)` | Creates a target requiring an exact semantic name. |
| `ByRole(SemanticsRole)` | Creates a target requiring a semantic role. |
| `WithName(string)` | Returns a new target additionally constrained by semantic name. |
| `Within(ServoTarget)` | Returns a new target constrained to descendants of matching semantic ancestors. |

## Applies to

Servo queries and actions.
