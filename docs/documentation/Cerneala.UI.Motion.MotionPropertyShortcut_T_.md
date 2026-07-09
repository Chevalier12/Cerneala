# MotionPropertyShortcut<T> Class

## Definition

Namespace: `Cerneala.UI.Motion`

Assembly/Project: `Cerneala`

Source: `UI/Motion/MotionAnimationBuilder.cs`

Provides a fluent shortcut for animating or scroll-binding one predefined `UiProperty<T>` on a `UIElement`.

```csharp
public sealed class MotionPropertyShortcut<T>
```

Inheritance:
`object` -> `MotionPropertyShortcut<T>`

## Examples

Animate one of the built-in motion shortcut properties:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

Border panel = new();

MotionHandle handle = panel.Motion()
    .Opacity
    .To(0.35f, Motion.Tween<float>(TimeSpan.FromMilliseconds(150)));
```

Bind a shortcut property to scroll progress:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Motion;

ScrollViewer scrollViewer = new();
Border header = new();

ScrollTimeline timeline = scrollViewer.Motion().ScrollTimeline();

header.Motion()
    .TranslateY
    .Bind(timeline.Progress.Map(0f, -24f));
```

## Remarks

`MotionPropertyShortcut<T>` is returned by shortcut properties on `MotionElementFacade`, such as `Opacity`, `TranslateX`, `TranslateY`, and `Scale`. It stores the target facade and `UiProperty<T>` internally; callers normally get instances through `element.Motion()` rather than constructing them directly.

`To(T value, MotionSpec<T> spec)` is a compact form of `facade.Animate(property).To(value).With(spec)`. It resolves the element's `MotionSystem`, creates or reuses the property binding, and starts an animation that holds the target value when complete.

`Bind(ScrollMotionBinding<T> binding)` applies a scroll-linked binding to the shortcut property. The binding writes its current mapped value immediately and then updates the property when the underlying scroll timeline progress changes. The same scroll binding rules apply as with `MotionAnimationBuilder<T>.Bind`, including the current `float` value support and the explicit `AllowLayout()` opt-in for layout-affecting properties.

The animation path requires the target element to be attached to a `UIRoot`, unless the element itself is a root. If the facade cannot resolve a motion system, animation throws `InvalidOperationException`.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `To(T value, MotionSpec<T> spec)` | `MotionHandle` | Starts an animation from the property's current motion value to `value` using `spec`. |
| `Bind(ScrollMotionBinding<T> binding)` | `void` | Binds a scroll-linked value source to the shortcut property. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `To(T value, MotionSpec<T> spec)` | `ArgumentNullException` | `spec` is `null` through the delegated animation builder path. |
| `To(T value, MotionSpec<T> spec)` | `InvalidOperationException` | The target element is not attached to a `UIRoot` and is not itself a root. |
| `Bind(ScrollMotionBinding<T> binding)` | `ArgumentNullException` | `binding` is `null`. |
| `Bind(ScrollMotionBinding<T> binding)` | `InvalidOperationException` | The scroll binding rejects the target property, such as a layout-affecting property without `AllowLayout()`, or the binding type is unsupported. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.MotionElementFacade`
- `Cerneala.UI.Motion.MotionAnimationBuilder<T>`
- `Cerneala.UI.Motion.Input.ScrollMotionBinding<T>`
- `Cerneala.UI.Motion.Input.ScrollTimeline`
- `Cerneala.UI.Motion.Core.MotionHandle`
- `Cerneala.UI.Motion.Specs.MotionSpec<T>`
