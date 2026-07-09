# AspectTarget Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectTarget.cs`

Describes the element type, optional template slot, and optional conditions that an aspect rule must match.

```csharp
public sealed class AspectTarget
```

Inheritance:
`object` -> `AspectTarget`

## Examples

Create a target that matches all `Button` instances:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectTarget target = new(typeof(Button));
```

Create a target that matches hovered buttons:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectTarget target = new(
    typeof(Button),
    conditions: [AspectCondition.State(AspectState.Hover)]);
```

Create a target for a named template slot:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectTarget target = new(
    typeof(ContentPresenter),
    ButtonSlots.Content,
    [AspectCondition.State(AspectState.Focus)]);
```

## Remarks

`AspectTarget` is used by `AspectRuleSet` to decide whether a rule applies to an `AspectMatchContext`. A target matches only when the context element is an instance of `ElementType`, the optional `Slot` equals the current slot path's slot, and every condition in `Conditions` evaluates with `Matches == true`.

The constructor accepts only types assignable to `UIElement`. Passing `typeof(UIElement)` creates the least component-specific target; passing a concrete element type contributes component specificity. A non-null slot contributes slot specificity, and condition specificity is added from each condition in `Conditions`.

`Conditions` is stored as the supplied read-only list, or as an empty list when `conditions` is `null`. The class does not copy or normalize the list.

`ToString()` returns the element type name for unslotted targets, or `ElementTypeName@SlotName` when a slot is present.

## Constructors

| Name | Description |
| --- | --- |
| `AspectTarget(Type elementType, AspectSlot? slot = null, IReadOnlyList<AspectCondition>? conditions = null)` | Initializes a target for a `UIElement` type, with an optional slot and optional match conditions. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ElementType` | `Type` | Gets the UI element type the target matches. |
| `Slot` | `AspectSlot?` | Gets the optional slot that must match `AspectMatchContext.SlotPath?.Slot`. |
| `Conditions` | `IReadOnlyList<AspectCondition>` | Gets the conditions that must all match the context. |
| `Specificity` | `AspectSpecificity` | Gets the cascade specificity contributed by the element type, slot, and conditions. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Matches(AspectMatchContext context)` | `bool` | Returns `true` when the context element type, optional slot, and all conditions match. |
| `ToString()` | `string` | Returns a compact target name, including the slot name when present. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `AspectTarget(...)` | `ArgumentNullException` | `elementType` is `null`. |
| `AspectTarget(...)` | `ArgumentException` | `elementType` does not derive from `UIElement`. |
| `Matches(AspectMatchContext context)` | `ArgumentNullException` | `context` is `null`. |

## Applies to

Cerneala UI aspect rule matching and cascade specificity.

## See also

- `Cerneala.UI.Aspect.AspectRuleSet`
- `Cerneala.UI.Aspect.AspectMatchContext`
- `Cerneala.UI.Aspect.AspectCondition`
- `Cerneala.UI.Aspect.AspectSpecificity`
- `Cerneala.UI.Elements.UIElement`
