# Scene Village tile-seam reproduction — 2026-09-24

## Contract and observation

Expected: opaque, grass-colored borders of adjacent Tiny Town path and ground
sprites join without a differently colored line. This is the specific village
art composition under test, not a general assertion that all cropped images
must hide their atlas neighbors under every sampling mode.

Observed: the current Windows SDL_GPU app at its declared 820×600-DIP minimum
produces a 1025×750 app-owned screenshot. Thin, straight off-color bands occur
on the left and bottom edges of the crossing made from tile 43. The original
sprite atlas is unchanged. The current captured RED image is
`C:\Users\lauri\Desktop\Cerneala\artifacts\ci\scene-village\screenshots\native-20260924-184519394\village.png`
(SHA-256 `6C30BA0D8BC9ED94D242093EC6E40CB23ECB16E3605111299DFC24E492130C19`).
It was created by window-backed Servo → `Window.SaveScreenshot`, not OS capture.

The source tile 43 crop is `(112,48,16,16)` in `tiny-town.png`. Its own left
and bottom edge texels are opaque `#84C669`, the same as background tile 0.
The immediately adjacent *outside-crop* atlas texels are `#EAA56C` at x=111
and `#3F2631` at y=64. At the same uniform grass parts of the native image:

| Edge | In-crop safe sample | Visible edge sample | Pixel mapping |
| --- | --- | --- | --- |
| Vertical path left, world x=2032 | x=476,y=203: `#84C669` | x=473,y=203: `#ADB96A` | ViewBox `(1850,1938,396,220)` |
| Horizontal path bottom, world y=2064 | x=633,y=439: `#84C669` | x=633,y=442: `#688653` | Surface `(14.4,102.4,792,440)` DIP |

The edge values numerically match linear blends of the correct grass and the
immediately adjacent atlas colors: `#ADB96A` is approximately 60% grass / 40%
left neighbor, and `#688653` is approximately 60% grass / 40% lower neighbor.
The next pixel inside each edge matches approximately 80% / 20%. This supports
sampling outside the selected source rectangle rather than a geometry gap or
an authored edge in tile 43. The responsible contract/layer remains to be
settled; default crop sampling is `Linear` with texture `Clamp` in the
recorded Sprite2D command, and the canonical docs do not explicitly define
whether a crop must isolate neighboring texels under linear filtering.

## Permanent RED

Before modifying production code, `NativeVillageWindowTests` gained an
app-owned screenshot assertion that maps the known path-edge world coordinates
through the live `ViewBox`, Servo surface bounds, and physical screenshot size.
It first validates safe interior grass pixels, then requires the two edge
samples to match. `seam-red-03.command.txt` is the exact Release command with
`CERNEALA_SDL_NATIVE_TESTS=1` and the existing artifact exclusion. Its raw log,
exit code, and TRX are next to this file. Result: **1 failed, 0 passed**, for
the intended edge-color mismatch above. The app/backend production source
remains unchanged.

The earlier `seam-red-01.*` and `seam-probe-02.*` are diagnostic history, not
valid RED regressions: their horizontal *safe* sample landed on the left edge
of another path tile and failed the fixture's expected grass check. Changing
only the horizontal world sample from a tile boundary to the tile center made
the safe control pass and exposed the intended two-edge violation.

The 21-file pre-seam app/test baseline was hash-verified before the test edit
and copied to `baseline-app-source.sha256.txt`.

## Point opt-in is not yet sufficient

The user chose per-`Sprite2D` sampling control over atlas extrusion. The
independent Core worker added `Sprite2D.Sampling` with default `Linear` and
focused Core/SourceGen evidence. The app now opts **Tiny Town sprites only**
into `Point`; character and stress sprites retain `Linear`. The first native
replay, `seam-green-01`, passed the original path-edge check, but its upward
camera screenshot revealed a remaining one-pixel horizontal line at fractional
camera phase 0.275. A stronger boundary assertion in the routed Servo case
passed once at a different phase 0.685 (`point-boundary-red-01`), so that case
alone is timing-dependent and not an acceptable GREEN gate.

`NativeVillageTileBoundaryTests` is a separate renderer-conformance fixture,
not a fake movement test. It creates a normal SDL window and plain
`RenderSurface2D` with a fixed **initial** ViewBox; it does not call the game
camera or mutate the ViewBox after rendering. Two opaque Tiny Town source rows
join at world y=32: tile 43's last row `(x=120,y=63)` and tile 0's first row
are both `#84C669`. The immediate atlas row outside tile 43's crop,
`(x=120,y=64)`, is `#3F2631`. The fixture has three otherwise identical
columns: tile 43 with Point, tile 43 with Linear, and uniform tile 0 with
Point. It captures through Servo's window-backed `Window.SaveScreenshot`.

