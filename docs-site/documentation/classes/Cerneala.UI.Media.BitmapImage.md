# BitmapImage Class

## Definition
Namespace: `Cerneala.UI.Media`

Assembly/Project: `Cerneala`

Source: `UI/Media/BitmapImage.cs`

Represents an image source identified by metadata and optionally backed by an `IDrawImage` instance.

```csharp
public sealed record BitmapImage : ImageSource
```

Inheritance:
`object` -> `ImageSource` -> `BitmapImage`

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

IDrawImage image = new PreviewImage(64, 32);
BitmapImage source = new("asset://logo", new LayoutSize(64, 32), image);

IDrawImage? resolved = source.ResolveDrawImage();
LayoutSize intrinsicSize = source.IntrinsicSize;

sealed class PreviewImage(int width, int height) : IDrawImage
{
    public int Width { get; } = width;

    public int Height { get; } = height;
}
```

```csharp
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

BitmapImage pending = new("asset://pending-logo", new LayoutSize(64, 32));

// The source can carry identity and intrinsic size before a draw image is available.
bool isResolved = pending.ResolveDrawImage() is not null;
```

## Remarks

`BitmapImage` is a concrete `ImageSource` for bitmap-like assets. It stores the inherited `Identity` and `IntrinsicSize` metadata and returns its `Image` value from `ResolveDrawImage()`.

The `image` constructor argument is optional. When it is omitted or `null`, `ResolveDrawImage()` returns `null`, allowing a caller to represent an image source whose backing draw image has not been loaded yet.

The base `ImageSource` constructor validates `identity` and `intrinsicSize`. `identity` must not be null, empty, or whitespace. `intrinsicSize` must use finite, non-negative width and height values.

Equality keeps the record metadata behavior from `ImageSource`, but compares `Image` by reference identity. Two `BitmapImage` instances with the same identity, intrinsic size, and same `IDrawImage` reference are equal; two instances with equal-but-different draw image objects are not equal.

## Constructors

| Name | Description |
| --- | --- |
| `BitmapImage(string identity, LayoutSize intrinsicSize, IDrawImage? image = null)` | Initializes a bitmap image source with an identity, intrinsic size, and optional draw image. Throws `ArgumentException` for an invalid identity and `ArgumentOutOfRangeException` for an invalid intrinsic size. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Image` | `IDrawImage?` | Gets the optional backing draw image returned by `ResolveDrawImage()`. |
| `Identity` | `string` | Gets the inherited image source identity. |
| `IntrinsicSize` | `LayoutSize` | Gets the inherited intrinsic image size. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ResolveDrawImage()` | `IDrawImage?` | Returns `Image`. |
| `Equals(BitmapImage? other)` | `bool` | Returns `true` when `other` has equal inherited metadata and the same `Image` reference, or when both references point to the same `BitmapImage` instance. |
| `GetHashCode()` | `int` | Combines the inherited image source hash with the reference-identity hash of `Image`; null images contribute `0`. |

## Applies To

Cerneala retained UI media image sources in the `Cerneala` project.

## See Also

- `Cerneala.UI.Media.ImageSource`
- `Cerneala.Drawing.IDrawImage`
- `Cerneala.UI.Layout.LayoutSize`
- `UI/Media/BitmapImage.cs`
