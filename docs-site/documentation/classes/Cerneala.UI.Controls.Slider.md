# Slider Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Slider.cs`

Represents a retained range selector that the user changes by dragging its thumb, by pressing its track, or with the keyboard.

```csharp
[TemplatePart("PART_Track", typeof(Track))]
[TemplatePart("PART_SelectionRange", typeof(UIElement))]
public class Slider : RangeBase
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `RangeBase` -> `Slider`

## Examples

Create a horizontal slider whose value can move from `0` to `100`:

```csharp
using Cerneala.UI.Controls;

Slider slider = new()
{
    Minimum = 0,
    Maximum = 100,
    Value = 25
};

slider.Value = 50;
float current = slider.Track.Value; // 50
```

Change the value with the keyboard:

```csharp
using Cerneala.UI.Controls;

Slider volume = new() { Minimum = 0, Maximum = 10, Value = 5, SmallChange = 1, LargeChange = 3 };
// The user presses Tab until the slider has the focus, then:
// Right    -> volume.Value == 6
// PageDown -> volume.Value == 3
// End      -> volume.Value == 10
// Home     -> volume.Value == 0
```

Show ticks every `2` units and snap the value to them:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;

Slider slider = new()
{
    Minimum = 0,
    Maximum = 10,
    TickFrequency = 2,
    TickPlacement = TickPlacement.BottomRight,
    IsSnapToTickEnabled = true
};
// Ticks at 0, 2, 4, 6, 8 and 10 are drawn below the track.
// Dragging the thumb to the point for 4.7 gives slider.Value == 4; to the point for 5.2 gives 6.
```

Create a vertical slider, a reversed slider, a slider whose track press jumps to the pointer, and a slider with a value tooltip and a selection range:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Layout;

Slider vertical = new() { Orientation = Orientation.Vertical, Minimum = 0, Maximum = 100, Value = 0 };
// The thumb is at the bottom; Up raises the value.

Slider reversed = new() { Minimum = 0, Maximum = 10, Value = 5, SmallChange = 1, IsDirectionReversed = true };
// Minimum is at the right; Right lowers the value to 4.

Slider jump = new() { Minimum = 0, Maximum = 10, IsMoveToPointEnabled = true };
// A press on the track beside the thumb moves the value to the pressed point.

