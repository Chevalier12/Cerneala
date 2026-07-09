# MotionStateRule Class

## Definition
Namespace: `Cerneala.UI.Motion.States`

Assembly/Project: `Cerneala`

Source: `UI/Motion/States/MotionStateRule.cs`

Represents a named motion-state rule.

```csharp
public sealed class MotionStateRule
```

Inheritance:
`object` -> `MotionStateRule`

## Examples

Create a rule for a named visual or interaction state:

```csharp
using Cerneala.UI.Motion.States;

MotionStateRule pressedRule = new("Pressed");

string stateName = pressedRule.StateName;
```

## Remarks

`MotionStateRule` stores the name of a motion state. The state name is supplied to the constructor and exposed through the read-only `StateName` property.

The constructor rejects `null`, empty, and whitespace-only state names. It preserves the provided string when it is valid; it does not normalize casing, trim whitespace, or map the name to a built-in state list.

## Constructors

| Name | Description |
| --- | --- |
| `MotionStateRule(string stateName)` | Initializes a rule for the supplied state name. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `StateName` | `string` | Gets the state name associated with the rule. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| None | N/A | This class declares no public methods beyond inherited `object` members. |

## Events

| Name | Description |
| --- | --- |
| None | This class declares no public events. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `MotionStateRule(string)` | `ArgumentException` | `stateName` is `null`, empty, or contains only white-space characters. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.States.MotionVisualStateController`
- `Cerneala.UI.Motion.States.MotionVisualStateSnapshot`
- `Cerneala.UI.Motion.MotionStateBuilder`
