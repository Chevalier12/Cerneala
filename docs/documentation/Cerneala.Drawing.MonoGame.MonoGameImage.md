# MonoGameImage Class

## Definition
Namespace: `Cerneala.Drawing.MonoGame`

Assembly/Project: `Cerneala`

Source: `UI/Drawing/MonoGame/MonoGameImage.cs`

Wraps a MonoGame `Texture2D` so it can be used as a Cerneala drawing image.

```csharp
public sealed class MonoGameImage : IDrawImage, IDisposable
```

Inheritance:
`object` -> `MonoGameImage`

Implements:
`Cerneala.Drawing.IDrawImage`, `System.IDisposable`

## Examples

```csharp
using Cerneala.Drawing.MonoGame;
using Microsoft.Xna.Framework.Graphics;

using FileStream stream = File.OpenRead("Assets/logo.png");
Texture2D texture = Texture2D.FromStream(graphicsDevice, stream);

using var image = new MonoGameImage(texture);

int width = image.Width;
int height = image.Height;
Texture2D monoGameTexture = image.Texture;
```

## Remarks

`MonoGameImage` is the MonoGame-backed implementation of `IDrawImage`. It exposes the wrapped `Texture2D` through `Texture` and reports image dimensions from `Texture.Width` and `Texture.Height`.

The constructor requires a non-null `Texture2D`; passing `null` throws `ArgumentNullException`. Disposing the `MonoGameImage` disposes the wrapped texture, so callers should treat the texture as owned by the image after construction.

`MonoGameDrawingBackend` requires draw image commands to use `MonoGameImage` instances when rendering through the MonoGame backend.

## Constructors

| Name | Description |
| --- | --- |
| `MonoGameImage(Texture2D texture)` | Creates an image wrapper for `texture`. Throws `ArgumentNullException` when `texture` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Texture` | `Texture2D` | Gets the wrapped MonoGame texture. |
| `Width` | `int` | Gets `Texture.Width`. Implements `IDrawImage.Width`. |
| `Height` | `int` | Gets `Texture.Height`. Implements `IDrawImage.Height`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Dispose()` | `void` | Disposes the wrapped `Texture2D`. |

## Applies to

Cerneala MonoGame drawing backend image resources.

## See also

- `Cerneala.Drawing.IDrawImage`
- `Cerneala.UI.Resources.MonoGame.MonoGameImageLoader`
- `Cerneala.Drawing.MonoGame.MonoGameDrawingBackend`
