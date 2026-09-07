# MonoGame / WindowsDX removal

## Current verification status — 2026-09-06

The source migration is implemented: MonoGame/WindowsDX production projects,
APIs and active consumers are removed; shared behavioral coverage targets SDL
or backend-neutral core APIs. Canonical documentation, shaders, package tooling
and CI commands have been migrated. **The full-green acceptance gate remains
blocked; this is not a fully verified completion.**

The final native-enabled, serial Release solution run executed all nine test
projects: **4,796 passed, four failed, zero skipped; 4,800 total**.

| Test project | Passed / total | Failed |
| --- | ---: | ---: |
| Cerneala.Tests | 3297 / 3297 | 0 |
| Cerneala.Tests.SdlGpu | 511 / 515 | 4 |
| Cerneala.Tests.Language | 190 / 190 | 0 |
| Cerneala.Tests.LanguageServer | 40 / 40 | 0 |
| Cerneala.Tests.PreviewHost | 13 / 13 | 0 |
| Cerneala.Tests.Scene2DImporters | 151 / 151 | 0 |
| Cerneala.Tests.SourceGen | 518 / 518 | 0 |
| Cerneala.Tests.VisualStudio | 47 / 47 | 0 |
| Cerneala.Tetris.Tests | 29 / 29 | 0 |

Evidence: `artifacts/monogame-removal/final-verification.log` and the nine TRX
files in `artifacts/monogame-removal/final-verification/`. The command is
`dotnet test Cerneala.slnx -c Release --no-build --no-restore -m:1`, with
`CERNEALA_SDL_NATIVE_TESTS=1`. No test/project filter was used.

Other completed gates:

- Fresh Release solution build: zero warnings/errors, 21.10 s
  (`final-verification-build.log`).
- Main-suite visual coverage includes all 133 frozen-reference comparisons and
  all six real application showcase screenshot cases, including four
  antialiasing cases. Baselines and pixel tolerances were not relaxed.
- Native input ownership scenario: 40/40 fresh-process repetitions pass;
  all eleven Windows native contract cases also pass in the final suite.
- PrismAudit: 178 catalog entries, 31 common properties, 216 public Prism types,
  seven extended public types, zero gaps. All six SDL shader artifacts verify.
- All six RID publish outputs pass the native-asset validator. The Windows x64
  published executable passes multi-window and Prism smokes.
- Canonical manifest: 1,119 entries, zero missing pages or duplicate paths.
  Both migrated workflows pass actionlint; all four relevant PowerShell scripts
  parse. ShellCheck and Pyflakes were not run.

The four remaining failures are the unchanged alpha-occlusion cases for content
1, 7, 6 and 3, with maximum differences 47, 25, 27 and 46 per 255. The same
occlusion invariant was reproduced using direct Microsoft D3D12 without SDL,
Graphix or Cerneala on the tested NVIDIA path; Intel and WARP passed that native
matrix. Its evidence and the rejected hypotheses are recorded below. No
rasterizer workaround, MSAA disablement, tolerance increase or skip was added.
Closing this gate requires a correction or independently verified change in
that NVIDIA/D3D12 path, followed by rerunning the original cases and full suite.

Hosted Cerneala CI, native Linux/macOS/ARM execution and human/manual validation
have not been performed. Cross-publishing is not runtime validation. Earlier
ignored diagnostic-artifact cleanup limitations remain as recorded below.

The following sections are a chronological execution record. Earlier pending,
RED and GREEN checkpoints describe their original runs, not the final state.

## Authorized scope

The user requested complete removal of the MonoGame / WindowsDX implementation,
public APIs, dependencies, build tooling, and active consumers. SDL3 + SDL_GPU is
the sole maintained desktop backend. There is no compatibility facade or fallback.
Historical reports, captures, and plans remain evidence and are not rewritten.

The user explicitly requires migration to SDL of every existing MonoGame-only
test that checks a backend-independent contract. Removing an implementation does
not authorize removal of its shared behavioral coverage.

## Preservation rules

- Keep backend-neutral drawing, input, hosting, resource, Prism, and SourceGen
  ownership boundaries. A single shipped backend does not justify coupling core
  or the generator to SDL.
- Keep the shared HLSL mathematics under `Drawing/Prism/Shaders/Hlsl`.
- Migrate shared-contract tests with their assertions and tolerances intact.
- Retire tests only when they test a removed implementation detail, or document
  the existing equivalent SDL coverage before removing duplicate coverage.
- Preserve historical visual baselines and their provenance. A backend mismatch
  is evidence of disagreement, not permission to replace an expected image.
- Capture application images only through the application-owned screenshot API.

## Initial evidence

- `Cerneala.Backends.MonoGame` owns the WindowsDX package and links drawing,
  hosting, input, image loading, and WindowsDX graphics-session sources.
- `Cerneala.Platforms.Win32` supplies the old native window platform.
- PreviewHost, Playground applications, Presentation, benchmarks, and the main
  test project still reference the old backend. Tetris has a conditional legacy
  build path.
- `.config/dotnet-tools.json` includes `dotnet-mgfxc`.
- CI includes legacy shader checks, a WindowsDX runtime smoke, and live
  WindowsDX-to-SDL pixel comparisons.
- The RoslynIndexer search command returns only 50 hits, including with a
  temporary larger configured limit and daemon disabled. A textual inventory was
  used to supplement that incomplete result; source reads still use the indexer.

## Verification record

Baseline command (before source/project modifications):

```powershell
dotnet test .\Cerneala.slnx -c Release `
  --logger 'trx;LogFilePrefix=before-removal' `
  --results-directory .\artifacts\monogame-removal\baseline
```

Baseline result: exit code 1. Nine test projects ran: 4,591 passed, two failed,
seven skipped (4,600 total). Both failures are the existing
`AlphaBlendRenderingTests.OpaqueStrokeOccludesEarlierStrokeAroundTranslucentContent`
cases: text (`content=1`) changes the image by 47/255, translucent fill
(`content=2`) by 25/255, against the unchanged maximum-delta contract of 1/255.
The empty-interleaving case passed. These shared rendering contracts must be
preserved on SDL; their failing legacy output is not a new golden reference.

Local TRX files and the complete log are under
`artifacts/monogame-removal/baseline/`.

## Coverage disposition

| Existing coverage | Disposition | Verification |
| --- | --- | --- |
| `PrismSdlGpuPixelConformanceTests` (132 catalog cases) and `SdlGpuDrawingConformanceTests` | Replaced live legacy captures with immutable historical references. Native SDL execution, automatic catalog enumeration, assertions, and tolerances remain. | Export: 133/133 comparisons passed. Migrated tests: 133 passed, 0 skipped, `frozen-conformance-green.trx`. |
| `AlphaBlendRenderingTests` | Migrated to native SDL, including both failing baseline occlusion cases. The dynamic-brush resource test preserves its upper bound; it no longer requires at least one texture because that allocation is an implementation detail. | Original SDL run: 11 passed, 1 failed, 0 skipped (`alpha-sdl-first.trx`): 64 cached gradient textures exceeded the unchanged upper bound of 2. After the brush lifetime fix, all 12 alpha tests and both new shared-window brush regressions pass (`brush-lifetime-green.trx`). All three occlusion cases pass. |
| Retired native smoke: multi-window lifetime, alpha image loading, text brushes, offscreen drawing brush, copy/presentation alpha | Shared assertions moved to `NativeDrawingSmokeTests`; isolated process test now invokes the existing SDL smoke. Separate GraphicsDevice/Texture2D/HWND assertions belong to the removed adapter; SDL uses CPU images and device-shared resources. Tilemap, collision, sprite-animation, and scene-debug modes already exist in the SDL smoke and use the same linked fixtures. | New tests and all native modes still require verification. |
| Presentation and Drawing API showcase tests | Removed unused Windows hosting imports; scenarios and assertions unchanged, with SDL registration now supplied by the test bootstrap and application assemblies. | Pending compilation and test execution. |
| Backend registration, caret width, packaged image resolution, font cache identity | Retained generic registration-conflict tests with a fake competing backend; caret uses the same neutral coordinate conversion without the removed XNA rectangle wrapper; image resolution exercises SDL loading; font-cache tests exercise SDL rendering rather than the removed private key. | Pending compilation and test execution. |
| Drawing shapes and state | Pixel expectations and color tolerances moved to the native SDL fixture. Removed private MonoGame stroke-cache/layer-pool counters rather than asserting that SDL uses the same mechanism. State restoration now also renders an unscoped full frame after the scoped frame. The old fixed physical-pixel samples now use an explicit 1.5 scale instead of depending on the desktop DPI. | Pending compilation and test execution. |
| Path fills, SVG tessellation, stroke bounds/caps/dashes | Tests call the existing backend-neutral mesh builders used by SDL directly. Area, topology, bounds, and scale assertions remain. Private MonoGame stroke-cache-key assertions are retired; command identity and scale-dependent geometry assertions remain. | Pending compilation and test execution. |
| Floating-point Prism kernels | Native shader tests move to `Cerneala.Tests.SdlGpu/Prism`. The shared fixture invokes the shipped SDL pipeline, uploads half-precision values, and reads floating-point targets back through SDL transfer buffers. Existing CPU comparisons and tolerances are retained. Technique-name checks become SDL selector checks; native execution verifies shader behavior. | Successive groups: 6, 20, then 35 passed, each with 0 skipped (`kernel-migration-first.trx`, `kernel-migration-second.trx`, `kernel-migration-third.trx`). Further migrated kernel cases remain pending. |

The 133 exported images and their provenance, comparisons, and hashes are under
`tests/Baselines/Conformance/RetiredWindowsDx/`. The temporary legacy export test
has been removed. Normal conformance tests cannot overwrite these references.

Implementation, remaining coverage disposition, and final verification: not yet complete.

## Direct removal checkpoint

At the user's direction, removed the production owner before adapting remaining
consumers: `Drawing/MonoGame`, the MonoGame hosting/input/resource adapters, both
WindowsDX composition/session sources, and the MonoGame and Win32 projects and
their source files. Shared HLSL and all remaining tests are preserved. These were
tracked source deletions; Git retains their previous versions.

The first post-removal Release solution build failed with 83 errors and 10
warnings. The log is `artifacts/monogame-removal/after-owner-removal-build.log`.
Consumers must now be migrated; this failed build is a dependency inventory,
not completed verification.

Composition migration selects SDL unconditionally in Presentation, Playground,
the three lab/oracle applications, Tetris, and PreviewHost. Project references
and the test bootstrap have been changed accordingly. The retired smoke project
was removed after transferring its shared checks; its duplicated scene capture
modes are already present in the existing SDL smoke. CI now targets SDL and
immutable historical-reference comparisons. The local mgfx compiler tool entry
was removed. CI changes have not been executed by the hosted runners.

The subsequent main test-project build exposed 607 compile errors in remaining
legacy-dependent test files (`shared-tests-build.log`, with unique diagnostics
in `shared-tests-errors.txt`). Some consumers were migrated after that run;
this number is a checkpoint, not an assertion about the final source state.
Remaining tests have not been excluded from compilation.

Latest checkpoint: `dotnet build Cerneala.slnx -c Release` failed with 650
errors and zero warnings across the remaining consumers. Full log:
`artifacts/monogame-removal/api-decision-checkpoint-build.log`.
`git diff --check` passed. The index was refreshed after the final C# batch.
The 15 canonical pages owned by removed adapters have been deleted together
with their manifest entries; these tracked pages remain recoverable from Git.
The SDL registration page now documents removal of the old configuration path.
Remaining active documentation and cross-references are not yet synchronized;
no documentation-completion claim is made.

## Public configuration decision

`PrismRendererOptions` is backend-neutral and still used by SDL's internal
Prism resources; it must not be deleted as a MonoGame implementation detail.
However, the documented public configuration entry points were the removed
`MonoGameDrawingBackend` and `MonoGameUiHostOptions`. SDL's public composition
only exposes `SdlGpuApplicationBackend.EnsureRegistered()`. Its drawing
resources instantiate options internally, with a 32 MiB retained-cache soft
limit, rather than accepting an application-supplied options instance.

The user explicitly chose to keep SDL's current internally fixed configuration
and remove the old adapter's public configurability. No replacement public
configuration API will be added. Canonical documentation must describe this
deliberate API removal and must not advertise the removed configuration path.

Separately, the canonical `RenderSurface2D` page promises unchanged-command
raster reuse and damage-region recomposition even in Continuous mode. SDL's
current surface path rerenders the whole target when FrameVersion changes.
The old tests for that documented behavior cannot be dismissed merely because
they assert counters on a retired internal session. This contract remains a
migration gate; no performance claim or waiver is recorded.

## Floating-point migration findings

The fourth native kernel batch produced 42 passes and three failures, with no
skips (`kernel-migration-fourth.trx`). Two valid regressions were Color and
ColorMatrix with clamping disabled: maximum observed component differences
were 0.030673 and 0.175146 against unchanged tolerances 0.006 and 0.003.
`FinalizeCatalogFilter` compared the public Normal enum value (145) although
SDL binds the shader's normalized blend code (0). The shared shader now checks
0, consistent with the shader dispatcher and SDL selector. Regenerated the
versioned DXIL/SPIR-V/MSL artifacts and metadata using the existing compiler;
the build's artifact verification passed. The same migrated kernel cases now
pass; full native catalog conformance and the full solution suite are still due.

The Dissolve failure was a test-fixture defect: the fixture supplied a white
texture instead of the embedded rank map at sampler slot 7. Binding the original
RGBA8 rank data restored the unchanged exact per-pixel assertion. This kernel
result does not establish that the graph executor binds every required resource.
The fifth batch passed all 45 existing kernel cases. Its two new SpinBlur cases
failed during fixture plan construction (missing generated property values),
not during rendering; these are not production RED evidence and remain pending.

The sixth kernel/artifact batch passed 56 tests with zero skips
(`kernel-migration-sixth.trx`), including both sentinel-mipmap SpinBlur cases,
StainedGlass CPU parity, Spatter CPU parity, builtin texture identity/shape/format,
and all offline shader-format loading checks. The original embedded-bytecode
minimum-size assertion is now in `SdlGpuShaderArtifactTests`; the MGFX resource-name
assertion retires with the removed assembly. Shared Prism test data is linked into
both test projects rather than duplicated.

Input migration passed 19 tests with zero skips (`input-migration.trx`). SDL
scancodes preserve Enter/Escape/A/modifier transitions; pointer scaling and
unscaled wheel/button state retain the original values. Existing SDL event-pump
coverage also verifies display-scale application before pointer conversion. The
removed adapter's public CoordinateScale setter and reflection-only host wrapper
checks are not new SDL public APIs. Windows registry GPU-preference tests retire
with that removed WindowsDX policy; SDL's current internal configuration remains.

Removed clip-stack and draw-mapper adapter-unit tests alongside those removed
public adapter types. Shared clipping/state pixel scenarios, coordinate-mapper
math tests, tessellation tests, and frozen drawing conformance remain; the old
helper's integer minimum-stroke-width behavior is not imposed on SDL's fractional
stroke rasterizer. The core-hosting boundary test now also rejects SDL platform
and renderer dependencies. Relay's core ownership and nullable contract remain;
only reflection over the removed wrapper is gone.

Content-service ownership checks now exercise `DrawingContentServices` directly,
including idempotent cache disposal and sequential lifetimes. Removed wrapper
constructor, SpriteBatch/white-pixel arguments, and wrapper-specific SetRoot
reattachment are retired APIs, not reintroduced facades. Native SDL window/image
lifetime verification remains a separate gate. These main-project tests have not
yet been executed because other consumers still prevent main-project compilation.

## Brush lifetime migration finding

The native 64-frame changing-gradient regression observed 64 cache entries where
at most two were allowed. Independent shape- and text-brush tests reproduced the
same lifetime defect across two windows sharing one device: after both windows
rendered empty frames, one unused brush texture remained instead of zero.

