# Stage 2 package checkpoint: package-project tests GREEN, native pending

All commands below ran from `C:\Users\lauri\Desktop\Cerneala` on 2026-09-24,
Release/net8.0, with `-p:DefaultItemExcludesInProjectFolder=artifacts/**`.
There were no overlapping builds/tests. This is a package-project result, **not**
the Stage 2 integrated gate or Stage 3 native/platform gate.

| Step | Exact command after `$e = 'C:\Users\lauri\Desktop\Cerneala\docs\plans\evidence\2026-09-23-scene2d-stage2\package'` | Outcome | Raw evidence |
| --- | --- | --- | --- |
| New Stage 0 API/behavior and public-boundary contracts | `dotnet test '.\tests\Cerneala.Tests.Scene2DPackages\Cerneala.Tests.Scene2DPackages.csproj' -c Release '-p:DefaultItemExcludesInProjectFolder=artifacts/**' --filter 'FullyQualifiedName~Stage0_' --logger 'trx;LogFileName=stage2-package-stage0.trx' --results-directory $e` | 22 passed, 0 failed, 0 skipped; built current test input | `stage0-focused.log`, `stage2-package-stage0.trx` |
| First full project | `dotnet test '.\tests\Cerneala.Tests.Scene2DPackages\Cerneala.Tests.Scene2DPackages.csproj' -c Release '-p:DefaultItemExcludesInProjectFolder=artifacts/**' --logger 'trx;LogFileName=stage2-package-full-first.trx' --results-directory $e` | 90 passed, 13 failed, 0 skipped. Twelve importer-corpus cases hit a migrated-test creator-thread dispose error after `await`; one free-placement test wrongly hardcoded `map` instead of authored default `Tiles`. Neither failure was a production behavior assertion. | `full-first.log`, `stage2-package-full-first.trx` |
| Repaired fixture subset | `dotnet test '.\tests\Cerneala.Tests.Scene2DPackages\Cerneala.Tests.Scene2DPackages.csproj' -c Release '-p:DefaultItemExcludesInProjectFolder=artifacts/**' --filter 'FullyQualifiedName~ImportedPackageTests|FullyQualifiedName~PreparedChunkHeadersDeclareTheirAcquisitionOwnedDataCharge' --logger 'trx;LogFileName=stage2-package-fixture-rerun.trx' --results-directory $e` | 14 passed, 0 failed, 0 skipped; rebuilt after test edits | `fixture-rerun.log`, `stage2-package-fixture-rerun.trx` |
| Current full project | `dotnet test '.\tests\Cerneala.Tests.Scene2DPackages\Cerneala.Tests.Scene2DPackages.csproj' -c Release --no-build --no-restore '-p:DefaultItemExcludesInProjectFolder=artifacts/**' --logger 'trx;LogFileName=stage2-package-full-green.trx' --results-directory $e` | **103 passed, 0 failed, 0 skipped** | `full-green.log`, `stage2-package-full-green.trx` |

The importer fixture repair replaced asynchronous disposal of an unattached
`TileMap2D` from a non-creator thread with private CPV2 index/block comparisons.
That retains exact corpus payload, header, metadata, asset, and ordered map-ID
assertions while avoiding an unrelated map-owner-thread violation. Public
`CreateTileMap` remains covered by Stage 0 contracts and the allocated native
warm test. The free-placement ID fix now derives the expectation from the actual
authored model rather than assuming a grid model's ID.

Current selected input SHA-256 values at the GREEN run:

