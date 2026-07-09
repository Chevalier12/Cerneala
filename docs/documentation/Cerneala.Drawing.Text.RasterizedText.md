# RasterizedText Class

## Definition
Namespace: `Cerneala.Drawing.Text`

Assembly/Project: `Cerneala`

Source: `UI/Drawing/Text/RasterizedText.cs`

Stores the rasterized RGBA pixel buffer and shaping metadata produced by the text rasterization pipeline.

```csharp
public sealed class RasterizedText
```

Inheritance:
`object` -> `RasterizedText`

## Examples
Create a one-pixel transparent rasterized text result:

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Text;

TextShapeResult shapeResult = new(string.Empty, 0);
byte[] pixels = new byte[] { 0, 0, 0, 0 };

RasterizedText text = new(
    width: 1,
    height: 1,
    rgbaPixels: pixels,
    shapeResult: shapeResult,
    originOffset: new DrawPoint(0, 0));

byte[] textureData = text.RgbaPixels;
```

## Remarks
`RasterizedText` is an immutable container for text pixels after rasterization. `Width` and `Height` describe the pixel dimensions of the buffer, and `RgbaPixels` returns RGBA data whose length is exactly `Width * Height * 4`.

The constructor defensively copies the supplied pixel buffer. The `RgbaPixels` property also returns a copy, so changing the original array or a returned array does not mutate the stored rasterized text.

`ShapeResult` keeps the shaping result associated with the rasterized pixels. `OriginOffset` stores the finite drawing offset associated with the pixel buffer; the overload without `originOffset` uses the default `DrawPoint` value.

`SkiaTextRasterizer` creates `RasterizedText` instances from shaped text. For empty shaped text, it returns a `1` by `1` transparent pixel buffer. For non-empty text, it uses Skia bounds and any trimmed transparent left columns to populate `OriginOffset`.

## Constructors
| Name | Description |
| --- | --- |
| `RasterizedText(int width, int height, byte[] rgbaPixels, TextShapeResult shapeResult)` | Initializes a rasterized text buffer with the default origin offset. |
| `RasterizedText(int width, int height, byte[] rgbaPixels, TextShapeResult shapeResult, DrawPoint originOffset)` | Initializes a rasterized text buffer with an explicit origin offset. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Width` | `int` | Gets the positive width of the rasterized pixel buffer, in pixels. |
| `Height` | `int` | Gets the positive height of the rasterized pixel buffer, in pixels. |
| `OriginOffset` | `DrawPoint` | Gets the finite offset associated with the rasterized pixel buffer. |
| `RgbaPixels` | `byte[]` | Gets a defensive copy of the RGBA pixel buffer. |
| `ShapeResult` | `TextShapeResult` | Gets the text shaping result associated with the rasterized pixels. |

## Exceptions
| Exception | Condition |
| --- | --- |
| `ArgumentOutOfRangeException` | `width` or `height` is less than or equal to `0`. |
| `ArgumentOutOfRangeException` | `originOffset.X` or `originOffset.Y` is not finite. |
| `ArgumentOutOfRangeException` | `width * height * 4` is larger than `int.MaxValue`. |
| `ArgumentNullException` | `rgbaPixels` is `null`. |
| `ArgumentException` | `rgbaPixels.Length` does not equal `width * height * 4`. |

## Applies to
Cerneala drawing text rasterization and the MonoGame text texture path.

## See also
- `Cerneala.Drawing.Text.SkiaTextRasterizer`
- `Cerneala.Drawing.Text.TextShapeResult`
- `Cerneala.Drawing.DrawPoint`
