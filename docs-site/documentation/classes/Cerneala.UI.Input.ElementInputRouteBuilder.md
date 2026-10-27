# ElementInputRouteBuilder Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/ElementInputRouteBuilder.cs`

Builds `ElementInputRouteMap` instances from a `UIRoot` visual subtree.

```csharp
public sealed class ElementInputRouteBuilder
```

Inheritance:
`Object` -> `ElementInputRouteBuilder`

## Examples

Build an input route map for a root and one visual child.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIRoot root = new();
UIElement child = new();

root.VisualChildren.Add(child);

ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);

bool childIsInRoute = map.Contains(child);
IReadOnlyList<UIElement> route = map.InputTree.GetRouteToRoot(child);
```

## Remarks

`ElementInputRouteBuilder` traverses the visual tree below the supplied `UIRoot`, plus input-subtree children supplied by hosts such as `RenderSurface2D` (its scene), and registers attached elements that participate in input. Registered elements are added to the returned `ElementInputRouteMap`, and their routed event handlers are exported to the map's `UiInputTree`.

Each registered element's route parent in `UiInputTree` is its nearest registered visual ancestor, with one exception: the projected presenter of an open `Overlay` gets the overlay (or, when the overlay is not registered, the overlay's nearest registered ancestor) as its route parent, although its visual parent is the root-owned overlay layer. Routed events from projected content therefore reach the overlay and its ancestors, as WPF routes `Popup` content. Traversal order is unchanged, so `ElementsInRouteOrder` keeps the projected content after the rest of the tree. When the overlay is outside the input route, for example under a collapsed ancestor, the projected content keeps its visual route parent.

`Build` excludes disabled elements, invisible elements, elements whose `Visibility` is not `Visible`, and elements that are leaving through presence state with `PresenceOptions.ExcludeInputWhileExiting` set to `true` (the default). An exiting element whose option is `false` has left `VisualChildren`, but it is still traversed, with its subtree, after its former parent's visual children, and its route parent is the former parent's nearest registered element, until the exit ends. Disabled elements do not become route nodes in a normal input map, but their visible and enabled descendants can still be included under the nearest included ancestor.

Elements that fail `UIElementVisibility.ParticipatesInInput` stop traversal for their whole visual subtree. In practice, elements with `IsVisible == false`, `Visibility.Hidden`, `Visibility.Collapsed`, or an exiting presence state that excludes input are excluded together with their descendants.

`BuildForCommandState` uses the same visibility and attachment rules as `Build`, but includes disabled elements. `UIRoot` uses this command-state route while refreshing command state so disabled command sources can still participate in command evaluation. The disabled state is still copied into `UiInputTree` when each element is added.

Both public build methods throw `ArgumentNullException` when `root` is `null`.

## Constructors

| Name | Description |
| --- | --- |
| `ElementInputRouteBuilder()` | Initializes a route builder. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Build(UIRoot)` | `ElementInputRouteMap` | Builds an input route map for attached, input-participating, enabled elements in the root visual subtree. |
| `BuildForCommandState(UIRoot)` | `ElementInputRouteMap` | Builds a command-state route map that includes disabled elements while still excluding elements that do not participate in input. |

## Applies to

Cerneala retained input routing, hit testing, routed events, focus, hover, text input, and command-state refresh paths.

## See also

- `Cerneala.UI.Input.ElementInputRouteMap`
- `Cerneala.UI.Input.ElementInputCache`
- `Cerneala.UI.Input.UiInputTree`
- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Elements.UIElementVisibility`
