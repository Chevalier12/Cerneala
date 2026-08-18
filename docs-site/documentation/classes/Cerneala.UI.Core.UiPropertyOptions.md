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

## Applies to
Cerneala UI runtime and framework API consumers.
