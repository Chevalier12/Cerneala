# TileChunk2D Class

## Definition

Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/TileMap2DModel.cs`

Stores one immutable rectangular row-major block of tile cells.

```csharp
public sealed class TileChunk2D
```

## Examples

```csharp
var chunk = new TileChunk2D(
    new TileCoordinate2D(0, 0),
    2,
    1,
    [new TileCell2D(1), new TileCell2D(0)]);
```

## Remarks

Width, height, and version must be positive. `Tiles.Count` must equal `Width * Height`. Negative origins are valid for sparse maps.

A chunk is limited to 1,048,576 cells. Dimension multiplication and exclusive Int32 endpoints are checked before cell enumeration. Enumeration stops at the first excess cell instead of consuming an unbounded tail. Failures retain argument-exception categories and can be mapped through [Scene2DModelValidator.GetDiagnostic](Cerneala.UI.Controls.Scene2DModelValidator.md).

## Properties

| Name | Description |
| --- | --- |
| `Origin` | Top-left tile coordinate. |
| `Width`, `Height` | Chunk dimensions in cells. |
| `Tiles` | Copied row-major cell view. |
| `Version` | Positive cache-visible chunk version. |
| `Properties` | Copied opaque importer metadata. |

## Methods

| Name | Description |
| --- | --- |
| `Contains` | Tests whether a tile coordinate belongs to the chunk. |
| `GetCell` | Returns the cell at a contained coordinate. |
