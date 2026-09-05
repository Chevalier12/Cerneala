# Scene2DDebugFlags Enum

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Scene2DDebugOverlay.cs`

```csharp
[Flags]
public enum Scene2DDebugFlags
```

## Remarks

Combine independent bits on [Scene2DDebugOverlay.Flags](Cerneala.UI.Controls.Scene2DDebugOverlay.md). Undefined bits are rejected. These flags do not modify imported models, picking, or collision participation.

## Fields

| Name | Value | Description |
| --- | --- | --- |
| `None` | `0` | No debug work or commands. |
| `Colliders` | `1` | Active exact collider outlines and layer/mask/filter-state labels. |
| `ChunkBounds` | `2` | Visible map chunk outlines. |
| `TileCoordinates` | `4` | Visible cell coordinates, including empty cells. |
| `TileIds` | `8` | Visible core tile IDs; zero identifies an empty cell. |
| `Order` | `16` | Recorded scene order/Y anchors and map layer/chunk labels. |
| `Navigation` | `32` | Viewport-bounded external navigation grid. |
| `PromotedTiles` | `64` | Original static slot, current quad, connector, and stable promotion identity. |
| `All` | `127` | Every defined diagnostic category. |
