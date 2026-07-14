# ScrollBar Class

## Definition

Namespace: `Cerneala.UI.Controls.Primitives`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Primitives/ScrollBar.cs`

Represents a retained scroll bar control that synchronizes inherited range values with the active template track and exposes repeating direction buttons in its default template.

```csharp
[TemplatePart("PART_Track", typeof(Track))]
[TemplatePart("PART_DecreaseButton", typeof(RepeatButton))]
[TemplatePart("PART_IncreaseButton", typeof(RepeatButton))]
public class ScrollBar : RangeBase
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `RangeBase` -> `ScrollBar`

## Examples

Create a horizontal scroll bar with a viewport-sized thumb:

```csharp
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Layout.Panels;

ScrollBar scrollBar = new()
{
    Orientation = Orientation.Horizontal,
    Minimum = 0,
    Maximum = 100,
    Value = 25,
    ViewportSize = 20
};

float current = scrollBar.Track.Value; // 25
```

Create a vertical scroll bar and update its value programmatically:

```csharp
using Cerneala.UI.Controls.Primitives;

ScrollBar scrollBar = new()
{
    Minimum = 0,
    Maximum = 500,
    ViewportSize = 120
};

scrollBar.Value = 80;
float trackValue = scrollBar.Track.Value; // 80
```

## Remarks

`ScrollBar` derives from `RangeBase`, so `Minimum`, `Maximum`, `Value`, `SmallChange`, and `LargeChange` use the inherited range validation and coercion rules. `Value` is coerced into the active `Minimum..Maximum` range, and non-finite range values are rejected by the inherited UI properties.

The default template requires `PART_Track` and supplies optional-contract parts `PART_DecreaseButton` and `PART_IncreaseButton` as `RepeatButton` instances. The buttons sit at the ends of an orientation-aware panel and use `<`, `>`, `^`, and `v` glyphs. Custom templates may omit both buttons, but must register a valid `Track`.

The constructor applies the default template and sets default visuals: `Background` is `new SolidColorBrush(new Color(235, 235, 235))`, `BorderBrush` is `new SolidColorBrush(new Color(130, 130, 130))`, and `BorderThickness` is `new Thickness(1)`.

`Orientation` defaults to `Orientation.Vertical`. The default template uses a fixed `12` layout length for each direction button and gives the remaining axis length to the track. Changing orientation rearranges the existing template instance rather than recreating it.

The scroll bar keeps its active track synchronized with `Minimum`, `Maximum`, `Value`, `SmallChange`, `LargeChange`, `ViewportSize`, and `Orientation`. When the track value changes, the scroll bar copies the new value back into `Value`. Direction-button activation applies `SmallChange`; track regions apply `LargeChange`; thumb dragging maps pointer travel to the range.

`ScrollEventType.SmallDecrement`, `SmallIncrement`, `LargeDecrement`, `LargeIncrement`, and `ThumbTrack` identify those interaction paths. The current control does not raise `EndScroll` on release because no release-level scroll consumer exists; the event is not emitted as decorative noise.

`Track` always returns `PART_Track` from the active template. Template replacement detaches the old track and button handlers before connecting the new parts. Assigning `null` to `ComponentTemplate` clears the local override and restores the default template.

The default template binds `Background`, `BorderBrush`, and `BorderThickness` to its border root.

## Constructors

