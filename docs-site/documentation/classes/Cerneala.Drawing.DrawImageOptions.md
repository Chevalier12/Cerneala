# DrawImageOptions Class

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawImageOptions.cs`

Describes source selection, appearance, placement, and sampling for an image command.

```csharp
public sealed record DrawImageOptions
```

## Examples

```csharp
DrawImageOptions options = new(
    source: new DrawRect(32, 0, 16, 16),
    tint: Color.White,
    opacity: 0.8f,
    rotation: MathF.PI / 4,
    origin: new DrawPoint(8, 8),
    flip: DrawImageFlip.Horizontal,
    layerDepth: 0.25f,
    sampling: DrawSamplingMode.Point,
    addressMode: DrawAddressMode.Clamp);

drawing.DrawImage(image, new DrawRect(40, 24, 64, 64), options);
```

## Remarks

`Source` and `Origin` use source-image pixel units. A null source selects the complete image. `Rotation` is measured in radians in the drawing coordinate system. `Opacity` is multiplied into the tint alpha, while `LayerDepth` participates in image ordering.

The source region must have positive dimensions and remain inside the image when the command is created. Sampling and addressing select cached backend states; they do not expose platform graphics types.

Sampling accepts only `Point` or `Linear`, and addressing accepts only `Clamp` or `Wrap`. Other numeric enum values throw `ArgumentOutOfRangeException`. These value-domain checks do not rebuild runtime enum metadata after garbage collection; constructing options still creates the immutable options object.

### Point and Clamp image edges on SDL GPU

For image-generated geometry recorded with `Point` sampling and `Clamp` addressing, the current SDL_GPU path uses ordinary interpolated texture coordinates when a covered pixel's center is strictly inside the logical image domain. This includes internal triangulation diagonals and nine-slice cell edges. A covered pixel whose center is exactly on the *external* image boundary, or outside it, uses a covered interpolation coordinate instead. This addresses fractional and exact external destination-edge sampling without shrinking `Source`, shifting its UVs by half a texel, or changing ordinary nearest-texel selection for true interior centers.

This treatment applies to `DrawImage`, each sprite quad in `DrawSpriteBatch`, the four-position `DrawImageQuad` overload that takes `DrawImageOptions`, and the *whole outer image domain* of `DrawNineSlice` rather than each internal slice cell. The explicit-vertex `DrawImageQuad` overload and arbitrary `DrawMesh`/`DrawTriangles` geometry are not selected; separate compositing passes are unchanged. `Linear` or `Wrap` keeps its existing sampling path. Fractional and subtexel source rectangles are not rounded or inset; true interior centers retain their ordinary nearest-texel choice. An exact tie at a source boundary may still choose an adjacent atlas texel. The source rectangle is not a universal texel-isolation clamp, and this SDL_GPU edge behavior is not a promise for every backend or render path.

## Constructors

| Name | Description |
| --- | --- |
| `DrawImageOptions(DrawRect? source = null, Color? tint = null, float opacity = 1, float rotation = 0, DrawPoint origin = default, DrawImageFlip flip = None, float layerDepth = 0, DrawSamplingMode sampling = Linear, DrawAddressMode addressMode = Clamp)` | Creates validated immutable image options. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Source` | `DrawRect?` | Gets the source-image pixel rectangle, or `null` for the complete image. |
| `Tint` | `Color` | Gets the image tint; the default is white. |
| `Opacity` | `float` | Gets opacity from `0` through `1`. |
| `Rotation` | `float` | Gets rotation in radians. |
| `Origin` | `DrawPoint` | Gets the placement and rotation origin in source-image pixels. |
| `Flip` | `DrawImageFlip` | Gets horizontal and vertical mirroring flags. |
| `LayerDepth` | `float` | Gets image depth from `0` through `1`. |
| `Sampling` | `DrawSamplingMode` | Gets the texture filtering mode. |
| `AddressMode` | `DrawAddressMode` | Gets the texture coordinate addressing mode. |

## Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentOutOfRangeException` | Source dimensions are not positive; opacity, rotation, or layer depth is invalid; or an enum/flip value is unsupported. |

## Applies To

Image, image-quad, nine-slice, and sprite-batch commands.

## See Also

- `DrawingContext.DrawImage`
- `DrawImageFlip`
- `DrawSamplingMode`
- `DrawAddressMode`
