# StackPanel Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/StackPanel.cs`

Provides the sealed controls-namespace `StackPanel` facade over the layout stack panel.

```csharp
public sealed class StackPanel : Layout.Panels.StackPanel
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Cerneala.UI.Layout.Panels.Panel` -> `Cerneala.UI.Layout.Panels.StackPanel` -> `StackPanel`

## Examples

Create a horizontal controls-facing stack panel and run layout:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

Cerneala.UI.Controls.StackPanel panel = new()
{
    Orientation = Orientation.Horizontal
};

FixedElement first = new(new LayoutSize(20, 10));
FixedElement second = new(new LayoutSize(30, 5));

panel.VisualChildren.Add(first);
panel.VisualChildren.Add(second);

LayoutSize desired = panel.Measure(new MeasureContext(new LayoutSize(100, 100)));
panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

// desired is new LayoutSize(50, 10)
// first.ArrangedBounds is new LayoutRect(0, 0, 20, 100)
// second.ArrangedBounds is new LayoutRect(20, 0, 30, 100)

internal sealed class FixedElement(LayoutSize size) : UIElement
{
    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        return size;
    }
}
```

## Remarks

`Cerneala.UI.Controls.StackPanel` declares no members of its own. It is a sealed wrapper around `Cerneala.UI.Layout.Panels.StackPanel`, making stack layout available from the controls namespace while preserving the inherited layout implementation.

The inherited stack panel arranges visual children in collection order. With the default `Orientation.Vertical`, measure uses the incoming available width and an infinite available height for each visible child; the desired width is the maximum child width and the desired height is the sum of child heights. With `Orientation.Horizontal`, measure uses an infinite available width and the incoming available height for each visible child; the desired width is the sum of child widths and the desired height is the maximum child height.

During arrange, vertical orientation gives each visible child the final rectangle width and its desired height, offset from top to bottom. Horizontal orientation gives each visible child its desired width and the final rectangle height, offset from left to right. Collapsed children are measured with `LayoutSize.Zero`, arranged to a zero-size rectangle at the final rectangle origin, and do not contribute to the stack size.

`OrientationProperty` is registered by the inherited layout stack panel with default value `Orientation.Vertical` and `UiPropertyOptions.AffectsMeasure`, so changing `Orientation` participates in layout invalidation through the UI property system.

## Constructors

| Name | Description |
| --- | --- |
| `StackPanel()` | Initializes a new sealed controls stack panel. The constructor is implicit and uses the inherited `Cerneala.UI.Layout.Panels.StackPanel` initialization path. |

## Declared Members

| Member Type | Members |
| --- | --- |
| Fields | None. |
| Properties | None. |
| Methods | None. |
| Events | None. |

## Relevant Inherited Fields

| Name | Type | Declared By | Description |
| --- | --- | --- | --- |
| `OrientationProperty` | `UiProperty<Orientation>` | `Cerneala.UI.Layout.Panels.StackPanel` | Identifies the `Orientation` UI property. Its default value is `Orientation.Vertical`, and its metadata affects measure. |

## Relevant Inherited Properties

| Name | Type | Declared By | Description |
| --- | --- | --- | --- |
| `Orientation` | `Orientation` | `Cerneala.UI.Layout.Panels.StackPanel` | Gets or sets whether children are stacked vertically or horizontally. |
| `VisualChildren` | `UIElementCollection` | `UIElement` | Gets the visual child collection measured and arranged by the inherited stack layout implementation. |
| `DesiredSize` | `LayoutSize` | `UIElement` | Gets the size produced by the last measure pass. |
| `ArrangedBounds` | `LayoutRect` | `UIElement` | Gets the bounds produced by the last arrange pass. |
| `Visibility` | `Visibility` | `UIElement` | Controls layout participation when this element is used as a child of another element. |

## Relevant Inherited Methods

| Name | Return Type | Declared By | Description |
| --- | --- | --- | --- |
| `Measure(MeasureContext context)` | `LayoutSize` | `UIElement` | Runs the measure pass and returns `DesiredSize`; the inherited stack panel core measures children according to `Orientation`. |
| `Arrange(ArrangeContext context)` | `LayoutRect` | `UIElement` | Runs the arrange pass and returns `ArrangedBounds`; the inherited stack panel core arranges children in visual order. |
| `GetValue<T>(UiProperty<T> property)` | `T` | `UiObject` | Reads UI property values such as `Orientation`. |
| `SetValue<T>(UiProperty<T> property, T value)` | `T` | `UiObject` | Writes UI property values such as `Orientation` and returns the previous value. |
| `Render(RenderContext context)` | `void` | `UIElement` | Runs the render pass. `StackPanel` does not add its own rendering behavior. |

## Layout Behavior

| Scenario | Result |
| --- | --- |
| Default orientation | `Orientation.Vertical`. |
| Vertical measure | Child available size is `(available width, infinity)`; desired size is `(max child width, sum child heights)`. |
| Horizontal measure | Child available size is `(infinity, available height)`; desired size is `(sum child widths, max child height)`. |
| Vertical arrange | Children receive the final width, their desired height, and increasing Y offsets. |
| Horizontal arrange | Children receive their desired width, the final height, and increasing X offsets. |
| Collapsed child | Measured with `LayoutSize.Zero`, arranged to zero size at the final rectangle origin, and excluded from stack accumulation. |

## Applies To

Cerneala retained UI controls and item presentation paths that need controls-namespace stack layout.

## See Also

- `Cerneala.UI.Layout.Panels.StackPanel`
- `Cerneala.UI.Layout.Panels.Orientation`
- `Cerneala.UI.Controls.Panel`
- `Cerneala.UI.Elements.UIElement`
