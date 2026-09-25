# Stage 3 SDL native fixture repair

The integrated opt-in SDL run reported 25 `NativeScenePrismDomainTests` failures
(`Expected: 9`, `Actual: 0`) and one `NativeImageLeaseTests` grid-cycle
failure (`Expected: 2`, `Actual: 1`). This worker reproduced one case from each
on the rebuilt native test binary before changing their assertions.

Every command below ran from the repository root with
`CERNEALA_SDL_NATIVE_TESTS=1`, Release, `-m:1`, `--nologo`, `-v:minimal`,
`-p:DefaultItemExcludesInProjectFolder=artifacts/**`, and the SDL test project.
For the first three runs, the filter was:

```text
FullyQualifiedName~NativeScenePrismDomainTests.LocalNeighborhoodInputMatchesCompleteDomainRendering|FullyQualifiedName~NativeImageLeaseTests.GridCameraInputRetiresAtlasesAndKeepsNpcTerrainAcross32Cycles
```

| Run | Build switches | Raw log / TRX | Result |
| --- | --- | --- | --- |
| Original assertions | `--no-build --no-restore` | [log](stage3-native-failures-uninstrumented.log) / [TRX](test-results/stage3-native-failures-uninstrumented.trx) | exit 1; 0 pass, 2 fail, 0 skip |
| Observational diagnostics, expectations unchanged | `--no-restore` | [log](stage3-native-failures-instrumented.log) / [TRX](test-results/stage3-native-failures-instrumented.trx) | exit 1; 0 pass, 2 fail, 0 skip |
| Contract-specific fixture repairs | `--no-restore` | [log](stage3-native-failures-repaired-focused.log) / [TRX](test-results/stage3-native-failures-repaired-focused.trx) | exit 0; 2 pass, 0 fail, 0 skip; 17 s |
| Both affected classes | `--no-build --no-restore` | [log](stage3-native-affected-classes.log) / [TRX](test-results/stage3-native-affected-classes.trx) | exit 0; 49 pass, 0 fail, 0 skip; 2 m 49 s |

Exact `dotnet test` invocations (PowerShell piped stdout/stderr through
`Tee-Object` to each matching log and exited with `$LASTEXITCODE`):

```powershell
$env:CERNEALA_SDL_NATIVE_TESTS = '1'
$out = 'docs/plans/evidence/2026-09-23-scene2d-stage3/native-windows/native-migration'
dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj -c Release -m:1 --nologo -v:minimal --no-build --no-restore '-p:DefaultItemExcludesInProjectFolder=artifacts/**' --filter 'FullyQualifiedName~NativeScenePrismDomainTests.LocalNeighborhoodInputMatchesCompleteDomainRendering|FullyQualifiedName~NativeImageLeaseTests.GridCameraInputRetiresAtlasesAndKeepsNpcTerrainAcross32Cycles' --logger 'trx;LogFileName=stage3-native-failures-uninstrumented.trx' --results-directory "$out/test-results" --blame-hang-timeout 2m
dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj -c Release -m:1 --nologo -v:minimal --no-restore '-p:DefaultItemExcludesInProjectFolder=artifacts/**' --filter 'FullyQualifiedName~NativeScenePrismDomainTests.LocalNeighborhoodInputMatchesCompleteDomainRendering|FullyQualifiedName~NativeImageLeaseTests.GridCameraInputRetiresAtlasesAndKeepsNpcTerrainAcross32Cycles' --logger 'trx;LogFileName=stage3-native-failures-instrumented.trx' --results-directory "$out/test-results" --blame-hang-timeout 2m
dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj -c Release -m:1 --nologo -v:minimal --no-restore '-p:DefaultItemExcludesInProjectFolder=artifacts/**' --filter 'FullyQualifiedName~NativeScenePrismDomainTests.LocalNeighborhoodInputMatchesCompleteDomainRendering|FullyQualifiedName~NativeImageLeaseTests.GridCameraInputRetiresAtlasesAndKeepsNpcTerrainAcross32Cycles' --logger 'trx;LogFileName=stage3-native-failures-repaired-focused.trx' --results-directory "$out/test-results" --blame-hang-timeout 2m
dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj -c Release -m:1 --nologo -v:minimal --no-build --no-restore '-p:DefaultItemExcludesInProjectFolder=artifacts/**' --filter 'FullyQualifiedName~NativeScenePrismDomainTests|FullyQualifiedName~NativeImageLeaseTests' --logger 'trx;LogFileName=stage3-native-affected-classes.trx' --results-directory "$out/test-results" --blame-hang-timeout 5m
```

The affected-class run did not rebuild the passing repaired source. Its 49
cases include all 25 previously failing Prism variants and the complete
32-cycle grid path.

## Evidence and correction

The diagnostic Prism failure occurred on `source=1` after Servo switched
`surface.Scene` to terrain. Before and after cropping, the retained actor
`ItemsSource` was the same nine-element list, but the actor control was
detached (`SimulationContext=null`, realized count zero) while terrain had
the active context. The unconditional actor count of nine was a fixture
mistake, not evidence that cropped active items disappeared. The repair checks
nine eager actor occurrences only on the actor branch, zero on the detached
terrain branch, unchanged source identity, and the existing map draw-count,
pixel parity, and final reattach identity checks.

The diagnostic grid failure occurred before its 32-cycle loop. The NPC's
active simulated collider occupied `x=1984..2000`; the only initially
resident map collider occupied `x=16..32`. The far wall begins at `x=2016`.
The retired manual source catalog had a wider collision envelope, whereas
the selected contract pins terrain only from a marked collider's *current*
geometry. The repair requests an explicit, bounded collision region over
the known upcoming movement sweep (`x=1984..2100`, `y=16..32`) and holds it
through Servo input cycles. This preserves original adapter counts
`2→1→1→2`, blocked movement, image-cache retire/reload, pixels, and 32-cycle
assertions rather than lowering counts or fabricating old metadata. The
region is disposed on the test owner thread during cleanup; cleanup attempts
and aggregates errors without discarding the primary test failure.

Final tested input SHA-256: `NativeScenePrismDomainTests.cs`
`A698A052F162DBC98AF3CD1FF1D8644F0A01B3F5E6E5A6EDAF985E391D63FA09`;
`NativeImageLeaseTests.cs`
`3B4F012BF366F2CFE2F5C4713A3690DDC25C1D2AA025A883E2E41970AF4F12E5`;
SDL csproj `F9B111DDC95F9508F14C3774EAD3B7B710629607A53BBD1C8C32CE4D65904DF2`.
`git diff --check` on the two owned source files exited zero (Git only warned
about its LF-to-CRLF working-copy policy). No SDL testhost process remained.
The complete SDL project and full solution have not been rerun after this
repair by this worker; integration owns those gates.
