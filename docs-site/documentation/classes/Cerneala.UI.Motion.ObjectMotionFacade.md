# ObjectMotionFacade Class

## Definition

Namespace: `Cerneala.UI.Motion`

Assembly/Project: `Cerneala`

Source: `UI/Motion/ObjectMotionFacade.cs`

Creates typed property animations for an arbitrary reference object.

```csharp
public sealed class ObjectMotionFacade
```

## Examples

```csharp
OuterGlowStyle glow = new() { Size = 3f };

MotionHandle handle = glow.Motion()
    .Animate(OuterGlowStyle.SizeProperty)
    .From(3f)
    .To(18f)
    .Start(pulse);
```

## Remarks

The facade is obtained from the `Motion(object)` extension method. It is deliberately independent of `RenderSurface2D` and `PrismImage`: the target may be any reference object with a compatible `MotionProperty<TTarget, TValue>` descriptor. The Cerneala host frame loop advances the animation even when the object is not currently drawn.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Animate<TTarget, TValue>(MotionProperty<TTarget, TValue>)` | `ObjectMotionAnimationBuilder<TTarget, TValue>` | Selects a typed target property and creates its animation builder. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Animate` | `ArgumentNullException` | `property` is `null`. |
| `Animate` | `InvalidOperationException` | The descriptor's target type is incompatible with the facade's object. |

## See also

- `Cerneala.UI.Motion.MotionExtensions`
- `Cerneala.UI.Motion.MotionProperty<TTarget, TValue>`
- `Cerneala.UI.Motion.ObjectMotionAnimationBuilder<TTarget, TValue>`
