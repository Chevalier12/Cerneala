# DrawSpriteBatch Class

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawBatches.cs`

Stores immutable sprite descriptions that share one image, sampler, and address mode.

```csharp
public sealed class DrawSpriteBatch
```

## Examples

```csharp
DrawSpriteBatch batch = new(
    atlas,
    [
        new DrawSprite2D(new DrawRect(0, 0, 32, 32)),
        new DrawSprite2D(new DrawRect(40, 0, 32, 32))
    ]);

drawing.DrawSpriteBatch(batch);
```

## Remarks

One batch uses exactly one platform-neutral image. Construction copies the sprite sequence and builds one textured triangle mesh. All sprites must use the same `Sampling` and `AddressMode`; cross-image grouping remains the caller's responsibility.

The batch does not own or dispose `Image`. Image changes participate in frame dependency tracking, while a new batch version changes retained identity and damage bounds.

## Constructors

| Name | Description |
| --- | --- |
| `DrawSpriteBatch(IDrawImage image, IEnumerable<DrawSprite2D> sprites)` | Copies a non-empty sprite sequence and validates the shared image and sampler settings. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Image` | `IDrawImage` | Gets the shared image or atlas. |
| `Sprites` | `IReadOnlyList<DrawSprite2D>` | Gets the copied sprite descriptions. |
| `Sampling` | `DrawSamplingMode` | Gets the common filtering mode. |
| `AddressMode` | `DrawAddressMode` | Gets the common texture addressing mode. |
| `Version` | `long` | Gets the stable immutable-payload version. |
| `Bounds` | `DrawRect` | Gets bounds for all transformed sprite corners. |

## Applies To

High-volume atlas drawing through one command and one primitive submission.

## See Also

- `DrawSprite2D`
- `DrawImageOptions`
