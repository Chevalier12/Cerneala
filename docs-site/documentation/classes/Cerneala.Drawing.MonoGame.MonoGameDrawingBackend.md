# MonoGameDrawingBackend Class

## Definition

Namespace: `Cerneala.Drawing.MonoGame`

Assembly/Project: `Cerneala`

Source: `Drawing/MonoGame/MonoGameDrawingBackend.cs`

Renders `DrawCommandList` instances through a MonoGame `SpriteBatch`.

```csharp
public sealed class MonoGameDrawingBackend :
    IDrawingBackend,
    IDrawingBackendFrameTimingSource,
    IDisposable
```

Inheritance:
`object` -> `MonoGameDrawingBackend`

Implements:
`IDrawingBackend`, `IDrawingBackendFrameTimingSource`, `IDisposable`

## Examples

`Render` owns its complete `SpriteBatch.Begin`/`End` pair. Do not begin the
supplied sprite batch before calling it.

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.Prism.Graph;
using Microsoft.Xna.Framework.Graphics;
using CernealaColor = Cerneala.Drawing.Color;
using XnaColor = Microsoft.Xna.Framework.Color;

using Texture2D whitePixel = new(graphicsDevice, 1, 1);
whitePixel.SetData(new[] { XnaColor.White });

using SpriteBatch spriteBatch = new(graphicsDevice);
using MonoGameDrawingBackend backend = new(spriteBatch, whitePixel);

DrawCommandList commands = new();
commands.Add(DrawCommand.PushClip(new DrawRect(0, 0, 200, 120)));
commands.Add(DrawCommand.FillRectangle(new DrawRect(10, 10, 80, 40), new CernealaColor(32, 120, 220, 255)));
commands.Add(DrawCommand.DrawRectangle(new DrawRect(10, 10, 80, 40), CernealaColor.White, 2));
commands.Add(DrawCommand.PopClip());

DrawingFrameContext frameContext = new(
    new PrismFrameAnalyzer().Analyze(commands));

backend.Render(commands, in frameContext);
```

## Remarks

`MonoGameDrawingBackend` maps Cerneala drawing commands to MonoGame drawing
operations. It supports filled and stroked rectangles, filled and stroked
ellipses, lines, filled SVG paths, images, text, and push/pop clip commands.

The backend treats the submitted command list as read-only while rendering and
owns the top-level `SpriteBatch.Begin`/`End` pair for every `Render` call. The
sprite batch must not already be active when `Render` starts.

The constructor borrows `SpriteBatch`, `whitePixel`, and their
`GraphicsDevice`. They must be alive, and the texture and sprite batch must
belong to the same device. Disposing the backend releases only backend-owned
states, effects, and cached GPU resources; it does not dispose the borrowed
sprite batch, texture, or graphics device.

`Render` first validates the supplied `DrawingFrameContext` against the command
list. A frame without analyzed Prism scopes follows the direct command path. A
frame with Prism scopes builds and optimizes the backend-neutral graph, captures
each scope once, executes the fundamental copy, normal-composite, mask, clip,
opacity, fill, color-conversion, and present passes on transient render targets,
then composites the result into the incoming host target.

The Prism executor uses build-embedded shader bytes and does not compile effects
at runtime or use the application's `ContentManager`. Missing non-fundamental
filter and style kernels follow the explicit Prism fallback policy. If the
fundamental shader resource cannot be loaded, the backend renders the raw
commands between the Prism delimiters instead of silently substituting another
effect.

Completed Prism captures, intermediate passes, and final compositions can be
retained as backend-owned GPU surfaces across frames. Lookup uses the complete
pixel dependency key, and retained entries are bounded by the configured byte
and entry limits. The cache is cleared or selectively invalidated when its
owner, resources, raster context, shader package, device, or backend lifetime
changes. `RendererDiagnostics` exposes an immutable snapshot of this work.
Both transient and retained Prism surfaces are released on device reset and
backend disposal.

When Prism renders into an offscreen host target, that target must use
`RenderTargetUsage.PreserveContents` if pixels written before `Render` must
survive the compositor's target switches. The WindowsDX `RenderPng` path
configures its capture target this way.

Clipping uses `GraphicsDevice.ScissorRectangle`. During `Render`, the backend
creates a clip stack from the current viewport and applies clip commands. In a
`finally` block, including when preparation or a command throws, it ends its
sprite batch and restores the incoming render targets, viewport, scissor
rectangle, blend state and blend factor, depth/stencil state, rasterizer state,
sampler slot 0, texture slot 0, and index buffer.

`CoordinateScale` converts logical coordinates into physical MonoGame
coordinates. Rectangles, vectors, line thickness, and text size are mapped
through the current scale. Invalid scale values are rejected by
`UiCoordinateMapper.ValidateScale`.

Line strokes are centered on the segment axis. This keeps connected segments
aligned at shared points and centers the square produced for a zero-length line.

Text rendering requires a `SkiaTextRasterizer` supplied to the constructor. If
no rasterizer is provided, text draw commands are ignored. For solid text,
Skia generates foreground-aware LCD masks so its gamma and contrast correction
is preserved when the masks are composited by MonoGame. Glyph masks are cached
by text, font, size, DPI scale, subpixel phase, and rasterization color.
Solid brushes tint the cached subpixel masks directly. Gradient, image,
drawing, and visual brushes are rendered into a device-local texture and
multiplied by the grayscale glyph mask. All text and brush textures are scoped
to the backend's `GraphicsDevice`. Equivalent `SkiaFont` wrappers share entries
when they use the same Skia typeface. At the end of each render, entries not
used by that frame are disposed and evicted so changing text and animated
subpixel positions cannot grow the GPU cache without bound. The complete cache
is also cleared on scale changes, device reset, or disposal.

Filled SVG paths are parsed and flattened into contours, tessellated into a
triangle mesh, and submitted directly through `GraphicsDevice` with
`DrawUserIndexedPrimitives`. Meshes are cached by SVG data, source view box,
destination size, subpixel phase, color, and opacity. The WindowsDX host uses
the highest available MSAA level up to 8 samples for edge antialiasing. SVG
path fills currently require a solid brush. Skia is not used for path geometry.

Image commands must use `MonoGameImage`. Passing another `IDrawImage`
implementation to `DrawCommand.DrawImage` and rendering it with this backend
throws `InvalidOperationException`.

## Constructors

| Name | Description |
| --- | --- |
| `MonoGameDrawingBackend(SpriteBatch spriteBatch, Texture2D whitePixel, SkiaTextRasterizer? textRasterizer = null)` | Initializes a backend that borrows `spriteBatch`, its graphics device, and `whitePixel`, and optionally rasterizes text with `textRasterizer`. The resources remain caller-owned and must use the same graphics device. |
| `MonoGameDrawingBackend(SpriteBatch spriteBatch, Texture2D whitePixel, SkiaTextRasterizer? textRasterizer, PrismRendererOptions prismRendererOptions)` | Initializes a backend with explicit Prism surface budgets and development-diagnostic behavior. The options are validated before backend resources are created. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `CoordinateScale` | `float` | Gets or sets the logical-to-physical coordinate scale used by the backend. The default is `1`. The setter validates the value with `UiCoordinateMapper.ValidateScale`. |
| `RendererDiagnostics` | `PrismRendererDiagnostics` | Gets an immutable snapshot of cumulative Prism cache work and current surface usage. Before the first Prism frame, the snapshot contains zero counters. |
| `ScissorRasterizerState` | `RasterizerState` | Creates a caller-owned rasterizer state with `ScissorTestEnable` set to `true`. `Render` uses its own internal state, so callers do not need this property for backend rendering. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Render(DrawCommandList commands, in DrawingFrameContext frameContext)` | `void` | Validates the current frame context, owns the sprite batch for the call, executes direct or Prism-composited drawing, and restores the documented incoming graphics-device state. |
| `Dispose()` | `void` | Disposes backend-owned states, effects, and caches without disposing the borrowed sprite batch, white-pixel texture, or graphics device. Calling it more than once is allowed. |

