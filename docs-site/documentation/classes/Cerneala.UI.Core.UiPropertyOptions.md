# UiPropertyOptions Enum
## Definition
Namespace: `Cerneala.UI.Core`
Assembly/Project: `Cerneala`
Source: `UI/Core/UiPropertyOptions.cs`
```csharp
[Flags]
public enum UiPropertyOptions
```

## Examples

```csharp
UiPropertyMetadata<int> metadata = new(
    0,
    UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender);
```

## Remarks
The enum is marked with `Flags`, so options can be combined with bitwise operators. `UiObject` forwards the invalidation-related flags to `IUiPropertyOwner.OnPropertyInvalidated` after an effective property value changes.

`AffectsParentArrange` is for a value that the parent panel reads when it places the child. For example, `Canvas.SetLeft(border, 40)` on a `Border` inside a `Canvas` invalidates the arrange pass of the `Canvas`; the `Canvas` then arranges the `Border` at its new position. The `Border`'s own arrange pass is not invalidated. When the element has no visual parent, nothing is invalidated.

## Fields

| Name | Value | Description |
| --- | --- | --- |
| `None` | `0` | No property-system option is enabled. |
| `AffectsMeasure` | `1 << 0` | The property can invalidate measurement. |
| `AffectsArrange` | `1 << 1` | The property can invalidate arrangement. |
| `AffectsRender` | `1 << 2` | The property can invalidate rendering. |
| `AffectsHitTest` | `1 << 3` | The property can invalidate hit testing. |
| `AffectsAspect` | `1 << 4` | The property can invalidate aspect processing. |
| `AffectsInputVisual` | `1 << 5` | The property can invalidate input visuals. |
| `Inherits` | `1 << 6` | The property participates in inherited-value propagation. |
| `ReadOnly` | `1 << 7` | The property is writable only through its matching `UiPropertyKey<T>`. |
| `AffectsSemantics` | `1 << 8` | The property can invalidate semantics. |
| `AffectsParentArrange` | `1 << 9` | A change invalidates the arrange pass of the element's visual parent, not of the element itself. `Canvas.LeftProperty` and `Canvas.TopProperty` use it, like WPF `FrameworkPropertyMetadataOptions.AffectsParentArrange`. |

## Applies to
Cerneala UI runtime and framework API consumers.