The SDL drawing backend now retains each brush texture while its own last frame
uses it. Device-shared resources release an entry only after every referencing
backend has stopped using it or has been disposed. GPU destruction still follows
the existing deferred-retirement path; image textures and text atlases are not
indiscriminately invalidated. No public configuration API or arbitrary cache
budget was introduced.

RED: `alpha-migration-red.trx`, `brush-shared-and-text-lifetime-red.trx`.
GREEN: 14 tests, zero skipped (`brush-lifetime-green.trx`).
Affected SDL project with native tests enabled: 200 passed, zero failed, zero
skipped (`sdl-affected-after-brush.trx`). The latest full Release solution build
still fails with 284 compiler errors, zero warnings, in remaining consumers
(`current-build.log`). This supersedes earlier build-error counts, not the
unfinished full-suite gate. No performance or full visual-conformance result is
claimed for this fix yet.

## Managed-surface migration finding

Eight presentation, drawing-event, Prism-image and OnDemand dependency tests
moved to `RenderSurface2DPresentationTests`; all eight pass with native SDL and
multisampling enabled (`surface-presentation-first.trx`).

Three independent retained-raster tests were RED before production changes:
identical frames submitted two draw calls rather than presentation alone (one),
and damage updates resubmitted distant commands (16/20 vertices instead of
12/16 including damage clear). Existing pixel expectations remained green, so
this was unnecessary rendering, not inferred image corruption.
`surface-reuse-and-damage-red.trx` preserves the reproduction.

The SDL surface now retains shared command-metadata snapshots, skips identical
raster work, and clears/replays the union of changed bounds. Changed compositing
or Prism scopes conservatively replay the whole target. Partial replay uses
opaque replacement for the clear, retains drawing order, and clips submissions
to damage. The fake SDL test asserts the exact old/new union `(2,2,12,4)`; native
pixel tests verify moved, overlapping and untouched content with multisampling.
A failed replay invalidates its retained snapshot rather than trusting a
partially modified target.

The migrated Prism-image disposal test then found four retained results after
disposal instead of zero. An independent empty-host-frame test reproduced the
same failure. The invalidation hub had queued the owner, but SDL consumed the
queue only while executing a nonempty Prism graph. Cleanup now also runs for
frames without Prism scopes, including empty host and managed-surface frames;
the existing owner/key reconciliation for active graphs remains.
RED: `surface-prism-lifetime-first.trx`, `empty-host-invalidation-red.trx`.
Focused GREEN: 27 passed, zero skipped
(`surface-reuse-damage-lifetime-green.trx`), including the unchanged alpha suite.
Full SDL affected-suite and complete solution verification remain separate gates.

The five old retained-session tests are represented by
`SdlGpuSurfaceRetainedTests`: identical-command reuse, changed-region replay,
overlap recomposition, Prism pass savings, and deterministic disposed-image
cache eviction. Private retired-session counters are replaced by actual SDL
submission/vertex counts, exact scissor observation, native pixels, GPU pass
counts, and retained resource counts; no public diagnostics facade was added.

Affected SDL suite after the managed-surface fixes: 215 passed, zero failed,
zero skipped (`sdl-affected-after-surface.trx`).

The eight native/backend portions of `DrawingImageMeshBatchTests`,
`DrawingIntegrationLifecycleTests`, and `DrawingTextLayoutTests` are now in
`DrawingRetainedPayloadTests`; core metadata/layout cases remain in the main
project. All eight migrated cases passed (`retained-payload-first.trx`). The
128-frame unchanged-mesh stress count is preserved. Resize/detach tests observe
backend-state replacement immediately and GPU release after SDL's existing
deferred-retirement flush rather than imposing Texture2D's old synchronous
`IsDisposed` representation. Advanced batches retain five logical submissions
and the at-most-five draw-call bound; compatible SDL submission merging is
allowed and deterministic pixels are still required.

The retained WindowsDX session test file was removed only after its full shared
coverage had migrated and the affected SDL project was green. Historical Git
versions remain available.

The single-scenario backend comparison runner is now an SDL-only measurement
runner, `--prism-sdlgpu`, with schema `cerneala-prism-sdlgpu-benchmark-v1`.
Its warmup (12 frames), measured sequence (96 frames), scenario and absolute
metrics remain. The live WindowsDX ratio and comparison gate are removed,
not synthesized from unrelated measurements. Historical comparison reports are
unchanged. Benchmark compilation/execution is pending the remaining runners.

## Remaining brush-contract migration

The six former brush-diagnostic tests now render native SDL pixels in
`Cerneala.Tests.SdlGpu/BrushRenderingTests.cs`. Gradient samples use explicit
pixel-center alignment; the old straight-alpha expected color is converted to
premultiplied bytes with a one-byte quantization tolerance. The complete uniform
tile rectangle and controlled cycle exception remain asserted. The retired
diagnostic helper methods were not recreated.

The split smoke tests isolated four native text failures: radial gradients at
both scales sampled window coordinates rather than glyph-local coordinates;
image text at both scales sampled transparent because that descriptor was not
handled. Drawing brushes threw `NotSupportedException`. Uniform image fitting
painted white across the whole destination instead of leaving its top half black.
RED records: `native-smoke-isolated.trx`, `brush-rendering-migration-red.trx`.

SDL now samples text gradients in glyph-local pixel-center coordinates. Tile
brushes use the ordinary GPU command/Prism and retained-surface path for capture;
text masks multiply the captured premultiplied channels by glyph coverage using
an internal blend state. No public configuration or compatibility API was added.
Stable visual and text captures reuse the completed raster; changed visual
commands repaint. Unused captures are retired through the existing GPU resource
owner rather than retained indefinitely.

A separate nested-layer test found earlier red content replaced by black
(128-byte red-channel error). It reproduced with both DrawingBrush and an
ordinary RenderSurface2D, with and without multisampling. The layer allocator
was indexed by each command range's local depth, letting a child range clear a
still-active parent layer. Allocation now uses the renderer's actual active
compositing depth across capture boundaries. The independent reproduction is
`layer-alias-independent-red.trx`.

The original copy/presentation smoke adaptation had an invalid empty Prism
layer definition and was not production RED evidence. It now executes the
shipped copy kernel directly, then presents its result; the original expected
premultiplied RGB `(106,52,46)` and tolerance 3 pass unchanged.

Focused GREEN: 52 passed, zero failed or skipped
(`brush-captures-and-layer-green.trx`), covering migrated brushes and smoke,
nested capture layers, retained raster reuse, alpha and surface presentation.
Canonical brush pages now describe the SDL implementation; no pages were added
or renamed, so the manifest is unchanged. Full affected-suite and final solution
verification remain separate gates.

Affected SDL suite after brush capture and layer ownership fixes: 251 passed,
zero failed or skipped (`sdl-affected-after-brush-captures.trx`).

## Drawing backend state and text-cache disposition

The backend-independent portions of `MonoGameDrawingBackendStateTests` now live
in `SdlGpuTextCacheContractTests` and `DrawingBackendLifecycleContractTests`.
Thirty migrated/related cases passed with zero skips
(`backend-state-and-capture-lifetime-green.trx`). The old file was removed only
after those tests passed. Coverage mapping:

- Gradient-axis behavior and coordinate scale use native pixels, including the
  exact physical rectangle `(2,4,6,8)`. A one-pixel-wide texture allocation is a
  removed adapter optimization, not a required representation of the gradient.
- Tight text-origin rounding, seven negative/boundary subpixel cases, and all
  four 8,193-position translation sequences retain their expected values and
  at-most-64-phase / more-than-8,000-hit assertions. Tests invoke existing SDL
  functions without adding replacement diagnostic APIs.
- Foreground-independent geometry keys, font/size/scale separation, actual A-B-A
  raster reuse, shared solid/gradient text phases, and scale-change rerasterization
  remain covered. The three 1,000-lookup allocation tests retain the 1,024-byte
  bound and zero-miss assertions for static, 64-variant, and A-B-A sequences.
- LRU overflow is exercised at SDL's existing eight-page atlas cap; active pages
  remain valid while older inactive pages are reused. Cache-entry limits and
  RGB/mask Texture2D destruction are replaced by SDL page ownership, bounded
  resource counts, and complete device-owner disposal, not copied as a second
  cache policy. Sixty-four dynamic brush/text frames retain only live and
  pending-retirement capture/mask textures, then return to the initial resource
  count after the documented deferred flush.
- Successful consecutive frames and recovery after a command throws inside a
  layer use native whole-frame pixel assertions. Existing
  `NestedStateUsesScissorAndBalancedStencilOperationsThenRestoresDefaults`
  preserves balanced clip/stencil ownership. Arbitrary borrowed XNA state objects,
  SpriteBatch constructor ownership, diagnostic snapshot types, device-reset
  callbacks, and exact eight-entry legacy layer pools retire with the adapter.
- The old extra-pop test invoked a clip-only diagnostic helper, bypassing the
  ordinary analyzer. `DrawCommandStateAnalyzer.CloseClip` already rejects an
  unmatched pop. That helper's permissiveness is not imposed on the shared
  command contract.

Two valid text-cache regressions were RED in `text-cache-migration-first.trx`:
an equal-phase text translation created texture 4 instead of reusing the three
existing textures, and completing one frame removed another active frame's
atlas entries. Glyph-local gradient textures no longer include absolute position
in their cache key. Atlas compaction waits until all active page readers finish,
so queued UVs cannot be invalidated by another frame's completion.
The focused reproduction plus native brush/lifetime scenarios passed 47 cases,
zero skipped (`text-cache-and-brush-green.trx`).

The first capture-lifetime test incorrectly assumed synchronous destruction;
`CompleteFrame` flushes retired resources before `EndFrameState` queues newly
unused captures. Its corrected bound explicitly counts one live and one pending
capture, and verifies final release after two empty frames. This fixture mismatch
was not treated as a production defect.

Latest complete Release build checkpoint: 259 errors, zero warnings,
`current-build.log`. That run predates removal of the old state-test file;
the remaining-consumer inventory is in `current-errors.txt`. The complete
solution gate is still open.

A final image-brush viewport regression found clamped edge pixels painted
outside a non-repeating `(8,8,8,8)` tile (`image-viewport-red.trx`). Explicit
viewports now use the clipped capture path rather than the whole-bounds image
fast path. Default fill brushes keep the direct texture path.

The repeated native brush group passed all 13 cases (`image-viewport-green.trx`).
The complete affected SDL suite after the text-cache and viewport fixes passed
282 tests, zero failed or skipped (`sdl-affected-after-text-cache.trx`).

## Prism surface ownership migration

The next full Release solution build still failed with 218 errors, zero warnings
(`current-build.log`); these are remaining legacy consumers, not a green solution.

Nineteen shared surface/cache cases now exercise SDL device resources: exact mip
bytes, compatible reuse, five incompatible storage variants, 2,048-frame bounded
ownership, promotion and exceptional release, owner/stale-key invalidation,
independent entry/byte LRU limits, pinned-entry protection, rejected promotion,
hard-budget reclamation/failure, disposal, and UI-free retained metadata.
The old surface/cache files remain until their remaining graph/thread/lifecycle
contracts have been accounted for.

Six RED cases exposed four owner defects (`surface-ownership-first.trx`):
invalidated pinned entries could be reacquired, promotions exceeded retention
limits when no entry could be evicted, hard-budget pressure returned retained
storage to the free list without reclaiming it, and late lease release recreated
512 free bytes after disposal. SDL resources now reject stale acquisitions and
over-budget promotions, reclaim enough unpinned storage before refusing allocation,
and retire late releases rather than rebuilding a disposed pool. Resource
destruction keeps the existing deferred GPU-retirement path; no public option or
replacement cache policy was added.

All 19 cases pass (`surface-ownership-green.trx`). The complete affected SDL suite
with `CERNEALA_SDL_NATIVE_TESTS=1` passes 301 tests, zero failed/skipped
(`sdl-affected-after-prism-ownership-native.trx`). An earlier run accidentally used
the wrong opt-in variable and skipped 76 cases; it is not native verification.

All six operational-diagnostic tests moved to SDL and pass alongside the 19 owner
cases (`prism-ownership-and-diagnostics-green.trx`). Stable/redacted dumps, weak
instance ownership, the 2,048-warmup/4,096-frame zero-allocation assertion and
operational snapshots remain unchanged. Allocation failure is injected at the
actual SDL texture API and flows through the graph executor; the diagnostic
reports SDL's existing hard limit rather than the removed configurable zero-limit
constructor. Direct resource tests independently exercise real hard-cap exhaustion.
Only after those six tests passed was the old operational-diagnostic file deleted.

## Remaining consumers and benchmark checkpoint

The benchmark project now builds in Release with zero warnings/errors. The
TileMap backend profile is SDL-only, uses schema v2, and removes legacy-only
counters and obsolete claims that SDL always redraws the complete surface.
The retained-raster gate observes exactly one warm-static presentation draw;
camera pan still rebuilds no tile batches and local mutation rebuilds exactly one.
All three scenarios completed at 768x512, 12 warmup and 96 measured frames
(`tilemap-backend-profile.json`). Local wall P95 values were 143.0 us static,
8,868.2 us pan, and 6,391.1 us mutation. These are local observations, not isolated
hardware comparisons or a cross-platform performance certification.

The retained Prism runner now uses SDL images, backdrop leases and submission;
its v2 output explicitly measures the complete frame rather than the retired
executor's prebuilt-plan-only boundary. Plan-building allocation remains a
separate measurement. The old exact-zero executor-allocation assertion cannot
be interpreted as an equivalent complete-frame result; no zero-allocation claim
is made. Removed cache-on/off and custom-small-budget modes belong to the
configuration entry points the user explicitly removed. Eleven ordinary scene
workloads and both resolutions remain, with 12/96/8 warmup/measurement/completion
counts. GPU completion is a fence-based CPU upper bound, not a GPU timestamp.

This runner is NOT green. At 256x144, the 24-common-instance static workload
executed all 16,128 planned passes and 2,304 captures over 96 frames, saving no
passes after warmup. Its retained-pass-savings gate stopped execution; later
scenarios and the medium resolution were not run. The log is
`prism-retained-benchmark.log`. The observed SDL cache limit remains 32 MiB;
neither the limit nor the failed gate was relaxed. Ownership/admission behavior
and the remaining migrated cache tests still require investigation.

The latest complete solution build fails with 171 errors, zero warnings
(`current-build.log`); all remaining reported consumers are tests. The unique
inventory is `current-errors.txt`. No main-project test has been excluded merely
to get a successful build.

The completeness audit and generated catalog coverage now identify the SDL
kernel/test owners and no longer require removed public adapter types or host
configuration members. Regenerated the active filter reference and completeness
report; historical plans, dated reports, captures and the original API baseline
remain unchanged. `PrismAudit --write` and `--check` pass for 178 catalog entries,
31 common properties, 216 public Prism types and seven extended types. This is
metadata/API auditing, not proof that the still-unfinished runtime suite passes.
All 18 focused `PrismCatalogCompilerTests` pass (`prism-catalog-owner-green.trx`).
The complete affected SourceGen suite also passes: 518 tests, zero failed/skipped
(`sourcegen-affected-after-owner-migration.trx`).

## Window-platform contract migration and unresolved semantics

Migrated the seven cursor mappings, hidden cursor, runtime hover routing,
factory/surface handoff, coalesced resize notifications and hide/dispose ownership
into `SdlWindowMigrationContractTests`. Its first compilable run passed ten cases
and failed the duplicate-mouse-motion case: expected one render request, observed
 two (`window-contracts-red.trx`). Earlier missing-using fixture failures were not
valid RED reproductions.

`SdlInputSource.MovePointer` now reports whether the logical pointer position
changed; `SdlPlatformWindow` requests a frame only for that change. The first
observed position is still meaningful even at (0,0); pointer leave resets that
observation, and a coordinate-scale change is compared in logical coordinates.
The original reproduction, new reentry/scale checks, existing SDL input tests and
platform tests pass together: 31 tests, zero failed/skipped
(`window-contracts-green.trx`). The fake SDL API records cursor creation/selection;
these are test observations, not new production diagnostics.

