# PresenceHandle Class

## Definition
Namespace: `Cerneala.UI.Motion.Presence`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Presence/PresenceHandle.cs`

Represents an active presence exit operation tracked by a `PresenceCoordinator`.

```csharp
public sealed class PresenceHandle
```

Inheritance:
`object` -> `PresenceHandle`

## Examples

Configure presence on an element and remove it from its parent. The coordinator creates the `PresenceHandle` internally while the element remains attached for the exit animation:

```csharp
using System;
using Cerneala.UI.Controls;
using Cerneala.UI.Motion.Presence;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

StackPanel parent = new();
Border item = new()
{
    Presence = PresenceOptions.FadeAndScale(
        MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(120)),
        MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(120)))
};

parent.VisualChildren.Add(item);
parent.VisualChildren.Remove(item);
```

## Remarks

`PresenceHandle` is a framework-owned handle for an element that is leaving a visual tree through presence motion. Application code does not construct it directly because its constructor is internal. The public surface is intentionally small: callers can inspect the owning element, the exiting element, and the current `PresenceState`.

The handle is created by `PresenceCoordinator` when an attached element with `PresenceOptions` is removed. During exit, the element is removed from the public child collection but stays attached through the coordinator's exiting visual children list so it can continue rendering until its exit animation completes.

The coordinator stores the handle while animating the element's internal presence opacity and scale. When the opacity animation completes without cancellation, the handle completes the removal once, detaches the element subtree from the root, restores presence visuals to `1`, and marks the state as `Detached`.

If the same element is re-added while exiting, the coordinator cancels the handle's motion, disposes the subscriptions used to update presence visuals, restores the element to `Present`, and prevents the delayed removal. By default, exiting elements are excluded from input hit testing through `PresenceOptions.ExcludeInputWhileExiting`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Owner` | `UIElement` | Gets the element that owned the exiting child when the exit began. |
| `Element` | `UIElement` | Gets the element being held alive for presence exit rendering. |
| `State` | `PresenceState` | Gets the presence state tracked by the handle. The setter is internal. |

## Applies to

Cerneala presence motion for `UIElement` removal transitions.

## See also

- `Cerneala.UI.Motion.Presence.PresenceCoordinator`
- `Cerneala.UI.Motion.Presence.PresenceOptions`
- `Cerneala.UI.Motion.Presence.PresenceState`
- `Cerneala.UI.Elements.UIElement`
