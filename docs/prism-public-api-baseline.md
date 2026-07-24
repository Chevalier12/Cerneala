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

## Final Compatibility Result

The final Prism public-surface audit covers 51 Prism types and 10 existing types
extended for Prism. Every retained symbol has a current author, backend, hosting,
or diagnostics scenario and a matching API page/manifest entry.

| Change | Compatibility | Decision |
| --- | --- | --- |
| `IDrawingBackend.Render(DrawCommandList)` -> `Render(DrawCommandList, in DrawingFrameContext)` | Source and binary breaking for custom drawing backends | Necessary: the host must pass one validated per-frame context, including the optional backdrop lease, without duplicate analysis or backend-specific host coupling. |
| `IUiBackend.BackdropFrameSource` | Additive default interface member | Existing implementations inherit `null`; no backdrop provider is required. |
| `MonoGameUiHostOptions.BackdropFrameSource` and `PrismRendererOptions` | Additive optional properties | Existing construction remains valid. |
| `BeginPrism`/`EndPrism` and the public authoring/runtime/host Prism types | Additive | Non-Prism backends ignore the delimiters and render interior commands. Exhaustive enum switches should retain a default case. |
| Interim public graph/planning types, graph-bearing frame/request members, and framework-only context construction | Source and binary breaking for consumers of the unfinished pre-release Prism surface | Necessary: graph analysis, freshness, planning, and requirement ownership are internal framework invariants, not application extension points. |

The .NET SDK ApiCompat task was also run from the `HEAD` assembly to the final
assembly. It reported exactly 28 `CP0001` removals for the internalized graph and
planning types and five `CP0002` removals for the graph-bearing context/request
members. Every diagnostic maps to the final table above; there were no additional
unclassified removals. The earlier `IDrawingBackend` signature break is recorded
from this pre-Prism baseline because it predates that `HEAD` assembly.

No public third-party operation SDK, runtime shader source, adaptive-quality API,
or speculative graph abstraction was retained. The reproducible compatibility
check is `dotnet run --project .\Tools\PrismAudit\PrismAudit.csproj -- --check`;
its generated report is `docs/prism-completeness-report.generated.md`.
