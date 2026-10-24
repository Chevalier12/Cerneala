# HitTestResult Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/HitTestResult.cs`

Stores the element and local coordinates produced by a retained UI hit test.

```csharp
public sealed class HitTestResult
```

Inheritance:
`Object` -> `HitTestResult`

## Examples

Use a hit test result to route input to the hit element and to preserve the local pointer coordinates.

```csharp
using Cerneala.UI.Input;

HitTestResult? result = inputCache.HitTest(root, pointerX, pointerY);
if (result is not null)
{
    RoutePointerEvent(result.Element, result.X, result.Y);
}
```

## Remarks

`HitTestResult` is an immutable result object. `Element` stores the matched `UIElement`, which is also the key the input route map and `UiInputTree` use, and `X` and `Y` store the hit coordinates associated with the result.

The constructor throws when `element` is `null`.

## Constructors

| Name | Description |
| --- | --- |
| `HitTestResult(UIElement, float, float)` | Initializes a hit test result for an element and coordinates. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Element` | `UIElement` | Gets the element matched by the hit test. |
| `X` | `float` | Gets the hit X coordinate. |
| `Y` | `float` | Gets the hit Y coordinate. |

## Applies to

- `Cerneala.UI.Input.HitTestResult`

## See also

- `Cerneala.UI.Input.HitTestService`
- `Cerneala.UI.Input.HitTestFilter`
- `Cerneala.UI.Input.ElementInputRouteMap`
