# Canvas Class

## Definition
Namespace: `Cerneala.UI.Layout.Panels`

Assembly/Project: `Cerneala`

Source: `UI/Layout/Panels/Canvas.cs`

Provides absolute child placement for retained UI elements by arranging each child at a stored `Left` and `Top` offset.

```csharp
public class Canvas : Panel
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Panel` -> `Canvas`

## Examples

Arrange a child at an absolute offset inside the canvas.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

Canvas canvas = new();
FixedElement child = new(new LayoutSize(20, 10));

Canvas.SetLeft(child, 5);
Canvas.SetTop(child, 7);
canvas.VisualChildren.Add(child);

canvas.Measure(new MeasureContext(new LayoutSize(100, 100)));
canvas.Arrange(new ArrangeContext(new LayoutRect(10, 20, 100, 100)));

LayoutRect childBounds = child.ArrangedBounds; // new LayoutRect(15, 27, 20, 10)

internal sealed class FixedElement(LayoutSize size) : UIElement
{
    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        return size;
    }
}
```

## Remarks

`Canvas` is a layout panel for absolute positioning. It stores per-child `Left` and `Top` coordinates for `UIElement` instances and applies those offsets during arrange. Unset coordinates read as `0`.

During measure, visible children are measured with `LayoutSize.Unconstrained`, collapsed children are measured with `LayoutSize.Zero`, and the canvas returns `LayoutSize.Zero` as its desired size. Child size and coordinate offsets do not expand the canvas desired size.

During arrange, each visible child is positioned at the canvas final rectangle origin plus its `Left` and `Top` values, using the child's `DesiredSize`. Collapsed children are arranged at the canvas final rectangle origin with zero size.

Changing `Left` or `Top` invalidates the parent canvas arrange pass only when the element's visual parent is a `Canvas` and the new coordinate differs from the stored value. Passing `null` to a coordinate getter or setter throws `ArgumentNullException`.

## Constructors

| Name | Description |
| --- | --- |
| `Canvas()` | Initializes a new canvas panel. The constructor is implicit. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetLeft(UIElement element)` | `float` | Gets the stored left offset for `element`, or `0` when no value is stored. |
| `GetTop(UIElement element)` | `float` | Gets the stored top offset for `element`, or `0` when no value is stored. |
| `SetLeft(UIElement element, float left)` | `void` | Sets the left offset for `element` and invalidates the parent canvas arrange pass when the value changes. |
| `SetTop(UIElement element, float top)` | `void` | Sets the top offset for `element` and invalidates the parent canvas arrange pass when the value changes. |

## Inherited Properties

| Name | Type | Description |
| --- | --- | --- |
| `VisualChildren` | `UIElementCollection` | Gets the visual child collection inherited from `UIElement`; `Canvas` measures and arranges these children. |
| `DesiredSize` | `LayoutSize` | Gets the size produced by the last measure pass. `Canvas` returns `LayoutSize.Zero` from its own measure implementation. |
| `ArrangedBounds` | `LayoutRect` | Gets the bounds produced by the last arrange pass. |

## Applies to

`Cerneala` retained UI layout panels that need absolute child positioning.

## See also

- `Cerneala.UI.Layout.Panels.Panel`
- `Cerneala.UI.Controls.Canvas`
- `Cerneala.UI.Elements.UIElement`