Seven Windows-native cases now also exercise SDL-owned windows in
`SdlWindowsNativeContractTests`. Their first run failed all seven
(`native-window-contracts-first.trx`):

- Native maximize reports a maximized HWND but does not cover the work area when
  the model has MaxWidth=700 and MaxHeight=600.
- Programmatic maximize overwrites the model's normal Left=80 with Left=0.
- Native icon queries return zero.
- The lower-right client grip returns HTCLIENT (1), not HTBOTTOMRIGHT (17).
- Immediate presentation assertions fail during both move and resize.
- The ownership/input test observes pointer X=-1 after the native event pump,
  not the injected WM_MOUSEMOVE X=45. Process ownership and independent graphics
  targets passed before this assertion. Real mouse-leave events may invalidate
  this synthetic-input fixture; input ownership is not yet a proven root cause.

These failures are not all established production defects. In particular, the
old synchronous WM_SIZE/WM_MOVE test boundary differs from SDL's modal-loop timer
and exposed-event path, and requires a faithful native reproduction before a
production change. SDL's release-3.4.14 Windows event source invokes live resize
updates from WM_TIMER; the current test does not pump that timer while inside the
modal loop. No delay or production hook was added to make the old test pass.
Reference: https://raw.githubusercontent.com/libsdl-org/SDL/release-3.4.14/src/video/windows/SDL_windowsevents.c

The maximize requirement is a material contract decision. The retired Win32
implementation explicitly relaxed max-track size during maximize, while current
SDL passes model limits to SDL_SetWindowMaximumSize. SDL documents those as the
maximum client-area dimensions (0 means unbounded):
https://wiki.libsdl.org/SDL3/SDL_SetWindowMaximumSize
The old test must not be silently weakened, nor may this migration silently
change SDL's user-visible size-limit semantics. User direction is required on
whether to preserve the legacy maximize exception or retain SDL's limits during
maximize. The old `Win32WindowPlatformTests.cs` has NOT been removed: native
contracts and their disposition are still open. No screenshot or human manual
validation was claimed.

The first complete affected SDL run after this checkpoint produced 284 passes
and 42 failures (`sdl-affected-window-migration.trx`). Investigation found three
remaining runtime metadata checks that still required the retired kernel-owner
prefix. This was a regression introduced by this migration's catalog metadata
rename, not an unrelated failure. New `PrismPlannerOwnershipTests` reproduced all
three disabled planner families without a GPU (three valid RED cases), after
fixture namespace corrections. Updated only the three expected owner strings in
`PrismCatalogFilterPlanner`, `PrismNeighborhoodPlanner` and
`PrismResamplingPlanner`; did not bypass coverage checks or alter algorithms.
The focused tests pass (`planner-owners-green.trx`).

Repeating the complete SDL suite with native opt-in now gives 322 passed,
7 failed, 0 skipped (`sdl-affected-planner-owners.trx`, 1m22s). The seven remaining
failures are exactly the Windows-native migration cases listed above. This is
not a green suite and does not complete the migration.

The earlier 42-failure run also included one text-cache allocation assertion:
`WarmStaticAnimatedAndABALookupsDoNotAllocateOrMiss(1)` measured 7,104 bytes against
its existing 1,024-byte ceiling. It passed in the subsequent full run without a
text-cache change. Its intermittence and allocation source remain unresolved;
a passing rerun is not proof that the allocation gate is stable. No tolerance,
warmup count, test exclusion or production cache policy was changed to hide it.

## Approved maximize contract

The user confirmed retaining SDL's finite MaxWidth/MaxHeight limits during
maximization and continuing the complete migration. This deliberately replaces
the retired Win32 host's maximize exception; it does not waive the remaining
window, graphics, lifecycle or performance failures. The canonical Window and
WindowState pages now state this contract. Existing pages were edited in place;
no manifest entries were added or renamed. Native tests now separately cover
unbounded maximize and bounded maximize, and keep restore-position/dimension
assertions. Verification is still in progress.

The approved unbounded/bounded native maximize cases pass without changing
SDL's size-limit policy. Separate permanent tests reproduced restore-position
loss during both maximize and minimize (Left=0 instead of 80), and explicit
placement being overwritten by screen/owner centering (Left=560 instead of 80).
See `window-state-contract-red.trx` and `window-placement-red.trx`. The old host
explicitly prioritized finite coordinates and retained them outside Normal;
these are independent of the approved maximum-size compatibility change.

SDL's placement owner now prioritizes explicit finite Left/Top, including when
assigning an owner; bounds notifications retain the model's restore coordinates
outside Normal. The relevant native/unit/platform group passes 28 tests with no
failures/skips (`window-state-contract-green.trx`). Canonical Window and
WindowStartupLocation remarks were synchronized with those verified contracts.

The faithful native window fixture now passes six of eight cases
(`native-window-faithful-fixture.trx`). The ownership/input case moves and restores
the real mouse cursor through SDL, rather than injecting a position contradicted
by the OS cursor. The live move/resize cases pump SDL's native event wait during
the modal loop, bounded to two seconds, without calling the framework's ordinary
frame pump. Both now observe presentation before the native size/move loop ends.
No production input-routing change or live-resize timing workaround was needed
for those three previous fixture failures. Only icon availability and the client
resize-grip native hit target still fail in this group.

## Native window feature parity

The original eight native migration cases now pass (8/8, zero skips,
`native-window-features-green.trx`). The two reproduced gaps were owned by the
SDL platform integration: CanResizeWithGrip never registered SDL hit-testing,
and a process without an executable icon had no system-icon fallback.

The platform now registers a rooted SDL hit-test callback for the Windows
system-sized lower-right client corner, restricted to normal resizable windows.
SDL continues to own the native window, event loop and actual resize operation.
The same platform assembly supplies the borrowed Windows IDI_APPLICATION icon
only when SDL's window/class has no application icon; no icon is destroyed or
replaced when SDL already supplies one. Native handle access uses SDL's published
window-property interface. This is platform-specific presentation integration,
not restoration of the removed Win32 host or graphics backend.

