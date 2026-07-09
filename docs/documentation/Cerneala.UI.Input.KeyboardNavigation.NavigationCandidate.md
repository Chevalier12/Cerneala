# KeyboardNavigation.NavigationCandidate Struct

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/KeyboardNavigation.cs`

Stores a focusable tab-navigation candidate together with its tab index and visual route order.

```csharp
private readonly record struct NavigationCandidate(UIElement Element, int TabIndex, int VisualOrder);
```

Containing type:
`Cerneala.UI.Input.KeyboardNavigation`

## Examples

`NavigationCandidate` is an implementation detail of `KeyboardNavigation`. The class creates candidates while collecting valid tab stops from an `ElementInputRouteMap`.

```csharp
if (FocusPolicy.CanFocus(element, routeMap) && element.IsTabStop)
{
    candidates.Add(new NavigationCandidate(element, element.TabIndex, routeOrder));
}
```

## Remarks

`NavigationCandidate` is a private nested readonly record struct used only by `KeyboardNavigation`. It is not part of the public API surface and cannot be constructed by callers outside the containing class.

The `Element` value is the tab stop that may receive keyboard focus. `TabIndex` captures the element's `UIElement.TabIndex` at collection time. `VisualOrder` captures the element's position in `ElementInputRouteMap.ElementsInRouteOrder`.

`KeyboardNavigation` sorts candidates by `TabIndex` first and `VisualOrder` second. When the current focused element is not itself a valid tab stop, the navigation logic uses `VisualOrder` to find the nearest valid candidate before wrapping.

## Constructors

| Name | Description |
| --- | --- |
| `NavigationCandidate(UIElement Element, int TabIndex, int VisualOrder)` | Initializes the candidate record with the focus target, tab index, and visual route order. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Element` | `UIElement` | The focusable tab-stop element represented by the candidate. |
| `TabIndex` | `int` | The candidate's tab order priority copied from `UIElement.TabIndex`. |
| `VisualOrder` | `int` | The candidate's index in `ElementInputRouteMap.ElementsInRouteOrder`. |

## Applies to

Cerneala retained UI keyboard navigation internals.

## See also

- `Cerneala.UI.Input.KeyboardNavigation`
- `Cerneala.UI.Input.ElementInputRouteMap`
- `Cerneala.UI.Input.FocusPolicy`
