# Scene Village decoration-art diagnosis and app fix (2026-09-25)

## Observed failure and source contract

The first user crop shows the lower green-tree art without its canopy. The
second crop shows the round green foliage as shipped. The crops match Tiny
Town atlas cells 16 and 5 respectively (the independent pixel comparison
reported 98.33% and 98.10% distinct-pixel matches, with one-pixel screenshot
edge residuals). The included `tiny-town.png` is the unchanged Kenney 1.1
`Tilemap/tilemap_packed.png`, SHA-256
`3A54D99ECDE790D4FDEA207A3644CF130FC56FA838F1BEB1507C185A95B8E902`.
The official archive contains 16 x 16 individual cell exports but no labeled
multi-cell object metadata. In the packed atlas, cell 4 directly above cell
16 forms a contiguous green tree: source `(64,0,16,32)`. The former app used
only `(64,16,16,16)` at four sites, so it omitted the upper canopy.

Cell 5 reaches the bottom row with the same flat lower edge visible in the
user crop. Cell 17 below starts with two fully transparent rows and is not an
evidenced continuation. The second crop is faithful to cell 5. Whether the user wants a
different shrub design is a product/art choice, not a demonstrated crop bug.
The app intentionally leaves cell 5 and the licensed PNG unchanged.

## RED evidence

The permanent model regression first failed 1/1 at the missing whole-tree
sprite (`Assert.Single` empty): [raw log](model-red-01.log),
`tests/Cerneala.Tests.SceneVillage/TestResults/decoration-model-red-01.trx`.

The first native attempt failed **for a fixture reason**, not the reported
defect: direct `Window.SaveScreenshot` inside `ContentRendered` preceded a
committed root command list. It is retained as [invalid RED evidence](native-red-01.log).
The fixture was corrected to use `Servo.SaveScreenshotAsync`, which queues the
application-owned window screenshot after commit. The second native run is
the valid visual RED: the unchanged cell-5 shrub control was `#479F4A`, but
the world pixel where cell 4's canopy leaf belongs was grass `#84C669`
instead of source leaf `#479F4A`. The capture was made after real Servo D/S
key taps (2/14) in the real minimum-size SDL window; no player or camera
property was assigned to simulate movement. See [raw log](native-red-02.log),
`tests/Cerneala.Tests.SceneVillage/TestResults/decoration-native-red-02.trx`,
and the app-owned screenshot:
`artifacts/ci/scene-village/screenshots/native-20260925-102414091/decorations.png`.

## Fix and GREEN evidence

`VillageArt.GreenTree` selects source `(64,0,16,32)` as one Point-sampled
sprite, destination 32 x 64. A site's existing `(x,y)` remains the lower
cell's top-left, so the sprite starts `(x,y-32)`. The collider local Y offset
changes from 20 to 52 only for this tree, preserving its prior world trunk
box `[x+9,x+23) x [y+20,y+30)`. Other decorations, house/ground art, camera,
and the Tiny Town file are unchanged. The layout comment, app README, and
art credits no longer misclassify cell 16 as a complete tree.

- Focused model GREEN: 1/1, [raw log](model-green-01.log),
  `tests/Cerneala.Tests.SceneVillage/TestResults/decoration-model-green-01.trx`.
- Original native canopy pixel GREEN: 1/1, [raw log](native-green-01.log),
  `tests/Cerneala.Tests.SceneVillage/TestResults/decoration-native-green-01.trx`.
- Strengthened native shape GREEN: 1/1, source-derived tree top outline,
  canopy leaf and trunk plus unchanged shrub control, [raw log](native-shape-green-01.log),
  `tests/Cerneala.Tests.SceneVillage/TestResults/decoration-native-shape-green-01.trx`.
