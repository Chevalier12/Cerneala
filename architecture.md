# Cerneala Architecture

This document explains how `Drawing` and `UI/Input` work in this repository.

Read this before changing drawing or input, adding WPF-style media/input classes, or implementing anything from `ROADMAP.md` that touches rendering, text, images, colors, geometry, shapes, controls, visual tree behavior, routed events, commands, focus, keyboard, mouse, drag/drop, or input routing.

## Big Picture

`Drawing` is a small retained command pipeline:

```text
future controls / playground code
        |
        v
DrawingContext
        |
        v
DrawCommandList
        |
        v
IDrawingBackend
        |
        v
MonoGameDrawingBackend
        |
        v
SpriteBatch / GraphicsDevice
```

The core idea is simple: application/UI code records drawing intent as backend-agnostic commands. A backend later consumes those commands and performs real rendering.

The drawing core does not own layout, input, styling, templates, dependency properties, visual tree behavior, or control state. It is only the low-level drawing command layer.

`UI/Input` is a small backend-neutral input model:

```text
MonoGame keyboard/mouse state
        |
        v
MonoGameInputSource
        |
        v
InputFrame
        |
        v
future control hit testing / focus selection
        |
        v
UiInputTree + RoutedEventRouter
        |
        v
routed event handlers / command bindings
```

The input core separates raw frame state from routed UI dispatch. `InputFrame` says what changed this frame. `UiInputTree` and `RoutedEventRouter` decide how a routed event moves through a UI element tree.

## Folder Layout

Production drawing files:

- `Drawing/DrawArgument.cs`
- `Drawing/Color.cs`
- `Drawing/DrawCommand.cs`
- `Drawing/DrawCommandKind.cs`
- `Drawing/DrawCommandList.cs`
- `Drawing/DrawingContext.cs`
- `Drawing/DrawPoint.cs`
- `Drawing/DrawRect.cs`
- `Drawing/DrawTextRun.cs`
- `Drawing/IDrawFont.cs`
- `Drawing/IDrawImage.cs`
- `Drawing/IDrawingBackend.cs`
- `Drawing/IFontSource.cs`
- `Drawing/MonoGame/MonoGameDrawingBackend.cs`
- `Drawing/MonoGame/MonoGameImage.cs`
- `Drawing/Text/RasterizedText.cs`
- `Drawing/Text/SkiaFont.cs`
- `Drawing/Text/SkiaTextRasterizer.cs`
- `Drawing/Text/SkiaTextShaper.cs`
- `Drawing/Text/SystemFontSource.cs`
- `Drawing/Text/TextShapeResult.cs`

Drawing tests:

- `tests/Cerneala.Tests/Drawing/DrawCommandListTests.cs`
- `tests/Cerneala.Tests/Drawing/DrawingContextTests.cs`
- `tests/Cerneala.Tests/Drawing/DrawingResourceTests.cs`
- `tests/Cerneala.Tests/Drawing/TextPipelineTests.cs`

Production input files:

- `UI/Input/CanExecuteRoutedEventArgs.cs`
- `UI/Input/CommandBinding.cs`
- `UI/Input/CommandEvents.cs`
- `UI/Input/ExecutedRoutedEventArgs.cs`
- `UI/Input/ICommand.cs`
- `UI/Input/IInputSource.cs`
- `UI/Input/InputButtonState.cs`
- `UI/Input/InputEvents.cs`
- `UI/Input/InputFrame.cs`
- `UI/Input/InputKey.cs`
- `UI/Input/InputMouseButton.cs`
- `UI/Input/KeyboardFocusChangedEventArgs.cs`
- `UI/Input/KeyboardSnapshot.cs`
- `UI/Input/KeyEventArgs.cs`
- `UI/Input/MonoGame/MonoGameInputMapper.cs`
- `UI/Input/MonoGame/MonoGameInputSource.cs`
- `UI/Input/MouseButtonEventArgs.cs`
- `UI/Input/MouseEventArgs.cs`
- `UI/Input/MouseWheelEventArgs.cs`
- `UI/Input/PointerSnapshot.cs`
- `UI/Input/RoutedCommand.cs`
- `UI/Input/RoutedEvent.cs`
- `UI/Input/RoutedEventArgs.cs`
- `UI/Input/RoutedEventRegistry.cs`
- `UI/Input/RoutedEventRouter.cs`
- `UI/Input/RoutingStrategy.cs`
- `UI/Input/TextCompositionEventArgs.cs`
- `UI/Input/TextInputSnapshotEvent.cs`
- `UI/Input/UiElementId.cs`
- `UI/Input/UiInputElement.cs`
- `UI/Input/UiInputTree.cs`

Input tests:

- `tests/Cerneala.Tests/Input/CommandingTests.cs`
- `tests/Cerneala.Tests/Input/InputEventsTests.cs`
- `tests/Cerneala.Tests/Input/InputFrameTests.cs`
- `tests/Cerneala.Tests/Input/MonoGameInputMapperTests.cs`
- `tests/Cerneala.Tests/Input/RoutedEventRouterTests.cs`
- `tests/Cerneala.Tests/Input/RoutedEventTests.cs`

Existing architecture diagram:

- `docs/diagrams/cerneala-drawing-flowchart.svg`

## Namespaces

The files live under the `Drawing` folder, but the main namespace is:

```csharp
namespace Cerneala.Drawing;
```

Text-specific implementation types use:

```csharp
namespace Cerneala.Drawing.Text;
```

MonoGame-specific implementation types use:

```csharp
namespace Cerneala.Drawing.MonoGame;
```

Input types use:

```csharp
namespace Cerneala.UI.Input;
```

MonoGame-specific input types use:

```csharp
namespace Cerneala.UI.Input.MonoGame;
```

This split matters. The folder says where the drawing layer lives in the project. The namespace says the public API is currently the generic drawing API, not a `Cerneala.UI.Drawing` control-specific API.

## Layer Boundaries

The drawing architecture has three distinct layers.

### 1. Drawing Core

Files:

- `DrawingContext.cs`
- `DrawCommandList.cs`
- `DrawCommand.cs`
- `DrawCommandKind.cs`
- `DrawRect.cs`
- `DrawPoint.cs`
- `Color.cs`
- `DrawTextRun.cs`
- `IDrawFont.cs`
- `IDrawImage.cs`
- `IDrawingBackend.cs`
- `IFontSource.cs`
- `DrawArgument.cs`

Responsibilities:

- represent primitive drawing values;
- validate primitive drawing arguments;
- record drawing commands;
- expose backend-neutral font and image handles;
- define the backend contract.

This layer must not reference MonoGame, SkiaSharp, HarfBuzzSharp, `SpriteBatch`, `Texture2D`, `SKCanvas`, or platform rendering details.

