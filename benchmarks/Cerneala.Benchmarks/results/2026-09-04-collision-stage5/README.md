# Collision and picking stage 5 verification

Date: 2026-09-04

## Native backend scenarios

Both scenarios construct the same retained house from separate wall sprites and
box colliders, a collider-backed door entity, and a player circle collider. The
closed state must stop `MoveAndCollide` and be the first ray hit. The open state
must allow the complete requested displacement. Each live window also verifies a
scene -> root -> scene coordinate round trip through its actual arranged ViewBox.

All application captures were produced by `Window.SaveScreenshot`.

| Backend | Capture size | Closed colors | Changed door pixels | Collision/query | Coordinate round trip |
| --- | ---: | ---: | ---: | --- | --- |
| WindowsDX | 640x400 | 431 | 5,600 | PASS | PASS |
| SDL_GPU | 800x500 | 460 | 8,775 | PASS | PASS |

Commands:

```powershell
dotnet build .\tests\Cerneala.WindowsDxSmoke\Cerneala.WindowsDxSmoke.csproj -c Release --no-restore
dotnet build .\tests\Cerneala.SdlGpuSmoke\Cerneala.SdlGpuSmoke.csproj -c Release --no-restore

dotnet run --project .\tests\Cerneala.WindowsDxSmoke\Cerneala.WindowsDxSmoke.csproj -c Release --no-build --no-restore -- `
  --capture-collision .\benchmarks\Cerneala.Benchmarks\results\2026-09-04-collision-stage5\WindowsDx

dotnet run --project .\tests\Cerneala.SdlGpuSmoke\Cerneala.SdlGpuSmoke.csproj -c Release --no-build --no-restore -- `
  --mode collision `
  --artifacts .\benchmarks\Cerneala.Benchmarks\results\2026-09-04-collision-stage5\SdlGpu
```

Each backend directory contains the generated atlas, closed/open captures, and
`collision-backend.json` with dimensions, changed-pixel count, SHA-256 hashes,
and the query/coordinate gate results.

## Spatial benchmark rerun

The Stage 2 production runner was rerun with the frozen seed, eight warmup
passes, 48 measured passes, and exhaustive false-negative oracle. The full
result is `benchmark-results.json`.

| Frozen gate scenario | Update P95 | Query P95 | Retained bytes | False negatives | Gate |
| --- | ---: | ---: | ---: | ---: | --- |
| `large-sparse` | 51.4 us | 241.3 us | 1,425,544 | 0 | PASS |
| `high-churn` | 472.8 us | 138.0 us | 403,040 | 0 | PASS |
| `long-fence` | 18.9 us | 91.1 us | 205,184 | 0 | PASS |

Command:

```powershell
dotnet run --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -c Release --no-restore -- `
  --collision-stage2 .\benchmarks\Cerneala.Benchmarks\results\2026-09-04-collision-stage5\benchmark-results.json
```

## Other gates

- Strict API Compat: `api-compat.md`.
- Canonical API pages and markup guide: `docs-site/documentation/classes/` and
  `docs/CernealaMarkupGuide.md`.
- Source-generator documentation markup: `CollisionStage=5`.

