# Plan: hardening for text texture cache and frame budget in CernealaPresentation

> Date: 2026-07-17
> Status: completed
> Dependency: Motion markup and existing CernealaPresentation automation
> Goal: we eliminate the repeated rerastering of the animated text and guarantee through a native benchmark that the loading of each relevant view remains within the 60 FPS budget.

## 1. Summary

The native profiling of CernealaPresentation demonstrated that all views except for
Welcome can exceed the budget of `16.6667 ms` when loading. The problem is not `Present()`
or VSync: `UiFrame.ProcessingTime` is captured before `Present()`, and the spikes
it is located in `Draw`.

The root cause is composed of three behaviors in the text pipeline:

- `MonoGameDrawingBackend.TextTextureKey` includes the exact subpixel phase of the position;
- Motion changes that phase almost every frame, producing repeated cache misses;
- `PruneInactiveTextTextureCaches()` evacuates after each frame all the textures that
  were not used in that frame, including the texts of temporarily collapsed views.

A cache miss executes synchronously `SkiaTextRasterizer.RasterizeSubpixel()`, creates
white/black references, three RGB layers, grayscale mask and four GPU textures.
The instrumented baseline observed approximately `4-6 MB` allocated on the frames
usual, `7-11 MB` on spikes and GC in 99 out of 102 frames over budget.

Two controlled experiments confirmed the cause:

- the neutralization of the subpixel phase strongly reduced allocations and spikes during Motion;
- neutralizing the phase plus keeping the cache between views left only the first one
  cold load; cycles 2 and 3 had zero exceedances, with warm maxima of approx
  ZZZ BLACK 10ZZZ.

The plan replaces the "last frame or garbage" cache policy with bounded retention,
uses canonical subpixel phases and optimizes cold rasterization. The native benchmark
used for diagnosis becomes permanent gate.

## 2. Objectives

- No rerastering of text just because a view was missing from a single frame.
- A finite and controlled number of subpixel variants for animated text.
- Bounded/LRU evacuation with correct `Dispose()` for all dependent GPU textures.
- Reduction of cold-path allocations, including the removal of the grayscale mask when the text is solid
  don't use it.
- Zero frames with `ProcessingTime > 16.6667 ms` at cold and warm load for Retained,
  Markup, Aspect, Motion, Frame Pipeline and Diagnostics on the native Release gate.
- Permanent benchmark, machine-readable and with non-zero exit code for regression.

## 3. Non-objectives

- We do not change the Motion semantics or the Presentation markup just to hide the cost.
- We do not remove subpixel text rendering and do not accept blurred text as an optimization.
- We do not introduce a global glyph atlas, GPU rasterization or worker pipeline
  asynchronous if the fixed bounded cache plus cold-path optimization satisfies the gate.
- We do not turn `UiFrame` into a public profiler API and do not add phase timings
  public only for benchmark.
- We do not guarantee 60 FPS for arbitrary hardware; the gate measures the same WindowsDX runtime,
  the same Release configuration and the same documented reference environment.

## 4. The proposed architecture

### 4.1 Canonical subpixel phase

`MonoGameDrawingBackend` will normalize the physical phase before building
`TextTextureKey`. The initial grid must be validated at 8 phases on the axis; if the inspection
pixel-diff shows that 4 phases are indistinguishable at the supported scales, you can choose
smaller grid.

The same canonical phase must be used both in keying and rasterization. It is not
legal for two positions to share the key, but the texture depends on the first exact position
which missed the cache.

The final draw position continues to use the actual baseline and existing mapping.
Quantization controls only the rasterized version, not the logical geometry.

### 4.2 Cache bounded between frames

The text cache will retain entries between frames and between view switches. Each one
entry will follow the last generation/frame in which it was used and the approximate cost
in bytes.

The evacuation will have two explicit limits:

- a memory/texture ceiling;
- a minimum retention period or an LRU policy that does not just evacuate the content
  because it was missing from the current frame.