### 2. Text Preparation

Files:

- `Text/SkiaFont.cs`
- `Text/SystemFontSource.cs`
- `Text/SkiaTextShaper.cs`
- `Text/SkiaTextRasterizer.cs`
- `Text/TextShapeResult.cs`
- `Text/RasterizedText.cs`

Responsibilities:

- load system fonts through Skia;
- shape text through HarfBuzz;
- rasterize shaped text through Skia;
- return immutable/copy-safe text results and pixel buffers.

This layer prepares text for drawing. It is not the final screen renderer.

### 3. MonoGame Adapter

Files:

- `MonoGame/MonoGameDrawingBackend.cs`
- `MonoGame/MonoGameImage.cs`

Responsibilities:

- implement `IDrawingBackend`;
- consume `DrawCommandList`;
- translate draw primitives to MonoGame types;
- call `SpriteBatch`;
- manage clip state through `GraphicsDevice.ScissorRectangle`;
- turn rasterized text pixels into cached `Texture2D` instances;
- wrap MonoGame textures as `IDrawImage`.

This is where final rendering happens.

## Drawing Flow

The current playground manually uses the drawing layer like this:

```text
DrawCommandList.Clear()
DrawingContext.FillRectangle(...)
DrawingContext.DrawRectangle(...)
DrawingContext.DrawText(...)
SpriteBatch.Begin(...)
MonoGameDrawingBackend.Render(commands)
SpriteBatch.End()
```

Important detail: `DrawingContext` does not render. It only appends commands to `DrawCommandList`.

## `DrawingContext`

File:

- `Drawing/DrawingContext.cs`

`DrawingContext` is a command recorder. It holds a `DrawCommandList` passed into its constructor.

Constructor behavior:

- rejects `null` command lists with `ArgumentNullException`;
- stores the command list reference;
- does not create its own list.

Methods:

- `FillRectangle(DrawRect rect, Color color)`
- `DrawRectangle(DrawRect rect, Color color, float thickness)`
- `DrawText(DrawTextRun textRun, DrawPoint position, Color color)`
- `DrawImage(IDrawImage image, DrawRect destination, Color color)`
- `PushClip(DrawRect rect)`
- `PopClip()`

Each method creates a `DrawCommand` through a static factory and adds it to the list.

Implications:

- command order is the render order;
- clips are explicit commands;
- the caller controls when the list is cleared;
- the caller controls when the backend renders;
- drawing state is not hidden inside `DrawingContext`.

## `DrawCommandList`

File:

- `Drawing/DrawCommandList.cs`

`DrawCommandList` is a mutable ordered list of `DrawCommand`.

It implements:

```csharp
IReadOnlyList<DrawCommand>
```

Members:

- `Count`
- indexer `this[int index]`
- `Add(DrawCommand command)`
- `Clear()`
- generic and non-generic enumerators

The tests prove:

- commands are stored in insertion order;
- `Clear()` removes all commands;
- backends can consume it through the read-only list/enumeration shape.

This is not a scene graph. It has no retained element hierarchy, no invalidation, no layout, and no ownership of resources.

## `DrawCommand`

File:

- `Drawing/DrawCommand.cs`

`DrawCommand` is a `readonly record struct`.

It stores all possible command payload fields:

- `Kind`
- `Rect`
- `Color`
- `Thickness`
- `Text`
- `TextRun`
- `Position`
- `Image`
- `Font`

It has a private constructor and public static factories:

- `FillRectangle(DrawRect rect, Color color)`
- `DrawRectangle(DrawRect rect, Color color, float thickness)`
- `DrawText(DrawTextRun textRun, DrawPoint position, Color color)`
- `DrawImage(IDrawImage image, DrawRect destination, Color color)`
- `PushClip(DrawRect rect)`
- `PopClip()`

Factory behavior:

- `DrawRectangle` validates `thickness` through `DrawArgument.ThrowIfNotValidPixelSize`;
- `DrawText` rejects `null` `DrawTextRun`;
- `DrawImage` rejects `null` `IDrawImage`;
- `PopClip` carries no meaningful payload.

Important command payload details:

- rectangle commands use `Rect`;
- text commands use `TextRun`, `Text`, `Font`, `Position`, and `Color`;
- image commands use `Image`, `Rect`, and `Color`;
- clip push uses `Rect`;
- clip pop uses only `Kind`.

The `Text` and `Font` properties duplicate data from `TextRun` for convenient inspection and backend access.

## `DrawCommandKind`

File:

- `Drawing/DrawCommandKind.cs`

Current command kinds:

- `FillRectangle`
- `DrawRectangle`
- `DrawText`
- `DrawImage`
- `PushClip`
- `PopClip`

There are no commands yet for ellipses, arbitrary geometry, paths, gradients, transforms, opacity, layers, shadows, rotation, scaling, or complex brushes.

That matters for the roadmap: WPF-style classes such as `Shape`, `Brush`, `Geometry`, `Transform`, and `Drawing` cannot simply forward to existing command kinds unless the command layer grows.

## Primitive Drawing Values

### `DrawRect`

File:

- `Drawing/DrawRect.cs`

`DrawRect` is a `readonly record struct` with:

- `X`
- `Y`
- `Width`
- `Height`
- `Right`
- `Bottom`

Validation:

- `X` and `Y` must be finite valid pixel coordinates;
- `Width` and `Height` must be finite, valid, non-negative pixel sizes;
- `X + Width` and `Y + Height` must also be valid pixel coordinates;
- coordinates and sizes are bounded by the drawing argument max pixel range.

Important detail: width and height may be zero, because `DrawArgument.ThrowIfNegativeOrNotValidPixelSize` allows zero. That differs from thickness and text size, which must be positive.

Tests prove:

- edge properties work;
- `NaN`, positive infinity, negative dimensions, overflowed edges, and outside-pixel-range values are rejected.

### `DrawPoint`

File:

- `Drawing/DrawPoint.cs`

`DrawPoint` is a `readonly record struct` with:

- `X`
- `Y`

Validation:

- both values must be finite;
- unlike `DrawRect`, it does not apply the max pixel coordinate range.

Tests prove:

- `NaN` and positive infinity are rejected.

### `Color`

File:

- `Drawing/Color.cs`

`Color` is a `readonly record struct` with:

- `R`
- `G`
- `B`
- `A`

All channels are `byte`.

Default alpha:

- `A = 255`

Static colors:

- the complete WPF named-color catalog (`Transparent`, `AliceBlue`, `Tomato`, `YellowGreen`, and the remaining standard names)
- `FromRgb` and `FromArgb` factories
- `TryParse` for named colors, hex values, and RGB/RGBA channel lists

Tests prove:

- constructing with RGB creates an opaque color by default.

