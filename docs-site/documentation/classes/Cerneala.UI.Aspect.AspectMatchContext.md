# AspectMatchContext Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectMatchContext.cs`

Carries the immutable element, slot, state, variant, environment, and data-context inputs used while aspect targets and conditions decide whether a rule matches.

```csharp
public sealed class AspectMatchContext
```

Inheritance:
`object` -> `AspectMatchContext`

## Examples

Evaluate a state condition against a manually created match context:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

Button button = new();
AspectMatchContext context = new(
    button,
    states: AspectStateSet.Empty.Add(AspectState.Hover));

bool matches = AspectCondition.State(AspectState.Hover)
    .Evaluate(context)
    .Matches;
```

Provide item data for a data-context condition:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

Button button = new();
AspectDataContext dataContext = new(new UserCard(true), index: 3);
AspectMatchContext context = new(button, dataContext: dataContext);

AspectCondition condition = AspectCondition.Data<UserCard>(
    "important user",
    user => user.IsImportant,
    AspectDataDependency.Property<UserCard, bool>(nameof(UserCard.IsImportant)));

bool applies = condition.Evaluate(context).Matches;
int? itemIndex = context.ItemIndex;

internal sealed record UserCard(bool IsImportant);
```

## Remarks

`AspectMatchContext` is the per-evaluation input object for `AspectTarget.Matches`, `AspectRuleSet.ResolveDeclarations`, and `AspectCondition.Evaluate`. `AspectEngine.Resolve` builds one from the element being resolved, the element's current `AspectStateSet`, the supplied variants, the environment version, optional data context, and optional slot path.

The constructor requires a non-null `UIElement`. Optional state, variant, and data-context arguments are normalized to `AspectStateSet.Empty`, `AspectVariantSet.Empty`, and `AspectDataContext.Empty`, so condition implementations can read them without extra null checks.

Built-in conditions read the context directly: state conditions check `States`, variant conditions check `Variants`, property conditions read UI properties from `Element`, data conditions use `Data`, and target slot matching compares the target slot with `SlotPath?.Slot`.

`Data`, `DataType`, and `ItemIndex` are convenience accessors over `DataContext.Data`, `DataContext.DataType`, and `DataContext.Index`. `OwnerComponent` and `EnvironmentVersion` are stored as supplied; custom predicate conditions can inspect them through the full context.

## Constructors

| Name | Description |
| --- | --- |
| `AspectMatchContext(UIElement element, UIElement? ownerComponent = null, AspectSlotPath? slotPath = null, AspectStateSet? states = null, AspectVariantSet? variants = null, int environmentVersion = 0, AspectDataContext? dataContext = null)` | Initializes a match context for an element and optional aspect metadata. Throws `ArgumentNullException` when `element` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Data` | `object?` | Gets `DataContext.Data`, the object used by data aspect conditions. |
| `DataContext` | `AspectDataContext` | Gets the data-context metadata for the current match, or `AspectDataContext.Empty` when none was supplied. |
| `DataType` | `Type?` | Gets `DataContext.DataType`, the declared or inferred type for `Data`. |
| `Element` | `UIElement` | Gets the UI element whose aspect rules are being matched. |
| `EnvironmentVersion` | `int` | Gets the environment version captured for the match context. |
| `ItemIndex` | `int?` | Gets `DataContext.Index`, when the context represents an indexed item. |
| `OwnerComponent` | `UIElement?` | Gets the optional component owner associated with the match. |
| `SlotPath` | `AspectSlotPath?` | Gets the optional slot path used by slot-targeted aspect rules. |
| `States` | `AspectStateSet` | Gets the active aspect states for the element. |
| `Variants` | `AspectVariantSet` | Gets the variant values available to variant aspect conditions. |

## Applies to

Cerneala UI aspect target matching, condition evaluation, and aspect rule resolution.

## See also

- `Cerneala.UI.Aspect.AspectCondition`
- `Cerneala.UI.Aspect.AspectTarget`
- `Cerneala.UI.Aspect.AspectRuleSet`
- `Cerneala.UI.Aspect.AspectDataContext`
- `Cerneala.UI.Aspect.AspectEngine`
