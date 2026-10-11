# AspectConditionDependencyKind Enum

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectConditionDependency.cs`

Names the kind of input an Aspect condition read while it was evaluated.

```csharp
public enum AspectConditionDependencyKind
```

## Examples

Evaluate a compound condition and list the kinds of its dependencies:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;

AspectCondition condition = AspectCondition.All(
    AspectCondition.State(AspectState.Hover),
    AspectCondition.Property(ButtonBase.IsPressedProperty).Is(true));

AspectConditionResult result = condition.Evaluate(new AspectMatchContext(new Button()));
foreach (AspectConditionDependency dependency in result.Dependencies)
{
    Console.WriteLine(dependency.Kind);
}
// State
// UiProperty
```

## Remarks

Each `AspectConditionDependency` in `AspectConditionResult.Dependencies` has a `Kind`. The built-in conditions report these kinds:

| Condition | Kind | Payload set on the dependency |
| --- | --- | --- |
| `AspectCondition.State` | `State` | `State` |
| `AspectCondition.Variant` | `Variant` | `Variant` |
| `AspectCondition.Property` | `UiProperty` | `Property` |
| `AspectCondition.Data` | `DataContext` | `Data`, one dependency per `AspectDataDependency` |
| `AspectCondition.Predicate` and `AspectCondition.Signal` | `Predicate` | `DiagnosticName` (the predicate name or the signal key name) |

No built-in condition reports `Token`.

`AspectEngine` uses the kinds of a matched rule to classify its declarations for Aspect motion (`AspectMotionSource`): `State`, `UiProperty`, and `Predicate` count as `State`; `Variant` counts as `Variant`; `DataContext` counts as `Data`. `Token` adds nothing. A rule whose conditions report none of these kinds is classified as `Base`.

`AspectTrace` diagnostics display the kind of each dependency.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `State` | `0` | The condition read an `AspectState`. |
| `Variant` | `1` | The condition read an Aspect variant. |
| `UiProperty` | `2` | The condition read a `UiProperty` value. |
| `DataContext` | `3` | The condition read data-context values. |
| `Token` | `4` | The condition read an Aspect token. Not reported by built-in conditions. |
| `Predicate` | `5` | The condition ran a predicate or checked a signal. |

## Applies to

Cerneala Aspect condition evaluation.

## See also

- `Cerneala.UI.Aspect.AspectConditionDependency`
- `Cerneala.UI.Aspect.AspectConditionResult`
- `Cerneala.UI.Aspect.AspectCondition`
- `Cerneala.UI.Aspect.AspectMotionSource`
