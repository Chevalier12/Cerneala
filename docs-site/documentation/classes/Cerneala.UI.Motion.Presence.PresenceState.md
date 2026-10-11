# PresenceState Enum

## Definition
Namespace: `Cerneala.UI.Motion.Presence`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Presence/PresenceState.cs`

Describes whether an element is attached, playing its presence exit animation, or detached.

```csharp
public enum PresenceState
```

## Examples

Remove an element that has presence options and read its state while the exit animation runs:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Presence;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

UIRoot root = new(100, 100);
UIElement child = new()
{
    Presence = PresenceOptions.FadeAndScale(
        MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)),
        MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)))
};
UIElement never = new();

root.VisualChildren.Add(child);
root.ProcessFrame();
Console.WriteLine(root.Motion.Presence.GetState(child)); // Present
Console.WriteLine(root.Motion.Presence.GetState(never)); // Detached

root.VisualChildren.Remove(child);
Console.WriteLine(root.Motion.Presence.GetState(child)); // Exiting
Console.WriteLine(child.IsAttached);                      // True, until the exit animation ends
```

## Remarks

Read the state with `PresenceCoordinator.GetState(UIElement)`, reached through `UIRoot.Motion.Presence`.

The coordinator stores a state only for an element that has `UIElement.Presence` set. For any other element, `GetState` returns `Present` when the element is attached and `Detached` when it is not.

The state of an element with presence options changes like this:

| Step | `GetState` result |
| --- | --- |
| The element is attached, including while its enter animation runs. | `Present` |
| The element is removed through `VisualChildren.Remove`. It stays attached and plays its exit animation. | `Exiting` |
| The element is added back while it is still exiting. The exit is canceled. | `Present` |
| The exit animation completes and the element is detached. | `Detached` |

While the state is `Exiting` and `PresenceOptions.ExcludeInputWhileExiting` is `true` (the default), the element takes no input and is skipped by hit testing. It is still rendered, so the exit animation stays visible.

`Entering` is declared but the current implementation never reports it. An element that is playing its enter animation reports `Present`.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Detached` | `0` | The element is not attached to a root. |
| `Present` | `1` | The element is attached and is not exiting. |
| `Entering` | `2` | Reserved. The current implementation does not report this value. |
| `Exiting` | `3` | The element was removed and is playing its presence exit animation before it is detached. |

## Applies to

Cerneala presence motion for elements that set `UIElement.Presence`.

## See also

- `Cerneala.UI.Motion.Presence.PresenceCoordinator`
- `Cerneala.UI.Motion.Presence.PresenceOptions`
- `Cerneala.UI.Elements.UIElement`
