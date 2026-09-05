# TileCellKey2D Structure

## Definition

Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/TileMap2DModel.cs`

Combines a stable layer ID and tile coordinate for promotion lookup.

```csharp
public readonly record struct TileCellKey2D(string LayerId, TileCoordinate2D Coordinate)
```

## Examples

```csharp
var key = new TileCellKey2D("Buildings", 18, 11);
TileInstance2D door = map.Promote(key);
```

## Remarks

Both constructors reject an empty layer ID. The owning `TileMap2D` supplies map identity.
