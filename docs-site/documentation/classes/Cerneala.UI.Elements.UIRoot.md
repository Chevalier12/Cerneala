# UIRoot Class

## Definition
Namespace: `Cerneala.UI.Elements`

Assembly/Project: `Cerneala`

Source: `UI/Elements/UIRoot.cs`

Represents the retained UI tree root and owns the viewport state, UI-thread Relay, frame scheduler, rendering, input, resource, aspect, theme, motion, and semantics services for the tree.

```csharp
public sealed class UIRoot : UIElement, IElementHost, IInvalidationSink
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `UIRoot`

Implements:
`IElementHost`, `IInvalidationSink`

## Examples

The following example creates a root for a viewport, attaches a visual child, and processes a retained UI frame.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

UIRoot root = new(viewportWidth: 1280, viewportHeight: 720, scale: 1);
UIElement content = new();

root.VisualChildren.Add(content);
content.Invalidate(InvalidationFlags.Render, "Initial render");

FrameStats stats = root.ProcessFrame();
```

The following example wires root-level services commonly used by a hosted application.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Resources;
using Cerneala.UI.Relay;
using Cerneala.UI.Theming;

UIRoot root = new(
    800,
    600,
    relayOptions: new UiRelayOptions { MaxCallbacksPerUpdate = 256 });
ResourceStore resources = new();

root.SetThemeProvider(new ThemeProvider(DefaultTheme.Create()));
root.SetResourceProvider(resources);
```

The following example opts into retained invalidation tracing for a diagnostic session.

```csharp
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;

