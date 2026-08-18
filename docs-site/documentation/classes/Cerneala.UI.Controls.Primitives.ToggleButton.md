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

`ToggleButton` inherits release activation from `ButtonBase`. When `ButtonBase` receives a completed left-button mouse-up, it invokes the overridden `OnClick`; `ToggleButton` first calls `OnToggle`, raises `Checked` or `Unchecked` for the resulting state, and then lets the base implementation raise `Click`.

`IsChecked` is backed by a `UiProperty<bool>` with a default value of `false`. Its metadata uses `AffectsRender` and `AffectsInputVisual`, so changing the checked state invalidates visual output relevant to rendering and input visuals.

Derive from `ToggleButton` and override `OnToggle` when a custom toggle policy is needed. The base implementation simply assigns `IsChecked = !IsChecked`.

## Constructors

| Name | Description |
| --- | --- |
| `ToggleButton()` | Initializes a toggle button using `ButtonBase`'s focus, cursor, and mouse-up activation behavior. |

## Fields

| Name | Description |
| --- | --- |
| `CheckedEvent` | Identifies the bubbling `Checked` event. |
| `UncheckedEvent` | Identifies the bubbling `Unchecked` event. |
| `IsCheckedProperty` | Identifies the `IsChecked` UI property. The default value is `false`; metadata affects render and input visuals. |

## Properties

| Name | Description |
| --- | --- |
| `IsChecked` | Gets or sets whether the toggle button is currently checked. |

## Methods

| Name | Description |
| --- | --- |
| `OnToggle()` | Flips `IsChecked`. Override this method to customize checked-state transitions. |
| `OnClick()` | Toggles the state, raises `Checked` or `Unchecked`, and then delegates to `ButtonBase.OnClick()` to raise `Click`. |

## Events

| Name | Event Type | Description |
| --- | --- | --- |
| `Checked` | `RoutedEventHandler` | Occurs after `IsChecked` changes to `true` during activation. |
| `Unchecked` | `RoutedEventHandler` | Occurs after `IsChecked` changes to `false` during activation. |

## Applies to

Cerneala retained UI controls.

## See also

- `Cerneala.UI.Controls.Button`
- `Cerneala.UI.Controls.Primitives.ButtonBase`
- `Cerneala.UI.Core.UiProperty<T>`
