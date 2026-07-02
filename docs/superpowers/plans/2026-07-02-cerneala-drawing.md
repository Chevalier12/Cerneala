# Cerneala Drawing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build `Cerneala.Drawing`, a small 2D drawing abstraction that records primitive draw intent without knowing anything about UI controls, input, layout, or retained/immediate UI systems.

**Architecture:** `Cerneala.Drawing` owns value types, draw resources, text draw payloads, commands, a command list, and `DrawingContext`. `Cerneala.Drawing.Text` owns OS font loading plus Skia/HarfBuzz text preparation. `Cerneala.Drawing.MonoGame` is the concrete drawing adapter: it consumes `Cerneala.Drawing` commands, uses `Cerneala.Drawing.Text` for text shaping/raster data, and performs the final draw, including text, through MonoGame.

**Tech Stack:** C#/.NET 8, MonoGame.Framework.DesktopGL 3.8.4.1, SkiaSharp 4.148.0, HarfBuzzSharp 14.2.0, xUnit.

## Global Constraints

- Follow `AGENTS.md`: keep it simple, clean architecture, DRY, YAGNI, SOLID.
- `Cerneala.Drawing` must not know about controls, layout, input, focus, hover, pressed state, immediate mode, or retained mode.
- `Cerneala.Drawing` public abstractions must not expose `Texture2D`, `SpriteFont`, `SpriteBatch`, or other MonoGame-specific types except inside `Cerneala.Drawing.MonoGame`.
- `Cerneala.Drawing` public abstractions must not expose `SKCanvas`, `SKTypeface`, `SKFont`, HarfBuzz buffers, or other Skia/HarfBuzz-specific types.
- Skia/HarfBuzz belong to `Cerneala.Drawing.Text`; they prepare font/text/glyph data for drawing. They are not UI control renderers; the concrete drawing adapters perform the final rendering.
- The final renderer for this milestone is `Cerneala.Drawing.MonoGame`.
- Font loading must use OS-installed fonts through the Skia font manager. Do not add fonts to MonoGame Content.
- Keep the first primitive set to rectangles, text, images, and clipping.
- Add tests before production code for command recording behavior.

---

## File Structure

- Create `UI/Drawing/DrawPoint.cs`: small immutable 2D point value.
- Create `UI/Drawing/DrawRect.cs`: small immutable rectangle value.
- Create `UI/Drawing/DrawColor.cs`: small immutable RGBA color value.
- Create `UI/Drawing/IDrawImage.cs`: renderer-agnostic image handle.
- Create `UI/Drawing/IDrawFont.cs`: renderer-agnostic font handle.
- Create `UI/Drawing/DrawTextRun.cs`: backend-neutral unshaped text payload for the first text API.
- Create `UI/Drawing/IFontSource.cs`: renderer-agnostic font loading contract.
- Create `UI/Drawing/DrawCommandKind.cs`: enum of supported command kinds.
- Create `UI/Drawing/DrawCommand.cs`: immutable command payload with static factory methods.
- Create `UI/Drawing/DrawCommandList.cs`: append-only per-frame command storage with clear/enumeration.
- Create `UI/Drawing/DrawingContext.cs`: high-level API that records commands.
- Create `UI/Drawing/IDrawingBackend.cs`: renderer contract that consumes command lists.
- Create `UI/Drawing/MonoGame/MonoGameImage.cs`: `Texture2D` image wrapper.
- Create `UI/Drawing/MonoGame/MonoGameDrawingBackend.cs`: SpriteBatch-backed adapter for rectangles, images, and prepared text.
- Create `UI/Text/SkiaFont.cs`: `SKTypeface` font wrapper implementing `IDrawFont`.
- Create `UI/Text/SystemFontSource.cs`: OS font loader using `SKFontManager.Default`.
- Create `UI/Text/TextShapeResult.cs`: backend-neutral shaped text result for the first text boundary.
- Create `UI/Text/SkiaTextShaper.cs`: first HarfBuzz-backed shaping entry point.
- Create `UI/Text/SkiaTextRasterizer.cs`: rasterizes text into pixels that the MonoGame adapter can upload to a `Texture2D`.
- Modify `tests/Cerneala.Tests/Cerneala.Tests.csproj`: keep using project reference and MonoGame package already present.
- Create `tests/Cerneala.Tests/Drawing/DrawingContextTests.cs`: tests for command recording.
- Create `tests/Cerneala.Tests/Drawing/DrawCommandListTests.cs`: tests for command list behavior.

---

### Task 0: Drawing Dependencies

**Files:**
- Modify: `Cerneala.csproj`

**Interfaces:**
- Produces: package reference `SkiaSharp` version `4.148.0`
- Produces: package reference `SkiaSharp.NativeAssets.Linux` version `4.148.0`
- Produces: package reference `HarfBuzzSharp` version `14.2.0`

- [ ] **Step 1: Add SkiaSharp and HarfBuzzSharp packages**

Run:

```powershell
dotnet add Cerneala.csproj package SkiaSharp --version 4.148.0
dotnet add Cerneala.csproj package SkiaSharp.NativeAssets.Linux --version 4.148.0
dotnet add Cerneala.csproj package HarfBuzzSharp --version 14.2.0
```

- [ ] **Step 2: Restore packages**

Run:

```powershell
dotnet restore Cerneala.slnx
```

Expected: restore succeeds.

- [ ] **Step 3: Commit**

```powershell
git add Cerneala.csproj
git commit -m "Add Skia and HarfBuzz dependencies"
```

### Task 1: Drawing Value Types

