# LayoutResult Struct

## Definition
Namespace: `Cerneala.UI.Layout`

Assembly/Project: `Cerneala`

Source: `UI/Layout/LayoutResult.cs`

Represents the outcome of a layout manager measure or arrange operation.

```csharp
public readonly record struct LayoutResult(
    LayoutSize DesiredSize,
    LayoutRect ArrangedBounds,
    bool UsedMeasureCache,
    bool UsedArrangeCache,
    bool BoundsChanged)
```

Inheritance:
`Object` -> `ValueType` -> `LayoutResult`

Implements:
`IEquatable<LayoutResult>`

## Examples

Read the result returned by a measure pass.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

UIRoot root = new(100, 100);
UIElement element = new();
root.VisualChildren.Add(element);

LayoutResult result = root.LayoutManager.Measure(
    element,
    new LayoutSize(100, 100));

LayoutSize desiredSize = result.DesiredSize;
bool reusedMeasure = result.UsedMeasureCache;
```

Read the result returned by an arrange pass.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

UIRoot root = new(100, 100);
UIElement element = new();
root.VisualChildren.Add(element);

root.LayoutManager.Measure(element, new LayoutSize(100, 100));

LayoutResult result = root.LayoutManager.Arrange(
    element,
    new LayoutRect(0, 0, 50, 25));

LayoutRect arrangedBounds = result.ArrangedBounds;
bool changedBounds = result.BoundsChanged;
```

## Remarks

`LayoutResult` is returned by `LayoutManager.Measure(UIElement, LayoutSize)` and `LayoutManager.Arrange(UIElement, LayoutRect)`. It packages the element layout state together with cache information from the operation that just ran.

For measure operations, `DesiredSize` is the element's measured desired size and `ArrangedBounds` is the element's current arranged bounds. `UsedMeasureCache` is `true` only when `LayoutManager.Measure` reuses the previous measure for the same available size and layout version. Measure results set `UsedArrangeCache` and `BoundsChanged` to `false`.

For arrange operations, `DesiredSize` is the element's current desired size and `ArrangedBounds` is the rectangle returned by the arrange pass. `UsedArrangeCache` is `true` only when `LayoutManager.Arrange` reuses the previous arrange for the same final rectangle and layout version. A non-cached arrange sets `BoundsChanged` according to whether the arranged bounds differ from the previous bounds. When arranged bounds change on an attached element, the layout manager invalidates render and hit-test work for that element.

`LayoutResult` is a `readonly record struct`, so its data is immutable after construction and equality is value-based across all five primary constructor components. The type does not validate or normalize the `LayoutSize`, `LayoutRect`, or Boolean values passed to its constructor.

## Constructors

| Name | Description |
| --- | --- |
| `LayoutResult(LayoutSize DesiredSize, LayoutRect ArrangedBounds, bool UsedMeasureCache, bool UsedArrangeCache, bool BoundsChanged)` | Initializes a layout result with measured size, arranged bounds, cache flags, and the arrange bounds-change flag. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `DesiredSize` | `LayoutSize` | Gets the desired size associated with the layout operation. |
| `ArrangedBounds` | `LayoutRect` | Gets the arranged bounds associated with the layout operation. |
| `UsedMeasureCache` | `bool` | Gets whether the measure operation was satisfied from the layout manager's measure cache. |
| `UsedArrangeCache` | `bool` | Gets whether the arrange operation was satisfied from the layout manager's arrange cache. |
| `BoundsChanged` | `bool` | Gets whether a non-cached arrange operation produced bounds different from the previous arranged bounds. |

## Methods

| Name | Description |
| --- | --- |
| `Deconstruct(out LayoutSize DesiredSize, out LayoutRect ArrangedBounds, out bool UsedMeasureCache, out bool UsedArrangeCache, out bool BoundsChanged)` | Deconstructs the result into its primary constructor components. |
| `Equals(LayoutResult other)` | Determines whether another `LayoutResult` has the same component values. |
| `GetHashCode()` | Returns a hash code based on the component values. |
| `ToString()` | Returns the compiler-generated record string representation. |

## Operators

| Name | Description |
| --- | --- |
| `operator ==(LayoutResult left, LayoutResult right)` | Determines whether two results have the same component values. |
| `operator !=(LayoutResult left, LayoutResult right)` | Determines whether two results have different component values. |

## Applies to

Cerneala UI layout measure and arrange pipeline.

## See also

- `Cerneala.UI.Layout.LayoutManager`
- `Cerneala.UI.Layout.LayoutSize`
- `Cerneala.UI.Layout.LayoutRect`
- `Cerneala.UI.Elements.UIElement`