## Supported Draw Commands

| Command Kind | Behavior |
| --- | --- |
| `FillRectangle` | Draws the `whitePixel` texture stretched to the mapped rectangle. |
| `DrawRectangle` | Draws four filled edges using the mapped rectangle and mapped thickness. |
| `FillEllipse` | Draws horizontal spans inside the mapped ellipse bounds. Empty or negative-size mapped bounds are ignored. |
| `DrawEllipse` | Draws an approximated ellipse ring with line segments. Empty or negative-size mapped bounds are ignored. |
| `DrawLine` | Draws a rotated `whitePixel` segment centered on the mapped start and end points. A zero-length line draws a centered square at the start point. |
| `FillPath` | Parses and tessellates SVG path data, stretches its source view box into the destination rectangle, and draws the resulting triangles with a solid brush. |
| `DrawImage` | Draws a `MonoGameImage.Texture` into the mapped destination rectangle with the command color as tint. |
| `DrawText` | Reuses cached glyph masks, then applies a solid, gradient, image, drawing, or visual brush at the mapped text position. |
| `PushClip` | Pushes the mapped rectangle onto the clip stack and assigns the intersected clip to `GraphicsDevice.ScissorRectangle`. |
| `PopClip` | Pops the clip stack and assigns the resulting clip to `GraphicsDevice.ScissorRectangle`. |
| `BeginPrism`, `EndPrism` | Delimits analyzed Prism scopes. The compositor captures their interior commands and executes the optimized graph; shader-unavailable fallback preserves the raw interior commands. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| Constructor | `ArgumentNullException` | `spriteBatch`, `whitePixel`, or the explicit `prismRendererOptions` is `null`. |
| Constructor | `ObjectDisposedException` | `spriteBatch`, `whitePixel`, or their graphics device is disposed. |
| Constructor | `ArgumentException` | `whitePixel` belongs to a different graphics device than `spriteBatch`. |
| Constructor | `ArgumentOutOfRangeException` | A Prism byte or entry limit is negative, or the retained soft byte limit exceeds the surface hard byte limit. |
| `CoordinateScale` | `ArgumentOutOfRangeException` | The assigned scale is not finite or is less than or equal to zero. |
| `Render` | `ArgumentNullException` | `commands` is `null`. |
| `Render` | `InvalidOperationException` | `frameContext` is uninitialized or its analysis does not match the command list and current Prism scope versions. |
| `Render` | `ObjectDisposedException` | The backend has already been disposed. |
| `Render` | `InvalidOperationException` | The sprite batch is already active or does not have a graphics device. |
| `Render` | `InvalidOperationException` | A command kind is unsupported, or a `DrawImage` command contains an image that is not `MonoGameImage`. |
| `Render` | `NotSupportedException` | A `FillPath` command uses a non-solid brush. |

## Applies To

Cerneala MonoGame drawing integration.

## See Also

- `Cerneala.Drawing.IDrawingBackend`
- `Cerneala.Drawing.DrawCommandList`
- `Cerneala.Drawing.DrawCommand`
- `Cerneala.Drawing.DrawingFrameContext`
- `Cerneala.Drawing.MonoGame.MonoGameImage`
- `Cerneala.Drawing.Prism.PrismRendererOptions`
- `Cerneala.Drawing.Prism.PrismRendererDiagnostics`
- `Cerneala.UI.Hosting.MonoGame.MonoGameUiHost`
