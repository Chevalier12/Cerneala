# Point+Clamp exact-boundary amendment: current status

The user approved a narrow addition to the logical-image sampling contract:
an image pixel whose center is **exactly on the external logical-image
boundary** uses a geometrically covered interpolation position. True interior
centers, including internal triangle and nine-slice patch edges, retain the
ordinary center UV. Source rectangles and nearest selection are unchanged;
there is no UV inset, clamp-to-crop operation, atlas padding, new public API,
per-crop batch split, or global ordinary-vertex-layout change.

The selected-only renderer candidate uses a 52-byte private Point+Clamp image
vertex, five normalized edge values, and one shared diagonal value. Ordinary
drawing retains its 32-byte vertex and existing shaders. The amended fragment
predicate in `Gpu/Shaders/DrawingImageDomain.frag.hlsl` uses strict outer-edge
signs and regards the shared diagonal as internal only when the two triangle
interiors lie on opposite sides. This is a *candidate*, not accepted code:
the exact Village mirrored-right edge is still RED.

## Permanent RED and the narrower GREEN

- `external-boundary-amendment-red-01.log` / matching SDL TRX: the exact
  integer-crop left-edge test failed for the intended reason before changing
  the fragment shader. Selected Point+Clamp sampled half magenta guard at an
  external-boundary center; an extracted crop sampled half green. Safe
  interior selected/ordinary parity had zero differences.
- `external-boundary-focused-01.log` / TRX: after the strict-edge shader
  candidate, the same left edge sampled green for both guard colors, and 18
  focused geometry/native tests passed. The ordinary authored-quad path still
  sampled the guard at that edge; its true interior matched the selected path.
- `external-boundary-village-01.log` / Village TRX: the original six-phase
  fixture appeared green because it did not assert the mirrored right edge at
  phase 0.500. Its raw output still recorded `flippedRight=#ffb7b56a` there.
- The app owner strengthened that existing fixture without changing geometry.
  `external-boundary-mirror-red-01.log` and
  `village-exact-coordinate-red-01.log` show the intended RED at phase 0.500.
  Exact logged coordinates are `ScreenX(0)=267.5`, `ScreenX(32)=347.5`, and
  `ScreenY(112)=550.5` (physical pixels). The TinyTown atlas's Tile43 crop is
  `[112,128)×[48,64)` on a 192×176 PNG. Its left border is grass `#84C669`;
  the adjacent x=111 atlas column is peach `#EAA56C`. The observed
  `#B7B56A` is their 50/50 mixture. The app-owned screenshot at
  `artifacts/ci/scene-village/screenshots/tile-boundary-20260925-010142567/phase-0.500.png`
  shows the stripe along only the mirrored sprite's right edge.

## Backend isolation and falsified hypotheses

`NativeDrawingInterpolationBoundaryTests.VillageScaleFlippedBoundaryUsesCoveredSampling`
is a test-owned raw GPU readback (not an OS screenshot). Its synthetic atlas
uses the actual 192×176 dimensions, `[112,128)×[48,64)` crop, grass and peach
border colors, horizontal flip, 80-physical-pixel sprite size, and contiguous
Tile0-like grass ground. Direct rendering into a 768×768 surface at physical
`[267.5,347.5]` is green, including one-ULP x shifts and layering
(`village-scale-layered-diagnostic-01.log`, `village-ulp-shift-diagnostic-01.log`).

Reproducing the actual RenderSurface2D arrangement—990×550 offscreen target,
quad `[249.5,329.5]×[410.5,490.5]`, parent composite offset `(18,100)`—is
RED with the exact app color: selected `#B7B56A`, extracted grass `#84C669`
(`village-transform-repro-01.log`). Supplying the same physical quad directly
without the ViewBox matrix remains RED; the top sprite alone already reads
peach, so neither the CPU ViewBox transformation nor the grass background
owns the violation (`village-transform-isolation-01.log`).

## Reversible shader diagnostics

The pre-probe selected fragment source, DXIL/SPIR-V/MSL binaries, and artifact
metadata were copied to `branch-color-probe-backup/`; exact SHA-256 values
are in `branch-color-probe-baseline.sha256.txt`. Each diagnostic variant was
compiled with the repository shader compiler and force-rebuilt into the SDL
backend before native execution. The first variant colored center evaluation
red and covered evaluation green while retaining the actual texture binding.
At the reproduced offscreen edge it chose **red/center**; at the direct
control it chose **green/covered** (`branch-color-village-native-01.log`).