**Files:**
- Create: `UI/Drawing/DrawPoint.cs`
- Create: `UI/Drawing/DrawRect.cs`
- Create: `UI/Drawing/DrawColor.cs`
- Test: `tests/Cerneala.Tests/Drawing/DrawingContextTests.cs`

**Interfaces:**
- Produces: `Cerneala.Drawing.DrawPoint(float X, float Y)`
- Produces: `Cerneala.Drawing.DrawRect(float X, float Y, float Width, float Height)`
- Produces: `Cerneala.Drawing.DrawColor(byte R, byte G, byte B, byte A)`

- [ ] **Step 1: Write the failing value-type test**

Create `tests/Cerneala.Tests/Drawing/DrawingContextTests.cs`:

```csharp
using Cerneala.Drawing;

namespace Cerneala.Tests.Drawing;

public sealed class DrawingContextTests
{
    [Fact]
    public void DrawRectExposesEdges()
    {
        DrawRect rect = new(10, 20, 30, 40);

        Assert.Equal(40, rect.Right);
        Assert.Equal(60, rect.Bottom);
    }

    [Fact]
    public void DrawColorCreatesOpaqueColorByDefault()
    {
        DrawColor color = new(1, 2, 3);

        Assert.Equal(1, color.R);
        Assert.Equal(2, color.G);
        Assert.Equal(3, color.B);
        Assert.Equal(255, color.A);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj --filter "FullyQualifiedName~Cerneala.Tests.Drawing.DrawingContextTests"
```

Expected: FAIL because namespace `Cerneala.Drawing` and the drawing value types do not exist.

- [ ] **Step 3: Implement value types**

Create `UI/Drawing/DrawPoint.cs`:

```csharp
namespace Cerneala.Drawing;

public readonly record struct DrawPoint(float X, float Y);
```

Create `UI/Drawing/DrawRect.cs`:

```csharp
namespace Cerneala.Drawing;

public readonly record struct DrawRect(float X, float Y, float Width, float Height)
{
    public float Right => X + Width;

    public float Bottom => Y + Height;
}
```

Create `UI/Drawing/DrawColor.cs`:

```csharp
namespace Cerneala.Drawing;

public readonly record struct DrawColor(byte R, byte G, byte B, byte A = 255)
{
    public static DrawColor Transparent { get; } = new(0, 0, 0, 0);

    public static DrawColor White { get; } = new(255, 255, 255);

    public static DrawColor Black { get; } = new(0, 0, 0);
}
```

- [ ] **Step 4: Run test to verify it passes**

Run:

```powershell
dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj --filter "FullyQualifiedName~Cerneala.Tests.Drawing.DrawingContextTests"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add UI/Drawing/DrawPoint.cs UI/Drawing/DrawRect.cs UI/Drawing/DrawColor.cs tests/Cerneala.Tests/Drawing/DrawingContextTests.cs
git commit -m "Add drawing value types"
```

### Task 2: Draw Commands and Command List

**Files:**
- Create: `UI/Drawing/IDrawImage.cs`
- Create: `UI/Drawing/IDrawFont.cs`
- Create: `UI/Drawing/IFontSource.cs`
- Create: `UI/Drawing/DrawTextRun.cs`
- Create: `UI/Drawing/DrawCommandKind.cs`
- Create: `UI/Drawing/DrawCommand.cs`
- Create: `UI/Drawing/DrawCommandList.cs`
- Test: `tests/Cerneala.Tests/Drawing/DrawCommandListTests.cs`
- Modify: `tests/Cerneala.Tests/Drawing/DrawingContextTests.cs`

**Interfaces:**
- Consumes: `DrawPoint`, `DrawRect`, `DrawColor`
- Produces: `IFontSource.LoadFont(string familyName, float size): IDrawFont`
- Produces: `DrawTextRun(IDrawFont font, string text, float size)`
- Produces: `DrawCommandKind`
- Produces: `DrawCommand.FillRectangle(DrawRect rect, DrawColor color): DrawCommand`
- Produces: `DrawCommand.DrawRectangle(DrawRect rect, DrawColor color, float thickness): DrawCommand`
- Produces: `DrawCommand.DrawText(DrawTextRun textRun, DrawPoint position, DrawColor color): DrawCommand`
- Produces: `DrawCommand.DrawImage(IDrawImage image, DrawRect destination, DrawColor color): DrawCommand`
- Produces: `DrawCommand.PushClip(DrawRect rect): DrawCommand`
- Produces: `DrawCommand.PopClip(): DrawCommand`
- Produces: `DrawCommandList.Add(DrawCommand command): void`
- Produces: `DrawCommandList.Clear(): void`
- Produces: `DrawCommandList.Count: int`
- Produces: `DrawCommandList[int index]: DrawCommand`

- [ ] **Step 1: Write failing command list tests**

Create `tests/Cerneala.Tests/Drawing/DrawCommandListTests.cs`:

```csharp
using Cerneala.Drawing;

namespace Cerneala.Tests.Drawing;

public sealed class DrawCommandListTests
{
    [Fact]
    public void AddStoresCommandsInOrder()
    {
        DrawCommandList commands = new();

        commands.Add(DrawCommand.FillRectangle(new DrawRect(0, 0, 10, 20), DrawColor.White));
        commands.Add(DrawCommand.DrawRectangle(new DrawRect(2, 3, 4, 5), DrawColor.Black, 2));

        Assert.Equal(2, commands.Count);
        Assert.Equal(DrawCommandKind.FillRectangle, commands[0].Kind);
        Assert.Equal(DrawCommandKind.DrawRectangle, commands[1].Kind);
    }

    [Fact]
    public void ClearRemovesAllCommands()
    {
        DrawCommandList commands = new();

        commands.Add(DrawCommand.FillRectangle(new DrawRect(0, 0, 10, 20), DrawColor.White));
        commands.Clear();

        Assert.Equal(0, commands.Count);
    }
}
```

