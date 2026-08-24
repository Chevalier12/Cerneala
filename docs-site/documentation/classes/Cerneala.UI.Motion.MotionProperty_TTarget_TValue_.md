# MotionProperty&lt;TTarget, TValue&gt; Class

## Definition

Namespace: `Cerneala.UI.Motion`

Assembly/Project: `Cerneala`

Source: `UI/Motion/MotionProperty.cs`

Describes how Motion reads, writes, and interpolates one typed property on a reference object.

```csharp
public sealed class MotionProperty<TTarget, TValue>
    where TTarget : class
```

## Type parameters

| Name | Description |
| --- | --- |
| `TTarget` | Reference type that owns the property. |
| `TValue` | Property value type. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Diagnostic name of the property. |
| `IsDiscrete` | `bool` | `true` when the property changes discretely instead of using interpolation. |

## Remarks

Create instances through `MotionProperty.Create` or `MotionProperty.CreateDiscrete`. The target-specific accessors and optional mixer are retained internally and used when `ObjectMotionFacade.Animate` binds the descriptor to an object.

## See also

- `Cerneala.UI.Motion.MotionProperty`
- `Cerneala.UI.Motion.ObjectMotionFacade`
- `Cerneala.UI.Motion.ObjectMotionAnimationBuilder<TTarget, TValue>`