- Full dedicated Village project with native SDL enabled on the final test
  source: **18 passed, 0 failed, 0 skipped**, [raw log](village-full-02.log),
  `tests/Cerneala.Tests.SceneVillage/TestResults/decoration-village-full-02.trx`.
  The final native capture is
  `artifacts/ci/scene-village/screenshots/native-20260925-103407814/decorations.png`.
  At real routed player center `(2100.9119,2214.9119)`, its source-derived
  top/leaf/trunk pixels were `#3F2631/#479F4A/#EAA56C`; the unchanged shrub
  control was `#479F4A`.

An earlier full Village run, [raw log](village-full-01.log), also passed
18/18. It predates the test-only widening of the acceptable routed player
position interval and is retained as historical evidence, not the final
source checkpoint.

The commands below record the executed PowerShell arguments from the
repository root. Each block section was a separate shell invocation, with
the environment assignments repeated each time.

```powershell
New-Item -ItemType Directory -Force -Path 'docs/plans/evidence/2026-09-25-scene-village-decoration-art/app' | Out-Null
$env:CERNEALA_SDL_NATIVE_TESTS='0'
$env:DefaultItemExcludesInProjectFolder='artifacts/**'
dotnet test 'tests/Cerneala.Tests.SceneVillage/Cerneala.Tests.SceneVillage.csproj' -c Release --filter 'FullyQualifiedName~VillageTreesUseTheWholeTwoCellArtworkWithoutMovingTheirTrunkColliders' --logger 'trx;LogFileName=decoration-model-red-01.trx' '-p:DefaultItemExcludesInProjectFolder=artifacts/**' *>&1 | Tee-Object -FilePath 'docs/plans/evidence/2026-09-25-scene-village-decoration-art/app/model-red-01.log'; exit $LASTEXITCODE

$env:CERNEALA_SDL_NATIVE_TESTS='0'
$env:DefaultItemExcludesInProjectFolder='artifacts/**'
dotnet test 'tests/Cerneala.Tests.SceneVillage/Cerneala.Tests.SceneVillage.csproj' -c Release --filter 'FullyQualifiedName~VillageTreesUseTheWholeTwoCellArtworkWithoutMovingTheirTrunkColliders' --logger 'trx;LogFileName=decoration-model-green-01.trx' '-p:DefaultItemExcludesInProjectFolder=artifacts/**' *>&1 | Tee-Object -FilePath 'docs/plans/evidence/2026-09-25-scene-village-decoration-art/app/model-green-01.log'; exit $LASTEXITCODE

$env:CERNEALA_SDL_NATIVE_TESTS='1'
$env:DefaultItemExcludesInProjectFolder='artifacts/**'
dotnet test 'tests/Cerneala.Tests.SceneVillage/Cerneala.Tests.SceneVillage.csproj' -c Release --filter 'FullyQualifiedName~RealVillageShowsTheTreeCanopyAboveItsOriginalTrunkPosition' --logger 'trx;LogFileName=decoration-native-red-02.trx' '-p:DefaultItemExcludesInProjectFolder=artifacts/**' *>&1 | Tee-Object -FilePath 'docs/plans/evidence/2026-09-25-scene-village-decoration-art/app/native-red-02.log'; exit $LASTEXITCODE

$env:CERNEALA_SDL_NATIVE_TESTS='1'
$env:DefaultItemExcludesInProjectFolder='artifacts/**'
dotnet test 'tests/Cerneala.Tests.SceneVillage/Cerneala.Tests.SceneVillage.csproj' -c Release --filter 'FullyQualifiedName~RealVillageShowsTheTreeCanopyAboveItsOriginalTrunkPosition' --logger 'trx;LogFileName=decoration-native-green-01.trx' '-p:DefaultItemExcludesInProjectFolder=artifacts/**' *>&1 | Tee-Object -FilePath 'docs/plans/evidence/2026-09-25-scene-village-decoration-art/app/native-green-01.log'; exit $LASTEXITCODE

$env:CERNEALA_SDL_NATIVE_TESTS='1'
$env:DefaultItemExcludesInProjectFolder='artifacts/**'
dotnet test 'tests/Cerneala.Tests.SceneVillage/Cerneala.Tests.SceneVillage.csproj' -c Release --filter 'FullyQualifiedName~RealVillageShowsTheTreeCanopyAboveItsOriginalTrunkPosition' --logger 'trx;LogFileName=decoration-native-shape-green-01.trx' '-p:DefaultItemExcludesInProjectFolder=artifacts/**' *>&1 | Tee-Object -FilePath 'docs/plans/evidence/2026-09-25-scene-village-decoration-art/app/native-shape-green-01.log'; exit $LASTEXITCODE

$env:CERNEALA_SDL_NATIVE_TESTS='1'
$env:DefaultItemExcludesInProjectFolder='artifacts/**'
dotnet test 'tests/Cerneala.Tests.SceneVillage/Cerneala.Tests.SceneVillage.csproj' -c Release --logger 'trx;LogFileName=decoration-village-full-01.trx' '-p:DefaultItemExcludesInProjectFolder=artifacts/**' *>&1 | Tee-Object -FilePath 'docs/plans/evidence/2026-09-25-scene-village-decoration-art/app/village-full-01.log'; exit $LASTEXITCODE

$env:CERNEALA_SDL_NATIVE_TESTS='1'
$env:DefaultItemExcludesInProjectFolder='artifacts/**'
dotnet test 'tests/Cerneala.Tests.SceneVillage/Cerneala.Tests.SceneVillage.csproj' -c Release --logger 'trx;LogFileName=decoration-village-full-02.trx' '-p:DefaultItemExcludesInProjectFolder=artifacts/**' *>&1 | Tee-Object -FilePath 'docs/plans/evidence/2026-09-25-scene-village-decoration-art/app/village-full-02.log'; exit $LASTEXITCODE
```

