# RenderTargetImage Class

## Definition
Namespace: `Cerneala.UI.Media`

Assembly/Project: `Cerneala`

Source: `UI/Media/RenderTargetImage.cs`

Represents an image source backed by an already available render target `IDrawImage`.

```csharp
public sealed record RenderTargetImage : ImageSource
```

Inheritance:
`object` -> `ImageSource` -> `RenderTargetImage`

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

IDrawImage renderTarget = new PreviewRenderTarget(128, 72);
RenderTargetImage source = new(
    "render-target://main",
    new LayoutSize(128, 72),
    renderTarget);

IDrawImage resolved = source.ResolveDrawImage();
LayoutSize intrinsicSize = source.IntrinsicSize;

sealed class PreviewRenderTarget(int width, int height) : IDrawImage
{
    public int Width { get; } = width;

    public int Height { get; } = height;
}
```

## Remarks

`RenderTargetImage` is a concrete `ImageSource` for render target images that are already materialized as an `IDrawImage`. Unlike `BitmapImage`, the backing image is required; passing `null` for `image` throws `ArgumentNullException`.

The inherited `Identity` and `IntrinsicSize` values are validated by `ImageSource`. `identity` must not be null, empty, or whitespace. `intrinsicSize` must use finite, non-negative width and height values.

`ResolveDrawImage()` always returns the non-null `Image` instance supplied to the constructor.

Equality keeps the record metadata behavior from `ImageSource`, but compares `Image` by reference identity. Two `RenderTargetImage` instances with the same identity, intrinsic size, and same `IDrawImage` reference are equal; two instances with equal-but-different draw image objects are not equal.

## Constructors

| Name | Description |
| --- | --- |
| `RenderTargetImage(string identity, LayoutSize intrinsicSize, IDrawImage image)` | Initializes a render target image source with an identity, intrinsic size, and required draw image. Throws `ArgumentException` for an invalid identity, `ArgumentOutOfRangeException` for an invalid intrinsic size, and `ArgumentNullException` when `image` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Image` | `IDrawImage` | Gets the backing draw image returned by `ResolveDrawImage()`. |
| `Identity` | `string` | Gets the inherited image source identity. |
| `IntrinsicSize` | `LayoutSize` | Gets the inherited intrinsic image size. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ResolveDrawImage()` | `IDrawImage` | Returns `Image`. |
| `Equals(RenderTargetImage? other)` | `bool` | Returns `true` when `other` has equal inherited metadata and the same `Image` reference, or when both references point to the same `RenderTargetImage` instance. |
| `GetHashCode()` | `int` | Combines the inherited image source hash with the reference-identity hash of `Image`. |

## Applies To

Cerneala retained UI media image sources in the `Cerneala` project.

## See Also

- `Cerneala.UI.Media.ImageSource`
- `Cerneala.UI.Media.BitmapImage`
- `Cerneala.Drawing.IDrawImage`
- `Cerneala.UI.Layout.LayoutSize`
- `UI/Media/RenderTargetImage.cs`
