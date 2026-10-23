# Image Class

## Definition
Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/Image.cs`

Displays an `IDrawImage` directly or prepares a path-backed `ImageResource` asynchronously, and sizes the picture by `Stretch` and `StretchDirection`.

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

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;

// picture is a 200 x 200 IDrawImage.
Image banner = new()
{
    Source = picture,
    Width = 400,
    Height = 100,
    Stretch = DrawBrushStretch.UniformToFill
};
// The picture is drawn 400 x 400 at (0, 0) and clipped to the 400 x 100 box: the top band is visible.
// With VerticalAlignment = VerticalAlignment.Center it is drawn at (0, -150): the middle band is visible.
```

Markup:

```xml
<Image Width="400" Height="100" Stretch="UniformToFill" StretchDirection="Both" />
```

## Remarks

`Image` can resolve its bitmap from `Source` or from `SourceResourceId`. When `SourceResourceId` is set, resource resolution takes precedence over `Source`. An explicit `ResourceProvider` is used exclusively. Without one, lookup follows the element's resource dictionaries through logical/visual parents, then the root resource provider. A nearer dictionary entry shadows an outer entry even when it has the wrong resource type.

### Asynchronous presentation

For a path-backed resource, the control acquires preparation interest in the root's shared [ImageResourceCache](Cerneala.UI.Resources.ImageResourceCache.md). A cold path requires [IAsyncImageLoader](Cerneala.UI.Resources.IAsyncImageLoader.md). The control never calls the synchronous decoder for a cold path and never waits for another consumer's pending atlas load. Direct, embedded and already-resident images can be ready immediately. There is no separate UI image cache or implicit worker wrapper for a synchronous-only loader.

While preparation is pending, `LoadingState` is `Loading`, `LoadingError` is `null`, and no bitmap is recorded. On failure, the bitmap remains omitted, `LoadingState` becomes `Error`, and `LoadingError` contains the failure. A missing loader/cache, or a synchronous-only loader for a cold path, is reported through this state rather than decoded synchronously. Null sources and unresolved resource ids are empty `Ready` images, not loading failures.

State describes the last processed source resolution, not a promise that a newly assigned source has already been processed. The initial state is `Ready`. Completion schedules invalidation through the root's UI relay; state and layout are updated on the UI thread during subsequent processing. Observe the readonly UI properties through bindings or the inherited `PropertyChanged` event. Detached controls are not updated by an old completion, and their last observed state is not automatically reset.

The control remains attached and participates in ordinary layout and input while loading. This is asynchronous image presentation, not UI-control virtualization. Reassigning the source or detaching during state notifications must not cause layout or rendering to use an acquisition released by that notification.

### Layout and rendering

`Stretch` and `StretchDirection` decide the picture size, as in WPF. From the available size (when measuring) or the arranged size (when rendering) the control computes a horizontal and a vertical scale factor for the natural image size:

| `Stretch` | Scale factors | 200 x 200 picture in a 400 x 100 area |
| --- | --- | --- |
| `None` | Both `1`. | 200 x 200 |
| `Fill` | Width and height scaled separately; the aspect ratio can change. | 400 x 100 |
| `Uniform` (default) | The smaller of the two factors for both axes; the picture fits inside. | 100 x 100 |
| `UniformToFill` | The larger of the two factors for both axes; the picture covers the area and the overflow is clipped. | 400 x 400, clipped to 400 x 100 |

When only one axis of the available size is finite, as in a vertical `StackPanel`, the factor of that axis is used for both axes. When neither axis is finite, both factors are `1`. `StretchDirection` then limits each factor: `UpOnly` raises a factor below `1` to `1`, and `DownOnly` lowers a factor above `1` to `1`. For example, a 50 x 50 picture with `Stretch = Uniform` and `StretchDirection = DownOnly` stays 50 x 50 in a 400 x 100 area.

When `UseIntrinsicSize` is `true`, the desired size is the ready image size multiplied by these factors, or zero while loading, on error, or without an image. An `Image` without `Width` and `Height` therefore takes the size that `Stretch` gives: a 200 x 200 picture with the default `Uniform` measures 100 x 100 in a 400 x 100 area and 400 x 400 in a vertical `StackPanel` 400 wide. The desired size can exceed the available size (`UniformToFill`, or `None` with a large picture). The layout system applies explicit `Width` and `Height` independently: declaring both reserves that space while loading; an omitted axis acquires its size when the bitmap becomes ready and may cause relayout. The control does not invent natural dimensions or keep the previous image as a placeholder.

When `UseIntrinsicSize` is `false`, the desired size remains `LayoutSize.Zero` for every `Stretch`, while explicit dimensions and arranged bounds still apply. Completion invalidates render only in this mode; it also invalidates measure when intrinsic sizing is currently enabled. Source, `Stretch`, `StretchDirection` and intrinsic-size mode changes invalidate measure and render as appropriate.