## Validation Rules

File:

- `Drawing/DrawArgument.cs`

`DrawArgument` is `internal static`, so validation is currently implementation detail of the drawing assembly.

Constants:

- `MaxPixelSize = 2_000_000_000f`
- `MaxTextSize = 16_384f`

Validation methods:

- `ThrowIfNotFinite`
- `ThrowIfNegativeOrNotFinite`
- `ThrowIfNotValidPixelCoordinate`
- `ThrowIfNegativeOrNotValidPixelSize`
- `ThrowIfNotPositiveFinite`
- `ThrowIfNotValidPixelSize`
- `ThrowIfNotValidTextSize`

Rules:

- pixel coordinates must be finite and between `-2_000_000_000` and `2_000_000_000`;
- non-negative pixel sizes must be finite, not negative, and not above `2_000_000_000`;
- positive pixel sizes must be finite, above zero, and not above `2_000_000_000`;
- text sizes must be finite, above zero, and not above `16_384`.

Why text max is smaller:

- the tests show it protects shaping/rasterization from unsafe HarfBuzz scale and rasterization sizes.

## Fonts

### `IDrawFont`

File:

- `Drawing/IDrawFont.cs`

Public abstraction for a font used by drawing.

Properties:

- `FamilyName`
- `Size`

It intentionally does not expose Skia types.

### `IFontSource`

File:

- `Drawing/IFontSource.cs`

Public abstraction for loading fonts.

Method:

```csharp
IDrawFont LoadFont(string familyName, float size);
```

### `SkiaFont`

File:

- `Drawing/Text/SkiaFont.cs`

Concrete `IDrawFont` backed by SkiaSharp.

Constructor inputs:

- `SKTypeface typeface`
- `string familyName`
- `float size`

Behavior:

- rejects `null` typeface;
- rejects `null`, empty, or whitespace family name;
- validates size through `DrawArgument.ThrowIfNotValidTextSize`;
- exposes the Skia `SKTypeface` through `Typeface`;
- exposes `FamilyName` and `Size` through `IDrawFont`.

Tests prove:

- null typefaces are rejected;
- empty family names are rejected;
- invalid sizes are rejected.

### `SystemFontSource`

File:

- `Drawing/Text/SystemFontSource.cs`

Concrete `IFontSource` that loads fonts from the operating system through:

```csharp
SKFontManager.Default.MatchFamily(familyName)
```

If Skia cannot match the requested family, it falls back to:

```csharp
SKTypeface.Default
```

Behavior:

- rejects `null`, empty, or whitespace family name;
- validates requested size;
- returns a `SkiaFont`;
- preserves the requested family name metadata even if Skia falls back internally.

Tests prove:

- it loads a `SkiaFont`;
- it preserves requested `FamilyName` and `Size`;
- it rejects empty family names and invalid sizes.

## Text Runs

File:

- `Drawing/DrawTextRun.cs`

`DrawTextRun` represents a specific text drawing request before shaping/rasterization.

Constructor inputs:

- `IDrawFont font`
- `string text`
- `float size`

Properties:

- `Font`
- `Text`
- `Size`

Behavior:

- rejects `null` font;
- rejects `null` text;
- validates size through `DrawArgument.ThrowIfNotValidTextSize`.

Important detail:

- the `IDrawFont` also has a `Size`, but `DrawTextRun` stores its own `Size`. Current rendering uses `DrawTextRun.Size` for shaping/rasterization. The backend text texture cache key includes both `Font` and `FontSize`.

Tests prove:

- invalid sizes are rejected;
- extreme sizes that would overflow HarfBuzz scale or unsafe rasterization are rejected.

## Text Shaping

File:

- `Drawing/Text/SkiaTextShaper.cs`

`SkiaTextShaper` turns a `DrawTextRun` into a `TextShapeResult`.

Input:

- `DrawTextRun`

Output:

- `TextShapeResult`

Behavior:

- rejects `null` text runs;
- requires `textRun.Font` to be `SkiaFont`;
- throws `InvalidOperationException` for non-`SkiaFont` implementations;
- reads font bytes from `SkiaFont.Typeface.OpenStream()`;
- creates a HarfBuzz `Blob`, `Face`, and `Font`;
- adds UTF-16 text to a HarfBuzz buffer;
- guesses segment properties;
- sets HarfBuzz scale to `Round(textRun.Size * 64)`, minimum `1`;
- shapes the buffer;
- converts glyph ids to `ushort`;
- converts HarfBuzz positions from 26.6 fixed units to pixels by dividing by `64f`;
- accumulates glyph positions from advances and offsets.

Position conversion:

- `x` starts at `0`;
- `y` starts at `0`;
- each glyph position is `x + XOffset`, `y - YOffset`;
- `x` advances by `XAdvance`;
- `y` subtracts `YAdvance`.

The Y values are inverted relative to HarfBuzz's values by subtracting offsets/advances.

Tests prove:

- null text runs are rejected;
- shaping system-font text returns a positive glyph count;
- returned glyph id and glyph position arrays match `GlyphCount`.

## `TextShapeResult`

File:

- `Drawing/Text/TextShapeResult.cs`

`TextShapeResult` is a `readonly record struct`.

It stores:

- text;
- glyph count;
- glyph ids;
- glyph positions.

Public members:

- `Text`
- `GlyphCount`
- `GlyphIds`
- `GlyphPositions`

Constructor overloads:

- `TextShapeResult(string text, int glyphCount)`
- `TextShapeResult(string text, int glyphCount, ushort[] glyphIds, DrawPoint[] glyphPositions)`

Behavior:

- rejects negative glyph count;
- rejects `null` text;
- rejects `null` glyph id arrays;
- rejects `null` glyph position arrays;
- rejects glyph arrays whose lengths do not match glyph count;
- rejects glyph counts too large to allocate;
- defensively clones input arrays;
- returns cloned arrays from getters;
- default value returns empty text and empty arrays.

Tests prove all of the above, including defensive copying.

## Text Rasterization

File:

- `Drawing/Text/SkiaTextRasterizer.cs`

`SkiaTextRasterizer` turns a `DrawTextRun` and `Color` into rasterized RGBA pixels.

Dependencies:

- owns a `SkiaTextShaper`;
- default constructor creates a new `SkiaTextShaper`;
- alternate constructor accepts a shaper and rejects `null`.

Behavior:

- rejects `null` text runs;
- requires `textRun.Font` to be `SkiaFont`;
- throws `InvalidOperationException` for non-`SkiaFont` implementations;
- shapes text first;
- for empty shaped text, returns a transparent `1x1` `RasterizedText`;
- creates an `SKFont` using `SkiaFont.Typeface` and `DrawTextRun.Size`;
- creates an antialiased `SKPaint` with converted `Color`;
- creates an `SKTextBlob` from glyph ids and positions;
- uses text blob bounds to determine raster bitmap width and height;
- uses `Ceiling(bounds.Width)` and `Ceiling(bounds.Height)`, minimum `1`;
- draws the text blob into an `SKBitmap`;
- returns `RasterizedText` with bitmap bytes and the original shape result.

Color conversion:

- `Color(R,G,B,A)` becomes `SKColor(R,G,B,A)`.

Tests prove:

- system-font text rasterizes to non-empty pixels;
- larger text run size creates larger raster output;
- empty text returns `1x1` pixels and empty shape data.

## `RasterizedText`

File:

- `Drawing/Text/RasterizedText.cs`

`RasterizedText` stores rasterized RGBA text pixels.

Constructor inputs:

- `int width`
- `int height`
- `byte[] rgbaPixels`
- `TextShapeResult shapeResult`

Properties:

- `Width`
- `Height`
- `RgbaPixels`
- `ShapeResult`

Behavior:

- rejects zero or negative width;
- rejects zero or negative height;
- rejects `null` pixel buffers;
- expects buffer length to equal `width * height * 4`;
- rejects dimensions whose RGBA buffer size exceeds `int.MaxValue`;
- defensively clones input pixels;
- returns cloned pixels from `RgbaPixels`.

Tests prove:

- invalid dimensions are rejected;
- mismatched pixel buffer length is rejected;
- pixel dimension overflow is rejected;
- pixel input and output are defensively copied.

## Images

### `IDrawImage`

File:

- `Drawing/IDrawImage.cs`

Public drawing abstraction for images.

Properties:

- `Width`
- `Height`

It intentionally does not expose backend-specific texture types.

### `MonoGameImage`

File:

- `Drawing/MonoGame/MonoGameImage.cs`

Concrete `IDrawImage` wrapper around MonoGame `Texture2D`.

Behavior:

- rejects `null` texture;
- exposes `Texture`;
- returns `Texture.Width` and `Texture.Height`.

Tests prove:

- null textures are rejected.

## Backend Contract

File:

- `Drawing/IDrawingBackend.cs`

The backend contract is:

```csharp
void Render(DrawCommandList commands);
```

Backends consume command lists. The current concrete backend is MonoGame.

Tests prove:

- the interface exposes `Render`;
- `MonoGameDrawingBackend` implements `IDrawingBackend`.

## MonoGame Backend

File:

- `Drawing/MonoGame/MonoGameDrawingBackend.cs`

`MonoGameDrawingBackend` is the final renderer for MonoGame.

Constructor inputs:

- `SpriteBatch spriteBatch`
- `Texture2D whitePixel`
- optional `SkiaTextRasterizer textRasterizer`

Behavior:

- rejects `null` sprite batch;
- rejects `null` white pixel;
- stores optional text rasterizer;
- owns a clip stack;
- owns a text texture cache;
- implements `IDrawingBackend`;
- implements `IDisposable`.

Static property:

- `ScissorRasterizerState`

`ScissorRasterizerState` enables scissor clipping:

```csharp
ScissorTestEnable = true
```

The caller must use this rasterizer state when beginning the `SpriteBatch` if clipping should work.

### Render Dispatch

`Render(DrawCommandList commands)`:

- rejects `null` command lists;
- enumerates commands in order;
- dispatches by `DrawCommandKind`.

Dispatch map:

- `FillRectangle` -> `FillRectangle`
- `DrawRectangle` -> `DrawRectangle`
- `DrawImage` -> `DrawImage`
- `DrawText` -> `DrawText`
- `PushClip` -> `PushClip`
- `PopClip` -> `PopClip`
- unknown kind -> `InvalidOperationException`

### Rectangle Filling

`FillRectangle`:

- converts `DrawRect` to MonoGame `Rectangle`;
- converts `Color` to MonoGame `Color`;
- draws `_whitePixel` stretched into the rectangle.

### Rectangle Stroke

`DrawRectangle`:

- rounds thickness to an integer;
- clamps rounded line thickness to at least `1`;
- converts bounds to MonoGame `Rectangle`;
- draws four stretched `_whitePixel` rectangles:
  - top edge;
  - bottom edge;
  - left edge;
  - right edge.

Thickness is already validated at command creation.

### Image Drawing

`DrawImage`:

- requires command image to be `MonoGameImage`;
- throws `InvalidOperationException` if the image is not a `MonoGameImage`;
- draws `MonoGameImage.Texture` into the command rectangle with tint color.

This means the current MonoGame backend cannot render arbitrary `IDrawImage` implementations.

### Text Drawing

`DrawText`:

- if `_textRasterizer` is `null`, it returns without drawing;
- if `command.TextRun` is `null`, it returns without drawing;
- builds a `TextTextureKey` from text run and color;
- looks up a cached `Texture2D`;
- if missing, rasterizes text through `SkiaTextRasterizer`;
- creates a new MonoGame `Texture2D`;
- uploads `RasterizedText.RgbaPixels` into that texture;
- caches the texture;
- draws the texture at command position with `Color.White`.

Text color is baked into the rasterized texture. The final sprite draw uses white so the baked pixels are not tinted again.

### Text Texture Cache

Private key:

```csharp
TextTextureKey(string Text, IDrawFont Font, float FontSize, Color Color)
```

Key source:

```csharp
TextTextureKey.From(DrawTextRun textRun, Color color)
```

The cache distinguishes:

- text;
- font instance;
- text run size;
- color.

Tests prove:

- two different `SkiaFont` instances with the same family metadata produce different cache keys when the underlying font differs.

### Clipping

`PushClip`:

- reads current `GraphicsDevice.ScissorRectangle`;
- if there is no previous clip, treats the full viewport as previous clip;
- converts requested `DrawRect` to MonoGame `Rectangle`;
- pushes previous clip on `_clipStack`;
- sets scissor rectangle to intersection of previous and requested clip.

`PopClip`:

- if clip stack is empty, returns without throwing;
- otherwise restores the previous scissor rectangle.

Intersection:

- uses max left/top and min right/bottom;
- if intersection is empty or inverted, returns a shared empty `0,0,0,0` rectangle.

Important behavior:

- unbalanced extra `PopClip` is tolerated;
- clipping is nested;
- actual clipping depends on `SpriteBatch.Begin` using `MonoGameDrawingBackend.ScissorRasterizerState`.

### Type Conversions

`DrawRect` -> MonoGame `Rectangle`:

- rounds `X`, `Y`, `Width`, `Height` with `MathF.Round`.

`Color` -> MonoGame `Color`:

- maps `R`, `G`, `B`, `A` directly.

`DrawPoint` -> MonoGame `Vector2`:

