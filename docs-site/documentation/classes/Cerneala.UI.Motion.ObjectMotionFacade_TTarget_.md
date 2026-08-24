# ObjectMotionFacade<TTarget> Class

## Definition

Namespace: `Cerneala.UI.Motion`

Assembly/Project: `Cerneala`

Source: `UI/Motion/ObjectMotionFacade.cs`

Creates strongly typed property animations for an arbitrary reference object.

```csharp
public sealed class ObjectMotionFacade<TTarget>
    where TTarget : class
```

Type parameters:

- `TTarget`: The reference-object type whose properties Motion updates.

## Examples

Animate a player property without declaring a separate `MotionProperty`:

```csharp
using System;
using Cerneala.Drawing;
using Cerneala.UI.Motion;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

Player player = new() { Position = new DrawPoint(40, 80) };

player.Motion()
    .Animate(current => current.Position)
    .From(new DrawPoint(40, 80))
    .To(new DrawPoint(360, 80))
    .Start(MotionFactory.Tween<DrawPoint>(TimeSpan.FromMilliseconds(240)));
```

## Remarks

Obtain this facade by calling `Motion()` on a concrete reference object. `Animate` accepts an expression that directly selects a writable instance property or field, such as `player => player.Position`. Nested access, methods, static members, and read-only members are rejected because Motion must read and write one stable member on the target object.

The expression is resolved once per target type, value type, and property. Motion caches its compiled getter, compiled setter, and descriptor, so expressions for the same property share the same conflict and retargeting identity.

The target does not need to derive from a Cerneala UI type or be attached to a visual tree. Cerneala's host frame loop advances the animation in the background. Rendering code reads the target's current value when it draws a frame.

Pass an explicit `MotionProperty<TPropertyTarget, TValue>` when custom accessors, discrete interpolation, or a property-specific mixer are required.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Animate<TValue>(Expression<Func<TTarget, TValue>> property)` | `ObjectMotionAnimationBuilder<TTarget, TValue>` | Selects a direct writable member and creates its animation builder. |
| `Animate<TPropertyTarget, TValue>(MotionProperty<TPropertyTarget, TValue> property)` | `ObjectMotionAnimationBuilder<TPropertyTarget, TValue>` | Selects an explicitly described property compatible with the target object. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Animate(Expression<...>)` | `ArgumentNullException` | `property` is `null`. |
| `Animate(Expression<...>)` | `ArgumentException` | The expression does not directly select a writable instance member. |
| `Animate(MotionProperty<...>)` | `ArgumentNullException` | `property` is `null`. |
| `Animate(MotionProperty<...>)` | `InvalidOperationException` | The descriptor's target type is incompatible with the facade's object. |

## Applies to

Code-driven Motion animations for custom game state, Prism operations, scene models, and other reference objects.

## See also

- `Cerneala.UI.Motion.MotionExtensions`
- `Cerneala.UI.Motion.ObjectMotionFacade`
- `Cerneala.UI.Motion.ObjectMotionAnimationBuilder<TTarget, TValue>`
- `Cerneala.UI.Motion.MotionProperty<TTarget, TValue>`
- `Cerneala.UI.Motion.Specs.Motion`
