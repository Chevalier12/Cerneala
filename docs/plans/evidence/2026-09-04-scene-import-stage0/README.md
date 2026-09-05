# Stage 0 verification checkpoint

Date: 2026-09-05. No production C# changed in this stage.

## Contract and fixtures

- [Compatibility contract](compatibility-matrix.md): runtime import, exact version acceptance, 399 field dispositions across 26 scopes, explicit supported/unsupported representations, path budgets, metadata conventions, diagnostics and zero-thickness two-sided polyline segments.
- `tests/Fixtures/Scene2DImport/New-Fixtures.ps1` generates original maps and diagnostic inputs. `atlas.svg` and `common.golden.json` are independently authored fixture assets/expectations, not copied editor sample content or importer output.
- Deterministic regeneration left all 33 JSON/map/tileset/level/atlas fixture files byte-identical.
- [Fixture validation](fixture-validation.json): 16 checks passed, including 10 native Tiled JSON round trips, official LDtk project/level schema validation and equality of inline/separate level data. All 15 intentionally invalid diagnostic cases have valid JSON syntax.
- Tiled 1.12.2 was downloaded from its official release and administratively extracted into ignored `.artifacts/scene-import-stage0/`; no system installation was performed. MSI SHA-256: `11A0E6C97CC105E07A57EA9995F2704617986722DE4AB699D286DAB0D2BECC3F`. Authenticode verification returned Valid. The Windows package has `qwindows`, not `qoffscreen`; the unsuccessful offscreen invocation was stopped, and the successful bounded export verifier uses the Windows plugin without an editor window.

## Executable RED evidence

| Suite | Result | Intended reason |
| --- | --- | --- |
| [Core](scene-import-stage0-core-red.trx) | 33 RED, 1 GREEN | 27 missing optional importer; 2 missing validator; 2 missing segment collider; 1 missing document data; 1 missing debug overlay. Every failure message was checked. The independent fixture/golden/category check passes. |
| [SourceGen](scene-import-stage0-sourcegen-red.trx) | 1 RED, 1 GREEN | Overlay markup fails only with `CERNEALAUI002` for the absent type. Existing imported-model binding and promoted-tile Aspect/Motion/Prism markup compile without an importer assembly dependency. |

The reflection contracts execute full semantic assertions after the planned types become available. They do not use production stubs or mark absent behavior skipped. Import cases are currently in the core test project so Stage 0 can run before the optional project exists; Stage 2 moves them into the optional test project. The full repository suite is intentionally not green at this RED checkpoint.

## Commands

```powershell
& tests/Fixtures/Scene2DImport/New-Fixtures.ps1
& tests/Fixtures/Scene2DImport/New-CompatibilityMatrix.ps1 -LdtkSchemaFile .artifacts/scene-import-stage0/ldtk-1.5.3.schema.json
& tests/Fixtures/Scene2DImport/Test-Fixtures.ps1 `
  -LdtkSchemaFile .artifacts/scene-import-stage0/ldtk-1.5.3.schema.json `
  -TiledExecutable .artifacts/scene-import-stage0/tiled-1.12.2/PFiles/Tiled/tiled.exe `
  -OutputDirectory .artifacts/scene-import-stage0/fixture-verification

dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj --no-restore -p:BuildProjectReferences=false --filter SceneImportStage=0 --logger "trx;LogFileName=scene-import-stage0-core-red.trx" --results-directory .artifacts/scene-import-stage0/red -m:1
dotnet test tests/Cerneala.Tests.SourceGen/Cerneala.Tests.SourceGen.csproj --no-restore -p:BuildProjectReferences=false --filter SceneImportStage=0 --logger "trx;LogFileName=scene-import-stage0-sourcegen-red.trx" --results-directory .artifacts/scene-import-stage0/red -m:1
```

Dependency builds completed before the recorded runs; `BuildProjectReferences=false` reused that current state. The initial build also rebuilt the existing MonoGame shader artifact and reported HLSL warnings. An initial test-fixture compile error was corrected before the recorded core RED run; it was not counted as regression evidence.

Roslyn index was refreshed after the final C# edit. It succeeded with 11 existing indexing warnings (missing-metadata diagnostics and large non-semantic artifacts); this is not a zero-warning claim. `git diff --check` passed for stage files. There was no human visual validation and none is claimed.
