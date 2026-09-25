# Cerneala Benchmarks

## Text characterization (no performance acceptance thresholds)

Run the real Skia/HarfBuzz pipeline, cache saturation and native SDL_GPU atlas
matrix in three fresh processes per case:

```powershell
.\benchmarks\Cerneala.Benchmarks\Measure-TextCharacterization.ps1 -OutputDirectory "$env:LOCALAPPDATA\Temp\CernealaTextCharacterization"
```

The output directory must not exist. The script builds normal Release once,
then runs 13 cases serially with a 120-second per-process timeout. It saves raw
JSON samples, logs and environment metadata. `-NoBuild` uses an already verified
Release build. Do not run a build, indexer, profiler or another benchmark
concurrently with measurements.

An individual case can be run in a fresh process with:

```powershell
dotnet .\benchmarks\Cerneala.Benchmarks\bin\Release\net8.0-windows\Cerneala.Benchmarks.dll --text-characterization native-phases .\tmp\text-native-phases.json
```

Cases:

- `pipeline-32`, `pipeline-512`: first/repeated font resolution, shaping, blob
  creation, subpixel rasterization and no-wrap/wrapped layout for 32/512 UTF-16
  characters. Dependencies warm sequentially; these are not independently cold
  stages or an additive startup breakdown.
- `cache-shape-32/512`, `cache-blob-32/512`, `cache-layout-32/512`: fill the existing
  capacity, introduce another capacity of unique keys, then replay the earliest
  and latest key 256 times. No eviction policy is changed.
- `native-static`: repeated identical ten-character run.
- `native-unique`: 320 distinct ten-digit runs from the same alphabet; the last
  256 samples never repeat a whole run.
- `native-cycle`: populate 64 ten-digit runs, then replay them 256 times.
- `native-phases`: populate all 64 canonical baseline phases for one run, then
  replay them 256 times. This is not a post-raster Motion transform.
- `native-first-view`: first/repeated drawing of 64 distinct labels.

CPU cases use real Arial (resolved family is recorded), not the synthetic font
in `DrawingTextLayoutBenchmarks`. Native cases use a real 960x640 SDL_GPU window
at scale 1, no MSAA or Present/VSync pacing. They measure CPU frame wall time,
raster/upload/command timings, raster requests/pixels, draw calls, geometry
bytes and atlas occupancy. Font resolution, command construction, analysis,
event pumping and serialization are outside frame samples. No UI layout/input
or isolated GPU time is measured. These are not full-application frame-budget
measurements.

Memory snapshots force GC only outside timed blocks. Managed live and
process-private bytes are not exact cache-owned memory: runtime, Skia and
native allocators can retain memory. Shape-array and atlas-page pixel payloads
are counted separately, excluding metadata/driver overhead. Read-only reflection
inspects the private shape cache outside timings; no production diagnostics are
added. Blob cases retain precomputed shapes before their memory baseline.
Allocation samples cover the executing managed thread, not native/all-thread
allocations. The text corpus is deterministic Latin/digits, not a Unicode
correctness or cross-platform conformance suite.

The runner fails on missing native work, disposal failures or incomplete
execution, not invented performance limits. Agree on workloads and budgets
from the results before introducing additional RED gates.

The primary suite covers Queue Engine and Relay internals. The separate WPF
project under `../Cerneala.WpfDispatcherBenchmarks/` compares the overlapping
Relay and WPF Dispatcher queueing contracts on dedicated STA threads.

Run the complete Queue Engine benchmark suite from the repository root:

```powershell
dotnet run -c Release --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -- --artifacts .\benchmarks\Cerneala.Benchmarks\artifacts\final
```

Run a focused benchmark by passing a BenchmarkDotNet filter, for example:

```powershell
dotnet run -c Release --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -- --filter "*QueueHasWorkBenchmarks*" --artifacts .\benchmarks\Cerneala.Benchmarks\artifacts\has-work
```

The suite records execution time, allocation volume, and garbage collections for idle `HasWork`, repeated `HasWork`, sparse snapshots, drains, shared-order snapshots, layout metadata promotion, and detached-subtree cleanup. Compare runs made with the same runtime, build configuration, and hardware. Absolute timing is intentionally not enforced in unit tests.

The archived Queue Engine 2.0 comparison is in [results/2026-07-13-queue-engine-2.md](results/2026-07-13-queue-engine-2.md). Raw BenchmarkDotNet output is written under `artifacts/` and remains a local generated artifact.

## Prism retained-composition benchmark

Run the deterministic Prism matrix from the repository root:

```powershell
dotnet run -c Release --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -- --prism-retained-cache
```

The runner exercises static and animated compositions, 24 common instances,
many layers, filter chains, styles, nested groups, a shared backdrop, resource
changes, and a deliberately undersized retained cache at 256 x 144 and
640 x 360. Each cache-off/cache-on row reports CPU graph-build and submission
time, a synchronized GPU-completion upper bound, managed allocations, passes,
captures, surface pressure, retained hit/miss counters, evictions, and estimated
GPU surface bytes. It fails immediately on a static allocation, missing retained
hit, unexpected capture, fallback, or leaked active surface.

The integration and budget reference is in
[results/2026-07-21-prism-integration-hardening.md](results/2026-07-21-prism-integration-hardening.md).
The earlier retained-cache-only baseline remains in
[results/2026-07-21-prism-retained-cache.md](results/2026-07-21-prism-retained-cache.md).

## Cerneala language core

Run the deterministic language performance gate from the repository root:

```powershell
dotnet run -c Release --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -- --language-core-gate
```

