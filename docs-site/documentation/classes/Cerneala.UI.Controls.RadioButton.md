# RadioButton Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/RadioButton.cs`

Represents a button-style selection control with an `IsChecked` UI property that is set to `true` when the radio button is clicked.

```csharp
public class RadioButton : Button
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ContentControl` -> `ButtonBase` -> `Button` -> `RadioButton`

## Examples

```csharp
using Cerneala.UI.Controls;

RadioButton option = new()
{
    Content = "Use pressure-sensitive ink",
    IsChecked = false
};

// The control also sets IsChecked to true when it is clicked.
option.IsChecked = true;
```

A `Button` in the content keeps its click to itself:

```csharp
using Cerneala.UI.Controls;

Button inner = new() { Content = "Inner", Width = 100, Height = 30 };
RadioButton radio = new() { Content = inner, Width = 200, Height = 80 };

// A left click on the center of inner raises inner.Click once; radio.IsChecked stays false.
// A left click on the radio button outside inner raises radio.Click; radio.IsChecked becomes true.
```

## Remarks

`RadioButton` derives from `Button`, so it keeps the button content, text rendering, focus, command, and input behavior inherited through `ButtonBase` and `ContentControl`. Unlike `Button`, its constructor sets `HorizontalContentAlignment` to `Left` and `VerticalContentAlignment` to `Top` as framework defaults, because WPF's `RadioButton` style is not based on the `ButtonBase` style. So a 300 x 70 `RadioButton` with the string content `"Choice"` draws the text at the top-left corner of its border and padding; set both properties to `Center` to center it.

The class adds a single selection state, `IsChecked`. It overrides `ButtonBase.OnClick` to set `IsChecked` to `true` and then raise `Click`, as the WPF `RadioButton` checks itself in its click. So `IsChecked` changes only when the radio button itself is clicked, with the same rule that decides when `ButtonBase` raises `Click`:

- A left release checks the radio button only when it follows a left press on the radio button. Press the left button outside the radio button, move onto it, and release there: `IsChecked` stays `false`.
- A click that a command source in the content owns is that control's click, not the radio button's. With a `Button`, `CheckBox`, or `ToggleButton` as the `Content` (or inside the content), a click on that control raises only its own `Click`, and `IsChecked` stays `false`. A click on the radio button outside that control still checks it.
- A `MouseUp` that is already handled does not click the radio button. For example, when a `ComboBox` is the `Content` of a `RadioButton`, a click on an item in the open drop-down selects that item, and `IsChecked` stays `false`, because the item marks that release handled (see `Selector`).
- Keyboard activation of the focused radio button (release of Space, or Enter) clicks it, so it checks it too.

A click on an already checked radio button raises `Click` again and leaves `IsChecked` `true`.

The implementation does not define radio groups, mutual exclusion, or automatic unchecking of sibling controls. Set `IsChecked` to `false` directly when an application needs to clear the state.

`IsCheckedProperty` is registered with `UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsAspect`, so changing `IsChecked` participates in render, input-visual, and aspect invalidation. `AspectState.Checked` is active while `IsChecked` is `true`.

## Constructors

| Name | Description |
| --- | --- |
| `RadioButton()` | Initializes a new, unchecked `RadioButton` whose content alignment defaults to `Left` and `Top`. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `IsCheckedProperty` | `UiProperty<bool>` | Identifies the `IsChecked` UI property. The default value is `false`; metadata options are `AffectsRender`, `AffectsInputVisual`, and `AffectsAspect`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `IsChecked` | `bool` | Gets or sets whether the radio button is checked. The value is stored in `IsCheckedProperty`. |

## Methods

| Name | Description |
| --- | --- |
| `OnClick()` | Protected override of `ButtonBase.OnClick`. Sets `IsChecked` to `true`, then raises `Click`. |

## Events

`RadioButton` does not declare public events.

## Property Information

| Item | Value |
| --- | --- |
| Identifier field | `IsCheckedProperty` |
| Property type | `bool` |
| Default value | `false` |
| Metadata/options | `AffectsRender`, `AffectsInputVisual`, `AffectsAspect` |

## Applies to

`Cerneala.UI.Controls.RadioButton` in the `Cerneala` project.

## See also

- `Cerneala.UI.Controls.Button`
- `Cerneala.UI.Controls.Primitives.ButtonBase`
- `Cerneala.UI.Core.UiProperty<T>`
