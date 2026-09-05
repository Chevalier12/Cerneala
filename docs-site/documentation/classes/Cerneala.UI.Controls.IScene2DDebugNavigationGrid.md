# IScene2DDebugNavigationGrid Interface

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Scene2DDebugOverlay.cs`

```csharp
public interface IScene2DDebugNavigationGrid
```

## Remarks

Supplies application-owned navigation visualization data to [Scene2DDebugOverlay](Cerneala.UI.Controls.Scene2DDebugOverlay.md). Coordinates are in the containing scene's local space before the overlay's presentation transform. The overlay never writes this data and does not infer a route or collider from it.

Provider properties must stay stable during a frame. Cells must have positive finite dimensions, origin must be finite, and bounds must be valid. Invalid grid geometry throws `InvalidOperationException` when recorded. Implement `TryGetCell` as a bounded coordinate lookup; the overlay does not request off-viewport cells. A provider must not mutate the observed scene while answering a diagnostic query.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Bounds` | `TileMapBounds2D` | Inclusive origin and exclusive right/bottom cell limits. |
| `Origin` | `DrawPoint` | Local-space location of coordinate `(0,0)`. |
| `CellSize` | `DrawSize` | Local-space dimensions of one cell. |

## Methods

| Name | Description |
| --- | --- |
| `TryGetCell(int x, int y, out bool blocked)` | Returns false for absent cells; otherwise false `blocked` means green traversable and true means red blocked. |
