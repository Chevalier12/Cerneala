# MotionGraph Class

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionGraph.cs`

Owns active motion nodes, creates graph-bound motion values, and advances registered motion work for each frame.

```csharp
public sealed class MotionGraph
```

Inheritance:
`object` -> `MotionGraph`

## Examples

Create a graph-owned value, start a tween, and tick the graph manually:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

MotionGraph graph = new(new MotionThreadGuard(Environment.CurrentManagedThreadId));
MotionValue<double> opacity = graph.CreateValue(0d);

opacity.AnimateTo(1d, Motion.Tween<double>(TimeSpan.FromMilliseconds(150)));

MotionFrame frame = new(
    TimeSpan.FromMilliseconds(150),
    TimeSpan.FromMilliseconds(150),
    frameIndex: 1,
    MotionFrameReason.Manual,
    MotionFramePhase.BeforeRender);

MotionFrameResult result = graph.Tick(frame);
double currentOpacity = opacity.Current;
```

## Remarks

`MotionGraph` is the low-level owner for active `MotionNode` instances. Higher-level APIs such as `MotionSystem` keep a graph instance and use it to sample active motion values during each motion frame.

All public mutation and sampling APIs verify access through the supplied `MotionThreadGuard`. Create and mutate the graph on its owning UI thread; cross-thread animation requests must be marshaled through the platform UI dispatcher before they call graph APIs.

`CreateValue<T>` returns a `MotionValue<T>` bound to this graph. If no mixer is supplied, the graph resolves one from its `ValueMixerRegistry`; the one-argument constructor creates a registry and registers the built-in mixers.

`Register` and `Unregister` are safe to call while the graph is ticking. During a tick, adds and removes are queued, then applied after sampling completes. Re-registering an already registered or pending node is ignored, and unregistering a node that is not registered is also ignored.

`Tick` applies pending changes, samples each active node once, aggregates the node tick results into a `MotionFrameResult`, unregisters nodes that report completion, and returns `MotionFrameResult.Empty(frame)` when there is no active work.

## Constructors

| Name | Description |
| --- | --- |
| `MotionGraph(MotionThreadGuard threadGuard)` | Initializes a graph with built-in value mixers, `ReducedMotionPolicy.Default`, and no diagnostics recorder. |
| `MotionGraph(MotionThreadGuard threadGuard, ValueMixerRegistry mixers, ReducedMotionPolicy reducedMotion, MotionDiagnostics? diagnostics = null)` | Initializes a graph with explicit mixer registry, reduced-motion policy, and optional diagnostics recorder. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `HasActiveMotion` | `bool` | Gets whether the graph has registered nodes or pending nodes waiting to be added. |
| `ActiveNodeCount` | `int` | Gets the number of registered nodes plus pending additions. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `CreateValue<T>(T initial, ValueMixer<T>? mixer = null)` | `MotionValue<T>` | Creates a graph-bound motion value with the supplied initial value and either the explicit mixer or the graph registry's mixer for `T`. |
| `Tick(MotionFrame frame)` | `MotionFrameResult` | Advances registered nodes for `frame`, applies queued graph changes, unregisters completed nodes, and returns aggregate frame counters. |
| `Register(MotionNode node)` | `void` | Registers a motion node, or queues it for registration if a tick is already in progress. |
| `Unregister(MotionNode node)` | `void` | Unregisters a motion node, or queues it for removal if a tick is already in progress. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `MotionGraph(MotionThreadGuard)` | `ArgumentNullException` | `threadGuard` is `null`. |
| `MotionGraph(MotionThreadGuard, ValueMixerRegistry, ReducedMotionPolicy, MotionDiagnostics?)` | `ArgumentNullException` | `threadGuard`, `mixers`, or `reducedMotion` is `null`. |
| `CreateValue<T>`, `Tick`, `Register`, `Unregister` | `InvalidOperationException` | The current thread is not the owner accepted by the graph's `MotionThreadGuard`. |
| `CreateValue<T>` | Mixer resolution exception from `ValueMixerRegistry` | No mixer is supplied and the registry cannot resolve a mixer for `T`. |
| `Register`, `Unregister` | `ArgumentNullException` | `node` is `null`. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `UI/Motion/Core/MotionGraph.cs`
- `UI/Motion/Core/MotionSystem.cs`
- `UI/Motion/Core/MotionNode.cs`
- `UI/Motion/Core/MotionValue{T}.cs`
- `UI/Motion/Core/MotionFrameResult.cs`
