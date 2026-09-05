# TileMap2D API compatibility

Date: 2026-09-04

## Baseline

The strict comparison uses the detached Servo baseline at commit `fed724b954bc2823c4799db69c94b92e2790b2b5` and composes the already audited Servo and RenderSurface2D scene-foundation suppression files with the tilemap additions.

- Baseline assembly: `C:\Users\lauri\Desktop\Cerneala-baseline-servo-fed724b\bin\Release\net8.0\Cerneala.dll`
- Baseline size: 5,031,936 bytes
- Baseline SHA-256: `2D20937BFC0783CEC52CB8F8328C4F27625E56C99A808C736C28FD822B8297F1`
- Current assembly: `C:\Users\lauri\Desktop\Cerneala\bin\Release\net8.0\Cerneala.dll`
- Current size: 5,165,568 bytes
- Current SHA-256: `E851C252840946D5128939C29B3C5574A135A31FDA79A8C3C6B3E560A165DA56`

## Approved tilemap additions

Strict API Compat reports each new public type as an additive difference. `api-compat.suppressions.xml` contains exactly the 13 types approved by the tilemap plan:

- `TileMap2D`, `TileLayer2D`, and `TileInstance2D`;
- `TileMap2DModel`, `TileLayer2DModel`, `TileSet2D`, `TileDefinition2D`, and `TileChunk2D`;
- `TileCell2D`, `TileCellKey2D`, `TileCoordinate2D`, `TileMapBounds2D`, and `TileFlip2D`.

Suppressing each new type covers only its members because the entire type is absent from the baseline. The file does not suppress a removal, signature change, or unrelated API. `PermitUnnecessarySuppressions` is `false`, so a stale or overbroad listed suppression fails the gate.

## Strict gate

```powershell
dotnet build .\Cerneala.csproj -c Release --no-restore
dotnet msbuild .\benchmarks\Cerneala.Benchmarks\results\2026-09-04-tilemap-stage5\api-compat.proj -t:Compare -v:minimal
```

Result: both commands exited 0. The Release build produced zero warnings and zero errors; strict API Compat reported no unsuppressed or unnecessary difference.
