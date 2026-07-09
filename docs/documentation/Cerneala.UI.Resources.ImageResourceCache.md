# ImageResourceCache Class

## Definition
Namespace: `Cerneala.UI.Resources`

Assembly/Project: `Cerneala`

Source: `UI/Resources/ImageResourceCache.cs`

Caches path-backed `ImageResource` instances after loading them through an `IImageLoader`.

```csharp
public sealed class ImageResourceCache : IDisposable
```

Inheritance:
`object` -> `ImageResourceCache`

Implements:
`IDisposable`

## Examples

Resolve the same path-backed image more than once while loading it only once:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Resources;

IImageLoader loader = new FixedImageLoader(new MemoryImage(64, 32));
ImageResourceCache cache = new(loader);
ImageResource logo = new("Assets/logo.png");

IDrawImage first = cache.Resolve(logo);
IDrawImage second = cache.Resolve(new ImageResource("Assets/logo.png"));

Console.WriteLine(ReferenceEquals(first, second)); // True
Console.WriteLine(cache.LoadCount); // 1

file sealed class FixedImageLoader(IDrawImage image) : IImageLoader
{
    public IDrawImage Load(string path) => image;
}

file sealed class MemoryImage(int width, int height) : IDrawImage
{
    public int Width { get; } = width;
    public int Height { get; } = height;
}
```

Clear cached images when the owning content services or root is replaced:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Resources;

ImageResourceCache cache = new(new FixedImageLoader(new MemoryImage(64, 32)));
cache.Resolve(new ImageResource("Assets/logo.png"));

cache.Clear();
cache.Dispose();

file sealed class FixedImageLoader(IDrawImage image) : IImageLoader
{
    public IDrawImage Load(string path) => image;
}

file sealed class MemoryImage(int width, int height) : IDrawImage
{
    public int Width { get; } = width;
    public int Height { get; } = height;
}
```

## Remarks

`ImageResourceCache` is used by `UIRoot` and `Image` controls to share loaded path-backed images across measure and render passes. A path-backed resource is cached by its `ImageResource.Identity`, which is the resource path for path-backed images. Repeated calls to `Resolve` for the same identity return the cached `IDrawImage` and do not call the loader again.

Embedded image resources are returned directly from the `ImageResource` and are not stored in the cache. Because embedded images are not cache-owned, `Clear`, `Remove`, and `Dispose` do not dispose them.

The cache requires an `IImageLoader` to resolve path-backed resources. If the cache was constructed without a loader, resolving a path-backed resource throws `InvalidOperationException`. `LoadCount` increments only when a path-backed resource is loaded and added to the cache.

`Clear`, `Remove`, and `Dispose` dispose cached images that implement `IDisposable`. `Dispose` delegates to `Clear`, so disposing an already-cleared cache is safe.

## Constructors

| Name | Description |
| --- | --- |
| `ImageResourceCache(IImageLoader? loader)` | Initializes a cache that uses `loader` for path-backed image resources. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `LoadCount` | `int` | Gets the number of path-backed images loaded through the cache. Cached hits and embedded images do not increment this value. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Resolve(ImageResource resource)` | `IDrawImage` | Returns an embedded image directly, returns a cached path-backed image when available, or loads and caches the path-backed image through the configured loader. |
| `Remove(ImageResource resource)` | `void` | Removes the cached image for `resource.Identity` and disposes it if the cached image implements `IDisposable`. |
| `Clear()` | `void` | Removes all cached path-backed images and disposes cached images that implement `IDisposable`. |
| `Dispose()` | `void` | Clears the cache and releases disposable cached images. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Resolve(ImageResource resource)` | `ArgumentNullException` | `resource` is `null`. |
| `Resolve(ImageResource resource)` | `InvalidOperationException` | `resource` is not embedded and is not path-backed. |
| `Resolve(ImageResource resource)` | `InvalidOperationException` | `resource` is path-backed and the cache has no loader. |
| `Remove(ImageResource resource)` | `ArgumentNullException` | `resource` is `null`. |

## Applies to

`Cerneala.UI.Resources` in the `Cerneala` project.

## See also

- `Cerneala.UI.Resources.ImageResource`
- `Cerneala.UI.Resources.IImageLoader`
- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Controls.Image`
- `Cerneala.UI.Hosting.MonoGame.MonoGameContentServices`
