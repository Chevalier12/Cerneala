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

`Exists` and `Missing` describe target cardinality and accept any nonzero match count or zero matches, respectively. The other values wait while no match exists and require a unique match when one appears; multiple matches throw `ServoTargetAmbiguousException` immediately. Conditions are reevaluated at retained frame boundaries.

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
