# game-loop-host-integration Specification

## Purpose
TBD - created by archiving change add-game-loop-host-integration. Update Purpose after archive.
## Requirements
### Requirement: Retained UI host owns frame integration
Cerneala SHALL provide a `UiHost` that owns the retained root, frame scheduler integration, input source, layout/render processing path, renderer submission path, and host services needed by applications.

#### Scenario: Host owns root and services
- **WHEN** a `UiHost` is created with a retained root and host options
- **THEN** the host exposes a single retained UI entry point for update and draw integration

#### Scenario: Host rejects missing root
- **WHEN** a host operation requires a retained root and no root has been configured
- **THEN** the host fails with a clear argument or invalid-operation error instead of silently doing nothing

### Requirement: Host frame data is explicit
Cerneala SHALL provide `UiFrame`, `UiViewport`, `UiHostOptions`, and `IUiClock` primitives for frame timing, viewport state, input frame state, and diagnostics.

#### Scenario: Frame captures update data
- **WHEN** `UiHost.Update(...)` completes
- **THEN** the host records a `UiFrame` containing elapsed time, viewport, input frame, and retained frame stats

#### Scenario: Viewport compares by value
- **WHEN** two `UiViewport` values have the same width, height, and scale
- **THEN** host viewport-change detection treats them as equal

#### Scenario: Clock can be faked
- **WHEN** tests provide a fake `IUiClock`
- **THEN** host update timing can be verified without using wall-clock time

### Requirement: Host update processes retained work
Cerneala SHALL process retained input capture, viewport application, visual state updates, layout queues, render queues, and diagnostics during `UiHost.Update(...)`.

#### Scenario: Update reads input source
- **WHEN** `UiHost.Update(...)` runs with an `IInputSource`
- **THEN** the host reads one `InputFrame` for that update

#### Scenario: Update can receive explicit input frame
- **WHEN** an explicit `InputFrame` is supplied to update
- **THEN** the host uses that frame instead of reading a second frame from the input source

#### Scenario: Update processes scheduler work
- **WHEN** retained work is queued before update
- **THEN** `UiHost.Update(...)` processes the retained root frame through the root scheduler

### Requirement: Host draw submits cached retained output
Cerneala SHALL provide `UiHost.Draw(...)` that renders the cached retained root `DrawCommandList` through an `IDrawingBackend` without forcing layout, arrange, hit-test, or render-cache scheduler work.

#### Scenario: Draw submits commands
- **WHEN** `UiHost.Draw(...)` is called with an `IDrawingBackend`
- **THEN** the host submits the retained root command list to that backend

#### Scenario: Draw does not process update work
- **WHEN** draw is called after an unchanged update
- **THEN** draw does not run measure, arrange, hit-test, or scheduler render-cache phases

#### Scenario: Missing backend fails clearly
- **WHEN** `UiHost.Draw(...)` is called without a drawing backend
- **THEN** the host fails with a clear argument or invalid-operation error

### Requirement: First frame primes retained layout and render
Cerneala SHALL ensure the first host update performs the retained measure, arrange, and render-cache work needed for the root to be drawable.

#### Scenario: First update performs retained work
- **WHEN** a host updates a newly assigned retained root for the first time
- **THEN** the update frame stats report retained layout or render-cache work instead of a no-work frame

#### Scenario: First draw has cached output
- **WHEN** draw is called after the first successful update
- **THEN** the root has a cached retained command list available for backend submission

### Requirement: Unchanged frames avoid retained regeneration
Cerneala SHALL keep later frames cheap when no retained state, viewport, input visual state, layout input, or render dependency has changed.

#### Scenario: Second unchanged update has no retained work
- **WHEN** a host processes two updates without retained changes between them
- **THEN** the second update reports no measure, arrange, render-cache, or hit-test work

#### Scenario: Second unchanged draw reuses cached output
- **WHEN** draw is called across unchanged frames
- **THEN** the cached retained root command list is reused instead of regenerated

