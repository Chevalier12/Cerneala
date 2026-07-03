## 1. Text Service Primitives

- [x] 1.1 Create `UI/Text/TextWrapping.cs` and `UI/Text/TextTrimming.cs` with explicit MVP policy values.
- [x] 1.2 Create `UI/Text/TextRunStyle.cs` with validated text style inputs and conversion support for `DrawTextRun`.
- [x] 1.3 Create `UI/Text/TextMeasureResult.cs` for measured size, line count, cache identity, and resolved metrics metadata.
- [x] 1.4 Create `UI/Text/LineBreakService.cs` with deterministic no-wrap and simple wrap behavior.

## 2. Font Resolution

- [x] 2.1 Create `UI/Text/FontResolver.cs` that resolves retained font requests through explicit font dependencies.
- [x] 2.2 Preserve backend neutrality so `UI/Text` does not reference MonoGame, Skia, HarfBuzz, `Texture2D`, or `SpriteBatch`.
- [x] 2.3 Add tests in `tests/Cerneala.Tests/UI/Text/FontResolverTests.cs`.

## 3. Measurement And Layout Cache

- [x] 3.1 Create `UI/Text/TextLayoutCache.cs` keyed by text, resolved font identity, font size, wrapping mode, wrapping width, trimming mode, and scale.
- [x] 3.2 Create `UI/Text/TextMeasurer.cs` that computes deterministic desired size through `FontResolver`, `LineBreakService`, and `TextLayoutCache`.
- [x] 3.3 Ensure foreground/color changes do not change text layout cache identity.
- [x] 3.4 Add tests in `tests/Cerneala.Tests/UI/Text/TextMeasurerTests.cs`.
- [x] 3.5 Add tests in `tests/Cerneala.Tests/UI/Text/TextLayoutCacheTests.cs`.

## 4. Text Rendering

- [x] 4.1 Create `UI/Text/TextRenderer.cs` that records text through `DrawingContext.DrawText`.
- [x] 4.2 Ensure `TextRenderer` reuses cached text layout for unchanged metrics inputs.
- [x] 4.3 Add tests in `tests/Cerneala.Tests/UI/Text/TextRendererTests.cs`.

## 5. TextBlock Integration

- [x] 5.1 Update `UI/Controls/TextBlock.cs` to measure through `UI.Text.TextMeasurer`.
- [x] 5.2 Update `UI/Controls/TextBlock.cs` to render through `UI.Text.TextRenderer`.
- [x] 5.3 Retire or adapt `UI/Controls/TextMeasurer.cs` and `UI/Controls/TextMeasurement.cs` so there is one active text service boundary.
- [x] 5.4 Ensure text changes invalidate metrics and render commands.
- [x] 5.5 Ensure font family and font size changes invalidate measurement and render.
- [x] 5.6 Ensure foreground changes invalidate render without forcing text measurement recomputation.
- [x] 5.7 Add or extend `tests/Cerneala.Tests/Controls/TextBlockInvalidationTests.cs`.

## 6. Retained Rendering Dependencies

- [x] 6.1 Extend retained text dependency tracking so text layout cache identity participates in render cache staleness checks.
- [x] 6.2 Ensure unchanged text dependencies allow retained local render command cache reuse.
- [x] 6.3 Add focused retained rendering cache tests for text dependency behavior.

## 7. Architecture Boundaries And Roadmap

- [x] 7.1 Extend architecture boundary tests to include `UI/Text` backend-neutrality.
- [x] 7.2 Update `ROADMAPv2.md` section 11 checklist as files, tests, and acceptance items are completed.
- [x] 7.3 Verify `openspec validate add-text-services --strict` passes.
- [x] 7.4 Verify `dotnet build Cerneala.slnx -warnaserror` passes.
- [x] 7.5 Verify `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj` passes.
