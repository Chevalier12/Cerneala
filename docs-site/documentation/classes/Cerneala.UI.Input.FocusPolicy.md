# FocusPolicy Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/FocusPolicy.cs`

Provides the shared predicate used to decide whether a UI element can receive keyboard focus.

```csharp
public static class FocusPolicy
```

Inheritance:
`object` -> `FocusPolicy`

## Examples

The following example checks whether an attached element is a valid keyboard focus target before requesting focus.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIRoot root = new();
UIElement editor = new()
{
    Focusable = true
};

root.VisualChildren.Add(editor);

ElementInputRouteMap routeMap = new ElementInputRouteBuilder().Build(root);

if (FocusPolicy.CanFocus(editor, routeMap))
{
    FocusManager focusManager = new();
    focusManager.Focus(editor, routeMap);
}
```

## Remarks

`FocusPolicy` centralizes the focus eligibility check used by `FocusManager`, pointer focus resolution in `ElementInputBridge`, and tab candidate collection in `KeyboardNavigation`.

`CanFocus` returns `true` only when all of the following are true:

| Requirement | Source condition |
| --- | --- |
| The element is not `null`. | `element is not null` |
| The element is attached to a `UIRoot`. | `UIElement.IsAttached` |
| The element explicitly accepts focus. | `UIElement.Focusable` |
| The element is enabled. | `UIElement.IsEnabled` |
| The element participates in input visibility. | `UIElementVisibility.ParticipatesInInput(element)` |
| The element is present in the supplied route map. | `ElementInputRouteMap.TryGetId(element, out _)` |

Input visibility requires the element not to be presence-exiting, `IsVisible` to be `true`, and `Visibility` to be `Visible`. Hidden, collapsed, runtime-invisible, detached, disabled, or non-focusable elements are rejected.

The supplied `ElementInputRouteMap` is part of the decision. A stale route map can reject an otherwise valid element if the element is not present in that map.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `CanFocus(UIElement? element, ElementInputRouteMap routeMap)` | `bool` | Returns `true` when `element` can receive keyboard focus under `routeMap`; returns `false` for `null` or ineligible elements. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `CanFocus` | `ArgumentNullException` | `routeMap` is `null`. |

## Applies to

Cerneala retained UI keyboard focus, pointer focus resolution, and tab navigation.

## See also

- `Cerneala.UI.Input.FocusManager`
- `Cerneala.UI.Input.KeyboardNavigation`
- `Cerneala.UI.Input.ElementInputRouteMap`
- `Cerneala.UI.Elements.UIElementVisibility`
