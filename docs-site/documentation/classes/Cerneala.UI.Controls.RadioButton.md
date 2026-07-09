# RadioButton Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/RadioButton.cs`

Represents a button-style selection control with an `IsChecked` UI property that is set to `true` on left mouse button release.

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

// The control also sets IsChecked to true when it receives a left MouseUp event.
option.IsChecked = true;
```

## Remarks

`RadioButton` derives from `Button`, so it keeps the button content, text rendering, focus, command, and input behavior inherited through `ButtonBase` and `ContentControl`.

The class adds a single selection state, `IsChecked`. Its constructor registers an input handler for `InputEvents.MouseUpEvent`; when that routed event is a `MouseButtonEventArgs` with `ChangedButton` equal to `InputMouseButton.Left`, the handler sets `IsChecked` to `true`.

The implementation does not define radio groups, mutual exclusion, or automatic unchecking of sibling controls. Set `IsChecked` to `false` directly when an application needs to clear the state.

`IsCheckedProperty` is registered with `UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual`, so changing `IsChecked` participates in render and input-visual invalidation.

## Constructors

| Name | Description |
| --- | --- |
| `RadioButton()` | Initializes a new `RadioButton` and registers the left mouse-up handler that checks the control. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `IsCheckedProperty` | `UiProperty<bool>` | Identifies the `IsChecked` UI property. The default value is `false`; metadata options are `AffectsRender` and `AffectsInputVisual`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `IsChecked` | `bool` | Gets or sets whether the radio button is checked. The value is stored in `IsCheckedProperty`. |

## Methods

`RadioButton` does not declare public methods beyond inherited members.

## Events

`RadioButton` does not declare public events.

## Property Information

| Item | Value |
| --- | --- |
| Identifier field | `IsCheckedProperty` |
| Property type | `bool` |
| Default value | `false` |
| Metadata/options | `UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual` |

## Applies to

`Cerneala.UI.Controls.RadioButton` in the `Cerneala` project.

## See also

- `Cerneala.UI.Controls.Button`
- `Cerneala.UI.Controls.Primitives.ButtonBase`
- `Cerneala.UI.Core.UiProperty<T>`
