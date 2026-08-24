# MotionExtensions Class

## Definition

Namespace: `Cerneala.UI.Motion`

Assembly/Project: `Cerneala`

Source: `UI/Motion/MotionExtensions.cs`

Provides extension methods that create Motion facades for UI elements and arbitrary reference objects.

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

Animate a property on an ordinary object without declaring a separate Motion descriptor or attaching it to the UI tree:

```csharp
using System;
using Cerneala.Drawing.Prism;
using Cerneala.UI.Motion;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

OuterGlowStyle glow = new() { Size = 3f };

glow.Motion()
    .Animate(effect => effect.Size)
    .From(3f)
    .To(18f)
    .Start(MotionFactory.Tween<float>(TimeSpan.FromSeconds(1)));
```

## Remarks

`MotionExtensions` is the entry point for both Motion models. Calling `Motion()` on a `UIElement` or a derived control creates a `MotionElementFacade` with element-specific property shortcuts, visual states, gestures, drag motion, and scroll timelines. Calling it on another reference object creates an `ObjectMotionFacade<TTarget>` whose `Animate` method accepts a direct writable-property expression such as `player => player.Position`.

Motion compiles the selected property's getter and setter once and caches the resulting internal descriptor by property identity. Repeating the same expression does not rebuild that access path and retargets the same object-property binding. Use an explicit `MotionProperty<TTarget, TValue>` only when the property needs a custom mixer, discrete interpolation, or custom accessors.

Object motion reuses the same motion graph, specs, interpolation system, and handles as retained UI motion. It is advanced by Cerneala's host frame loop, but the target does not need to be a UI element, attached to a tree, or currently drawn. Drawing code observes the target's current property values; object motion does not invalidate an on-demand render surface automatically.

The `UIElement` overloads validate `element` before constructing the facade. Later UI-element operations may require the element to be attached to a `UIRoot`, or to be a `UIRoot` itself, because those animations resolve the active `MotionSystem` from the root. The typed object overload accepts reference types. Its trailing parameter array is reserved for overload resolution and should be omitted. The non-generic object overload rejects value types because a boxed copy could not mutate the caller's original value.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Motion(UIElement element)` | `MotionElementFacade` | Creates a motion facade for the supplied UI element. |
| `Motion<TElement>(TElement element)` | `MotionElementFacade` | Keeps a derived UI-element receiver on the UI-specific Motion facade. |
| `Motion<TTarget>(TTarget target, params object[] _)` | `ObjectMotionFacade<TTarget>` | Creates a strongly typed property-motion facade for an arbitrary reference object. |
| `Motion(object target)` | `ObjectMotionFacade` | Creates a property-oriented motion facade for an arbitrary reference object. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Motion(UIElement)`, `Motion<TElement>` | `ArgumentNullException` | `element` is `null`. |
| `Motion<TTarget>` | `ArgumentNullException` | `target` is `null`. |
| `Motion(object)` | `ArgumentNullException` | `target` is `null`. |
| `Motion(object)` | `InvalidOperationException` | `target` is a boxed value type. |

## Applies to

Cerneala retained UI motion facade APIs and object motion APIs.

## See also

- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Motion.MotionElementFacade`
- `Cerneala.UI.Motion.ObjectMotionFacade`
- `Cerneala.UI.Motion.ObjectMotionFacade<TTarget>`
- `Cerneala.UI.Motion.MotionProperty<TTarget, TValue>`
- `Cerneala.UI.Motion.MotionAnimationBuilder<T>`
- `Cerneala.UI.Motion.Core.MotionSystem`
