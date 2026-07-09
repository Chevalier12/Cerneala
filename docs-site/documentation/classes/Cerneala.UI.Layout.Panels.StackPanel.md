# StackPanel Class

## Definition
Namespace: `Cerneala.UI.Layout.Panels`

Assembly/Project: `Cerneala`

Source: `UI/Layout/Panels/StackPanel.cs`

Arranges visual children in a single vertical or horizontal line.

```csharp
public class StackPanel : Panel
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Panel` -> `StackPanel`

## Examples

Create a horizontal stack, add two measured children, and arrange them into the final panel bounds.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

StackPanel panel = new()
{
    Orientation = Orientation.Horizontal
};

FixedElement first = new(new LayoutSize(20, 10));
FixedElement second = new(new LayoutSize(30, 15));

panel.VisualChildren.Add(first);
panel.VisualChildren.Add(second);

LayoutSize desired = panel.Measure(new MeasureContext(new LayoutSize(200, 100)));
panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 200, 40)));

LayoutRect firstBounds = first.ArrangedBounds;   // new LayoutRect(0, 0, 20, 40)
LayoutRect secondBounds = second.ArrangedBounds; // new LayoutRect(20, 0, 30, 40)

internal sealed class FixedElement(LayoutSize size) : UIElement
{
    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        return size;
    }
}
```

## Remarks

`StackPanel` is a layout panel for one-dimensional child layout. It reads children from the inherited `VisualChildren` collection and processes them in collection order.

The default `Orientation` is `Orientation.Vertical`. In vertical orientation, visible children are measured with the available width from the parent and an infinite height. The panel desired width is the maximum child desired width, and the desired height is the sum of child desired heights. During arrange, each child receives the panel final width and its own desired height, with each child placed below the previous one.

In horizontal orientation, visible children are measured with an infinite width and the available height from the parent. The panel desired width is the sum of child desired widths, and the desired height is the maximum child desired height. During arrange, each child receives its own desired width and the panel final height, with each child placed after the previous one on the X axis.

Collapsed children are measured with `LayoutSize.Zero` and arranged at the final rectangle origin with zero width and height.

Changing `Orientation` uses `UiPropertyOptions.AffectsMeasure`, so orientation changes participate in layout invalidation through the UI property system.

## Constructors

| Name | Description |
| --- | --- |
| `StackPanel()` | Initializes a new stack panel. The constructor is implicit. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `OrientationProperty` | `UiProperty<Orientation>` | Identifies the `Orientation` UI property. The registered default value is `Orientation.Vertical`, with `UiPropertyOptions.AffectsMeasure`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Orientation` | `Orientation` | Gets or sets the axis used to stack children. Use `Orientation.Vertical` to stack top-to-bottom or `Orientation.Horizontal` to stack left-to-right. |

## Inherited Properties

| Name | Type | Description |
| --- | --- | --- |
| `VisualChildren` | `UIElementCollection` | Gets the visual child collection inherited from `UIElement`; `StackPanel` measures and arranges these children in order. |
| `DesiredSize` | `LayoutSize` | Gets the size produced by the last measure pass. |
| `ArrangedBounds` | `LayoutRect` | Gets the bounds produced by the last arrange pass. |

## Layout Behavior

| Orientation | Measure constraint for each visible child | Desired size rule | Arrange rule |
| --- | --- | --- | --- |
| `Orientation.Vertical` | Available width, infinite height | Width is the maximum child width; height is the sum of child heights. | Children keep their desired height and receive the panel final width. |
| `Orientation.Horizontal` | Infinite width, available height | Width is the sum of child widths; height is the maximum child height. | Children keep their desired width and receive the panel final height. |

## Applies to

`Cerneala` retained UI layout panels that need simple one-axis child layout.

## See also

- `Cerneala.UI.Layout.Panels.Panel`
- `Cerneala.UI.Layout.Orientation`
- `Cerneala.UI.Elements.UIElement`
