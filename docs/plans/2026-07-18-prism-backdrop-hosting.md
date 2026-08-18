# Prism — backdrop and host integration

## Purpose

It allows Prism to render the game world and UI under control without
readback CPU and without confusing the backdrop with the control image.

**Dependencies:** `2026-07-18-prism-retained-composition-graph.md` and
`2026-07-18-prism-monogame-compositor.md`.

## Stage 0 — RED contracts

- [x] Add backend-neutral RED tests for zero/one backdrop, the obligation to
  be the last direct child and exclude own/overlapping content.
- [x] Add host RED tests for zero acquisition when not required, exactly one
  per frame when several compositions use it and release in `finally`.
- [x] Fix through tests the order: game world + lower UI → backdrop plane
  → control content → top UI.
- [x] Add RED cases for host without provider, resize, source replacement,
  nested Prism, Hidden/Collapsed and exception in executor.

### Gate stage 0

- [x] The tests clearly distinguish "capture control" from "backdrop frame" and do not assume
  a MonoGame implementation in backend-neutral contracts.

## Stage 1 — host contracts

- [x] Add a minimum public contract `IBackdropFrameSource` and a lease
  frame-scoped readonly, no generic texture ownership API.
- [x] Defines required metadata: size, pixel scale, color profile,
  alpha/format, coordinate transform, `ContentVersion` monotone and lifetime up to
  at the end of the frame.
- [x] Add the optional provider in `IUiBackend`/`MonoGameUiHostOptions` or in
  the smallest host contract already responsible for frame acquisition;
  avoid service locator and global singleton.
- [x] Validates provider/backend compatibility at host startup and offers
  clear diagnosis when backdrop cannot be provided.
- [x] Explicitly documents that the application retains ownership of the scene, and
  Cerneala borrows only the already rendered frame.

### Gate stage 1

- [x] Host without Prism/backdrop doesn't need to implement anything new
  mandatory, and the lease cannot survive the frame.

## Stage 2 — Coordinated Analysis Acquisition

- [x] Use exclusively `PrismFrameAnalysis` to decide if the frame requires
  backdrop; do not rescan `DrawCommandList`.
- [x] Purchase at most one lease per frame and put it in
  `DrawingFrameContext` for all compatible consumers.
- [x] Skip purchase when all backdrops are invisible, clipped-out or
  removed by the optimizer.
- [x] Release the lease in `finally` after submit, including exceptions and
  device reset.
- [x] Add counters for requested/acquired/shared/skipped/failed and tests
  for each path.

### Gate stage 2

- [x] A frame without need makes zero calls, and a frame with any amount
  Compatible backdrops make one purchase.

## Stage 3 — graph semantics
- [x] Extends the graph with a separate backdrop input and clipping it into
  coordinates of the control, without turning it into layer `Source`.
- [x] Process the filters/styles/mask/properties declared in `@backdrop`
  according to the catalog and compose the result before the control layers.
- [x] Respect UI order: the backdrop only sees what the host has completed under
  control, not the siblings drawn afterwards or the content of the control itself.
- [x] Apply color profile and alpha metadata only once; reject times
  observably degrades incompatible formats through central policy.
- [x] Add graph snapshots for a backdrop, more controls,
  nested groups and invisible layer.

### Gate stage 3

- [x] The graph does not contain cycles and shows explicitly what frame backdrop it is
  split and where it is cropped/converted.

## Step 4 — MonoGame adapter

- [x] Implements the WindowsDX adapter that provides the already rendered scene texture
  or an explicit GPU resolver; prohibits `GetData` and CPU copying.
- [x] Integrate the order in `WindowsDxWindowGraphicsSession` and the normal path
  `MonoGameUiHost.Draw`, keeping the same contract for `RenderPng`.
- [x] Reuse the frame-local lease between compositions and allocate only
  the intermediate surfaces required by the graph.
- [x] Manage resize, MSAA resolve, format/color mismatch, device reset and
  provider replacement without orphaned resources.
- [x] Propagate `ContentVersion`, lower UI versions and raster metadata in
  the dependency stamp consumed by the retained cache plan; the host does not retain
  source texture by frame.

### Gate stage 4

- [x] The backdrop runs completely GPU-side, only once per frame, and
  surfaces and leases are released at all exits.

## Stage 5 — conformance, lifecycle and API docs

- [x] Add auto cutscenes with recognizable gameplay/background, inferior UI,
  control with blur/color backdrop and top UI unaffected.
- [x] Capture via `IWindowPlatform.RenderPng` and check goldens for
  coordinates, blur edges, alpha, resize and two controls that divide the frame.
  (The actual contract in the repo is `IWindowScreenshotSource.RenderPng`, used by
  runtime over the session created by `IWindowPlatform`.)
- [x] Runs navigation stress, hide/unhide, provider replacement and device
  reset with resource counters and `WeakReference`.
- [x] Documents public contracts with the skill
  `writing-api-documentation`, update `IUiBackend`,
  `MonoGameUiHostOptions`, the backdrop types and the manifest.
- [x] Run reindex after every C# batch/project.
- [x] Running
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "PrismBackdrop|MonoGameUiHost|RenderPng"`,
  `dotnet test .\Cerneala.slnx` and `git diff --check`.

## The definition of done
- [x] Prism can render the game world/lower UI via a lease
  frame-scoped, with no readback, and exposes all versions needed for reuse
  correctness of the processed result between frames.
- [x] Order, sharing, failure paths, lifecycle and documentation are
  checked automatically.