Slider range = new()
{
    Minimum = 0,
    Maximum = 10,
    AutoToolTipPlacement = AutoToolTipPlacement.TopLeft,
    AutoToolTipPrecision = 1,
    IsSelectionRangeEnabled = true,
    SelectionStart = 2,
    SelectionEnd = 6
};
// While the thumb is dragged, a ToolTip above it shows the value, for example 4.7.
// A blue band covers the track from 2 to 6.
```

## Remarks

`Slider` derives from `RangeBase`, so `Minimum`, `Maximum`, `Value`, `SmallChange`, and `LargeChange` use the same validation and range coercion as other range controls. `Value` is coerced into the active `Minimum..Maximum` range, and non-finite range values are rejected by the inherited UI properties.

The constructor installs the default component template as a framework default at the `Default` value source. Custom templates must register a `Track` named `PART_Track`; `PART_SelectionRange` is optional. The `Track` property applies the current template and returns that active part. Replacing a template detaches the previous track event subscription before synchronizing the new part. Clearing a locally assigned template with `ClearValue(Slider.ComponentTemplateProperty)` restores the default template, because the framework default applies again.

### Focus and keyboard

As WPF's `Slider`, a `Slider` is focusable and a tab stop by default (`Focusable` and `IsTabStop` are `true`), so Tab reaches it. A left press anywhere inside it also focuses it, also a press on its `Thumb` or on its track, which handle the press themselves and cannot take the focus. As WPF's `Slider` does, it handles `MouseDown` with `handledEventsToo`, focuses itself unless the focus is already inside it, and marks that `MouseDown` handled when the focus moved. For example, a `Button` has the focus and the user presses the thumb of a `Slider`: after the press, `slider.IsKeyboardFocused` is `true` and the `Button` lost the focus. A slider with `Focusable = false` leaves the focus where it was.

A focused, enabled slider handles these keys when no Ctrl, Shift, or Alt key is down, as WPF's slider key gestures:

| Key | Result |
| --- | --- |
| Right, Up | Raises `Value` by `SmallChange`; lowers it when `IsDirectionReversed` is `true`. |
| Left, Down | Lowers `Value` by `SmallChange`; raises it when `IsDirectionReversed` is `true`. |
| PageUp | Raises `Value` by `LargeChange`. |
| PageDown | Lowers `Value` by `LargeChange`. |
| Home | Sets `Value` to `Minimum`. |
| End | Sets `Value` to `Maximum`. |

The key is marked handled also when `Value` is already at the end of the range. With `IsSnapToTickEnabled`, the arrow keys and PageUp/PageDown move to the next tick (see "Ticks and snapping").

### Track presses, repeat, and drags

The default `Track` has two transparent `RepeatButton` parts, one on each side of the thumb. With the default `IsMoveToPointEnabled = false`, a left press on the track beside the thumb changes `Value` by `LargeChange` toward the press. When the press is held longer than `Delay` milliseconds, the change repeats every `Interval` milliseconds, until the button is released or the thumb reaches the pointer. For example, `new Slider { Minimum = 0, Maximum = 10, Value = 5, LargeChange = 1 }` and a press right of the thumb give `Value == 6` at once; holding it gives `7` after `500` ms and `8` after `100` ms more. The slider copies `Delay` and `Interval` to both repeat buttons.

With `IsMoveToPointEnabled = true`, a left press anywhere on the slider outside the thumb (on the track, or on a `TickBar` beside it) moves `Value` to the pressed point instead, snapped to a tick when `IsSnapToTickEnabled` is `true`, as WPF's `Slider.OnPreviewMouseLeftButtonDown`: the slider handles the left `PreviewMouseDown`, so the press neither pages nor repeats, and it starts no thumb drag. A press on the thumb still starts a drag. Hit testing and this check apply render transforms, so the press is compared with where the thumb is drawn. For example, the same slider on a 110-pixel track with a 10-pixel thumb and a press at x `85` gives `Value == 8`.

Dragging the thumb changes `Value` continuously. The slider computes the value from the value at the start of the drag and the total distance that the pointer moved, as WPF's slider does, so a snapped thumb still follows the pointer to the next tick.

### Direction

As WPF's `Slider`, `IsDirectionReversed = false` puts `Minimum` at the left end of a horizontal slider and at the bottom end of a vertical slider, so the value of a vertical slider grows upward. `IsDirectionReversed = true` puts `Minimum` at the right end and at the top end, swaps what the arrow keys do, and mirrors the ticks and the selection range. PageUp, PageDown, Home, and End are not swapped.

### Ticks and snapping

`TickPlacement` shows tick marks beside the track: `TopLeft` above a horizontal slider and left of a vertical one, `BottomRight` below and right, `Both` on both sides, `None` (the default) nowhere. The default template has a `TickBar` on each side, 4 pixels thick and 2 pixels away from the track; a side without ticks has its `TickBar` collapsed, so it takes no space. The ticks are drawn at `Minimum`, at `Maximum`, and at every `TickFrequency` from `Minimum`, or at the values of `Ticks` when it has at least one value. They use the slider's `Foreground` and line up with the thumb's center (see `TickBar`).

With `IsSnapToTickEnabled = true`, the slider snaps the value as WPF's `Slider.SnapToTick` and `Slider.MoveToNextTick` do:

- A thumb drag and a move-to-point press set `Value` to the nearest tick. `Minimum` and `Maximum` count as ticks. A value exactly halfway between two ticks goes to the greater one. With `TickFrequency = 2` on `0..10`, `4.7` becomes `4` and `5` becomes `6`.
- The arrow keys, PageUp, PageDown, and the track's repeat buttons move `Value` by the change and snap the result; when the snap would leave `Value` where it was, it moves to the next tick in that direction. With `TickFrequency = 2`, `SmallChange = 1`, and `Value = 4`, Right gives `6`.
- When `Ticks` has values, they replace `TickFrequency` for snapping too. `Minimum` and `Maximum` still count as ticks, also when `Ticks` does not list them: with `Ticks = [3, 7]` on `0..10` and `SmallChange = 1`, Right goes from `3` to `7` and from `7` to `10`, and Left goes from `3` to `0`. WPF's `Slider.MoveToNextTick` leaves `Value` at `7` and at `3` in this case.
- Home and End set `Minimum` and `Maximum`, and setting `Value` from code does not snap.

`Ticks` is an `IReadOnlyList<float>?`. Every value must be finite; the values can be in any order. The slider reads the list when it draws and snaps, and it does not observe changes inside a list that is already assigned: assign a new list to change the ticks.

### Selection range

As WPF's `Slider`, `IsSelectionRangeEnabled = true` shows the template's `PART_SelectionRange` between `SelectionStart` and `SelectionEnd`. In the default template it is a band filled with `new SolidColorBrush(new Color(0, 120, 215))` over the track bar and under the thumb. It takes no pointer input, so a press on it still reaches the track's repeat button. The band runs along the thumb center's travel: on a 110-pixel horizontal slider with `Minimum = 0`, `Maximum = 10`, and a 10-pixel thumb, `SelectionStart = 2` and `SelectionEnd = 6` put it at x `25` with width `40`. `IsSelectionRangeEnabled = false`, the default, collapses it.

`SelectionStart` is coerced into `Minimum..Maximum`, and `SelectionEnd` into `SelectionStart..Maximum`. Both are coerced again when `Minimum` or `Maximum` changes, and `SelectionEnd` also when `SelectionStart` changes. Non-finite values are rejected. The default template positions its own band; a custom template's `PART_SelectionRange` is shown and collapsed by the slider, but the template positions it.

### Value tooltip

With `AutoToolTipPlacement` other than `None`, a thumb drag opens a `ToolTip` that the slider hosts, and it shows `Value` with `AutoToolTipPrecision` decimals in the current culture's number format (the `"N"` format, as WPF). For example, with precision `1`, `4.7` in English, or `1,234.5` for `1234.5`. The text changes with each change of `Value` during the drag, the tooltip follows the thumb, and it closes when the drag ends. As WPF's placement callback, `TopLeft` centers it above a horizontal thumb and left of a vertical thumb, and `BottomRight` below and right. The tooltip stays inside the window's viewport. Keys and track presses do not open it.

### Synchronization

The slider keeps its track synchronized with its range, change increments, orientation, and direction, and copies `Delay` and `Interval` to the track's repeat buttons. When the track's value changes through the thumb, the repeat buttons, or the track's own methods, the slider decides the new `Value` (with snapping when it is enabled) and the track then shows it.

## Constructors

| Name | Description |
| --- | --- |
| `Slider()` | Initializes a focusable tab-stop `Slider` with its default component template, and registers the `MouseDown` handler (`handledEventsToo: true`) that focuses it on a left press inside it, the thumb drag handlers, and the `KeyDown` handler. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `OrientationProperty` | `UiProperty<Orientation>` | Identifies `Orientation`. Default `Orientation.Horizontal`; `AffectsMeasure`, `AffectsArrange`, `AffectsRender`. |
| `IsDirectionReversedProperty` | `UiProperty<bool>` | Identifies `IsDirectionReversed`. Default `false`; `AffectsArrange`, `AffectsRender`. |
| `IsMoveToPointEnabledProperty` | `UiProperty<bool>` | Identifies `IsMoveToPointEnabled`. Default `false`. |
| `DelayProperty` | `UiProperty<int>` | Identifies `Delay`. Default `500`; values must be `0` or greater. |
| `IntervalProperty` | `UiProperty<int>` | Identifies `Interval`. Default `100`; values must be greater than `0`. |
| `TickFrequencyProperty` | `UiProperty<float>` | Identifies `TickFrequency`. Default `1`; values must be finite and non-negative. |
| `TicksProperty` | `UiProperty<IReadOnlyList<float>?>` | Identifies `Ticks`. Default `null`; every value must be finite. |
| `IsSnapToTickEnabledProperty` | `UiProperty<bool>` | Identifies `IsSnapToTickEnabled`. Default `false`. |
| `TickPlacementProperty` | `UiProperty<TickPlacement>` | Identifies `TickPlacement`. Default `TickPlacement.None`; values must be defined members; `AffectsMeasure`, `AffectsArrange`, `AffectsRender`. |
| `SelectionStartProperty` | `UiProperty<float>` | Identifies `SelectionStart`. Default `0`; values must be finite and are coerced into `Minimum..Maximum`. |
| `SelectionEndProperty` | `UiProperty<float>` | Identifies `SelectionEnd`. Default `0`; values must be finite and are coerced into `SelectionStart..Maximum`. |
| `IsSelectionRangeEnabledProperty` | `UiProperty<bool>` | Identifies `IsSelectionRangeEnabled`. Default `false`. |
| `AutoToolTipPlacementProperty` | `UiProperty<AutoToolTipPlacement>` | Identifies `AutoToolTipPlacement`. Default `AutoToolTipPlacement.None`; values must be defined members. |
| `AutoToolTipPrecisionProperty` | `UiProperty<int>` | Identifies `AutoToolTipPrecision`. Default `0`; values must be `0` to `99`, as in WPF. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Orientation` | `Orientation` | Gets or sets whether the slider is laid out horizontally or vertically. |
| `IsDirectionReversed` | `bool` | Gets or sets whether `Minimum` is at the right end or the top end (`true`) instead of the left end or the bottom end (`false`). Also swaps the arrow keys. |
| `IsMoveToPointEnabled` | `bool` | Gets or sets whether a press on the slider outside the thumb moves `Value` to the pressed point (`true`) instead of changing it by `LargeChange` and repeating while held (`false`). |
| `Delay` | `int` | Gets or sets the milliseconds that a press held on the track waits before it repeats. Markup constraint: non-negative. |
| `Interval` | `int` | Gets or sets the milliseconds between the repeats of a press held on the track. Markup constraint: positive. |
| `TickFrequency` | `float` | Gets or sets the distance between ticks, counted from `Minimum`, used when `Ticks` is `null` or empty. Markup constraint: non-negative. |
| `Ticks` | `IReadOnlyList<float>?` | Gets or sets the tick values. When it has values, they replace `TickFrequency` for drawing and snapping. |
| `IsSnapToTickEnabled` | `bool` | Gets or sets whether drags and track presses snap `Value` to the nearest tick and the keys and repeat buttons move it to the next tick. |
| `TickPlacement` | `TickPlacement` | Gets or sets where the ticks are drawn. |
| `SelectionStart` | `float` | Gets or sets the start of the selection range. |
| `SelectionEnd` | `float` | Gets or sets the end of the selection range. |
| `IsSelectionRangeEnabled` | `bool` | Gets or sets whether `PART_SelectionRange` is shown. |
| `AutoToolTipPlacement` | `AutoToolTipPlacement` | Gets or sets where a thumb drag shows the value tooltip; `None` shows none. |
| `AutoToolTipPrecision` | `int` | Gets or sets the number of decimals of the value in the tooltip, `0` to `99`; another value throws `ArgumentException`. Markup constraint: non-negative. |
| `Track` | `Track` | Applies the current template and gets its required `PART_Track` part. |

