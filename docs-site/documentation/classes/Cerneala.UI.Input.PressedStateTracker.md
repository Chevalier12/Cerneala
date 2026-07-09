# PressedStateTracker Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/PressedStateTracker.cs`

Tracks the current `IInputPressable` element for pointer press state and clears that state on release or cancel.

```csharp
public sealed class PressedStateTracker
```

Inheritance:
`Object` -> `PressedStateTracker`

## Examples

```csharp
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Input;

ButtonBase button = new();
PressedStateTracker tracker = new();

tracker.Press(button);
bool pressedDuringPointerDown = button.IsPressed;

tracker.Release();
bool pressedAfterRelease = button.IsPressed;

// pressedDuringPointerDown == true
// pressedAfterRelease == false
```

```csharp
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

ButtonBase button = new();
UIElement child = new();
button.VisualChildren.Add(child);

PressedStateTracker tracker = new();
tracker.Press(child);

// tracker.PressedElement == button
// button.IsPressed == true
```

## Remarks

`PressedStateTracker` resolves the nearest `IInputPressable` by walking from the supplied `UIElement` up through `VisualParent`. This lets a pointer press on a visual child set the pressed state on an ancestor control such as `ButtonBase`.

Calling `Press` with `null`, or with an element whose visual ancestor chain does not contain an `IInputPressable`, cancels any existing pressed state. Calling `Press` for the same resolved pressable again leaves the state unchanged. Calling `Press` for a different pressable first clears the previous `PressedElement`, then sets the new pressable's `IsPressed` property to `true`.

`Release` delegates to `Cancel`; both clear the current `PressedElement` and set its `IsPressed` property to `false`. `ElementInputBridge` uses this tracker during pointer button dispatch to set pressed visuals on pointer down and clear them on pointer up.

## Constructors

| Name | Description |
| --- | --- |
| `PressedStateTracker()` | Initializes a tracker with no pressed element. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `PressedElement` | `IInputPressable?` | Gets the currently pressed input element, or `null` when no pressable element is tracked. |

## Methods

| Name | Description |
| --- | --- |
| `Press(UIElement?)` | Resolves the supplied element or one of its visual ancestors to an `IInputPressable`, sets it as `PressedElement`, and sets `IsPressed` to `true`. If no pressable target is found, clears any existing pressed state. |
| `Release()` | Clears the current pressed state. |
| `Cancel()` | Clears the current pressed state without requiring a pointer release. |

## Applies to

Cerneala retained UI pointer input.

## See also

- `Cerneala.UI.Input.IInputPressable`
- `Cerneala.UI.Input.ElementInputBridge`
- `Cerneala.UI.Controls.Primitives.ButtonBase`
- `Cerneala.UI.Elements.UIElement`
