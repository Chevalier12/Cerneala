# RadioButton Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/RadioButton.cs`

Represents a button that the user can check but not clear, and that unchecks the other radio buttons of its group when it is checked.

```csharp
public class RadioButton : ToggleButton
```

Attributes:
`TemplatePart("PART_CheckMark", typeof(Cerneala.UI.Elements.UIElement))`

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ContentControl` -> `ButtonBase` -> `Button` -> `ToggleButton` -> `RadioButton`

## Examples

Radio buttons that share a parent form one group:

```csharp
using Cerneala.UI.Controls;

StackPanel panel = new();
RadioButton small = new() { Content = "Small" };
RadioButton large = new() { Content = "Large" };
panel.Children.Add(small);
panel.Children.Add(large);

// The user clicks "Small", then "Large":
// small.IsChecked == false, large.IsChecked == true.
// The second click raises small.Unchecked, then large.Checked.
```

`GroupName` makes groups that do not depend on the parent:

```csharp
using Cerneala.UI.Controls;

StackPanel panel = new();
RadioButton red = new() { Content = "Red", GroupName = "Color" };
RadioButton blue = new() { Content = "Blue", GroupName = "Color" };
RadioButton small = new() { Content = "Small", GroupName = "Size" };
RadioButton large = new() { Content = "Large", GroupName = "Size" };
panel.Children.Add(red);
panel.Children.Add(small);
panel.Children.Add(blue);
panel.Children.Add(large);

red.IsChecked = true;
small.IsChecked = true;
blue.IsChecked = true;
// red.IsChecked == false, blue.IsChecked == true, small.IsChecked == true, large.IsChecked == false.
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

`RadioButton` derives from `ToggleButton`, as WPF's `RadioButton` does. It inherits `IsChecked`, `IsCheckedProperty`, `Checked`, and `Unchecked` from `ToggleButton`, so a handler for `ToggleButton.CheckedEvent` on a panel also receives the `Checked` events of radio buttons inside it. Its constructor sets `HorizontalContentAlignment` to `Left` and `VerticalContentAlignment` to `Top` as framework defaults, because WPF's `RadioButton` style is not based on the `ButtonBase` style. The default template does not read these two properties; a custom template can.

### Default template

The constructor installs the default component template `RadioButton.Default`, a one-pixel `BorderThickness`, and a `BorderBrush` of RGB (100, 110, 125) as framework defaults at the `Default` value source. Any Aspect, local, or other higher-source value replaces them; inside a `UIRoot` with the default aspect package, the `button.base` rule for `Button` also styles radio buttons. The template is laid out like the `CheckBox` template:

- A round indicator, 12 x 12 pixels (smaller only when less space is available), vertically centered at the left. It contains a ring, an `Ellipse` whose `Stroke` is `BorderBrush` and whose `StrokeThickness` is the largest absolute value of the sides of `BorderThickness` (so `-1` and `1` both give a 1 pixel stroke), and the `PART_CheckMark` dot, a 6 x 6 black `Ellipse` centered in the ring.
- A `ContentPresenter` six pixels to the right of the indicator, with `Content`, `Foreground`, `FontFamily`, and `FontSize` bound to it.
- An outer `Border` with `Background` and `Padding` bound to it, so the padding surrounds both the indicator and the content.

`PART_CheckMark` may be any `UIElement`. The control sets its `Visibility` to `Visible` while `IsChecked` is `true` and to `Hidden` otherwise, so a checked radio button draws the dot and an unchecked one draws only the ring. For example, after the user clicks `small` and then `large`, `large` draws its dot and `small` does not. The dot's `Fill` belongs to the template and is not bound to `Foreground`. The part is optional: a custom component template without it still works, but no element shows the checked state. A `ComponentTemplate` set by the application replaces the default template.

The ring `Ellipse` draws its stroke inside its arranged bounds, like WPF: the outer edge of the stroke touches the edge of the indicator, so even a thick `BorderThickness` stays inside the 12 x 12 indicator. The ring uses the magnitude of `BorderThickness`: the sign, which on a `Border` chooses inside or outside, is ignored here. For example, with the default one-pixel `BorderThickness`, the ring is drawn in the rectangle `(0.5, 0.5, 11, 11)` of the indicator, and nothing is drawn outside `(0, 0, 12, 12)`.

### Checking by click and keyboard

`RadioButton` overrides `OnToggle` to set `IsChecked` to `true`. A click never unchecks it: a click on an already checked radio button raises `Click` again, leaves `IsChecked` `true`, and raises no `Checked` event. The click follows the rule that decides when `ButtonBase` raises `Click`:

- A left release checks the radio button only when it follows a left press on the radio button. Press the left button outside the radio button, move onto it, and release there: `IsChecked` stays `false`.
- A click that a command source in the content owns is that control's click, not the radio button's. With a `Button`, `CheckBox`, or `ToggleButton` as the `Content` (or inside the content), a click on that control raises only its own `Click`, and `IsChecked` stays `false`. A click on the radio button outside that control still checks it.
- A `MouseUp` that is already handled does not click the radio button. For example, when a `ComboBox` is the `Content` of a `RadioButton`, a click on an item in the open drop-down selects that item, and `IsChecked` stays `false`, because the item marks that release handled (see `Selector`).
- Keyboard activation of the focused radio button (release of Space, or Enter) clicks it, so it checks it too.

