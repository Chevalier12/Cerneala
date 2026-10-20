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
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ContentControl` -> `ButtonBase` -> `Button` -> `ToggleButton`

Derived:
`Cerneala.UI.Controls.CheckBox`, `Cerneala.UI.Controls.RadioButton`

## Examples

```csharp
using Cerneala.UI.Controls.Primitives;

ToggleButton toggle = new()
{
    IsChecked = false
};
int checkedCount = 0;
toggle.Checked += (_, _) => checkedCount++;

toggle.IsChecked = true;  // checkedCount == 1
toggle.IsChecked = true;  // no change: checkedCount stays 1
```

## Remarks

`ToggleButton` inherits release activation from `ButtonBase`. When `ButtonBase` receives a completed left-button mouse-up, it invokes the overridden `OnClick`; `ToggleButton` first calls `OnToggle`, and then lets the base implementation raise `Click`.

Every actual change of `IsChecked` raises `Checked` (new value `true`) or `Unchecked` (new value `false`), as in WPF, whatever made the change: a click, code such as `toggle.IsChecked = true`, a binding, or markup. Setting the value that `IsChecked` already has is no change and raises nothing. So a click raises exactly one `Checked` or `Unchecked`, before `Click`. The events are raised from `OnPropertyChanged` through the protected virtual `OnChecked` and `OnUnchecked` methods; an override that does not call the base method suppresses the event.

`IsChecked` is backed by a `UiProperty<bool>` with a default value of `false`. Its metadata uses `AffectsRender`, `AffectsInputVisual`, and `AffectsAspect`. `AspectState.Checked` is active while `IsChecked` is `true`; changing it invalidates rendering, input visuals, and aspect resolution.

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
| `IsCheckedProperty` | Identifies the `IsChecked` UI property. The default value is `false`; metadata affects render, input visuals, and aspect resolution. |

## Properties

| Name | Description |
| --- | --- |
| `IsChecked` | Gets or sets whether the toggle button is currently checked. |

## Methods

| Name | Description |
| --- | --- |
| `OnToggle()` | Flips `IsChecked`. Override this method to customize checked-state transitions. |
| `OnClick()` | Calls `OnToggle`, and then delegates to `ButtonBase.OnClick()` to raise `Click`. The `IsChecked` change raises `Checked` or `Unchecked`. |
| `OnChecked(RoutedEventArgs)` | Protected virtual. Called after `IsChecked` changes to `true`; raises `Checked`. |
| `OnUnchecked(RoutedEventArgs)` | Protected virtual. Called after `IsChecked` changes to `false`; raises `Unchecked`. |
| `OnPropertyChanged(UiPropertyChangedEventArgs)` | Protected override. After the base handling, calls `OnChecked` or `OnUnchecked` when `IsChecked` changed. |

## Events

| Name | Event Type | Description |
| --- | --- | --- |
| `Checked` | `RoutedEventHandler` | Bubbling. Occurs after each change of `IsChecked` to `true`, from a click, code, a binding, or markup. |
| `Unchecked` | `RoutedEventHandler` | Bubbling. Occurs after each change of `IsChecked` to `false`, from a click, code, a binding, or markup. |

## Applies to

Cerneala retained UI controls.

## See also

- `Cerneala.UI.Controls.Button`
- `Cerneala.UI.Controls.CheckBox`
- `Cerneala.UI.Controls.RadioButton`
- `Cerneala.UI.Controls.Primitives.ButtonBase`
- `Cerneala.UI.Core.UiProperty<T>`
