# MotionPropertyInvalidationClassifier Class

## Definition

Namespace: `Cerneala.UI.Motion.Properties`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Properties/MotionPropertyInvalidationClassifier.cs`

Maps UI property metadata flags to the motion invalidation categories used when animation writes are flushed.

```csharp
public static class MotionPropertyInvalidationClassifier
```

Inheritance:
`object` -> `MotionPropertyInvalidationClassifier`

## Examples

Classify a property registered in the UI property system:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Motion.Properties;

MotionPropertyInvalidationCategory category =
    MotionPropertyInvalidationClassifier.Classify(Control.BackgroundProperty);

bool requiresRenderInvalidation =
    category.HasFlag(MotionPropertyInvalidationCategory.Render);
```

Classify raw property metadata options when no `UiProperty` instance is needed:

```csharp
using Cerneala.UI.Core;
using Cerneala.UI.Motion.Properties;

MotionPropertyInvalidationCategory category =
    MotionPropertyInvalidationClassifier.Classify(
        UiPropertyOptions.AffectsMeasure |
        UiPropertyOptions.AffectsRender);

bool affectsLayout = category.HasFlag(MotionPropertyInvalidationCategory.Layout);
bool affectsRender = category.HasFlag(MotionPropertyInvalidationCategory.Render);
```

## Remarks

`MotionPropertyInvalidationClassifier` is used by the motion property pipeline to decide which broad invalidation buckets are associated with animated property writes. `MotionPropertyBinding<T>` stores the classified category for its target property, and `MotionPropertyStore` uses that category while flushing staged animation writes.

The classifier groups `UiPropertyOptions.AffectsMeasure` and `UiPropertyOptions.AffectsArrange` into `MotionPropertyInvalidationCategory.Layout`. It groups `UiPropertyOptions.AffectsRender` and `UiPropertyOptions.AffectsInputVisual` into `MotionPropertyInvalidationCategory.Render`. `UiPropertyOptions.AffectsHitTest` maps to `HitTest`, and `UiPropertyOptions.AffectsSemantics` maps to `Semantics`.

Flags that are not motion invalidation categories, such as `AffectsAspect`, `Inherits`, and `ReadOnly`, do not add a category by themselves. Multiple matching flags produce a combined `MotionPropertyInvalidationCategory` value.

The `UiProperty` overload validates that the property is not `null`, then delegates to the `UiPropertyOptions` overload using the property's `Options`.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Classify(UiProperty property)` | `MotionPropertyInvalidationCategory` | Classifies the supplied property's metadata options into motion invalidation categories. |
| `Classify(UiPropertyOptions options)` | `MotionPropertyInvalidationCategory` | Classifies raw UI property option flags into motion invalidation categories. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Classify(UiProperty property)` | `ArgumentNullException` | `property` is `null`. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Properties.MotionPropertyInvalidationCategory`
- `Cerneala.UI.Motion.Properties.MotionPropertyBinding<T>`
- `Cerneala.UI.Motion.Properties.MotionPropertyStore`
- `Cerneala.UI.Motion.Properties.AnimatablePropertyRegistry`
- `Cerneala.UI.Core.UiProperty`
- `Cerneala.UI.Core.UiPropertyOptions`
