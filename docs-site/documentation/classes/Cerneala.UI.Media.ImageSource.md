# ImageSource Class

## Definition
Namespace: `Cerneala.UI.Media`

Assembly/Project: `Cerneala`

Source: `UI/Media/ImageSource.cs`

Defines the abstract base record for image sources that expose an identity, an intrinsic layout size, and an optional resolved draw image.

```csharp
public abstract record ImageSource
```

Inheritance:
`object` -> `ImageSource`

Derived:
`BitmapImage`, `RenderTargetImage`

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

IDrawImage image = new PreviewImage(64, 32);
ImageSource source = new BitmapImage("asset://logo", new LayoutSize(64, 32), image);

string identity = source.Identity;
LayoutSize intrinsicSize = source.IntrinsicSize;
IDrawImage? resolved = source.ResolveDrawImage();

sealed class PreviewImage(int width, int height) : IDrawImage
{
    public int Width { get; } = width;

    public int Height { get; } = height;
}
```

```csharp
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

ImageSource pending = new BitmapImage("asset://pending-logo", new LayoutSize(128, 64));

// A source can carry metadata before the backing draw image is available.
bool hasImage = pending.ResolveDrawImage() is not null;
```

## Remarks

`ImageSource` carries metadata shared by concrete image source types. `Identity` identifies the source, and `IntrinsicSize` records the natural layout size associated with that source.

The protected constructor validates shared metadata for derived types. `identity` must not be null, empty, or whitespace. `intrinsicSize` must have finite, non-negative width and height values; unconstrained, infinite, `NaN`, or negative dimensions are rejected.

`ResolveDrawImage()` is implemented by derived types. It may return `null` when a source is valid but the backing `IDrawImage` has not been resolved yet, as `BitmapImage` can do for pending assets. Derived types can also require a non-null image, as `RenderTargetImage` does.

Because `ImageSource` is a record, equality for the base metadata is record-based. Concrete image source types may add their own equality behavior for their backing image data.

## Constructors

| Name | Description |
| --- | --- |
| `protected ImageSource(string identity, LayoutSize intrinsicSize)` | Initializes the shared image source metadata for a derived type. Throws `ArgumentException` when `identity` is null, empty, or whitespace, and `ArgumentOutOfRangeException` when `intrinsicSize` is not finite and non-negative. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Identity` | `string` | Gets the source identity supplied by the derived image source. |
| `IntrinsicSize` | `LayoutSize` | Gets the finite, non-negative natural size associated with the image source. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ResolveDrawImage()` | `IDrawImage?` | When implemented by a derived type, returns the resolved draw image, or `null` if no draw image is currently available. |

## Applies To

Cerneala retained UI media image sources in the `Cerneala` project.

## See Also

- `Cerneala.UI.Media.BitmapImage`
- `Cerneala.UI.Media.RenderTargetImage`
- `Cerneala.Drawing.IDrawImage`
- `Cerneala.UI.Layout.LayoutSize`
- `UI/Media/ImageSource.cs`
