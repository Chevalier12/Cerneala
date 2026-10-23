# Panel Class

## Definition
Namespace: `Cerneala.UI.Layout.Panels`

Assembly/Project: `Cerneala`

Source: `UI/Layout/Panels/Panel.cs`

Provides the base layout panel that measures non-collapsed visual children to their maximum desired size and arranges non-collapsed children into the same final rectangle.

```csharp
public class Panel : UIElement
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Panel`

Derived:
`Canvas`, `Grid`, `StackPanel`, `VirtualizingStackPanel`, `Cerneala.UI.Controls.Panel`

## Examples

Add a child with `Children`, then measure and arrange the panel:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

Panel panel = new();
FixedElement child = new(new LayoutSize(20, 10));

panel.Children.Add(child);
// child.LogicalParent is panel
// child.VisualParent is panel

LayoutSize desired = panel.Measure(new MeasureContext(new LayoutSize(100, 100)));
LayoutRect arranged = panel.Arrange(new ArrangeContext(new LayoutRect(1, 2, 30, 40)));

// desired is new LayoutSize(20, 10)
// arranged is new LayoutRect(1, 2, 30, 40)
// child.ArrangedBounds is new LayoutRect(1, 2, 30, 40)

internal sealed class FixedElement(LayoutSize desiredSize) : UIElement
{
    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        return desiredSize;
    }
}
```

Draw a child that comes first in `Children` above a later child with `ZIndex`:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Media;

// Canvas and Panel are the Cerneala.UI.Controls types; SetZIndex is inherited from Cerneala.UI.Layout.Panels.Panel.
Canvas canvas = new();
Border front = new() { Width = 100, Height = 100, Background = new SolidColorBrush(Color.Green) };
Border back = new() { Width = 100, Height = 100, Background = new SolidColorBrush(Color.Red) };
canvas.Children.Add(front);
canvas.Children.Add(back);

Panel.SetZIndex(front, 1);

// Drawing order: back, then front. front covers back.
// A click on the overlapping area reaches front.
// Layout order is unchanged: canvas.Children[0] is still front.
```

The same property in markup:

```xml
<Canvas>
    <Border Width="100" Height="100" Panel.ZIndex="1" />
    <Border Width="100" Height="100" />
</Canvas>
```

## Remarks

`Panel` is the default base implementation for layout containers in `Cerneala.UI.Layout.Panels`. Application code adds and removes children with `Children`, like WPF `Panel.Children`. `panel.Children.Add(child)` makes the child a visual child of the panel, so the panel measures, arranges, and draws it, and a logical child of the panel, so resource lookup and logical-tree walks reach it. `panel.Children.Remove(child)` clears both parents.

The panel measures, arranges, and draws only `VisualChildren`. A child that is only a logical child would be ignored without any error, so `panel.LogicalChildren.Add(child)` throws `InvalidOperationException` when `child` is not already a visual child of the panel; the message names `Panel.Children.Add`. `panel.VisualChildren.Add(child)` still adds a visual-only child, which the panel lays out and draws.

During measure, the panel iterates over `VisualChildren`. Non-collapsed children are measured with the incoming `MeasureContext`; the panel reports the maximum measured child width and the maximum measured child height. If there are no non-collapsed children, the result is `LayoutSize.Zero`.

Collapsed children are still measured, but with `LayoutSize.Zero`, and they do not contribute to the panel desired size. During arrange, non-collapsed children receive the same final rectangle supplied to `ArrangeCore`. Collapsed children are arranged at the final rectangle origin with zero width and zero height.

`Panel` does not add rendering behavior of its own. Rendering remains the inherited `UIElement.Render(RenderContext)` path unless a derived panel or child element provides rendering.

### Z-order

`ZIndex` is an attached property, like WPF `Panel.ZIndex`. It is an `int` with the default value `0`, and it applies to the visual children of any `Panel`, including `Canvas`, `Grid`, `StackPanel`, and `VirtualizingStackPanel`. On an element whose visual parent is not a `Panel`, it has no effect.

The panel orders its visual children by ascending `ZIndex`. Children with the same `ZIndex` keep their `Children` order. A negative value is allowed and puts the child below the children with `0`. Example: `Children` is `[a, b, c]` and `Panel.SetZIndex(a, 1)` is called. Then:

- Drawing order is `b`, `c`, `a`, so `a` is drawn on top.
- Hit testing checks `a` first, so a click on the area that `a` and `c` both cover reaches `a`.
- Keyboard Tab order is `b`, `c`, `a`.
- The accessibility semantics tree lists the panel's children as `b`, `c`, `a`.
- Measure and arrange keep the `Children` order. In a `StackPanel`, `a` stays at the top.

