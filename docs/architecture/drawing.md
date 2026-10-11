# Drawing

> Code: `Drawing` (without `Drawing/Prism` and `Drawing/Text`), `DrawCore` in `UI/Hosting/UiHost.cs` · Verified at commit `0cb32300` (2026-10-10)

## Responsibility

The drawing layer records drawing intent as a flat list of backend-neutral
commands and hands that list to a backend. It is not a UI tree: it does not own
layout, input, control state, Aspect, Motion or element lifecycle. Controls
never call SDL or GPU APIs; they record commands through `DrawingContext`, and
only an `IDrawingBackend` turns commands into pixels.

## Components

| Type | File | Role | Owner |
|---|---|---|---|
| `DrawingContext` | `Drawing/DrawingContext.cs` | Records commands into one `DrawCommandList`: fills, strokes, paths, text, images (`:23-187`), state pairs `PushClip`/`PopClip`, `PushTransform`/`PopTransform`, `PushOpacity`/`PopOpacity`, `PushBlend`/`PopBlend`, `PushLayer`/`PopLayer` (`:216-271`), and scoped forms `Transform`, `Clip`, `Opacity`, `Blend`, `Layer` (`:273-321`). | Whoever records; rendering creates one per `Render` call (`UI/Rendering/ElementRenderCache.cs:98`) |
| `DrawCommand` | `Drawing/DrawCommand.cs` | One command value (`readonly partial record struct`) with its kind and payload. | The list that holds it |
| `DrawCommandList` | `Drawing/DrawCommandList.cs` | Flat ordered list of `DrawCommand` (`IReadOnlyList<DrawCommand>`) with a `Version` number. | Its creator (for example `ElementRenderCache`, `RetainedRenderCache`) |
| `DrawTransformScope`, `DrawClipScope`, `DrawOpacityScope`, `DrawBlendScope`, `DrawLayerScope` | `Drawing/DrawState.cs:48-146` | `ref struct` handles returned by the scoped methods; `Dispose` writes the matching pop. | The recording method |
| `DrawCommandStateAnalyzer`, `DrawCommandStateAnalysis`, `DrawCommandStateEntry` | `Drawing/DrawState.cs:148-225` | Computes, for every command, its world `Transform`, `Bounds`, `ClipBounds`, `Opacity`, `BlendMode` and the index of its matching push or pop. | One draw frame |
| `IDrawingBackend` | `Drawing/IDrawingBackend.cs` | One method: `Render(DrawCommandList commands, in DrawingFrameContext frameContext)`. | The window graphics session |
| `DrawingFrameContext` | `Drawing/DrawingFrameContext.cs` | Per-draw data passed with the commands: public `StateAnalysis` and `BackdropLease`; internal Prism analysis, backdrop source token and Prism cache invalidation queue. Both constructors are `internal`. | One `UiHost.Draw` call |
| `IBackdropFrameSource`, `IBackdropFrameLease` | `Drawing/Prism/IBackdropFrameSource.cs` | Source and lease of the backdrop image that Prism scopes can read. | The backend; the lease lives for one draw |

Production implementer of `IDrawingBackend`: only `SdlGpuDrawingBackend`
(`Cerneala.Backends.SdlGpu/Gpu/SdlGpuDrawingBackend.cs:13-16`). Tests use
`tests/Cerneala.Tests/UI/Hosting/FakeDrawingBackend.cs`. The SDL backend also
implements the internal `IDrawingBackendFrameTimingSource`
(`Drawing/DrawingBackendFrameTiming.cs:3`), which
`WindowApplicationRuntime` reads for frame timing
(`UI/Hosting/Windowing/WindowApplicationRuntime.cs:799-801`).

## Data Flow

1. **Recording.** An element's `Render` gets a `RenderContext` whose
   `DrawingContext` writes into the element's local list (see
   [Rendering](rendering.md)). Every `Add` appends one `DrawCommand` and
   increments `DrawCommandList.Version`.
2. **Composition.** `DrawCommandListBuilder` copies the local commands into the
   root list and writes the element's transform, clip and Prism scope as
   push/pop commands around them ([Rendering](rendering.md)). The root list
   stays flat; nesting exists only as matching push and pop commands.
3. **Analysis.** `UiHost.DrawCore` (`UI/Hosting/UiHost.cs:281-311`) takes the
   committed root list (`RetainedRenderer.Render`) and calls
   `PrismFrameAnalyzer.Analyze`. The resulting `PrismFrameAnalysis` holds the
   Prism scopes, the backdrop requirement and a `DrawCommandStateAnalysis`
   (`Drawing/Prism/Graph/PrismFrameAnalysis.cs:9-26`).
4. **Backdrop.** `UiHost.AcquireBackdropFrame` (`UiHost.cs:331`) leases a
   backdrop frame only when the analysis has a backdrop requirement and an
   `IBackdropFrameSource` is configured. Otherwise the lease is `null`.
5. **Frame context.** `UiHost` builds the `DrawingFrameContext` from the
   analysis, the lease, the source token and the root's
   `PrismCacheInvalidations` (`UiHost.cs:297-301`).
6. **Submission.** `RetainedRenderer.Submit` calls
   `DrawingFrameContext.EnsureCurrent(commands)` and then
   `backend.Render(commands, in frameContext)`. `EnsureCurrent` throws when the
   list is not the same object, or its `Version` or `Count` differs from the
   analysis (`Drawing/DrawState.cs:211-220`, `PrismFrameAnalysis.cs:42-59`).
