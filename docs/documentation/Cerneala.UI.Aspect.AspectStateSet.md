# AspectStateSet Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectStateSet.cs`

Represents an immutable set of `AspectState` values used when matching aspect conditions for a UI element.

```csharp
public sealed class AspectStateSet : IEquatable<AspectStateSet>
```

Inheritance:
`object` -> `AspectStateSet`

Implements:
`IEquatable<AspectStateSet>`

## Examples

Create a state set manually and test whether it contains a state:

```csharp
using Cerneala.UI.Aspect;

AspectState danger = AspectState.Create("danger");
AspectStateSet states = AspectStateSet.Empty
    .Add(AspectState.Hover)
    .Add(danger);

bool hasHover = states.Contains(AspectState.Hover);
bool hasDanger = states.Contains(danger);
```

Build a state set from an element before evaluating a state condition:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

Button button = new()
{
    IsPointerOver = true
};

AspectStateSet states = AspectStateSet.FromElement(button);
bool matchesHover = states.Contains(AspectState.Hover);
```

## Remarks

`AspectStateSet` stores the current aspect states that participate in aspect matching. `AspectEngine` creates a set with `FromElement(UIElement)` and passes it to `AspectMatchContext`, where `AspectCondition.State(AspectState)` can check it.

The set is value-like. `Add` and `Remove` return a new `AspectStateSet` when the contents change and return the same instance when the requested change is already true. Equality and hash codes are order-independent. `ToString()` joins state names in ordinal name order, so diagnostic output is stable.

`FromElement(UIElement)` maps built-in element state to aspect state. It adds `Hover` from `UIElement.IsPointerOver`, `Pressed` when the element implements `IInputPressable` and `IsPressed` is true, `Focus` from `UIElement.IsKeyboardFocused`, `FocusWithin` from `UIElement.IsKeyboardFocusWithin`, `Disabled` when `UIElement.IsEnabled` is false, and `Selected` when the element implements `ISelectableItemContainer` and `IsSelected` is true. It does not infer `Checked` or `Expanded`.

Methods that accept an `AspectState` or `UIElement` throw `ArgumentNullException` for `null`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Empty` | `AspectStateSet` | Gets a shared empty state set. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Add(AspectState state)` | `AspectStateSet` | Returns a set containing `state`; returns the current instance when `state` is already present. |
| `Contains(AspectState state)` | `bool` | Returns whether the set contains `state`. |
| `Equals(AspectStateSet? other)` | `bool` | Returns whether this set and `other` contain the same states, regardless of order. |
| `Equals(object? obj)` | `bool` | Returns whether `obj` is an `AspectStateSet` with the same states. |
| `FromElement(UIElement element)` | `AspectStateSet` | Creates a state set from the built-in interactive state exposed by `element`. |
| `GetHashCode()` | `int` | Returns an order-independent hash code based on the contained states. |
| `Remove(AspectState state)` | `AspectStateSet` | Returns a set without `state`; returns the current instance when `state` is absent. |
| `ToString()` | `string` | Returns the contained state names joined with `, ` in ordinal name order. |

## Applies to

Cerneala UI aspect resolution and state-based aspect conditions.

## See also

- `Cerneala.UI.Aspect.AspectState`
- `Cerneala.UI.Aspect.AspectCondition`
- `Cerneala.UI.Aspect.AspectMatchContext`
- `Cerneala.UI.Aspect.AspectEngine`