Children that are leaving with a presence exit animation are drawn after all other children, whatever their `ZIndex`.

Changing `ZIndex` requests render, hit-test, and semantics invalidation (`UiPropertyOptions.AffectsRender | AffectsHitTest | AffectsSemantics`). It does not request measure or arrange. Adding, removing, or moving a child also updates the order.

In markup, `Panel.ZIndex="1"` sets the property, and `Panel.ZIndex="$DataContext.Layer"` binds it.

## Constructors

| Name | Description |
| --- | --- |
| `Panel()` | Initializes a new `Panel` with an empty `Children` collection. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `ZIndexProperty` | `UiProperty<int>` | Identifies the `ZIndex` attached property. Default value `0`. Options: `AffectsRender`, `AffectsHitTest`, `AffectsSemantics`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Children` | `PanelChildCollection` | Gets the children of the panel. `Add`, `Insert`, and `Remove` change the visual tree and the logical tree together. Enumeration, `Count`, and the indexer read `VisualChildren`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetZIndex(UIElement element)` | `int` | Gets the `ZIndex` of `element`. Returns `0` when it was never set. Throws `ArgumentNullException` when `element` is `null`. |
| `SetZIndex(UIElement element, int value)` | `void` | Sets the `ZIndex` of `element`. Throws `ArgumentNullException` when `element` is `null`. |

## Declared Public Members

| Member Type | Members |
| --- | --- |
| Fields | `ZIndexProperty` |
| Properties | `Children` |
| Methods | `GetZIndex`, `SetZIndex` |
| Events | None. |

## Protected Layout Overrides

| Name | Return Type | Description |
| --- | --- | --- |
| `MeasureCore(MeasureContext context)` | `LayoutSize` | Measures each visual child and returns the maximum desired width and height from non-collapsed children. |
| `ArrangeCore(ArrangeContext context)` | `LayoutRect` | Arranges each non-collapsed visual child into `context.FinalRect`, arranges collapsed children to a zero-size rectangle at the same origin, and returns `context.FinalRect`. |

## Relevant Inherited Members

| Name | Member Type | Declared By | Description |
| --- | --- | --- | --- |
| `VisualChildren` | Property | `UIElement` | Gets the visual child collection measured and arranged by the panel. |
| `LogicalChildren` | Property | `UIElement` | Gets the logical child collection. On a panel, inserting a child that is not already a visual child of the panel throws `InvalidOperationException`; use `Children`. |
| `DesiredSize` | Property | `UIElement` | Gets the size produced by the most recent measure pass. For this panel, that size is based on the maximum desired width and height of non-collapsed children. |
| `ArrangedBounds` | Property | `UIElement` | Gets the bounds produced by the most recent arrange pass. |
| `Visibility` | Property | `UIElement` | Controls layout participation. When a child is collapsed, the panel measures and arranges it with zero size. |
| `Measure(MeasureContext context)` | Method | `UIElement` | Runs the public measure pass and calls the panel's `MeasureCore` implementation when the element participates in layout. |
| `Arrange(ArrangeContext context)` | Method | `UIElement` | Runs the public arrange pass and calls the panel's `ArrangeCore` implementation when the element participates in layout. |

## Layout Behavior

| Scenario | Result |
| --- | --- |
| No visual children | Measures to `LayoutSize.Zero`. |
| One or more non-collapsed visual children | Measures to the maximum child width and maximum child height. |
| Non-collapsed child during arrange | Receives the panel `ArrangeCore` final rectangle. |
| Collapsed child during measure | Receives `LayoutSize.Zero` and does not affect the panel desired size. |
| Collapsed child during arrange | Receives a zero-size rectangle at the panel final rectangle origin. |
| Child with a `ZIndex` | Measured and arranged in `Children` order, like any other child. `ZIndex` changes only drawing, hit-testing, Tab, and semantics order. |

## Applies To

Cerneala retained UI layout containers that need a basic overlapping panel or a base class for specialized panels.

## See Also

- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Layout.Panels.PanelChildCollection`
- `Cerneala.UI.Elements.UIElementCollection`
- `Cerneala.UI.Layout.Panels.Canvas`
- `Cerneala.UI.Layout.Panels.Grid`
- `Cerneala.UI.Layout.Panels.StackPanel`
- `Cerneala.UI.Layout.Panels.VirtualizingStackPanel`
