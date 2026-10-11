# MotionPropertyInvalidationCategory Enum

## Definition
Namespace: `Cerneala.UI.Motion.Properties`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Properties/MotionPropertyInvalidationCategory.cs`

Flags that describe which kinds of work an animated property can invalidate.

```csharp
[Flags]
public enum MotionPropertyInvalidationCategory
```

## Examples

Classify built-in properties:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Properties;

Console.WriteLine(MotionPropertyInvalidationClassifier.Classify(UIElement.OpacityProperty));
// Render
Console.WriteLine(MotionPropertyInvalidationClassifier.Classify(UIElement.WidthProperty));
// Layout
Console.WriteLine(MotionPropertyInvalidationClassifier.Classify(UIElement.VisibilityProperty));
// Render, Layout, HitTest, Semantics
```

## Remarks

`MotionPropertyInvalidationClassifier.Classify` computes the category from the property's `UiPropertyOptions`:

| `UiPropertyOptions` flag | Category flag added |
| --- | --- |
| `AffectsMeasure` or `AffectsArrange` | `Layout` |
| `AffectsRender` or `AffectsInputVisual` | `Render` |
| `AffectsHitTest` | `HitTest` |
| `AffectsSemantics` | `Semantics` |

Each `MotionPropertyBinding<T>` classifies its property when it is created and attaches the category to every write it stages. `MotionPropertyOptions.InvalidationCategory` exposes the category for registered animatable properties.

The category is used for two things:

- **Counters.** When the motion system flushes a write that changes the property value, it counts one render invalidation for a write with `Render` and one layout invalidation for a write with `Layout`. The totals appear in `MotionFrameResult.MotionRenderInvalidations` and `MotionFrameResult.MotionLayoutInvalidations`. A write that does not change the value is skipped and not counted.
- **Scroll-linked layout check.** Binding a `ScrollMotionBinding<T>` to a property whose category contains `Layout` throws `InvalidOperationException` unless `ScrollMotionBinding<T>.AllowLayout()` was called first.

The category does not decide what the element invalidates. When a write changes the value, the element invalidates measure, arrange, render, or hit testing according to the property's `UiPropertyOptions`, the same way as for any other value change. No current code reads the `HitTest` or `Semantics` flags of this enum.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `None` | `0` | The property affects none of the listed kinds of work. |
| `Render` | `1` | The property affects rendering or the input visual. |
| `Layout` | `2` | The property affects measure or arrange. |
| `HitTest` | `4` | The property affects hit testing. |
| `Semantics` | `8` | The property affects semantics. |

## Applies to

Cerneala motion property bindings and animatable property registration.

## See also

- `Cerneala.UI.Motion.Properties.MotionPropertyInvalidationClassifier`
- `Cerneala.UI.Motion.Properties.MotionPropertyOptions`
- `Cerneala.UI.Motion.Properties.MotionPropertyFlushResult`
- `Cerneala.UI.Motion.Core.MotionFrameResult`
- `Cerneala.UI.Motion.Input.ScrollMotionBinding_T_`
- `Cerneala.UI.Core.UiPropertyOptions`
