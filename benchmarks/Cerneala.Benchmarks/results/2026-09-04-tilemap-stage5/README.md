# TileMap2D stage 5: backend conformance

This directory contains deterministic `TileMap2D` captures from the real WindowsDX/MonoGame and SDL_GPU application paths.

## Scenario

- Logical viewport: 640 x 420 scene units.
- Physical capture: 800 x 525 pixels on both backends.
- Multisampling: disabled explicitly for the WindowsDX fixture; the SDL_GPU fixture uses its non-MSAA presentation path.
- Map: two 8 x 10 chunks forming a 16 x 10 map, two atlases, and two semantic layers.
- Covered state: layer/map/tile transforms, opacity, tint, horizontal and vertical flips, the x=8 chunk edge, camera pan and zoom, and a promoted tile at x=7/y=4.
- The promoted tile receives Aspect state, a Motion tint sample, and an individual Prism `Invert` scope.
- Every application capture is produced by `Window.SaveScreenshot(string)`. No OS-level capture path is used.

Each backend directory contains the generated atlases, three captures, and `tilemap-conformance-backend.json` with feature checks, changed-pixel counts, and SHA-256 hashes.

## Reproduction

From the repository root:

```powershell
dotnet build .\tests\Cerneala.WindowsDxSmoke\Cerneala.WindowsDxSmoke.csproj -c Release --no-restore
dotnet build .\tests\Cerneala.SdlGpuSmoke\Cerneala.SdlGpuSmoke.csproj -c Release --no-restore

dotnet run --project .\tests\Cerneala.WindowsDxSmoke\Cerneala.WindowsDxSmoke.csproj -c Release --no-build --no-restore -- `
  --capture-tilemap-conformance .\benchmarks\Cerneala.Benchmarks\results\2026-09-04-tilemap-stage5\WindowsDx

dotnet run --project .\tests\Cerneala.SdlGpuSmoke\Cerneala.SdlGpuSmoke.csproj -c Release --no-build --no-restore -- `
  --mode tilemap-conformance `
  --artifacts .\benchmarks\Cerneala.Benchmarks\results\2026-09-04-tilemap-stage5\SdlGpu

dotnet run --project .\tests\Cerneala.WindowsDxSmoke\Cerneala.WindowsDxSmoke.csproj -c Release --no-build --no-restore -- `
  --verify-tilemap-conformance `
  .\benchmarks\Cerneala.Benchmarks\results\2026-09-04-tilemap-stage5\WindowsDx `
  .\benchmarks\Cerneala.Benchmarks\results\2026-09-04-tilemap-stage5\SdlGpu `
  .\benchmarks\Cerneala.Benchmarks\results\2026-09-04-tilemap-stage5\comparison.json
```

## Contract and result

`comparison.json` compares all 800 x 525 physical pixels. Interior pixels must match within a per-channel tolerance of 3. A differing pixel may use the one-pixel raster-edge tolerance only when it is on a local color edge in both captures and a color from either pixel matches the other backend's 3 x 3 neighborhood within the channel tolerance. This permits backend-specific subpixel edge coverage without accepting displaced geometry, missing content, or an interior color mismatch.

| Capture | Pixels over channel tolerance | Accepted one-pixel edge differences | Unresolved pixels | Maximum channel delta |
| --- | ---: | ---: | ---: | ---: |
| Initial | 8,525 | 8,525 | 0 | 96 |
| Motion + Prism | 8,525 | 8,525 | 0 | 96 |
| Pan + zoom | 8,235 | 8,235 | 0 | 91 |

The remaining differences are confined to backend rasterization coverage at transformed sprite edges. Global half-pixel offsets and sampler changes were tested and rejected because they increased disagreement or made no change. No golden artifact was updated (`GoldenUpdated` is `false`).

## Resolved mismatches

The initial comparison exposed contract violations rather than a tilemap-specific backend path:

- SDL_GPU applied nested opacity independently to each primitive. Overlapping children therefore differed from MonoGame's group-opacity result. `PushOpacity` now renders the complete scope to a compositing target and applies opacity once when the scope closes.
- A Prism host range could split an open opacity/layer scope. SDL_GPU now carries the active target, draw state, and compositing-scope stack across host ranges; Prism captures and presents into that active target.
- Prism graph analysis previously lost the scene/ViewBox transform before backend execution. The analyzed combined transform is now retained in the graph.
- MonoGame now restores a Prism host to the active outer drawing-layer target rather than assuming the root target.

The conformance scene still emits the common `DrawSpriteBatch`/scene command contract. No backend-native tilemap command, resource-ownership change, or golden rewrite was introduced.
