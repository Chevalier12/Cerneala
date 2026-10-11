# Orientation Enum

## Definition
Namespace: `Cerneala.UI.Layout`

Assembly/Project: `Cerneala`

Source: `UI/Layout/Orientation.cs`

Specifies whether content is laid out along the vertical or the horizontal axis.

```csharp
public enum Orientation
```

## Examples

A stack panel with a 40 x 20 and a 60 x 30 child:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

UIRoot root = new(300, 300);
StackPanel panel = new()
{
    Orientation = Orientation.Horizontal,
    HorizontalAlignment = HorizontalAlignment.Left,
    VerticalAlignment = VerticalAlignment.Top
};
UIElement first = new() { Width = 40, Height = 20 };
UIElement second = new() { Width = 60, Height = 30 };
panel.VisualChildren.Add(first);
panel.VisualChildren.Add(second);
root.VisualChildren.Add(panel);
root.ProcessFrame();

Console.WriteLine($"{panel.DesiredSize.Width}x{panel.DesiredSize.Height} at {second.ArrangedBounds.X},{second.ArrangedBounds.Y}");
// 100x30 at 40,0

panel.Orientation = Orientation.Vertical;
root.ProcessFrame();
Console.WriteLine($"{panel.DesiredSize.Width}x{panel.DesiredSize.Height} at {second.ArrangedBounds.X},{second.ArrangedBounds.Y}");
// 60x50 at 0,20
```

## Remarks

`Cerneala.UI.Layout.Panels.StackPanel.Orientation` defaults to `Vertical` and invalidates measure when it changes:

| Value | Desired size | Children placed |
| --- | --- | --- |
| `Vertical` | Width = largest child width; height = sum of child heights. | Top to bottom, each with its desired height. |
| `Horizontal` | Width = sum of child widths; height = largest child height. | Left to right, each with its desired width. |

`StackPanel` checks only for `Vertical`, so a value outside the enum lays out like `Horizontal`. The property has no validator.

Other users and their defaults: `Track`, `Slider`, `TrackLayoutPanel`, and `ScrollBarLayoutPanel` default to `Horizontal`; `ScrollBar` defaults to `Vertical`. `Slider` and `ScrollBar` pass their orientation to their `Track`, and `ScrollViewer` sets the orientation of its two scroll bars. `Grid`, `Canvas`, `Panel`, and `VirtualizingStackPanel` do not use it.

In `.crn` markup the value is the member name, matched without regard to case, for example `Orientation="Horizontal"`.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Vertical` | `0` | Lays content out from top to bottom. |
| `Horizontal` | `1` | Lays content out from left to right. |

## Applies to

`StackPanel.Orientation`, `Track.Orientation`, `ScrollBar.Orientation`, `Slider.Orientation`, `TrackLayoutPanel.Orientation`, and `ScrollBarLayoutPanel.Orientation`.

## See also

- `Cerneala.UI.Layout.Panels.StackPanel`
- `Cerneala.UI.Controls.Slider`
- `Cerneala.UI.Controls.Primitives.ScrollBar`
