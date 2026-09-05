# TileFlip2D Enum

## Definition

Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/TileMap2DModel.cs`

Defines composable horizontal, vertical, and normalized diagonal tile-source reflection.

```csharp
[Flags]
public enum TileFlip2D
```

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `None` | 0 | No reflection. |
| `Horizontal` | 1 | Reflect horizontally. |
| `Vertical` | 2 | Reflect vertically. |
| `Diagonal` | 4 | Swap normalized X/Y axes before horizontal and vertical reflection. |

## Remarks

All eight combinations are supported for static and promoted tiles. Diagonal swaps bottom-left and top-right corners, matching the [Tiled orthogonal GID contract](https://doc.mapeditor.org/en/stable/reference/global-tile-ids/). On a rectangular tile it swaps normalized axes within the original destination cell, not the map grid dimensions. Rendering uses existing batched sprite rotation/UV commands; imported collider geometry receives the equivalent affine transform. No static cell becomes a public scene node.

## Examples

```csharp
TileFlip2D flip = TileFlip2D.Horizontal | TileFlip2D.Vertical;
```