At the exact Village scale (792×440-DIP surface, 396×220-world ViewBox,
1025×750 screenshot), its bounded initial-ViewBox phase matrix is RED on the
Point column:

| Physical y phase at tile boundary | Point tile 43 edge | Linear tile 43 edge | Uniform tile 0 edge |
| ---: | --- | --- | --- |
| 0.125 | `#7BB262` | `#7FBA65` | `#84C669` |
| 0.275 | `#739E5B` | `#7AB061` | `#84C669` |
| 0.375 | `#6A8A54` | `#76A65E` | `#84C669` |
| 0.500 | `#84C669` | `#729E5B` | `#84C669` |
| 0.625 | `#84C669` | `#6F9658` | `#84C669` |
| 0.875 | `#84C669` | `#6B8A54` | `#84C669` |

Point-column pixels two rows above/below the edge remain `#84C669`; the
uniform control is pure grass at every phase. The first three Point values
numerically match 1/8, 2/8, and 3/8 contributions of atlas row 64's dark
pixel mixed with grass; the cutoff at phase 0.5 is consistent with center
interpolation outside a partially covered primitive. This is a **falsifiable
hypothesis**, not yet an established root cause: the actual surface sample
count, fragment interpolation, resolve output, and subsequent surface-to-window
composite have not been directly measured. Source inspection shows the surface
requests up to 8× MSAA independently of the application's window-level
`UseMultisampling=False`; the HLSL texture-coordinate varying has no centroid
qualifier. An independent Core diagnostic A/B is pending under root ownership.

`tile-boundary-phase-red-04.command.txt`, `.log`, `.exit.txt`, and `.trx` are
the current exact six-phase RED evidence. The six app-owned PNG paths are in
the TRX/log; for phase 0.275 the path is
`C:\Users\lauri\Desktop\Cerneala\artifacts\ci\scene-village\screenshots\tile-boundary-20260924-193928679\phase-0.275.png`.
`tile-boundary-red-01` and `tile-boundary-controls-red-03` were earlier valid
single-phase RED captures. `tile-boundary-controls-red-02` is diagnostic
history, not a valid regression: it incorrectly required the intentionally
Linear control's nearby pixels to be pure grass, so that fixture check failed
before the Point assertion.

No final fix, all-phase GREEN, affected-suite, or full-repository gate is
claimed yet.

## Controlled shader A/B and strengthened RED fixture

The Core worker performed a **reversible diagnostic**, not a retained fix:
the unchanged six-phase fixture was RED with the original fragment shader and
GREEN when only the fragment texture-coordinate varying used centroid
interpolation. The original shader source and generated artifacts were then
restored. Raw commands, logs, TRXs, and artifact inventories are under
`..\core\diagnostic\`. The shared Astra's scoped review accepted centroid
interpolation as the invariant-owner correction to investigate, subject to
broader conformance gates; it did **not** accept a permanent shader change.

The app test fixture was then strengthened without backend or game-production
edits. Its preflight native window measures the actual surface origin, bounds,
viewport scale, and screenshot pixels-per-world-unit. Six subsequent windows
receive independently computed **initial** ViewBoxes for requested x **and**
y phases 0.125, 0.275, 0.375, 0.500, 0.625, and 0.875. Each captured phase
is asserted within 0.002 on both axes; no DPI or 2.5× scale is assumed by
the test. The fixture now includes:

- Opaque tile-43/grass joins on bottom and left edges with a 3×3 contiguous
  tile-0 underlay and a uniform-tile control at the same layout.
- A separate Linear column. All six captures had 121 different sampled
  interior pixels between Point and Linear, while known uniform grass
  interior pixels remained `#84C669` in both modes.
- A horizontally flipped tile's right join and a 90° rotated tile's left
  join, checked at representative vulnerable phases.
- Two half-opacity sprites over **magenta** clear color, with no opaque
  underlay. Safe interiors are `#C163B4`, consistent with 50% source-over
  of border grass; the join is compared to that measured safe reference.
  This exercises uniform sprite opacity, **not** per-texel alpha cutouts.

The unchanged original shader artifacts were SHA-256-checked against the
Core worker's baseline inventory, then the backend was forcibly rebuilt in
Release (`baseline-backend-rebuild-05.command.txt`, exit 0) so the earlier
diagnostic DLL could not make a false GREEN. The strengthened fixture is
valid RED (`portable-fixture-red-08.command.txt`, raw log, exit 1, TRX):

