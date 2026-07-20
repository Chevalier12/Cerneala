# BackdropFrameMetadata Struct

## Definition
Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/BackdropFrameMetadata.cs`

Describes the immutable raster, color, coordinate, and version state of one borrowed backdrop frame.

```csharp
public readonly record struct BackdropFrameMetadata
```

## Examples

```csharp
using System.Numerics;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;

BackdropFrameMetadata metadata = new(
    PixelWidth: 1920,
    PixelHeight: 1080,
    PixelScale: 1,
    ColorProfile: PrismColorProfile.LinearSrgb,
    PixelFormat: BackdropPixelFormat.Rgba16Float,
    AlphaMode: BackdropAlphaMode.Premultiplied,
    CoordinateTransform: Matrix3x2.Identity,
    ContentVersion: 42);
```

## Remarks

`CoordinateTransform` maps host logical coordinates to source pixel coordinates. A provider must publish a `ContentVersion` that never decreases and must increment it whenever scene or lower-UI pixels, or pixel-affecting metadata, change. The WindowsDX source advances the version for every begun frame; retained Prism dependency tracking combines it with the UI and raster state used by the composition.

Metadata does not expose a graphics texture and does not imply ownership. It remains a value snapshot even though the corresponding `IBackdropFrameLease` is valid only for the drawing frame that acquired it.

## Constructors

| Name | Description |
| --- | --- |
| `BackdropFrameMetadata(int, int, float, PrismColorProfile, BackdropPixelFormat, BackdropAlphaMode, Matrix3x2, long)` | Creates validated immutable metadata for one backdrop frame. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `PixelWidth` | `int` | Gets the source width in physical pixels. |
| `PixelHeight` | `int` | Gets the source height in physical pixels. |
| `PixelScale` | `float` | Gets the source pixels per host logical unit. |
| `ColorProfile` | `PrismColorProfile` | Gets the declared source color profile. |
| `PixelFormat` | `BackdropPixelFormat` | Gets the backend-neutral pixel format. |
| `AlphaMode` | `BackdropAlphaMode` | Gets the source alpha encoding. |
| `CoordinateTransform` | `System.Numerics.Matrix3x2` | Gets the transform from host logical coordinates to source pixels. |
| `ContentVersion` | `long` | Gets the provider's monotonic pixel-content version. |

## Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentOutOfRangeException` | A dimension is not positive, the scale is not finite and positive, an enum value is invalid, the transform contains a non-finite value, or `ContentVersion` is negative. |

## Applies to

Cerneala backdrop frame providers and Prism dependency tracking.

## See also

- `Cerneala.Drawing.Prism.IBackdropFrameLease`
- `Cerneala.Drawing.Prism.IBackdropFrameSource`
