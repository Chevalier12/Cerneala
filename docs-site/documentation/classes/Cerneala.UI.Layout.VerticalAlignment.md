# VerticalAlignment Enum

## Definition
Namespace: `Cerneala.UI.Layout`

Assembly/Project: `Cerneala`

Source: `UI/Layout/Alignment.cs`

Specifies where an element is placed vertically inside the space its parent arranges it in.

```csharp
public enum VerticalAlignment
```

## Examples

Three elements in a 200 x 100 root. The root arranges each child in its full 200 x 100 slot:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

UIRoot root = new(200, 100);
UIElement top = new() { Width = 50, Height = 20, VerticalAlignment = VerticalAlignment.Top };
UIElement bottom = new() { Width = 50, Height = 20, VerticalAlignment = VerticalAlignment.Bottom };
UIElement sized = new() { Width = 50, Height = 20 };
root.VisualChildren.Add(top);
root.VisualChildren.Add(bottom);
root.VisualChildren.Add(sized);
root.ProcessFrame();

Console.WriteLine($"{top.ArrangedBounds.Y} {top.ArrangedBounds.Height}");
Console.WriteLine($"{bottom.ArrangedBounds.Y} {bottom.ArrangedBounds.Height}");
Console.WriteLine($"{sized.ArrangedBounds.Y} {sized.ArrangedBounds.Height}");
// 0 20
// 80 20
// 40 20
```

`sized` keeps the default `Stretch` but has `Height = 20`, so it is centered.

## Remarks

`UIElement.VerticalAlignment` defaults to `Stretch`. Changing it invalidates arrange, not measure.

`UIElement` applies the alignment at the end of arrange, after it removes `Margin` from the slot:

| Value | Height | Y position |
| --- | --- | --- |
| `Stretch`, `Height` not set | The slot height. | The slot's top edge. |
| `Stretch`, `Height` set | The smaller of the desired height and the slot height. | Centered in the slot. |
| `Top` | The smaller of the desired height and the slot height. | The slot's top edge. |
| `Center` | The smaller of the desired height and the slot height. | Centered in the slot. |
| `Bottom` | The smaller of the desired height and the slot height. | The slot's bottom edge minus the height. |

`Height` is "not set" when it is `NaN`, which is its default.

The property has no validator. A value outside the enum is accepted, and arrange treats it like `Top`.

In `.crn` markup the value is the member name, matched without regard to case: `VerticalAlignment="Bottom"` and `VerticalAlignment="bottom"` are the same. Numbers such as `"3"` are rejected with diagnostic `CERNEALAUI004`.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Stretch` | `0` | Fills the slot height when `Height` is not set; otherwise centers the element. |
| `Top` | `1` | Places the element at the top edge of the slot. |
| `Center` | `2` | Centers the element in the slot. |
| `Bottom` | `3` | Places the element at the bottom edge of the slot. |

## Applies to

`UIElement.VerticalAlignment`.

## See also

- `Cerneala.UI.Layout.HorizontalAlignment`
- `Cerneala.UI.Elements.UIElement`