7. **Backend.** `SdlGpuDrawingBackend.Render`
   (`SdlGpuDrawingBackend.cs:118-145`) throws `ObjectDisposedException` after
   dispose, throws when no window frame is active, checks `EnsureCurrent`
   again, processes Prism cache invalidations directly when the frame has no
   Prism scopes, returns early when the session is suspended or the list is
   empty, and then reads
   `frameContext.StateAnalysis`.
8. **End.** `UiHost` disposes the backdrop lease in a `finally` block.

Example. This code records a scaled rectangle inside a translation:

```csharp
DrawCommandList commands = new();
DrawingContext drawing = new(commands);
drawing.PushTransform(Matrix3x2.CreateTranslation(10, 20));
drawing.PushTransform(Matrix3x2.CreateScale(2));
drawing.FillRectangle(new DrawRect(1, 2, 3, 4), Color.White);
drawing.PopTransform();
drawing.PopTransform();

DrawCommandStateAnalysis analysis = new DrawCommandStateAnalyzer().Analyze(commands);
```

- `commands` holds 5 commands: push, push, fill, pop, pop.
- `analysis.Entries[2].Bounds` is `DrawRect(12, 24, 6, 8)`: the rectangle is
  scaled by 2 first, then moved by (10, 20).
- `analysis.Entries[0].MatchingCommandIndex` is 4 and
  `analysis.Entries[1].MatchingCommandIndex` is 3.

## State Scopes

- A raw pop must match the kind of the innermost open scope. `PopClip` after
  `PushTransform` throws `"PopClip does not match the current drawing state
  scope."` (`DrawingContext.cs:353-364`).
- A scope handle must be disposed once, innermost first. Disposing an outer
  handle while an inner one is open throws `InvalidOperationException`
  (`DrawingContext.cs:323-335`); disposing a handle twice throws
  `ObjectDisposedException` (`DrawState.cs:59-65`).
- `DrawingContext` checks only the commands it recorded itself.
  `DrawCommandListBuilder` adds its push and pop commands to the root list
  directly. The whole list is checked by `DrawCommandStateAnalyzer`, which
  throws when pushes and pops of different kinds are not nested (message
  contains `"not LIFO"`).

## Lifecycle And Ownership

- A `DrawCommandList` belongs to whoever created it. The UI keeps one per
  element and one root list per `UIRoot` (see [Rendering](rendering.md)).
- `DrawingFrameContext` is a `readonly struct` created by the host for one
  draw. Its constructors are `internal`; besides `UiHost`, only the SDL
  backend's 2D surface path creates one
  (`Cerneala.Backends.SdlGpu/Gpu/SdlGpuDrawingBackend.Surface2D.cs:125-129`).
  `Tools/PrismAudit` reports an error if it gets a public constructor
  (`Tools/PrismAudit/Program.cs:522`).
- The backend owns GPU resources and the window session. The backdrop lease is
  disposed by `UiHost` after each draw.

## Frame Integration

Drawing has no `FramePhase`. Recording happens in the `RenderCache` phase,
composition at the end of `UiHost` update (`RetainedRenderer.Commit`), and
analysis plus submission in `UiHost.Draw`, after the update.

## Invariants

Test paths are under `tests/Cerneala.Tests/` unless they start with
`tests/Cerneala.Tests.SdlGpu/`.

- Every `Add` and every `Clear`, also of an empty list, increments
  `DrawCommandList.Version` by 1. Test:
  `Drawing/Prism/PrismCommandListContractTests.cs:StructuralVersionAdvancesDeterministicallyForEveryMutation`.
  The increments of the internal `ReplaceAt`, `AddRetained` and `Truncate`:
  netestat.
- Mixed push/pop kinds that are not nested are rejected with the command
  indexes. Test:
  `Drawing/DrawingStateTests.cs:AnalyzerRejectsMixedStackImbalanceWithPushIndex`.
- Scope handles must be disposed innermost first and only once. Test:
  `Drawing/DrawingStateTests.cs:RefScopesRequireLifoAndRejectDoubleDispose`.
- The analyzer composes the parent transform after the child transform and
  reports world bounds. Test:
  `Drawing/DrawingStateTests.cs:AnalyzerComposesParentThenChildAndReportsWorldBounds`.
- Analysis entries are read-only. Test:
  `Drawing/DrawingStateTests.cs:AnalyzerAllocatesOneOwnedEntrySnapshotAndKeepsItImmutable`.
- An analysis built for an older version of the list is rejected. Test:
  `Drawing/DrawingIntegrationLifecycleTests.cs:FullyRevalidatedEquivalentCommandsShareTheImmutableStateSnapshot`
  (on `PrismFrameAnalysis.StateAnalysis`).
- `UiHost.Draw` passes one frame context whose analysis matches the submitted
  list's `Version`. Test:
  `UI/Hosting/UiHostTests.cs:DrawSubmitsOneCurrentFrameAnalysisWithTheCommittedCommands`.
- A failed SDL frame does not break the next frame. Test:
  `tests/Cerneala.Tests.SdlGpu/DrawingBackendLifecycleContractTests.cs:FailedCompositingFrameDoesNotPoisonTheNextFrame`
  (native, opt-in with `CERNEALA_SDL_NATIVE_TESTS=1`).
- The public `DrawingFrameContext.StateAnalysis` property: netestat.
  `SdlGpuDrawingBackend.Render` after dispose throws: netestat.

## Diagnostic

The drawing layer itself exposes no Detective data. Render counters belong to
[Rendering](rendering.md); backend frame timing comes from
`IDrawingBackendFrameTimingSource`; Prism evidence is produced by the backend
(see [Prism technical design](prism-technical-design.md)).
