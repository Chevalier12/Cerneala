# Sprite2D sampling contract evidence

The native Village atlas-seam reproduction is owned separately by the Village app worker. This directory records the permanent Core and generated-markup contract tests for the approved per-sprite API.

All valid test commands were run from the repository root with both the process environment variable and explicit MSBuild property set to exclude pre-existing generated `artifacts/**` source from the root project's SDK glob:

```powershell
$env:DefaultItemExcludesInProjectFolder='artifacts/**'
dotnet test 'tests/Cerneala.Tests/Cerneala.Tests.csproj' --no-restore '-p:DefaultItemExcludesInProjectFolder=artifacts/**' --filter 'FullyQualifiedName~Sprite2DSamplingContractTests'
dotnet test 'tests/Cerneala.Tests.SourceGen/Cerneala.Tests.SourceGen.csproj' --no-restore '-p:DefaultItemExcludesInProjectFolder=artifacts/**' --filter 'FullyQualifiedName~SpriteSamplingMarkup'
```

Before the production change, the valid Core RED (`sprite-sampling-core-red-valid.log`) failed compilation only because `Sprite2D.Sampling` and `SamplingProperty` did not exist. The valid SourceGen RED (`sprite-sampling-sourcegen-red-valid.log`) likewise failed only on the missing `Sprite2D.Sampling` getter. No source or backend change preceded these results. An earlier `sprite-sampling-core-red.log` is **not** valid RED evidence: it failed while compiling generated files already present under `artifacts/**` into `Cerneala.csproj` (duplicate assembly attributes and missing Xunit).

After the production change:

- `sprite-sampling-core-green.log` / `.trx`: 7 passed, 0 failed. Default Linear, per-instance Point, render-only invalidation, rejected unsupported enum value, retained animated crop/draw options, and all valid flip mappings.
- `sprite-sampling-sourcegen-green.log` / `.trx`: 1 passed, 0 failed. Generated markup literal Point and one-way enum binding update.
- `sprite-sampling-affected-core.log` / `.trx`: 68 passed, 0 failed (`Sprite2D`, `SpriteAnimation`, `TileMap2DContractTests` filter).
- `sprite-sampling-affected-sourcegen.log` / `.trx`: 22 passed, 0 failed (`Sprite` filter).

These tests inspect recorded draw-command options; the original native screenshot/pixel reproduction and full repository suite are separate gates. No manual validation or performance measurement is claimed here.