Rendering skips null sources and non-positive render bounds. For a valid source, the picture is placed inside the arranged bounds by the control's own `HorizontalAlignment` and `VerticalAlignment`, as WPF places an element in its layout slot: `Left`/`Top` start at the bounds edge, `Right`/`Bottom` end at the opposite edge, and `Center` centers the picture. `Stretch` alignment centers a picture that fits on that axis and starts a larger picture at the left or top edge. A picture larger than the bounds is clipped to the bounds; a picture that fits records no clip. Images are drawn with an opaque white tint; `Foreground` is reserved for foreground content such as text and no longer tints image pixels.

### Resource lifetime and recovery

Resource-backed images participate in resource render dependencies. When the image comes from an explicit `ResourceProvider` or from the root provider, and a `ResourceDependencyTracker` is available locally or through the root, the control records dependency effects. An image found in an ancestor element's `Resources` records no tracker dependency; it is versioned by that dictionary's `Version` instead. Intrinsic-size mode decides whether resource changes affect both measure and render or render only.

Changing the resource id/provider or detaching releases the control's acquisition. Other consumers and retained drawing commands keep their own acquisitions; only the last outstanding interest cancels a pending load or releases an owned bitmap. A late abandoned load cannot publish into a replacement source. Direct and embedded images remain caller-owned.

A failed preparation is retained as an observable error, not retried every frame. A different path identity, a direct/embedded image or a replacement loader/cache can supply a new result. Re-registering the same path does not by itself forget a shared failed cache entry. To retry that path with the same resource id, call `root.ImageResourceCache.Remove(resource)` and invalidate the control with `InvalidationFlags.Measure | InvalidationFlags.Render`; forgetting a cache entry alone does not schedule a UI frame. Applications choose when to retry. Explicit application calls to `ImageResourceCache.Acquire` remain synchronous.

`SourceProperty` uses reference equality for `IDrawImage` values. Replacing an image with another instance that compares equal still counts as a value change, which keeps intrinsic measurement and rendering in sync with the actual image instance.

## Constructors

| Name | Description |
| --- | --- |
| `Image()` | Initializes an empty `Ready` image with no loading error, no resource id/provider, `Stretch` set to `Uniform`, `StretchDirection` set to `Both`, and `UseIntrinsicSize` set to `true`. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `SourceProperty` | `UiProperty<IDrawImage?>` | Identifies the `Source` UI property. The property defaults to `null`, affects measure and render, and compares values by reference. |
| `StretchProperty` | `UiProperty<DrawBrushStretch>` | Identifies the `Stretch` UI property. Defaults to `Uniform` and affects measure and render. |
| `StretchDirectionProperty` | `UiProperty<StretchDirection>` | Identifies the `StretchDirection` UI property. Defaults to `Both` and affects measure and render. |
| `LoadingStateProperty` | `UiProperty<ImageLoadingState>` | Identifies the readonly loading-state UI property. Defaults to `Ready`. |
| `LoadingErrorProperty` | `UiProperty<Exception?>` | Identifies the readonly loading-error UI property. Defaults to `null`. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Source` | `IDrawImage?` | `null` | Gets or sets the direct draw image. Used only when `SourceResourceId` is `null`. |
| `SourceResourceId` | `ResourceId<ImageResource>?` | `null` | Gets or sets the image resource id to resolve through a resource provider. Changing it invalidates render and, when intrinsic sizing is enabled, measure. |
| `Stretch` | `DrawBrushStretch` | `Uniform` | Gets or sets how the picture fills the control: `None`, `Fill`, `Uniform` or `UniformToFill`. Used by measure and render. |
| `StretchDirection` | `StretchDirection` | `Both` | Gets or sets whether `Stretch` may enlarge the picture, shrink it, or both. Used by measure and render. |
| `UseIntrinsicSize` | `bool` | `true` | Gets or sets whether measure should report the resolved image size scaled by `Stretch` and `StretchDirection`. Changing it invalidates measure and render. |
| `ResourceProvider` | `IResourceProvider?` | `null` | Gets or sets an explicit resource provider, used exclusively when assigned. Changing it invalidates measure and render. |
| `ResourceDependencyTracker` | `ResourceDependencyTracker?` | `null` | Gets or sets the local resource dependency tracker used before the root tracker. Setting this property does not invalidate layout or rendering by itself. |
| `LoadingState` | `ImageLoadingState` | `Ready` | Gets the last processed source state: `Loading`, `Ready` or `Error`. `Ready` also includes empty sources. |
| `LoadingError` | `Exception?` | `null` | Gets the failure from the last processed source resolution, or `null` when it has no failure. |

## Methods

`Image` does not declare public methods. It overrides protected layout and rendering hooks from `Control`/`UIElement`.

## Applies To

Cerneala retained UI controls in the `Cerneala` project.

## See Also

- [ImageLoadingState](Cerneala.UI.Controls.ImageLoadingState.md)
- [StretchDirection](Cerneala.UI.Controls.StretchDirection.md)
- [DrawBrushStretch](Cerneala.Drawing.DrawBrushStretch.md)
- [ImageResource](Cerneala.UI.Resources.ImageResource.md)
- [ImageResourceCache](Cerneala.UI.Resources.ImageResourceCache.md)
- [IAsyncImageLoader](Cerneala.UI.Resources.IAsyncImageLoader.md)
