# ObjectMotionFacade Class

## Definition

Namespace: `Cerneala.UI.Motion`

Assembly/Project: `Cerneala`

Source: `UI/Motion/ObjectMotionFacade.cs`

Creates descriptor-based property animations when the receiver is statically typed as `object`.

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

The facade is obtained from the non-generic `Motion(object)` extension method, primarily when the receiver is statically typed as `object`. For a concrete receiver type, `Motion<TTarget>` returns `ObjectMotionFacade<TTarget>` and permits direct property expressions without a declared descriptor.

Both object facades are independent of `RenderSurface2D` and `PrismImage`. The Cerneala host frame loop advances the animation even when the target is not currently drawn.

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
- `Cerneala.UI.Motion.ObjectMotionFacade<TTarget>`
- `Cerneala.UI.Motion.MotionProperty<TTarget, TValue>`
- `Cerneala.UI.Motion.ObjectMotionAnimationBuilder<TTarget, TValue>`