```text
EBFF26F710B14C098C895D5AB4EA432D971B93DA1B1A8DC318E12B438BBA7F02 Cerneala.Scene2D.Packages/IScene2DPackageRangeReader.cs
D470C2509B393010495BA4A4996EFFAD143EB5C44498BB1A10E9BEB9542D4E9E Cerneala.Scene2D.Packages/LocalPackageRangeReader.cs
1043BAB838990E45FEA681FD2062B4457413349ADDA425928B26E2CA7A8F49C6 Cerneala.Scene2D.Packages/Scene2DPackage.cs
135C214FC960714CDD1E15E2B1ECF7ACF7FC0507E91E4BB7D6AE5EB76BA4002C Cerneala.Scene2D.Packages/Scene2DPackageLevel.cs
5DDABEAF94FFD77C639A4289B0A11C5D7C96E7C541CE5F82B0E5ABB7E37DB840 Cerneala.Scene2D.Packages/Scene2DPackageLevel.Model.cs
246EC3641642B53C4962AD8079B9B05D4B39CEA638FF65DCB934B3460F7F77F5 tests/Cerneala.Tests.Scene2DPackages/ImportedPackageTests.cs
CD53F8BA8C2D697015F6362B01E7B622BBE438DE9557B1F2A99BF88A3F79EC48 tests/Cerneala.Tests.Scene2DPackages/PackageValueCodecTests.cs
5AC0F22FD257C12D3FC2F99C6A186F8CF27348CE679B9E39F2A5EE79C566F9C1 tests/Cerneala.Tests.Scene2DPackages/Scene2DPackageEntityTests.cs
BE5CB9556854C89CC26C731497D3CE37B2E02F0DDB4CFCF24B368AAB4249A3A1 tests/Cerneala.Tests.Scene2DPackages/Scene2DPackageGridGeometryTests.cs
8493C99F4B6C4B2F55342504316AD729AF1DB0C851C1AF2B732A9C70B60B89D4 tests/Cerneala.Tests.Scene2DPackages/Scene2DPackageGridTests.cs
2D63D9048535A10A26B9556295BB9D364430FDB05A03F109580CD905CB07CB5C tests/Cerneala.Tests.Scene2DPackages/Scene2DPackageTests.cs
D87A7B8CB6B7AF83E129FA976393A305AE07F54922129AC474A154CE87538601 tests/Cerneala.Tests.Scene2DPackages/Scene2DPublicBoundaryContractTests.cs
D078F37C1DEBB9F91C02364F86BFC85BFEE30E28AF57C15C6A7D4F0969BADE9F tests/Cerneala.Tests.Scene2DPackages/Stage0PackageContractTests.cs
4F6B95E9859227A413338CB6F851D7E7191F881C43C9D13C636BB2E8DC74DF82 UI/Controls/SceneItems2D.cs
```

The Stage 1 pre-cutover deterministic package remains byte-identical in the
new suite: catalog SHA-256
`A417E679D0ACE8599A59FCF31738C725B137A3ECD5491ADA1FB38F15A4B8815E`,
payload SHA-256
`F6A5EA008D9AEF3C88C4212F3FF93B32D1AFE2BBAB2F8FE5BE5D5665878A326E`.
`NativePackageWarmStreamingTests.cs` is edited but has **not** been compiled or
executed; its current SHA-256 is
`7AD296BDDA03CD45B547E326C712F7E26027F9E2B983D031513576066955E558`.
The integrated SDL project, ApiCompat, documentation, broader suites, and native
platform probes remain outside this package-project result.

## Conditional public-payload retention regression

Strict Core ApiCompat later identified that `TileMapChunkData2D` had been made
internal mechanically. Root selected the narrower cutover: keep this decoded
nonspatial value type public; do not re-export source/catalog/entry/lease or
residency types. The new `Stage2_NonspatialTileMapChunkDataRemainsPublic` test in
`Scene2DPublicBoundaryContractTests.cs` asserts that visibility independently.
Before the Core owner restores the declaration, the exact RED command was:

```powershell
$e='C:\Users\lauri\Desktop\Cerneala\docs\plans\evidence\2026-09-23-scene2d-stage2\package'
dotnet test '.\tests\Cerneala.Tests.Scene2DPackages\Cerneala.Tests.Scene2DPackages.csproj' -c Release '-p:DefaultItemExcludesInProjectFolder=artifacts/**' --filter 'FullyQualifiedName~Stage2_NonspatialTileMapChunkDataRemainsPublic' --logger 'trx;LogFileName=chunkdata-public-red.trx' --results-directory $e
```