- maps `X`, `Y` directly.

### Disposal

`Dispose()`:

- disposes every cached text texture;
- clears the text texture cache.

It does not own or dispose:

- `SpriteBatch`;
- `_whitePixel`;
- external image textures;
- the optional rasterizer.

## Existing Tests As Architecture Contracts

The tests currently enforce these contracts:

- `DrawRect` exposes edges and rejects invalid/overflowing values.
- `DrawPoint` rejects non-finite values.
- `Color` defaults alpha to opaque.
- `DrawingContext` records fill, text, image, and clip commands.
- `DrawRectangle` rejects invalid thickness.
- `DrawTextRun` rejects invalid sizes.
- `DrawCommandList` preserves insertion order and can be cleared.
- `MonoGameImage` rejects null textures.
- `SkiaFont` validates typeface, family name, and size.
- `SystemFontSource` loads system fonts and preserves requested metadata.
- `IDrawingBackend.Render` exists.
- `MonoGameDrawingBackend` implements `IDrawingBackend`.
- text cache keys distinguish font instances.
- `SkiaTextShaper` rejects null text runs and returns valid glyph data.
- `SkiaTextRasterizer` returns pixels, respects text run size, and handles empty text.
- `TextShapeResult` validates array lengths and defensively copies glyph data.
- `RasterizedText` validates dimensions and defensively copies pixels.

If future changes break any of these, they are changing architecture, not just implementation.

## Input Architecture

`UI/Input` has three separate responsibilities:

- represent raw input snapshots and frame transitions;
- define WPF-style routed event metadata and routing;
- provide early command-routing primitives.

It does not yet own:

- hit testing;
- focus selection;
- cursor management;
- drag/drop behavior;
- command manager routing;
- keyboard navigation;
- text composition platform integration beyond queued text strings;
- control-level event hookup.

Those are planned roadmap areas that should build on this foundation instead of replacing it blindly.

## Input Flow

The current intended flow is:

```text
MonoGameInputSource.GetFrame()
        |
        v
InputFrame
        |
        v
future control system chooses target
        |
        v
RoutedEventArgs subclass is created
        |
        v
RoutedEventRouter.Raise(tree, targetId, args)
        |
        v
UiInputTree handlers run according to routing strategy
```

Important boundary:

- `MonoGameInputSource` reads raw hardware state and produces an `InputFrame`;
- it does not raise routed events;
- `RoutedEventRouter` raises routed events;
- it does not read hardware state;
- future controls must bridge between hit-tested controls and routed input dispatch.

## Input Source Contract

File:

- `UI/Input/IInputSource.cs`

`IInputSource` is the backend-neutral source of input frames.

Contract:

```csharp
InputFrame GetFrame();
```

The interface does not expose MonoGame types.

## `MonoGameInputSource`

File:

- `UI/Input/MonoGame/MonoGameInputSource.cs`

`MonoGameInputSource` implements `IInputSource`.

State it owns:

- previous pointer snapshot;
- current pointer snapshot;
- previous keyboard snapshot;
- current keyboard snapshot;
- queued text input events.

Methods:

- `QueueTextInput(string text)`
- `GetFrame()`

`QueueTextInput`:

- wraps text in `TextInputSnapshotEvent`;
- appends it to an internal queue;
- validation happens in `TextInputSnapshotEvent`.

`GetFrame`:

- moves current pointer into previous pointer;
- moves current keyboard into previous keyboard;
- reads a new pointer snapshot from `Mouse.GetState()`;
- reads a new keyboard snapshot from `Keyboard.GetState()`;
- copies queued text input events to an array;
- clears the queue;
- returns an `InputFrame`.

Pointer reading:

- position comes from `MouseState.X` and `MouseState.Y`;
- wheel value comes from `MouseState.ScrollWheelValue`;
- left/middle/right/XButton1/XButton2 are mapped from MonoGame `ButtonState.Pressed`.

Keyboard reading:

- reads MonoGame pressed keys;
- maps each key through `MonoGameInputMapper.MapKey`;
- removes `InputKey.Unknown` and `InputKey.None`;
- creates a `KeyboardSnapshot`.

Important detail:

- text input is queued manually; the source does not itself subscribe to an OS text composition event.

## `MonoGameInputMapper`

File:

- `UI/Input/MonoGame/MonoGameInputMapper.cs`

Maps MonoGame input identifiers to Cerneala identifiers.

`MapKey(Keys key)` maps:

- navigation and editing keys: Back, Tab, Enter, Escape, Space, PageUp, PageDown, End, Home, arrows, Insert, Delete;
- digits D0-D9;
- letters A-Z;
- modifiers LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt;
- F1-F12;
- unknown/unmapped values to `InputKey.Unknown`.

`MapMouseButton(int buttonIndex)` maps:

- `0` -> `InputMouseButton.Left`
- `1` -> `InputMouseButton.Middle`
- `2` -> `InputMouseButton.Right`
- `3` -> `InputMouseButton.XButton1`
- `4` -> `InputMouseButton.XButton2`
- anything else -> `InputMouseButton.None`

Tests currently prove representative key mappings and unknown key fallback. Mouse button mapping exists but is not directly covered by the current test file.

## Raw Input Identifiers

### `InputKey`

File:

- `UI/Input/InputKey.cs`

`InputKey` is Cerneala's backend-neutral keyboard key enum.

It currently includes:

- `Unknown`
- `None`
- Back, Tab, Enter, Escape, Space;
- PageUp, PageDown, End, Home;
- arrows;
- Insert, Delete;
- digits D0-D9;
- letters A-Z;
- left/right Shift, Ctrl, Alt;
- F1-F12.

### `InputMouseButton`

File:

- `UI/Input/InputMouseButton.cs`

Mouse button enum:

- `None`
- `Left`
- `Middle`
- `Right`
- `XButton1`
- `XButton2`

### `InputButtonState`

File:

- `UI/Input/InputButtonState.cs`

Small value object:

- `WasDown`
- `IsDown`
- `IsPressed`
- `IsReleased`

`IsPressed` is true when current is down and previous was not.

`IsReleased` is true when current is not down and previous was down.

Important detail:

- this type exists, but `InputFrame` currently computes transitions directly rather than storing `InputButtonState` values.

## Pointer Snapshots

File:

- `UI/Input/PointerSnapshot.cs`

`PointerSnapshot` is an immutable-style snapshot of pointer state.

State:

- `X`
- `Y`
- `WheelValue`
- button dictionary

Static value:

- `PointerSnapshot.Empty`

Methods:

- `IsDown(InputMouseButton button)`
- `WithPosition(float x, float y)`
- `WithWheelValue(int wheelValue)`
- `WithButton(InputMouseButton button, bool isDown)`

