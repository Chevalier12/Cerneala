# HitTestFilter Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/HitTestFilter.cs`

Provides an optional element predicate that controls which elements participate in hit testing.

```csharp
public sealed class HitTestFilter
```

Inheritance:
`Object` -> `HitTestFilter`

## Examples

Use `IncludeAll` when hit testing should consider every element.

```csharp
using Cerneala.UI.Input;

HitTestFilter filter = HitTestFilter.IncludeAll;
HitTestResult? result = inputCache.HitTest(root, x, y, filter);
```

Create a predicate when a hit test should ignore specific elements or whole subtrees.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

HitTestFilter filter = new(element =>
    element.IsHitTestVisible
        ? HitTestFilterBehavior.Include
        : HitTestFilterBehavior.ExcludeSubtree);
```

## Remarks

`HitTestFilter` wraps a `Func<UIElement, HitTestFilterBehavior>` predicate. If no predicate is supplied, `Evaluate` returns `HitTestFilterBehavior.Include`.

`Evaluate` throws when the element argument is `null`.

The shared `IncludeAll` instance is equivalent to a filter with no predicate.

## Constructors

| Name | Description |
| --- | --- |
| `HitTestFilter(Func<UIElement, HitTestFilterBehavior>?)` | Initializes a hit test filter with an optional predicate. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `IncludeAll` | `HitTestFilter` | Gets a filter that includes every element. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Evaluate(UIElement)` | `HitTestFilterBehavior` | Evaluates the filter behavior for an element. Throws if `element` is `null`. |

## Applies to

- `Cerneala.UI.Input.HitTestFilter`

## See also

- `Cerneala.UI.Input.HitTestFilterBehavior`
- `Cerneala.UI.Input.HitTestService`
- `Cerneala.UI.Input.HitTestResult`
