# InputButtonState Struct

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/InputButtonState.cs`

Represents the previous and current down state of an input button and exposes edge-state helpers for press and release transitions.

```csharp
public readonly record struct InputButtonState(bool WasDown, bool IsDown)
```

Inheritance:
`Object` -> `ValueType` -> `InputButtonState`

Implements:
`IEquatable<InputButtonState>`

## Examples

```csharp
using Cerneala.UI.Input;

InputButtonState state = new(wasDown: false, isDown: true);

if (state.IsPressed)
{
    // The button changed from up to down in this input frame.
}
```

## Remarks

`InputButtonState` is a small immutable value type for comparing two button samples. `WasDown` stores the previous sample, while `IsDown` stores the current sample.

`IsPressed` is `true` only when the current sample is down and the previous sample was not down. `IsReleased` is `true` only when the current sample is up and the previous sample was down. If both samples have the same value, both transition properties are `false`.

Because this is a `readonly record struct`, it has value equality and record-generated members such as `Deconstruct`, `Equals`, `GetHashCode`, and `ToString`.

## Constructors

| Name | Description |
| --- | --- |
| `InputButtonState(bool WasDown, bool IsDown)` | Initializes the state with the previous and current down values. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `WasDown` | `bool` | Gets whether the button was down in the previous sample. |
| `IsDown` | `bool` | Gets whether the button is down in the current sample. |
| `IsPressed` | `bool` | Gets whether the button transitioned from up to down. |
| `IsReleased` | `bool` | Gets whether the button transitioned from down to up. |

## Methods

| Name | Description |
| --- | --- |
| `Deconstruct(out bool WasDown, out bool IsDown)` | Deconstructs the positional record fields into separate values. |
| `Equals(InputButtonState other)` | Determines whether another `InputButtonState` has the same component values. |
| `Equals(object? obj)` | Determines whether an object is an equivalent `InputButtonState`. |
| `GetHashCode()` | Returns a hash code based on the record component values. |
| `ToString()` | Returns the record-style string representation. |

## Applies to

Cerneala UI input state tracking.

## See also

- `Cerneala.UI.Input.InputMouseButton`
- `Cerneala.UI.Input.InputFrame`
- `Cerneala.UI.Input.PointerSnapshot`
