# Canvas.CanvasPosition Class

## Definition

Namespace: `Cerneala.UI.Layout.Panels`

Assembly/Project: `Cerneala`

Source: `UI/Layout/Panels/Canvas.cs`

Stores the per-element canvas coordinates used internally by `Canvas`.

```csharp
private sealed class CanvasPosition
```

Containing type: `Cerneala.UI.Layout.Panels.Canvas`

Inheritance:
`object` -> `Canvas.CanvasPosition`

## Examples

`CanvasPosition` is a private implementation detail, so application code uses it indirectly through the static `Canvas` coordinate methods.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

Canvas canvas = new();
FixedElement child = new(new LayoutSize(24, 12));

Canvas.SetLeft(child, 16);
Canvas.SetTop(child, 8);
canvas.VisualChildren.Add(child);

float left = Canvas.GetLeft(child); // 16
float top = Canvas.GetTop(child);   // 8

internal sealed class FixedElement(LayoutSize size) : UIElement
{
    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        return size;
    }
}
```

## Remarks

`CanvasPosition` is a private nested storage type used by `Canvas` with a `ConditionalWeakTable<UIElement, CanvasPosition>`. A position object is created for an element when `SetLeft` or `SetTop` needs to store a coordinate for that element.

Both coordinates default to `0`. `Canvas.GetLeft` and `Canvas.GetTop` return `0` when no `CanvasPosition` is stored for the element.

Changing `Left` or `Top` through `Canvas.SetLeft` or `Canvas.SetTop` invalidates the arrange pass of the element's visual parent when that parent is a `Canvas`. Setting a coordinate to its current value returns without invalidating arrange.

The type is not part of the public surface of `Canvas`. Its public properties are only usable inside the containing implementation because the nested class itself is private.

## Constructors

| Name | Description |
| --- | --- |
| `CanvasPosition()` | Initializes a position record with default `Left` and `Top` values of `0`. The constructor is implicit and not directly accessible outside the private nested type. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Left` | `float` | Gets or sets the horizontal offset stored for a canvas child. |
| `Top` | `float` | Gets or sets the vertical offset stored for a canvas child. |

## Applies to

Internal layout storage for `Cerneala.UI.Layout.Panels.Canvas`.

## See also

- `Cerneala.UI.Layout.Panels.Canvas`
- `Cerneala.UI.Elements.UIElement`
