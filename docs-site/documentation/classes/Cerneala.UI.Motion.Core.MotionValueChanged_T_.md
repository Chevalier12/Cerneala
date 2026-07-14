# MotionValueChanged<T> Struct

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionValue{T}.cs`

Describes a value change reported by a motion value subscription.

```csharp
public readonly record struct MotionValueChanged<T>(
    T OldValue,
    T NewValue,
    T Target,
    bool IsAnimating);
```

Inheritance:
`ValueType` -> `MotionValueChanged<T>`

### Type Parameters

| Name | Description |
| --- | --- |
| `T` | The type of value carried by the motion value. |

## Examples

```csharp
using Cerneala.UI.Motion.Core;

MotionGraph graph = new();
MotionValue<double> opacity = graph.CreateValue(0d);

using IDisposable subscription = opacity.Subscribe(change =>
{
    double before = change.OldValue;
    double after = change.NewValue;
    double target = change.Target;
    bool isAnimating = change.IsAnimating;
});

opacity.JumpTo(1d);
```

## Remarks

`MotionValueChanged<T>` is created by `MotionValue<T>` and `DerivedMotionValue<T>` when subscribers are notified of a value change. `OldValue` contains the value before the change, and `NewValue` contains the value delivered to subscribers.

For `MotionValue<T>`, `Target` is the value currently targeted by the motion value when the notification is raised. During an active animation, `IsAnimating` reflects whether the value still has an active sampler and handle at the moment the change object is created.

For `DerivedMotionValue<T>`, notifications use the computed value for both `NewValue` and `Target`, and `IsAnimating` is `false`.

The struct is immutable and uses record-struct value equality across all four fields.

## Constructors

| Name | Description |
| --- | --- |
| `MotionValueChanged(T oldValue, T newValue, T target, bool isAnimating)` | Initializes a change notification with the previous value, current value, target value, and animation state. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `OldValue` | `T` | Gets the value before the notification. |
| `NewValue` | `T` | Gets the value after the notification. |
| `Target` | `T` | Gets the motion target associated with the notification. |
| `IsAnimating` | `bool` | Gets whether the source motion value was animating when the notification was created. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Deconstruct(out T oldValue, out T newValue, out T target, out bool isAnimating)` | `void` | Deconstructs the notification into its component values. |
| `Equals(MotionValueChanged<T> other)` | `bool` | Determines whether another notification has the same field values. |
| `Equals(object? obj)` | `bool` | Determines whether an object is an equivalent `MotionValueChanged<T>` value. |
| `GetHashCode()` | `int` | Returns a hash code for the notification. |
| `ToString()` | `string` | Returns the generated record-struct string representation. |

## Applies to

Cerneala motion value subscriptions.

## See also

- `Cerneala.UI.Motion.Core.MotionValue<T>`
- `Cerneala.UI.Motion.Core.DerivedMotionValue<T>`
- `Cerneala.UI.Motion.Core.MotionValue`
