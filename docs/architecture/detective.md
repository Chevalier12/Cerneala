# Detective

> Code: `UI/Detective`, `UI/Elements/UIRoot.cs` (`Detective`, `Trace`) · Verified at commit `0cb32300` (2026-10-10)

## Responsibility

Detective is the public read-only window into a root's runtime evidence: frame counters, invalidation and motion traces, render, input, layout and Aspect state, resources, platform services, tile maps and audio. The subsystems produce the evidence; Detective only exposes it. It does not drive, fix or invalidate anything.

Example: after `FrameStats stats = root.ProcessFrame();`, the call `root.Detective.Capture(stats)` returns one `DetectiveSnapshot`. `root.Detective.Format(snapshot)` turns it into one line such as `detective viewport=320x180, scale=1.5, frame inherited=0, ...`.

## Components

| Type | File | Role | Owner |
|---|---|---|---|
| `Detective` | `UI/Detective/Detective.cs` | Facade; internal constructor | `UIRoot` (`UIRoot.Detective`, created in the `UIRoot` constructor, never replaced) |
| `DetectiveSnapshot` | `UI/Detective/Detective.cs` | Record of 7 members: `Viewport`, `Frame`, `Input`, `Rendering`, `Resources`, `Platform`, `Motion` | caller |
| `InvalidationTrace` | `UI/Detective/InvalidationTrace.cs` | Opt-in list of invalidation requests, propagation, queueing and phases | `UIRoot` (`Detective.Invalidation`; the same instance is the scheduler's trace) |
| `MotionDiagnostics` | `UI/Detective/MotionDiagnostics.cs` | Motion phases, warnings, counters, opt-in `MotionTrace` | `MotionSystem`; exposed as `Detective.Motion` |
| `AspectEngineCounters` | `UI/Detective/AspectEngineCounters.cs` | Rule, condition, declaration, token and cache counters | `AspectEngine`; exposed as `Detective.AspectCounters` |
| `RenderCounters` | `UI/Rendering/RenderCounters.cs` | Render cache hits, misses and command counts | `UIRoot`; exposed as `Detective.RenderingCounters` |
| `FrameDiagnostics`, `InputDiagnostics`, `LayoutDiagnostics`, `RenderDiagnostics`, `RoutedEventTrace`, `AspectTrace` | `UI/Detective/` | Static builders behind the `Capture*` and `Trace*` methods | stateless |
| `ElementTreeDumper`, `DirtyTreeDumper`, `RenderCacheDumper`, `DebugOverlay`, `DebugAdorner` | `UI/Detective/` | Text dumps and a visual overlay | caller |

Naming trap: `Detective.Motion` is the `MotionDiagnostics` object. `DetectiveSnapshot.Motion` is a `MotionGraphSnapshot` (counts). Likewise `DetectiveSnapshot.Input` is a `RootInputDiagnosticsSnapshot`, while `Detective.CaptureInput(...)` returns an `InputDiagnosticsSnapshot`.

## Data Flow

`Detective.Capture(FrameStats stats)` reads current state once and builds every member eagerly:

| Member | Source |
|---|---|
| `Viewport` | `UIRoot.ViewportWidth`, `ViewportHeight`, `Scale` |
| `Frame` | `FrameDiagnostics.Capture(stats)`: the counters in the `FrameStats` you pass |
| `Input` | `UIRoot.InputCache`: `IsDirty`, `RebuildCount`, `LastInvalidationReason` |
| `Rendering` | `UIRoot.RetainedRenderCache`: `IsRootValid`, `Version`, root command count |
| `Resources` | `UIRoot.ImageResourceCache`: present or not, `LoadCount` |
| `Platform` | `UIRoot.PlatformServices`: whether clipboard, cursor, file dialogs, text input, DPI and accessibility services exist |
| `Motion` | `MotionDiagnostics.CreateSnapshot` |

Each other method answers one question when called:

- `CaptureFrame(stats)`, `CaptureInput(hitTarget, routedEvent)`, `CaptureLayout(element)`, `CaptureRendering()` / `CaptureRendering(element)`.
- `CaptureAspect(element)` and `TraceAspect(element, property)`: which Aspect declaration won and why.
- `CaptureMotion()`.
- `CaptureTileMap(map)`: throws `ArgumentException` when the map belongs to another root.
- `CaptureTimbre()`: returns `null` when the root has no `TimbreRuntime`.
- `TraceRoutedEvent(target, routedEvent, role)`: the route an event would take.

## Lifecycle And Ownership

- One `Detective` per `UIRoot`, for the root's whole life. It keeps a strong reference to the root.
- The invalidation trace is chosen when the root is built:
  - `new UIRoot(...)` uses the shared `InvalidationTrace.Disabled`, which records nothing.
  - `new UIRoot(new InvalidationTrace(), ...)` enables it. Default capacity is `InvalidationTrace.DefaultCapacity` = 4096 entries. When full, it drops the oldest `max(1, Capacity / 4)` entries.
- The motion trace is off until `root.Detective.Motion.IsEnabled = true`. While on, `MotionTrace` grows without a limit. `Phases` and `Warnings` are cleared at the start of every Motion frame.

## Frame Integration

Detective adds no frame phase. The evidence it reads is written during the frame by the owners:

- the scheduler, through `FrameStats` and `InvalidationTrace`;
- the Motion coordinator, through `MotionDiagnostics`;
- the Aspect engine and the render cache builders, through their counters.

Capturing does not invalidate: calling `Capture` leaves the retained render cache valid and its version unchanged.

## Invariants

- One Detective per root, exposing invalidation, motion, Aspect and rendering evidence. `tests/Cerneala.Tests/UI/Detective/DetectiveTests.cs:RootOwnsOneDetectiveForItsRetainedSystems`, `:DetectiveOwnsTracingAndCounters`.
- One `Capture` covers all root-owned domains with current values. `DetectiveTests.cs:CaptureProducesOneSnapshotAcrossRootOwnedDomains`.
- Old diagnostic entry points are not public (`UIRoot.Trace`, `UIRoot.RenderCounters`, `MotionSystem.Diagnostics`, `AspectEngine.Counters`, `AspectEngine.GetDiagnostics`); the diagnostic types live in `Cerneala.UI.Detective`. `DetectiveTests.cs:LegacyDiagnosticEntryPointsAreNotPublic`.
- Capture does not invalidate the root. `tests/Cerneala.Tests/UI/Detective/DetectiveSnapshotTests.cs:DetectiveCaptureDoesNotInvalidateRoot`.
- Missing optional services are reported as absent, not as errors. `DetectiveSnapshotTests.cs:DetectiveHandlesMissingOptionalServices`.
- The invalidation trace records nothing unless opted in, and keeps only the newest entries up to its capacity. `tests/Cerneala.Tests/UI/Detective/InvalidationTraceTests.cs:RootDoesNotRetainInvalidationTraceUnlessOptedIn`, `:EnabledTraceRetainsOnlyTheNewestEntriesUpToItsCapacity` (capacity 3, five requests, keeps `request-2`…`request-4`).
- The motion trace records nothing while disabled. `tests/Cerneala.Tests/UI/Detective/MotionDiagnosticsTests.cs:DiagnosticsCanBeDisabledWithoutRecordingTraceEvents`.
- `UI/Detective` source does not mention the concrete backends `Skia` or `HarfBuzz`. `tests/Cerneala.Tests/UI/Rendering/ArchitectureBoundaryTests.cs:DetectiveDoesNotReferenceConcreteBackends` (a text check, not a reference check).
- Netestat: allocation cost of `Capture` (seven objects per call, read from the code; no test measures it).

## Prism And Backend Evidence

Detective has no Prism member. `PrismRendererDiagnostics` lives in `Cerneala.UI.Detective`, but no production code constructs it. Prism operational evidence is produced at the backend boundary (`PrismOperationalDiagnostics`, internal, captured by `WindowApplicationRuntime`).

## Known Limitations

- Some work runs even when no one reads it: `FrameStats` counters and phase timestamps, `MotionDiagnostics` phase and warning lists, `RenderCounters`, and Aspect diagnostics captured on every `AspectEngine` apply. No measurement exists for this cost (nemăsurat).
- `InvalidationTrace.RecordPhase` builds `phase.ToString()` before it checks `IsEnabled`.
- `MotionTrace.Clear()` has no production caller.
- `PrismRendererDiagnostics` is a public type with no production producer.
