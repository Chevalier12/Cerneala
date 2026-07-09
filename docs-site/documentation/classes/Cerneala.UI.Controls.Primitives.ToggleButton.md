# ToggleButton Class

## Definition
Namespace: `Cerneala.UI.Controls.Primitives`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Primitives/ToggleButton.cs`

Represents a button control that toggles a checked state when a completed left-click is released on it.

```csharp
public class ToggleButton : Button
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ContentControl` -> `Button` -> `ToggleButton`

## Examples

```csharp
using Cerneala.UI.Controls.Primitives;

ToggleButton toggle = new()
{
    IsChecked = false
};

toggle.IsChecked = true;
```

## Remarks

`ToggleButton` registers a mouse-up handler in its constructor. When the routed mouse event is a completed left-button click, the control calls `OnToggle`, which flips `IsChecked`.

`IsChecked` is backed by a `UiProperty<bool>` with a default value of `false`. Its metadata uses `AffectsRender` and `AffectsInputVisual`, so changing the checked state invalidates visual output relevant to rendering and input visuals.

Derive from `ToggleButton` and override `OnToggle` when a custom toggle policy is needed. The base implementation simply assigns `IsChecked = !IsChecked`.

## Constructors

| Name | Description |
| --- | --- |
| `ToggleButton()` | Initializes a toggle button and registers the mouse-up handler that drives click toggling. |

## Fields

| Name | Description |
| --- | --- |
| `IsCheckedProperty` | Identifies the `IsChecked` UI property. The default value is `false`; metadata affects render and input visuals. |

## Properties

| Name | Description |
| --- | --- |
| `IsChecked` | Gets or sets whether the toggle button is currently checked. |

## Methods

| Name | Description |
| --- | --- |
| `OnToggle()` | Flips `IsChecked`. Override this method to customize checked-state transitions. |

## Applies to

Cerneala retained UI controls.

## See also

- `Cerneala.UI.Controls.Button`
- `Cerneala.UI.Controls.Primitives.ButtonBase`
- `Cerneala.UI.Core.UiProperty<T>`
