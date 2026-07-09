# MotionVisualStateSnapshot Struct

## Definition
Namespace: `Cerneala.UI.Motion.States`

Assembly/Project: `Cerneala`

Source: `UI/Motion/States/MotionVisualStateSnapshot.cs`

Represents the pointer, keyboard-focus, and enabled visual-state flags captured from a `UIElement`.

```csharp
public readonly record struct MotionVisualStateSnapshot(
    bool IsPointerOver,
    bool IsKeyboardFocused,
    bool IsKeyboardFocusWithin,
    bool IsEnabled)
```

Inheritance:
`Object` -> `ValueType` -> `MotionVisualStateSnapshot`

Implements:
`IEquatable<MotionVisualStateSnapshot>`

## Examples

Capture an element's current visual-state flags:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.States;

UIElement element = new()
{
    IsPointerOver = true,
    IsKeyboardFocused = false,
    IsKeyboardFocusWithin = true,
    IsEnabled = true
};

MotionVisualStateController controller = new();
MotionVisualStateSnapshot snapshot = controller.Capture(element);

bool isHoverVisualActive = snapshot.IsPointerOver && snapshot.IsEnabled;
```

Create a snapshot directly when the state values are already known:

```csharp
using Cerneala.UI.Motion.States;

MotionVisualStateSnapshot snapshot = new(
    IsPointerOver: false,
    IsKeyboardFocused: true,
    IsKeyboardFocusWithin: true,
    IsEnabled: true);
```

## Remarks

`MotionVisualStateSnapshot` is a compact value object used by `MotionVisualStateController` to hold the visual-state flags read from a `UIElement`. The controller copies `UIElement.IsPointerOver`, `UIElement.IsKeyboardFocused`, `UIElement.IsKeyboardFocusWithin`, and `UIElement.IsEnabled` into a new snapshot.

The struct does not observe later element changes and does not start motion by itself. Capture a new snapshot when current visual-state flags are needed.

Because this type is a `readonly record struct`, values are immutable after construction and use value-based equality. The compiler provides record-struct members such as equality, deconstruction, hashing, and string formatting based on the primary constructor components.

The primary constructor does not validate the boolean values. Direct construction is useful for tests and state-comparison code that already has explicit visual-state values.

## Constructors

| Name | Description |
| --- | --- |
| `MotionVisualStateSnapshot(bool IsPointerOver, bool IsKeyboardFocused, bool IsKeyboardFocusWithin, bool IsEnabled)` | Initializes a visual-state snapshot with explicit pointer, keyboard-focus, focus-within, and enabled flags. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `IsPointerOver` | `bool` | Gets whether the captured element had pointer-over state. |
| `IsKeyboardFocused` | `bool` | Gets whether the captured element had keyboard focus. |
| `IsKeyboardFocusWithin` | `bool` | Gets whether the captured element or one of its descendants had keyboard focus. |
| `IsEnabled` | `bool` | Gets whether the captured element was enabled. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Deconstruct(out bool IsPointerOver, out bool IsKeyboardFocused, out bool IsKeyboardFocusWithin, out bool IsEnabled)` | `void` | Deconstructs the snapshot into its captured visual-state flags. |
| `Equals(MotionVisualStateSnapshot other)` | `bool` | Determines whether another snapshot has the same flag values. |
| `GetHashCode()` | `int` | Returns a hash code based on the snapshot flag values. |
| `ToString()` | `string` | Returns the compiler-generated record string representation. |

## Applies to

Cerneala retained UI motion visual-state APIs in the `Cerneala.UI.Motion.States` namespace.

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.States.MotionVisualStateController`
- `Cerneala.UI.Elements.UIElement`
- `UI/Motion/States/MotionVisualStateSnapshot.cs`
