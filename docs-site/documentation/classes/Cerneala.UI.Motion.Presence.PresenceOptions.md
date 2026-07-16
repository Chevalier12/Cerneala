# PresenceOptions Class

## Definition
Namespace: `Cerneala.UI.Motion.Presence`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Presence/PresenceOptions.cs`

Stores the motion specifications used by an element's presence enter and exit animations.

```csharp
public sealed class PresenceOptions
```

Inheritance:
`object` -> `PresenceOptions`

## Examples
Assign presence motion to an element so it fades and scales when added to or removed from the retained visual tree.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion.Presence;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

Border item = new()
{
    Padding = new Thickness(12),
    Presence = PresenceOptions.FadeAndScale(
        MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(120)),
        MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(120)),
        excludeInputWhileExiting: false)
};
```

## Remarks
`PresenceOptions` is assigned through `UIElement.Presence`. When an attached element with presence options is added to the tree, `PresenceCoordinator` initializes its presence visual state to opacity `0` and scale `0.95`, then animates both values to `1` using `Enter`.

When an attached element with presence options is removed from a visual child collection, the coordinator keeps it attached as an exiting visual child until its exit opacity animation completes. During that exit, opacity animates to `0` and scale animates to `0.95` using `Exit`.

`ExcludeInputWhileExiting` defaults to `true`. The three-parameter `FadeAndScale` overload can opt an exiting element back into hit testing while preserving the same coordinator-owned enter and exit lifecycle.

Re-adding an element while it is exiting cancels the exit, clears the exiting input state, restores presence opacity and scale to `1`, and marks the element present again.

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Enter` | `MotionSpec<float>` | Gets the float motion specification used for enter opacity and scale animations. |
| `Exit` | `MotionSpec<float>` | Gets the float motion specification used for exit opacity and scale animations. |
| `ExcludeInputWhileExiting` | `bool` | Gets the init-only value that controls whether an exiting element is excluded from input; defaults to `true`. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `FadeAndScale(MotionSpec<float> enter, MotionSpec<float> exit)` | `PresenceOptions` | Creates presence options that use the supplied enter and exit specifications for opacity and scale animation. |
| `FadeAndScale(MotionSpec<float> enter, MotionSpec<float> exit, bool excludeInputWhileExiting)` | `PresenceOptions` | Creates presence options and explicitly controls whether the exiting element participates in input hit testing. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `FadeAndScale(MotionSpec<float>, MotionSpec<float>)` | `ArgumentNullException` | `enter` or `exit` is `null`. |
| `FadeAndScale(MotionSpec<float>, MotionSpec<float>, bool)` | `ArgumentNullException` | `enter` or `exit` is `null`. |

## Applies to
Cerneala retained UI presence motion in the `Cerneala` project.

Target framework: `net8.0`

## See also
- `UI/Motion/Presence/PresenceOptions.cs`
- `UI/Motion/Presence/PresenceCoordinator.cs`
- `UI/Motion/Presence/PresenceState.cs`
- `UI/Elements/UIElement.cs`
- `UI/Motion/Specs/Motion.cs`
