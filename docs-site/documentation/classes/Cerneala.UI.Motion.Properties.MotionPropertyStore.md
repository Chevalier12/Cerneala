# MotionPropertyStore Class

## Definition

Namespace: `Cerneala.UI.Motion.Properties`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Properties/MotionPropertyStore.cs`

Stores motion property bindings and batches animation-source writes before they are flushed into UI property storage.

```csharp
public sealed class MotionPropertyStore
```

Inheritance:
`object` -> `MotionPropertyStore`

## Examples

Get or create a typed binding through the root motion system and advance the root so staged animation writes are flushed:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;

UIRoot root = new();
Control control = new();
root.VisualChildren.Add(control);

MotionPropertyBinding<Color> binding =
    root.Motion.Properties.GetOrCreateBinding(
        root.Motion,
        control,
        Control.BackgroundProperty);

binding.AnimateTo(
    Color.White,
    Motion.Tween<Color>(TimeSpan.FromMilliseconds(100)));

root.ProcessFrame();
```

## Remarks

`MotionPropertyStore` is owned by `MotionSystem.Properties`. It keeps one `MotionPropertyBinding` per target/property pair, keyed by `MotionPropertyKey`, and exposes `BindingCount` for the number of cached bindings.

`GetOrCreateBinding<T>` returns an existing typed binding when one already exists for the same target and property. If the cached binding has a different value type, the method throws `InvalidOperationException`; otherwise it creates a new `MotionValue<T>` from the target's current property value and the value mixer resolved from `MotionSystem.Mixers`.

Animation samples are not written directly when bindings tick. Internal staging methods replace any pending write for the same target/property key, so the next flush applies only the latest staged set or clear operation. `Flush` writes staged values as `UiPropertyValueSource.Animation`, clears animation-source values when requested, skips unchanged effective values, and returns `MotionPropertyFlushResult` counters for property writes, render invalidations, and layout invalidations.

`MotionSystem.Tick` calls `Flush` after sampling the motion graph and merges the returned counters into `MotionFrameResult`. `MotionSystem.HasActiveMotion` remains true while the store has pending writes, even if the graph itself is idle.

## Constructors

| Name | Description |
| --- | --- |
| `MotionPropertyStore()` | Initializes an empty store with no cached bindings or pending writes. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `HasPendingWrites` | `bool` | Gets whether at least one staged animation property write is waiting to be flushed. |
| `BindingCount` | `int` | Gets the number of cached motion property bindings. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetOrCreateBinding<T>(MotionSystem motion, UIElement target, UiProperty<T> property)` | `MotionPropertyBinding<T>` | Gets the cached typed binding for `target` and `property`, or creates one from the target's current value and a resolved value mixer. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `GetOrCreateBinding<T>(...)` | `ArgumentNullException` | `motion`, `target`, or `property` is `null`. |
| `GetOrCreateBinding<T>(...)` | `InvalidOperationException` | An existing binding for the same target/property key has an incompatible value type, or the value mixer registry cannot resolve a mixer for the property type. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Core.MotionSystem`
- `Cerneala.UI.Motion.Properties.MotionPropertyBinding<T>`
- `Cerneala.UI.Motion.Properties.MotionPropertyKey`
- `Cerneala.UI.Motion.Properties.MotionPropertyFlushResult`
- `Cerneala.UI.Core.UiObject`
- `Cerneala.UI.Core.UiProperty<T>`
- `Cerneala.UI.Core.UiPropertyValueSource`
