# Image Class

## Definition
Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/Image.cs`

Displays an `IDrawImage` directly or through an `ImageResource`, preserving the image aspect ratio when rendered.

```csharp
public class Image : Control
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `Image`

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;

Image image = new()
{
    Source = new PreviewImage(64, 32),
    UseIntrinsicSize = true
};

LayoutSize desiredSize = image.Measure(new MeasureContext(new LayoutSize(200, 200)));

private sealed class PreviewImage(int width, int height) : IDrawImage
{
    public int Width { get; } = width;

    public int Height { get; } = height;
}
```

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Resources;

ResourceId<ImageResource> logoId = new("app.logo");
ResourceStore resources = new();
resources.SetResource(logoId, new ImageResource(new PreviewImage(128, 64)));

Image logo = new()
{
    ResourceProvider = resources,
    SourceResourceId = logoId
};

private sealed class PreviewImage(int width, int height) : IDrawImage
{
    public int Width { get; } = width;

    public int Height { get; } = height;
}
```

## Remarks

`Image` can resolve its bitmap from `Source` or from `SourceResourceId`. When `SourceResourceId` is set, resource resolution takes precedence over `Source`. The control first uses its local `ResourceProvider`, then the root resource provider if the control is attached to a `UIRoot`.

When `UseIntrinsicSize` is `true`, measuring returns the resolved image width and height. When it is `false`, measuring returns `LayoutSize.Zero`, while rendering can still draw into the arranged bounds. Source changes and intrinsic-size mode changes invalidate measure and render as appropriate.

Rendering skips null sources and non-positive render bounds. For a valid source, the rendered destination rectangle is centered inside the arranged bounds and scaled uniformly with `MathF.Min`, so the original aspect ratio is preserved instead of stretched. The inherited `Foreground` color is passed to the drawing context when drawing the image.

Resource-backed images participate in resource render dependencies. If a `ResourceDependencyTracker` is available locally or through the root, the control records dependency effects; intrinsic-size mode decides whether resource changes affect both measure and render or render only. If an `ImageResource` is path-backed and no root image cache or loader is available, resolving that resource can throw from `ImageResource.Resolve`.

`SourceProperty` uses reference equality for `IDrawImage` values. Replacing an image with another instance that compares equal still counts as a value change, which keeps intrinsic measurement and rendering in sync with the actual image instance.

## Constructors

| Name | Description |
| --- | --- |
| `Image()` | Initializes a new image control with `Source` set to `null`, no resource id, no local resource provider, and `UseIntrinsicSize` set to `true`. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `SourceProperty` | `UiProperty<IDrawImage?>` | Identifies the `Source` UI property. The property defaults to `null`, affects measure and render, and compares values by reference. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Source` | `IDrawImage?` | `null` | Gets or sets the direct draw image. Used only when `SourceResourceId` is `null`. |
| `SourceResourceId` | `ResourceId<ImageResource>?` | `null` | Gets or sets the image resource id to resolve through a resource provider. Changing it invalidates render and, when intrinsic sizing is enabled, measure. |
| `UseIntrinsicSize` | `bool` | `true` | Gets or sets whether measure should report the resolved image intrinsic size. Changing it invalidates measure and render. |
| `ResourceProvider` | `IResourceProvider?` | `null` | Gets or sets the local resource provider used before the root provider. Changing it invalidates measure and render. |
| `ResourceDependencyTracker` | `ResourceDependencyTracker?` | `null` | Gets or sets the local resource dependency tracker used before the root tracker. Setting this property does not invalidate layout or rendering by itself. |

## Methods

`Image` does not declare public methods. It overrides protected layout and rendering hooks from `Control`/`UIElement`.

## Applies To

Cerneala retained UI controls in the `Cerneala` project.

## See Also

- `UI/Controls/Image.cs`
- `UI/Resources/ImageResource.cs`
- `UI/Resources/ResourceId{T}.cs`
- `Drawing/IDrawImage.cs`
