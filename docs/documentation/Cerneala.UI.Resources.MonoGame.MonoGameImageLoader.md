# MonoGameImageLoader Class

## Definition
Namespace: `Cerneala.UI.Resources.MonoGame`

Assembly/Project: `Cerneala`

Source: `UI/Resources/MonoGame/MonoGameImageLoader.cs`

Loads image files into MonoGame-backed `IDrawImage` instances.

```csharp
public sealed class MonoGameImageLoader : IImageLoader
```

Inheritance:
`object` -> `MonoGameImageLoader`

Implements:
`IImageLoader`

## Examples

Load an image file through a MonoGame graphics device:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Resources.MonoGame;
using Microsoft.Xna.Framework.Graphics;

GraphicsDevice graphicsDevice = GetGraphicsDevice();
MonoGameImageLoader loader = new(graphicsDevice);

IDrawImage image = loader.Load("Assets/logo.png");
```

## Remarks

`MonoGameImageLoader` uses `Texture2D.FromStream` with the supplied `GraphicsDevice`, then wraps the created texture in a `MonoGameImage`.

The constructor throws `ArgumentNullException` when `graphicsDevice` is `null`. `Load` throws `ArgumentException` when `path` is `null`, empty, or whitespace. It opens the file with `File.OpenRead`, so file system exceptions can surface when the path cannot be read.

## Constructors

| Signature | Description |
| --- | --- |
| `MonoGameImageLoader(GraphicsDevice graphicsDevice)` | Initializes the loader with the graphics device used to create textures. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Load(string path)` | `IDrawImage` | Loads an image file from disk and returns a MonoGame-backed draw image. |

## Applies To

Cerneala MonoGame UI resource loading.

## See Also

- `Cerneala.UI.Resources.IImageLoader`
- `Cerneala.Drawing.MonoGame.MonoGameImage`
- `Cerneala.Drawing.IDrawImage`