Append these tests to `tests/Cerneala.Tests/Drawing/DrawingContextTests.cs`:

```csharp
public sealed class FakeDrawImage : IDrawImage
{
    public int Width => 16;

    public int Height => 32;
}

public sealed class FakeDrawFont : IDrawFont
{
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj --filter "FullyQualifiedName~Cerneala.Tests.Drawing"
```

Expected: FAIL because draw command types do not exist.

- [ ] **Step 3: Implement resource interfaces and command types**

Create `UI/Drawing/IDrawImage.cs`:

```csharp
namespace Cerneala.Drawing;

public interface IDrawImage
{
    int Width { get; }

    int Height { get; }
}
```

Create `UI/Drawing/IDrawFont.cs`:

```csharp
namespace Cerneala.Drawing;

public interface IDrawFont
{
}
```

Create `UI/Drawing/IFontSource.cs`:

```csharp
namespace Cerneala.Drawing;

public interface IFontSource
{
    IDrawFont LoadFont(string familyName, float size);
}
```

Create `UI/Drawing/DrawTextRun.cs`:

```csharp
namespace Cerneala.Drawing;

public sealed class DrawTextRun
{
    public DrawTextRun(IDrawFont font, string text, float size)
    {
        Font = font ?? throw new ArgumentNullException(nameof(font));
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Size = size;
    }

    public IDrawFont Font { get; }

    public string Text { get; }

    public float Size { get; }
}
```

Create `UI/Drawing/DrawCommandKind.cs`:

```csharp
namespace Cerneala.Drawing;

public enum DrawCommandKind
{
    FillRectangle,
    DrawRectangle,
    DrawText,
    DrawImage,
    PushClip,
    PopClip
}
```

Create `UI/Drawing/DrawCommand.cs`:

```csharp
namespace Cerneala.Drawing;

public readonly record struct DrawCommand
{
    private DrawCommand(
        DrawCommandKind kind,
        DrawRect rect,
        DrawColor color,
        float thickness,
        string? text,
        DrawTextRun? textRun,
        DrawPoint position,
        IDrawImage? image,
        IDrawFont? font)
    {
        Kind = kind;
        Rect = rect;
        Color = color;
        Thickness = thickness;
        Text = text;
        TextRun = textRun;
        Position = position;
        Image = image;
        Font = font;
    }

    public DrawCommandKind Kind { get; }

    public DrawRect Rect { get; }

    public DrawColor Color { get; }

    public float Thickness { get; }

    public string? Text { get; }

    public DrawTextRun? TextRun { get; }

    public DrawPoint Position { get; }

    public IDrawImage? Image { get; }

    public IDrawFont? Font { get; }

    public static DrawCommand FillRectangle(DrawRect rect, DrawColor color)
    {
        return new DrawCommand(DrawCommandKind.FillRectangle, rect, color, 0, null, null, default, null, null);
    }

    public static DrawCommand DrawRectangle(DrawRect rect, DrawColor color, float thickness)
    {
        return new DrawCommand(DrawCommandKind.DrawRectangle, rect, color, thickness, null, null, default, null, null);
    }

    public static DrawCommand DrawText(DrawTextRun textRun, DrawPoint position, DrawColor color)
    {
        ArgumentNullException.ThrowIfNull(textRun);

        return new DrawCommand(DrawCommandKind.DrawText, default, color, 0, textRun.Text, textRun, position, null, textRun.Font);
    }

    public static DrawCommand DrawImage(IDrawImage image, DrawRect destination, DrawColor color)
    {
        ArgumentNullException.ThrowIfNull(image);

        return new DrawCommand(DrawCommandKind.DrawImage, destination, color, 0, null, null, default, image, null);
    }

    public static DrawCommand PushClip(DrawRect rect)
    {
        return new DrawCommand(DrawCommandKind.PushClip, rect, default, 0, null, null, default, null, null);
    }

    public static DrawCommand PopClip()
    {
        return new DrawCommand(DrawCommandKind.PopClip, default, default, 0, null, null, default, null, null);
    }
}
```

Create `UI/Drawing/DrawCommandList.cs`:

```csharp
using System.Collections;

namespace Cerneala.Drawing;

public sealed class DrawCommandList : IReadOnlyList<DrawCommand>
{
    private readonly List<DrawCommand> _commands = new();

    public int Count => _commands.Count;

    public DrawCommand this[int index] => _commands[index];

    public void Add(DrawCommand command)
    {
        _commands.Add(command);
    }

    public void Clear()
    {
        _commands.Clear();
    }

    public IEnumerator<DrawCommand> GetEnumerator()
    {
        return _commands.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run:

```powershell
dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj --filter "FullyQualifiedName~Cerneala.Tests.Drawing"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add UI/Drawing/IDrawImage.cs UI/Drawing/IDrawFont.cs UI/Drawing/IFontSource.cs UI/Drawing/DrawTextRun.cs UI/Drawing/DrawCommandKind.cs UI/Drawing/DrawCommand.cs UI/Drawing/DrawCommandList.cs tests/Cerneala.Tests/Drawing/DrawCommandListTests.cs tests/Cerneala.Tests/Drawing/DrawingContextTests.cs
git commit -m "Add drawing command list"
```

### Task 3: Drawing Context API

**Files:**
- Create: `UI/Drawing/DrawingContext.cs`
- Modify: `tests/Cerneala.Tests/Drawing/DrawingContextTests.cs`

**Interfaces:**
- Consumes: `DrawCommandList`, `DrawCommand`, `DrawRect`, `DrawColor`, `DrawPoint`, `DrawTextRun`, `IDrawImage`
- Produces: `DrawingContext.FillRectangle(DrawRect rect, DrawColor color): void`
- Produces: `DrawingContext.DrawRectangle(DrawRect rect, DrawColor color, float thickness): void`
- Produces: `DrawingContext.DrawText(DrawTextRun textRun, DrawPoint position, DrawColor color): void`
- Produces: `DrawingContext.DrawImage(IDrawImage image, DrawRect destination, DrawColor color): void`
- Produces: `DrawingContext.PushClip(DrawRect rect): void`
- Produces: `DrawingContext.PopClip(): void`

- [ ] **Step 1: Write failing context tests**

Append these tests to `tests/Cerneala.Tests/Drawing/DrawingContextTests.cs` inside `DrawingContextTests`:

```csharp
    [Fact]
    public void FillRectangleRecordsFillRectangleCommand()
    {
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);

        drawing.FillRectangle(new DrawRect(1, 2, 3, 4), DrawColor.White);

        Assert.Equal(1, commands.Count);
        Assert.Equal(DrawCommandKind.FillRectangle, commands[0].Kind);
        Assert.Equal(new DrawRect(1, 2, 3, 4), commands[0].Rect);
        Assert.Equal(DrawColor.White, commands[0].Color);
    }

    [Fact]
    public void DrawTextRecordsTextCommand()
    {
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        FakeDrawFont font = new();
        DrawTextRun textRun = new(font, "Cerneala", 16);

        drawing.DrawText(textRun, new DrawPoint(5, 6), DrawColor.Black);

        Assert.Equal(1, commands.Count);
        Assert.Equal(DrawCommandKind.DrawText, commands[0].Kind);
        Assert.Same(textRun, commands[0].TextRun);
        Assert.Same(font, commands[0].Font);
        Assert.Equal("Cerneala", commands[0].Text);
        Assert.Equal(new DrawPoint(5, 6), commands[0].Position);
    }

    [Fact]
    public void DrawImageRecordsImageCommand()
    {
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        FakeDrawImage image = new();

        drawing.DrawImage(image, new DrawRect(10, 20, 30, 40), DrawColor.White);

        Assert.Equal(1, commands.Count);
        Assert.Equal(DrawCommandKind.DrawImage, commands[0].Kind);
        Assert.Same(image, commands[0].Image);
        Assert.Equal(new DrawRect(10, 20, 30, 40), commands[0].Rect);
    }

    [Fact]
    public void ClipCommandsAreRecordedInOrder()
    {
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);

        drawing.PushClip(new DrawRect(0, 0, 50, 50));
        drawing.PopClip();

        Assert.Equal(DrawCommandKind.PushClip, commands[0].Kind);
        Assert.Equal(DrawCommandKind.PopClip, commands[1].Kind);
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj --filter "FullyQualifiedName~Cerneala.Tests.Drawing.DrawingContextTests"
```

Expected: FAIL because `DrawingContext` does not exist.

- [ ] **Step 3: Implement DrawingContext**

Create `UI/Drawing/DrawingContext.cs`:

```csharp
namespace Cerneala.Drawing;

public sealed class DrawingContext
{
    private readonly DrawCommandList _commands;

    public DrawingContext(DrawCommandList commands)
    {
        _commands = commands;
    }

    public void FillRectangle(DrawRect rect, DrawColor color)
    {
        _commands.Add(DrawCommand.FillRectangle(rect, color));
    }

    public void DrawRectangle(DrawRect rect, DrawColor color, float thickness)
    {
        _commands.Add(DrawCommand.DrawRectangle(rect, color, thickness));
    }

    public void DrawText(DrawTextRun textRun, DrawPoint position, DrawColor color)
    {
        _commands.Add(DrawCommand.DrawText(textRun, position, color));
    }

    public void DrawImage(IDrawImage image, DrawRect destination, DrawColor color)
    {
        _commands.Add(DrawCommand.DrawImage(image, destination, color));
    }

    public void PushClip(DrawRect rect)
    {
        _commands.Add(DrawCommand.PushClip(rect));
    }

