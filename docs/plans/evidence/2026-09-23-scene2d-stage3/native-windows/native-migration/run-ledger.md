# Stage 3 Windows SDL native migration run

Host: Windows, `dotnet 10.0.400`; SDL test target `net8.0` / Release. Native
opt-in was set to the exact required value `CERNEALA_SDL_NATIVE_TESTS=1` for
both completed test runs. These are automated native tests, not human manual
validation. Tests were serialized; no other build/test job was running in the
shared workspace during this lease.

## Inputs at the passing runs

| Input | SHA-256 |
| --- | --- |
| `tests/Cerneala.Tests.SdlGpu/Cerneala.Tests.SdlGpu.csproj` | `F9B111DDC95F9508F14C3774EAD3B7B710629607A53BBD1C8C32CE4D65904DF2` |
| `tests/Cerneala.Tests.SdlGpu/NativeTetrisWindowTests.cs` | `9C8EBA1AF089F9485172B6CFA340D6242C79028098282A9EB86E7F75CB37DF77` |
| `tests/Cerneala.Tests.SdlGpu/NativePackageLifecycleTests.cs` | `8E8D3394D05951BB1A442B81DD6E9BC9E27B1E0A2585D8DC2FA5CC161BDD88D8` |

The first build/test command (same first filter below, without `--no-restore`)
failed before test execution with `CS0118` at
`NativePackageLifecycleTests.cs:86`: `Scene2D` resolved as a namespace. The test
fixture was repaired with an explicit `SceneGraph2D` alias. The raw failed
build log is [scene2d-stage3-new-native-windows.log](scene2d-stage3-new-native-windows.log).
This was a fixture compile failure, not a reproduced runtime defect or RED
behavior test.

## Passing new native fixtures

From repository root, after the alias repair:

```powershell
$env:CERNEALA_SDL_NATIVE_TESTS = '1'
$out = 'docs/plans/evidence/2026-09-23-scene2d-stage3/native-windows/native-migration'
dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj -c Release -m:1 --no-restore --nologo -v:minimal '-p:DefaultItemExcludesInProjectFolder=artifacts/**' --filter 'FullyQualifiedName~NativeTetrisWindowTests|FullyQualifiedName~NativePackageLifecycleTests' --logger 'trx;LogFileName=scene2d-stage3-new-native-windows-rerun1.trx' --results-directory (Join-Path $out 'test-results') --blame-hang-timeout 2m
```

Result: exit 0; **2 passed, 0 failed, 0 skipped** in 9 seconds. Raw
[log](scene2d-stage3-new-native-windows-rerun1.log) and
[TRX](test-results/scene2d-stage3-new-native-windows-rerun1.trx) are archived.
The Tetris test hosts the actual generated `MainWindow` on SDL_GPU, uses
Servo-routed click/key input, observes the app's `ObservableCollection` and
template nodes, and compares pre/post hard-drop pixels from captures made only
through `Window.SaveScreenshot`. The package lifecycle test uses a real CPV2
package map: camera exit cancels a deliberately parked first acquisition,
its late lease is released, and a second camera visit reacquires the same chunk
and renders it. Its black/white captures also use `Window.SaveScreenshot`.
Both tests remove their temporary PNGs after assertions; the TRX is retained.

## Passing plan-named native subset

After that successful build, without source/project changes:

```powershell
$env:CERNEALA_SDL_NATIVE_TESTS = '1'
$out = 'docs/plans/evidence/2026-09-23-scene2d-stage3/native-windows/native-migration'
dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj -c Release -m:1 --no-build --no-restore --nologo -v:minimal '-p:DefaultItemExcludesInProjectFolder=artifacts/**' --filter 'FullyQualifiedName~NativePackageWarmStreamingTests|FullyQualifiedName~NativePackageGridSubdivisionTests|FullyQualifiedName~NativeScenePrismStreamingTests' --logger 'trx;LogFileName=scene2d-stage3-required-six-native-windows.trx' --results-directory (Join-Path $out 'test-results') --blame-hang-timeout 2m
```

Result: exit 0; **6 passed, 0 failed, 0 skipped** in 25 seconds. Raw
[log](scene2d-stage3-required-six-native-windows.log) and
[TRX](test-results/scene2d-stage3-required-six-native-windows.trx) are archived.
The six source-defined cases are four `NativePackageWarmStreamingTests`, one
`NativePackageGridSubdivisionTests`, and one `NativeScenePrismStreamingTests`.

`git diff --check` for the project file and trailing-whitespace/conflict-marker
scans of the two new files passed. No `testhost` process remained after the
commands. The broader SDL project, full solution, Linux/macOS SDL, and
WindowsDX were **not** run under this worker's lease; they remain separate
Stage 3 gates. The absence of a Linux/macOS/WindowsDX run is not a waiver.
