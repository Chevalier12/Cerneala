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

Measure and arrange a layout panel with one visual child:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

Panel panel = new();
FixedElement child = new(new LayoutSize(20, 10));

panel.VisualChildren.Add(child);

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

## Remarks

`Panel` is the default base implementation for layout containers in `Cerneala.UI.Layout.Panels`. It does not expose panel-specific public properties, fields, methods, or events; child management comes from `UIElement.VisualChildren`.

During measure, the panel iterates over `VisualChildren`. Non-collapsed children are measured with the incoming `MeasureContext`; the panel reports the maximum measured child width and the maximum measured child height. If there are no non-collapsed children, the result is `LayoutSize.Zero`.

Collapsed children are still measured, but with `LayoutSize.Zero`, and they do not contribute to the panel desired size. During arrange, non-collapsed children receive the same final rectangle supplied to `ArrangeCore`. Collapsed children are arranged at the final rectangle origin with zero width and zero height.

`Panel` does not add rendering behavior of its own. Rendering remains the inherited `UIElement.Render(RenderContext)` path unless a derived panel or child element provides rendering.

## Constructors

| Name | Description |
| --- | --- |
| `Panel()` | Initializes a new `Panel`. The constructor is implicit and uses the inherited `UIElement` initialization path. |

## Declared Public Members

| Member Type | Members |
| --- | --- |
| Fields | None. |
| Properties | None. |
| Methods | None. |
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

## Applies To

Cerneala retained UI layout containers that need a basic overlapping panel or a base class for specialized panels.

## See Also

- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Elements.UIElementCollection`
- `Cerneala.UI.Layout.Panels.Canvas`
- `Cerneala.UI.Layout.Panels.Grid`
- `Cerneala.UI.Layout.Panels.StackPanel`
- `Cerneala.UI.Layout.Panels.VirtualizingStackPanel`
