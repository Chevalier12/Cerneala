# TrackLayoutPanel Class

## Definition
Namespace: `Cerneala.UI.Controls.Primitives`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Primitives/TrackLayoutPanel.cs`

Provides the template layout surface used by `Track` to measure a thumb without taking ownership of its arranged position.

```csharp
public sealed class TrackLayoutPanel : Panel
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Panel` -> `TrackLayoutPanel`

## Examples

Use `TrackLayoutPanel` inside a custom `Track` component template:

```xml
<Border
    Background="$owner.Background"
    BorderBrush="$owner.BorderBrush"
    BorderThickness="$owner.BorderThickness">
    <TrackLayoutPanel Orientation="$owner.Orientation">
        <Thumb Name="PART_Thumb" />
    </TrackLayoutPanel>
</Border>
```

## Remarks

`TrackLayoutPanel` is intended for `Track` templates. It measures its visual children and reports an orientation-aware minimum desired size, but deliberately does not arrange them. The owning `Track` remains responsible for arranging `PART_Thumb` from the current range, value, viewport size, and orientation.

Using a normal panel or decorator directly around `PART_Thumb` can arrange the thumb to the full template bounds after `Track` computes its range position. Place the thumb inside `TrackLayoutPanel` to preserve single ownership of thumb geometry.

For horizontal orientation, desired width is at least `32` and desired height is at least `10`. For vertical orientation, desired width is at least `10` and desired height is at least `32`.

## Constructors

| Name | Description |
| --- | --- |
| `TrackLayoutPanel()` | Initializes a track template layout panel with horizontal orientation. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `OrientationProperty` | `UiProperty<Orientation>` | Identifies the `Orientation` UI property. The default is `Orientation.Horizontal`; metadata options are `AffectsMeasure` and `AffectsArrange`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Orientation` | `Orientation` | Gets or sets the orientation used to calculate the minimum desired size. |

## Layout Behavior

| Orientation | Desired size |
| --- | --- |
| `Orientation.Horizontal` | At least `32` wide and at least `10` high. |
| `Orientation.Vertical` | At least `10` wide and at least `32` high. |

During arrange, the panel returns its final rectangle without arranging visual children. The owning `Track` subsequently positions `PART_Thumb`.

## Applies to

Custom component templates for `Cerneala.UI.Controls.Primitives.Track`.

## See also

- `Cerneala.UI.Controls.Primitives.Track`
- `Cerneala.UI.Controls.Primitives.Thumb`
- `Cerneala.UI.Layout.Panels.Panel`
