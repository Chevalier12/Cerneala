# ImageReference Class

## Definition

Namespace: `Cerneala.UI.Resources`

Assembly/Project: `Cerneala`

Source: `UI/Resources/ImageReference.cs`

An immutable choice between a direct `IDrawImage` and a typed `ResourceId<ImageResource>`.

```csharp
public sealed class ImageReference : IEquatable<ImageReference>
```

## Examples

```csharp
Sprite2D sprite = new()
{
    Image = new ImageReference(new ResourceId<ImageResource>("WorldAtlas")),
    X = 100,
    Y = 80,
    SourceX = 32,
    SourceWidth = 16,
    SourceHeight = 24
};
```

For an existing `IDrawImage`, use `new ImageReference(image)`. To clear a sprite's image, assign `null` to `Sprite2D.Image`.

## Remarks

Exactly one constructor-selected source is present. There is no precedence or fallback between two sources. Construction does not resolve resources, load images, or transfer image ownership. `Sprite2D` resolves resource IDs through its normal element/root resource scope and root image cache. `Tile` uses the same reference type; its containing `TileMap2D` owns resource resolution.

Equality compares direct images by reference identity, or resource IDs by value. A null direct image or an empty/default resource ID is rejected.

## Constructors

| Name | Description |
| --- | --- |
| `ImageReference(IDrawImage)` | References a non-null existing image. |
| `ImageReference(ResourceId<ImageResource>)` | References a resource by non-empty typed ID. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `DirectImage` | `IDrawImage?` | Direct image, or null for a resource reference. Read-only. |
| `ResourceId` | `ResourceId<ImageResource>?` | Resource ID, or null for a direct image. Read-only. |

## Methods

| Name | Description |
| --- | --- |
| `Equals(ImageReference?)` | Compares source identity. |
| `Equals(object?)` | Compares another image reference. |
| `GetHashCode()` | Returns a hash consistent with equality. |

## See also

- [Sprite2D](Cerneala.UI.Controls.Sprite2D.md)
- [Tile](Cerneala.UI.Controls.Tile.md)
