# ImageResource Class

## Definition
Namespace: `Cerneala.UI.Resources`

Assembly/Project: `Cerneala`

Source: `UI/Resources/ImageResource.cs`

Represents an image resource that either wraps an already available `IDrawImage` or stores a path that can be resolved later through an `IImageLoader`.

```csharp
public sealed class ImageResource
```

Inheritance:
`object` -> `ImageResource`

## Examples

Create a resource from an existing drawing image:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Resources;

IDrawImage drawImage = new MemoryImage(64, 32);
ImageResource resource = new(drawImage);

IDrawImage resolved = resource.Resolve();

file sealed class MemoryImage(int width, int height) : IDrawImage
{
    public int Width { get; } = width;
    public int Height { get; } = height;
}
```

Create a path-backed resource and resolve it with a loader:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Resources;

ImageResource resource = new("Assets/logo.png");
IImageLoader loader = new FixedImageLoader(new MemoryImage(64, 32));

IDrawImage resolved = resource.Resolve(loader);

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

`ImageResource` has two construction modes. `ImageResource(IDrawImage)` stores an embedded image instance and resolves without an image loader. `ImageResource(string)` stores a non-empty, non-whitespace path and requires an `IImageLoader` when resolved.

`Identity` is the path for path-backed resources. For embedded images, `Identity` is generated from the runtime hash code of the stored image and prefixed with `embedded:`.

Path-backed resources are commonly resolved through `ImageResourceCache`, which loads each identity once and reuses the resulting `IDrawImage`. Calling `Resolve()` directly on a path-backed resource without a loader throws `InvalidOperationException`.

## Constructors

| Name | Description |
| --- | --- |
| `ImageResource(IDrawImage image)` | Creates an embedded image resource. Throws `ArgumentNullException` when `image` is `null`. |
| `ImageResource(string path)` | Creates a path-backed image resource. Throws `ArgumentException` when `path` is `null`, empty, or whitespace. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Identity` | `string` | Gets the path for path-backed resources, or an `embedded:` identity based on the embedded image instance. |
| `IsPathBacked` | `bool` | Gets whether the resource was created from a path. |
| `Path` | `string?` | Gets the path for path-backed resources, or `null` for embedded images. |
| `HasEmbeddedImage` | `bool` | Gets whether the resource wraps an embedded `IDrawImage`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Resolve(IImageLoader? loader = null)` | `IDrawImage` | Returns the embedded image, or loads the path-backed image through `loader`. Throws `InvalidOperationException` when a path-backed resource has no loader or the loader returns `null`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `ImageResource(IDrawImage image)` | `ArgumentNullException` | `image` is `null`. |
| `ImageResource(string path)` | `ArgumentException` | `path` is `null`, empty, or whitespace. |
| `Resolve(IImageLoader? loader = null)` | `InvalidOperationException` | The resource is path-backed and no loader is supplied. |
| `Resolve(IImageLoader? loader = null)` | `InvalidOperationException` | The supplied loader returns `null`. |

## Applies to

Cerneala UI image resources used by controls such as `Cerneala.UI.Controls.Image` and by resource infrastructure such as `ResourceStore` and `ImageResourceCache`.

## See also

- `Cerneala.Drawing.IDrawImage`
- `Cerneala.UI.Resources.IImageLoader`
- `Cerneala.UI.Resources.ImageResourceCache`
- `Cerneala.UI.Controls.Image`