References: [SDL hit-testing](https://wiki.libsdl.org/SDL3/SDL_SetWindowHitTest),
[SDL native window properties](https://wiki.libsdl.org/SDL3/SDL_GetWindowProperties),
[Windows shared system icons](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-loadiconw).
Canonical ResizeMode documentation now identifies the Windows-only client grip
and the unchanged CanResize behavior on other platforms. No documentation pages
were added or renamed. Expanded native boundary and single-axis maximum-size
checks are running; the full affected and repository gates are not yet green.

## Blocking SDL single-axis maximum-size defect

The expanded window group passes 35 cases and fails two, with zero skips
(`window-expanded-contracts.trx`). New native cursor checks pass, including
WM_SETCURSOR after real pointer movement. The grip's system-metric boundary,
outside-corner rejection and maximized-state rejection pass. The graphics
factory rejects a foreign surface before device creation. The native window
class has no background brush during the modal presentation tests.

The two new failures exercise the approved maximum-size contract independently:
MaxWidth=700 with MaxHeight=infinity maximizes to client width 1920;
MaxHeight=600 with MaxWidth=infinity maximizes to client height 991.
Both-finite and both-unbounded tests continue to pass. The same failures repeat
in the complete SDL suite: 336 passed, 2 failed, 0 skipped, 338 total, 1m22s
(`sdl-affected-native-window-features.trx`). No other SDL failures occurred in
that run; this does not resolve the previously recorded allocation intermittence.

The loaded native artifact reports file/product version 3.4.14.0. In SDL's
[release-3.4.14 Windows message owner](https://raw.githubusercontent.com/libsdl-org/SDL/release-3.4.14/src/video/windows/SDL_windowsevents.c),
WM_GETMINMAXINFO sets constrain_max_size only when both max_w and max_h are
nonzero, then sets both tracking limits together. The inspected current upstream
main source still contains that condition. Cerneala maps infinity to SDL's zero
(unbounded) argument, so the dependency drops the other finite axis. No native
hook, arbitrary substitute bound, clamping-after-resize, dependency patch or
weakened expectation has been introduced to conceal this defect.

The approved product contract is not being reopened. Resolving the dependency
defect now requires an explicit architecture/maintenance decision: maintain a
patched SDL native build (fix at the owner), or authorize a Windows-specific
constraint hook in Cerneala's SDL platform as a compatibility workaround. Neither
is silently in scope. Implementation stops at that decision; the old Win32 test
file has not yet been retired, and the remaining migration/build/performance
gates are still open. Canonical Window and WindowState pages disclose the
observed limitation without redefining it as supported semantics.

All four changed canonical window pages have exactly one existing manifest
entry, resolve to files, contain Definition and Remarks/Examples sections and
have no TODO/TBD/PLACEHOLDER markers. git diff --check passes (only line-ending
warnings). The index was refreshed after the last C# edit. No temporary harness
or screenshot was created, and no human/manual validation is claimed.

## SDL 3.4.16 verification (no migration dependency change)

At the user's request, the same eleven Windows-native window contract tests
were run against the official, unmodified SDL 3.4.16 win-x64 release binary.
The release archive SHA-256 matched the GitHub asset digest. The existing test
output was copied into an isolated generated probe directory; only that copy's
native SDL library was substituted. No source, project, NuGet cache or original
build output was changed for this experiment.

Result: 9 passed, 2 failed, 0 skipped, 11 total, 6 seconds. The two failures are
unchanged: width 1920 with MaxWidth=700/MaxHeight=infinity, and height 991 with
MaxHeight=600/MaxWidth=infinity. Both-finite and both-unbounded maximize, restore,
ownership/input, cursor, icon, grip and live move/resize cases pass.

The testhost process module list confirmed loading the isolated official DLL,
file version 3.4.16.0, SHA-256
1F98969319302A100931F4385E5918A0BD53AB07773040682D22E7EDB54858C0.
The release source still contains the same max_w && max_h condition in
WM_GETMINMAXINFO. Source:
https://raw.githubusercontent.com/libsdl-org/SDL/release-3.4.16/src/video/windows/SDL_windowsevents.c

Evidence is under artifacts/monogame-removal/sdl-3.4.16-probe/: provenance.json,
loaded-module.json, result.json, native-window.log and sdl-3.4.16-native-window.trx.
The original 3.4.14 native DLL hash was checked unchanged. Cleanup was attempted
only against the validated generated probe work directory, but the execution
policy rejected deletion. Consequently the downloaded archive, extracted DLL
and isolated test-output copy remain under that directory; cleanup is pending.
This is a focused Windows x64 diagnosis, not full-suite certification of 3.4.16.
No SDL clone/fork, upstream patch, remote publication or permanent dependency
upgrade was performed.

## Graphix fork created under the authorized personal account

The user selected the name Graphix and authorized the currently authenticated
personal GitHub account. gh identified Chevalier12. No existing Graphix repository
or SDL fork was found in that account. Created the real GitHub fork:
https://github.com/Chevalier12/Graphix (parent libsdl-org/SDL, fork=true).

A separate clone is at C:/Users/lauri/Desktop/Graphix. Its graphix branch starts
at SDL release-3.4.16 commit fa2c02bb6e21974a89ea9824bc53c9932abe5f9c and contains
one documentation-only identity commit, 6bff0720f28a53fc83acb267e83b44d535bd45b1.
The GitHub default branch is graphix. Upstream history was not rewritten;
origin points to Chevalier12/Graphix and upstream to libsdl-org/SDL. Cerneala's
origin remains unchanged.

Graphix README states the actual reproduced defect, independent fork identity,
SDL provenance and the difference in AI contribution policies without alleging
that an upstream patch was submitted or rejected. LICENSE.txt remains exactly
unchanged (git blob e9adee4484511b4b1730461679fed21d9b13d88e). The original SDL
README is preserved exactly as README-SDL.md (blob
68b0923a7f55455bb5bf6f27125c05a7427cc008). Graphix-specific CONTRIBUTING.md,
AGENTS.md, CLAUDE.md and PR template disclose AI assistance and preserve the
upstream policy distinction. GRAPHIX.md records the base, changes and actual
verification status. All seven changed paths are documentation/policy only;
no native implementation, header, library name or ABI was changed.

Verification: git ancestry check, exact license/README blob comparisons,
documentation-only diff allowlist, local Markdown link checks, git diff --check,
clean clone worktree, and remote default-branch/fork-parent/commit identity.
No native Graphix build, testautomation run or patched binary is claimed.

The local toolchain check did not find CMake or a C/C++ compiler on PATH;
Visual Studio's installer query found no VC.Tools.x86.x64 component, and its
standard bundled CMake executable path does not exist. No system components
were installed. The fork retains upstream workflow files, but no Actions run
was triggered or claimed. Choosing/provisioning the native build environment
is still needed before verifying an SDL source fix and continuing the consumer
migration. Cerneala still references its existing 3.4.14.1 native packages.

## Graphix native dependency preparation (2026-09-06)

The user selected versioned Graphix NuGet packages and approved installation of
the C++/CMake toolchain, then approved six-RID GitHub Actions builds without
NuGet publication. Graphix's contribution/agent policies were separately
removed at the user's request; its current published tip is
688e273dc85630a5d6eff02b499b6edf3ab02dc1. Earlier entries describe historical
state, not the current policy files or toolchain availability.

Visual Studio's signed installer added the MSVC x64/x86 tools, CMake and the
Windows 11 SDK. The installer returned 3010 (restart requested); no restart
was performed. The installed compiler and CMake worked without restarting.
MSVC 19.51 and CMake 4.3.1 built the unchanged Graphix native source in Release
with warnings treated as errors and static CRT. The first CTest run passed
25/25 in 66.41 seconds; the new Graphix package build recipe's CTest tree later
passed 25/25 in 56.34 seconds. CTest uses dummy audio/video, not real desktop
window validation.

Graphix now has uncommitted build/pack scripts, a native-only Graphix.Native
NuGet project and a manual six-RID CI workflow. Native filenames and SDL's ABI
are preserved. Packaging requires all six matching-commit RID payloads and
checks hashes, archive layout, license and provenance. The incomplete local
RID set was rejected as intended; no complete package exists yet. The new
workflow passes actionlint 1.7.12. An inherited commit-message interpolation
warning in SDL's build.yml was reproduced at HEAD, not introduced by this
change; that controller is now disabled for Graphix. CI commit/push/run and
the GitHub authentication workflow scope remain pending. There is no NuGet
publication or permanent package-feed decision.

The isolated Graphix win-x64 probe loaded SDL3.dll with SHA-256
8b5b00ee2b19fac0bf8f3e42303294d7855dc6b42f6d999de5f918e23b9ef6cd.
The permanent dependency provenance test was RED on the existing SDL3-CS
runtime revision and GREEN against Graphix's SDL_GetRevision(). This is an
isolated dependency experiment, not a completed NuGet migration.

An additional ownership/input fixture failure was reproduced on official
SDL 3.4.16, Graphix, and eventually the existing 3.4.14 runtime with observation
enabled. SDL received a key event with WindowID=0 before keyboard focus;
Cerneala correctly did not assign it to the owner window. The permanent
fixture now uses a real native click, waits for native focus, sends a scan-code
key event, and releases input/restores the pointer in cleanup. Production input
routing was not changed. The fixture passed on the existing runtime and then
three runs each on official SDL 3.4.16 and Graphix.

With that fixture corrected, the Graphix native window/provenance run produced
10 passes, 2 failures, 0 skips. Only the known independent-axis maximum-size
failures remain: width 1920 with maximum 700, height 991 with maximum 600.
Graphix's native implementation remains unmodified. The prior SDL3-CS Metal
fence-query patch was checked against its release provenance; SDL 3.4.16 already
contains the corrected negation. No macOS runtime test is claimed.

Evidence: artifacts/monogame-removal/graphix-native-provenance-red.*, the
graphix-input-fixture-3.4.14 and graphix-input-observation-3.4.14 logs here;
Graphix's ignored out/evidence/baseline-win-x64/, out/cerneala-probe/baseline/
and out/graphix/3.4.16-graphix.1/win-x64/logs/ contain native build, module,
comparison and CTest evidence. Cerneala still references SDL3-CS native
3.4.14.1; cross-platform builds, actual package integration, the Graphix native
fix, broader/full-suite verification and human validation are not complete.

## Graphix CI published; Windows ARM64 process-test gate is RED

After explicit approval of the eight-file change and commit message, committed
and pushed Graphix 7c52ac5efcdb741f87205be8750c3f20a4e48b02:
`build(graphix): add six-RID native NuGet build pipeline`.
Verified local graphix and origin/graphix at the same commit, with no tracked
or untracked source changes remaining in Graphix. The commit excludes all
Cerneala changes and generated Graphix build/test artifacts. Existing Git
credentials permitted the normal push and workflow dispatch; the earlier
prediction that an additional workflow scope would block them was disproven.
No authentication scopes were changed.

Dispatched https://github.com/Chevalier12/Graphix/actions/runs/33996830811
for version 3.4.16-graphix.1. The inherited all-platform push workflow was
skipped as intended. All six native builds succeeded. Downloaded CTest JUnit
reports show 25 tests, zero failures and zero disabled tests on win-x64,
linux-x64, linux-arm64, osx-x64 and osx-arm64. Windows ARM64 ran 25 tests with
one failure: testprocess terminated with SEGFAULT while executing
process_testStdinToStdout. The last observations confirm creation of the child
process and retrieval of its stdin/stdout streams; there is no crash stack.

A second run of the failed ARM64 job, without source or recipe changes,
failed in the same test again. Seeds were PYYDU0DMT75FOLTR (first run) and
FV7817M2BX373BWP (second run). Local x64 executions of the focused test with
the first seed passed both normally and with --randmem. Those passes do not
explain or invalidate the native ARM64 failure. Initial direct launches had
a missing-DLL setup error; the corrected launches used CTest's native DLL
search path and are the only valid local comparison runs.

The owner of the ARM64 crash is not yet established: native runtime, test
fixture and toolchain remain possible owners. No native source or test was
patched speculatively. Package assembly was skipped by the failed gate; no
complete Graphix.Native package was produced or published. Evidence lives in
Graphix/out/ci/33996830811/, including the first run's six JUnit reports,
failed.log, failed-attempt-2.log and process-ready-x64-comparison.json.

The user rejected temporary-local-feed integration and selected nuget.org
as the permanent public feed. This selection did not authorize publication
of a concrete package. Cerneala's native dependencies remain unchanged while
the six-RID package gate is RED. The independent-axis window-sizing fix is
also still pending.

## Process-fixture overread isolated and corrected locally

The user authorized extending the work to isolate and repair the Windows
ARM64 crash before package integration and the window-sizing fix.

A temporary native x64 diagnostic copied the existing process test unchanged
except for installing a guarded SDL realloc allocator before test startup.
Fresh allocations were filled with nonzero bytes and ended at a protected
page; ordinary malloc/calloc behavior and the native Graphix DLL were retained.
With seed PYYDU0DMT75FOLTR, the unguarded test passed but the guarded original
test failed with an access violation at the same stdout-transfer phase. The
stack was `process_testStdinToStdout -> strstr`; the read hit the guard page
after a 5120-byte allocation. This establishes an overread in the test's
unbounded search of an unterminated dynamic stream, not a reason to modify
the process runtime.

Graphix/test/testprocess.c now uses SDL_strnstr with total_read for that search.
With the identical guarded allocator, each of the two recorded CI seeds passed
three times, preserving the full 1 MiB byte comparison and lifecycle assertions.
The Windows x64 Release CTest suite passed 25/25 in 57.15 seconds afterward.
No production native implementation or public API was changed.

A separate manual Graphix process-diagnostics workflow is prepared to run
the existing test with MSVC 2026 AddressSanitizer and symbols on x64/ARM64,
against exact source commits. It preserves text logs only and cannot produce
or publish packages. Four PowerShell steps passed syntax checks; actionlint
passed with only its stale runner-label diagnostic excluded after checking
GitHub's official Windows ARM64 VS2026 image documentation. The original
six-RID release gate is unchanged.

The corrected test, diagnostic workflow and Graphix change record have not
been committed or pushed. Native ARM64 RED/GREEN confirmation and complete
package assembly still require CI. An earlier request to approve a diagnostic-
only two-file commit is superseded by this three-file test-fix scope.
There are no Graphix repository Actions secrets and no NUGET_API_KEY in this
process; a publication account/authorization has not been established.

Evidence is in Graphix/out/ci/33996830811/guard-probe/ and the
ctest-process-fixed-x64.log/xml files. The intentionally crashing probe left
no child process running. Cleanup of the exact, validated temporary directory
Graphix/out/experiments/process-guard/ was rejected by execution policy; its
source and binaries remain ignored there pending cleanup. No bypass was used.
Cerneala's SDL native references and the independent-axis sizing behavior
remain unchanged.

## Process-fixture correction published for native CI confirmation

After explicit approval of the three-file scope and message, committed and
pushed Graphix 64d5819b479751ba988f21b8a8b7a74a84a4dafd:
`fix(tests): bound process stdout marker searches`.
The commit contains only test/testprocess.c, the opt-in native diagnostics
workflow and GRAPHIX.md. Cached diff/whitespace checks passed. Fetch followed
by a normal push left graphix and origin/graphix at the same commit (0/0).
All Cerneala work and ignored Graphix experiments/artifacts were excluded.
Git identity was supplied per command using the same author identity as the
previous approved Graphix commit; no Git configuration was changed.

Dispatched the approved native CI runs:

- Original source 7c52ac5: https://github.com/Chevalier12/Graphix/actions/runs/34018245034
- Corrected source 64d5819: https://github.com/Chevalier12/Graphix/actions/runs/34018246220
- Six-RID package gate: https://github.com/Chevalier12/Graphix/actions/runs/34018247187

The diagnostic runs use the corrected workflow revision but check out their
respective exact source_commit inputs. Results and package assembly remain
pending at dispatch. This operation does not publish to NuGet or change
Cerneala's package references.

All three native CI runs completed. On the original source, each recorded seed
failed under AddressSanitizer on both x64 and ARM64 (four valid RED runs).
All four reports identify heap-buffer-overflow through SDL_strstr at
test/testprocess.c:503: a 32769-byte read crosses a 32768-byte allocation.
On the corrected source, both seeds passed on both architectures (four GREEN
runs, each Total=1 Passed=1 Failed=0 Skipped=0). No native runtime workaround
was needed. Logs are retained in Graphix/out/ci/34018245034/ and 34018246220/.

The six-RID release-recipe gate also passed: 25/25 tests, zero failures and
zero disabled tests on each platform (150 CTest entries total). Downloaded
JUnit durations were win-x64 37s, win-arm64 37s, linux-x64 34s, linux-arm64 32s,
osx-x64 45s and osx-arm64 43s. These dummy-driver suites do not certify desktop
window sizing or GPU behavior.

Package assembly succeeded and verified the native payload, provenance,
license and documentation bytes. Downloaded the resulting
Graphix.Native.3.4.16-graphix.1.nupkg to
Graphix/out/ci/34018247187/package/. SHA-256:
fc83c63d8d73d5a9fc14f71f23783c6fa71b424db3fcde7f441b81afcf2dc2f8.
Independent local archive verification matched all 12 native file hashes
against the six manifests, each pinned to source 64d5819b479751ba988f21b8a8b7a74a84a4dafd;
there are no managed lib/ref assets. The nuspec records that exact repository
commit. Results are in Graphix/out/ci/34018247187/package-verification.json.

The package is a CI artifact, not yet published to nuget.org. The publishing
owner/account and authorization remain unresolved. Cerneala's dependency
replacement, the independent-axis maximization fix, broader/full Cerneala
verification and human validation remain pending. Temporary harness cleanup
is still outstanding as previously recorded.

## Local Graphix integration and native maximum-size fix

The user explicitly approved a temporary local feed on 2026-09-06, superseding
its earlier rejection. NuGet.Config adds artifacts/graphix-packages without
clearing existing sources or changing user-wide configuration. Replaced the
native SDL3-CS platform providers in the SDL platform, benchmarks and shader
compiler projects with Graphix.Native. Managed SDL3-CS 3.4.14.1 and ShaderCross
3.0.0.9 remain unchanged. The initial real restore used the verified immutable
3.4.16-graphix.1 CI package; NuGet metadata records the local feed as its source.
The package cache was not populated by hand and no package was published.

The baseline native/provenance run passed 10/12 with exactly the two known
single-axis maximum failures. The full SDL suite had 334 passes and five
failures among 339 tests, zero skips: those two maxima, an input pointer value
of -1 instead of 45, a warmed text-cache allocation measurement of 4776 bytes
against 1024, and an opaque-stroke/text MSAA comparison differing by 47/255.
Focused allocation and input cases passed, but the rendering difference
reproduced. An isolated DLL-only comparison passed all three rendering cases
with SDL 3.4.14 and failed only the text case with both Graphix and official
SDL 3.4.16. This establishes dependency sensitivity, not the invariant owner.
Additional cold/warm comparisons found identical results within each variant;
the hidden-versus-no-hidden difference remains 47 across 18 pixels after
warmup. Temporary observational test instrumentation was removed. No rendering
or input production change was made on that evidence alone.

Graphix's permanent native window regression drives WM_SYSCOMMAND/SC_MAXIMIZE
through 36 bordered/borderless scenarios (six axis-limit cases, three cycles).
The original native source failed 30 of 400 assertions. Separate finite-axis
flags in WM_GETMINMAXINFO repaired bordered windows; measured outer/client
rectangles then isolated the borderless failure to WM_NCCALCSIZE expanding a
constrained window to the whole work area. Intersecting that rectangle instead
of replacing it yielded 400/400 passes. The final local native CTest suite
passed 25/25 in 55.09s. The original Cerneala maximum cases passed with that
corrected DLL in the isolated probe; its input case still failed intermittently.

After explicit approval of the exact four-file scope and message, committed
and normally pushed 0c23f43af6884849165ebf21ba1d14fa2d6cdf51:
`fix(windows): honor independent native maximum window dimensions`.
Graphix and origin/graphix were verified at 0/0 and the same remote SHA. No
Cerneala files or ignored experiments were committed. No force push or release
was performed. Actionlint, PowerShell syntax and cached whitespace checks passed.

CI https://github.com/Chevalier12/Graphix/actions/runs/34020135259 passed all
six native CTest suites, 25/25 each, zero failures/disabled entries. Durations:
win-x64 38s, win-arm64 37s, linux-x64 33s, linux-arm64 32s, osx-x64 38s and
osx-arm64 38s. The new real Windows maximize gate passed 400/400 assertions
with zero skipped cases on both native x64 and ARM64. Package assembly passed.
Downloaded Graphix.Native.3.4.16-graphix.2.nupkg SHA-256:
76cd49998c816a20d8d6b029553c3765ae7b920be97130ac7157ee8a8e96912b.
Independent archive verification matched all 12 native files to six manifests
pinned to 0c23f43a and confirmed no managed lib/ref assets. Copied those exact
bytes into the approved local feed and updated the three project references
to the new immutable version. Cerneala restore/runtime verification of this
final package remains pending at this checkpoint, as do broader/full suites,
the three additional findings, cleanup and human validation.

Evidence: artifacts/monogame-removal/graphix-local-*, alpha-observe.log,
Graphix/out/evidence/windows-maximize/, out/cerneala-probe/dependency-64d/ and
out/ci/34020135259/. The final package's Windows x64 DLL SHA-256 is
e5594af003e42c0b4285bc7be9ed576d78643cefdbce65c3b0c68230bb0c9cfd.

Final-package integration verification: `dotnet restore Cerneala.slnx --force`
passed. NuGet's cached package hash matches the downloaded CI archive and its
metadata names the approved local feed. The shader build verified all five
artifacts. The actual final-package native/provenance suite passed 12/12 with
zero skips (`graphix-2-native.trx`). The SDL project then passed 338/339, zero
skips, in 1m26s (`graphix-2-sdl-suite.trx`); only the reproducible 47/255 text/MSAA
case failed. The input and allocation cases passed unchanged in this run,
which does not establish why their earlier runs failed. Removed the obsolete
single-axis Windows limitation from canonical Window and WindowState pages;
existing manifest entries and required sections were checked. The local-feed
setup page now records the final package version, source SHA and archive hash.
The complete solution test command is running; no full-suite pass is claimed.

The complete solution test attempt finished RED. It reports 169 compiler errors
in seven remaining legacy main-test files (MonoGame Prism/cache/conformance and
Win32 hosting); those contracts still need migration, not compile exclusion.
Six completed projects passed: Language 190, Tetris 29, VisualStudio 47,
SourceGen 518, Scene2DImporters 151 and LanguageServer 40. SDL passed 337/339,
with the stable rendering failure and a native foreground-window mismatch.
PreviewHost passed 12/13; PreviewRenderScaleIsIndependentOfDesktopDpi expected
576 pixels but observed 640. All completed projects reported zero skips.
These runtime failures are not classified as unrelated. Desktop interference
between concurrently executing projects is a hypothesis for the foreground
mismatch and must be checked with isolated/serial execution. The shared explicit
TRX filename was overwritten by later projects; the full text log preserves
all summaries/errors. Future solution runs must use distinct/default TRX names.
Evidence: graphix-2-full.log. No passing full-suite gate is claimed.

Further MSAA diagnosis: the native API trace records eight-sample targets and
ordered hidden/text/covering draws in one render pass. No intermediate resolve
occurs between those draws, so that hypothesis was rejected. Extending the
existing occlusion matrix reproduces the defect with gradient-brush text
(46/255), a 2x2 alpha image (27/255), and a constant premultiplied half-alpha
image (25/255). Transparent text and non-overlapping text pass. Thus it is not
specific to glyph caching or channel-write masks. D3D12 passes four of eight
cases in Release and Debug; Vulkan passes eight of eight on the same NVIDIA
GeForce RTX 2060, driver 591.59. No tolerance was relaxed and no GPU production
fix has been chosen. Temporary Cerneala native-call tracing was removed and its
remaining NativeSdlApi diff contains only the prior icon/resize-grip work.

A separate diagnostic defect was reproduced in Graphix/SDL's D3D12 logger:
RegisterMessageCallback with its current null output-cookie pointer returns
0x80070057 (E_INVALIDARG). An opt-in isolated native probe retried with an actual
DWORD pointer and obtained S_OK eight times; an injected application warning
confirmed delivery. With the logger working, the eight-case rendering run
reported 32 clear-color and 32 clear-depth optimization warnings (#820/#821),
but no D3D12 error/corruption messages. That does not prove rendering correctness.
The temporary native probe source was removed; its normalized source blob
matches Graphix HEAD exactly. This logger defect is separate from maximization
and is not included in the committed fix or the immutable .2 package. Its
permanent correction has not been authorized or implemented.

The PreviewHost scale failure also reproduces in isolation: expected 576,
actual 640 (`graphix-2-preview-scale.trx`), so concurrent desktop tests alone
cannot explain it. Its owner remains to be investigated.

Additional evidence: alpha-native-trace.log, alpha-channel-red.log,
alpha-image-red.log, alpha-image-vulkan.log, alpha-d3d12-device.log,
alpha-debug-validation.log; Graphix/out/cerneala-probe/d3d12-validation/
native-callback-validation.log. The Microsoft callback contract is documented
at https://microsoft.github.io/DirectX-Specs/d3d/MessageCallback.html.

### D3D12 rasterizer flag isolation (2026-09-06)

The suspected sample-count difference was falsified by temporary native-boundary
logging. Both SDL 3.4.14 and Graphix-based 3.4.16 use eight-sample color and
D24S8 attachments for this scenario. SDL 3.4.14 passes all eight cases.

An isolated native build changed only RasterizerState.MultisampleEnable to
FALSE under an opt-in experiment switch, keeping SampleDesc.Count=8 and all
Cerneala geometry/shaders unchanged. The eight-case matrix passed 8/8. Running
the same DLL with the switch absent (the shipped TRUE setting) passed 4/8 and
failed the same four text/image variants. This identifies the triggering native
state change, but does not establish the ultimate driver/hardware defect.

Microsoft documents MultisampleEnable as a line-rasterization selector; at
feature levels 10.1+ it does not affect MSAA on points or triangles. All traced
Cerneala draws in this experiment have TriangleList topology. The observed
difference on the RTX 2060 therefore conflicts with that documented expectation.
No permanent flag change, backend substitution, MSAA reduction, or tolerance
change has been made. A compatibility workaround needs an explicit decision;
coverage on other D3D12 adapters is not established.

Evidence: sample-observe-old.log (8/8, actual Eight attachments),
sample-observe-flag-false.log (8/8, Count=8),
sample-observe-flag-default.log (4/8, same experimental DLL).
Contract: https://learn.microsoft.com/en-us/windows/win32/api/d3d12/ns-d3d12-d3d12_rasterizer_desc

The temporary C# and C source instrumentation was removed, the semantic index
refreshed, and the normal SDL test project and local native library rebuilt
successfully. Native source blob equals Graphix HEAD; the immutable
Graphix.Native 3.4.16-graphix.2 package was not modified. Deletion of the new
Graphix/out/cerneala-probe/sample-count directory was blocked by execution
policy despite a bounded path guard; its copied experimental binaries remain
ignored and must not be installed or packaged. No bypass was attempted.
All broader failures listed above remain open; there is no full-suite green claim.

### Cross-adapter and read-only NVIDIA configuration checks

After the user's explicit instruction to fix the invariant owner, not apply a
workaround, an opt-in isolated native adapter-selection experiment ran the same
eight-case test assembly with the shipped MultisampleEnable=TRUE behavior.
Intel UHD Graphics 630, driver 26.20.100.6911: 8/8 pass, actual attachments Eight.
NVIDIA RTX 2060, driver 32.0.15.9159: 4/8 pass on the same experimental native DLL.
Only the adapter-selection switch differed. The native source switch was removed
and the local native library rebuilt; source blob again matches Graphix HEAD.
Logs: alpha-low-power-adapter.log and alpha-high-power-adapter.log.

A read-only NVAPI DRS probe using NVIDIA's public headers queried base/global
profiles and application associations. AA_MODE_SELECTOR_ID (0x107efc5b) is
0x00000001 (AA_MODE_SELECTOR_OVERRIDE), not the predefined 0 (APP_CONTROL).
AA_MODE_METHOD_ID (0x10d773d2) is 0 (AA_MODE_METHOD_NONE). The base and current
global profile report the same values. No application association was found for
testhost.exe, dotnet.exe or Cerneala.Presentation.exe (NVAPI_EXECUTABLE_NOT_FOUND,
-166). The supersampling-replay value is 0; transparency-multisampling has no
explicit setting (-160), which must not be misreported as a successful query.

This establishes that the NVIDIA test environment is not application-controlled
for antialiasing. Whether this causes the pixel failure still requires a
controlled A/B. No SetSetting, SaveSettings, profile creation or driver mutation
was called; the probe only initialized, loaded, read and destroyed its session.
Changing driver configuration requires explicit authorization. No GPU workaround
or permanent rendering fix has been applied.
Evidence: nvidia-profile-read-only.log. Probe source/official headers are under
Graphix/out/cerneala-probe/nvapi-read, not a permanent dependency.
NVIDIA contracts: https://github.com/NVIDIA/nvapi/blob/main/NvApiDriverSettings.h
and https://docs.nvidia.com/nvapi/group__drsapi.html.

The user also requested upstream SDL issues for defects confirmed by this
investigation, with reproductions and evidence and AI attribution. This is
conditional on establishing actual defects. Human manual reproduction has not
been confirmed: the clarification was that the reports concern what the agent
found. Reports must identify the existing runs as automated, not claim that the
user manually reproduced them. No upstream issues have yet been opened.

### Approved NVIDIA setting A/B: hypothesis rejected

The user explicitly approved temporarily setting global antialiasing mode to
Application-controlled and restoring the original value. Preflight confirmed
AA_MODE_SELECTOR=1. The bounded test runner used the unchanged installed
Graphix.Native .2 DLL (SHA256 E5594AF003E42C0B4285BC7BE9ED576D78643CEFDBCE65C3B0C68230BB0C9CFD),
with a 120-second child-process timeout and a finally block for restoration.

- Original Override=1: 4/8 tests pass.
- Application-controlled=0: 4/8 tests pass, same failures.
- Restored Override=1: 4/8 tests pass, same failures.

NVAPI SetSetting/SaveSettings succeeded for both transitions. A fresh settings
reload confirmed 0 during the experiment and 1 after restoration. No other
setting was changed. The configured global override does not explain the
observed failure in this A/B. Restoration is complete, not awaiting human work.
Logs: nvidia-ab-before.log, nvidia-ab-application-controlled.log,
nvidia-ab-restored.log; alpha-nvidia-override-before.log,
alpha-nvidia-application-controlled.log, alpha-nvidia-override-after.log.

### Independent D3D12-only reproduction

A native C++ probe now reproduces the occlusion failure without SDL, Cerneala,
textures, managed bindings, or Cerneala shaders. It creates an offscreen 180x90
BGRA8 target at eight samples using the standard sample pattern. A trivial SM5
vertex/pixel shader consumes position and color only. Premultiplied source-over
blending is ONE / INV_SRC_ALPHA. It draws an opaque thin triangle, an optional
full-target translucent quad, and an identical opaque covering triangle.
Rendering with and without the first triangle should be pixel-identical because
the final triangle covers exactly the same samples. It resolves, fences and
reads back its own offscreen resource; no application/OS screenshot is involved.

The bounded matrix executes 100 repetitions of each of two scenarios per state,
three adapters, two MultisampleEnable values: 1,200 comparisons total.

| Adapter | Flag FALSE | Flag TRUE |
| --- | --- | --- |
| NVIDIA RTX 2060, driver 32.0.15.9159 | 200/200 pass | 100/200 pass |
| Intel UHD 630, driver 26.20.100.6911 | 200/200 pass | 200/200 pass |
| Microsoft WARP | 200/200 pass | 200/200 pass |

On NVIDIA with TRUE, all 100 translucent-intermediate cases fail, with the
representative first/last result maximum delta 24/255 across 29 pixels. The
no-intermediate controls pass. All six runs report zero D3D12 validation errors.
This isolates the failure outside Graphix/Cerneala to the NVIDIA D3D12 path.
It does not provide NVIDIA driver source or a way to repair that proprietary
implementation in this repository. No disabling-MSAA/flag workaround is applied.

Source: Graphix/out/cerneala-probe/d3d12-occlusion/occlusion.cpp (temporary native
reproduction, retained for the requested evidence/reporting, not project code).
Build from that directory in an x64 MSVC environment:
cl /nologo /EHsc /std:c++17 occlusion.cpp /Fe:occlusion.exe /link d3d12.lib dxgi.lib d3dcompiler.lib
Run: occlusion.exe nvidia 1 100; repeat nvidia 0, intel 0/1 and warp 0/1.
Evidence: d3d12-only-100-{nvidia,intel,warp}-{0,1}.log.

### Upstream reporting check

Read-only upstream inspection at SDL main c1ef7793c75c2bde6321ec0c9894ea0b2a7694fe
still finds both the coupled max_w && max_h condition and the null D3D12
callback-cookie argument. Existing closed issue #2379 concerns the older
maximization/max-size topic; the independently unlimited-axis reproduction
must be distinguished. Existing #16182 / PR #16183 explain the line-rasterization
flag change; the now-independent NVIDIA reproduction must not be filed as a
proven SDL renderer defect.

Upstream AGENTS.md explicitly requests no AI-generated bug-report comments or
code. No issue has been submitted. AI attribution and truthful automated-only
validation do not by themselves satisfy that contribution policy. Reporting
requires resolving this boundary with the user; no human authorship/manual
validation assertion will be fabricated.
Policy: https://github.com/libsdl-org/SDL/blob/c1ef7793c75c2bde6321ec0c9894ea0b2a7694fe/AGENTS.md

### Correction: the pre-migration MonoGame occlusion baseline was already RED

The assistant incorrectly accepted and repeated the premise that all MonoGame
tests passed before this migration. The saved pre-modification TRX and full log
contradict that statement for this exact test. This is a correction to the
conversation, not a change to historical expectations or reference artifacts.

Baseline evidence:
artifacts/monogame-removal/baseline/before-removal_net8.0_20260905173313.trx
and baseline/full-suite.log. Both failures use the fully qualified name
Cerneala.Tests.Drawing.MonoGame.AlphaBlendRenderingTests.OpaqueStrokeOccludesEarlierStrokeAroundTranslucentContent.
Text content=1 failed with 47/255; translucent fill content=2 failed with 25/255.
The original empty-content control passed. The main project had 3482 passed,
2 failed and 2 skipped; the complete nine-project run had 4591 passed, 2 failed
and 7 skipped. This predates the migration's source/project modifications.

The surviving old test DLL was initially tried as a baseline, but its test name
already uses Drawing.SdlGpu: it is an intermediate migrated binary, not a valid
MonoGame reproduction. Its three passing results in alpha-monogame-original-binary.log
must NOT be cited as MonoGame results despite that mistaken log filename.

A temporary isolated harness then used the actual preserved MonoGame backend
assembly and original WindowsDxFixture, reconstructing the original test's
same three scenes and reading their offscreen targets. It did not restore any
MonoGame project reference or production source into the active solution.
Fresh measured results on this machine:

| Content | MonoGame D3D11 | Current Graphix-backed SDL D3D12 |
| --- | --- | --- |
| No intermediate content | max delta 0 | passes <=1 |
| Text | max delta 47, 18 changed pixels | max delta 47 |
| Translucent solid fill | max delta 25, 65 changed pixels | passes <=1 |

The fresh MonoGame run matches the saved pre-migration failure values exactly.
Observed MonoGame version 3.8.4.1, NVIDIA RTX 2060, D3D11 feature level 11.0.
The actual native surface is RGBA8_UNORM (DXGI format 28), SampleDescription
Count=8/Quality=0, no depth attachment. Its rasterizer has multisampling TRUE,
antialiased lines FALSE, depth clip TRUE, scissor TRUE, solid fill and no culling.
Native blending is enabled, alpha-to-coverage FALSE, independent blending FALSE,
ONE/INV_SRC_ALPHA with ADD for color and alpha, all color channels writable.
The current SDL path uses D3D12, eight-sample BGRA8 color plus D24S8 depth/stencil,
the standard sample pattern and the same premultiplied blend factors. These
are not identical configurations, but MonoGame does not avoid the observed
occlusion failure in this baseline. The earlier native D3D12-only reproduction
still isolates an NVIDIA execution failure independently of both frameworks;
the observed symptom is not exclusive to the D3D12 path.

Evidence: monogame-parity-state.log (harness exit 1 for the two expected RED
comparisons), monogame-parity-observe.log; source fixtures inspected in HEAD.
Preserved old Cerneala.Backends.MonoGame.dll SHA256:
B797CBF27032509BFBBF738FCEA9B918349E12805E421C81156F137678B6F516.
MonoGame.Framework.dll SHA256:
97063554F0508C29F269892E796603C4C9041242215FFF72B0E1B87570539144.
No production fix, changed tolerance, skip or backend workaround was introduced.
The user's subsequent instruction leaves upstream issue publication aside.

### Draw-call grouping isolation (2026-09-06)

The user asked whether the passing SDL-backed translucent-fill path can provide
Cerneala's general solution. The passing fill test already exercises Cerneala's
DrawingContext through its SDL GPU backend; it is not a separate SDL-only API.
The reason that particular case passes, while text/image variants fail, remains
unresolved.

Tested the hypothesis that merging primitive submissions alone prevents the
occlusion defect. In the independent D3D12-only probe, kept the same vertex
buffer, primitive order, shader, blend state and 8x MSAA target on RTX 2060 with
MultisampleEnable=true. Compared separate hidden/fill/cover DrawInstanced calls
with one combined DrawInstanced call. Each mode ran 100 iterations of the
no-intermediate and translucent-intermediate scenarios (400 comparisons total).
Both modes passed all 100 no-intermediate comparisons and failed all 100
translucent-intermediate comparisons: maximum difference 24/255, 29 affected
pixels, zero D3D12 debug-layer errors. Thus merging draw calls alone is not a fix
for the native reproduction. This does not establish why the actual Cerneala
fill case passes; its geometry and pipeline must not be conflated with the
minimal probe. No production rendering change was made.

Evidence: artifacts/monogame-removal/d3d12-only-batching-0.log and
artifacts/monogame-removal/d3d12-only-batching-1.log. Temporary native probe source
and binaries remain in the previously noted ignored experiment location.

## Preview scale and completed native-window coverage (2026-09-06)

The explicit preview scale was applied to drawing but not native window sizing:
640 logical units at 0.9 produced a 640-pixel frame instead of 576. Four new
platform regression cases (pixel densities 1 and 2, overrides 0.9 and 1.25) were
RED with the expected pixel-size mismatch. SdlPlatformWindow now consistently
uses render scale / pixel density for logical-to-native dimensions and input,
including when render scale is explicitly overridden. No capture resizing,
PreviewHost special case or assertion relaxation was introduced.

Verification: preview-scale-platform-red.trx, four intended failures;
preview-scale-platform-green.trx, all 13 platform tests passed;
preview-scale-affected-green.trx, all 13 PreviewHost tests passed, including the
original native capture regression. Full affected SDL and solution gates remain.

Retired the old Win32WindowPlatformTests.cs only after verifying its migrated
coverage: window-migration-final.trx has 41 passes, zero failures/skips across
SdlWindowPlatformTests, SdlWindowMigrationContractTests and
SdlWindowsNativeContractTests. Cursor mapping/hidden cursor, hovered native
cursor, graphics factory surface validation, resize coalescing, ownership,
process identity, input isolation, icon handles, resize grip, live resize/move,
mouse coalescing and disposal all have corresponding SDL assertions. Separate
GPU devices and the Win32 surface type are retired implementation details;
SDL asserts separate targets/sessions on its intentionally shared device.
Maximization follows the already-approved finite-client-limit contract, while
unbounded maximization still asserts monitor-work-area coverage. This removes
obsolete duplicate coverage, not the shared behavior tests.

## Remaining executor kernel coverage migration (2026-09-06)

Migrated sixteen more native GPU contracts from the still-present legacy
PrismGraphExecutorTests into PrismFundamentalGpuTests (seven) and
PrismAdditionalKernelGpuTests (nine). These preserve the original analytical
reference inputs and numeric bounds: every blend mode / four alpha cases,
channel masks / BlendIf / knockout, dual-backdrop knockout, seeded Dissolve,
five color-profile round trips, fundamental premultiplied-alpha operations,
mask channel / density / invert / transformed edges / feathering, ChannelMixer,
Posterize, translated Transform, six Spherize combinations, deterministic
uniform/Gaussian noise, HalftonePattern area/colors, WaveNoise spectral parity,
ColorHalftone angle sensitivity and LightingEffects height/exposure.

The existing SdlPrismKernelFixture now accepts explicitly supplied sampler-slot
textures and can read RGBA8 targets, in addition to its existing float targets.
This is test infrastructure invoking the shipped shader, not a production
compatibility adapter. Bindings follow the existing HLSL uniform/texture
manifest. No shader or production renderer changed for this batch.

Verification: kernel-fixture-extension.trx, six existing native cases passed;
fundamental-first.trx, seven passed; additional-kernels-first.trx, nine passed;
all with zero skips. These are focused results, not full-suite completion.
The legacy executor file remains until its remaining multipass, cache and
performance contracts have been migrated and verified. Main test compilation
currently still has 159 errors in six legacy Prism files
(main-after-window-migration.log). No compile exclusion was added.

## Multipass, thread ownership, and restore provisioning (2026-09-06)

PrismMultipassKernelGpuTests migrates eight additional native cases: ColoredPencil,
Fresco, three XDoG filters, PosterEdges, BasRelief and Cutout. Inputs, pass packing,
CPU references and original numeric bounds are retained. All eight pass with
zero skips (multipass-kernels-native.trx). The preceding complete SDL run had
360 passes, four alpha-occlusion failures and zero skips out of 364
(sdl-after-preview-and-kernels.trx).

Retained-cache migration exposed missing owning-thread validation. Seven cases
(rent, acquire, promote, invalidate, stale-key invalidation, dispose and lease
release) failed because the wrong-thread operation succeeded. Device-owned Prism
resources now reject these mutations before changing ownership; a rejected lease
disposal preserves the lease for release by its owner. All 27 surface ownership
cases pass, including replacement and the original seven RED cases
(surface-thread-red.trx and surface-thread-green.trx). Full affected native
verification is running; full-solution verification remains blocked by the
remaining legacy test sources, not waived.

Both active workflows previously attempted restore without supplying the pending
Graphix.Native package. Get-GraphixNativePackage.ps1 now provisions the exact
existing artifact and verifies its pinned SHA256, and both workflows call it
before restore. Existing mismatched packages are rejected, never overwritten.
The local existing-package hash check passes. Hosted workflow execution and the
cross-repository GITHUB_TOKEN download path have not been verified here; no
commit, push or workflow dispatch was performed. The 30-day artifact lifetime
remains an explicit temporary dependency limitation, not a permanent feed.

The full SDL suite after owning-thread enforcement completed with 376 passes,
four unchanged alpha-occlusion failures and zero skips (380 total,
sdl-after-thread-ownership.trx). No new failure was introduced by that change.

The old PrismRetainedSurfaceCacheTests coverage is now represented by the 27
PrismSurfaceOwnershipTests cases: transfer and exact storage accounting,
exception-safe unpinning, owner/stale-key invalidation, pinned-storage protection,
entry/byte LRU budgets, zero-budget rejection, transient pressure, hard-cap
fallback, delayed disposal, replacement, forbidden lifecycle metadata, and
wrong-thread rejection. The delayed-disposal case also flushes retired storage
before releasing outstanding leases and confirms their native handles remain
live. All 27 pass (retained-surface-migration-green.trx).

Removed-adapter eviction reason counters, its owner-index visit counter and
MonoGame DeviceReset callback are not SDL API contracts. SDL cache invalidation
returns reusable storage to the device pool instead of requiring immediate
Texture2D disposal; device-resource disposal prevents new work and retires owned
storage after outstanding leases are released. The old duplicate retained-cache
file may therefore be retired without deleting its shared behavioral coverage.

## Retained dependency pruning and additional native contracts (2026-09-06)

The migrated static-frame regression was RED: after a final cache hit, the SDL
executor still rendered the covered Layer node (two diagnostic passes instead
of only root presentation). DumpExecutedGraph identified that exact unnecessary
node. SdlGpuPrismExecutor now walks the shared plan's RootOutputExecutionIndices
and CacheInputExecutionIndices backwards, pins required cached results, and
stops dependency traversal at those hits. It executes only required nodes in
the original topological/painter order and releases all pins in the existing
finally path. The common plan already includes nested-capture dependencies;
no parallel dependency graph or scene-specific bypass was introduced.

The focused native reproduction and adjacent tests passed 9/9
(cache-pruning-green.trx). Complete SDL verification then passed 381/385, with
only the same four alpha-occlusion failures and zero skips
(sdl-after-cache-pruning.trx). This is not an all-green result.

PrismExecutionMigrationTests also preserves the old Euclidean stroke GPU and
executor samples, checkerboard minification bounds, and live-opacity pixel
comparison. Initial raw-stroke failure was a test binding error: the shared
shader uses dedicated sampler 11 for its distance field, while the old adapter
aliased that field from StyleMaskTexture. Supplying both slots restored the
original numeric assertions; no shader changed.

PrismRetainedExecutionTests subsequently passed ten native cases: original
simple-alpha, masked isolated-group and nested scene pixel matrices, final-hit
pruning, and content/structure/parameter/motion/bounds/scale/resource-version
mutations versus fresh rendering (retained-execution-native.trx). Fresh
rendering is obtained by explicitly invalidating all retained results before
rendering, not by adding a runtime cache-off option. Further explicit lifecycle
and partial-hit assertions are still being verified.

The main test build now has 141 errors in five remaining legacy files
(main-after-retained-migration.log). Only the verified duplicate
PrismRetainedSurfaceCacheTests.cs was removed in this batch; no source was
excluded from compilation. Complete test migration, full-suite and remaining
visual/performance gates are still open.

The explicit empty-frame lifecycle cases were RED because Prism diagnostics
still reported the preceding frame's eight passes after all retained entries
had correctly been released. SdlGpuDrawingBackend.BeginFrame now resets its
Prism execution diagnostics alongside its other frame counters. The 12 native
retained-execution cases, including partial-hit capture pruning, empty frames
and reappearance, pass (retained-lifecycle-green.trx); the two original failures
are in retained-execution-lifecycle-native.trx. Full affected verification is
running after this final one-line lifecycle correction.

## Native conformance migration and Hald binding (2026-09-06)

PrismWindowsDxConformanceTests was retired only after 47 SDL replacement cases
passed in conformance-migration-green.trx. PrismBaselineConformanceMigrationTests
preserves all 21 versioned 96x64 golden scenes, the original per-channel 2/255
tolerance, semantic anchors, content coverage, and historical manifest metadata.
No golden or tolerance changed. It also preserves the catalog gallery, fresh
deterministic nested/transform graph dumps, resize/recreation lifecycle, and
versioned JFA+1/Sobel shader-source assertions. MonoGame-specific DeviceReset
event and renderer-option assertions are replaced by the SDL-owned resize,
resource-lifetime, frame-counter and retained-reuse contracts, not recreated APIs.

PrismStyleConformanceMigrationTests preserves the original glow continuity,
isotropy, expanded backdrop and hole/long-distance cases; five bevel profiles;
shadow isotropy; Extrude, Fibers, Mosaic, Twirl and Hald semantic probes. Its Hald
case was RED: expected red/blue swap, actual RGB (0,0,8). Two direct native kernel
experiments passed with the correct resource width/height and cube size, while
three malformed Hald shapes also failed to report their required fallback.
The SDL executor had passed the execution extent as LUT dimensions and zero as
the cube size. It now validates the canonical square level-cubed shape, passes
the LUT extent and level-squared cube size, and bypasses invalid resources with
UnsupportedCapability diagnostics. No shader, sampling tolerance or reference
image changed. All 20 style/resource cases pass (hald-resource-green.trx).

The new baseline test initially added an unsupported assumption that every
repeat is a final cache hit. Diagnostic reproductions showed the unchanged
shared planner explicitly marks unversioned masks ResourceVersionUnavailable
and frame backdrops FrameBackdrop/MissingRequiredDependency. The fixture was
corrected to preserve the original pixel contract and capture reuse without
requiring those intentionally uncacheable dependents to disappear. Original
assertions were not weakened; fresh pass counts and captures are still checked.

Full SDL after the Hald repair passed 413/417 with zero skips; only the same four
alpha-occlusion failures remained (sdl-after-hald-fix.trx). An earlier complete
run had also exceeded the text-cache allocation budget (7104 and 2144 bytes,
limit 1024). Both cases passed unchanged in the focused 25-case rerun and two
subsequent complete SDL runs. Their intermittent cause remains unresolved;
a passing rerun is not a diagnosis. Two attempted builds overlapped active
native test hosts and failed on locked DLL copies; serialized retries succeeded.

The remaining MonoGame surface-pool tests are now represented by 37
PrismSurfaceOwnershipTests cases, including mip-chain storage/172-byte sizing,
compatible reuse, incompatible storage, exceptions, promotion, pinned
invalidation, native-handle disposal, UI-thread affinity, budgets and the original
2048-frame alternating-size bound. A new executor test checks the shared plan's
peak graph leases and proves every allocation is free after retained invalidation.
The removed per-step PrismSurfaceFrame API has no SDL counterpart; lifetime
consumption now belongs to SdlGpuPrismExecutor, not a resurrected adapter.
DeviceReset/Reset destroying active MonoGame resources is not SDL's lifetime
contract: live leases remain valid and retire after their final release.

Five input-contract cases were RED: non-mipmapped invalid dimensions were
misreported as GPU allocation failures (including requestedBytes=-32), and an
undefined format was classified as supported-enum-but-unimplemented. Validation
now precedes accounting/allocation for both mipmapped and non-mipmapped rentals.
The original argument-error contract is preserved, with no native calls or pool
state changes on rejection (surface-input-red.trx, surface-input-green.trx).
PrismSurfacePoolTests.cs was removed after all 37 replacements passed. No source
compile exclusion was added. Full repository verification is still pending.

Backdrop hosting migration now has seven native SDL/UiHost tests plus three
rectangle edge regressions (backdrop-migration-green.trx: 10 passed, zero skips).
Both frozen backdrop PNGs (96x64 and 144x96 at scale 1.5), original pixel/semantic
tolerances, one shared lease per frame, transactional provider replacement without
ownership transfer, expired leases, capture delegate execution, and 16 visibility/
resize/provider-replacement frames with eventual collection are preserved.
RenderPng uses SDL's persistent frame target, not the deleted MonoGame temporary
RenderTarget2D. The removed public MonoGame typed-texture format mismatch cannot
be constructed through SDL's internal lease interface; its rejection/no-readback
boundary is represented by an incompatible public IBackdropFrameLease, while
session format normalization and handle retirement remain separately tested.
PrismBackdropMonoGameAdapterTests.cs was retired after those replacements passed.

Three isolated rectangle cases were RED at physical half-pixel edges (including
scale 1.5 pixel 139,3). SDL AddFillRectangle now uses the existing documented
UiCoordinateMapper AwayFromZero edge mapping before the drawing transform,
preserving the deleted MonoGameDrawMapper's rectangle coverage. The canonical
DrawingContext page records this behavior. Rectangle-only cases passed; the full
SDL run then passed 458/463, with the four known alpha failures plus a different
scaled backdrop mismatch (sdl-after-rectangle-edges.trx).

That second mismatch at (12,15) copied lower UI from the wrong vertical location:
BackdropInput had reduced the 144x96 provider raster to an execution-sized 144x80
snapshot, while BackdropCrop correctly addressed the provider's metadata space.
BackdropInput now snapshots the provider raster dimensions without an execution
origin shift; subsequent crop/filter surfaces retain their bounded execution
extents. This also preserves source pixels needed by transformed backdrop sampling,
not merely the first mismatching golden pixel. The original scaled-host reproduction
and all ten focused cases pass unchanged. Full SDL after this repair is running;
full repository verification remains pending (two legacy test files still remain).

Correction to the initial backdrop repair: the complete SDL run rejected its
full-provider-sized snapshot in two existing bounded-surface tests. That approach
was discarded, not accommodated by changing their expected limits. BackdropInput
now applies provider coordinate/alpha mapping once while sampling directly into
the bounded execution raster; BackdropCrop addresses that raster in its own pixel
space. The golden scenes, rectangle regressions, and all existing executor tests
pass together: bounded-backdrop-green.trx, 37/37. The initial full-snapshot approach
is absent from current source. The four alpha failures are unchanged.

The typed Curves resource test reproduced an SDL MissingResource fallback for a
valid PrismCurvesResource (typed-curves-red.trx). The resolver previously searched
only image resources. SDL now resolves the typed resource, caches the existing
shared 1024-sample PCHIP LUT as a half-float RGB texture, binds its actual dimensions,
and invalidates it when value/identity/version changes. Seven focused cases pass
(curves-cache-green.trx), including native rendered output and exact repeat reuse,
1024x1 R16G16B16A16 storage, deferred old-handle retirement, and measured zero bytes
for 256 warm cache lookups. This is not a claim of zero-allocation frame rendering.
A complete SDL run is in progress. Main test compilation now reports 92 errors,
all in the two remaining legacy graph/retained-cache contract files.

The full post-Curves SDL run passed 461/466, zero skips. In addition to the four
alpha failures, NativeOwnershipInputAndGraphicsLifetimesRemainIndependent read
pointer X=-1 instead of 45 after the native click/key sequence. No input production
code changed in this batch. Its unchanged focused rerun passed; a bounded ten-run
reproduction is in progress. Cause is not established and the rerun is not treated
as a diagnosis (sdl-after-curves-fix.trx, native-window-input-recheck.trx).

Retained dependency migration now passes eight cases: separate owner colors,
four shared raster-key discriminators, independently changed lower-UI/backdrop
versions, and an exception inside actual capture rendering followed by clean
resource release and fresh-pixel parity (retained-dependency-capture-recheck.trx).
The first fault-injection fixture threw during frame analysis, before execution;
that was a fixture error, not a cache defect. The corrected fixture preanalyzes
before arming the failing brush and passes without a production change.

The native input stress reproduced one failure in ten uninstrumented runs and
two in forty observed runs (native-input-observed-34/35.trx). In both observed
failures SDL emitted Enter, Motion(45,35), ButtonDown, ButtonUp, Leave, KeyDown;
SDL mouse focus was null although WindowFromPoint still identified the test HWND.
The adapter's (-1,-1) state follows that Leave event. Ownership of the unexpected
native transition remains unresolved; no input production patch was applied.

Eight portable blend-math cases preserve the original split-feather values and
opaque Multiply/Screen/Difference channel equations. The native 2048-frame
opacity alternation also passes with stable created-surface counts and both
actual pipeline handles, increasing pool reuse, and no fallback. SDL retains
unchanged captures while layer opacity changes, unlike the retired executor's
extra command replay; independent mutation/fresh-pixel tests cover that contract.

The four static executor budget migrations are RED. Each uses eight warm frames,
a GC/finalizer cycle, one further warm frame, and sixteen measured Execute calls
with prebuilt frame contexts and native frame acquisition/completion/readback
outside the allocation interval. Simple allocated 20,992 bytes (1,312/frame);
chained, 48-style stress, and nested each allocated 21,632 bytes (1,352/frame).
Every scene had one reported presentation, zero captures, but peak-live=1 and
one pooled presentation rental per frame. Created surfaces stayed stable.
The old zero-allocation/no-pool-work/zero-transient-peak thresholds remain intact
(executor-budget-migration-red.trx, executor-allocation-migration-red.trx).
No production performance repair or relaxed threshold has been applied.

The legacy PrismRetainedCacheRedContractTests.cs harness was retired after the
58-case combined retained-execution/dependency/surface-ownership/live-opacity
gate passed with native tests enabled and zero skips
(retained-contract-final-migration.trx). Its simple/mask/group/nested cases map
to retained-execution pixel comparisons; mask and group originally used the
same complex helper. Backdrop content/lower UI have separate native version
mutations. Four raster discriminators remain shared-key equality tests because
SDL's renderer does not expose mutable profile/format/capability/shader overrides.
Native capture exceptions now prove release and subsequent fresh-pixel parity.
Owner invalidation/empty frames plus native hosting visibility lifecycle cover
detach, hide, collapse, and replacement. The removed Reset API cannot be mapped
to destroying pinned SDL resources; deferred native retirement is tested instead.
The old private cache-off switch and tunable zero-promotion executor fixture are
not restored: fresh references are obtained through complete retained invalidation,
and rejected promotion's live-lease safety is tested at the owning resource pool.
Private diagnostics/lookup counters from the deleted MonoGame cache are not
invented for SDL. The graph executor file remains until its performance gates
and remaining explicit coverage dispositions are resolved.

Opt-in allocation phase observations accounted for every measured byte:
64 target-wrapper, 0 validation/invalidation, 24 graph/plan retrieval, 0 extent/
reconciliation, 504 diagnostics/command-range state, 272 retained acquisition,
0 host prelude, 448 simple or 488 other execution, and 0 final release bytes per
warm frame (executor-allocation-phases.trx). This identifies boundaries, not every
allocation stack. The temporary executor/test probes were removed and the executor
was byte-compared (line-ending normalized) against its pre-probe source. No
production performance change remains. Main compilation is down to 69 errors,
all in the single remaining legacy PrismGraphExecutorTests.cs file.

### 2026-09-06 window-target allocation owner

The full native SDL suite completed: 479 passed, 8 failed, zero skipped out of
487 (sdl-with-executor-budget-gates.trx). Failures are the four previously
isolated native alpha cases and the four migrated static executor budget gates.
The native input case passed in this run; its earlier intermittent Leave-event
failure remains unresolved and is not considered disproved by this pass.

A smaller permanent target-descriptor regression reproduced 4096 allocated bytes
for 64 WindowRenderTarget property accesses, with and without MSAA. The session
now retains that immutable descriptor for its size-resource lifetime and clears
it on resource release and presentation reconfiguration. Resize, minimize, and
restore assertions cover descriptor dimensions and current texture handles.
Both RED cases became GREEN; the entire graphics-session class passes 23/23
(window-target-allocation-red.trx, window-target-allocation-green.trx).

The original native executor budget reproduction was rerun unchanged:
19,968 bytes over 16 simple executions (1248/frame), 20,608 over the other
scenarios (1288/frame). Exactly 64 bytes per execution were removed. The four
zero-budget cases remain RED; pooled presentation rental and peak=1 remain
(executor-budget-after-target-reuse.trx). No zero-allocation claim is made.
The full SDL suite has not been rerun after this descriptor-only change.
Index refresh and git diff --check completed. The last main build still has
69 errors in the remaining legacy graph-executor test file.

Remaining performance work is not a wrapper substitution. Current retained
acquisition creates a reference-type lease per pin, and root presentation rents
an intermediate conversion surface. Recycling the same disposable lease object
would let stale aliases release a later acquisition; merely exempting conversion
rentals from counters would hide work. A candidate redesign is allocation-free
lease tokens with owner-enforced single release, and conversion during the actual
presentation draw instead of an extra surface. Neither redesign has been applied;
its lifetime, clipping/blending, shader-artifact and visual gates would be required.

### 2026-09-06 approved value-lease ownership refactor

The user approved the internal allocation-free lease/presentation redesign.
Retained acquisition is now a readonly value token with a monotonically increasing
identity. Its resource owner holds the active acquisition state and consumes the
identity once on release. Promotion is visible to all live copies, and old copies
cannot release subsequent acquisitions. Foreign/released promotion is rejected;
owner-thread verification precedes mutation, including late releases after disposal.
Typed equality avoids boxing in executor lease collections. Three nullable out
annotations in the executor were updated for the value type; presentation is not
yet changed.

New warm tests measured 17,408 retained bytes and 23,552 transient bytes over 64
acquire/release operations before the refactor. Retained became zero; transient
first dropped to 6,144 bytes (96/operation). Surface buckets and their linked-list
nodes now live with the actual GPU storage rather than being allocated on every
return. Buckets disappear when their final physical surface is retired. Both warm
cases now measure zero. Single-release, stale-copy, promotion-copy, owner validation,
late-disposal, thread-affinity and existing budget/lifetime tests pass.

A same-sized hard-cap replacement regression was caught during review of the new
bucket implementation: budget eviction could remove the bucket held in a local.
The permanent test reproduced KeyNotFoundException on return. Re-resolving the
bucket after budget enforcement fixed it. The entire ownership class is now 44/44
(surface-value-lease-red.trx, surface-lease-owner-validation-red.trx,
surface-value-lease-first-green.trx [42/43, intermediate],
surface-bucket-last-eviction-red.trx, surface-bucket-last-eviction-green.trx).

The full native SDL suite was then rerun: 487 passed, 9 failed, zero skipped out of
496 (sdl-after-value-leases.trx). Four failures are the existing native alpha cases;
four static budget cases now each measure 10,368 bytes over 16 executions
(648/Execute) and still rent a conversion surface. The ninth failed the native
input fixture's pre-click WindowFromPoint check: the pointer was over HWND 6357538,
not the expected owner HWND 51449104. This happens before input injection and before
any Prism graph is rendered; the test renders only its session clear at that point.
It differs from the earlier unexpected Leave-event failure. Native positioning/input
synchronization ownership remains unresolved; no input production change was made.
The full suite is not green and the main legacy migration remains incomplete.

### 2026-09-06 fused working-color presentation

Approved presentation conversion now runs in the final Cerberus blended draw,
using the existing shared HLSL working-to-output functions. Root and nested
presentations consume the retained source directly. No conversion target is
rented, and blend, stencil, scissor, opacity and destination state stay owned by
Cerberus. Ordinary drawing keeps its original shader. Presentation profiles are
part of batch compatibility; the drawing-resource owner caches and releases the
separate presentation pipeline/shader. Six logical shaders were compiled and
verified in all three offline artifact formats. Native execution is Windows only.

The full SDL run (sdl-fused-presentation.trx) passed 488/498, zero skipped. All
native Prism visual cases passed. Four known alpha failures and four allocation
budget failures remain. Two fake binding fixtures failed because they assumed
all fragment uniforms use the 944-byte catalog layout; the new presentation
manifest intentionally uses one 16-byte vector. Those tests now distinguish the
catalog passes from the final draw and assert both exact layouts and the final
conversion selector. The focused run passed their contracts and added profile
batch-separation/pipeline-cache coverage (presentation-bindings-and-graph-budget.trx;
33 passed, one allocation failure, two opt-in native cases not enabled in this run).

Warm presentation rental counts no longer grow: created surfaces are 6/51/9/11
for simple/styles/chained/nested (one fewer each), reused=1 after the unchanged
warm sequence. Allocations still measured 648 B/Execute; removing GPU work did not
remove those remaining managed allocations. Peak-live diagnostics still report 1.

A permanent isolated builder/optimizer test reproduced 1536 bytes over 64 cached
Optimize calls (24/call), while cached Build calls allocate zero. The optimizer's
cold-path LINQ closure was allocated before its early retained-plan return.
Moving the unchanged cold body to its own method made both tests pass and reduced
the original four executor cases by exactly 24 B/Execute to 624 (9984/16).
The zero-budget gates remain RED (executor-after-optimizer-closure.trx: 2 passed,
4 failed). Full verification after the optimizer change remains pending.

The executor now reuses its privately owned host command-range state between
executions. Reset restores all root transform/opacity/blend/clip/stencil stacks,
updates target dimensions/coordinate scale, and drops abandoned child scopes.
It does not pool state between windows or independent capture ranges. Pure state
reset tests cover complete reset after an abandoned compositing child and zero
warm reset allocation. Existing host-clip continuation and 2048 animated-frame
gates pass. The original static cases dropped by exactly 504 B/Execute to 120
(1920/16), still RED (executor-after-state-reuse.trx: 6 passed, 4 failed).

An isolated native geometry-upload regression reproduced the remaining 1920 bytes
over 16 warmed uploads across the three-slot frame ring (geometry-upload-allocation-red.trx).
The upload's capturing callback allocated 120 bytes each time. RunCopyPass now has
a state-carrying overload; its original Action overload forwards to the same body.
The arena passes value state to a static callback, preserving copy-pass lifetime,
offsets, buffer uploads and render-target resumption without a per-upload closure.
The isolated test and all 23 graphics-session tests pass. All four original static
executor scenarios now measure zero allocated bytes, no captures, unchanged
created/reused surface counts; only their peak-live counter assertion remains RED
(expected transient zero, SDL reports retained pin count one). Evidence:
executor-after-geometry-copy.trx, 27 passed and 4 failed, zero skipped.

The diagnostic ownership was checked against the retired executor and pool:
ObserveLiveSurfaces received ActiveLeaseCount; promotion decremented that count,
and retained pins did not increment it. SDL now samples transient frame leases
before rendering/promoting and after presentation, rather than counting all
retained entries in its graph lookup. Cold-frame positive-peak assertions were
added to all four budget cases, preventing a constant-zero counter from passing.
All four zero-budget cases, 2048 animated frames, graph/geometry/state allocations
and relevant backdrop contracts passed (executor-budget-green.trx: 17/17).

The next full SDL run passed 498/503, zero skipped (sdl-after-presentation-budgets.trx).
Besides the four unchanged alpha failures, one migrated lifetime test overasserted
that observed transient usage must equal the unpruned plan's theoretical peak:
expected 3, observed 2. The shared optimizer calculates lifetimes before retained
promotion/pruning; its peak is a ceiling. SDL promotes completed candidates earlier
than the retired transient pool. The test now checks a positive cold transient
peak within that same ceiling, confirms retained entries exist, and keeps the
original complete-release/invalidate byte-accounting assertions. This is an explicit
correction to the migrated fixture's assumption, not a raised performance budget.

### 2026-09-06 remaining graph migration contracts

The retired Transform test required both mipmaps and an anisotropic source
sampler, not merely a gray checkerboard center. Its native pixel gate had already
migrated, but SDL's sampler descriptor could not express anisotropy. The migrated
request-level gate reproduced that absence (remaining-registry-and-anisotropy-red.trx:
invalid-ID boundaries passed, anisotropy failed with an explicit unsupported
sampler-configuration observation). The descriptor/adapter now forward enablement
and the maximum anisotropy; the drawing-resource cache distinguishes the variant.
Only the Transform resampling kernel's source slot requests it. Ordinary and
other Prism samplers keep their prior configuration. The native Transform pixel
case, sampler-request gate, invalid-ID gates and all execution budgets pass
(registry-anisotropy-and-budgets-green.trx: 10/10, zero skipped).

The compatibility value is 4, matching the removed AnisotropicClamp defaults
([MonoGame SamplerState](https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.SamplerState.html)).
SDL exposes the corresponding enablement and limit in its existing native
[GPUSamplerCreateInfo](https://wiki.libsdl.org/SDL3/SDL_GPUSamplerCreateInfo).
No shader algorithm or artifact was changed for this sampler migration.
The 2048-frame test now checks the actual fused-presentation pipeline handle,
not the obsolete catalog-presentation pipeline variant.

### 2026-09-06 final legacy graph-test disposition

All 501 non-alpha cases in the complete native SDL run pass; the only four
failures in 505 cases are the unchanged alpha-occlusion cases already isolated
outside SDL/Cerneala (sdl-after-complete-executor-gates.trx, zero skipped).
The additional always-on Prism runtime source guard passes independently
(prism-runtime-readback-guard.trx). It checks every runtime Prism C# source,
not only the executor, while the native backdrop test retains its narrower
active-frame acquisition guard.

The final PrismGraphExecutorTests harness is now retired, with these explicit
remaining dispositions in addition to the earlier kernel/retained migration:

- Shared mathematical GPU cases and exact tolerances live in the native Prism
  kernel suites and PrismExecutionMigrationTests. Curves uploads/version changes,
  multi-pass resources, masks, profiles, blends and styles exercise SDL resources.
- Transform keeps its native minification pixel gate and now also verifies the
  actual mipmapped anisotropic sampler configuration and source-slot binding.
- Catalog dispatch/aliases remain covered by SdlGpuPrismExecutorTests and native
  catalog conformance. Invalid IDs are tested at the current owning lookups.
  Removed MonoGame Effect techniques, registry getters and private Try methods
  are not new SDL APIs.
- Backend routing is exercised through DrawingBackend.Render by the native
  execution/hosting/retained suites, not by invoking only an isolated shader.
- The four original warm execution scenarios preserve zero allocation, no new
  surface rentals, no captures, and zero transient peak. The animated scenario
  preserves 2048 frames and stable graph/presentation pipeline handles.
- Private MonoGame renderer-diagnostics accessors, dependency-diff enablement
  and cache-off switches retire with that executor. Shared diagnostics/options
  types remain; SDL execution counters, fallback reasons, resource budgets,
  rejection safety, ownership isolation and reset/deferred retirement have
  explicit current-owner coverage. No replacement public switch is invented.
- Fresh-versus-retained pixels, partial hits, hidden-owner invalidation and
  zero-promotion safety are covered by the already-green 58-case retained gate
  and the expanded 44-case resource ownership suite. Native device-owner
  lifetime is tested without copying MonoGame's immediate-reset semantics.

The old file has no user diff and RoslynIndexer reports no references to its
class. No compile exclusion or test skip replaces it. Main-project and complete
repository verification follow this final retirement; they are not yet green.

### 2026-09-06 solution and documentation closure in progress

After retiring the graph harness, the main project exposed one remaining direct
MonoGame call in TextPipelineTests. Its four DPI cases compare an intermediate
physical position (20.049, 30.049) with canonical phase zero. The shared rasterizer
test now supplies that same phase directly; four SDL tests separately assert the
actual renderer selects zero at those exact positions/scales. No coverage bounds
or rasterization inputs changed. All 39 TextPipelineTests pass
(text-pipeline-migration-green.trx); the complete solution is now compiling and
running rather than failing on the retired types.

Canonical manifest validation found 1119 entries, zero missing pages and zero
duplicate page paths. Current architecture/roadmap and website copy still claimed
MonoGame was available; those claims now describe the SDL-only composition and
explicitly distinguish source migration from completed verification. Historical
benchmark numbers and captures remain unchanged and labeled historical.
The core project's old directory exclusion globs remain intentional protection
against ignored generated bin/obj files left under retired project directories;
they do not include source or reference a retired project/package.

During source inventory I accidentally overlapped indexer query processes after
shell calls yielded. They failed on the query daemon shadow-copy DLL lock. No
source modification was inferred from those failed reads. Exact files were read
through the permitted fallback; subsequent indexer queries use its existing
RI_DISABLE_DAEMON=1 switch and run serially. The indexer itself was not modified.

Complete-solution verification is still running. It has already exposed a
packaged image fixture decode failure, stale catalog metadata expectations, and
the intermittent native pre-click WindowFromPoint mismatch, in addition to the
four established alpha cases. These are not an all-green result and are being
tracked individually.

The complete solution run finished: 4792 tests, 4770 passed, 22 failed, zero
skipped (full-after-migration/, complete log: full-after-migration.log).
Seven projects pass completely. SDL passes 505/510 (four alpha cases plus the
native pre-click window mismatch); main passes 3277/3294. Besides stale retired
file/metadata assertions, main has one image decode failure, four application
screenshot antialiasing failures (zero partially covered pixels), and one
transform/geometric-clip/group-opacity pixel mismatch (black expected, gray 128
observed). The latter failures require their own reproductions; the alpha-driver
finding does not automatically explain them. The new desktop dependency boundary
tests were written after this run's main binary compiled and are being verified
separately before the next complete run.

The migrated dependency boundaries pass: 9/9 focused desktop/input cases, then
32/32 combined architecture and dependency cases (desktop-boundary-migration-green.trx,
all-backend-boundaries-green.trx). Core, Language and SourceGen stay independent
of native SDL packages; the platform owns SDL3-CS and the approved Graphix native
package. Retired adapter-folder exceptions were removed rather than preserved as
allowed coupling. Native/public SDL API boundary coverage also passed in the
complete SDL suite.

A read-only process lookup identified the recurring foreign WindowFromPoint
handle 6357538 as a Chrome window (process 22108), not a Cerneala window. This
identifies the occluding process; it does not establish why the native test
window was behind it. No focus policy, driver setting or application state was
changed by that lookup.

The architecture bitmap was inspected and still showed the two retired runtime
branches. It remains an unchanged historical asset, now linked and explicitly
labeled historical from README and architecture.md instead of displayed as a
current diagram. Getting-started commands, markup hosting guidance and Prism's
old public diagnostics/budget claims were synchronized with the SDL-only source
and existing canonical documentation. No benchmark or image baseline was altered.

The advanced draw-command GPU/source guard and shader incremental-input guard
pass 15/15 cases (advanced-and-shader-guards-green.trx). The retired MGFX style
package assertion now verifies SDL's actual shared-HLSL, wrapper, manifest and
compiler inputs and verification stamp, not a fictitious MGFX-compatible split.

The packaged-relative-path test contained an invalid hand-authored PNG payload:
its IDAT data also fails independent System.IO.Compression.ZLibStream inflation.
The production loader correctly rejected it. The fixture now encodes a one-pixel
red PNG with the existing Skia dependency and additionally asserts decoded RGBA.
All seven image-cache cases pass (image-fixture-green.trx); no loader change.

The migrated drawing-state clip sample (physical 52,36 at scale 1.5) corresponds
to logical (34.67,24), inside the translated triangle x+y <= 64. Requiring black
there was an incorrect migration fixture expectation. The unchanged half-opacity
and black assertions now sample explicit logical single-fill (44,16), overlap
(32,16) and clipped (44,28) points at scales 1,1.25,1.5,2. State restoration still
requires every subsequent unscoped frame pixel to be white. Verification pending.

Drawing-state verification passes 10/10, including all four native DPI cases
(drawing-state-coordinate-green.trx). No production clip/rendering change.

Surface antialiasing diagnosis: DesignPreviewSession has always requested a
single-sample window. Retired MonoGameRenderSurface2DSession.CreateRenderTarget
independently tried 16/8/4/2 samples before single-sample fallback; SDL instead
inherited parentTarget.SampleCount. Four unchanged native application screenshot
checks found zero partially covered pixels. A permanent fake-API capability matrix
is RED for supported 8/4/2 samples (single-sample-only fallback passes), confirming
that the surface was created with one sample despite MSAA support. The first test
compile attempt exposed internal-enum/public-theory accessibility and was corrected
before this valid RED run (surface-aa-red.trx: 3 failed, 1 passed).

The surface now selects the highest supported SDL sample count independently,
using the existing session selector with an explicit requested count. Window
sample requests and fallback diagnostics keep their previous behavior. No preview
configuration change, no new rasterizer, no screenshot path workaround, and no
pixel-threshold change. Native screenshot and broader verification pending.

Surface AA verification: 34/34 session/capability/retained-surface cases pass with
native tests enabled (surface-aa-green.trx); all six real application screenshot
showcase cases pass (surface-aa-window-green.trx), including the four previous
failures at scale 1 and 1.5. RenderSurface2D canonical remarks now describe this
existing surface-owned multisampling contract and device-supported fallback.

The four full catalog test fixtures were inspected before changing only their
kernel-owner metadata expectation from PrismKernelRegistry to the shipped
SdlGpuPrismKernelSelector. All mathematical references, deterministic scenarios,
parameter packing, alpha bounds, profile checks, counts and tolerances remain
unchanged. Their original four full-suite failures are the RED evidence.

All four catalog fixtures pass 211/211 (catalog-metadata-migration-green.trx).
The two backend-neutral retained key/version fixtures moved from the retired
MonoGame test namespace/directory to Drawing/Prism/Cache; all 16 tests pass with
no behavioral edits (neutral-cache-test-location-green.trx).

Release solution build passes with zero warnings/errors (43.92 s,
final-solution-build.log). PrismAudit --check passes: 178 catalog entries,
31 common properties, 216 public Prism types and 7 extended public types, zero
gaps (final-prism-audit.log). Canonical manifest validation: 1119 entries, zero
missing page files and zero duplicate paths. git diff --check passes. FileTree
was regenerated after the final neutral test moves.

The complete native-enabled solution suite is rerunning against these binaries
(final-suite/, final-suite.log); these successful focused/build/audit gates do
not establish a green full suite. No human/manual validation has been claimed.

## Final native-enabled solution run (2026-09-06)

The complete suite at `final-suite/` ran 4,799 tests: 4,792 passed, seven
failed, none skipped. The main project passed all 3,297 tests, including the
133 frozen-reference comparisons and the original antialiasing screenshots.
SDL passed 509/514: the four established occlusion failures remain at
47/25/27/46 per 255, plus one surface-resize resource assertion. PreviewHost
passed 11/13; two initial render protocol requests exceeded the unchanged
60-second limit during the concurrent solution run. Other projects passed.

The surface resize assertion still expected the former single-sample target's
two textures. The corrected independent-MSAA surface owns three: color, resolve
and depth/stencil. Updated only that exact count and documented its ownership;
unchanged-frame and frame-version-change reuse assertions are untouched. Its
RED evidence is the full-suite expected 8 versus actual 9 assertion. Focused
verification and another complete run are required after this fixture update.

Shader artifact verification passed for all six artifacts
(`final-shader-verify.log`). PreviewHost is being rerun independently without
changing protocol timeouts or implementation; its timeout cause is not yet
established. Shader verification and a Roslyn read overlapped the previous
solution test run, so resource contention is a hypothesis, not a conclusion.

The exact surface allocation/reuse regression and its drawing/surface neighbors
now pass 40/40 (`surface-resize-final-green.trx`). PreviewHost passes 13/13 in
isolation with the original binaries and timeouts (`preview-isolated-final.trx`):
the two formerly timed-out tests take 17.32 s and 17.08 s respectively. This
is consistent with load sensitivity, not a proven timeout root cause. No PreviewHost
production or test source was changed. A complete solution rerun without the
parallel shader compiler is the next distinguishing experiment.

Both final process smokes pass through the real SDL runtime:
`final-smoke-multi-window.log` reports mainFrames=2, secondaryFrames=1;
`final-smoke-prism.log` reports mainFrames=2. They are rendering/lifecycle
checks, not manual interaction validation.

Additional packaging gate found during final CI review: the existing shared
publish validator requires Linux `libSDL3.so.0.4.14`, but the integrated immutable
Graphix 3.4.16-graphix.2 package contains `libSDL3.so.0.4.16` for both Linux RIDs.
Calling `Assert-SdlGpuPublishedAssets` on the installed package's linux-x64/native
directory fails for precisely the absent old filename. The unversioned and major
version aliases exist. This is a stale publish contract, not a missing native
library; permanent regression coverage and the validator update are pending.

The publish regression now executes the real shared PowerShell validator against
all six restored Graphix native directories. RED reached both valid Windows
RIDs and failed on Linux's old 0.4.14 filename (`graphix-publish-assets-red.trx`).
Updated the single versioned Linux filename to 0.4.16; aliases and foreign-platform
rejection remain unchanged. GREEN: both Graphix dependency tests pass, including
six validated RID directories (`graphix-publish-assets-green.trx`). The test uses
PowerShell 7, already required by the repository's publish/CI scripts. Actual
published-output checks are being run separately; package-directory checks alone
are not proof of a valid published application.

The second parallel full run has 4,799 tests, with six failures: four alpha,
one PreviewHost initial-render timeout and one native pre-click window mismatch.
Main again passes 3,297/3,297, and the surface-resize assertion passes. The
PreviewHost failure happened before any indexer was started in this run, and no
shader compiler was running, so shader compilation is not a necessary trigger.
Added the actual target process ID to the native click precondition's failure
message without changing the assertion or injecting input into the wrong window.
The parallel solution run includes multiple real-window test processes; a serial
complete solution run will distinguish inter-project interference from failures
that reproduce without competing repository test hosts.

Actual publish validation now passes for all six RIDs using the repository helper:
win-x64, win-arm64, linux-x64, linux-arm64, osx-x64 and osx-arm64. Logs are
`final-publish-<rid>.log`; outputs are `final-publish/<rid>/`. The Windows x64
published executable passes both multi-window and Prism native smoke modes via
`Invoke-SdlGpuSmoke.ps1` (`final-published-smoke/`). Linux/macOS/ARM execution has
not been performed locally; cross-publishing and inspecting assets do not prove
native execution on those systems.

The isolated Windows fixture initially failed 10/11 at its pointer assertion:
SDL local coordinates were (45,35), the native HWND under the cursor was the
owner, but SDL mouse focus was null after Enter/Motion/Down/Up/Leave/KeyDown.
This matches the earlier bounded stress observation; competing test projects
are not a sufficient explanation (`native-window-final-isolated.trx`).

Source inspection identifies a mismatch between the fixture and a native input
path. In Graphix's unchanged Windows implementation, `WIN_WarpMouse` calls
`WIN_SetCursorPos`, marks `SDL_last_warp_time`, and sends SDL motion directly.
The event pump intentionally drops associated WM_MOUSEMOVE messages before the
window procedure can arm native mouse tracking. `WIN_CaptureMouse(NULL)` clears
SDL mouse focus when that native tracking flag is false. In contrast,
`WIN_WarpMouseGlobal` sends only the OS cursor movement.

The fixture now converts the same client (45,35) to screen coordinates, moves via
the OS-only global path, and pumps until native mouse focus enters the owner
before injecting the same click and A key. This exercises native tracking rather
than an immediately synthesized SDL motion event. Exact coordinates, child
isolation, key state, GPU lifetime and destruction assertions are unchanged.
No SDL/Cerneala input production code, retry, arbitrary delay or ignored leave
event was added. Windows fixture GREEN: 11/11 (`native-motion-fixture-green.trx`).
The same ownership/input scenario is being repeated in 40 fresh test processes.

Native input stress closure: 40/40 fresh-process ownership/input runs pass,
with unchanged coordinate/key/isolation/lifetime assertions and no skips
(`native-motion-repeat/run-1.trx` through `run-40.trx`). No temporary native
instrumentation or driver-setting change was needed. The source is indexed.

The latest Release solution build passes with zero warnings/errors
(`final-closure-build.log`, 16.88 s); the final input fixture was then rebuilt
and passed its 11-case Windows suite and 40-run stress. The full native-enabled
solution is running with `-m:1` (`final-serial.log`, `final-serial/`) to avoid
concurrent test projects sharing one desktop and to recheck PreviewHost under
an isolated repository-test workload. All projects and native tests remain
included; no timeout or rendering tolerance was increased.

PreviewHost passes 13/13 in the serial full solution run (1 m 37 s), without a
source or timeout change. The native-enabled CI full-regression command and the
getting-started command now use `-m:1`, matching this verification run. Native
input tests in different test projects share OS-global cursor/focus state; their
process-local xUnit collections cannot reserve the desktop across processes.
Serializing those projects belongs to test orchestration, not application input
or compiler behavior. No test/project filter or skip was introduced. This also
avoids the parallel-only initial-render timeout observed on this machine; the
exact resource responsible for that timeout has not been instrumented/proven.

Serial-run results so far: PreviewHost 13/13 and SDL 511/515, with exactly the
four established NVIDIA alpha failures in SDL; native input passes. A separate
LanguageServer memory gate failed: OpenChangeCloseCyclesPlateauAndBoundedCachesAreReleased
reported retained growth of 35,678,280 bytes (`final-serial_net10.0_20260906185108.trx`).
This is a new observed full-suite failure; it has not been attributed to the
migration or dismissed as unrelated. Its ownership and measurement are under
inspection while the remaining serial projects finish.

The first complete serial run finished: 4,800 total, 4,795 passed, five failed,
zero skipped. Main passes all 3,297 cases; SDL passes 511/515 with only the four
alpha failures; all other projects pass except the one LanguageServer memory
case. The memory case passes alone (`workspace-memory-isolated.trx`).

Its source uses process-global `GC.GetTotalMemory`, while WorkspaceTests had no
nonparallel collection. Other test classes create CernealaWorkspace objects
and protocol servers in that same process (ProtocolTestClient calls
LanguageServerHost.RunAsync in-process; DiagnosticsTests loads repository
projects). The retention metric cannot attribute their concurrent heap growth
to this workspace. Added a nonparallel collection for WorkspaceTests so the
process-wide heap gate runs without those other test collections. The 1,000
cycles, cache-capacity/clear checks, telemetry bound and 32 MiB threshold are
unchanged. No LanguageServer production code changed. Full project verification
and another complete native-enabled serial run are required after this change.

LanguageServer full-project verification passes 40/40 after isolating the heap
measurement (`workspace-heap-isolation-green.trx`, 2 m 3 s). No assertion changed.
The previous TRX records completion timestamps and durations; subtracting those
durations gives inferred overlap of the failed memory measurement with the
in-process completion/navigation requests and a repository-project diagnostics
load. These inferred intervals corroborate the measurement-isolation issue;
they are not a heap attribution or proof of each workload's byte contribution.

A fresh Release solution build and complete native-enabled serial suite are now
running against the final test isolation (`final-verification-build.log`,
`final-verification.log`, `final-verification/`). The entire suite still includes
the four failing NVIDIA cases; they have not been filtered or downgraded.

## Final verification closure

The final fresh build passed with zero warnings/errors in 21.10 s. The complete
native-enabled serial suite finished with 4,796 passed, four failed and zero
skipped across nine projects (4,800 total). The only failures are the four
established NVIDIA alpha-occlusion cases. LanguageServer's 32 MiB memory gate,
PreviewHost's unchanged timeouts, native input, all retained execution budgets,
all 133 frozen-reference cases and all six application showcase screenshots pass.
Final logs and nine TRX files are under `final-verification*` in the local
migration artifact directory. The current-status summary above records the
remaining external and unexecuted gates rather than marking this task complete.

## User-directed retirement of legacy dependency guards — 2026-09-07

The user explicitly requested removal of tests that prohibit MonoGame dependencies.
Removed five dedicated legacy-absence tests and the MonoGame/XNA-specific checks
from mixed boundary tests, together with their obsolete adapter exceptions and
an unused test helper. Current backend/platform boundaries and behavioral tests
remain; historical visual references are unchanged. No production API or runtime
implementation changed in this cleanup.

Both affected test projects were rebuilt and run completely in Release with
`CERNEALA_SDL_NATIVE_TESTS=1`: core 3,302/3,302 and SourceGen 517/517, zero failures
or skips. Evidence: `artifacts/monogame-removal/guard-removal-core.{log,trx}` and
`guard-removal-sourcegen.{log,trx}`. The index was refreshed and the scoped diff
check passed. The full solution suite was not rerun for this test-only cleanup;
this does not change the previously recorded NVIDIA rendering failures.

## User-directed removal of retired project exclusions — 2026-09-07

Removed the twelve MonoGame/WindowsDX/Win32 `Compile`, `EmbeddedResource` and
`None` exclusions from `Cerneala.csproj`; current project boundaries are unchanged.
Initial MSBuild evaluation exposed twelve old generated C# files under the two
retired projects' `obj` directories. The user manually removed only those projects'
`bin` and `obj` directories after the tool rejected deletion. All four directories
are now absent; the old Tetris comparison binary remains present.

After cleanup, MSBuild evaluates the same 1,062 compile inputs and two embedded
resources as before removal. The only `None` differences are three diagnostic
JSON reports produced by the comparison, with no output/publish-copy metadata.
Release solution build passes with zero warnings/errors in 51.47 s
(`artifacts/monogame-removal/retired-exclusions-build.log`). The index and file tree
were refreshed and the project diff check passes. Tests were not rerun for this
project-file cleanup; the preceding affected-project test results remain a
separate verification record.

## Generated MSL newline correction and pre-commit verification — 2026-09-07

The staged whitespace check found an extra final blank line in the new MSL
presentation shader. ShaderCross emits the same double newline for all six MSL
outputs. The user authorized correcting the generator and regenerating the
artifacts, rather than accepting the warning or editing a generated file alone.

The existing embedded-format regression now requires exactly one final newline
for MSL. Before the fix, the MSL case failed for the extra newline while DXIL and
SPIR-V passed (`msl-newline-red.{log,trx}`). `CompileMsl` now normalizes only the
trailing CR/LF sequence before encoding and hashing. Regeneration removed exactly
one final LF from each MSL output; all twelve DXIL/SPIR-V files are byte-identical
to their pre-correction versions. The metadata's six MSL hashes were regenerated.
The verifier still recompiles and compares artifacts; it was not relaxed.

Verification: all six shader artifacts reproduce under `--verify`, and all seven
shader-artifact tests pass with native tests enabled (`msl-newline-green.{log,trx}`).
The final Release solution build passes with zero warnings/errors in 31.96 s
(`msl-newline-solution-build.log`). The complete native-enabled serial solution
run executes nine projects: **4,800 passed, four failed, zero skipped; 4,804 total**
(`msl-newline-full.log` and nine TRX files under `msl-newline-full/`). The only
failures remain the unchanged NVIDIA alpha-occlusion cases for content 1, 7, 6
and 3, explicitly accepted by the user for this commit. `git diff --cached --check`
now passes. Native macOS execution and hosted CI were not performed.
