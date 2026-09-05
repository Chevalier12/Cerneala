# TileCell2D Structure

## Definition

Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/TileMap2DModel.cs`

Stores a tile ID and horizontal/vertical flip flags.

```csharp
public readonly record struct TileCell2D
```

## Examples

```csharp
var cell = new TileCell2D(1, TileFlip2D.Horizontal);
var empty = new TileCell2D(0);
```

## Remarks

ID `0` is empty. Negative IDs and unknown flip bits are rejected.

## Properties

| Name | Description |
| --- | --- |
| `TileId` | Non-negative global tile ID. |
| `Flip` | Horizontal/vertical source reflection flags. |
