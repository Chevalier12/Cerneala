# AspectConditionDependency Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectConditionDependency.cs`

Represents one input dependency reported while evaluating an aspect condition.

```csharp
public sealed record AspectConditionDependency(
    AspectConditionDependencyKind Kind,
    AspectState? State = null,
    AspectVariantKey? Variant = null,
    UiProperty? Property = null,
    AspectDataDependency? Data = null,
    AspectToken? Token = null,
    string? DiagnosticName = null);
```

Inheritance:
`object` -> `AspectConditionDependency`

Implements:
`IEquatable<AspectConditionDependency>`

## Examples

Inspect dependency kinds after evaluating a compound condition:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;

AspectVariantKey<Button, ButtonKind> key = AspectVariantKey.For<Button, ButtonKind>("kind");
AspectCondition condition = AspectCondition.All(
    AspectCondition.State(AspectState.Hover),
    AspectCondition.Variant(key, ButtonKind.Primary),
    AspectCondition.Property(ButtonBase.IsPressedProperty).Is(true),
    AspectCondition.Data<UserCard>("important user", user => user.IsImportant, AspectDataDependency.Named("user")));

AspectConditionResult result = condition.Evaluate(new AspectMatchContext(new Button(), dataContext: new AspectDataContext(new UserCard(true))));

bool dependsOnState = result.Dependencies.Any(dependency => dependency.Kind == AspectConditionDependencyKind.State);
bool dependsOnData = result.Dependencies.Any(dependency => dependency.Kind == AspectConditionDependencyKind.DataContext);

internal sealed record UserCard(bool IsImportant);
```

Create a dependency record directly for diagnostic or test code:

```csharp
using Cerneala.UI.Aspect;

AspectConditionDependency dependency = new(
    AspectConditionDependencyKind.State,
    State: AspectState.Hover);
```

## Remarks

`AspectConditionDependency` is the dependency payload stored on `AspectConditionResult.Dependencies`. Internal condition nodes create records for the inputs they read: states, variants, UI properties, data-context dependencies, or predicate diagnostic names.

`AspectEngine.Resolve` collects these records from evaluated rule targets and projects them into the resolved `AspectDependencySet`. Only dependencies with a populated category payload are copied into the final state, variant, property, or data dependency lists. Predicate dependencies currently carry `DiagnosticName` for diagnostics and do not become a concrete invalidation category in `AspectDependencySet`.

The type is a record, so equality is value-based across `Kind` and all payload properties. The constructor does not enforce that `Kind` and payload match; built-in condition nodes supply the matching payload for each built-in dependency kind.

## Constructors

| Name | Description |
| --- | --- |
| `AspectConditionDependency(AspectConditionDependencyKind kind, AspectState? state = null, AspectVariantKey? variant = null, UiProperty? property = null, AspectDataDependency? data = null, AspectToken? token = null, string? diagnosticName = null)` | Initializes a dependency record with a category and optional payload values for that category. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Kind` | `AspectConditionDependencyKind` | Gets the dependency category reported by the condition. |
| `State` | `AspectState?` | Gets the aspect state used by a state condition, when `Kind` is `State`. |
| `Variant` | `AspectVariantKey?` | Gets the variant key used by a variant condition, when `Kind` is `Variant`. |
| `Property` | `UiProperty?` | Gets the UI property read by a property condition, when `Kind` is `UiProperty`. |
| `Data` | `AspectDataDependency?` | Gets the data-context dependency declared by a data condition, when `Kind` is `DataContext`. |
| `Token` | `AspectToken?` | Gets an aspect token dependency payload, when supplied. |
| `DiagnosticName` | `string?` | Gets the diagnostic name associated with a predicate dependency or custom dependency record. |

## Applies to

Cerneala UI aspect condition evaluation, diagnostics, and invalidation dependency tracking.

## See also

- `Cerneala.UI.Aspect.AspectCondition`
- `Cerneala.UI.Aspect.AspectConditionResult`
- `Cerneala.UI.Aspect.AspectDependencySet`
- `Cerneala.UI.Aspect.AspectEngine`
