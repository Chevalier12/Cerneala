# KeyboardNavigation Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/KeyboardNavigation.cs`

Provides focus movement helpers for keyboard tab navigation over a prepared input route map.

```csharp
public sealed class KeyboardNavigation
```

Inheritance:
`object` -> `KeyboardNavigation`

## Examples

Move focus to the next valid tab stop from the current focused element.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIRoot root = new();
FocusManager focusManager = new();
ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);

KeyboardNavigation navigation = new();
bool moved = navigation.MoveNext(root, focusManager, routeMap, reverse: false);
```

Find the previous tab stop without changing focus.

```csharp
UIElement? previous = navigation.FindNext(
    root,
    focusManager.FocusedElement,
    routeMap,
    reverse: true);
```

## Remarks

`KeyboardNavigation` works over the caller-provided `ElementInputRouteMap`. Candidates are collected from `ElementInputRouteMap.ElementsInRouteOrder`, filtered through `FocusPolicy.CanFocus`, and must also have `UIElement.IsTabStop` set to `true`.

Candidate ordering is by `UIElement.TabIndex` first and route/visual order second. If the current element is already a candidate, moving forward or backward wraps around the candidate list. If the current element is focusable but is not a tab stop, navigation uses the element's visual route position to choose the nearest valid target and ignores that current element's `TabIndex`.

`Focus` and `MoveNext` delegate the actual focus change to `FocusManager.Focus`, so focus events and `IsKeyboardFocused` state updates are handled by `FocusManager`. `FindNext` only returns the target element; it does not change focus.

The `root` parameter is validated by `FindNext` and `MoveNext`, while candidate selection comes from the supplied `routeMap`. In the retained input pipeline, `ElementInputBridge` builds the route map before keyboard dispatch and passes that same map into default tab navigation.

## Constructors

| Name | Description |
| --- | --- |
| `KeyboardNavigation()` | Initializes a new `KeyboardNavigation` instance. |

## Methods

| Name | Description |
| --- | --- |
| `Focus(UIElement element, FocusManager focusManager, ElementInputRouteMap routeMap)` | Focuses `element` by calling `focusManager.Focus(element, routeMap)`. Returns `true` when focus changes successfully. |
| `FindNext(UIRoot root, UIElement? current, ElementInputRouteMap routeMap, bool reverse)` | Returns the next or previous valid tab stop from `current`, or the first/last candidate when `current` is `null` or is not present in the route map. Returns `null` when no valid candidates exist. |
| `MoveNext(UIRoot root, FocusManager focusManager, ElementInputRouteMap routeMap, bool reverse)` | Finds the next or previous valid tab stop from `focusManager.FocusedElement` and focuses it. Returns `false` when no target exists or focus cannot be changed. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Focus` | `ArgumentNullException` | `element`, `focusManager`, or `routeMap` is `null`. |
| `FindNext` | `ArgumentNullException` | `root` or `routeMap` is `null`. |
| `MoveNext` | `ArgumentNullException` | `focusManager` is `null`; `root` or `routeMap` is also validated through `FindNext`. |

## Applies to

Cerneala retained UI input and focus navigation.

## See also

- `FocusManager`
- `FocusPolicy`
- `ElementInputRouteMap`
- `KeyboardNavigationController`
