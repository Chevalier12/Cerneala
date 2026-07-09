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

bool childIsInRoute = map.TryGetId(child, out UiElementId childId);
IReadOnlyList<UiElementId> route = map.InputTree.GetRouteToRoot(childId);
```

## Remarks

`ElementInputRouteBuilder` traverses the visual tree below the supplied `UIRoot` and registers attached elements that participate in input. Registered elements are added to the returned `ElementInputRouteMap`, and their routed event handlers are exported to the map's `UiInputTree`.

`Build` excludes disabled elements, invisible elements, elements whose `Visibility` is not `Visible`, and elements that are leaving through presence state. Disabled elements do not become route nodes in a normal input map, but their visible and enabled descendants can still be included under the nearest included ancestor.

Elements that fail `UIElementVisibility.ParticipatesInInput` stop traversal for their whole visual subtree. In practice, elements with `IsVisible == false`, `Visibility.Hidden`, `Visibility.Collapsed`, or an exiting presence state are excluded together with their descendants.

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
