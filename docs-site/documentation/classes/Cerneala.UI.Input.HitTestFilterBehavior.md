# HitTestFilterBehavior Enum

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/HitTestFilter.cs`

Specifies whether hit testing may return an element and whether it searches the element's children.

```csharp
public enum HitTestFilterBehavior
```

## Examples

An `outer` element contains an `inner` element. The root is 200 by 200, so the layout centers `outer` at 50..150 and `inner` at 75..125. The point (60, 60) is inside `outer` only, and the point (80, 80) is inside both.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIRoot root = new(200, 200);
UIElement outer = new() { Width = 100, Height = 100 };
UIElement inner = new() { Width = 50, Height = 50 };
root.VisualChildren.Add(outer);
outer.VisualChildren.Add(inner);
root.ProcessFrame();

HitTestFilter exclude = new(element =>
    ReferenceEquals(element, outer) ? HitTestFilterBehavior.Exclude : HitTestFilterBehavior.Include);
HitTestFilter excludeSubtree = new(element =>
    ReferenceEquals(element, outer) ? HitTestFilterBehavior.ExcludeSubtree : HitTestFilterBehavior.Include);

HitTestResult? a = root.InputCache.HitTest(root, 60, 60, exclude);        // a.Element is root
HitTestResult? b = root.InputCache.HitTest(root, 80, 80, exclude);        // b.Element is inner
HitTestResult? c = root.InputCache.HitTest(root, 80, 80, excludeSubtree); // c.Element is root
```

Without a filter, (60, 60) returns `outer` and (80, 80) returns `inner`.

## Remarks

A `HitTestFilter` calls its predicate for each element it visits and uses the returned value. A filter without a predicate, including `HitTestFilter.IncludeAll`, returns `Include` for every element. Pass a filter to `ElementInputCache.HitTest` or `HitTestService.HitTest`; a `null` filter means `IncludeAll`.

`Exclude` skips only the element itself. Its children are still tested, and the search continues to the element's parent when no child is hit. `ExcludeSubtree` skips the element and all of its descendants.

The same values apply to `RenderSurface2D` scene picking. An `ExcludeSubtree` scene node hides its whole scene subtree. When a collider child of a scene node is hit, picking normally returns the node; when the filter returns `Exclude` for the node, picking returns the collider itself instead. The child nodes of an `Exclude` node are still tested, but its own bounds are not. A collider is pickable only when the filter returns `Include` for it.

The framework itself creates only `Include`, as the default. `Exclude` and `ExcludeSubtree` come only from a predicate you supply.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Include` | `0` | The element can be returned, and its children are tested. |
| `Exclude` | `1` | The element is not returned, but its children are still tested. |
| `ExcludeSubtree` | `2` | Neither the element nor any of its descendants can be returned. |

## Applies to

Cerneala hit testing through `HitTestFilter`, `ElementInputCache`, and `HitTestService`.

## See also

- `Cerneala.UI.Input.HitTestFilter`
- `Cerneala.UI.Input.HitTestService`
- `Cerneala.UI.Input.ElementInputCache`
- `Cerneala.UI.Input.HitTestResult`