When an input is evicted, the backend must release Red, Green, Blue and the mask
optional, then remove all brush textures dependent on the same key.
`CoordinateScale` change, device reset and `Dispose()` continues to immediately empty everything.

### 4.3 Cold rasterization

Solid text will not build the mask texture used exclusively by text brushes
not solid. The subpixel pipeline will avoid LINQ copies and unnecessary temporary buffers and
will use pooling only where the ownership of the buffer is after `Texture2D.SetData()`
clearly

The first appearance of a text remains synchronous at this stage, but the aggregate cost of
the first view load must satisfy the same gate of `16.6667 ms`. If the optimizations
local are not enough, the implementation stops and documents the remaining profile
before expanding the scope to prewarm/async rasterization.

### 4.4 Permanent native gate

`PresentationWindow.Automation.cs` will get a separate opt-in mode for frame budget.
He will manage the existing controls through automation peers, exclude Welcome and will
capture the first 45 frames after each switch.

A dedicated runner from `benchmarks/Cerneala.PresentationFrameBudget/` will start
CernealaPresentation in `Release`, will validate the JSON report and will fail if:

- a view does not produce the expected number of frames;
- an exception occurs or the process exceeds the timeout;
- any cold or warm frame has `ProcessingTime > 16.6667 ms`;
- the report includes Welcome or omits one of the six views;
- warm loads show rerastering/GC churn over the budgets established in stage 0.

The benchmark is a native Windows gateway, not a cross-platform unit test, and it does not run
default in `dotnet test`.

## 5. Estimated files

- `Drawing/MonoGame/MonoGameDrawingBackend.cs`
- `Drawing/Text/SkiaTextRasterizer.cs`
- `tests/Cerneala.Tests/Drawing/MonoGame/MonoGameDrawingBackendStateTests.cs`
- `tests/Cerneala.Tests/Drawing/TextPipelineTests.cs`
- `CernealaPresentation/PresentationWindow.Automation.cs`
- `benchmarks/Cerneala.PresentationFrameBudget/Cerneala.PresentationFrameBudget.csproj`
- `benchmarks/Cerneala.PresentationFrameBudget/Program.cs`
- `benchmarks/Cerneala.PresentationFrameBudget/README.md`
- `benchmarks/results/<data>-presentation-frame-budget/README.md`
- `Cerneala.slnx`

No public API change is planned. If the implementation requires members
public/protected new in Cerneala, the respective stage stops for review and
API documentation before continuing.

## 6. Implementation stages

### Stage 0 - Permanent RED Benchmark and baseline

- [x] Promote the temporary harness in a frame-budget opt-in mode in `PresentationWindow.Automation.cs`, without reflection and without markup changes made only for the test.
- [x] Captures for each sample: cycle, chapter, frame index, `ProcessingTime`, `ElapsedTime`, `FrameStats`, cold/warm and relative timestamp.
- [x] Excludes explicitly Welcome and runs Retained, Markup, Aspect, Motion, Frame Pipeline and Diagnostics in the actual navigation order.
- [x] Add the `benchmarks/Cerneala.PresentationFrameBudget` runner with 8 cycles, 45 frames per load, bounded timeout, JSON report and readable summary.
- [x] Make the runner output non-zero for any frame above `16.6667 ms`, incomplete report, asynchronous error or blocked process.
- [x] Record the RED baseline in `benchmarks/results/<data>-presentation-frame-budget/README.md`, including hardware, OS, configuration, maximums per view, counts over budget and exact order.
- [x] Confirm that the baseline reproduces allocations/GC associated with the draw through a separate profiling run; does not introduce permanent public phase timings.
- [x] Reindex after each C# or project-file change.

**Gate Stage 0**

- [x] The benchmark command runs end-to-end on the real WindowsDX window and fails RED for the observed cause, not for build, timeout or fixture failure.
- [x] The report contains exactly the six required views, separated cold/warm, and preserves the evidence of the baseline.