### Requirement: Viewport changes invalidate retained output
Cerneala SHALL apply host viewport changes to the retained root and schedule the retained arrange/render work needed for the new viewport.

#### Scenario: Viewport size change invalidates root
- **WHEN** the host viewport width or height changes
- **THEN** the retained root receives the new viewport and arrange/render work is scheduled

#### Scenario: Viewport scale change invalidates root
- **WHEN** the host viewport scale changes
- **THEN** the retained root receives the new scale and arrange/render work is scheduled

#### Scenario: Same viewport does not requeue work
- **WHEN** the host receives the same viewport on a later update
- **THEN** no viewport-caused arrange or render work is queued

### Requirement: Host backend bridge is backend-neutral
Cerneala SHALL provide an `IUiBackend` host bridge that exposes backend input and drawing services without requiring core hosting to reference a concrete rendering library.

#### Scenario: Core host uses abstract backend services
- **WHEN** core hosting reads input or draws output
- **THEN** it does so through `IInputSource`, `InputFrame`, `IUiBackend`, and `IDrawingBackend`

#### Scenario: Core host has no MonoGame dependency
- **WHEN** core `UI/Hosting` files are compiled
- **THEN** they do not reference `Microsoft.Xna.Framework`, `SpriteBatch`, `Texture2D`, `GameTime`, or MonoGame static input APIs

### Requirement: MonoGame host adapter composes existing adapters
Cerneala SHALL provide a MonoGame hosting adapter that composes `MonoGameInputSource`, `MonoGameDrawingBackend`, and MonoGame content glue while keeping controls and core hosting free of MonoGame types.

#### Scenario: MonoGame host owns adapter setup
- **WHEN** a MonoGame application creates `MonoGameUiHost`
- **THEN** the adapter wires MonoGame input and drawing services into a core `UiHost`

#### Scenario: Text input can be queued
- **WHEN** MonoGame text input arrives
- **THEN** the MonoGame host adapter can queue it into `MonoGameInputSource`

#### Scenario: MonoGame content services stay in adapter namespace
- **WHEN** image or font service glue is needed for MonoGame
- **THEN** it lives under `UI/Hosting/MonoGame` instead of core retained UI or controls

### Requirement: Playground uses retained host path
Cerneala SHALL update the playground to exercise the retained UI host instead of only drawing immediate commands.

#### Scenario: Playground creates MonoGame host
- **WHEN** playground content is loaded
- **THEN** `Game1` creates a `MonoGameUiHost` and retained `UIRoot`

#### Scenario: Playground calls host update and draw
- **WHEN** the MonoGame game loop runs
- **THEN** `Game1.Update(...)` calls host update and `Game1.Draw(...)` calls host draw

### Requirement: Hosting integration is tested
Cerneala SHALL include focused tests for host primitives, frame contract, viewport behavior, and MonoGame boundary rules.

#### Scenario: Required hosting tests exist
- **WHEN** this implementation phase is complete
- **THEN** tests exist under `tests/Cerneala.Tests/UI/Hosting` for `UiHost`, frame contract, viewport behavior, MonoGame boundary behavior, fake clock, fake backend, and fake input source

#### Scenario: Full tests pass
- **WHEN** this implementation phase is complete
- **THEN** `dotnet test` passes

### Requirement: Host update dispatches retained input
Cerneala SHALL dispatch retained input through the input bridge during `UiHost.Update(...)` before retained scheduler processing.

#### Scenario: Input bridge runs before scheduler
- **WHEN** `UiHost.Update(...)` receives an input frame
- **THEN** retained input dispatch runs before layout, render-cache, and hit-test queue processing

#### Scenario: Input visual state can affect same frame work
- **WHEN** retained input dispatch changes hover, pressed, or focus visual state
- **THEN** the resulting invalidation is processed by the same update frame scheduler pass

#### Scenario: Host remains backend-neutral
- **WHEN** retained input dispatch is integrated into host update
- **THEN** core hosting continues to depend on input abstractions rather than MonoGame APIs

