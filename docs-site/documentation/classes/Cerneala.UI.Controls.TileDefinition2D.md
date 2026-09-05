# TileDefinition2D Class

## Definition

Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/TileMap2DModel.cs`

Defines one positive global tile ID, its atlas source rectangle, importer metadata, and optional collision descriptors.

```csharp
public sealed class TileDefinition2D
```

## Examples

```csharp
var wall = new TileDefinition2D(
    100,
    new DrawRect(16, 0, 16, 16),
    colliders:
    [
        new TileColliderDescriptor2D(
            TileColliderShape2D.Box,
            width: 16,
            height: 16,
            debugIdentity: "house-wall")
    ]);
```

## Remarks

IDs must be positive and source rectangles must have positive dimensions. The owning map model rejects IDs shared by multiple tilesets.

Constructor inputs are copied. `Properties` and `Colliders` are exposed as read-only collections, so replacing a definition is the supported way to change imported collision metadata.

Collider enumeration is limited to 4,096 descriptors per definition. The owning map additionally bounds descriptor expansion across repeated cells before any collision adapters are created; see [Scene2DModelValidator](Cerneala.UI.Controls.Scene2DModelValidator.md).

Every non-empty tile cell that resolves to this definition receives the listed collision descriptors. The tilemap collision adapter remains scene-owned; graphics backends never receive collider forms.

## Constructors

| Name | Description |
| --- | --- |
| `TileDefinition2D(int, DrawRect, IReadOnlyDictionary<string, object?>?, IEnumerable<TileColliderDescriptor2D>?)` | Creates a tile definition and copies its optional metadata and collider descriptors. |

## Properties

| Name | Description |
| --- | --- |
| `Id` | Positive global tile ID. |
| `SourceRect` | Rectangle in atlas pixel coordinates. |
| `Properties` | Copied opaque importer metadata. |
| `Colliders` | Copied tile-local collision descriptors adapted for every cell using this definition. |

## Exceptions

- `ArgumentOutOfRangeException` is thrown when `id` is not positive or `sourceRect` has a non-positive dimension.
- `ArgumentException` is thrown when `colliders` contains a null descriptor.

## Applies to

Project: `Cerneala`

## See also

- [TileColliderDescriptor2D](Cerneala.UI.Controls.TileColliderDescriptor2D.md)
- [TileMap2D](Cerneala.UI.Controls.TileMap2D.md)
