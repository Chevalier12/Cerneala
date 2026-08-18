# Prism — MonoGame composer

## Purpose

Run the Prism graph on MonoGame/WindowsDX, with clear state ownership,
frame-local transient surfaces and compiled shaders at build. Stage delivers
GPU engine and underlying kernels; the retained cache is built separately
over these contracts.

**Dependency:** `2026-07-18-prism-retained-composition-graph.md`.

## Stage 0 — baseline GPU and shader pipeline

- [x] Extends existing tests
  `tests/Cerneala.Tests/Drawing/MonoGame/MonoGameDrawingBackendStateTests.cs`
  with RED cases for full ownership, exceptions, restore and consecutive render.
- [x] Add a local spike, small and deleted after decision, that checks the tool
  MonoGame effects compilation compatible with `3.8.4.1` pack.
- [x] Fix pipeline contract: `.fx` sources versioned under
  `Drawing/MonoGame/Prism/Shaders/`, deterministic compilation to build,
  `.mgfxo` embedded in assembly and loading from bytes.
- [x] Pin the build tool to the repository and add a clean check
  build/CI detecting missing or stale artifacts; prohibit compilation to
  runtime and the application's `ContentManager` dependency.
- [x] Check the minimal copy/composite shader through an integration test
  WindowsDX before building the kernel registry.

### Gate stage 0

- [x] A clean checkout can deterministically produce and load the minimal shader,
  no manually generated files or globally installed tools by default.

## Stage 1 — ownership and status restoration

- [x] Move ownership of top-level `SpriteBatch.Begin/End` to
  `MonoGameDrawingBackend.Render`; update
  `UI/Hosting/MonoGame/MonoGameUiHost.cs` to stop opening the external batch.
- [x] Update `WindowsDxWindowGraphicsSession` and `RenderPng` path to
  the same contract, without two divergent implementations.
- [x] Capture and restore to `finally` render targets, viewport, scissor,
  blend/depth/rasterizer/sampler state and any state modified by the executor.
- [x] Defines explicitly who owns `SpriteBatch`, `GraphicsDevice` and
  the lifetime of the backend; validate options in
  `MonoGameUiHostOptions`.
- [x] Add tests for exception in the middle of a pass, device state
  pre-existing, two sequential hosts and frames without Prism.

### Gate stage 1

- [x] Upon success or exception, the host receives exactly the documented state again
  The UI without Prism produces the same output as the baseline.

## Stage 2 — frame-local surfaces

- [x] Add `Drawing/MonoGame/Prism/Surfaces/PrismSurfacePool` with typed keys
  for size, format, samples and color space.
- [x] Pool reuses only compatible resources, releases leases in
  `finally` and evacuate resources at resize/device reset/dispose.
- [x] Explicitly defines the contract by which a finished surface can be
  subsequently promoted to an owner retained, without the transient pool to them
  recycle content.
- [x] Apply the backend-neutral calculated lifetime and peak surfaces plan;
  the executor does not recalculate the graph or the liveness.
- [x] Introduce public options only for measurable limits needed now;
  the final numerical values ​​remain unfixed until the hardening benchmarks.
  (It was not necessary: the transient limit is derived from
  `PeakLiveSurfaces`, without an arbitrary public value.)
- [x] Add tests for reuses, incompatible sizes/formats, exceptions,
  resize, device reset and bounded growth on thousands of frames.

### Gate stage 2

- [x] The resource counter resets to zero active leases after each frame and
  GPU memory does not grow uncontrollably in the stress test.

## Stage 3 — executor and core kernels

- [x] Add `Drawing/MonoGame/Prism/Execution/PrismGraphExecutor` that consumes
  exclusive of the optimized graph and surface plane.
- [x] Implements the fundamental passes: capture, copy, clear, normal
  composite, mask alpha, clip alpha, color conversion and present.
- [x] Add `PrismKernelRegistry` generated/validated against the catalog, but
  register only fundamental kernels at this stage.
- [x] Centralize type parameter bind and alpha/UV/pixel conventions
  size; does not allow string uniforms in the per-frame loop.
- [x] Avoid `GetData`, CPU readback, hidden flushes and creating
  `Effect`/`RenderTarget2D` during a pass.
- [x] Link capability errors to `PrismFallbackPolicy` and diagnostics,
  no catch that silently transforms the effect into another result.

### Gate stage 3

- [x] A simple composition captures control once, runs
  offscreen and composes correctly without managed allocations after warmup.

## Stage 4 — basic diagnostics and compliance

- [x] Internally displays counters for passes, captures, surfaces created/reused,
  peak live surfaces, fallback and CPU time to submit.
- [x] Add deterministic dump of executed graph and correlation with
  `PrismFrameAnalysis`, without exposing GPU objects to the public.
- [x] Build minimal test scenes for normal blend, opacity, fill,
  mask, clip, nested Prism and transform.
- [x] Capture the result exclusively through the API
  `IWindowPlatform.RenderPng`/WindowsDX and compare it with versioned goldens,
  with profile, alpha and tolerance declared.
- [x] Add tests for device lost/reset and disposal during navigation.

### Gate stage 4

- [x] Semantic tests and core images are green on WindowsDX again
  diagnostics confirm the absence of unnecessary passes and surfaces.

## Stage 5 — API docs and verification
- [x] Updates with the `writing-api-documentation` skill
  `MonoGameDrawingBackend`, `MonoGameUiHostOptions` and any public contract
  changed; also corrects the old statement that the backend does not own
  `Begin/End`.
- [x] Sync `docs-site/documentation/manifest.json`.
- [x] Run reindexing after every C# batch/project.
- [x] Running
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "MonoGame|Prism"`,
  `dotnet test .\Cerneala.slnx` and a clean build that recompiles the shaders.
- [x] Run `git diff --check` and check for unexplained binaries or
  shaders are compiled at runtime.

## The definition of done

- [x] The backend owns and restores the state, the executor runs the underlying graph,
  and the pool is bounded and exception/reset safe.
- [x] The shader pipeline is reproducible from clean checkout.
- [x] Core GPU tests, documentation and gates are green.