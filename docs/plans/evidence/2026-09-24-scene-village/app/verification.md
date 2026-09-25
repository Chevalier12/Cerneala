# Scene Village app verification — 2026-09-24

Scope: new `Playground/Cerneala.SceneVillage` Windows SDL_GPU app and its
dedicated `tests/Cerneala.Tests.SceneVillage` project. No Cerneala Core
production files or APIs were changed by this worker. Root solution/CI
integration and the full solution verification belong to the integration
owner and are **not** claimed here.

## Environment and command record

The local root contains pre-existing `artifacts/rendersurface3d/.../obj/*.cs`.
The first app build (`app-build-01.*`) failed before reaching village source:
`Cerneala.csproj` globbed those generated files, yielding 52 duplicate
assembly/Xunit errors. Those artifacts were preserved. Subsequent commands
used the repository's already-established
`DefaultItemExcludesInProjectFolder='artifacts/**'` both in the PowerShell
process environment and as the MSBuild property.

`app-build-02.*` reached the new source and found seven invalid two-component
button `Padding` values in markup. They were changed to four-component
thickness values. `app-build-03.*` then passed: **0 warnings, 0 errors**.

The dedicated model suite first passed 8/8 (`model-01.*`) and, after the
camera/collision contract was added, 9/9 (`model-02.*`).
`pre-native-01.*` found a test-only namespace mistake in the SDL backend
registration call; `pre-native-02.*` compiled and passed 9/9 model cases,
with the one native case correctly skipped because its opt-in was absent.

The first `CERNEALA_SDL_NATIVE_TESTS=1` run (`native-01.*`) failed on the
initial screenshot: a direct `Window.SaveScreenshot` call from an async
post-frame continuation encountered an uncommitted retained root. Source
inspection showed that the window-backed `Servo.SaveScreenshotAsync` queues
the same app-owned `Window.SaveScreenshot` capture at the supported
post-draw phase. The test was changed to that existing API; no Core or app
production code was altered for this failure. `native-02.*` passed 10/10.

Coverage was then extended to collision with a 10,000-item preset, exact HUD
counts, original artwork hashes, preset semantics, and a bounded game-area
pixel difference showing a different animation frame was presented (the
directional frame mapping was corrected in the later audit repair).
`native-03.*` and `native-04.*` passed 13/13. The pre-audit source-state run was:

```powershell
$env:DefaultItemExcludesInProjectFolder = 'artifacts/**'
$env:CERNEALA_SDL_NATIVE_TESTS = '1'
dotnet test .\tests\Cerneala.Tests.SceneVillage\Cerneala.Tests.SceneVillage.csproj -c Release -m:1 '-p:DefaultItemExcludesInProjectFolder=artifacts/**' --blame-hang-timeout 7m --logger 'trx;LogFileName=native-05.trx'
```

`native-05.exit.txt` is **0**; `native-05.log` and the copied
`native-05.trx` record **13 passed, 0 failed, 0 skipped**. The native case
took 37.50 seconds and reported:

- routed Servo A taps contacted the west-house collider at player x = 1984;
- the animated 10,000-item preset changed 12,685 sampled game-area pixels
  versus the static idle preset (the crop excludes the HUD);
- routed Servo D taps contacted a stress-mode collider at x = 2640;
- all three 10,000-item presets had 10,000 realized `SceneItems2D` children
  at the same camera `ViewBox`;
- the initial UI showed requested 0 / realized 0; Servo clicks on 100,
  1,000 and 10,000 showed the matching requested and realized numbers.

The native test uses the real generated app / SDL_GPU window lifecycle,
Servo-routed clicks and key taps, and application-owned screenshots via
`Servo.SaveScreenshotAsync` → `Window.SaveScreenshot`. It does **not** use
direct test property writes to pretend user input. Servo exposes discrete
key taps, not arbitrary held-key duration; deterministic model tests cover
delta-time speed and diagonal normalization. The held-key clearing method
used by the window's `Deactivated` handler is tested directly, but a physical
Alt-Tab/OS focus-loss event was not automated.

The pre-audit screenshot directory is
`C:\Users\lauri\Desktop\Cerneala\artifacts\ci\scene-village\screenshots\native-20260924-152311403`.
It contains `village.png`, `static-10000.png`, `animated-10000.png`, and
`collision-10000.png`; each was produced by the application screenshot API.
The images were visually inspected. Their SHA-256 hashes, in that order,
are `0FE44A7B6970506197AE0244EB1832B0A7FBAF2D682E3616D496349B2E2E498B`,
`134222D5CD5E199962642F31E12DAEB1724438C6B6AF304F90B56654754179FE`,
`36392DDBAEC3B7EDBD2980D962A38DDEF7C2BC1C44AE28B9C211B839F62366C0`,
and `3622009A71B60C6A4115797E6E869A912ED1535A802C71E0A46864D64AAEA45F`.

`git diff --check` exited 0. It reported line-ending conversion warnings
from existing tracked dirty files, not whitespace errors. This command does
not inspect the new untracked files; the independent auditor still needs to
review their actual contents and integrated solution/CI changes.

## Shared-audit repair and current app gate