The second variant encoded the sign of the right outer edge E12 in red/green
and logarithmic magnitude in blue. At the reproduced offscreen edge E12 was
positive, while the direct control was negative
(`edge-sign-village-native-01.log`). The 8-bit MSAA-resolved blue values imply
roughly `1e-8` magnitude, but are *not* an exact float readback. Mathematically
both pixel centers lie on the external right edge. The normalized edge value's
GPU interpolation changes sign with target position; strict comparison alone
cannot reliably identify equality. This is the supported mechanism. The
diagnostics do not prove that centroid sampling itself fails when chosen.

A final reversible falsification forced `CoveredTextureCoordinate` in the
selected fragment shader at this exact raw-GPU fixture while leaving ordinary
drawing untouched. The selected atlas and extracted-crop frames then agreed:
the layered target was grass `#84C669` and the top-only edge was matching
half-coverage grass (`force-covered-village-native-01.log`). Thus the existing
centroid UV is sufficient at this observed edge **when selected**; the defect
is the edge-classification decision, not a failure of centroid at this pixel.
This forced-centroid diagnostic is not a production fix because it would
change true-interior evaluation and reproduce prior conformance regressions.
The five probe files again were restored byte-equal to the hashes in
`force-covered-probe-baseline.sha256.txt`; the restored backend Rebuild and
10/10 artifact verification passed, and the exact native regression returned
to RED (`force-covered-restored-village-red-01.log`).

All five probe files were restored byte-equal to the pre-probe hashes. The
restored backend was force-rebuilt (`branch-color-restored-backend-rebuild-01.log`),
shader artifacts verified 10/10 (`branch-color-restored-shader-verify-01.log`),
and the original raw GPU reproduction remained RED with the same
`#B7B56A`/`#84C669` disagreement (`branch-color-restored-village-red-01.log`).
The probe variant hashes are retained in `edge-sign-probe-variant.sha256.txt`.

## Gate state

The pre-amendment selected candidate had a full native SDL result of 925 pass,
5 skip, 0 fail (`five-edge-full-sdl-native-01.log`). That result is **not** a
final gate for the amended predicate. The exact mirrored-boundary regression
is RED, so broad conformance, Village acceptance, warm candidate measurements,
and integration have not been claimed. No global-centroid shader, debug-color
shader, epsilon, UV inset, or asset-padding workaround is retained. An
architecture review of a geometrically analytic classification representation
is required before another production change.

## Flat64 analytic candidate and current gate (2026-09-25)

The reviewed follow-on candidate keeps the ordinary 32-byte drawing vertex and
ordinary shaders unchanged. Only provenance-eligible Point+Clamp image quads
carry a private 64-byte vertex with four identical, already-transformed
target-local physical corners. The selected fragment shader tests the ordinary
pixel center against the whole logical two-triangle image union. A strictly
interior center keeps ordinary UV interpolation, including internal diagonal
and NineSlice patch boundaries; an exterior or exact external-boundary center
uses the covered interpolant. There is no UV inset, clamp, epsilon, source-rect
rewrite, per-crop batch split, or change to authored-vertex mesh semantics.
`flat64-final-source-artifact.sha256.txt` records the current source, generated
shader, and Release binary hashes. The shader generator's independent `--verify`
passed 10/10 artifacts (`flat64-shader-verify-01.command.txt`, `.log`, `.exit.txt`).

The 64-byte layout had an intended representation RED before source changes
(`flat64-representation-red-01`). Focused geometry/unit, descriptor, upload,
and native interpolation evidence is in `flat64-geometry-unit-01` (30/30),
`flat64-native-descriptor-01` (1/1), `flat64-payload-01` (2/2),
`flat64-mixed-upload-bytes-01` (1/1), and
`flat64-interpolation-focused-02` (14/14). The original Village native six-phase
fixture passed all six fractional phases after the candidate (`flat64-village-sixphase-01`,
1/1); the strengthened exact mirrored edge, transformed edge, opaque/partial-alpha,
and control observations are retained in its raw TRX and application-owned
`Window.SaveScreenshot` artifacts. At a one-ULP strictly interior center, the
selected Point result matches an authored ordinary quad even where an extracted
texture chooses a different nearest texel (`flat64-one-ulp-parity-01`). This is
the approved ordinary-center contract, not a promise of universal atlas
isolation at every camera phase. Finite-geometry probes passed but do not prove
universal FP32 exactness across all possible finite coordinates or platforms.

