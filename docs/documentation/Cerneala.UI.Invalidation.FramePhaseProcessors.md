# FramePhaseProcessors Class

## Definition
Namespace: `Cerneala.UI.Invalidation`

Assembly/Project: `Cerneala`

Source: `UI/Invalidation/FramePhaseProcessors.cs`

Provides optional per-frame callbacks that `UiFrameScheduler` invokes for queued invalidation phases.

```csharp
public sealed class FramePhaseProcessors
```

Inheritance:
`object` -> `FramePhaseProcessors`

## Examples

The following example runs a custom render-cache callback for render invalidations queued on a retained UI tree.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

UIRoot root = new();
UIElement child = new();

root.VisualChildren.Add(child);
root.ProcessFrame();

child.Invalidate(InvalidationFlags.Render, "example render update");

int rendered = 0;
FrameStats stats = root.ProcessFrame(new FramePhaseProcessors
{
    RenderCache = element =>
    {
        if (ReferenceEquals(element, child))
        {
            rendered++;
        }
    }
});
```

## Remarks

`FramePhaseProcessors` is a small dispatch container for the retained invalidation frame loop. Each public init-only property maps one scheduler phase to an `Action<UIElement>` callback. When a callback is `null`, that phase is counted and cleared by the scheduler, but no user-provided phase work runs.

`UIRoot.ProcessFrame` normally creates framework processors that wire inherited property propagation, command state refresh, aspect processing, layout, render-cache processing, and hit-test cache updates. Supplying a custom `FramePhaseProcessors` instance replaces those callbacks for that frame and is useful for tests or specialized frame processing.

`UiFrameScheduler` processes phases in this order when work is queued: inherited properties, command state, aspect, inherited properties again, measure, arrange, render cache, and hit test. Same-phase work queued while a phase is processing is deferred to a later frame; downstream work may run in the same frame if its snapshot has not been taken yet.

If a phase callback throws, the scheduler requeues the affected element for that phase and preserves the relevant dirty state before rethrowing.

## Constructors

| Name | Description |
| --- | --- |
| `FramePhaseProcessors()` | Initializes a processor set with all phase callbacks unset. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Empty` | `FramePhaseProcessors` | Gets a shared processor set with no callbacks configured. |
| `InheritedProperties` | `Action<UIElement>?` | Gets or initializes the callback invoked for `FramePhase.InheritedProperties`. |
| `CommandState` | `Action<UIElement>?` | Gets or initializes the callback invoked for `FramePhase.CommandState`. |
| `Aspect` | `Action<UIElement>?` | Gets or initializes the callback invoked for `FramePhase.Aspect`. |
| `Measure` | `Action<UIElement>?` | Gets or initializes the callback invoked for `FramePhase.Measure`. |
| `Arrange` | `Action<UIElement>?` | Gets or initializes the callback invoked for `FramePhase.Arrange`. |
| `RenderCache` | `Action<UIElement>?` | Gets or initializes the callback invoked for `FramePhase.RenderCache`. |
| `HitTest` | `Action<UIElement>?` | Gets or initializes the callback invoked for `FramePhase.HitTest`. |

## Applies to

Retained UI invalidation frames in the `Cerneala` project.

## See also

- `Cerneala.UI.Invalidation.FramePhase`
- `Cerneala.UI.Invalidation.UiFrameScheduler`
- `Cerneala.UI.Elements.UIRoot`
