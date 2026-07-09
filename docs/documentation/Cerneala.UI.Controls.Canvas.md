# Canvas Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Canvas.cs`

Provides the sealed controls-namespace `Canvas` facade over the layout canvas panel.

```csharp
public sealed class Canvas : Layout.Panels.Canvas
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Cerneala.UI.Layout.Panels.Panel` -> `Cerneala.UI.Layout.Panels.Canvas` -> `Canvas`

## Examples

Position a child by setting its inherited canvas coordinates before arranging the canvas.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

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

`Cerneala.UI.Controls.Canvas` declares no members of its own. It is a sealed wrapper around `Cerneala.UI.Layout.Panels.Canvas`, making the canvas panel available from the controls namespace while preserving the base panel behavior.

The inherited canvas coordinate methods store per-child `Left` and `Top` values for `UIElement` instances. Unset coordinates read as `0`. Passing `null` to any coordinate getter or setter throws `ArgumentNullException`.

During measure, visible children are measured with `LayoutSize.Unconstrained`, collapsed children are measured with `LayoutSize.Zero`, and the canvas itself reports `LayoutSize.Zero` as its desired size. Child offsets do not expand the canvas desired size.

During arrange, each visible child is arranged at the canvas final rectangle origin plus its inherited `Left` and `Top` coordinates, using the child's desired size. Collapsed children are arranged at the canvas final rectangle origin with zero size.

Changing `Left` or `Top` for a child whose visual parent is a canvas invalidates the parent canvas arrange pass. Setting a coordinate to its current value returns without invalidating.

## Constructors

| Name | Description |
| --- | --- |
| `Canvas()` | Initializes a new sealed controls canvas. The constructor is implicit and uses the inherited `Cerneala.UI.Layout.Panels.Canvas` initialization path. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetLeft(UIElement element)` | `float` | Inherited from `Cerneala.UI.Layout.Panels.Canvas`. Gets the stored left offset for `element`, or `0` when no value is stored. |
| `GetTop(UIElement element)` | `float` | Inherited from `Cerneala.UI.Layout.Panels.Canvas`. Gets the stored top offset for `element`, or `0` when no value is stored. |
| `SetLeft(UIElement element, float left)` | `void` | Inherited from `Cerneala.UI.Layout.Panels.Canvas`. Sets the child left offset and invalidates the parent canvas arrange pass when the value changes. |
| `SetTop(UIElement element, float top)` | `void` | Inherited from `Cerneala.UI.Layout.Panels.Canvas`. Sets the child top offset and invalidates the parent canvas arrange pass when the value changes. |

## Inherited Properties

| Name | Type | Description |
| --- | --- | --- |
| `VisualChildren` | `UIElementCollection` | Gets the visual child collection inherited from `UIElement`; canvas layout iterates this collection during measure and arrange. |
| `DesiredSize` | `LayoutSize` | Gets the size produced by the last measure pass. For the canvas itself, the layout implementation returns `LayoutSize.Zero`. |
| `ArrangedBounds` | `LayoutRect` | Gets the bounds produced by the last arrange pass. |

## Applies to

`Cerneala` retained UI controls that need absolute child placement through the controls namespace.

## See also

- `Cerneala.UI.Layout.Panels.Canvas`
- `Cerneala.UI.Layout.Panels.Panel`
- `Cerneala.UI.Elements.UIElement`
