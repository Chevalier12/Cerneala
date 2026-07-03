## Why

The retained controls MVP currently has `TextBlock`, but text measurement is only a deterministic approximation inside `UI.Controls`. Cerneala needs a backend-neutral text services layer above the existing Skia/HarfBuzz drawing text pipeline so retained controls can measure, cache, and render text without rebuilding low-level shaping or rasterization.

## What Changes

- Add `UI.Text` services for font resolution, text run style, text measurement results, text layout caching, text rendering, wrapping, and MVP line breaking.
- Keep `UI.Drawing.Text` as the low-level Skia/HarfBuzz shaping and rasterization engine.
- Move `TextBlock` text measurement and render orchestration onto the new text services instead of the controls-local approximate measurer.
- Track text layout dependencies so text content, font family, font size, wrapping width, and scale changes invalidate measurement and render correctly.
- Preserve render-only invalidation for color changes when glyph metrics are unchanged.
- Defer bidi, selection, and editing controllers as later text editing capabilities.

## Capabilities

### New Capabilities
- `text-services`: Backend-neutral retained text services above drawing text primitives, including font resolution, text run styling, measurement, caching, rendering, wrapping policy, and MVP line breaking.

### Modified Capabilities
- `first-controls-panels`: `TextBlock` shall use the retained text services layer for measurement and rendering instead of controls-local approximations.
- `retained-rendering-cache`: Text render dependencies shall include text layout cache identity so unchanged text can reuse retained layout/render work while text-only changes invalidate the correct caches.

## Impact

- Affected code: `UI/Text`, `UI/Controls/TextBlock.cs`, existing controls text helper types, `UI/Rendering`, and text-related tests.
- Affected drawing layer: `UI/Drawing/DrawTextRun.cs`, `UI/Drawing/Text/SkiaTextShaper.cs`, and `UI/Drawing/Text/SkiaTextRasterizer.cs` remain the low-level engine and should only receive adapter-level changes if needed.
- Tests added under `tests/Cerneala.Tests/UI/Text` and extended `tests/Cerneala.Tests/Controls/TextBlockInvalidationTests.cs`.