## Methods

`Slider` does not declare public methods beyond inherited members.

## Events

`Slider` does not declare public events. `RangeBase.ValueChanged` is raised for every change of `Value`.

## Relevant Inherited Fields

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `MinimumProperty` | `UiProperty<float>` | `RangeBase` | Identifies the `Minimum` property. Default is `0`; values must be finite. |
| `MaximumProperty` | `UiProperty<float>` | `RangeBase` | Identifies the `Maximum` property. Default is `1`; values must be finite. |
| `ValueProperty` | `UiProperty<float>` | `RangeBase` | Identifies the `Value` property. Default is `0`; values must be finite and are coerced to the active range. |
| `SmallChangeProperty` | `UiProperty<float>` | `RangeBase` | Identifies the `SmallChange` property. Default is `0.1`; values must be finite and non-negative. |
| `LargeChangeProperty` | `UiProperty<float>` | `RangeBase` | Identifies the `LargeChange` property. Default is `1`; values must be finite and non-negative. |
| `ComponentTemplateProperty` | `UiProperty<ComponentTemplate?>` | `Control` | Identifies the control template property. Slider templates must provide `PART_Track`. |

## Relevant Inherited Properties

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `Minimum` | `float` | `RangeBase` | Gets or sets the lower bound for `Value`. If set above `Maximum`, `Maximum` is raised to match it. |
| `Maximum` | `float` | `RangeBase` | Gets or sets the upper bound for `Value`. If set below `Minimum`, `Minimum` is lowered to match it. |
| `Value` | `float` | `RangeBase` | Gets or sets the current slider value. The value is coerced into the current range. |
| `SmallChange` | `float` | `RangeBase` | Gets or sets the change of the arrow keys. |
| `LargeChange` | `float` | `RangeBase` | Gets or sets the change of PageUp, PageDown, and a press on the track beside the thumb. |
| `Foreground` | `Brush?` | `Control` | Gets or sets the brush that the default template draws the ticks with. |
| `ComponentTemplate` | `ComponentTemplate?` | `Control` | Gets or sets the control template. A custom template must register a `Track` named `PART_Track`. |

