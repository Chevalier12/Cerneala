# MotionStateBuilder Class

## Definition

Namespace: `Cerneala.UI.Motion`

Assembly/Project: `Cerneala`

Source: `UI/Motion/MotionStateBuilder.cs`

Registers property targets that animate when an element enters or leaves an Aspect state.

```csharp
public sealed class MotionStateBuilder
```

Inheritance:
`object` -> `MotionStateBuilder`

## Examples

Animate opacity while a pointer is over an attached element, then return to the captured baseline:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Specs;

UIRoot root = new();
UIElement element = new() { Opacity = 1f };
root.VisualChildren.Add(element);

element.Motion().States()
    .When(AspectState.Hover)
    .Set(
        UIElement.OpacityProperty,
        0.6f,
        Motion.Tween<float>(TimeSpan.FromMilliseconds(100)));
```

## Remarks

Obtain the builder through `element.Motion().States()`. Calls for the same element return the same builder, so registrations made through separate facade instances participate in one state table. The element must be attached to a `UIRoot`, or be the root itself, when `Set` registers a target.

The builder observes the built-in element states represented by `AspectStateSet.FromElement`, including hover, pressed, focus, enabled/disabled, checked, selected, expanded, validation, and drag states. When more than one registered state matches the same property, the most recently registered matching state wins.

The property's effective value at its first registration is captured as its baseline. When no registered state matches, the property animates back to that baseline using the last active state's specification. State-created motion uses `MotionPriority.Interactive`, holds its completed value, and yields to active normal- or reduced-motion-priority animation.

If a higher-priority animation rejects a state target, the builder keeps that property pending. Once the blocking animation completes or is canceled (including disposal), a motion frame re-evaluates the element's current states and starts the current winning target, or restores the baseline if no registered state still matches. Changes made while blocked replace the pending intent; an obsolete state target is not replayed. A replacement higher-priority animation continues to block the state motion. Waiting stops if the element detaches or becomes effectively invisible.

## Constructors

| Name | Description |
| --- | --- |
| None | Use `MotionElementFacade.States()` to obtain the element-owned builder. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `When(AspectState state)` | `MotionStateTargetBuilder` | Selects a state for a subsequent property target registration. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `When` | `ArgumentNullException` | `state` is `null`. |
| `MotionStateTargetBuilder.Set` | `InvalidOperationException` | The target element is detached and is not a `UIRoot`. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.MotionStateTargetBuilder`
- `Cerneala.UI.Motion.MotionElementFacade`
- `Cerneala.UI.Aspect.AspectState`
- `Cerneala.UI.Motion.Core.MotionPriority`
