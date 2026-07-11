# MonoGameDrawingBackend Class

## Definition

Namespace: `Cerneala.Drawing.MonoGame`

Assembly/Project: `Cerneala`

Source: `Drawing/MonoGame/MonoGameDrawingBackend.cs`

Renders `DrawCommandList` instances through a MonoGame `SpriteBatch`.

```csharp
public sealed class MonoGameDrawingBackend : IDrawingBackend, IDisposable
```

Inheritance:
`object` -> `MonoGameDrawingBackend`

Implements:
`IDrawingBackend`, `IDisposable`

## Examples

The backend is expected to render inside a `SpriteBatch.Begin`/`End` pair. Use
`ScissorRasterizerState` when command lists can contain clip commands.

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

Texture2D whitePixel = new(graphicsDevice, 1, 1);
whitePixel.SetData(new[] { Color.White });

using SpriteBatch spriteBatch = new(graphicsDevice);
using MonoGameDrawingBackend backend = new(spriteBatch, whitePixel);

DrawCommandList commands = new();
commands.Add(DrawCommand.PushClip(new DrawRect(0, 0, 200, 120)));
commands.Add(DrawCommand.FillRectangle(new DrawRect(10, 10, 80, 40), new Color(32, 120, 220, 255)));
commands.Add(DrawCommand.DrawRectangle(new DrawRect(10, 10, 80, 40), new Color(255, 255, 255, 255), 2));
commands.Add(DrawCommand.PopClip());

spriteBatch.Begin(
    sortMode: SpriteSortMode.Immediate,
    rasterizerState: MonoGameDrawingBackend.ScissorRasterizerState);

try
{
    backend.Render(commands);
}
finally
{
    spriteBatch.End();
}
```

## Remarks

`MonoGameDrawingBackend` maps Cerneala drawing commands to MonoGame drawing
operations. It supports filled and stroked rectangles, filled and stroked
ellipses, lines, images, text, and push/pop clip commands.

The backend treats the submitted command list as read-only while rendering. It
does not call `SpriteBatch.Begin` or `SpriteBatch.End`; callers own the
surrounding MonoGame batch lifetime.

Clipping uses `GraphicsDevice.ScissorRectangle`. During `Render`, the backend
creates a clip stack from the current viewport, applies clip commands, and
restores the previous scissor rectangle in a `finally` block.

`CoordinateScale` converts logical coordinates into physical MonoGame
coordinates. Rectangles, vectors, line thickness, and text size are mapped
through the current scale. Invalid scale values are rejected by
`UiCoordinateMapper.ValidateScale`.

Text rendering requires a `SkiaTextRasterizer` supplied to the constructor. If
no rasterizer is provided, text draw commands are ignored. Rasterized text
textures are cached by text, font, size, and color, then disposed when the
backend is disposed.

Image commands must use `MonoGameImage`. Passing another `IDrawImage`
implementation to `DrawCommand.DrawImage` and rendering it with this backend
throws `InvalidOperationException`.

## Constructors

| Name | Description |
| --- | --- |
| `MonoGameDrawingBackend(SpriteBatch spriteBatch, Texture2D whitePixel, SkiaTextRasterizer? textRasterizer = null)` | Initializes a backend that draws through `spriteBatch`, uses `whitePixel` for primitive shapes and lines, and optionally rasterizes text with `textRasterizer`. Throws `ArgumentNullException` when `spriteBatch` or `whitePixel` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `CoordinateScale` | `float` | Gets or sets the logical-to-physical coordinate scale used by the backend. The default is `1`. The setter validates the value with `UiCoordinateMapper.ValidateScale`. |
| `ScissorRasterizerState` | `RasterizerState` | Gets a shared rasterizer state with `ScissorTestEnable` set to `true`, intended for `SpriteBatch.Begin` calls that render clipped command lists. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Render(DrawCommandList commands)` | `void` | Renders each command in `commands`. Throws `ArgumentNullException` when `commands` is `null` and `ObjectDisposedException` after the backend has been disposed. |
| `Dispose()` | `void` | Disposes cached text textures, clears the text texture cache, and marks the backend as disposed. Calling it more than once is allowed. |

## Supported Draw Commands

| Command Kind | Behavior |
| --- | --- |
| `FillRectangle` | Draws the `whitePixel` texture stretched to the mapped rectangle. |
| `DrawRectangle` | Draws four filled edges using the mapped rectangle and mapped thickness. |
| `FillEllipse` | Draws horizontal spans inside the mapped ellipse bounds. Empty or negative-size mapped bounds are ignored. |
| `DrawEllipse` | Draws an approximated ellipse ring with line segments. Empty or negative-size mapped bounds are ignored. |
| `DrawLine` | Draws a rotated `whitePixel` segment between the mapped start and end points. A zero-length line draws a square at the start point. |
| `DrawImage` | Draws a `MonoGameImage.Texture` into the mapped destination rectangle with the command color as tint. |
| `DrawText` | Rasterizes and caches text through `SkiaTextRasterizer` when one is available, then draws the cached texture at the mapped text position. |
| `PushClip` | Pushes the mapped rectangle onto the clip stack and assigns the intersected clip to `GraphicsDevice.ScissorRectangle`. |
| `PopClip` | Pops the clip stack and assigns the resulting clip to `GraphicsDevice.ScissorRectangle`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| Constructor | `ArgumentNullException` | `spriteBatch` or `whitePixel` is `null`. |
| `CoordinateScale` | `ArgumentOutOfRangeException` | The assigned scale is not finite or is less than or equal to zero. |
| `Render` | `ArgumentNullException` | `commands` is `null`. |
| `Render` | `ObjectDisposedException` | The backend has already been disposed. |
| `Render` | `InvalidOperationException` | A command kind is unsupported, or a `DrawImage` command contains an image that is not `MonoGameImage`. |

## Applies To

Cerneala MonoGame drawing integration.

## See Also

- `Cerneala.Drawing.IDrawingBackend`
- `Cerneala.Drawing.DrawCommandList`
- `Cerneala.Drawing.DrawCommand`
- `Cerneala.Drawing.MonoGame.MonoGameImage`
- `Cerneala.UI.Hosting.MonoGame.MonoGameUiHost`
