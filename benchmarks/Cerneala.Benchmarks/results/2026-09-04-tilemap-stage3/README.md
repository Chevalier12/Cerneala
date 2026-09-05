# TileMap2D stage 3 culling evidence

Captured on 2026-09-04 through the application-owned `Window.SaveScreenshot`
path. The fixture contains four visible one-tile chunks with alternating atlas
colors. The second capture pans the `ViewBox` by exactly one 16-pixel tile.

Both native harnesses fail the run unless every output pixel belongs to one of
the two expected tile colors, the middle scanline contains exactly three tile
boundaries, and the first pixel changes after pan. This detects missing edge
tiles, background holes, a pan that was not applied, and whole-surface overdraw.

| Backend | Capture | Size | SHA-256 |
| --- | --- | ---: | --- |
| WindowsDX / MonoGame | `WindowsDx/tilemap-before.png` | 640x240 | `81AC69A0C754755CDBB422935BE1340C807EE55F61A07AE020A5104754B5AACD` |
| WindowsDX / MonoGame | `WindowsDx/tilemap-after.png` | 640x240 | `B2842E83854847BB0C71DEF82381A9AFA8B96A787187A17546ADAFF70CCD45C3` |
| SDL_GPU | `SdlGpu/tilemap-before.png` | 800x525 | `F52D8CCA5C449E018DAEB2CB761B3E682201D197ECC88007CD828247F85FBD8C` |
| SDL_GPU | `SdlGpu/tilemap-after.png` | 800x525 | `BD45D088CAABF252191A908CBE97B3D3B54088175D0947079A90FC907DAAFC9F` |

Reproduction commands, from the repository root:

```powershell
dotnet run --no-build --project .\tests\Cerneala.WindowsDxSmoke\Cerneala.WindowsDxSmoke.csproj -- --capture-tilemap .\benchmarks\Cerneala.Benchmarks\results\2026-09-04-tilemap-stage3\WindowsDx
dotnet run --no-build --project .\tests\Cerneala.SdlGpuSmoke\Cerneala.SdlGpuSmoke.csproj -- --mode tilemap --artifacts .\benchmarks\Cerneala.Benchmarks\results\2026-09-04-tilemap-stage3\SdlGpu
```

Focused runtime counters from `TileMap2DCacheContractTests`:

- total chunks: 258;
- candidate chunks: at most 4;
- exactly 1 visible chunk;
- 64 candidate/drawn tiles for the 8x8 visible chunk;
- promoted Motion/Prism tile: 1 visible or 1 culled according to the viewport,
  never both, with attachment and active Motion preserved while culled.
