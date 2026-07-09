# PresenceCoordinator Class

## Definition
Namespace: `Cerneala.UI.Motion.Presence`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Presence/PresenceCoordinator.cs`

Coordinates enter and exit presence motion for `UIElement` instances owned by a `MotionSystem`.

```csharp
public sealed class PresenceCoordinator
```

Inheritance:
`object` -> `PresenceCoordinator`

## Examples

Inspect the presence state for an element that opts into fade-and-scale presence motion:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Presence;
using Cerneala.UI.Motion.Specs;

UIRoot root = new(100, 100);
Canvas parent = new();
Rectangle child = new()
{
    Fill = new SolidColorBrush(DrawColor.White),
    Geometry = new RectangleGeometry(new DrawRect(0, 0, 20, 20)),
    Presence = PresenceOptions.FadeAndScale(
        Motion.Tween<float>(TimeSpan.FromMilliseconds(120)),
        Motion.Tween<float>(TimeSpan.FromMilliseconds(120)))
};

root.VisualChildren.Add(parent);
parent.VisualChildren.Add(child);

PresenceState state = root.Motion.Presence.GetState(child);
int activeExitCount = root.Motion.Presence.ActiveExitCount;
```

When a visual child with presence options is removed, the public child collection no longer contains it, but the coordinator keeps it renderable until exit motion completes:

```csharp
parent.VisualChildren.Remove(child);

bool isExiting = root.Motion.Presence.GetState(child) == PresenceState.Exiting;
bool isRenderedAsExitingChild = root.Motion.Presence
    .GetExitingVisualChildren(parent)
    .Contains(child);
```

## Remarks

`PresenceCoordinator` is created by `MotionSystem` and exposed through `MotionSystem.Presence`. Application code normally configures presence through `UIElement.Presence`; the retained element collection and attach/detach lifecycle call the coordinator when elements enter or leave the visual tree.

An attached element with `PresenceOptions` starts enter motion when it is attached. The coordinator initializes presence opacity to `0` and presence scale below `1`, then animates both values to `1` by using the element's enter spec.

Removing a visual child with `PresenceOptions` starts exit motion instead of detaching the subtree immediately. The child is removed from the owner's public `VisualChildren` collection, tracked as an exiting visual child for the former owner, marked as `PresenceState.Exiting`, and animated toward opacity `0` and scale `0.95`. The renderer appends exiting visual children after the owner's normal visual children so they can continue drawing during the exit.

By default, exiting elements are excluded from hit testing through `PresenceOptions.ExcludeInputWhileExiting`. Re-adding an element while it is exiting cancels the exit, restores presence opacity and scale to `1`, and marks the element present again. When the exit opacity animation completes without cancellation, the coordinator detaches the subtree from the root and records the element as detached.

Presence visual state participates in render transforms and opacity composition through `UIElement.PresenceOpacity` and `UIElement.PresenceScale`. The coordinator also coexists with layout motion; exit motion does not clear an element's layout-motion binding.

## Constructors

| Name | Description |
| --- | --- |
| `PresenceCoordinator(MotionSystem motion)` | Initializes a coordinator owned by the supplied motion system. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ActiveExitCount` | `int` | Gets the number of elements currently tracked as active exits. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetState(UIElement element)` | `PresenceState` | Returns the tracked presence state for `element`, or derives `Present`/`Detached` from the element attachment state when no tracked state exists. |
| `GetExitingVisualChildren(UIElement owner)` | `IReadOnlyList<UIElement>` | Returns the visual children that are exiting for `owner`, or an empty list when no children are exiting. |

## Method Details

### GetState

```csharp
public PresenceState GetState(UIElement element)
```

Returns the current tracked state for an element. If the coordinator has not tracked the element yet, the method returns `PresenceState.Present` when the element is attached and `PresenceState.Detached` when it is not attached.

### GetExitingVisualChildren

```csharp
public IReadOnlyList<UIElement> GetExitingVisualChildren(UIElement owner)
```

Returns the retained exit list for an owner. This list is used by rendering to draw visual children that were removed from `owner.VisualChildren` but have not finished exit motion.

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `PresenceCoordinator(MotionSystem)` | `ArgumentNullException` | `motion` is `null`. |
| `GetState(UIElement)` | `ArgumentNullException` | `element` is `null`. |
| `GetExitingVisualChildren(UIElement)` | `ArgumentNullException` | `owner` is `null`. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Core.MotionSystem`
- `Cerneala.UI.Motion.Presence.PresenceOptions`
- `Cerneala.UI.Motion.Presence.PresenceState`
- `Cerneala.UI.Motion.Presence.PresenceHandle`
- `Cerneala.UI.Elements.UIElement`
