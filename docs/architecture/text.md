# Text

> Code: `UI/Text`, `Drawing/Text`, `Drawing/IDrawFont.cs`, `Drawing/IFontSource.cs`, `Drawing/DrawTextLayout.cs`, `UI/Controls/TextBlock.cs`, `UI/Controls/TextInputViewport.cs`, `Cerneala.Backends.SdlGpu/Gpu/SdlGpuDrawingResources.cs` (text atlas) · Verified at commit `0cb32300` (2026-10-10)

## Responsibility

Text turns a string, a font and an available width into lines, emits one `DrawText` command per line, and lets the backend rasterize and cache the glyph pixels. It also holds the editing model behind `TextBox` (document, caret, selection, undo, IME composition).

There are **two layout engines**:

- **The UI path** (`TextMeasurer`, `LineBreakService`, `TextRenderer`, `TextLayoutCache`), used by `TextBlock`, `TextBox` and the other text-input controls.
- **The draw-API path** (`DrawTextLayout` in `Drawing/`), used by `DrawingContext.Text`, `RenderSurface2DFrame.Text` and `DrawCommandTransform`. It has styled runs, character wrapping, glyph fallback fonts and right-to-left alignment.

`TextBlock` and `TextBox` do not use `DrawTextLayout`. This document describes the UI path; the draw-API path is listed where it differs.

## Components

| Type | File | Role | Owner |
|---|---|---|---|
| `TextAspect` | `UI/Text/TextAspect.cs` | Family, size, wrapping, trimming, scale, foreground, optional font resource id | created per measure or render call |
| `TextMeasurer` | `UI/Text/TextMeasurer.cs` | Resolves the font, builds a `TextLayoutKey`, returns a cached `TextMeasureResult`; one lock around cache lookup and creation | `TextMeasurer.Default` (process-wide), or one per control |
| `TextLayoutCache` | `UI/Text/TextLayoutCache.cs` | LRU of `TextMeasureResult`, capacity `DefaultCapacity` = 512, `Hits` and `Misses` counters, no lock of its own | the `TextMeasurer` that owns it |
| `TextLayoutKey` | `UI/Text/TextLayoutKey.cs` | `Text`, `FontIdentity`, `FontSize`, `Wrapping`, `WrappingWidth`, `Trimming`, `Scale`, `VisibleLineCount`; foreground is **not** in the key | value |
| `TextMeasureResult`, `TextLine` | `UI/Text/TextMeasureResult.cs`, `TextLine.cs` | Size, lines (text plus width), cache key, `RenderIdentity` | cache entry |
| `LineBreakService` | `UI/Text/LineBreakService.cs` | Splits text into lines and applies ellipsis, through `UnicodeLineBreakEngine`; measures widths with the shaper | `LineBreakService.Default` |
| `UnicodeLineBreakEngine` | `Drawing/Text/UnicodeLineBreakEngine.cs` (internal) | Rule-based greedy wrapping on text elements (grapheme clusters) | static |
| `TextRenderer` | `UI/Text/TextRenderer.cs` | Measures again (cache hit), then calls `DrawingContext.DrawText` once per line | `TextRenderer.Default`, or one per control |
| `FontResolver`, `ResolvedTextFont` | `UI/Text/FontResolver.cs`, `ResolvedTextFont.cs` | Turns a `TextAspect` into an `IDrawFont` plus a string identity (`"Family:Size"`, or `"resource:{id}:{version}"` for a font resource); no caching | `FontResolver.Default` |
| `TextLineMetrics` | `UI/Text/TextLineMetrics.cs` | Line height and baseline, from the sample `"Ag"` | static |
| `TextShaper`, `SkiaTextShaper` | `Drawing/Text/TextShaper.cs`, `SkiaTextShaper.cs` | HarfBuzz shaping (`HarfBuzzSharp`) for `SkiaFont`; shape cache of 4,096 entries per typeface | static, process-wide |
| `IDrawFont`, `IFontSource` | `Drawing/IDrawFont.cs`, `Drawing/IFontSource.cs` | `IDrawFont` has only `FamilyName` and `Size`; `IFontSource.LoadFont(familyName, size)` | contract |
| `SystemFontSource`, `SkiaFont` | `Drawing/Text/SystemFontSource.cs`, `SkiaFont.cs` | The only production `IFontSource`; static case-insensitive typeface cache with no eviction; unknown families get `SKTypeface.Default` | static, process-wide |
| `SkiaTextRasterizer`, `SkiaTextBlobCache` | `Drawing/Text/SkiaTextRasterizer.cs`, `SkiaTextBlobCache.cs` | Rasterizes runs to RGBA or three subpixel coverage layers; caches `SKTextBlob` objects (4,096 per typeface), not pixels | backend; blob cache static |
| `TextCaretLayout` | `UI/Text/TextCaretLayout.cs` | Caret X positions and caret hit testing | per text-input control |
| `TextEditor`, `TextDocument`, `UndoRedoStack`, `TextCompositionManager` | `UI/Text/*.cs` | Editing model: text plus version, caret and selection, undo snapshots, IME preview | `TextInputCore` (per text-input control) |
| `BidiTextService`, `UnicodeBidiEngine` | `UI/Text/BidiTextService.cs`, `Drawing/Text/UnicodeBidiEngine.cs` | First-strong base direction and directional runs | no production caller in the UI path |
| GPU text atlas | `Cerneala.Backends.SdlGpu/Gpu/SdlGpuDrawingResources.cs`, `SdlGpuTextAtlasAllocator.cs` | Pages of 1024 × 1024 px, at most 8; shelf packer; LRU eviction of entries not used by an active frame | the SDL GPU drawing resources (device) |

