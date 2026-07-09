# LayoutBoundary Class

## Definition
Namespace: `Cerneala.UI.Layout`  
Assembly/Project: `Cerneala`  
Source: `UI/Layout/LayoutBoundary.cs`

Provides static helpers for reading and setting the layout-boundary flag on a `UIElement`.

```csharp
public static class LayoutBoundary
```

## Examples

Mark an element as a layout boundary so measure invalidation from its visual descendants stops at that element instead of continuing to higher ancestors.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

UIRoot root = new();
UIElement boundary = new();
UIElement child = new();

root.VisualChildren.Add(boundary);
boundary.VisualChildren.Add(child);

LayoutBoundary.SetIsBoundary(boundary, true);

child.Invalidate(InvalidationFlags.Measure, "measure");

bool isBoundary = LayoutBoundary.IsBoundary(boundary);
```

## Remarks

`LayoutBoundary` is a small API wrapper over `UIElement.IsLayoutBoundary`. It does not own separate boundary state; `IsBoundary` reads the element flag and `SetIsBoundary` writes it.

During measure invalidation propagation, ancestors are marked with measure and arrange work until a visual ancestor with `IsLayoutBoundary` set to `true` is reached. The boundary element itself is still marked; propagation stops after that element. `UIRoot` initializes itself as a layout boundary.

Both public methods throw `ArgumentNullException` when `element` is `null`.

## Methods

| Name | Description |
| --- | --- |
| `IsBoundary(UIElement element)` | Returns the current value of `element.IsLayoutBoundary`. |
| `SetIsBoundary(UIElement element, bool isBoundary)` | Sets `element.IsLayoutBoundary` to `isBoundary`. |

## Exceptions

| Method | Exception | Condition |
| --- | --- | --- |
| `IsBoundary(UIElement element)` | `ArgumentNullException` | `element` is `null`. |
| `SetIsBoundary(UIElement element, bool isBoundary)` | `ArgumentNullException` | `element` is `null`. |

## Applies to

Cerneala retained UI layout invalidation and visual-tree measure propagation.

## See also

- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Invalidation.DirtyPropagation`
