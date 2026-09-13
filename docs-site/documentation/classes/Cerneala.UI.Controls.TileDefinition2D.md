# TileDefinition2D Class

## Definition

Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/TileMap2DModel.cs`

Defines one positive global tile ID, its atlas source rectangle, importer metadata, and an optional collision descriptor.

```csharp
public sealed class TileDefinition2D
```

## Examples

```csharp
var wall = new TileDefinition2D(
    100,
    new DrawRect(16, 0, 16, 16),
    collider: new TileColliderDescriptor2D(
        TileColliderShape2D.Box,
        width: 16,
        height: 16,
        debugIdentity: "house-wall"));
```

## Remarks

IDs must be positive and source rectangles must have positive dimensions. The owning map model rejects IDs shared by multiple tilesets.

`Properties` is copied and `Collider` references an immutable descriptor. Replacing a definition is the supported way to change imported collision metadata.

Each definition owns zero or one descriptor by construction. The owning map additionally bounds collider expansion across repeated cells before any collision adapters are created; see [Scene2DModelValidator](Cerneala.UI.Controls.Scene2DModelValidator.md).

Every non-empty tile cell that resolves to this definition receives its optional collision descriptor. The tilemap collision adapter remains scene-owned; graphics backends never receive collider forms.

## Constructors

| Name | Description |
| --- | --- |
| `TileDefinition2D(int, DrawRect, IReadOnlyDictionary<string, object?>?, TileColliderDescriptor2D?)` | Creates a tile definition with copied optional metadata and one optional immutable collider descriptor. |

## Properties

| Name | Description |
| --- | --- |
| `Id` | Positive global tile ID. |
| `SourceRect` | Rectangle in atlas pixel coordinates. |
| `Properties` | Copied opaque importer metadata. |
| `Collider` | Optional immutable tile-local collision descriptor adapted for every cell using this definition. |

## Exceptions

- `ArgumentOutOfRangeException` is thrown when `id` is not positive or `sourceRect` has a non-positive dimension.

## Applies to

Project: `Cerneala`

## See also

- [TileColliderDescriptor2D](Cerneala.UI.Controls.TileColliderDescriptor2D.md)
- [TileMap2D](Cerneala.UI.Controls.TileMap2D.md)
