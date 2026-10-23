# StretchDirection Enum

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Image.cs`

Limits the direction in which an `Image` scales its picture.

```csharp
public enum StretchDirection { UpOnly, DownOnly, Both }
```

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;

// icon is a 50 x 50 IDrawImage.
Image image = new()
{
    Source = icon,
    Stretch = DrawBrushStretch.Uniform,
    StretchDirection = StretchDirection.DownOnly
};

LayoutSize desired = image.Measure(new MeasureContext(new LayoutSize(400, 100)));
// desired is 50 x 50: Uniform would scale by 2, but DownOnly does not enlarge.
```

Markup:

```xml
<Image Stretch="Uniform" StretchDirection="DownOnly" />
```

## Remarks

`Image.Stretch` computes a horizontal and a vertical scale factor. `StretchDirection` then limits each factor separately: `UpOnly` raises a factor below `1` to `1`, and `DownOnly` lowers a factor above `1` to `1`. The same limit applies when the image measures and when it renders. With `Stretch = None` both factors are already `1`, so the direction has no effect.

The default of `Image.StretchDirection` is `Both`.

## Fields

| Name | Value | Description |
| --- | --- | --- |
| `UpOnly` | `0` | The picture can only grow. A factor below `1` becomes `1`. |
| `DownOnly` | `1` | The picture can only shrink. A factor above `1` becomes `1`. |
| `Both` | `2` | The picture grows or shrinks as `Stretch` asks. |

## See also

- [Image](Cerneala.UI.Controls.Image.md)
- [DrawBrushStretch](Cerneala.Drawing.DrawBrushStretch.md)
