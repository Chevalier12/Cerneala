# TileSet2D Class

## Definition

Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/TileMap2DModel.cs`

Maps unique positive tile definitions to one shared image atlas resource.

```csharp
public sealed class TileSet2D
```

## Examples

```csharp
var terrain = new TileSet2D(
    "Terrain",
    new ResourceId<ImageResource>("VillageTerrain"),
    [new TileDefinition2D(1, new DrawRect(0, 0, 16, 16))]);
```

## Remarks

The constructor requires a non-empty ID, atlas resource ID, at least one tile, unique tile IDs, and a positive version. Inputs and importer properties are copied.

Definition enumeration is limited to 1,048,576 entries. Source rectangles are checked against actual/declared atlas dimensions by [Scene2DModelValidator](Cerneala.UI.Controls.Scene2DModelValidator.md); the constructor does not load an image.

In a complete [TileMap2DModel](Cerneala.UI.Controls.TileMap2DModel.md), the public `TileSets` collection exposes the authored definitions. A prepared package keeps only used definitions with its private loaded chunk data and returns the complete authored palette through map metadata. Changing a set's definitions, atlas reference or metadata requires a replacement immutable model/map; incrementing `TileSet2D.Version` does not mutate an existing node.

## Properties

| Name | Description |
| --- | --- |
| `Id` | Stable tileset ID. |
| `AtlasResourceId` | Shared `ImageResource` atlas ID. |
| `Tiles` | Immutable tile definitions. |
| `Version` | Positive authored tileset version preserved in prepared data. |
| `Properties` | Copied opaque importer metadata. |
