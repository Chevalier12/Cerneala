# Panel Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Panel.cs`

Provides the sealed controls-namespace `Panel` facade over the base layout panel.

```csharp
public sealed class Panel : Layout.Panels.Panel
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Cerneala.UI.Layout.Panels.Panel` -> `Panel`

## Examples

Measure and arrange visual children with the controls-facing panel:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

Panel panel = new();
FixedElement child = new(new LayoutSize(20, 10));

panel.VisualChildren.Add(child);

LayoutSize desired = panel.Measure(new MeasureContext(new LayoutSize(100, 100)));
panel.Arrange(new ArrangeContext(new LayoutRect(1, 2, 30, 40)));

// desired is new LayoutSize(20, 10)
// child.ArrangedBounds is new LayoutRect(1, 2, 30, 40)

internal sealed class FixedElement(LayoutSize size) : UIElement
{
    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        return size;
    }
}
```

## Remarks

`Cerneala.UI.Controls.Panel` declares no members of its own. It is a sealed wrapper around `Cerneala.UI.Layout.Panels.Panel`, making the default panel available from the controls namespace and for APIs that require a controls-facing `Panel`, such as `ItemsPanelTemplate.CreatePanel()` and `ItemsPresenter.PanelRoot`.

Layout behavior is inherited from `Cerneala.UI.Layout.Panels.Panel`. During measure, the panel measures each visible visual child with the incoming `MeasureContext` and reports the maximum child width and maximum child height. Collapsed children are measured with `LayoutSize.Zero` and do not contribute to the desired size.

During arrange, each visible visual child is arranged into the same final rectangle passed to the panel. Collapsed children are arranged at the final rectangle origin with zero width and zero height. The panel returns the final rectangle as its arranged bounds.

## Constructors

| Name | Description |
| --- | --- |
| `Panel()` | Initializes a new sealed controls panel. The constructor is implicit and uses the inherited `Cerneala.UI.Layout.Panels.Panel` initialization path. |

## Declared Members

| Member Type | Members |
| --- | --- |
| Fields | None. |
| Properties | None. |
| Methods | None. |
| Events | None. |

## Relevant Inherited Properties

| Name | Type | Declared By | Description |
| --- | --- | --- | --- |
| `VisualChildren` | `UIElementCollection` | `UIElement` | Gets the visual child collection measured and arranged by the inherited panel layout implementation. |
| `DesiredSize` | `LayoutSize` | `UIElement` | Gets the size produced by the last measure pass. For this panel, it is the maximum desired width and height of visible children. |
| `ArrangedBounds` | `LayoutRect` | `UIElement` | Gets the bounds produced by the last arrange pass. |
| `Visibility` | `Visibility` | `UIElement` | Controls layout participation; collapsed children are measured and arranged with zero size by the panel. |

## Relevant Inherited Methods

| Name | Return Type | Declared By | Description |
| --- | --- | --- | --- |
| `Measure(MeasureContext context)` | `LayoutSize` | `UIElement` | Runs the measure pass and returns `DesiredSize`; the inherited panel core measures visual children. |
| `Arrange(ArrangeContext context)` | `LayoutRect` | `UIElement` | Runs the arrange pass and returns `ArrangedBounds`; the inherited panel core arranges visual children into the final rectangle. |
| `Render(RenderContext context)` | `void` | `UIElement` | Runs the render pass. `Panel` does not add its own rendering behavior. |

## Layout Behavior

| Scenario | Result |
| --- | --- |
| No visual children | Measures to `LayoutSize.Zero`. |
| One or more visible visual children | Measures to the maximum child width and maximum child height. |
| Visible child during arrange | Receives the panel final rectangle. |
| Collapsed child during measure | Receives `LayoutSize.Zero` and does not affect the panel desired size. |
| Collapsed child during arrange | Receives a zero-size rectangle at the panel final rectangle origin. |

## Applies To

Cerneala retained UI controls and item presentation paths that need the default controls-facing panel.

## See Also

- `Cerneala.UI.Layout.Panels.Panel`
- `Cerneala.UI.Controls.Items.ItemsPanelTemplate`
- `Cerneala.UI.Controls.ItemsPresenter`
- `Cerneala.UI.Elements.UIElement`
