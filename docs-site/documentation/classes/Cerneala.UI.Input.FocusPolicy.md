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
| The element is present in the supplied route map. | `ElementInputRouteMap.Contains(element)` |
| The element is present in the current route map of its `UIRoot`. | `root.InputCache.EnsureCurrent(root).Contains(element)` |

Input visibility requires the element not to be presence-exiting with `PresenceOptions.ExcludeInputWhileExiting = true` (the default), `IsVisible` to be `true`, and `Visibility` to be `Visible`. Hidden, collapsed, runtime-invisible, detached, disabled, or non-focusable elements are rejected.

The supplied `ElementInputRouteMap` is part of the decision. A stale route map can reject an otherwise valid element if the element is not present in that map. A stale route map cannot accept an element that left the input route after the map was built: the element must also be in the current route map of its root, as WPF's `Keyboard.Focus` fails and WPF's `KeyboardNavigation` skips an element whose `IsVisible` is `false`. For example, a `KeyDown` handler collapses the panel that holds a button after the frame's route map was built. The map still contains the button, but `CanFocus(button, frameMap)` returns `false`, so a Tab of the same frame skips the button and moves the focus to the next visible tab stop, and `FocusManager.Focus(button, frameMap)` returns `false`.

```csharp
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

UIRoot root = new();
UIElement panel = new();
ButtonBase button = new();
panel.VisualChildren.Add(button);
root.VisualChildren.Add(panel);

ElementInputRouteMap frameMap = root.InputCache.EnsureCurrent(root);
panel.Visibility = Visibility.Collapsed;

bool inMap = frameMap.Contains(button);              // true
bool canFocus = FocusPolicy.CanFocus(button, frameMap); // false
```

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