The project built; the one selected test failed only `Assert.True()` at
`typeof(TileMapChunkData2D).IsPublic` (actual `false`). `chunkdata-public-red.log`
and `chunkdata-public-red.trx` are raw evidence. This is an intended RED, not a
new runtime/fixture failure.

The Core owner changed only `internal sealed class TileMapChunkData2D` to
`public sealed class TileMapChunkData2D`; its constructors/members were unchanged.
After Core quiesced, the exact GREEN command was:

```powershell
$e='C:\Users\lauri\Desktop\Cerneala\docs\plans\evidence\2026-09-23-scene2d-stage2\package'
dotnet test '.\tests\Cerneala.Tests.Scene2DPackages\Cerneala.Tests.Scene2DPackages.csproj' -c Release '-p:DefaultItemExcludesInProjectFolder=artifacts/**' --filter 'FullyQualifiedName~Scene2DPublicBoundaryContractTests' --logger 'trx;LogFileName=chunkdata-public-green.trx' --results-directory $e
```

It rebuilt current Core/Packages/test inputs and passed all **5 public-boundary
tests**, 0 failed, 0 skipped. `chunkdata-public-green.log` and `.trx` are raw
evidence. Since this was only a type-accessibility repair with no implementation
behavior change, the prior 103/103 package behavior run was not repeated here;
the later integrated full-suite gate must run on the final source state.

```text
EDC5EEFB75D79158379D54DFBB0BE1355FAE9E103733B710661762A96501B72C UI/Controls/TileMapSource2D.cs (GREEN)
0D90AA4EA880CD9893D38E18727EA431A5E44F90DCE93671B84210D2F2B1AC5B tests/Cerneala.Tests.Scene2DPackages/Scene2DPublicBoundaryContractTests.cs
D9FD586BE20BEE51BEB0D0A4DE8798B22A4A31A020C3AA38A402ADD603F7F533 chunkdata-public-red.log
0D7F63D611D428C61135D49663CEE97220FB1E1ED8DAED5E3F5A8CE99CE145A8 chunkdata-public-red.trx
579C5F1779A532B195A04DEA335E5312485D1783957C4ED5388A2718F7F96F2B chunkdata-public-green.log
37C58499F782CAFCEBD1F8D3C5FF0DB6112E3089866B38C4E37223ADAF2DA130 chunkdata-public-green.trx
```

## Native warm fixture teardown repair (unverified)

Independent Stage 2 review found that the warm fixture's new terminal map
disposal still ran while the map remained in `scene.Children`, violating the
selected attached-map rejection contract. The bounded fixture repair now closes
the window, removes that map from its scene on the owner thread, then attempts
both map terminal drains and the package terminal drain in order. Every cleanup
step is attempted even if an earlier step fails; cleanup failures are aggregated
with any primary test failure rather than replacing it. The outer temporary
directory cleanup likewise preserves a prior run failure if deletion fails.
This follows the detached-before-drain pattern of
`NativePackageGridSubdivisionTests.cs` without changing production behavior.

Current `NativePackageWarmStreamingTests.cs` SHA-256:
`D9A96DFE1F38975C151D0B933A7D822503B5A8C47A00606568A9797FD8A730C7`.
`git diff --check` for this file returned exit 0. **No build, SDL native run,
or screenshot conformance was executed for this repair**; Core worker held the
serialized build/test slot. Proposed verification after slot release: build
`tests/Cerneala.Tests.SdlGpu/Cerneala.Tests.SdlGpu.csproj` Release with the
repository artifact exclusion, then run the four
`NativePackageWarmStreamingTests` with `CERNEALA_SDL_NATIVE_TESTS=1` on the
configured Windows native runner. A skipped non-opt-in run is not this gate.