| Name | Description |
| --- | --- |
| `ScrollBar()` | Initializes the default visuals and viewport size, then applies the default component template with direction buttons and `PART_Track`. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `OrientationProperty` | `UiProperty<Orientation>` | Identifies the `Orientation` UI property. The default value is `Orientation.Vertical`; metadata options are `AffectsMeasure`, `AffectsArrange`, and `AffectsRender`. |
| `ViewportSizeProperty` | `UiProperty<float>` | Identifies the `ViewportSize` UI property. The default value is `0`; values must be finite and non-negative. Metadata options are `AffectsArrange` and `AffectsRender`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Orientation` | `Orientation` | Gets or sets whether the active template parts are laid out vertically or horizontally. |
| `ViewportSize` | `float` | Gets or sets the visible viewport size forwarded to the active `Track.ViewportSize`. |
| `Track` | `Track` | Gets `PART_Track` from the active component template. The getter applies the template before returning. |

## Methods

`ScrollBar` does not declare public methods beyond inherited members.

## Events

| Name | Event Type | Description |
| --- | --- | --- |
| `Scroll` | `EventHandler<ScrollEventArgs>` | Routed bubble event raised for actual small, large, and thumb-drag value changes. `ScrollEventType` identifies the interaction source. Programmatic synchronization and clamped no-op changes do not produce false interaction events. |

## Relevant Inherited Fields

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `MinimumProperty` | `UiProperty<float>` | `RangeBase` | Identifies the `Minimum` property. Default is `0`; values must be finite. |
| `MaximumProperty` | `UiProperty<float>` | `RangeBase` | Identifies the `Maximum` property. Default is `1`; values must be finite. |
| `ValueProperty` | `UiProperty<float>` | `RangeBase` | Identifies the `Value` property. Default is `0`; values must be finite and are coerced to the active range. |
| `SmallChangeProperty` | `UiProperty<float>` | `RangeBase` | Identifies the `SmallChange` property. Default is `0.1`; values must be finite and non-negative. |
| `LargeChangeProperty` | `UiProperty<float>` | `RangeBase` | Identifies the `LargeChange` property. Default is `1`; values must be finite and non-negative. |
| `ComponentTemplateProperty` | `UiProperty<ComponentTemplate?>` | `Control` | Identifies the control template property. A local `null` restores the default scroll bar template. |

## Relevant Inherited Properties

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `Minimum` | `float` | `RangeBase` | Gets or sets the lower bound for `Value`. If set above `Maximum`, `Maximum` is raised to match it. |
| `Maximum` | `float` | `RangeBase` | Gets or sets the upper bound for `Value`. If set below `Minimum`, `Minimum` is lowered to match it. |
| `Value` | `float` | `RangeBase` | Gets or sets the current scroll bar value. The value is coerced into the current range and synchronized to `Track.Value`. |
| `SmallChange` | `float` | `RangeBase` | Gets or sets the small range increment synchronized to `Track.SmallChange`. |
| `LargeChange` | `float` | `RangeBase` | Gets or sets the large range increment synchronized to `Track.LargeChange`. |
| `Background` | `Brush?` | `Control` | Gets or sets the brush bound to the default template root. The constructor sets a solid brush with color `Color(235, 235, 235)`. |
| `BorderBrush` | `Brush?` | `Control` | Gets or sets the brush bound to the default template root border. The constructor sets a solid brush with color `Color(130, 130, 130)`. |
| `BorderThickness` | `Thickness` | `Control` | Gets or sets the thickness bound to the default template root border. The constructor sets it to `new Thickness(1)`. |
| `ComponentTemplate` | `ComponentTemplate?` | `Control` | Gets or sets the control template. Valid templates require `PART_Track`; assigning `null` restores the default template. |

## Property Information

| Property | Identifier field | Default value | Metadata/options |
| --- | --- | --- | --- |
| `Orientation` | `OrientationProperty` | `Orientation.Vertical` | `UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender` |
| `ViewportSize` | `ViewportSizeProperty` | `0` | `UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender`; value must be finite and non-negative. |

## Layout Behavior

| Condition | Result |
| --- | --- |
| Default template and `Orientation.Horizontal` | Arranges decrease button, flexible track, and increase button from left to right. |
| Default template and `Orientation.Vertical` | Arranges decrease button, flexible track, and increase button from top to bottom. |
| Axis length is below `24` | Splits the available length between the two buttons and gives the track zero remaining length. |
| Custom template without buttons | Measures and arranges normally through the template root; drag and page scrolling remain available through `PART_Track`. |

## Track Synchronization

| ScrollBar member | Synchronized track member |
| --- | --- |
| `Minimum` | `Track.Minimum` |
| `Maximum` | `Track.Maximum` |
| `Value` | `Track.Value` |
| `SmallChange` | `Track.SmallChange` |
| `LargeChange` | `Track.LargeChange` |
| `ViewportSize` | `Track.ViewportSize` |
| `Orientation` | `Track.Orientation` |

## Applies to

`Cerneala.UI.Controls.Primitives.ScrollBar` in the `Cerneala` project.

## See also

- `Cerneala.UI.Controls.Primitives.RangeBase`
- `Cerneala.UI.Controls.Primitives.Track`
- `Cerneala.UI.Controls.Primitives.Thumb`
- `Cerneala.UI.Controls.ScrollViewer`
- `Cerneala.UI.Controls.Control`
