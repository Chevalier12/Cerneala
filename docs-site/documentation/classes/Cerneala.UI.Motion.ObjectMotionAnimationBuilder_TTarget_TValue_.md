# ObjectMotionAnimationBuilder&lt;TTarget, TValue&gt; Class

## Definition

Namespace: `Cerneala.UI.Motion`

Assembly/Project: `Cerneala`

Source: `UI/Motion/ObjectMotionAnimationBuilder.cs`

Builds and starts a Motion animation for one typed property on an arbitrary reference object.

```csharp
public sealed class ObjectMotionAnimationBuilder<TTarget, TValue>
    where TTarget : class
```

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `From(TValue value)` | `ObjectMotionAnimationBuilder<TTarget, TValue>` | Sets the explicit starting value. If omitted, Motion starts from the property's current value. |
| `To(TValue value)` | `ObjectMotionAnimationBuilder<TTarget, TValue>` | Sets the destination value. |
| `Start(MotionSpec<TValue> spec)` | `MotionHandle` | Starts the animation and holds its destination after completion. |
| `Start(MotionSpec<TValue> spec, MotionPropertyStartOptions options)` | `MotionHandle` | Starts the animation with explicit completion and handoff options. |

## Remarks

`Start` is the operation that schedules the animation. It accepts the existing `MotionSpec<TValue>` hierarchy, including tweens, springs, keyframes, sequences, repeats, and ping-pong specifications. The returned `MotionHandle` uses the same lifecycle and cancellation model as retained UI motion.

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Start` | `ArgumentNullException` | `spec` or `options` is `null`. |
| `Start` | `InvalidOperationException` | No mixer is available for a non-discrete property value type. |

## See also

- `Cerneala.UI.Motion.ObjectMotionFacade`
- `Cerneala.UI.Motion.MotionProperty<TTarget, TValue>`
- `Cerneala.UI.Motion.Specs.MotionSpec<T>`
- `Cerneala.UI.Motion.Properties.MotionPropertyStartOptions`
- `Cerneala.UI.Motion.Core.MotionHandle`
