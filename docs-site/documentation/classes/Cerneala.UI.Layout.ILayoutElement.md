# ILayoutElement Interface

## Definition
Namespace: `Cerneala.UI.Layout`

Assembly/Project: `Cerneala`

Source: `UI/Layout/ILayoutElement.cs`

Defines the two-pass layout contract: measure, then arrange.

```csharp
public interface ILayoutElement
```

## Examples

Read the layout results of an element through the interface:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

UIRoot root = new(200, 100);
UIElement box = new() { Width = 50, Height = 20 };
root.VisualChildren.Add(box);
root.ProcessFrame();

ILayoutElement layout = box;
Console.WriteLine($"{layout.DesiredSize.Width} {layout.ArrangedBounds.X} {layout.LayoutVersion}");
// 50 75 4

box.Width = 80;
root.ProcessFrame();
Console.WriteLine($"{layout.DesiredSize.Width} {layout.ArrangedBounds.X} {layout.LayoutVersion}");
// 80 60 5
```

The exact `LayoutVersion` numbers depend on how many layout changes the element went through; only the increase matters.

## Remarks

`UIElement` is the only implementation. Custom layout is written by deriving from `UIElement` (or `Panel`) and overriding `MeasureCore` and `ArrangeCore`, not by implementing this interface.

During a frame, the layout manager measures and arranges the root. Each parent then measures and arranges its own children from its `MeasureCore` and `ArrangeCore`; `StackPanel`, `Grid`, `Canvas`, `Panel`, `ContentControl`, and `Decorator` do this.

`DesiredSize` is stored at the end of `Measure` and read by parents while they measure and arrange. `ArrangedBounds` is stored at the end of `Arrange` and is the rectangle used for rendering. `LayoutVersion` increases when a property registered with `AffectsMeasure` or `AffectsArrange` changes, when a detached element is invalidated, and when the element enters or leaves `Visibility.Collapsed`. Layout caches compare it to decide whether a stored result is still valid.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `DesiredSize` | `LayoutSize` | Gets the size requested by the last `Measure`. |
| `ArrangedBounds` | `LayoutRect` | Gets the rectangle assigned by the last `Arrange`. |
| `LayoutVersion` | `int` | Gets a counter that increases when the element's layout inputs change. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Measure(MeasureContext context)` | `LayoutSize` | Computes and stores `DesiredSize` for the available size in `context`. |
| `Arrange(ArrangeContext context)` | `LayoutRect` | Places the element in the final rectangle of `context` and stores `ArrangedBounds`. |

## Applies to

`UIElement`.

## See also

- `Cerneala.UI.Layout.MeasureContext`
- `Cerneala.UI.Layout.ArrangeContext`
- `Cerneala.UI.Layout.LayoutManager`
- `Cerneala.UI.Elements.UIElement`