Behavior:

- `InputMouseButton.None` is always treated as not down;
- setting `InputMouseButton.None` returns the same snapshot;
- mutating methods return new snapshots;
- button data is copied when changing a button.

Tests prove:

- `InputMouseButton.None` is ignored;
- frame-level pointer transitions work;
- wheel delta is derived from previous/current snapshots.

## Keyboard Snapshots

File:

- `UI/Input/KeyboardSnapshot.cs`

`KeyboardSnapshot` stores the set of currently down keys.

Static value:

- `KeyboardSnapshot.Empty`

Factory:

```csharp
KeyboardSnapshot.FromDownKeys(IEnumerable<InputKey> keys)
```

Behavior:

- rejects `null` key sequences;
- removes `InputKey.None`;
- removes `InputKey.Unknown`;
- stores unique keys in a set;
- exposes `IsDown(InputKey key)`.

Tests prove frame-level keyboard transitions work through `InputFrame`.

## Text Input Snapshots

File:

- `UI/Input/TextInputSnapshotEvent.cs`

`TextInputSnapshotEvent` is a record for text input captured during an input frame.

Property:

- `Text`

Validation:

- rejects `null`;
- rejects empty strings.

Important detail:

- whitespace text is allowed if it is not empty.

Tests prove:

- text input carries Unicode text;
- null text is rejected;
- `InputFrame` copies text input event lists and exposes immutable/read-only event data.

## `InputFrame`

File:

- `UI/Input/InputFrame.cs`

`InputFrame` represents the previous/current comparison for one input tick.

Constructor inputs:

- previous pointer;
- current pointer;
- previous keyboard;
- current keyboard;
- text input events.

Behavior:

- rejects `null` pointer snapshots;
- rejects `null` keyboard snapshots;
- rejects `null` text event list;
- creates a `PointerFrame`;
- creates a `KeyboardFrame`;
- copies text events to an array;
- exposes text events through `Array.AsReadOnly`.

### `InputFrame.PointerFrame`

Exposes:

- `X`
- `Y`
- `WheelValue`
- `WheelDelta`
- `IsDown(InputMouseButton button)`
- `IsPressed(InputMouseButton button)`
- `IsReleased(InputMouseButton button)`

`WheelDelta` is current wheel value minus previous wheel value.

### `InputFrame.KeyboardFrame`

Exposes:

- `IsDown(InputKey key)`
- `IsPressed(InputKey key)`
- `IsReleased(InputKey key)`

Tests prove:

- mouse press/down/release transitions are computed from previous/current snapshots;
- wheel delta is computed correctly;
- keyboard press/down/release transitions are computed from previous/current snapshots;
- text input events are copied and exposed read-only.

## Routed Event Metadata

### `RoutingStrategy`

File:

- `UI/Input/RoutingStrategy.cs`

Values:

- `Direct`
- `Bubble`
- `Tunnel`

Meaning:

- Direct: target only;
- Bubble: target to root;
- Tunnel: root to target.

### `RoutedEvent`

File:

- `UI/Input/RoutedEvent.cs`

Stores routed event metadata:

- `Name`
- `OwnerType`
- `RoutingStrategy`
- `ArgsType`

Constructor behavior:

- internal;
- rejects empty/whitespace names;
- rejects null owner type;
- rejects null args type.

### `RoutedEventRegistry`

File:

- `UI/Input/RoutedEventRegistry.cs`

Creates `RoutedEvent` metadata through:

```csharp
RoutedEvent Register(string name, Type ownerType, RoutingStrategy routingStrategy, Type argsType)
```

Behavior:

- rejects null args type;
- rejects unsupported enum values;
- requires args type to derive from `RoutedEventArgs`;
- returns a new `RoutedEvent`.

Tests prove:

- registration stores metadata;
- empty event names are rejected;
- null args type is rejected;
- unsupported routing strategies are rejected.

Important limitation:

- this registry does not currently store global event registrations or prevent duplicate names. It is a factory/validator, not a global WPF-style event manager.

## Routed Event Arguments

### `RoutedEventArgs`

File:

- `UI/Input/RoutedEventArgs.cs`

Base class for routed event args.

Constructor inputs:

- `RoutedEvent routedEvent`
- `object originalSource`

Properties:

- `RoutedEvent`
- `OriginalSource`
- `Source`
- `Handled`

Behavior:

- rejects null routed event;
- rejects null original source;
- initializes `Source` to `OriginalSource`;
- `Handled` defaults to false.

During routing, `RoutedEventRouter` mutates `Source` to the current route element.

### Mouse Args

Files:

- `UI/Input/MouseEventArgs.cs`
- `UI/Input/MouseButtonEventArgs.cs`
- `UI/Input/MouseWheelEventArgs.cs`

`MouseEventArgs` adds:

- `X`
- `Y`

`MouseButtonEventArgs` adds:

- `ChangedButton`
- `ClickCount`

`MouseWheelEventArgs` adds:

- `Delta`

Tests prove:

- mouse button args expose button, position, and click count.

### Keyboard Args

Files:

- `UI/Input/KeyEventArgs.cs`
- `UI/Input/KeyboardFocusChangedEventArgs.cs`

`KeyEventArgs` adds:

- `Key`

`KeyboardFocusChangedEventArgs` adds:

- `OldFocus`
- `NewFocus`

### Text Args

File:

- `UI/Input/TextCompositionEventArgs.cs`

Adds:

- `Text`

Behavior:

- rejects null text.

### Command Args

Files:

- `UI/Input/CanExecuteRoutedEventArgs.cs`
- `UI/Input/ExecutedRoutedEventArgs.cs`

`CanExecuteRoutedEventArgs` adds:

- `Command`
- `Parameter`
- settable `CanExecute`

`ExecutedRoutedEventArgs` adds:

- `Command`
- `Parameter`

Both reject null commands.

## Routed Event Catalog

File:

- `UI/Input/InputEvents.cs`

`InputEvents` declares WPF-named routed events for many input categories.

Mouse events include:

- `PreviewMouseDownEvent` tunnel;
- `MouseDownEvent` bubble;
- `PreviewMouseUpEvent` tunnel;
- `MouseUpEvent` bubble;
- `PreviewMouseMoveEvent` tunnel;
- `MouseMoveEvent` bubble;
- `PreviewMouseWheelEvent` tunnel;
- `MouseWheelEvent` bubble;
- `MouseEnterEvent` direct;
- `MouseLeaveEvent` direct;
- capture/cursor events;
- direct left/right button events;
- double click events.

Keyboard/focus/text events include:

