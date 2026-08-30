# SvgImage Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/SvgImage.cs`

Displays an SVG document by rasterizing it into an image owned by the control.

```csharp
public class SvgImage : Image
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `Image` -> `SvgImage`

## Examples

Declare an SVG image in Cerneala markup:

```xml
<SvgImage
    SourcePath="Assets/logo.svg"
    Width="64"
    Height="64"
    UseIntrinsicSize="False" />
```

Create the same control from code:

```csharp
using Cerneala.UI.Controls;

SvgImage logo = new()
{
    SourcePath = "Assets/logo.svg",
    Width = 64,
    Height = 64,
    UseIntrinsicSize = false
};
```

## Remarks

`SvgImage` resolves relative paths against the current working directory first and then `AppContext.BaseDirectory`. When attached to a `UIRoot` that has an image loader and belongs to an effectively visible subtree, the drawing layer resolves the SVG raster and passes an in-memory PNG stream to the root `IImageLoader`. Controls in collapsed or otherwise non-renderable subtrees defer this work until the subtree becomes renderable.

If a compiled sidecar named `<source>.svg.cerneala.png` and its `.sha256` signature exist beside the SVG, Cerneala verifies the source hash, reads that artifact directly, and does not parse the SVG at runtime. `Tools/Cerneala.SvgAssetCompiler` creates both files from the same rasterizer used by the runtime. A missing or stale signature makes Cerneala ignore the sidecar and rasterize the current SVG, preventing stale visuals. No temporary runtime image file is created. Repeated uses of an unchanged path reuse cached raster bytes while each control still owns its backend image instance.

The configured image loader must support `IImageLoader.Load(Stream)`. The built-in `MonoGameImageLoader` supports both path-backed and stream-backed images. A loader that only implements `Load(string)` uses the interface default implementation and throws `NotSupportedException` for stream-backed images. Attaching to a root without an image loader is valid and leaves `Source` empty.

Changing `SourcePath` while the control is attached and effectively visible reloads the SVG immediately. When the control is not effectively visible, loading is deferred until it becomes renderable. The previous loaded image is disposed when it implements `IDisposable`. Detaching the control also releases its loaded image. An empty or `null` source path clears the image.

The SVG is rasterized at its document bounds. Inherited `Image` layout and rendering behavior then preserves that raster's aspect ratio. Set inherited `Width` and `Height` for a fixed display size, and set `UseIntrinsicSize` to `false` when the SVG's intrinsic dimensions should not determine desired layout size.

## Constructors

| Signature | Description |
| --- | --- |
| `SvgImage()` | Initializes an SVG image with `SourcePath` set to `null`. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `SourcePathProperty` | `UiProperty<string?>` | Identifies the `SourcePath` UI property. It affects measure and render. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `SourcePath` | `string?` | `null` | Gets or sets the SVG file path. A visible attached control reloads when this value changes; a non-renderable control defers loading. |

`SvgImage` also inherits `Source`, `UseIntrinsicSize`, resource properties, and layout properties from `Image` and `UIElement`.

## Applies To

Cerneala retained UI controls with an attached image loader.

## See Also

- `Cerneala.UI.Controls.Image`
- `Cerneala.UI.Resources.IImageLoader`
- `Cerneala.UI.Resources.MonoGame.MonoGameImageLoader`