### Stage 1 - RED contracts for GPU cache and lifecycle

- [x] Replaces the `CompletingFrameEvictsTextTexturesNotUsedByThatFrame` test with RED tests for retention between frames and evacuation only when the bounded policy is exceeded.
- [x] Add a test that uses the text A, then B, then A and requires a cache hit to return to A.
- [x] Add a test with enough keys to exceed the cap and check for deterministic LRU eviction, not unlimited growth.
- [x] Check by test that the exhaust releases all RGB textures, optional mask and dependent `textBrushTextureCache` inputs.
- [x] Keep tests for `Dispose()` idempotent, coordinate-scale reset and device reset; everyone must immediately empty the caches.
- [x] Add internal/test-only diagnostics counters for hits, misses, evictions and estimated bytes, without public API.
- [x] Reindex after C# changes.

**Gate stage 1**

- [x] The new tests fail RED against the current per-frame pruning policy and accurately describe GPU resource ownership.
- [x] No test asks for unlimited cache or skipping `Dispose()`.

### Stage 2 - Finished and correct subpixel phases

- [x] Introduces a unique normalization/quantization function for the physical phase, with defined behavior for negative positions, fractional scales and values close to 0/1.
- [x] Uses the canonical phase in `TextTextureKey` and the same phase in the `RasterizeSubpixel` input; remove the dependency on accurate animated floats.
- [x] Add tests for the maximum number of keys produced by a long translation at scales 1, 1.25, 1.5 and 2.
- [x] Add pixel-diff tests for the canonical phases and the positions between them; check baseline, clipping, color/gamma and the absence of jumps greater than the accepted tolerance.
- [x] Check solid text and brush text, because both caches include the text key.
- [x] Keep `TextTextureKey` separately for font, size, scale coordinates and rasterization color.
- [x] Reindex after C# changes.

**Gate stage 2**

- [x] A position animation produces a bounded number of variants, then cache hits, without continuous rerastering.
- [x] The pixel-diff confirms that the optimization does not turn the text into visual marmalade.

### Stage 3 - Cache bounded and cold-path optimization

- [x] Implements generation/frame usage and bounded/LRU policy in `MonoGameDrawingBackend`.
- [x] Evacuates inputs only after applying explicit limits and disposes resources in safe order for GraphicsDevice.
- [x] Bind the lifecycle `textBrushTextureCache` to the key of the parent text, so that an escape does not leave orphaned textures.
- [x] Make grayscale mask optional and build it only when a non-solid text brush requires it.
- [x] Remove `Select(...).ToArray()` and other temporary copies from `CreateGrayscaleMask` and profile large buffers from `RasterizeSubpixelReference`/`CreateSubpixelLayers`.
- [x] Use `ArrayPool<byte>` only if the buffer can be returned after upload without `RasterizedText` or `Texture2D` keeping its reference.
- [x] Add allocation tests after warm-up for static text, animated text and A-B-A view switching.
- [x] Run the benchmark after each substep and keep the cold/warm comparison against the baseline.
- [x] Reindex after C# changes.

**Gate stage 3**

- [x] Warm view switching has zero cache misses for already retained textures, apart from legitimate font/scale/color invalidations.
- [x] Animated text reaches hits after the popularization of the canonical variants and no longer produces repeated GC in the draw.
- [x] The cache respects the ceiling and all evacuated resources are disposed.
- [x] None of the six views exceeds `16.6667 ms` in the benchmark; cold and warm pass separately.

### Stage 4 - Hardening of the benchmark and integration

- [x] Add the benchmark project to the `/benchmarks/` folder from `Cerneala.slnx`.
- [x] Document in the README the command, the WindowsDX requirements, the report format, the timeout and the fact that the results are environment-specific.
- [x] Run the benchmark three times in clean Release processes and ask for zero exceedances in all three runs.
- [x] Keep the final result in `benchmarks/results/<data>-presentation-frame-budget/README.md` near the baseline, with the same hardware and the same parameters.
- [x] Confirm that the benchmark does not force GC, does not write in the frame callback and does not include the time of its own serialization in `ProcessingTime`.
- [x] Confirm that the process closes the window on success, failure and timeout and does not leave Cerneala/dotnet processes active.
- [x] Reindex after C# and project-file changes.

