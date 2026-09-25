# Scene Village character-line investigation

2026-09-25, Windows SDL native path. This ledger covers only the app/test
source owned by the Village worker. Repository-wide verification and the
independent audit are separate gates.

## Observed behavior and ownership

- The user's right-facing screenshot has two short dark marks above the
  character. The source `ch003.png` right-walk frames 0 and 2 have a fully
  transparent first row; the preceding atlas row has opaque foot pixels in
  the two corresponding horizontal runs. The native 4-direction × 4-frame
  comparison rendered those right frames with Linear and measured 22 dark
  top-row pixels per frame absent from an isolated-cell visible-pixel control.
- In the actual `VillageArt.Player` native fixture, all 16 walk poses were
  frozen using their real clips and sampled at physical origins
  `(40 + 200*frame, 40 + 200*direction)` with an observed 2.5 physical
  pixels/world-unit. Source alpha verifies that frames 0/2 of each direction
  have 0 opaque top-row texels. Before the fix, their rendered top rows had
  134 non-background pixels in aggregate: Down 0, Up 30, Left 52, Right 52.
  The untouched user-visible invariant is no foreign mark on a source-
  transparent first row. See `character-production-red-01.log` and its TRX.
- The app had selected the global Sprite2D default Linear sampling for the
  tightly packed character sheet. Explicit Point native controls removed the
  foreign top-row pixels for Right and Up without editing the PNG. The
  documented global Linear mode is not a promise of crop isolation, so this
  is an app art/sampling mismatch, not evidence of a Core renderer breach.
- **Correction to the first Up-image diagnosis:** the earlier in-memory
  reader called screenshot rows 52–60 the "near-hands band" and reported
  372 dark matches / 0 false positives. Those rows are mostly head/neck,
  **not the user-reported hands area**. That number was not preserved as raw
  evidence and is withdrawn. The actual broad shoulder/hands ROI in the
  original 80 × 122 crop is `x=[8,58), y=[70,86)`. The reproducible read-only
  comparison in `inspect_up_hands_band.py` and `up-hands-02.raw.txt` fits the
  **whole screenshot silhouette** rather than optimizing only the hands ROI,
  to Up neutral source
  cell row 1, column 1 (column 3 has identical visible RGBA). At 2.5×, 12
  origin phases tie on alpha fit (3128 TP, 362 observed non-background
  pixels outside the nearest binary source mask, 0 FN). Among those
  alpha-optimal phases, the best whole-image nearest-RGB fits are `ox=-7.25/-7.0`,
  `oy=23.25/23.5` with MAE 5.94/channel. With a declared near-neutral black
  mask (`max RGB <= 55`, channel spread `<= 8`), **all 97** observed black
  pixels in the hands ROI map to source-black texels under those color-best
  fits; across all 12 alpha-optimal origins the overlap is 95–97/97. The
  source contains disjoint hand/torso black runs at local rows 18–24, so this
  is not evidence for an additional continuous foreign horizontal line.
  This nearest-fit analysis is not an exact Linear renderer model; its phase
  ambiguity and the 362 silhouette residuals remain explicit. The actual
  Point production Up-neutral native capture retains the black hand/torso
  outlines (447 near-neutral black pixels across relative rows 45–64), so
  **the reported Up feature is not removed by this app sampling fix**. It
  is consistent with authored source art. The app neither repaints nor
  silently replaces licensed art.

## RED and GREEN

| Gate | Raw result | Outcome |
| --- | --- | --- |
| Baseline diagnostic, explicit Linear/Point and isolated-visible-cell matrix, real Servo D/W tap transition | `character-diagnostic-01.log`; `tests/Cerneala.Tests.SceneVillage/TestResults/character-diagnostic-01.trx` | 2/2 diagnostic tests passed; reports Linear atlas/control 570 differing pixels and 142 dark top-row pixels across all poses. It is observational, not the permanent RED. |
| Permanent native production-frame regression before app edit | `character-production-red-01.log`; `tests/Cerneala.Tests.SceneVillage/TestResults/character-production-red-01.trx` | RED for intended invariant: 134 foreign top-row pixels, exact phase/origin and all 16 source-top/visible-art guards passed. |
| App character-sampling model contract before app edit | `character-model-red-01.log`; `tests/Cerneala.Tests.SceneVillage/TestResults/character-model-red-01.trx` | RED: expected Point, actual Linear on production player. |
| Same permanent native regression after app edit | `character-production-green-01.log`; `tests/Cerneala.Tests.SceneVillage/TestResults/character-production-green-01.trx` | GREEN 1/1: 0 foreign top-row pixels, authored first-row hair visible on neutral frames. |
| Same model contract after app edit | `character-model-green-01.log`; `tests/Cerneala.Tests.SceneVillage/TestResults/character-model-green-01.trx` | GREEN 1/1. |
| Full Village project with native opt-in | `character-village-full-01.log`; `tests/Cerneala.Tests.SceneVillage/TestResults/character-village-full-01.trx` | GREEN 21/21, 0 skipped; includes original real Servo D/W route, diagnostic matrix, production-frame regression, tree, layout, camera, collision and stress cases. |

The specific production edit is `VillageArt.CharacterSampling = Point` for
both `Player` and `StressActor`. Tiny Town was already Point. The global
Sprite2D default remains Linear, and no Core/backend/asset pixels changed.
`VillageModelTests` now records both app character factory modes and the
unchanged global default. The README and CC0 credits describe the behavior
and retained authored Up art.