The gate measures cold and warm parse, a local incremental edit, semantic bind,
and a warm symbol query for small, medium, and `AspectChapterView.crn`
documents. It reports p95, maximum latency, and managed bytes per operation. The
large-document budgets are below 50 ms for parse/edit and below 25 ms for a warm
semantic query; every measured synchronous operation must remain below 100 ms.
The approved hardware baseline and allocation review are recorded in
[results/2026-08-13-language-core.md](results/2026-08-13-language-core.md).

For a full BenchmarkDotNet profile with allocation statistics:

```powershell
dotnet run -c Release --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -- --filter "*CernealaLanguageBenchmarks*"
```

## RenderSurface2D Drawing API

Run the focused Drawing matrix with a short repeatable job:

```powershell
dotnet run -c Release --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -- --filter "*Drawing*Benchmarks*" --job short --artifacts .\tmp\drawing-api-benchmarks
```

The matrix covers individual commands versus immutable batches, rounded and
reusable paths, nested state analysis, solid/dashed/round-join stroke
tessellation, and rebuilt versus reused text layouts. The accepted machine
baseline and comparison thresholds are recorded in
[results/2026-08-24-rendersurface2d-drawing-api.md](results/2026-08-24-rendersurface2d-drawing-api.md).

## TileMap grid recording and bounded warm presentation

```powershell
dotnet build .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -c Release --no-restore -m:1 -v:quiet
dotnet run --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -c Release --no-build --no-restore -- --tilemap-stage4 .\TestResults\SceneStreaming\tilemap-stage4.json
```

Run without concurrent builds, indexers, profilers, tests or other benchmarks.
The deterministic three-map fixture uses 64 target warmup and 512 measured operations.
Its CPU samples include source publication/camera updates, the root frame pump,
surface readiness preparation, map recording and all optional batch preparation.
The maintained fixture uses internal in-memory `TileMapSource2D` adapters through
the benchmark assembly's friend access to Core. Its `SetCatalog` publication
deliberately measures a private one-chunk transition; it is not an application-
facing map source API or a measurement of public `FromModel` node-replacement
editing. An explicitly asynchronous image-loader stub completes inline. Required
preparation is inside the measured operation, not a preload before the clock.
Final command recording still calls the scene directly: this is not the native input/layout/
submission/presentation path, package I/O, actual image decoding or GPU timing.

The current report schema is `cerneala-tilemap-stage4-v5-separated-phases`. All original
CPU and allocation thresholds remain unchanged. Following the approved extra
warm-memory policy, the former 1 MiB warm-static retained estimate gate is now
explicitly the **required visible-entry** estimate (`RetainedBytes` minus
`WarmRetainedBytes`), not the total. Total estimates are still reported. Optional
memory is separately gated at the implementation's 1 MiB charge per map, and
preparation at 256 cells **shared by the surface per recording**, not 256 for
each map. The frame completion that coordinates this work is inside the timed
operation, after required scene recording. The whole-fixture estimate limit
is unchanged. These are estimates/charges, not process/RSS/native/GPU bytes.
See the canonical TileMap2D and TileMapDiagnosticsSnapshot API pages for their
accounting definitions.

In addition to the historical final-operation counters, every scenario reports
the maximum visible rebuild count, maximum prepared cells, total optional
batches prepared, and maximum warm counts/charges over **all** measured
operations. Thus a zero final rebuild count cannot hide earlier pan phases.
Aggregate bookkeeping happens outside each timed operation; it does
not move preparation out of the measured recording path. No report claims that
an arbitrary camera jump can always be satisfied by the bounded warm set.

### Separated startup, transition, and measurement phases

For each scenario, v5 times fixture construction and retains all 64 target warmup
samples. It then runs 1,024 matching operations on a **separate fixture**, in eight
blocks of 128 with a requested 100 ms pause after each block. This runtime primer
does not add target recordings or prepare the target's retained batches. Target
warmup, primer, and measurement use the same sample-collection helper. The report
includes primer duration, operation time, allocations, and completed-JIT count.

After primer disposal and full GC/finalizer/full GC, the runner observes a 100 ms
interval without an increase in the process-wide completed-JIT counter, bounded
by a 2,000 ms timeout. This observation is not proof that no compilation is pending.
It then keeps all 512 chronological operation samples, with no trimming,
subtraction, or replacement of outliers. Exact measured optional-batch totals
must remain 19 / 647 / 19 for static / pan / mutation; static and pan must have
zero visible rebuilds throughout the measured window, not just at its end.

The approved cold fixture limit is 300 ms for construction plus the first command
recording in each fresh process. Only the first scenario is process-cold. This
limit is **not** a game-startup or first-native-frame budget and excludes package
opening, cold image decoding, and GPU presentation. Use three fresh default-runtime
Release processes for a validation set and keep every result, including failures.
Historical v4 reports retain their original protocol and are not directly
comparable speedup measurements against v5. Earlier v5 results used the former
direct `Model` control path; they do not verify this internal source-publication workload.

The maintained v5 runner reports the process-wide completed-JIT delta as a
diagnostic. The user explicitly removed the agent-added zero-JIT rejection
criterion after CLR attribution identified a benchmark `TimeSpan` helper.
All timing/allocation samples still include compilation costs; numeric, work,
memory, startup and quiescence gates are unchanged. A JIT count is neither a
reflection-invocation count nor attribution to Cerneala; method identities and
reachable source paths require separate evidence.
The current acceptance status and all failed sets are recorded in
`TestResults/SceneStreaming/implementation-status.md`.
