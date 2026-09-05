# TileMapBounds2D Structure

## Definition

Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/TileMap2DModel.cs`

Defines positive finite map bounds in tile coordinates.

```csharp
public readonly record struct TileMapBounds2D
```

## Examples

```csharp
var bounds = new TileMapBounds2D(0, 0, 128, 96);
```

## Remarks

Width and height must be positive. A `TileMap2DModel` uses null rather than this structure to represent sparse/infinite bounds.

Exclusive `Right` and `Bottom` endpoints must fit Int32 at construction. The default zero-initialized structure is not a valid map bound and is rejected by `TileMap2DModel`.

## Properties

| Name | Description |
| --- | --- |
| `X`, `Y` | Top-left tile coordinate. |
| `Width`, `Height` | Positive size in cells. |
| `Right`, `Bottom` | Exclusive coordinate limits. |

## Methods

| Name | Description |
| --- | --- |
| `Contains` | Tests whether a coordinate is inside the finite bounds. |