Native screenshot outputs, all through Servo's Window-owned screenshot path:

- RED actual production poses: `artifacts/ci/scene-village/screenshots/character-production-frames-20260925-163507639/production-frames.png`
- GREEN actual production poses: `artifacts/ci/scene-village/screenshots/character-production-frames-20260925-163806460/production-frames.png`
- Final full-run actual production poses: `artifacts/ci/scene-village/screenshots/character-production-frames-20260925-163916848/production-frames.png`
- Final full-run real Servo post-tap IdleRight/IdleUp: `artifacts/ci/scene-village/screenshots/character-routed-20260925-163909406/right.png`, `up.png`
- Final full-run explicit sampler/source matrix: `artifacts/ci/scene-village/screenshots/character-matrix-20260925-163915297/matrix.png` and `isolated-frames.png`

The user Up crop is
`C:/Users/lauri/AppData/Local/Temp/codex-clipboard-990fb52a-42f2-4e53-a2f3-4a8422076ca5.png`,
SHA-256 `6DC67AF59129DBD6B2247CCA57210B74CF4B61F21A8F20B3EB53F5C31C53C089`.
The exact analysis inputs, frame-source coordinates, alpha-fit grid, color
threshold, hands ROI, source black runs, and native Point/Linear controls are
printed in `up-hands-02.raw.txt`; the script reads images in memory and saves
no screenshots or modified artwork. `up-hands-01.raw.txt` is the earlier run
before whole-image RGB tie-ranking was added; it is retained, not presented
as the final analysis.

Servo's public `PressKeyAsync` is a discrete press/release sequence. The
routed screenshots therefore show the resulting idle facing after real D/W
input; frame callbacks observed one WalkRight and one WalkUp state each.
They are **not** claimed to capture a key held down. The deterministic
all-frame fixture is render setup, not a substitute for user input.

The isolated-frame control changes texture size and UV origin and loses
hidden RGB in alpha-zero PNG texels. Its visible RGBA/alpha cells are checked,
but full-frame pixel equality—especially for horizontally flipped Point
sprites at 2.5× nearest-sample tie phases—is not a valid oracle. The
permanent regression instead uses the unmodified source's top-row alpha and
actual production sprite screenshots, with measured geometry/phase.

## Reproduction commands

The focused native RED and GREEN commands were the same except for the
unique TRX and log names. Example GREEN invocation from the repository root:

```powershell
$env:CERNEALA_SDL_NATIVE_TESTS='1'
$env:DefaultItemExcludesInProjectFolder='artifacts/**'
dotnet test 'tests/Cerneala.Tests.SceneVillage/Cerneala.Tests.SceneVillage.csproj' -c Release --filter 'FullyQualifiedName~ProductionCharacterFramesHaveNoForeignMarksOnTransparentTopRows' --logger 'trx;LogFileName=character-production-green-01.trx' '-p:DefaultItemExcludesInProjectFolder=artifacts/**' *>&1 | Tee-Object -FilePath 'docs/plans/evidence/2026-09-25-scene-village-character-lines/app/character-production-green-01.log'
exit $LASTEXITCODE
```

The final full-project command was:

```powershell
$env:CERNEALA_SDL_NATIVE_TESTS='1'
$env:DefaultItemExcludesInProjectFolder='artifacts/**'
dotnet test 'tests/Cerneala.Tests.SceneVillage/Cerneala.Tests.SceneVillage.csproj' -c Release --logger 'trx;LogFileName=character-village-full-01.trx' '-p:DefaultItemExcludesInProjectFolder=artifacts/**' *>&1 | Tee-Object -FilePath 'docs/plans/evidence/2026-09-25-scene-village-character-lines/app/character-village-full-01.log'
exit $LASTEXITCODE
```

The corrected Up-image analysis command was:

```powershell
& 'C:\Users\lauri\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' 'docs/plans/evidence/2026-09-25-scene-village-character-lines/app/inspect_up_hands_band.py' --source 'Playground/Cerneala.SceneVillage/Assets/ch003.png' --user-crop 'C:\Users\lauri\AppData\Local\Temp\codex-clipboard-990fb52a-42f2-4e53-a2f3-4a8422076ca5.png' --linear-capture 'artifacts/ci/scene-village/screenshots/character-matrix-20260925-163915297/matrix.png' --point-capture 'artifacts/ci/scene-village/screenshots/character-production-frames-20260925-163916848/production-frames.png' *>&1 | Tee-Object -FilePath 'docs/plans/evidence/2026-09-25-scene-village-character-lines/app/up-hands-02.raw.txt'
exit $LASTEXITCODE
```

## Integrity and remaining gates

- Unmodified source PNG SHA-256:
  `02AC85F7A6DD90A486ED4126CC1CE80D482F6D9D17C02AD42DBAAC1C80B479DB`.
- Changed app/test files since integration preflight are exactly
  `VillageArt.cs`, app `README.md`, `Assets/CREDITS.md`,
  `VillageModelTests.cs`, and the new `NativeCharacterAtlasTests.cs`.
  All other preflight-hashed app/test inputs still match.
- The up-facing hands/torso dark region remains visible in source and current
  Point native output. Removing/redesigning it would be a separate art
  decision, not a sampling bug fix.
- No physical Alt-Tab, human play session, repository-wide suite, or
  independent final audit is claimed here.
