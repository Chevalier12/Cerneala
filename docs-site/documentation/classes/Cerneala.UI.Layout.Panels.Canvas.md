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

Place the same child in markup. `Canvas.Left` and `Canvas.Top` accept a number or a binding:

```xml
<Canvas Width="300" Height="300">
  <Border Width="10" Height="10" Canvas.Left="12" Canvas.Top="20" />
  <Border Width="10" Height="10" Canvas.Left="$DataContext.X" />
</Canvas>
```

After layout, the first `Border` is arranged at `(12, 20)` inside the canvas, the same as `Canvas.SetLeft(border, 12)` and `Canvas.SetTop(border, 20)`. The second `Border` follows the `X` value of its data context. A value that is not a number, for example `Canvas.Left="abc"`, reports `CERNEALAUI004`, and an unknown name, for example `Canvas.Foo="1"`, reports `CERNEALAUI003`, in the editor and in the build.

## Remarks

`Canvas` is a layout panel for absolute positioning. `Left` and `Top` are attached properties, like WPF `Canvas.Left` and `Canvas.Top`: `LeftProperty` and `TopProperty` are `UiProperty<float>` values with the default value `0`, stored on each child `UIElement`. The canvas applies them as offsets during arrange. `SetLeft`, `GetLeft`, `SetTop`, and `GetTop` call `SetValue` and `GetValue` with these properties.

During measure, visible children are measured with `LayoutSize.Unconstrained`, collapsed children are measured with `LayoutSize.Zero`, and the canvas returns `LayoutSize.Zero` as its desired size. Child size and coordinate offsets do not expand the canvas desired size.

During arrange, each visible child is positioned at the canvas final rectangle origin plus its `Left` and `Top` values, using the child's `DesiredSize`. Collapsed children are arranged at the canvas final rectangle origin with zero size.

When the effective value of `Left` or `Top` changes, from any value source (`SetLeft`, `SetValue`, or a markup binding), the child requests an arrange pass (`UiPropertyOptions.AffectsArrange`), and when the child's visual parent is a `Canvas`, that parent canvas is invalidated for arrange too, so the child moves on the next frame. Setting the value the property already has changes nothing. Passing `null` to a coordinate getter or setter throws `ArgumentNullException`.

In markup, `<Canvas>` is `Cerneala.UI.Controls.Canvas`, which derives from this class, so `Canvas.Left` and `Canvas.Top` resolve to `LeftProperty` and `TopProperty` here.

## Constructors

| Name | Description |
| --- | --- |
| `Canvas()` | Initializes a new canvas panel. The constructor is implicit. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `LeftProperty` | `UiProperty<float>` | Identifies the `Left` attached property. Default value `0`. Options: `AffectsArrange`. |
| `TopProperty` | `UiProperty<float>` | Identifies the `Top` attached property. Default value `0`. Options: `AffectsArrange`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetLeft(UIElement element)` | `float` | Gets the `Left` value of `element`. Returns `0` when it was never set. |
| `GetTop(UIElement element)` | `float` | Gets the `Top` value of `element`. Returns `0` when it was never set. |
| `SetLeft(UIElement element, float left)` | `void` | Sets the `Left` value of `element`; the parent canvas arranges again when the value changes. |
| `SetTop(UIElement element, float top)` | `void` | Sets the `Top` value of `element`; the parent canvas arranges again when the value changes. |

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
