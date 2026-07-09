# UiFrameScheduler Class

## Definition
Namespace: `Cerneala.UI.Invalidation`

Assembly/Project: `Cerneala`

Source: `UI/Invalidation/UiFrameScheduler.cs`

Coordinates one retained UI frame by processing invalidation queues in a deterministic phase order.

```csharp
public sealed class UiFrameScheduler
```

Inheritance:
`object` -> `UiFrameScheduler`

## Examples

Process a frame through the scheduler owned by a `UIRoot`:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

UIRoot root = new(viewportWidth: 800, viewportHeight: 600);

if (root.Scheduler.HasWork)
{
    FrameStats stats = root.ProcessFrame();
}
```

Run the scheduler directly with custom phase processors:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

UIRoot root = new(800, 600);

FramePhaseProcessors processors = root.LayoutManager.CreatePhaseProcessors();
FrameStats stats = root.Scheduler.ProcessFrame(processors);
```

## Remarks

`UiFrameScheduler` is the frame coordinator for the retained invalidation pipeline. `UIRoot` creates one scheduler and exposes it through `UIRoot.Scheduler`; normal root frame processing goes through `UIRoot.ProcessFrame`, which supplies the default phase processors and motion coordinator.

When work exists, `ProcessFrame` runs phases in this order: inherited properties, command state, aspect, inherited properties again, motion before layout, measure, arrange, motion after layout, motion before render, render cache, hit test, and motion end frame. Each phase processes a snapshot of its queue. Same-phase work enqueued while a snapshot is being processed is deferred to a later frame, while downstream phase work can still run in the current frame if that downstream snapshot has not been taken yet.

The scheduler clears the dirty flag associated with a completed concrete phase when the element is no longer queued for that phase. When no concrete work flags remain on an element, specialized flags for text, image, resources, input visuals, semantics, and subtree work are also cleared.

If a phase processor throws, the scheduler requeues the element for that phase and restores any dirty flags it cleared before rethrowing. The optional `InvalidationTrace` records per-phase entries, clear operations, idle frames, and phase summaries.

If there is no invalidation work and no motion coordinator is supplied, `ProcessFrame` counts a no-work frame and records the idle phase. If a motion coordinator is supplied, the scheduler still advances motion and runs the layout, render, and hit-test phase hooks against empty queue snapshots.

`FrameBudget` is accepted by `ProcessFrame` and defaults to `FrameBudget.ProcessAll`; the current implementation does not defer work based on the budget value.

## Constructors

| Name | Description |
| --- | --- |
| `UiFrameScheduler(LayoutQueue, InheritedPropertyQueue, CommandStateQueue, AspectQueue, RenderQueue, HitTestQueue, InvalidationTrace?)` | Initializes a scheduler over the supplied invalidation queues. Throws `ArgumentNullException` for any required queue argument that is `null`. Uses `InvalidationTrace.Disabled` when `trace` is `null`. |

## Properties

| Name | Description |
| --- | --- |
| `HasWork` | Gets whether any inherited-property, command-state, aspect, layout, render, or hit-test queue currently contains work. |

## Methods

| Name | Description |
| --- | --- |
| `ProcessFrame(FramePhaseProcessors?, FrameBudget, FrameStats?, MotionFrameCoordinator?, MotionFrameReason)` | Processes one scheduler frame, records work into the supplied or newly created `FrameStats`, advances the optional motion coordinator, and returns the stats instance. |

## Method Details

### ProcessFrame

```csharp
public FrameStats ProcessFrame(
    FramePhaseProcessors? processors = null,
    FrameBudget budget = default,
    FrameStats? stats = null,
    MotionFrameCoordinator? motion = null,
    MotionFrameReason motionReason = MotionFrameReason.Scheduled)
```

#### Parameters

| Name | Description |
| --- | --- |
| `processors` | Phase callbacks used to perform inherited-property propagation, command-state refresh, aspect processing, layout, render-cache update, and hit-test cache update. `null` uses `FramePhaseProcessors.Empty`. |
| `budget` | Frame budget value. `default` is normalized to `FrameBudget.ProcessAll`. |
| `stats` | Existing statistics object to update. `null` creates a new `FrameStats`. |
| `motion` | Optional motion frame coordinator. When supplied, motion hooks are invoked even when no invalidation queue has work. |
| `motionReason` | Reason passed to `motion.BeginFrame`. |

#### Returns

The supplied `stats` instance, or a new `FrameStats` instance when `stats` is `null`.

## Applies to

Cerneala retained UI invalidation, layout, render-cache, hit-test, command-state, aspect, and motion frame processing.

## See also

- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Invalidation.FramePhaseProcessors`
- `Cerneala.UI.Invalidation.FrameStats`
- `Cerneala.UI.Invalidation.FrameBudget`
- `Cerneala.UI.Diagnostics.InvalidationTrace`