InvalidationTrace trace = new();
UIRoot root = new(invalidationTrace: trace);
```

## Remarks

Root-owned mutable operations share `Relay` as their owner-thread authority. Resource, platform-service, image-cache, viewport, theme, and invalidation methods reject off-thread calls before changing retained state. Use `Relay.Post` or `Relay.InvokeAsync` to request those mutations from a worker thread.

Notifications from an assigned `ThemeProvider` and `IObservableResourceProvider` are the deliberate exceptions because they describe external source changes rather than direct root mutations. UI-thread notifications retain their synchronous behavior. Off-thread theme bursts are coalesced into one aspect refresh that reads the current theme on the UI thread. Off-thread resource notifications are posted individually and retain FIFO order so resource deltas are not collapsed. Replacing either provider invalidates callbacks queued by the old subscription.

`UIRoot` is the owner object for a retained Cerneala UI tree. The constructor captures its calling thread as the owner of `Relay`, initializes the other root services, registers the default aspect package, marks the root as a layout boundary, and attaches the root element to itself through the element lifecycle.

Children are attached by adding them to the inherited `VisualChildren` or `LogicalChildren` collections. Attached subtrees receive root ownership and element IDs through `ElementIds`; removing a subtree detaches it, releases its IDs, and removes its pending work from every root-owned queue.

The root viewport is stored in `ViewportWidth`, `ViewportHeight`, and the root-level `Scale` property. `SetViewport` updates those values and increments `TreeVersion`. Adding, removing, or moving attached children also increments `TreeVersion`, which invalidates cached visual queue order and semantics. The root `Scale` property hides `UIElement.Scale`; on `UIRoot`, it represents viewport scale rather than the inherited render scale UI property.

Invalidation requests are expanded through `DirtyPropagation` and queued into the root-owned layout, inherited property, command-state, aspect, render, and hit-test queues. Invalidation tracing is disabled by default so routine invalidation does not allocate diagnostic entries in the frame hot path. Pass an enabled `InvalidationTrace` to the constructor when retained invalidation history is required. Queue snapshots share one visual preorder index per `TreeVersion`, while idle `HasWork` checks read queue counts without tree traversal. Render invalidation clears the retained render root, hit-test invalidation clears the input cache, and semantics invalidation marks the cached semantics tree dirty.

`ProcessFrame` verifies Relay access, drains one stable Relay snapshot, and then runs scheduled frame work through `Scheduler`. Relay invalidations participate in that same frame; callbacks posted during the drain remain queued for the next call. If the scheduler has work or `Motion` has active motion, the frame is processed with the root motion frame coordinator.

`GetSemanticsTree` caches the generated `SemanticsTree` until semantics become dirty or `TreeVersion` changes.

## Constructors

| Name | Description |
| --- | --- |
| `UIRoot(float viewportWidth = 0, float viewportHeight = 0, float scale = 1, IMotionClock? motionClock = null, ReducedMotionPolicy? reducedMotion = null, UiRelayOptions? relayOptions = null)` | Initializes a root with viewport dimensions, viewport scale, and optional motion and Relay configuration. Invalidation tracing is disabled. |
| `UIRoot(InvalidationTrace invalidationTrace, float viewportWidth = 0, float viewportHeight = 0, float scale = 1, IMotionClock? motionClock = null, ReducedMotionPolicy? reducedMotion = null, UiRelayOptions? relayOptions = null)` | Initializes a root with an enabled or disabled caller-owned invalidation trace plus optional viewport, motion, and Relay configuration. Throws `ArgumentNullException` when `invalidationTrace` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `AspectProcessor` | `AspectProcessor` | Processes aspect rules for elements in the root tree. |
| `AspectQueue` | `AspectQueue` | Queues elements that need aspect processing. |
| `AspectRegistry` | `AspectRegistry` | Stores aspect packages for the root; the default aspect package is registered during construction. |
| `CommandStateQueue` | `CommandStateQueue` | Queues elements whose command state must be refreshed. |
| `ElementIds` | `ElementIdProvider` | Provides stable element IDs for attached elements. |
| `HitTestQueue` | `HitTestQueue` | Queues hit-test cache work. |
| `ImageLoader` | `IImageLoader?` | Gets the image loader assigned by `SetImageLoader` or `SetImageResourceCache`. |
| `ImageResourceCache` | `ImageResourceCache?` | Gets the image cache assigned by `SetImageLoader` or `SetImageResourceCache`. |
| `InheritedPropertyPropagator` | `InheritedPropertyPropagator` | Propagates inherited UI property values through the tree. |
| `InheritedPropertyQueue` | `InheritedPropertyQueue` | Queues inherited property propagation work. |
| `InputCache` | `ElementInputCache` | Stores and invalidates input route and hit-test cache state for the root tree. |
| `LayoutManager` | `LayoutManager` | Creates and runs layout phase processors for the root. |
| `LayoutQueue` | `LayoutQueue` | Queues measure and arrange work. |
| `Motion` | `MotionSystem` | Coordinates motion values and motion frames for the root. |
| `PlatformServices` | `IPlatformServices` | Gets the platform services assigned by `SetPlatformServices`, or the empty platform services object when none are assigned. |
| `Relay` | `UiRelay` | Gets the root-owned queue used to marshal callbacks to the UI thread captured during construction. |
| `RenderCounters` | `RenderCounters` | Tracks render counters for retained rendering. |
| `RenderQueue` | `RenderQueue` | Queues render cache work. |
| `RenderQueueProcessor` | `RenderQueueProcessor` | Processes render queue entries into the retained render cache. |
| `ResourceDependencyTracker` | `ResourceDependencyTracker` | Tracks resource dependencies and maps resource changes to invalidation effects. |
| `ResourceProvider` | `IResourceProvider?` | Gets the resource provider assigned by `SetResourceProvider`. |
| `RetainedRenderCache` | `RetainedRenderCache` | Stores retained render data for the tree. |
| `RetainedRenderer` | `RetainedRenderer` | Renders retained draw command lists from the root render cache. |
| `Scale` | `float` | Gets the root viewport scale. This property hides `UIElement.Scale`. |
| `Scheduler` | `UiFrameScheduler` | Coordinates retained frame phases for queued root work. |
| `ThemeProvider` | `ThemeProvider?` | Gets the current theme provider assigned by `SetThemeProvider`. |
| `Trace` | `InvalidationTrace` | Gets the supplied invalidation trace, or the disabled trace when none was supplied. |
| `TreeVersion` | `int` | Gets the root tree version. It changes when the tree or viewport changes. |
| `ViewportHeight` | `float` | Gets the viewport height assigned at construction or by `SetViewport`. |
| `ViewportWidth` | `float` | Gets the viewport width assigned at construction or by `SetViewport`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetSemanticsTree()` | `SemanticsTree` | Builds or returns the cached semantics tree for the root. |
| `Invalidate(InvalidationRequest request)` | `void` | Records and propagates an invalidation request through the root queues. Throws if `request` is `null`. |
| `ProcessFrame(FramePhaseProcessors? processors = null, FrameBudget budget = default, FrameStats? stats = null, MotionFrameReason motionReason = MotionFrameReason.Scheduled)` | `FrameStats` | Drains one Relay snapshot, processes one retained UI frame on the owning thread, and returns the statistics object used for the frame. |
| `SetImageLoader(IImageLoader? loader)` | `void` | Sets the image loader and creates a matching `ImageResourceCache`; clears the old cache when replaced. |
| `SetImageResourceCache(IImageLoader? loader, ImageResourceCache? cache)` | `void` | Sets the image loader and image cache pair, clears the old cache when the cache changes, and invalidates resource/render state. |
| `SetPlatformServices(IPlatformServices? services)` | `void` | Sets platform services, falling back to empty services for `null`, and syncs reduced-motion mode when available. |
| `SetResourceProvider(IResourceProvider? provider)` | `void` | Sets the root resource provider, updates observable resource-change subscriptions, and invalidates root resource state. Off-thread resource deltas are dispatched FIFO through `Relay`. |
| `SetThemeProvider(ThemeProvider? provider)` | `void` | Sets the theme provider, updates theme-change subscription, and invalidates aspect state for the subtree. Off-thread theme bursts are coalesced through `Relay`. |
| `SetViewport(float width, float height, float scale)` | `void` | Updates viewport width, height, and root scale, then increments `TreeVersion`. |

## Explicit Interface Implementations

| Name | Description |
| --- | --- |
| `IElementHost.Root` | Returns this `UIRoot` instance. |

## Applies to

Cerneala retained UI runtime.

## See Also

- `UI/Elements/UIRoot.cs`
- `UI/Elements/UIElement.cs`
- `UI/Hosting/UiHost.cs`
- `UI/Invalidation/UiFrameScheduler.cs`
- `UI/Relay/UiRelay.cs`
