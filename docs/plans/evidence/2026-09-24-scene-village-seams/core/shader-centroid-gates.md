# SDL drawing centroid experiment: conformance gate is RED

This is **not an accepted renderer fix**. The shared drawing fragment varying was
changed from default interpolation to `linear centroid` in
`Cerneala.Backends.SdlGpu/Gpu/Shaders/Drawing.frag.hlsl`, and the repository
compiler regenerated only that fragment's SPIR-V, DXIL, MSL, and
`Shaders/artifacts.json`. The five current target hashes match
`diagnostic/target-centroid-hashes.json`; the original five bytes/hashes are in
`diagnostic/baseline-bytes/` and `diagnostic/target-baseline-hashes.json`.
The backend source has **no retained diagnostic probe**. Its SHA-256 after
byte-exact probe restoration is
`FC7FC7072499A156F4E7E10AD5FE0D8321E35676A766F1BEAA7F54601186FAC7`.

All commands below ran on Windows from the repository root. Every build/test
set process `DefaultItemExcludesInProjectFolder=artifacts/**` and passed the
explicit quoted MSBuild property `'-p:DefaultItemExcludesInProjectFolder=artifacts/**'`
to avoid the known pre-existing SDK glob contamination. Native test commands
also set `CERNEALA_SDL_NATIVE_TESTS=1`. Logs and `.exit.txt` files are in this
directory; TRXs are in the corresponding test project's `TestResults/`.

## Positive evidence, with limits

- `dotnet run --project .\Tools\Cerneala.SdlShaderCompiler\Cerneala.SdlShaderCompiler.csproj -c Release '-p:DefaultItemExcludesInProjectFolder=artifacts/**' --` regenerated 8 artifacts; `-- --verify` then verified all 8. Logs: `shader-permanent-generate.log`, `shader-permanent-verify.log`.
- `dotnet build .\Cerneala.Backends.SdlGpu\Cerneala.Backends.SdlGpu.csproj --configuration Release --no-restore --target:Rebuild '-p:DefaultItemExcludesInProjectFolder=artifacts/**'` passed and verified the artifacts. The clean rebuild after removing the probe also passed (`shader-permanent-clean-backend-rebuild.log`).
- `dotnet test .\tests\Cerneala.Tests.SceneVillage\Cerneala.Tests.SceneVillage.csproj --configuration Release --filter 'FullyQualifiedName~OpaquePointSampledAtlasTileJoinsOpaqueGroundAtFractionalCameraPosition' '-p:DefaultItemExcludesInProjectFolder=artifacts/**' --logger 'trx;LogFileName=portable-fixture-centroid-final-11.trx' --logger 'console;verbosity=detailed'` passed **1/1** after the clean rebuild. Raw `portable-fixture-centroid-final-11.log` and TRX show all six measured x/y phases 0.125, 0.275, 0.375, 0.500, 0.625, 0.875: Point bottom/left, flipped-right, rotated-left, uniform controls all match opaque grass `#84C669`; half-opacity joins match the safe interior `#C163B4`. Linear interior remains observably different, with 118–121 changed pixels across phases. The same unchanged portable fixture was RED under a forced original-shader rebuild in the app worker's `../app/portable-fixture-red-08.*` evidence.
- A temporary *opt-in* `Console.Error` probe at offscreen surface allocation (then removed byte-exact) logged seven allocations in `diagnostic/observational-count/portable-fixture-centroid-msaa-probe-10.log`: requested `Eight`, actual offscreen selected `Eight`, parent/window target `One`, size `990x550`, format `B8G8R8A8Unorm`. The native fixture passed during that probe. These counts apply to this Windows device and fixture, not every platform or fallback. The probe source bytes, baseline/probe hashes, and TRX are in `diagnostic/observational-count/`.
- `dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj --configuration Release --no-restore -m:1 '-p:DefaultItemExcludesInProjectFolder=artifacts/**' --filter 'FullyQualifiedName=Cerneala.Tests.SdlGpu.SdlGpuShaderArtifactTests.Native_device_creates_drawing_and_prism_pipelines_from_offline_artifacts' --logger 'trx;LogFileName=shader-native-pipeline-12.trx'` passed **1/1** (`shader-native-pipeline-12.log`).

## Mandatory native SDL project is RED

The full opt-in native SDL project command was:

```powershell
dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj --configuration Release --no-restore -m:1 '-p:DefaultItemExcludesInProjectFolder=artifacts/**' --logger 'trx;LogFileName=shader-full-sdl-13.trx'
```

Result: **47 failed, 847 passed, 5 skipped, 899 total**, in 11m43s.
Raw `shader-full-sdl-13.log` and
`tests/Cerneala.Tests.SdlGpu/TestResults/shader-full-sdl-13.trx` retain the
failures. TRX grouping: 45 `NativeScenePrismDomainTests` exact-domain image
differences; one `NativePackageGridSubdivisionTests` exact byte difference
at position 6180 (expected 118, actual 117); one
`NativeRenderSurface3DTests.Near_view_parallel_line_remains_bounded_after_side_clipping`
color difference (expected `#ff0000ff`, actual `#ff0404ff`). These are
**failures**, not accepted tolerances.

A representative three-test controlled A/B used this exact filter:

```text
FullyQualifiedName~NativePackageGridSubdivisionTests.PreparedGridMatchesAuthoredPixelsAcrossBoundariesFlipsAndZoom|FullyQualifiedName~NativeScenePrismDomainTests.GlobalInputMatchesFullDomainRenderingCroppedAfterTheEffect|FullyQualifiedName~NativeRenderSurface3DTests.Near_view_parallel_line_remains_bounded_after_side_clipping
```

With the five centroid B targets and rebuilt backend, all **3 failed**
(`shader-failures-centroid-b-focused-14.log`, TRX). We then swapped all five
targets to the byte-verified original A artifacts, forced a Release backend
rebuild (`shader-failures-baseline-a-rebuild-15.log`), and ran the **same test
project/filter/environment**: all **3 passed**
(`shader-failures-baseline-a-focused-16.log`, TRX). Backend and test-copy DLLs
matched SHA-256 `0517DC49103BE4297C7429BF45CE1A2002AA25E8CAD97DEB6C0FDE8E4B5DF7C0`
for A. This establishes a shader-induced regression in representatives of all
three failure groups; it does not yet establish the violated invariant owner
or justify changing those tests.

For that initial A/B handoff, the five B targets were restored from their
checked bytes, and both the backend and SDL test project were built without
executing more tests
(`shader-failures-centroid-b-restored-rebuild-17.log`,
`shader-failures-centroid-b-testcopy-build-18.log`). Both B DLL copies now
matched SHA-256 `EE33B2B2FEE6EA123A8F584CE55803D6A3D594FDFF0F06E61634D28D8488AB13`.
That was historical state, **not the current workspace**. The independent
review rejected generic centroid after the 47 shader-induced conformance
failures. The original A shader source/artifacts/metadata were restored
byte-exact and rebuilt, and a new logical-domain native A/B was run; see
`diagnostic/logical-domain.md`. Current source and Release backend/test-copy
DLLs are original A, SHA-256
`0517DC49103BE4297C7429BF45CE1A2002AA25E8CAD97DEB6C0FDE8E4B5DF7C0`.
The temporary C# probe remains absent. A new renderer regression test is
intentionally RED on original A at the external Point crop boundary; there is
still **no accepted renderer fix**.

The separate historical Drawing/Prism 133-case conformance corpus, full
repository suite, Linux/macOS native runs, and human interaction validation
were not run here. Do not claim this renderer change is verified while the
native SDL gate is RED. No reference images or test expectations were changed.
