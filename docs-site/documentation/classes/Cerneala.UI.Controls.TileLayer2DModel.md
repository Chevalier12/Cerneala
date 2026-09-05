# TileLayer2DModel Class

## Definition

Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/TileMap2DModel.cs`

Defines the data, ordering, and default presentation of one tile layer.

```csharp
public sealed class TileLayer2DModel
```

## Examples

```csharp
var ground = new TileLayer2DModel("Ground", chunks, order: 0);
```

## Remarks

Chunks are copied and may not overlap. `Order` is the primary semantic layer order; source order breaks ties. Visibility, offset, opacity, and tint compose with the corresponding `TileLayer2D` presentation node.

Construction bounds enumeration to 65,536 chunks. Invalid overlap, identity, numeric values, or versions have stable diagnostics available through [Scene2DModelValidator](Cerneala.UI.Controls.Scene2DModelValidator.md).

## Properties

| Name | Description |
| --- | --- |
| `Id` | Stable layer ID. |
| `Chunks` | Immutable existing chunks. |
| `Order` | Semantic layer order. |
| `IsVisible`, `Offset`, `Opacity`, `Tint` | Model presentation defaults. |
| `Version` | Positive cache-visible layer version. |
| `Properties` | Copied opaque importer metadata. |

## Methods

| Name | Description |
| --- | --- |
| `TryGetCell` | Resolves a coordinate from existing chunks without inventing sparse cells. |