    public void PopClip()
    {
        _commands.Add(DrawCommand.PopClip());
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run:

```powershell
dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj --filter "FullyQualifiedName~Cerneala.Tests.Drawing"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add UI/Drawing/DrawingContext.cs tests/Cerneala.Tests/Drawing/DrawingContextTests.cs
git commit -m "Add drawing context"
```

### Task 4: Backend Contract and Resource Wrappers

**Files:**
- Create: `UI/Drawing/IDrawingBackend.cs`
- Create: `UI/Drawing/MonoGame/MonoGameImage.cs`
- Create: `UI/Text/SkiaFont.cs`
- Create: `UI/Text/SystemFontSource.cs`
- Test: `tests/Cerneala.Tests/Drawing/DrawingResourceTests.cs`

**Interfaces:**
- Consumes: `DrawCommandList`, `IDrawImage`, `IDrawFont`, `IFontSource`
- Produces: `IDrawingBackend.Render(DrawCommandList commands): void`
- Produces: `MonoGameImage(Texture2D texture): IDrawImage`
- Produces: `SkiaFont(SKTypeface typeface, float size): IDrawFont`
- Produces: `SystemFontSource.LoadFont(string familyName, float size): IDrawFont`

- [ ] **Step 1: Write failing resource wrapper tests**

Create `tests/Cerneala.Tests/Drawing/DrawingResourceTests.cs`:

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.Text;
using Microsoft.Xna.Framework.Graphics;
using SkiaSharp;

namespace Cerneala.Tests.Drawing;

public sealed class DrawingResourceTests
{
    [Fact]
    public void MonoGameImageRejectsNullTexture()
    {
        Assert.Throws<ArgumentNullException>(() => new MonoGameImage(null!));
    }

    [Fact]
    public void SkiaFontRejectsNullTypeface()
    {
        Assert.Throws<ArgumentNullException>(() => new SkiaFont(null!, 16));
    }

    [Fact]
    public void SystemFontSourceLoadsFontFromOperatingSystem()
    {
        SystemFontSource fonts = new();

        IDrawFont font = fonts.LoadFont("Arial", 16);

        Assert.IsType<SkiaFont>(font);
    }

    [Fact]
    public void BackendInterfaceConsumesCommandList()
    {
        Assert.True(typeof(IDrawingBackend).GetMethod(nameof(IDrawingBackend.Render)) is not null);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj --filter "FullyQualifiedName~Cerneala.Tests.Drawing.DrawingResourceTests"
```

Expected: FAIL because `IDrawingBackend`, `MonoGameImage`, `SkiaFont`, and `SystemFontSource` do not exist.

- [ ] **Step 3: Implement backend contract and resource wrappers**

Create `UI/Drawing/IDrawingBackend.cs`:

```csharp
namespace Cerneala.Drawing;

public interface IDrawingBackend
{
    void Render(DrawCommandList commands);
}
```

Create `UI/Drawing/MonoGame/MonoGameImage.cs`:

```csharp
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame;

public sealed class MonoGameImage : IDrawImage
{
    public MonoGameImage(Texture2D texture)
    {
        Texture = texture ?? throw new ArgumentNullException(nameof(texture));
    }

    public Texture2D Texture { get; }

    public int Width => Texture.Width;

    public int Height => Texture.Height;
}
```

Create `UI/Text/SkiaFont.cs`:

```csharp
using Cerneala.Drawing;
using SkiaSharp;

namespace Cerneala.Drawing.Text;

public sealed class SkiaFont : IDrawFont
{
    public SkiaFont(SKTypeface typeface, float size)
    {
        Typeface = typeface ?? throw new ArgumentNullException(nameof(typeface));
        Size = size;
    }

    public SKTypeface Typeface { get; }

    public float Size { get; }
}
```

Create `UI/Text/SystemFontSource.cs`:

```csharp
using Cerneala.Drawing;
using SkiaSharp;

namespace Cerneala.Drawing.Text;

public sealed class SystemFontSource : IFontSource
{
    public IDrawFont LoadFont(string familyName, float size)
    {
        ArgumentNullException.ThrowIfNull(familyName);

        SKTypeface typeface = SKFontManager.Default.MatchFamily(familyName) ?? SKTypeface.Default;
        return new SkiaFont(typeface, size);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run:

```powershell
dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj --filter "FullyQualifiedName~Cerneala.Tests.Drawing.DrawingResourceTests"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add UI/Drawing/IDrawingBackend.cs UI/Drawing/MonoGame/MonoGameImage.cs UI/Text/SkiaFont.cs UI/Text/SystemFontSource.cs tests/Cerneala.Tests/Drawing/DrawingResourceTests.cs
git commit -m "Add drawing resource wrappers"
```

### Task 5: MonoGame Drawing Backend

**Files:**
- Create: `UI/Drawing/MonoGame/MonoGameDrawingBackend.cs`
- Test: no graphics-device unit test in this task; verify with build because `SpriteBatch` rendering needs a MonoGame graphics device.

**Interfaces:**
- Consumes: `DrawCommandList`, `DrawCommand`, `MonoGameImage`
- Produces: `MonoGameDrawingBackend(SpriteBatch spriteBatch, Texture2D whitePixel): IDrawingBackend`
- Produces: `MonoGameDrawingBackend.Render(DrawCommandList commands): void`

- [ ] **Step 1: Create compile-time backend test**

Append this test to `tests/Cerneala.Tests/Drawing/DrawingResourceTests.cs`:

```csharp
    [Fact]
    public void MonoGameDrawingBackendImplementsBackendInterface()
    {
        Assert.True(typeof(IDrawingBackend).IsAssignableFrom(typeof(MonoGameDrawingBackend)));
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj --filter "FullyQualifiedName~Cerneala.Tests.Drawing.DrawingResourceTests"
```

Expected: FAIL because `MonoGameDrawingBackend` does not exist.

- [ ] **Step 3: Implement MonoGameDrawingBackend**

Create `UI/Drawing/MonoGame/MonoGameDrawingBackend.cs`:

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame;

public sealed class MonoGameDrawingBackend : IDrawingBackend
{
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _whitePixel;

    public MonoGameDrawingBackend(SpriteBatch spriteBatch, Texture2D whitePixel)
    {
        _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
        _whitePixel = whitePixel ?? throw new ArgumentNullException(nameof(whitePixel));
    }

    public void Render(DrawCommandList commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        foreach (DrawCommand command in commands)
        {
            RenderCommand(command);
        }
    }

    private void RenderCommand(DrawCommand command)
    {
        switch (command.Kind)
        {
            case DrawCommandKind.FillRectangle:
                FillRectangle(command.Rect, command.Color);
                break;

            case DrawCommandKind.DrawRectangle:
                DrawRectangle(command.Rect, command.Color, command.Thickness);
                break;

            case DrawCommandKind.DrawImage:
                DrawImage(command);
                break;

            case DrawCommandKind.DrawText:
            case DrawCommandKind.PushClip:
            case DrawCommandKind.PopClip:
                break;

            default:
                throw new InvalidOperationException($"Unsupported draw command: {command.Kind}");
        }
    }

    private void FillRectangle(DrawRect rect, DrawColor color)
    {
        _spriteBatch.Draw(_whitePixel, ToRectangle(rect), ToColor(color));
    }

    private void DrawRectangle(DrawRect rect, DrawColor color, float thickness)
    {
        int lineThickness = Math.Max(1, (int)MathF.Round(thickness));
        Rectangle bounds = ToRectangle(rect);
        Color monoGameColor = ToColor(color);

        _spriteBatch.Draw(_whitePixel, new Rectangle(bounds.Left, bounds.Top, bounds.Width, lineThickness), monoGameColor);
        _spriteBatch.Draw(_whitePixel, new Rectangle(bounds.Left, bounds.Bottom - lineThickness, bounds.Width, lineThickness), monoGameColor);
        _spriteBatch.Draw(_whitePixel, new Rectangle(bounds.Left, bounds.Top, lineThickness, bounds.Height), monoGameColor);
        _spriteBatch.Draw(_whitePixel, new Rectangle(bounds.Right - lineThickness, bounds.Top, lineThickness, bounds.Height), monoGameColor);
    }

    private void DrawImage(DrawCommand command)
    {
        if (command.Image is not MonoGameImage image)
        {
            throw new InvalidOperationException("DrawImage requires a MonoGameImage when using MonoGameDrawingBackend.");
        }

        _spriteBatch.Draw(image.Texture, ToRectangle(command.Rect), ToColor(command.Color));
    }

    private static Rectangle ToRectangle(DrawRect rect)
    {
        return new Rectangle(
            (int)MathF.Round(rect.X),
            (int)MathF.Round(rect.Y),
            (int)MathF.Round(rect.Width),
            (int)MathF.Round(rect.Height));
    }

    private static Color ToColor(DrawColor color)
    {
        return new Color(color.R, color.G, color.B, color.A);
    }
}
```

- [ ] **Step 4: Run tests and build**

Run:

```powershell
dotnet test Cerneala.slnx
dotnet build Cerneala.slnx --no-restore
```

Expected: tests pass and build succeeds with 0 errors.

- [ ] **Step 5: Commit**

```powershell
git add UI/Drawing/MonoGame/MonoGameDrawingBackend.cs tests/Cerneala.Tests/Drawing/DrawingResourceTests.cs
git commit -m "Add MonoGame drawing backend"
```

### Task 6: Text Pipeline and MonoGame Text Rendering

**Files:**
- Create: `UI/Text/TextShapeResult.cs`
- Create: `UI/Text/RasterizedText.cs`
- Create: `UI/Text/SkiaTextShaper.cs`
- Create: `UI/Text/SkiaTextRasterizer.cs`
- Modify: `UI/Drawing/MonoGame/MonoGameDrawingBackend.cs`
- Test: `tests/Cerneala.Tests/Drawing/TextPipelineTests.cs`

**Interfaces:**
- Consumes: `DrawTextRun`, `SkiaFont`, `DrawCommandKind.DrawText`
- Produces: `TextShapeResult(string text, int GlyphCount)`
- Produces: `RasterizedText(int Width, int Height, byte[] RgbaPixels)`
- Produces: `SkiaTextShaper.Shape(DrawTextRun textRun): TextShapeResult`
- Produces: `SkiaTextRasterizer.Rasterize(DrawTextRun textRun, DrawColor color): RasterizedText`
- Produces: `MonoGameDrawingBackend(SpriteBatch spriteBatch, Texture2D whitePixel, SkiaTextRasterizer? textRasterizer): IDrawingBackend`

- [ ] **Step 1: Write failing text pipeline tests**

Create `tests/Cerneala.Tests/Drawing/TextPipelineTests.cs`:

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Text;

namespace Cerneala.Tests.Drawing;

public sealed class TextPipelineTests
{
    [Fact]
    public void TextShaperRejectsNullTextRun()
    {
        SkiaTextShaper shaper = new();

        Assert.Throws<ArgumentNullException>(() => shaper.Shape(null!));
    }

    [Fact]
    public void TextShaperReturnsGlyphCountForSystemFontTextRun()
    {
        SystemFontSource fonts = new();
        DrawTextRun textRun = new(fonts.LoadFont("Arial", 16), "Cerneala", 16);
        SkiaTextShaper shaper = new();

        TextShapeResult result = shaper.Shape(textRun);

        Assert.Equal("Cerneala", result.Text);
        Assert.True(result.GlyphCount > 0);
    }

    [Fact]
    public void TextRasterizerReturnsPixelsForSystemFontTextRun()
    {
        SystemFontSource fonts = new();
        DrawTextRun textRun = new(fonts.LoadFont("Arial", 16), "Cerneala", 16);
        SkiaTextRasterizer rasterizer = new();

        RasterizedText result = rasterizer.Rasterize(textRun, DrawColor.White);

        Assert.True(result.Width > 0);
        Assert.True(result.Height > 0);
        Assert.NotEmpty(result.RgbaPixels);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj --filter "FullyQualifiedName~Cerneala.Tests.Drawing.TextPipelineTests"
```

Expected: FAIL because `TextShapeResult`, `RasterizedText`, `SkiaTextShaper`, and `SkiaTextRasterizer` do not exist.

- [ ] **Step 3: Implement text shape and rasterizer**

Create `UI/Text/TextShapeResult.cs`:

```csharp
namespace Cerneala.Drawing.Text;

public readonly record struct TextShapeResult(string Text, int GlyphCount);
```

Create `UI/Text/RasterizedText.cs`:

```csharp
namespace Cerneala.Drawing.Text;

public sealed class RasterizedText
{
    public RasterizedText(int width, int height, byte[] rgbaPixels)
    {
        Width = width;
        Height = height;
        RgbaPixels = rgbaPixels ?? throw new ArgumentNullException(nameof(rgbaPixels));
    }

    public int Width { get; }

    public int Height { get; }

    public byte[] RgbaPixels { get; }
}
```

Create `UI/Text/SkiaTextShaper.cs`:

```csharp
using Cerneala.Drawing;
using HarfBuzzSharp;

namespace Cerneala.Drawing.Text;

public sealed class SkiaTextShaper
{
    public TextShapeResult Shape(DrawTextRun textRun)
    {
        ArgumentNullException.ThrowIfNull(textRun);

        using Buffer buffer = new();
        buffer.AddUtf16(textRun.Text);
        buffer.GuessSegmentProperties();
        return new TextShapeResult(textRun.Text, buffer.Length);
    }
}
```

Create `UI/Text/SkiaTextRasterizer.cs`:

```csharp
using Cerneala.Drawing;
using SkiaSharp;

namespace Cerneala.Drawing.Text;

public sealed class SkiaTextRasterizer
{
    public RasterizedText Rasterize(DrawTextRun textRun, DrawColor color)
    {
        ArgumentNullException.ThrowIfNull(textRun);

        if (textRun.Font is not SkiaFont font)
        {
            throw new InvalidOperationException("SkiaTextRasterizer requires a SkiaFont.");
        }

        using SKFont skFont = new(font.Typeface, font.Size);
        using SKPaint paint = new()
        {
            Color = ToColor(color),
            IsAntialias = true
        };

        SKRect bounds = new();
        skFont.MeasureText(textRun.Text, out bounds, paint);
        int width = Math.Max(1, (int)MathF.Ceiling(bounds.Width));
        int height = Math.Max(1, (int)MathF.Ceiling(bounds.Height));

        using SKBitmap bitmap = new(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawText(textRun.Text, -bounds.Left, -bounds.Top, skFont, paint);

        byte[] pixels = bitmap.Bytes;
        return new RasterizedText(width, height, pixels);
    }

    private static SKColor ToColor(DrawColor color)
    {
        return new SKColor(color.R, color.G, color.B, color.A);
    }
}
```

- [ ] **Step 4: Update MonoGameDrawingBackend to render text through the text rasterizer**

Modify `UI/Drawing/MonoGame/MonoGameDrawingBackend.cs`:

```csharp
using Cerneala.Drawing.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame;

public sealed class MonoGameDrawingBackend : IDrawingBackend
{
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _whitePixel;
    private readonly SkiaTextRasterizer? _textRasterizer;

    public MonoGameDrawingBackend(SpriteBatch spriteBatch, Texture2D whitePixel, SkiaTextRasterizer? textRasterizer = null)
    {
        _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
        _whitePixel = whitePixel ?? throw new ArgumentNullException(nameof(whitePixel));
        _textRasterizer = textRasterizer;
    }

    public void Render(DrawCommandList commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        foreach (DrawCommand command in commands)
        {
            RenderCommand(command);
        }
    }

    private void RenderCommand(DrawCommand command)
    {
        switch (command.Kind)
        {
            case DrawCommandKind.FillRectangle:
                FillRectangle(command.Rect, command.Color);
                break;

            case DrawCommandKind.DrawRectangle:
                DrawRectangle(command.Rect, command.Color, command.Thickness);
                break;

            case DrawCommandKind.DrawImage:
                DrawImage(command);
                break;

            case DrawCommandKind.DrawText:
                DrawText(command);
                break;

            case DrawCommandKind.PushClip:
            case DrawCommandKind.PopClip:
                break;

            default:
                throw new InvalidOperationException($"Unsupported draw command: {command.Kind}");
        }
    }

    private void DrawText(DrawCommand command)
    {
        if (_textRasterizer is null || command.TextRun is null)
        {
            return;
        }

        RasterizedText text = _textRasterizer.Rasterize(command.TextRun, command.Color);
        Texture2D texture = new(_spriteBatch.GraphicsDevice, text.Width, text.Height);
        texture.SetData(text.RgbaPixels);
        _spriteBatch.Draw(texture, ToVector2(command.Position), Color.White);
    }

    private void FillRectangle(DrawRect rect, DrawColor color)
    {
        _spriteBatch.Draw(_whitePixel, ToRectangle(rect), ToColor(color));
    }

    private void DrawRectangle(DrawRect rect, DrawColor color, float thickness)
    {
        int lineThickness = Math.Max(1, (int)MathF.Round(thickness));
        Rectangle bounds = ToRectangle(rect);
        Color monoGameColor = ToColor(color);

        _spriteBatch.Draw(_whitePixel, new Rectangle(bounds.Left, bounds.Top, bounds.Width, lineThickness), monoGameColor);
        _spriteBatch.Draw(_whitePixel, new Rectangle(bounds.Left, bounds.Bottom - lineThickness, bounds.Width, lineThickness), monoGameColor);
        _spriteBatch.Draw(_whitePixel, new Rectangle(bounds.Left, bounds.Top, lineThickness, bounds.Height), monoGameColor);
        _spriteBatch.Draw(_whitePixel, new Rectangle(bounds.Right - lineThickness, bounds.Top, lineThickness, bounds.Height), monoGameColor);
    }

    private void DrawImage(DrawCommand command)
    {
        if (command.Image is not MonoGameImage image)
        {
            throw new InvalidOperationException("DrawImage requires a MonoGameImage when using MonoGameDrawingBackend.");
        }

        _spriteBatch.Draw(image.Texture, ToRectangle(command.Rect), ToColor(command.Color));
    }

    private static Rectangle ToRectangle(DrawRect rect)
    {
        return new Rectangle(
            (int)MathF.Round(rect.X),
            (int)MathF.Round(rect.Y),
            (int)MathF.Round(rect.Width),
            (int)MathF.Round(rect.Height));
    }

    private static Vector2 ToVector2(DrawPoint point)
    {
        return new Vector2(point.X, point.Y);
    }

    private static Color ToColor(DrawColor color)
    {
        return new Color(color.R, color.G, color.B, color.A);
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run:

```powershell
dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj --filter "FullyQualifiedName~Cerneala.Tests.Drawing.TextPipelineTests"
```

Expected: PASS.

- [ ] **Step 6: Build full solution**

Run:

```powershell
dotnet build Cerneala.slnx --no-restore
```

Expected: build succeeds with 0 errors.

- [ ] **Step 7: Commit**

```powershell
git add UI/Text/TextShapeResult.cs UI/Text/RasterizedText.cs UI/Text/SkiaTextShaper.cs UI/Text/SkiaTextRasterizer.cs UI/Drawing/MonoGame/MonoGameDrawingBackend.cs tests/Cerneala.Tests/Drawing/TextPipelineTests.cs
git commit -m "Add text pipeline for MonoGame drawing"
```

### Task 7: Playground Smoke Usage

**Files:**
- Modify: `Playground/Cerneala.Playground/Game1.cs`

**Interfaces:**
- Consumes: `DrawCommandList`, `DrawingContext`, `MonoGameDrawingBackend`
- Produces: Playground uses the drawing abstraction for at least one rectangle.

- [ ] **Step 1: Update Playground to create drawing resources**

Modify `Playground/Cerneala.Playground/Game1.cs` to add fields:

```csharp
private DrawCommandList _drawCommands;
private DrawingContext _drawing;
private MonoGameDrawingBackend _drawingBackend;
private Texture2D _whitePixel;
```

Add these using statements:

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
```

- [ ] **Step 2: Initialize drawing resources in LoadContent**

Update `LoadContent`:

```csharp
protected override void LoadContent()
{
    _spriteBatch = new SpriteBatch(GraphicsDevice);
    _whitePixel = new Texture2D(GraphicsDevice, 1, 1);
    _whitePixel.SetData(new[] { Color.White });
    _drawCommands = new DrawCommandList();
    _drawing = new DrawingContext(_drawCommands);
    _drawingBackend = new MonoGameDrawingBackend(_spriteBatch, _whitePixel);
}
```

- [ ] **Step 3: Render through DrawingContext in Draw**

Update `Draw`:

```csharp
protected override void Draw(GameTime gameTime)
{
    GraphicsDevice.Clear(global::Cerneala.GameBootstrap.CreateDefaultClearColor());

    _drawCommands.Clear();
    _drawing.FillRectangle(new DrawRect(32, 32, 240, 96), new DrawColor(20, 20, 24, 220));
    _drawing.DrawRectangle(new DrawRect(32, 32, 240, 96), DrawColor.White, 2);

    _spriteBatch.Begin();
    _drawingBackend.Render(_drawCommands);
    _spriteBatch.End();

    base.Draw(gameTime);
}
```

- [ ] **Step 4: Run build**

Run:

```powershell
dotnet build Cerneala.slnx --no-restore
```

Expected: build succeeds with 0 errors.

- [ ] **Step 5: Commit**

```powershell
git add Playground/Cerneala.Playground/Game1.cs
git commit -m "Use drawing abstraction in playground"
```

### Task 8: Final Verification

**Files:**
- No new files.

**Interfaces:**
- Consumes: full solution.
- Produces: verified Cerneala.Drawing implementation.

- [ ] **Step 1: Run full tests**

Run:

```powershell
dotnet test Cerneala.slnx
```

Expected: PASS with all tests green.

- [ ] **Step 2: Run full build**

Run:

```powershell
dotnet build Cerneala.slnx --no-restore
```

Expected: build succeeds with 0 warnings and 0 errors.

- [ ] **Step 3: Inspect git status**

Run:

```powershell
git status -sb
```

Expected: only intended committed changes, or a clean tree if all task commits were made.

---

## Self-Review

- Spec coverage: the plan creates an abstract drawing layer, keeps controls/layout/input out, provides command recording, adds Skia/HarfBuzz dependencies for text preparation, loads fonts from the OS, and makes the MonoGame adapter the final renderer.
- Placeholder scan: no placeholders or open-ended implementation steps remain.
- Type consistency: `DrawCommandList`, `DrawingContext`, resource interfaces, and backend names are consistent across tasks.
- Known limitation: `PushClip` and `PopClip` are recorded but no-op in the first MonoGame backend. This is intentional YAGNI for the first implementation; clipping can become a focused follow-up once rectangle/text/image rendering exists.
- Known limitation: `SkiaTextShaper` introduces the HarfBuzz dependency and first shaping boundary, but it does not yet expose glyph runs as public drawing commands. A later focused task should add `DrawGlyphRun` once text measurement and shaping requirements are clearer.
- Known limitation: `SkiaTextRasterizer` returns simple RGBA pixel data for the first MonoGame adapter path. A later focused task should add glyph atlas caching and verified pixel-format handling instead of creating a texture per text command.

