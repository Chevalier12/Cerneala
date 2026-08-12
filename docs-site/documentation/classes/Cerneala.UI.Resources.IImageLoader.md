# IImageLoader Interface
## Definition
Namespace: `Cerneala.UI.Resources`
Assembly/Project: `Cerneala`
Source: `UI/Resources/IImageLoader.cs`
Loads backend-specific draw images from file paths or readable streams.
```csharp
public interface IImageLoader
```

## Examples

```csharp
IDrawImage fileImage = loader.Load("Assets/logo.png");

using Stream stream = File.OpenRead("Assets/preview.png");
IDrawImage streamImage = loader.Load(stream);
```

## Remarks

Implementations must provide `Load(string)`. `Load(Stream)` has a default implementation that throws `NotSupportedException`, preserving path-only loaders while allowing controls such as `SvgImage` to provide rasterized image data without writing temporary files.

Implementations that support streams consume image bytes from the stream's current position and do not own the supplied stream unless their own documentation states otherwise.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Load(string path)` | `IDrawImage` | Loads an image from a path. |
| `Load(Stream stream)` | `IDrawImage` | Loads an image from a readable stream. The default implementation throws `NotSupportedException`. |

## Applies to
Cerneala UI runtime and framework API consumers.
