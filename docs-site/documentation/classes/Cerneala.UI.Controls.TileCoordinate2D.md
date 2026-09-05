# TileCoordinate2D Structure

## Definition

Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/TileMap2DModel.cs`

Identifies one signed tile coordinate.

```csharp
public readonly record struct TileCoordinate2D(int X, int Y)
```

## Examples

```csharp
var coordinate = new TileCoordinate2D(-4, 12);
```

## Remarks

Coordinates are signed so sparse maps and chunks can extend into negative scene space.