- `PreviewKeyDownEvent` tunnel;
- `KeyDownEvent` bubble;
- `PreviewKeyUpEvent` tunnel;
- `KeyUpEvent` bubble;
- preview/non-preview keyboard focus events;
- `GotFocusEvent`;
- `LostFocusEvent`;
- preview/non-preview text input events.

Also listed:

- stylus events;
- touch events;
- manipulation events;
- drag/drop events.

Many non-mouse categories currently use plain `RoutedEventArgs`. The event names exist before full behavior exists.

`InputEvents.All` contains every declared input event.

Tests prove:

- representative WPF event names and routing strategies are present;
- representative stylus/touch/manipulation/drag-drop events are present.

## Command Event Catalog

File:

- `UI/Input/CommandEvents.cs`

Declared command events:

- `PreviewCanExecuteEvent` tunnel;
- `CanExecuteEvent` bubble;
- `PreviewExecutedEvent` tunnel;
- `ExecutedEvent` bubble.

`CommandEvents.All` contains all command events.

Tests prove:

- command events use WPF names and expected routing strategies.

## UI Input Tree

### `UiElementId`

File:

- `UI/Input/UiElementId.cs`

Value type wrapping a string `Value`.

Behavior:

- `ToString()` returns `Value`.

No validation currently rejects null/empty ids.

### `UiInputElement`

File:

- `UI/Input/UiInputElement.cs`

Record containing:

- `Id`
- `ParentId`
- `IsEnabled`

### `UiInputTree`

File:

- `UI/Input/UiInputTree.cs`

`UiInputTree` stores:

- registered input elements by id;
- handlers by `(UiElementId, RoutedEvent)`.

Public methods:

- `Add(UiElementId id, UiElementId? parentId, bool isEnabled = true)`
- `AddHandler(UiElementId id, RoutedEvent routedEvent, RoutedEventHandler handler)`
- `GetRouteToRoot(UiElementId targetId)`

Internal method:

- `GetHandlers(UiElementId id, RoutedEvent routedEvent)`

`Add` behavior:

- parent must already exist if `parentId` is provided;
- duplicate ids are rejected;
- stores enabled state.

`AddHandler` behavior:

- rejects null routed event;
- rejects null handler;
- element must already be registered;
- appends handler in registration order.

`GetRouteToRoot` behavior:

- target must be registered;
- returns list starting at target and walking parent ids until root;
- route order is target, parent, grandparent, root.

`GetHandlers` behavior:

- disabled elements return an empty handler list;
- handler lists are returned as arrays, creating a snapshot for dispatch;
- this means handlers added during dispatch do not run until the next dispatch.

Tests prove:

- bubble routes call target then ancestors;
- tunnel routes call root then target;
- disabled elements do not invoke handlers;
- handlers added during dispatch do not run during the same dispatch.

Important limitation:

- this is not the future visual/logical tree. It is only an input routing tree.

## Routed Event Routing

File:

- `UI/Input/RoutedEventRouter.cs`

Main method:

```csharp
void Raise(UiInputTree tree, UiElementId targetId, RoutedEventArgs args)
```

Behavior:

- rejects null tree;
- rejects null args;
- gets route from target to root;
- selects route based on `args.RoutedEvent.RoutingStrategy`;
- direct route is target only;
- bubble route is target to root;
- tunnel route is root to target;
- unsupported routing strategy throws `InvalidOperationException`;
- before invoking handlers on each route element, sets `args.Source` to that element id;
- stops immediately if `args.Handled` is true;
- stops after any handler sets `Handled`;
- asks tree for handlers so disabled elements are skipped.

Tests prove:

- bubble order;
- tunnel order;
- source mutation for each route element;
- `OriginalSource` remains target/original object;
- handled events stop routing.

Important detail:

- `Source` ends as the last route element that received the event, unless routing stopped earlier.

## Commanding

### `ICommand`

File:

- `UI/Input/ICommand.cs`

Minimal command interface:

- `bool CanExecute(object? parameter)`
- `void Execute(object? parameter)`

This mirrors the shape of WPF commanding without referencing WPF.

### `RoutedCommand`

File:

- `UI/Input/RoutedCommand.cs`

Stores:

- `Name`
- `OwnerType`

Behavior:

- rejects empty/whitespace names;
- rejects null owner type;
- `CanExecute` currently always returns false;
- `Execute` throws `InvalidOperationException`.

Important limitation:

- routed command execution is not implemented yet. The class is metadata plus a deliberate failure path until command routing exists.

Tests prove:

- name and owner are stored.

### `CommandBinding`

File:

- `UI/Input/CommandBinding.cs`

Stores:

- `Command`
- optional executed handler;
- optional can-execute handler.

Constructor behavior:

- rejects null command.

Methods:

- `OnExecuted(UiElementId sender, ExecutedRoutedEventArgs args)`
- `OnCanExecute(UiElementId sender, CanExecuteRoutedEventArgs args)`

Behavior:

- rejects null args;
- compares command by reference;
- ignores args for different command instances;
- invokes matching executed handler if present;
- invokes matching can-execute handler if present.

Tests prove:

- executed handlers are invoked for matching commands;
- executed args for different commands are ignored;
- can-execute args for different commands are ignored;
- can-execute args can set `CanExecute` and `Handled`.

## Existing Input Tests As Architecture Contracts

The input tests currently enforce these contracts:

- pointer button transitions are computed from previous/current snapshots;
- `InputMouseButton.None` is ignored;
- mouse wheel delta is current minus previous wheel value;
- keyboard transitions are computed from previous/current snapshots;
- text input events carry Unicode text;
- text input rejects null text;
- `InputFrame` copies text input events;
- exposed input frame text events are read-only;
- WPF-style input event names and routing strategies exist;
- representative stylus/touch/manipulation/drag-drop event names exist;
- command routed event names and routing strategies exist;
- mouse button args expose changed button, position, and click count;
- routed event registration stores metadata;
- routed event registration rejects invalid names, args types, and routing strategies;
- routed event args default `Source` to `OriginalSource`;
- bubble routes target to root;
- tunnel routes root to target;
- handled events stop routing;
- disabled elements do not invoke handlers;
- handler snapshots prevent newly added handlers from running during the same dispatch;
- routed commands store name and owner;
- command bindings dispatch only for the matching command instance;
- MonoGame key mapping maps representative keys and returns `Unknown` for unmapped keys.

If future changes break any of these, they are changing architecture, not just implementation.

## What Input Is Not

`UI/Input` is not yet:

- a full WPF `InputManager`;
- a full WPF `CommandManager`;
- a focus manager;
- a keyboard navigation engine;
- a drag/drop implementation;
- a cursor system;
- a gesture system;
- a hit-testing system;
- a control event bridge;
- a text IME/composition engine;
- a global event registry with duplicate detection;
- a visual tree.

It provides the lower-level pieces those systems should use.