To clear a radio button, check another one of its group, or set `IsChecked` to `false` in code.

### Groups

When a radio button becomes checked, by a click, by an arrow key, or by code such as `large.IsChecked = true`, every other checked radio button of its group becomes unchecked first. Their `Unchecked` events are raised before the `Checked` event of the newly checked one, as in WPF. The group is:

- With a non-empty `GroupName`: every `RadioButton` with exactly the same `GroupName` (ordinal, case-sensitive comparison) under the same visual root, which is the topmost `VisualParent` ancestor. Radio buttons with `GroupName = "Size"` in two different panels of one window form one group. Radio buttons with the same name in two different `UIRoot` trees do not affect each other.
- With an empty `GroupName` (the default): the radio buttons with an empty `GroupName` among the `LogicalChildren` of this radio button's `LogicalParent`. When it has no `LogicalParent`, the `VisualChildren` of its `VisualParent` are used. So two `StackPanel` parents each hold their own group, and a radio button that is the `Content` of a `ContentControl` is alone in its group.

The group is looked up each time a radio button becomes checked or receives an arrow key. A named group walks the whole visual tree under the root, so its cost grows with the number of elements in that tree.

### Arrow keys

When the radio button has keyboard focus, a `KeyDown` of `Up` or `Left` moves to the previous radio button of its group, and `Down` or `Right` moves to the next one, in visual tree order. The target receives keyboard focus and becomes checked, which unchecks the others. The move wraps around: `Down` on the last radio button of the group moves to the first. Radio buttons that cannot take focus, for example disabled ones, are skipped. The key is marked handled when the move happens. When no other radio button of the group can take focus, the key does nothing and stays unhandled.

This differs from WPF, where arrow keys move only focus between radio buttons and Space checks the focused one. A key that comes from a focused element inside the radio button's content is ignored, and modifier keys are not considered.

`IsCheckedProperty` is registered by `ToggleButton` with `UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsAspect | UiPropertyOptions.AffectsSemantics`, so changing `IsChecked` participates in render, input-visual, aspect, and semantics invalidation. `AspectState.Checked` is active while `IsChecked` is `true`.

In the semantics tree a `RadioButton` has role `SemanticsRole.RadioButton` and a `SemanticsProperty.IsChecked` entry (present also when `false`), produced by `ToggleButtonAutomationPeer`.

## Constructors

| Name | Description |
| --- | --- |
| `RadioButton()` | Initializes a new, unchecked `RadioButton` with an empty `GroupName`, its default component template, border brush, one-pixel border thickness, and `Left`/`Top` content alignment. |

## Template Parts

| Name | Type | Description |
| --- | --- | --- |
| `PART_CheckMark` | `Cerneala.UI.Elements.UIElement` | Optional element whose visibility reflects `IsChecked`. The default template uses a 6 x 6 black `Ellipse` centered in the ring. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `GroupNameProperty` | `UiProperty<string>` | Identifies the `GroupName` UI property. The default value is `string.Empty`; metadata options are `None`; `null` is rejected. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `GroupName` | `string` | Gets or sets the name of the group of radio buttons that are checked exclusively. An empty string means the group is the radio buttons that share this one's parent. |

## Methods

| Name | Description |
| --- | --- |
| `OnToggle()` | Protected override of `ToggleButton.OnToggle`. Sets `IsChecked` to `true`; a click never unchecks a radio button. |
| `OnChecked(RoutedEventArgs)` | Protected override of `ToggleButton.OnChecked`. Unchecks the other checked radio buttons of the group, then raises `Checked`. |

## Key Inherited Members

| Name | Declared By | Description |
| --- | --- | --- |
| `IsChecked` | `ToggleButton` | Gets or sets whether the radio button is checked. Defaults to `false`. |
| `IsCheckedProperty` | `ToggleButton` | Identifies the `IsChecked` UI property. `RadioButton.IsCheckedProperty` refers to this field. |
| `Checked` | `ToggleButton` | Raised each time `IsChecked` changes to `true`. |
| `Unchecked` | `ToggleButton` | Raised each time `IsChecked` changes to `false`, including when another radio button of the group is checked. |
| `Click` | `ButtonBase` | Raised on each click, also on a click that leaves `IsChecked` unchanged. |

## Property Information

| Item | Value |
| --- | --- |
| Identifier field | `GroupNameProperty` |
| Property type | `string` |
| Default value | `string.Empty` |
| Metadata/options | `None` |

## Applies to

`Cerneala.UI.Controls.RadioButton` in the `Cerneala` project.

## See also

- `Cerneala.UI.Controls.Primitives.ToggleButton`
- `Cerneala.UI.Controls.Button`
- `Cerneala.UI.Controls.Primitives.ButtonBase`
- `Cerneala.UI.Core.UiProperty<T>`