Final SHA-256 checkpoint (relative to repository root; the integration
preflight ledger records the prior hashes):

| File | SHA-256 |
| --- | --- |
| `Playground/Cerneala.SceneVillage/VillageArt.cs` | `C8719E8E7C38702216F0F2DAA3B3DCE4FF4891A7FF6BD889F43F71B07FF6E3C4` |
| `Playground/Cerneala.SceneVillage/VillageGameSurface.cs` | `E251626778895D84C770209DA19BC8B005A4154E29CE997D6F6651B78F5EF45F` |
| `Playground/Cerneala.SceneVillage/VillageLayout.cs` | `C6E0478ECBDFA06ED88DB9E4ACD1FD63C7EAC75B01DC42A65C931E1A7B756720` |
| `Playground/Cerneala.SceneVillage/Assets/CREDITS.md` | `51144264044595F816E6DF18279C71CF45D270BFCD9BCFFF96431A0CA91A1713` |
| `Playground/Cerneala.SceneVillage/README.md` | `E8512234811641613D03393891C6CEF720FB4D00BC83C8279160EA87F8356507` |
| `tests/Cerneala.Tests.SceneVillage/VillageModelTests.cs` | `5E07DA6D628DAA036354C5FE40B608A1131142FCF4E9DEA9FB29664480B29FBA` |
| `tests/Cerneala.Tests.SceneVillage/NativeVillageWindowTests.cs` | `96887DC25EEAB5DE6D48860895836D883C08564E3ED470DC0ED1C4143CEACB15` |
| `Playground/Cerneala.SceneVillage/Assets/tiny-town.png` (unchanged) | `3A54D99ECDE790D4FDEA207A3644CF130FC56FA838F1BEB1507C185A95B8E902` |
| `artifacts/ci/scene-village/screenshots/native-20260925-103407814/decorations.png` | `E3D618AB300C5714416277929B199107AB3D361C9E7B10473C909F97DF097949` |

The full repository suite and independent audit are not claimed here; those
are integration/root gates. Physical OS Alt-Tab and human visual acceptance
were not performed. The intended replacement, if any, for the source-faithful
cell-5 shrub remains a user decision.
