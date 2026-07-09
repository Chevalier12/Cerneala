# MotionExtensions Class

## Definition

Namespace: `Cerneala.UI.Motion`

Assembly/Project: `Cerneala`

Source: `UI/Motion/MotionExtensions.cs`

Provides extension methods that create the motion facade for a `UIElement`.

```csharp
public static class MotionExtensions
```

Inheritance:
`object` -> `MotionExtensions`

## Examples

Create a motion facade for an element and start a property animation:

```csharp
using System;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

UIRoot root = new();
UIElement target = new();
root.VisualChildren.Add(target);

MotionHandle handle = target.Motion()
    .Opacity
    .To(0.5f)
    .With(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(150)));
```

## Remarks

`MotionExtensions` is the entry point for the fluent motion API on `UIElement` instances. Calling `Motion()` does not start an animation by itself; it creates a new `MotionElementFacade` around the element so callers can choose property shortcuts, typed property animation, visual state helpers, gesture motion, drag motion, or scroll timelines through the facade.

The extension method validates the `element` argument before constructing the facade. Later operations on the returned facade may require the element to be attached to a `UIRoot`, or to be a `UIRoot` itself, because animations resolve the active `MotionSystem` from the root.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Motion(UIElement element)` | `MotionElementFacade` | Creates a motion facade for the supplied UI element. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Motion(UIElement)` | `ArgumentNullException` | `element` is `null`. |

## Applies to

Cerneala retained UI motion facade APIs for `UIElement`.

## See also

- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Motion.MotionElementFacade`
- `Cerneala.UI.Motion.MotionAnimationBuilder<T>`
- `Cerneala.UI.Motion.Core.MotionSystem`
