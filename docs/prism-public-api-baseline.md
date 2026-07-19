# Prism Public API Baseline

## Scope

Captured before the Prism foundation implementation on 2026-07-19. This baseline
records the public host and drawing contracts that later Prism plans are expected
to extend. It intentionally contains no proposed members and no Prism types.

The final API comparison must account for every signature added to or changed from
this file. Unrelated public API changes are outside the Prism plans.

## Cerneala.Drawing.IDrawingBackend

```csharp
namespace Cerneala.Drawing;

public interface IDrawingBackend
{
    void Render(DrawCommandList commands);
}
```

## Cerneala.UI.Hosting.IUiBackend

```csharp
namespace Cerneala.UI.Hosting;

public interface IUiBackend
{
    IInputSource? InputSource { get; }
    IDrawingBackend? DrawingBackend { get; }
}
```

## Cerneala.UI.Hosting.MonoGame.MonoGameUiHostOptions

```csharp
namespace Cerneala.UI.Hosting.MonoGame;

public sealed class MonoGameUiHostOptions
{
    public required SpriteBatch SpriteBatch { get; init; }
    public required Texture2D WhitePixel { get; init; }
    public UIRoot? Root { get; init; }
    public UiViewport Viewport { get; init; }
    public MonoGameInputSource? InputSource { get; init; }
    public MonoGameContentServices? ContentServices { get; init; }
    public IImageLoader? ImageLoader { get; init; }
    public IUiClock? Clock { get; init; }
    public SkiaTextRasterizer? TextRasterizer { get; init; }
    public IPlatformServices? PlatformServices { get; init; }
}
```

## Anticipated Prism Touch Points

- `IDrawingBackend.Render` will receive an explicit frame context in a later plan.
- `IUiBackend` will expose the optional backdrop source in a later plan.
- `MonoGameUiHostOptions` will accept backdrop and Prism renderer options in later
  plans.

The foundation-and-catalog plan does not change these three APIs.
