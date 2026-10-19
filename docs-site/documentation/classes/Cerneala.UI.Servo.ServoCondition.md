# ServoCondition Enum

## Definition
Namespace: `Cerneala.UI.Servo`
Assembly/Project: `Cerneala`
Source: `UI/Servo/ServoCondition.cs`

Identifies an observable condition for a Servo target.

```csharp
public enum ServoCondition
```

## Examples

```csharp
ServoCondition expected = ServoCondition.Visible;
```

## Remarks

`Exists` and `Missing` describe target cardinality and accept any nonzero match count or zero matches, respectively. The other values require a unique match: they keep waiting while no match exists and while multiple matches exist, because transitions can briefly keep an outgoing and an incoming copy of the same target. If the last evaluation before the timeout still found multiple matches, `ServoTimeoutException` states the match count, for example `The Servo target still matched 2 elements; exactly one was required.` Conditions are reevaluated at retained frame boundaries.

## Fields

| Name | Description |
| --- | --- |
| `Exists` | At least one match exists. |
| `Missing` | No match exists. |
| `Visible` | The unique match is effectively visible. |
| `Hidden` | The unique match is not effectively visible. |
| `Enabled` | The unique match is enabled. |
| `Disabled` | The unique match is disabled. |
| `Focused` | The unique match has keyboard focus. |

## Applies to

Servo wait operations.
