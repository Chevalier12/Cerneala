# Prism Public API Baseline

## Scope

Captured before the Prism foundation implementation on 2026-07-19. This baseline
records the public host and drawing contracts that later Prism plans are expected
to extend. It intentionally contains no proposed members and no Prism types.

The MonoGame/WindowsDX signatures below are historical baseline inputs only.
Those adapters were retired; the current audit loads the core and SDL_GPU
delivered assemblies.

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

## Historical Cerneala.UI.Hosting.MonoGame.MonoGameUiHostOptions

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

## Anticipated Prism Touch Points At Capture Time

- `IDrawingBackend.Render` was expected to receive an explicit frame context.
- `IUiBackend` was expected to expose the optional backdrop source.
- `MonoGameUiHostOptions` was expected to accept backdrop and Prism renderer
  options.

The foundation-and-catalog plan did not change these three APIs.

## Final Compatibility Result

The current Prism public-surface audit covers 216 Prism types and 8 existing types
extended for Prism. Every retained symbol has a current author, backend, hosting,
or diagnostics scenario and a matching API page/manifest entry.

| Change | Compatibility | Decision |
| --- | --- | --- |
| `IDrawingBackend.Render(DrawCommandList)` -> `Render(DrawCommandList, in DrawingFrameContext)` | Source and binary breaking for custom drawing backends | Necessary: the host must pass one validated per-frame context, including the optional backdrop lease, without duplicate analysis or backend-specific host coupling. |
| `IUiBackend.BackdropFrameSource` | Additive default interface member | Existing implementations inherit `null`; no backdrop provider is required. |
| Historical `MonoGameUiHostOptions.BackdropFrameSource` and `PrismRendererOptions` | Additive optional properties when introduced; later removed with the adapter | The maintained SDL composition does not expose a replacement configuration facade. |
| `SceneNode2D.PrismInputDomain` | Additive | Declares the required finite source domain for a spatially virtualized scene composition when automatic input selection is unavailable. |
| `BeginPrism`/`EndPrism` and the public authoring/runtime/host Prism types | Additive | Non-Prism backends ignore the delimiters and render interior commands. Exhaustive enum switches should retain a default case. |
| `PrismImage`, `PrismPipeline`, `PrismOperation`, `PrismFilter`, `PrismStyle`, and the 144 catalog-generated filter/style types | Additive | Strongly typed image-pipeline authoring approved by the RenderSurface2D delivery; the catalog remains the source of operation names and parameter contracts. |
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

The audit loads both the core and SDL_GPU assemblies. Catalog-generated filter
and style type names are derived from `prism-catalog.json` rather than copied into
a second manual list. For existing types extended by Prism, the audit compares the
Prism/backdrop members only; unrelated approved members such as
`DrawingFrameContext.StateAnalysis` belong to their own drawing contract.