| Requested/actual phase x/y | Point bottom | Point left | Flipped right | Rotated left | Half-opacity join vs safe reference |
| ---: | --- | --- | --- | --- | --- |
| 0.125/0.125 | `#7BB262` | grass | `#91C269` | grass | `#BC59B0` vs `#C163B4` |
| 0.275/0.275 | `#739E5B` | grass | `#9DBE6A` | grass | `#B84FAD` vs `#C163B4` |
| 0.375/0.375 | `#6A8A54` | grass | `#AABA6A` | grass | `#B445A9` vs `#C163B4` |
| 0.500/0.500 | grass | `#B7B56A` | `#B7B56A` | grass | reference |
| 0.625/0.625 | grass | `#AABA6A` | grass | `#6A8A54` | reference |
| 0.875/0.875 | grass | `#91C269` | grass | `#7BB262` | reference |

The current reference asset is the **unchanged** original
`Playground/Cerneala.SceneVillage/Assets/tiny-town.png`, SHA-256
`3A54D99ECDE790D4FDEA207A3644CF130FC56FA838F1BEB1507C185A95B8E902`.
No new runtime art pipeline or synthetic asset was introduced. The current
fixture source hash at the strengthened-baseline RED checkpoint was
`F240A382DC802A3A2B4DCFB75684BE42CEA28B07FD68BA05F4A722B3961B9911`.
Its app-owned captures are listed in the TRX/log; phase 0.275 is
`C:\Users\lauri\Desktop\Cerneala\artifacts\ci\scene-village\screenshots\tile-boundary-20260924-201705188\phase-0.275.png`.

Actual native **offscreen** sample count remains unmeasured: the existing
public app diagnostics report the window's count, not the independently
selected `RenderSurface2D` target count. An existing fake-SDL test verifies
8→4→2→1 fallback, including a one-sample target, but is not native
single-sample evidence. The Core worker owns any temporary native allocation
instrumentation or direct-window 1× conformance test. No final GREEN or
full-suite result is claimed here.

## Mirrored half-pixel assertion gap — 2026-09-25

The Core worker's external-boundary candidate passed the **old** six-phase
Village assertion, but its raw log
`..\core\diagnostic\external-boundary-village-01.log` recorded, at measured
x/y phase 0.500, Point bottom and left `#84C669` while the horizontally
flipped tile's right edge remained `#B7B56A`. The old fixture asserted the
flipped edge only at phases 0.275 and 0.625, so that passing test was not
evidence that the mirrored join was clean at 0.500.

The same unchanged tile 43 source crop places its opaque grass **left** border
on the mirrored sprite's **right** edge. Opaque tile-0 grass is adjacent in
the scene at every sampled phase. The fixture now includes `FlippedRight` in
the aggregated opaque-join assertion at **all six** calibrated phases, rather
than only the two representative phases. Rotated and half-opacity checks keep
their existing representative phases; no scene setup, screenshot path, or
game-production code changed. Current test-source SHA-256 is
`1AC63162FA50A7750A13D93DBDE4A4E7C1D9D64CB78D300A716C601CB707082E`.

This assertion edit was made while the Core worker held the exclusive
build/native lease, so it has **not yet been compiled or replayed**. The Core
worker's isolated backend-native flipped-edge test is GREEN
(`..\core\diagnostic\exact-flipped-edge-red-01.log`) but does not reproduce
the Village scene's 0.500 result. Ownership of that remaining discrepancy is
still under investigation; neither the assertion nor the observed pixel was
weakened.

Read-only inspection of the app-owned phase-0.500 capture from that passing
but incomplete Core candidate,
`C:\Users\lauri\Desktop\Cerneala\artifacts\ci\scene-village\screenshots\tile-boundary-20260925-005941276\phase-0.500.png`
(SHA-256 `8FA0D7B8C4CFBB8FB264F27CDB5B806C819F5FDEEF043EFC71231C826B107891`),
found x=347, y=511–589 uniformly `#B7B56A`. Adjacent columns x=346 and
x=348 at those rows, and x=347 outside that y interval, are `#84C669`.
The stripe is confined to the flipped sprite's right-edge footprint. Its
color numerically equals 50% grass plus 50% atlas x=111 orange; this
localizes the visible contribution but does not prove the exact GPU
interpolation/coverage mechanism. The Core worker's exact-scale isolated
flipped DrawImage control remains GREEN, so the app-path discrepancy needs
further evidence.

For that experiment the fixture now logs raw round-trip (`G9`) values of
`ViewBox.X/Y`, `ScreenX(0)`, `ScreenX(32)`, and `ScreenY(112)` alongside the
existing pixel probe; source SHA-256 after this **logging-only** change is
`A4259CA328EF53616CE30034E65FC86E115925D56AA79A17440E6DF36FD504EF`.
No build or native run was made by the app worker under the Core worker's
exclusive lease; the new log output remains pending.