**Gate Stage 4**

- [x] The order `dotnet run -c Release --project .\benchmarks\Cerneala.PresentationFrameBudget\Cerneala.PresentationFrameBudget.csproj -- --cycles 8 --frames-per-load 45 --budget-ms 16.6667` is GREEN three times in a row.
- [x] Each report contains 360 frames for each view, except for differences explicitly justified by closing the window.
- [x] Welcome does not appear in samples or aggregates.

### Stage 5 - Final verification and documentation

- [x] Run the focused tests for `MonoGameDrawingBackendStateTests` and `TextPipelineTests`.
- [x] Runs `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj`.
- [x] Runs `dotnet test .\Cerneala.slnx`.
- [x] Run the final benchmark exactly from the stage 4 gate and compare the report with the RED baseline.
- [x] Request human visual validation for static and animated text at scales 1, 1.25, 1.5 and 2; the agent does not invent the result of this gate.
- [x] Run public API diff; if it is empty, it records that no new pages are needed in `docs-site/documentation/classes/`. (The strict diff contains only two documented additions; the non-strict compatibility check is GREEN.)
- [x] If there are unexpected public changes, update the documentation via `writing-api-documentation` and synchronize the manifest where pages are added or renamed. (The existing pages for `UIRoot` and `UiFrame` are synchronized; the manifest does not require change.)
- [x] Run `git diff --check` and RoslynIndexer `doctor/status` after final indexing.
- [x] Confirm that there are no new warnings, new skipped tests, remaining processes or temporary profiling artifacts. (The indexer warning for `tmp/presentation-frame-cause.nettrace.etlx` is a pre-existing user artifact, not one created by the plan.)

**Gate Stage 5**

- [x] The focused tests, the runtime project, the entire solution and the native benchmark are GREEN.
- [x] Human validation confirms clear and stable text in Motion, without jitter, ghosting or color/gamma changes.
- [x] The benchmark demonstrates zero frames over the budget for each relevant load, not just a nice average that hides annoying spikes.

## 7. Recommended order

1. Freeze the RED benchmark and baseline before production.
2. Write the RED contracts for retention, bounded eviction and phase cardinality.
3. Canonicalize the subpixel phase.
4. Replace per-frame pruning with bounded/LRU cache.
5. Optimize cold rasterization until the same gate passes.
6. Strengthen the runner permanently, document the results and run the full check.

## 8. Stop conditions

- [x] Stops the extension to glyph atlas or asynchronous rasterization if the local fix passes the gate. (No glyph atlas or asynchronous rasterization were introduced.)
- [x] Stop the implementation and ask for a review if the solution requires a new public API in Cerneala. (The user has explicitly approved the Diagnostics extension; the two additions are documented and do not expose phase timings.)
- [x] Do not relax the budget, do not exclude slow frames and do not move the serialization so that the benchmark beautifies the result.
- [x] Do not solve the problem by hiding the views, removing Motion or reducing the Presentation content.

## 9. The definition of ready

- [x] The text cache keeps reusable content between frames and views without uncontrolled growth.
- [x] Motion no longer produces a new key for each float position and no longer triggers continuous rescanning.
- [x] Cold text rasterization no longer allocates and copies buffers that are not necessary for the type of text drawn.
- [x] GPU resources are disposed deterministically for eviction, scale change, device reset and backend disposal.
- [x] All contract and lifecycle tests are GREEN.
- [x] The Release native benchmark is GREEN three times in a row and reports zero frames above `16.6667 ms` for all six views.
- [x] The user visually confirms that the static and animated text remains clear; performance was not bought with broken pixels.
