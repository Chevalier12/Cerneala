# MotionSystem Class

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionSystem.cs`

Owns the root-level motion services, frame ticking, transactions, diagnostics, reduced-motion policy, and property-motion plumbing for a `UIRoot`.

```csharp
public sealed class MotionSystem
```

Inheritance:
`object` -> `MotionSystem`

## Examples

Use the motion system exposed by a root to create a graph value and advance it with a deterministic clock:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

ManualMotionClock clock = new();
UIRoot root = new(motionClock: clock);

MotionValue<double> opacity = root.Motion.Graph.CreateValue(0d);
opacity.AnimateTo(1d, Motion.Tween<double>(TimeSpan.FromMilliseconds(100)));

MotionFrameResult first = root.Motion.Tick(MotionFrameReason.Manual);
clock.Advance(TimeSpan.FromMilliseconds(16));
MotionFrameResult second = root.Motion.Tick(MotionFrameReason.Manual);
```

Wrap animatable property mutations in a transaction:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Specs;

UIRoot root = new();

using (root.Motion.BeginTransaction(Motion.Tween(TimeSpan.FromMilliseconds(150))))
{
    // Mutations to registered animatable UI properties can be converted into motion.
}
```

## Remarks

`MotionSystem` is created by `UIRoot` and is available through `UIRoot.Motion`. It composes the main motion subsystems used by the retained UI runtime: `MotionGraph`, `MotionFrameCoordinator`, `MotionPropertyStore`, `MotionTransactionContext`, `LayoutMotionCoordinator`, `PresenceCoordinator`, diagnostics, tokens, mixers, and animatable-property registration.

The constructor captures thread affinity immediately by creating a `MotionThreadGuard` for the current managed thread. Public APIs that mutate or sample motion, such as `Tick` and transaction creation, verify that access through the guard. Create the owning root on the UI thread and marshal cross-thread motion requests through the platform UI dispatcher before calling motion APIs.

`Tick` samples active graph motion and flushes staged property writes. When there is no active graph work and no pending property writes, it returns an empty `MotionFrameResult`, resets the previous timestamp, and does not increment the frame index. The first active tick after an idle period uses a zero delta. Subsequent active ticks use the clock delta clamped to `MaxDelta`; negative clock deltas are treated as zero.

The frame result combines graph counters with property flush counters. `NeedsAnotherFrame` stays `true` while the graph has active motion or the property store still has pending writes. When motion becomes idle again, the next active run restarts from a zero delta.

`Frames` is the normal integration point for the retained frame scheduler. It coordinates pre-input, layout, render, and diagnostics phases while delegating actual sampling to `Tick`.

## Constructors

| Name | Description |
| --- | --- |
| `MotionSystem(UIRoot root, IMotionClock clock, ReducedMotionPolicy reducedMotion)` | Initializes a root-owned motion system, captures UI-thread affinity, registers built-in value mixers, and creates the graph, frame coordinator, diagnostics, property store, transactions, layout, and presence coordinators. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `ActiveOpacityRenderInvalidationsPerTickBudget` | `int` | Stress budget for active opacity render invalidations per tick. The current value is `1`. |
| `SimultaneousRenderAnimationStressBudget` | `int` | Stress budget for simultaneous render animations. The current value is `100`. |
| `LayoutMotionStressBudget` | `int` | Stress budget for layout motion work. The current value is `100`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Root` | `UIRoot` | Gets the root that owns this motion system. |
| `ThreadGuard` | `MotionThreadGuard` | Gets the guard used to enforce motion thread affinity. |
| `ReducedMotion` | `ReducedMotionPolicy` | Gets the reduced-motion policy used by the graph and related motion services. |
| `Graph` | `MotionGraph` | Gets the graph that owns active motion nodes and graph-bound motion values. |
| `Timelines` | `MotionTimelineRegistry` | Gets the registry for named or shared motion timelines. |
| `Diagnostics` | `MotionDiagnostics` | Gets the diagnostics recorder and snapshot source for motion frames. |
| `Frames` | `MotionFrameCoordinator` | Gets the coordinator that integrates motion sampling with retained frame phases. |
| `Tokens` | `MotionTokens` | Gets the motion token set used by motion-aware styling and state APIs. |
| `Mixers` | `ValueMixerRegistry` | Gets the value mixer registry. Built-in mixers are registered during construction. |
| `Properties` | `MotionPropertyStore` | Gets the store for motion-driven UI property bindings and pending animation writes. |
| `AnimatableProperties` | `AnimatablePropertyRegistry` | Gets the registry of UI properties that can be animated by motion transactions. |
| `Transactions` | `MotionTransactionContext` | Gets the transaction context that observes eligible property mutations and turns them into animations. |
| `Layout` | `LayoutMotionCoordinator` | Gets the coordinator for layout-motion bindings and snapshot-based correction work. |
| `Presence` | `PresenceCoordinator` | Gets the coordinator for presence and exit-motion state. |
| `MaxDelta` | `TimeSpan` | Gets or sets the maximum per-frame delta accepted by `Tick`. The default is `100` milliseconds. |
| `HasActiveMotion` | `bool` | Gets whether the graph has active motion or the property store has pending writes. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `BeginTransaction(MotionSpec defaultSpec)` | `MotionTransactionScope` | Begins a motion transaction using the supplied default motion spec. |
| `BeginTransaction(MotionTransactionOptions options)` | `MotionTransactionScope` | Begins a motion transaction using explicit transaction options. |
| `Disable()` | `MotionTransactionScope` | Begins a disabled motion transaction scope so observed property mutations are not animated. |
| `Tick(MotionFrameReason reason = MotionFrameReason.Scheduled, MotionFramePhase phase = MotionFramePhase.BeforeRender)` | `MotionFrameResult` | Verifies thread access, samples active graph motion for the current clock time, flushes pending property writes, and returns aggregate frame counters. |

## Method Details

### Tick

```csharp
public MotionFrameResult Tick(
    MotionFrameReason reason = MotionFrameReason.Scheduled,
    MotionFramePhase phase = MotionFramePhase.BeforeRender)
```

`Tick` reads the configured `IMotionClock`, builds a `MotionFrame`, samples `Graph` when it has active nodes, flushes `Properties`, and returns a combined `MotionFrameResult`. If motion is idle, the returned result is empty and uses the current frame index without advancing it.

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `MotionSystem(UIRoot, IMotionClock, ReducedMotionPolicy)` | `ArgumentNullException` | `root`, `clock`, or `reducedMotion` is `null`. |
| `BeginTransaction(MotionSpec)`, `BeginTransaction(MotionTransactionOptions)`, `Disable()`, `Tick(...)` | `InvalidOperationException` | The current thread is not the thread captured by `ThreadGuard`. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Motion.Core.MotionGraph`
- `Cerneala.UI.Motion.Core.MotionFrameCoordinator`
- `Cerneala.UI.Motion.Core.MotionFrameResult`
- `Cerneala.UI.Motion.Transactions.MotionTransactionContext`
- `Cerneala.UI.Motion.Properties.MotionPropertyStore`
