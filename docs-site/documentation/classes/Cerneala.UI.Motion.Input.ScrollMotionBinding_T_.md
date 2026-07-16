# ScrollMotionBinding<T> Class

## Definition

Namespace: `Cerneala.UI.Motion.Input`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Input/ScrollMotionBinding.cs`

Maps scroll timeline progress to a float motion value that can be bound to a UI property.

```csharp
public sealed class ScrollMotionBinding<T> : IDisposable
```

Inheritance:
`object` -> `ScrollMotionBinding<T>`

Implements:
`IDisposable`

## Examples

Bind vertical scroll progress to visual properties on elements owned by a `ScrollViewer`:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Input;

ScrollViewer viewer = new()
{
    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
};

Border header = new();
Border progressBar = new()
{
    RenderTransformOrigin = new LayoutPoint(0, 0.5f)
};

ScrollTimeline timeline = viewer.Motion().ScrollTimeline();
header.Motion().Opacity.Bind(timeline.Progress.Map(1f, 0.6f));
progressBar.Motion().Animate(UIElement.ScaleXProperty).Bind(timeline.Progress.Map(0.04f, 1f));

timeline.Update();
```

Opt in before binding a scroll-linked value to a property whose metadata affects layout:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion;

ScrollTimeline timeline = scrollViewer.Motion().ScrollTimeline();
TextBlock label = new() { Text = "Resizable label" };

label.Motion()
    .Animate(Control.FontSizeProperty)
    .Bind(timeline.Progress.Map(12f, 24f).AllowLayout());
```

## Remarks

`ScrollMotionBinding<T>` is created by `ScrollTimelineProgress.Map(float from, float to)`. The public creation path currently returns `ScrollMotionBinding<float>`, mapping normalized scroll progress from `0` to `1` into the supplied output range.

`Current` converts the timeline's current progress through the stored `MotionRange`. The range clamps input progress before interpolation, so values below the input start map to the output start and values above the input end map to the output end.

Binding is applied through `MotionAnimationBuilder<T>.Bind` or `MotionPropertyShortcut<T>.Bind`. When bound, the class immediately writes the current mapped value to the target `UiProperty<T>` using `UiPropertyValueSource.Animation`, then updates the property whenever the timeline progress changes.

Call `Dispose` when the binding is detached. Disposal is idempotent, releases the progress subscription, and removes every target listener so later timeline updates cannot write detached elements. A disposed binding cannot be bound again; create a new mapping for a new attachment session.

Generated `@scroll` Aspect behavior owns the binding for one attachment
lifetime, disposes it on detach, and creates a new mapping on reattach. Markup
supports only linear `float` ranges and requires `allowLayout = true` for
layout-affecting properties.

Only `float` bindings are supported by the current implementation. Calling `Current` or processing an update for another generic type throws `InvalidOperationException`.

By default, scroll-linked bindings reject properties classified as layout-affecting by `MotionPropertyInvalidationClassifier`. Call `AllowLayout` before binding when the property intentionally affects measure or arrange.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Current` | `T` | Gets the mapped value for the current scroll progress. The current implementation supports `float` values. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `AllowLayout()` | `ScrollMotionBinding<T>` | Enables binding to layout-affecting properties and returns the same binding for fluent use. |
| `Dispose()` | `void` | Idempotently releases the timeline subscription and all bound target listeners. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Current` | `InvalidOperationException` | `T` is not `float`. |
| `AllowLayout` then bind path | `InvalidOperationException` | Not thrown by `AllowLayout` itself; without calling it, binding a layout-affecting property throws. |
| Bind/update path | `InvalidOperationException` | `T` is not `float`, or a layout-affecting property is bound without `AllowLayout`. |
| Bind path | `ObjectDisposedException` | The binding has already been disposed. |

## Applies to

Cerneala scroll-linked UI motion input.

## See also

- `Cerneala.UI.Motion.Input.ScrollTimeline`
- `Cerneala.UI.Motion.Input.ScrollTimelineProgress`
- `Cerneala.UI.Motion.Input.MotionRange`
- `Cerneala.UI.Motion.MotionAnimationBuilder<T>`
- `Cerneala.UI.Motion.MotionPropertyShortcut<T>`
- `docs/motion-markup-syntax-proposal.md`
