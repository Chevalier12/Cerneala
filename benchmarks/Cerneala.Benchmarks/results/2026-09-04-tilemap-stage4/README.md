# TileMap2D stage 4: batching and backend profile

This directory archives the stage 4 measurements for the deterministic village fixture defined by `TileMapStage4ModelFactory`.

## Environment

- Commit: `2d82aaca04ea7b00f534e6148a367df08e386fc2` with a dirty working tree containing the plan implementation.
- Runtime: .NET 8.0.30, x64, Windows 10.0.26200.
- CPU: Intel64 Family 6 Model 158 Stepping 10, 8 logical processors.
- Fixture: seed 23063, 128x96 tiles, 3 layers, 16x16 chunks, 48x32-tile viewport, two atlases.
- Core benchmark: 64 warmup iterations and 512 measured iterations per scenario.
- Native backend profile: 12 warmup frames and 96 measured frames per scenario and backend, at 768x512 pixels and coordinate scale 1.

## Reproduction

From the repository root:

```powershell
dotnet build .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -c Release --no-restore

dotnet run --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -c Release --no-build --no-restore -- `
  --tilemap-stage4 .\benchmarks\Cerneala.Benchmarks\results\2026-09-04-tilemap-stage4\optimized.json

dotnet run --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -c Release --no-build --no-restore -- `
  --tilemap-stage4-backends .\benchmarks\Cerneala.Benchmarks\results\2026-09-04-tilemap-stage4\backend-profile.json
```

The native profile creates real WindowsDX and SDL_GPU windows, renders through the existing `DrawCommand.RenderSurface2D` path, and uses generated local atlases `profile-terrain.png` and `profile-structures.png`.

## Core gate results

The canonical passing result is `optimized.json`.

| Scenario | P95 CPU | Gate | Allocation | Gate | Commands | Rebuild / reuse |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Warm static | 458.8 us | <= 875 us | 15,819 B/op | <= 30,000 B/op | 36 | 0 / 36 |
| Camera pan | 189.3 us | <= 1,460 us | 18,834 B/op | <= 192,000 B/op | 48 | 0 / 48 |
| One-chunk mutation | 803.2 us | <= 1,135 us | 198,923 B/op | <= 717,000 B/op | 36 | 1 / 35 |

The warm viewport retains 917,120 bytes against the 1 MiB gate. The full fixture retains 4,891,904 bytes against the 5 MiB gate. All 19 archived gates pass.

Compared with the stage 0 baseline:

| Scenario | P95 reduction | P95 speedup | Allocation reduction |
| --- | ---: | ---: | ---: |
| Warm static | 86.9% | 7.62x | 99.5% |
| Camera pan | 93.5% | 15.40x | 99.5% |
| One-chunk mutation | 64.5% | 2.82x | 93.1% |

`optimized-rerun.json` is retained as pre-fix evidence: before topology-compatible spatial-index reuse, the mutation P95 was 1,254.6 us and missed the 1,135 us gate. The final path reuses spatial buckets when only chunk contents/versions change and resolves tile IDs through the immutable model lookup.

## Batching contract

- Static tiles are grouped by atlas inside one semantic order segment and emitted through the existing `DrawSpriteBatch` command with Point sampling and Clamp addressing.
- Atlas grouping never crosses a layer or promoted-tile order boundary.
- A promoted tile flushes its current order segment, occupies its row-major semantic slot, and starts a new static segment only if later tiles require it.
- `BatchSplits` counts only a promoted/Prism slot with static content on both sides. With zero promotions it remains zero and the compact static path emits 36 commands for the warm viewport rather than one command per tile.
- Static chunk intersection is half-open: a chunk touching a viewport only at an edge has zero visible area and is excluded. Promoted Motion/Prism nodes retain conservative individual bounds so an effect touching the edge is not falsely culled.

## Backend profile and damage tracking

The canonical native result is `backend-profile.json`.

| Backend / scenario | Wall P50 | Wall P95 | Command P95 | Core commands | Core rebuild / reuse |
| --- | ---: | ---: | ---: | ---: | ---: |
| WindowsDX / warm static | 20.2 us | 187.5 us | 160.9 us | 36 | 0 / 36 |
| WindowsDX / camera pan | 6,504.6 us | 9,221.7 us | 9,194.2 us | 48 | 0 / 48 |
| WindowsDX / mutation | 3,169.4 us | 6,882.5 us | 6,856.7 us | 36 | 1 / 35 |
| SDL_GPU / warm static | 124.1 us | 389.0 us | 12.8 us | 36 | 0 / 36 |
| SDL_GPU / camera pan | 1,686.2 us | 8,180.3 us | 7,463.1 us | 48 | 0 / 48 |
| SDL_GPU / mutation | 679.3 us | 1,260.0 us | 885.7 us | 36 | 1 / 35 |

The difference is explicit rather than normalized away:

- WindowsDX owns a `MonoGameRenderSurface2DSession` with retained command comparison and damage rectangles. The unchanged measured sequence caused zero inner rasterizations. Pan and mutation caused 96/96 rasterizations. In this scenario the ViewBox transform is context-sensitive, so the retained analyzer conservatively reported `ContextSensitiveCommand` and a full 393,216-pixel damage rectangle; average replay was 51.25 commands for pan and 40 for mutation.
- SDL_GPU retains the offscreen surface while the frame version is unchanged, producing one outer composite draw call in the warm sequence. When the frame version changes, `AddRenderSurface` clears and rerecords the full offscreen command list; it exposes no damage rectangle. The measured pan averaged 20.3125 draw calls and 48.25 submissions; mutation averaged 17 draw calls and 37 submissions.

No backend resource ownership, public draw contract, or backend-native tilemap command changed. The core numeric gate passed, so the conditional low-level command amendment was not triggered.