## Data Flow

Example: `textBlock.Text = "Hello"` in a `TextBlock` with the default `NoWrap` and no trimming.

1. **Property change.** `TextBlock.TextProperty` has `AffectsMeasure | AffectsRender | AffectsSemantics`. The element's layout and render versions go up, and it is queued for measure and render (see [property-system.md](property-system.md) and [invalidation-and-frame.md](invalidation-and-frame.md)).
2. **Measure.** `TextBlock.MeasureCore` builds a `TextAspect` and calls `TextMeasurer.Measure(Text, aspect, availableWidth)`.
3. `NoWrap` with no trimming makes the wrapping width `+∞`. `FontResolver` resolves the family through `SystemFontSource`, which gives a `SkiaFont`.
4. The key is `("Hello", "Family:Size", size, NoWrap, +∞, None, 1)`. On a cache miss, `LineBreakService.BreakLines` produces the lines. Each width comes from `TextShaper.Default.TryShape`, which is HarfBuzz for a `SkiaFont`. When shaping is not possible, the width is `text.Length × FontSize × Scale × 0.5`. Here there is one line, `"Hello"`.
5. The result size is the widest line by `lineHeight × lineCount`. `MeasureCore` stores `RenderIdentity` as the element's text-layout render dependency (see [rendering.md](rendering.md)) and returns the size.
6. **Render.** `TextBlock.OnRender` calls `TextRenderer.Render` with `context.Bounds.Width`. It measures again, which is a cache hit for the same key, then emits `DrawCommand.DrawText` for each line at `y + i × lineHeight + baseline`, with the foreground brush (see [drawing.md](drawing.md)).
7. **Backend.** The SDL GPU backend builds a raster key from font identity, text, size, coordinate scale and pixel phase; color is not part of it. On a miss, `SkiaTextRasterizer` produces three coverage layers. They go into the text atlas, the page is marked dirty, and the dirty pages are uploaded before the batch is drawn. Each layer is drawn as a quad with a channel mask (see [sdl-desktop-backend.md](sdl-desktop-backend.md)).

Changing only `Foreground` sets `Render` but not `Measure`, and the text-layout identity stays the same. The text is drawn again with the new brush, and the layout is reused.

### Line breaking

`UnicodeLineBreakEngine` is hand-written. It is **not** UAX #14:

- Paragraphs split only at `\r` and `\n`.
- A break is allowed only after whitespace or after one of `-`, `/`, `\`, `,`, `;`, `:`.
- It walks text elements, so it never splits a surrogate pair or a combining cluster.
- A word wider than the line is broken at a character boundary.
- Trimming (`CharacterEllipsis`, `WordEllipsis`) appends `…`. When the height allows fewer lines than the text needs, the last visible line is collapsed with an ellipsis.

Example, from `TextMeasurerTests`: `"Alpha beta gamma"` at size 10 and width 45 wraps to `["Alpha", "beta", "gamma"]`. `"Alpha beta\r\ngamma"` at width 100 gives `["Alpha beta", "gamma"]`.

## Lifecycle And Ownership

- `TextMeasurer.Default` and `TextRenderer.Default` share **one** 512-entry `TextLayoutCache` for every control that uses the defaults. Entries are evicted least-recently-used first.
- When a `TextBlock` uses a font resource (`FontResourceId` plus a resource provider), it builds a new `TextMeasurer` and `TextRenderer` for each call. They use the control's own `resourceTextLayoutCache`, which is cleared when `FontResourceId` or `ResourceProvider` changes. The font identity includes the resource version, so replacing the resource produces new keys (see [theming-and-resources.md](theming-and-resources.md)).
- The typeface cache, shape cache and blob cache are static and live for the whole process. Shape and blob caches are keyed per `SKTypeface` through a `ConditionalWeakTable` and evict first in, first out. The typeface cache never evicts.
- **GPU text atlas**, per SDL GPU drawing resources:
  - The backend opens a text-atlas frame with `BeginTextAtlasFrame` and closes it with `EndTextAtlasFrame`.
  - Entries used in a frame are pinned (`ActiveFrameCount`).
  - When all 8 pages are full, eviction walks the LRU list and frees only entries with `ActiveFrameCount == 0`.
  - If no space can be made, or a layer is larger than 1022 px, the run falls back to separate per-layer textures.
  - Disposing the resources clears the atlas and releases its textures.

## Frame Integration

Text adds no frame phase. Text properties invalidate `Measure` and `Render`, and caret or selection changes in `TextBox` invalidate `Render` only. The work runs in the `Measure` and `RenderCache` phases. The text atlas uploads happen in the backend's draw, before the batches that sample it.

## Invariants

- The same key returns the same result; a hit is counted once and a miss once. `tests/Cerneala.Tests/UI/Text/TextLayoutCacheTests.cs:UnchangedLayoutHitsCache`.
- At capacity, the least recently used entry is evicted (capacity 2: `first`, `second`, `first`, `third` → `second` is gone). `TextLayoutCacheTests.cs:CacheEvictsTheLeastRecentlyUsedLayoutAtCapacity`.
- Text content is part of the key; color is not. `TextLayoutCacheTests.cs:TextContentChangesCacheIdentity`, `TextLayoutCacheTests.cs:ColorDoesNotAffectCacheIdentity`.
- 64 parallel measurements of the same text produce 1 miss and 63 hits. `tests/Cerneala.Tests/UI/Text/TextMeasurerTests.cs:MeasureAllowsConcurrentAccessToSharedCache`.
- Wrapping breaks at word boundaries before breaking inside a word, and keeps explicit line breaks. `TextMeasurerTests.cs:WrappedMeasurementBreaksAtWordBoundariesBeforeHardWrapping`, `TextMeasurerTests.cs:WrappedMeasurementPreservesExplicitLineBreaks`.
- Rendering emits exactly one `DrawText` per measured line, and measure plus render use one cache entry (1 miss, 1 hit). `tests/Cerneala.Tests/UI/Text/TextRendererWrapContractTests.cs:RenderDrawsOneCommandPerMeasuredWrappedLine`, `TextRendererWrapContractTests.cs:RenderUsesSameLayoutCacheForMeasurementAndLineDrawing`.
- Changing `Text` invalidates measure and render and changes the text-layout identity. `tests/Cerneala.Tests/Controls/TextBlockInvalidationTests.cs:TextChangeInvalidatesMetricsAndRender`.
- Changing `Foreground` invalidates render only and keeps the text-layout identity; the text is not measured again. `TextBlockInvalidationTests.cs:ForegroundChangeDoesNotChangeTextLayoutIdentity`, `TextBlockInvalidationTests.cs:ForegroundChangeDoesNotForceTextMeasurementRecompute`.
- Measure and render of a wrapped `TextBlock` agree on one text-layout identity. `tests/Cerneala.Tests/Controls/TextBlockLayoutContractTests.cs:TextBlockMeasureAndRenderUseSameTextLayoutKey`.
- The GPU raster key separates font, size, coordinate scale and text, and has no color member. `tests/Cerneala.Tests.SdlGpu/SdlGpuTextCacheContractTests.cs:TextRasterKeySeparatesGeometryAndExcludesForegroundColor`.
- Ending one atlas frame does not evict or move another active frame's entries; overflow evicts only inactive entries, and the page count stays at 8. `SdlGpuTextCacheContractTests.cs:CompletingOneFrameCannotEvictOrMoveAnotherActiveFramesAtlasEntries`, `SdlGpuTextCacheContractTests.cs:AtlasOverflowEvictsLeastRecentlyUsedInactivePagesWithoutReleasingActivePages`.
- Disposing the drawing resources clears the atlas and all GPU textures, and a second dispose is harmless. `SdlGpuTextCacheContractTests.cs:ResourceOwnerDisposalClearsCachedTextAndIsIdempotent`.
- Shaping does not fill the blob cache. `tests/Cerneala.Tests/Drawing/TextPipelineTests.cs:TextShapingDoesNotPopulateRasterBlobCache`.
- `LineBreakService` and `UnicodeLineBreakEngine` have no direct tests. They are covered only through `TextMeasurer` and `DrawTextLayout` tests.

## Known Limitations

- Line breaking is rule-based, not UAX #14 (see Line breaking). The UI path offers only `NoWrap` and `Wrap`; `TextBox` is always `NoWrap` with no trimming.
- The UI path has no bidirectional layout. `BidiTextService` has no production caller. The draw-API path detects direction and reverses the fragment order for right-to-left text, but it has no embedding, isolate or mirroring support.
- The UI path has no per-glyph font fallback; an unknown family uses `SKTypeface.Default`. Glyph fallback exists only in `DrawTextLayout` (`FallbackFonts`).
- A font that is not a `SkiaFont` cannot be shaped. Its width falls back to `0.5 × FontSize` per UTF-16 character.
- Measure wraps at the available width and render wraps at the arranged bounds width. When they differ, they produce two cache entries.
- The UI path measures line height with `"Ag"`, and `DrawTextLayout` with `"Mg"`.
- `TextMeasureResult.RenderIdentity` is the key turned into a string, including the whole text. Its cost grows with text length (nemăsurat).
- `TextCaretLayout` measures every prefix of the text and has no cache (nemăsurat).
- Undo snapshots store the full text for each step.
- `SystemFontSource` never evicts typefaces.
