# Detective Class

Namespace: `Cerneala.UI.Detective`

Assembly: `Cerneala.dll`

Source: [`UI/Detective/Detective.cs`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Detective/Detective.cs)

## Definition

Provides the root-owned entry point for runtime snapshots, traces, and counters.

```csharp
public sealed class Detective
```

## Examples

```csharp
using Cerneala.UI.Detective;
using Cerneala.UI.Elements;

UIRoot root = new(1280, 720, 1.25f);
FrameStats stats = root.ProcessFrame();

DetectiveSnapshot snapshot = root.Detective.Capture(stats);
string summary = root.Detective.Format(snapshot);
```

## Remarks

Every `UIRoot` creates exactly one `Detective`. Capture methods copy current retained state; they do not rebuild caches, load resources, or invalidate the tree. Domain systems continue to produce their own evidence, but `Detective` is the public root-level owner used to inspect it.

Invalidation tracing is disabled by the default `UIRoot` constructor. Supply an `InvalidationTrace` when constructing the root to retain invalidation entries.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `AspectCounters` | `AspectEngineCounters` | Gets the counters collected by the root's Aspect engine. |
| `Invalidation` | `InvalidationTrace` | Gets the root's retained invalidation trace. |
| `Motion` | `MotionDiagnostics` | Gets Motion counters, warnings, phases, and trace state for the root. |
| `RenderingCounters` | `RenderCounters` | Gets retained-rendering cache and rebuild counters. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `Capture(FrameStats stats)` | `DetectiveSnapshot` | Captures one aggregate snapshot for the root and supplied frame statistics. |
| `CaptureAspect(UIElement element)` | `AspectDiagnostics.Snapshot` | Captures the latest Aspect resolution evidence for an element. |
| `CaptureFrame(FrameStats stats)` | `FrameDiagnosticsSnapshot` | Copies retained frame counters. |
| `CaptureInput(UIElement? hitTarget, RoutedEvent? routedEvent = null)` | `InputDiagnosticsSnapshot` | Captures hit-target and routed-event input state. |
| `CaptureLayout(UIElement element)` | `LayoutDiagnosticsSnapshot` | Captures current layout state for an element. |
| `CaptureMotion()` | `MotionGraphSnapshot` | Captures current Motion graph and latest-frame activity. |
| `CaptureRendering()` | `RootRenderDiagnosticsSnapshot` | Captures retained root render-cache state. |
| `CaptureRendering(UIElement element)` | `ElementRenderDiagnosticsSnapshot` | Captures retained render-cache state for an element. |
| `CaptureTileMap(TileMap2D map)` | `TileMapDiagnosticsSnapshot` | Copies the latest tilemap recording counters. The map must be attached to this root; null throws `ArgumentNullException`, detached/foreign-root maps throw `ArgumentException`. Does not record, invalidate, synchronize, or reset the map. |
| `Format(DetectiveSnapshot snapshot)` | `string` | Returns the aggregate snapshot's invariant diagnostic line. |
| `TraceAspect(UIElement element, UiProperty property)` | `AspectTraceSnapshot` | Captures a property-specific Aspect resolution trace. |
| `TraceRoutedEvent(UIElement target, RoutedEvent routedEvent, ElementChildRole role = ElementChildRole.Visual)` | `RoutedEventTraceSnapshot` | Builds the routed-event path without raising the event. |

## See also

- [`DetectiveSnapshot`](Cerneala.UI.Detective.DetectiveSnapshot.md)
- [`UIRoot`](Cerneala.UI.Elements.UIRoot.md)
- [`TileMapDiagnosticsSnapshot`](Cerneala.UI.Detective.TileMapDiagnosticsSnapshot.md)