The first shared audit found four concrete defects: a loose Tiny Town branch
cell used as a standalone tree, mislabeled directional artwork, a clipped
Village button at the declared 820-pixel minimum width, and a claim about
clicking `0` that the then-current test had not performed. The tree now uses
a complete green-tree atlas cell and keeps its collider on that sprite. The
toolbar is two rows without increasing the declared minimum. The permanent
native bounds assertion first failed in `audit-red-02.*` with
`Village button ends at 837.6 beyond viewport width 820`, then passed after
the layout change. The native test now *actually clicks* `0` after 10,000
and asserts both zero realized children and a cleared app-owned capture.

An initial attempt to map the old `villager.png` artwork was wrong despite
`audit-final-02.*` passing 15/15. The complete old
[8-by-4 contact sheet](scene-village-full-villager-contact-20260924.png)
shows that the presumed lateral walk cells include front-facing action
poses; that prior green run is historical evidence, **not** acceptance for
character art. The old PNG was an own-task asset, and only that exact file
was removed after the replacement was selected. The older
`audit-final-02-source.sha256.txt` pins a superseded state.

The current character asset is Belohlavek's unmodified transparent
`ch003.png`, SHA-256
`02AC85F7A6DD90A486ED4126CC1CE80D482F6D9D17C02AD42DBAAC1C80B479DB`.
The primary [OpenGameArt upload](https://opengameart.org/content/character-sprite-walk-animation)
lists it under CC0; exact source and license context are in the current
`Assets/CREDITS.md`. The
[labeled contact sheet](scene-village-belohlavek-contact-20260924.png)
is derived inspection evidence, not a runtime asset. Image inspection maps
its rows to down, up, authored left, and right. The app uses the verified
down/up/right rows and, per the user-requested mirror coverage, **reuses the
right-row source rectangles with per-frame horizontal flip for left**. The
authored left row exists but is deliberately unused. `Sprite2D.Flip` stays
at its default `None`; public sprite rendering composes it with the sampled
frame flip by XOR, so Left → Right → Down cannot retain the left mirror.
The PNG is not rewritten.

The asset-switch focused RED (`asset-switch-red-01.*`) failed against the
old 16-pixel mapping, and focused GREEN (`asset-switch-green-01.*`) passed
against the new 32-pixel sheet. The pre-mirror `asset-final-01.*` 15/15
native pass is likewise historical, not the current gate. For the final
mirror requirement, `mirror-red-01.*` failed **for the intended contract**:
Left idle was reading source Y=64, whereas the approved mirrored Right row
requires Y=96. After the per-frame flip change, `mirror-focus-01.*` passed
1/1. The current permanent test checks source rectangles and frame flips
for all four player directions, that Left and Right read the same source
cells with opposite flip flags, and the player's base flip is `None`.

The **current-source opt-in native command** is recorded exactly in
[`mirror-native-01.command.txt`](mirror-native-01.command.txt). It sets
`DefaultItemExcludesInProjectFolder='artifacts/**'` and
`CERNEALA_SDL_NATIVE_TESTS='1'` and runs the dedicated Release test
project with the artifact-exclusion MSBuild property. The raw
[`mirror-native-01.log`](mirror-native-01.log), exit-code file and copied
[`mirror-native-01.trx`](mirror-native-01.trx) record **15 passed,
0 failed, 0 skipped**; the native case took about 42 seconds. The generated
real SDL_GPU window began at 820×600. Servo-routed WASD taps captured all
four facings, including actual Left → Right → Down transitions; the native
test asserted selected clip source rows and frame flips after those inputs.
Its app-owned screenshots visibly show left/right/down facing changes. The
same run contacted the west house at x=1984, contacted a stress collider at
x=2640, realized 10,000 SceneItems2D children in each of the three modes at
the same ViewBox, then clicked `0` and retired all 10,000. The animated
preset differed from static idle in 2,483 sampled game-area pixels. That
number is an animation-presentation check, **not** an FPS, allocation, draw
call, or performance measurement.

The 11 current captures are under
`C:\Users\lauri\Desktop\Cerneala\artifacts\ci\scene-village\screenshots\native-20260924-171744809`.
They were created through `Servo.SaveScreenshotAsync` →
`Window.SaveScreenshot`, not OS capture. I viewed the complete village,
up/left/right/down and Left → Right → Down frames, the three 10,000-object
modes, and the zero-cleared frame. Exact capture hashes are in
[`mirror-native-01-screenshots.sha256.txt`](mirror-native-01-screenshots.sha256.txt).
The current 21-file app/test/asset hash ledger is
[`mirror-native-01-source.sha256.txt`](mirror-native-01-source.sha256.txt).
The source and capture files are available for the shared Astra re-audit.

The native harness proves routed app input and rendered output for this
deterministic scenario. It does not simulate continuous key-hold duration
through Servo, which exposes only discrete taps. Separate model tests cover
delta-time movement, normalized diagonals, camera center/clamp/resize, and
the key-clear method called on window deactivation. Physical OS Alt-Tab
focus switching still requires human validation.

## Limits

- Windows SDL_GPU only; Linux, macOS and WindowsDX were not tested for this
  game under the approved platform scope.
- No FPS, allocation, GPU-time or performance threshold was specified or
  measured. The 10,000 count is a realized-object workload, not a claim that
  all 10,000 draw in one viewport or meet a frame-time budget.
- Physical Alt-Tab focus loss and human play validation remain unperformed.
- Full repository verification and independent integrated audit remain
  pending after this app handoff.