The first full native SDL gate failed only at subsequent device creation after
our newly added Village-scale regression constructed two independent
`SdlDrawingFixture`s with overlapping lifetimes. The D3D12 error was
`0x887A0007`; native bisection and temporary no-draw controls are retained as
`flat64-device-*`. This observation does **not** establish a renderer shader
defect or a general multiwindow limitation. Repository test ownership already
states that independent fixtures must not overlap because either fixture's
`SdlPlatformLifetime.Dispose` calls `SDL_Quit`
(`tests/Cerneala.Tests.SdlGpu/SdlGpuGradientCacheTests.cs`, lines 67-68;
`tests/Shared/SdlDrawingFixture.cs`; `SdlPlatformLifetime.cs`). The new test
now disposes its first fixture before creating the second, preserving every
pixel and ULP assertion. Its previously failing Village-scale + artifact-test
pair passed 2/2 (`flat64-device-sequential-pair-01`). The complete SDL native
project then passed 933, skipped 5, failed 0 of 938
(`flat64-full-sdl-native-03.command.txt`, `.log`, `.trx`, `.exit.txt`, 7m26s).

The approved native warm measurement has 1,024 pre-recorded quads, 32 warmup
frames, and 20 measured frames per workload, with no GPU readback in the timed
path. It measures `BeginFrame` + backend render +
`CompleteFrame(present: false)`, not GPU time or actual swapchain presentation;
allocated bytes are for the measured thread only. The focused run passed 1/1
(`flat64-native-warm-02.trx`); the first attempt was a test-only missing-namespace
compile error (`flat64-native-warm-01`) and is not a RED contract result.
Median native CPU time / current-thread allocation / vertex bytes / draw calls:

| Workload | CPU ms | Bytes allocated | Vertex bytes | Draws |
| --- | ---: | ---: | ---: | ---: |
| ordinary Linear images | 2.209 | 760 | 131,072 | 1 |
| selected Point images | 3.877 | 760 | 262,144 | 1 |
| alternating Linear/Point images | 3.977 | 784 | 196,608 | 1,024 |
| alternating Point images/authored Point quads | 1.935 | 760 | 196,608 | 1,024 |

These are same-current-build workload comparisons; there is no matched
pre-change native baseline, no GPU-time result, and no claim of a causal frame
regression from these medians. Raw min/median/p95/max, all 20 samples, and
batch counters are in the TRX. The warm test followed by the shader artifact
test passed in one native process (`flat64-native-warm-device-pair-01`, 2/2),
confirming this measurement did not reproduce the fixture overlap failure.
The fake-API warm A/B remains separate evidence because its string-recording
observer materially contributes to its managed allocation numbers.

This is an integrated candidate awaiting independent audit and final
integration. The separate Core Drawing+Prism 133-case native corpus, final
build-enabled full solution, current-Core API compatibility comparison,
and any platform-specific non-Windows visual conformance are not claimed here.
The 933/5/0 SDL result predates the additional measurement-only test; its
focused native run and same-process artifact pair are recorded above.

## Independent-audit P2 repair (2026-09-25)

The Village-scale one-ULP native fixture previously asserted only the
strictly-interior branch. It now asserts a nonzero center/edge separation and
checks both signed cases: a strictly exterior center must match the isolated
crop, while a strictly interior center must match the equivalent authored
ordinary Point quad. The loop inputs, image setup, fixture lifetime, and all
earlier edge/transform assertions are unchanged.

Audit also found that every `Cerberus` construction eagerly allocated 1,024
selected 64-byte vertices (65,536 bytes) even for ordinary-only drawing. A
permanent test using the existing private-storage inspection helper proved
the intended contract RED before production modification:
`flat64-lazy-storage-red-01` failed expected selected capacity 0, actual
1,024, at construction. The selected array now starts empty; the existing
selected-only reserve/growth path allocates on its first actual use. The test
also checks ordinary allocation leaves selected capacity zero, first and
second selected allocations grow to 4 and 8 vertices, and their merged draw
and rebased indices remain correct. The adjacent mixed upload-arena fixture's
nominal selected byte count now uses the actual 64-byte layout, rather than
the obsolete 52-byte candidate value.

The intended regression became GREEN (`flat64-lazy-storage-green-01`, 1/1).
Combined Cerberus, upload arena, Village-scale one-ULP native, and native warm
measurement focus passed 27/27 (`flat64-audit-repair-focused-01.command.txt`,
`.log`, `.trx`, `.exit.txt`). This is the final focused repair gate, not a
replacement for the full native SDL result or later integration. The repeated
native warm medians in that 27-case run differ materially from the isolated
measurement above; run conditions vary, and neither set establishes a
pre-change performance delta. `flat64-final-source-artifact.sha256.txt` was
refreshed after this repair. Full native SDL and the separate 133-case Core
Drawing/Prism corpus have **not** been rerun after these P2 edits; final
integration owns those gates after the same independent auditor re-audits.
