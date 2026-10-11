# HorizontalAlignment Enum

## Definition
Namespace: `Cerneala.UI.Layout`

Assembly/Project: `Cerneala`

Source: `UI/Layout/Alignment.cs`

Specifies where an element is placed horizontally inside the space its parent arranges it in.

```csharp
public enum HorizontalAlignment
```

## Examples

Three elements in a 200 x 100 root. The root arranges each child in its full 200 x 100 slot:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

UIRoot root = new(200, 100);
UIElement right = new() { Width = 50, Height = 20, HorizontalAlignment = HorizontalAlignment.Right };
UIElement stretched = new() { Height = 20 };
UIElement sized = new() { Width = 50, Height = 20 };
root.VisualChildren.Add(right);
root.VisualChildren.Add(stretched);
root.VisualChildren.Add(sized);
root.ProcessFrame();

Console.WriteLine($"{right.ArrangedBounds.X} {right.ArrangedBounds.Width}");
Console.WriteLine($"{stretched.ArrangedBounds.X} {stretched.ArrangedBounds.Width}");
Console.WriteLine($"{sized.ArrangedBounds.X} {sized.ArrangedBounds.Width}");
// 150 50
// 0 200
// 75 50
```

`stretched` has no `Width`, so `Stretch` fills the slot. `sized` keeps the default `Stretch` but has `Width = 50`, so it is centered.

## Remarks

`UIElement.HorizontalAlignment` defaults to `Stretch`. Changing it invalidates arrange, not measure.

`UIElement` applies the alignment at the end of arrange, after it removes `Margin` from the slot:

| Value | Width | X position |
| --- | --- | --- |
| `Stretch`, `Width` not set | The slot width. | The slot's left edge. |
| `Stretch`, `Width` set | The smaller of the desired width and the slot width. | Centered in the slot. |
| `Left` | The smaller of the desired width and the slot width. | The slot's left edge. |
| `Center` | The smaller of the desired width and the slot width. | Centered in the slot. |
| `Right` | The smaller of the desired width and the slot width. | The slot's right edge minus the width. |

`Width` is "not set" when it is `NaN`, which is its default.

The property has no validator. A value outside the enum is accepted, and arrange treats it like `Left`.

In `.crn` markup the value is the member name, matched without regard to case: `HorizontalAlignment="Right"` and `HorizontalAlignment="right"` are the same. Numbers such as `"3"` are rejected with diagnostic `CERNEALAUI004`.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Stretch` | `0` | Fills the slot width when `Width` is not set; otherwise centers the element. |
| `Left` | `1` | Places the element at the left edge of the slot. |
| `Center` | `2` | Centers the element in the slot. |
| `Right` | `3` | Places the element at the right edge of the slot. |

## Applies to

`UIElement.HorizontalAlignment`.

## See also

- `Cerneala.UI.Layout.VerticalAlignment`
- `Cerneala.UI.Elements.UIElement`
