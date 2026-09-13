# TileCellKey2D Structure

## Definition

Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/TileMap2DModel.cs`

Combines a stable map ID and grid coordinate for imported cell metadata.

```csharp
public readonly record struct TileCellKey2D
```

## Examples

```csharp
var key = new TileCellKey2D("Buildings", 18, 11);
TileMap2DModel buildings = level.TileMaps.Single(map => map.Id == key.MapId);
bool exists = buildings.TryGetCell(key.Coordinate, out TileCell2D cell);
```

## Remarks

Both constructors require a nonempty map ID. Coordinates may be negative for sparse grids. The key is data; it does not create or retrieve a live scene node. Owning-level validation resolves keys used by sparse composition metadata.

## Constructors

| Name | Description |
| --- | --- |
| `TileCellKey2D(string mapId, int x, int y)` | Creates a stable map/cell address. |
| `TileCellKey2D(string mapId, TileCoordinate2D coordinate)` | Creates an address from a coordinate value. |

## Properties

| Name | Description |
| --- | --- |
| `MapId` | Stable owning map identity. |
| `Coordinate` | Grid cell address. |