## Template Parts

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `PART_Track` | `Track` | Yes | Owns thumb layout, the repeat buttons, and pointer input for the slider value. |
| `PART_SelectionRange` | `UIElement` | No | Shows the selection range. The slider sets its `Visibility` from `IsSelectionRangeEnabled`. |

## Layout Behavior

The default template is a panel that arranges, in drawing order: the top or left `TickBar`, the track bar (a `Border` with background `Color(225, 225, 225)`, border `Color(120, 120, 120)`, and thickness `1`, a positive side painted outside the bar, so a 110 x 20 track bar draws its border as a pen of thickness 1 at `(-0.5, -0.5, 111, 21)`), `PART_SelectionRange`, the `Track`, and the bottom or right `TickBar`. The `Track` draws no background and no border in this template, so its transparent repeat buttons and its thumb lie over the bar. Each visible `TickBar` adds 6 pixels (4 pixels of ticks and a 2-pixel gap) across the track: a horizontal slider with `TickPlacement.Both` and no set `Height` is 12 pixels taller than the same slider with `TickPlacement.None`.

The slider applies its component template before measure, synchronizes `PART_Track`, and uses the standard `Control` template layout path. It synchronizes the track again before arrange.

## Track Synchronization

| Slider member | Synchronized track member |
| --- | --- |
| `Minimum` | `Track.Minimum` |
| `Maximum` | `Track.Maximum` |
| `Value` | `Track.Value` |
| `SmallChange` | `Track.SmallChange` |
| `LargeChange` | `Track.LargeChange` |
| `Orientation` | `Track.Orientation` |
| `IsDirectionReversed` | `Track.IsDirectionReversed` |
| `Delay`, `Interval` | `Delay` and `Interval` of `Track.DecreaseRepeatButton` and `Track.IncreaseRepeatButton` |

## Applies to

`Cerneala.UI.Controls.Slider` in the `Cerneala` project.

## See also

- `Cerneala.UI.Controls.Primitives.RangeBase`
- `Cerneala.UI.Controls.Primitives.Track`
- `Cerneala.UI.Controls.Primitives.Thumb`
- `Cerneala.UI.Controls.Primitives.TickBar`
- `Cerneala.UI.Controls.Primitives.TickPlacement`
- `Cerneala.UI.Controls.Primitives.AutoToolTipPlacement`
- `Cerneala.UI.Controls.Primitives.RepeatButton`
- `Cerneala.UI.Controls.Control`