## Relationship Between Input And Future Controls

Future controls should use this boundary:

```text
MonoGameInputSource
        |
        v
InputFrame
        |
        v
Control hit test / focus manager
        |
        v
RoutedEventArgs subclass
        |
        v
RoutedEventRouter + UiInputTree
        |
        v
control handlers / command bindings
```

Future `UIElement` should probably bridge to `UiInputTree`, not replace it blindly.

Future `InputManager`, `Keyboard`, `Mouse`, `FocusManager`, `KeyboardNavigation`, `CommandManager`, `DragDrop`, `Cursor`, and `ControlInputBridge` should clarify whether they:

- wrap existing snapshot APIs;
- build routes through `UiInputTree`;
- add missing state such as focus/capture;
- or replace a current temporary primitive.

Do not add another parallel routed-event tree unless there is a clear reason. That would be a futere futere de cap.

## Input Risk Areas

### Duplicate Event Management

`RoutedEventRegistry` currently validates and creates routed event metadata. It does not behave like WPF's full `EventManager`.

If `EventManager.cs` is added later, decide whether it replaces `RoutedEventRegistry`, wraps it, or becomes the global storage layer above it.

### Duplicate Input Trees

`UiInputTree` already stores parent relationships and handlers for routed input. Future visual/logical trees must either feed this tree or intentionally replace it.

Do not maintain two unrelated trees with different parent relationships unless there is a bridge that proves they stay synchronized.

### Command Routing Is Not Finished

`RoutedCommand` currently cannot execute directly. `CommandBinding` can respond to routed command args, but no `CommandManager` walks routes yet.

Future command work should complete route-based query/execute behavior rather than pretending `RoutedCommand.Execute` already works.

### Text Input Is Queued, Not Composed

`MonoGameInputSource.QueueTextInput` accepts text strings. There is no IME/composition lifecycle yet. Future `TextCompositionManager`-style work should preserve `TextInputSnapshotEvent` as the simple frame payload or explicitly replace it.

### WPF Event Names Exist Before Behavior

`InputEvents` includes many WPF-style event names for stylus, touch, manipulation, and drag/drop. Most of these currently have only metadata. Do not assume their full WPF behavior exists just because the routed event name exists.

## What Drawing Is Not

`Drawing` is not:

- a WPF visual tree;
- a layout system;
- a control framework;
- a dependency property system;
- a styling or templating system;
- a retained scene graph;
- a resource dictionary;
- an animation system;
- a data binding system;
- a general vector graphics API yet.

It currently supports only a small set of command primitives:

- fill rectangle;
- stroke rectangle;
- draw text;
- draw image;
- push clip;
- pop clip.

## Relationship To The Roadmap

The roadmap contains WPF-style classes such as:

- `Point`
- `Rect`
- `Color`
- `Brush`
- `Pen`
- `Geometry`
- `Shape`
- `ImageSource`
- `GlyphRun`
- `FormattedText`
- `TextFormatter`
- `DrawingVisual`
- `Drawing`

These must not be implemented blindly as duplicate wrappers around existing `Draw*` types.

### Classes That Existing Drawing Makes Suspicious

These roadmap ideas overlap heavily with existing drawing primitives:

- `UI/Controls/Point.cs` overlaps with `DrawPoint`.
- `UI/Controls/Rect.cs` overlaps with `DrawRect`.
- `UI/Media/Color.cs` overlaps with `Color`.
- `UI/Media/GlyphRun.cs` overlaps partly with `TextShapeResult`.
- `UI/FormattedText.cs` and `UI/Text/TextFormatter.cs` overlap partly with `DrawTextRun`, `SkiaTextShaper`, and `SkiaTextRasterizer`.
- `UI/Media/BitmapSource.cs` overlaps partly with `IDrawImage` if it only exposes width and height.

The right question for every roadmap class is:

```text
Is this a real higher-level WPF-style concept,
or just a second name for an existing Draw* primitive?
```

If it is just a second name, do not add it. Either reuse the existing type, rename/promote the existing type, or add a clear conversion boundary.

### Recommended Boundary

Keep this mental model:

```text
WPF-style API layer:
  UIElement, Shape, Brush, Color, ImageSource, TextBlock

Drawing command layer:
  DrawingContext, DrawCommandList, DrawCommand, DrawRect, Color

Backend layer:
  MonoGameDrawingBackend, MonoGameImage, SpriteBatch
```

The WPF-style layer may translate its richer concepts into drawing commands. The drawing layer should remain small and backend-neutral. The backend should remain the only place that knows MonoGame details.

## Rules For Future Work

- Do not call `SpriteBatch` outside a backend.
- Do not call Skia or HarfBuzz from controls.
- Do not add WPF-style classes that only duplicate `DrawPoint`, `DrawRect`, or `Color`.
- Do not make `DrawingContext` own layout or control state.
- Do not make `DrawCommandList` a visual tree.
- Do not add backend-specific types to `DrawCommand`.
- Do not skip tests when adding a new command kind.
- If a roadmap class overlaps with drawing, document whether it wraps, replaces, or translates to the drawing type.

## Current Risk Areas

### Duplicate Geometry

The roadmap currently contains planned control geometry types. Existing drawing already has `DrawPoint` and `DrawRect`.

Before adding `Point` or `Rect`, decide whether the project wants:

- one shared geometry API used by both layout and drawing;
- separate layout and drawing geometry with explicit conversion;
- renamed drawing primitives promoted into WPF-style names.

Do not leave two identical geometry systems without a reason.

### Duplicate Color

`Color` is already enough for immediate RGBA drawing. A future WPF-style `Color` is only useful if it becomes the public media color model and `Color` becomes an internal/backend command color or conversion target.

### Text API Layering

The low-level text pipeline already shapes and rasterizes text. Future WPF-style text APIs should use it instead of rebuilding shaping/rasterization.

Good future direction:

```text
TextBlock / FormattedText / GlyphRun
        |
        v
DrawTextRun
        |
        v
SkiaTextShaper / SkiaTextRasterizer
        |
        v
MonoGameDrawingBackend
```

### Image API Layering

`IDrawImage` is intentionally tiny. Future `ImageSource` and `BitmapSource` can exist, but they need real responsibilities such as decoding, source identity, caching, metadata, pixel access, or backend image creation. If they only expose `Width` and `Height`, they duplicate `IDrawImage`.

## Summary

`Drawing` is already a clean low-level command architecture:

- `DrawingContext` records;
- `DrawCommandList` stores;
- `DrawCommand` describes;
- `IDrawingBackend` renders;
- text is shaped/rasterized through Skia/HarfBuzz;
- MonoGame performs the final draw.

Future WPF-style APIs should sit above this layer and translate into it. They should not duplicate it casually.
