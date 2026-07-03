## Context

Cerneala already has a retained UI pipeline with typed properties, layout, invalidation, render caches, first controls, and a low-level drawing text stack under `UI.Drawing.Text`. `TextBlock` exists, but it currently measures through a controls-local approximation and records a `DrawTextRun` directly during render. That is enough for early control tests, but it does not provide a reusable text service boundary for font resolution, wrapping, layout cache keys, or retained invalidation semantics.

The existing Skia/HarfBuzz types remain the drawing-layer engine. This change adds a backend-neutral `UI.Text` layer above those drawing primitives so controls can request text measurement and rendering without depending on Skia, HarfBuzz, or concrete host resources.

## Goals / Non-Goals

**Goals:**
- Add `UI.Text` services for font resolution, text run styling, text measurement, layout cache identity, text rendering, wrapping policy, and MVP line breaking.
- Replace `TextBlock`'s controls-local measurement path with the `UI.Text` service layer.
- Preserve retained invalidation semantics: text/font/wrapping changes affect measure and render; color-only changes affect render without invalidating text metrics.
- Keep text services backend-neutral and make low-level shaping/rasterization an adapter detail behind drawing abstractions.
- Update tests and `ROADMAPv2.md` checklist for phase 11.

**Non-Goals:**
- Do not rebuild HarfBuzz shaping or Skia rasterization.
- Do not implement rich text, inline runs, editing, selection, bidi, IME composition, or caret behavior.
- Do not introduce WPF-style `FormattedText` or heavyweight text document machinery.
- Do not add hidden global font/resource lookup.

## Decisions

### Add `UI.Text` as the retained text service boundary

`UI.Text` will own `FontResolver`, `TextRunStyle`, `TextMeasureResult`, `TextMeasurer`, `TextLayoutCache`, `TextRenderer`, `TextWrapping`, `TextTrimming`, and `LineBreakService`.

Rationale: controls need text layout concepts, but drawing should remain a low-level command layer and `UI.Controls` should not accumulate measurement/cache infrastructure. This keeps the architecture clean: controls describe text, text services measure/cache/record, drawing executes commands.

Alternative considered: keep `TextMeasurer` in `UI.Controls`. Rejected because wrapping, font resolution, cache identity, and future resource invalidation would leak into controls and duplicate behavior across text controls.

### Keep `UI.Drawing.Text` as the low-level engine

`SkiaTextShaper` and `SkiaTextRasterizer` remain responsible for concrete shaping and rasterization. `UI.Text` converts retained text style into `DrawTextRun` and drawing commands without referencing Skia or HarfBuzz.

Rationale: the repo already has this pipeline. Replacing it would be churn and would mix this phase with backend engineering.

Alternative considered: make `UI.Text` call Skia/HarfBuzz directly for metrics. Rejected because that would break the backend-neutral retained UI boundary.

### Make text layout cache keys explicit

`TextLayoutCache` will key cached measurement/layout by text content, resolved font identity, font size, wrapping mode, available wrapping width, trimming mode, and DPI/scale. Color is intentionally excluded from metrics cache identity.

Rationale: retained rendering must reuse unchanged text work across frames, while invalidating accurately when metrics-affecting inputs change. Excluding color preserves render-only invalidation for foreground changes.

Alternative considered: store a single last measurement on `TextBlock`. Rejected because it hides cache policy in a control and does not scale to multiple text controls or shared services.

### Treat wrapping as MVP policy, not full typography

`TextWrapping` is introduced now. `LineBreakService` supports deterministic MVP line splitting for no-wrap and simple wrap cases. `TextTrimming` exists as a policy type, but advanced trimming can remain unsupported until needed.

Rationale: phase 11 needs layout/cache services for `TextBlock`, not a complete text engine. The API can be explicit without overbuilding.

Alternative considered: defer all wrapping. Rejected because wrapping width is part of the roadmap's cache key and should be represented in the service contract now.

### Replace controls-local text helpers carefully

The existing `UI.Controls.TextMeasurer` and `TextMeasurement` should either become compatibility shims over `UI.Text` or be removed if no public compatibility is needed inside the repo. `TextBlock` should depend on `UI.Text.TextMeasurer` and `TextRenderer`.

Rationale: two active `TextMeasurer` concepts would be confusing as hell and easy to misuse. The implementation should converge on one service boundary.

Alternative considered: leave both types side by side. Rejected because duplicate names across namespaces would make future code harder to read and review.

## Risks / Trade-offs

- [Risk] Accurate font metrics may require real font resources that are not available yet through the upcoming resource system. -> Mitigation: `FontResolver` wraps the existing `IFontSource`/`IDrawFont` drawing abstractions and keeps explicit fallback behavior.
- [Risk] Text cache invalidation can become stale if cache keys omit layout-affecting inputs. -> Mitigation: tests must cover text, font family, font size, wrapping width, and scale changes.
- [Risk] Color-only changes could accidentally invalidate measurement. -> Mitigation: tests must prove foreground changes invalidate render without measurement cache miss when metrics inputs are unchanged.
- [Risk] Wrapping behavior can expand into a typography project. -> Mitigation: keep MVP line breaking deterministic and document bidi/editing/selection as later work.

## Migration Plan

1. Add `UI.Text` primitives and service types without changing drawing backend APIs.
2. Update `TextBlock` to use `UI.Text` for measurement and rendering.
3. Retire or adapt controls-local text helper types.
4. Add focused `UI.Text` tests and extend `TextBlock` invalidation tests.
5. Update `ROADMAPv2.md` phase 11 checklist as implementation tasks complete.

Rollback is straightforward: revert `TextBlock` to the previous controls-local measurer and remove `UI.Text` service usage. No persisted data or wire contracts are involved.

## Open Questions

- Should the MVP `FontResolver` use a host-provided `IFontSource` immediately, or keep a deterministic fallback font until phase 12 resources land?
- Should scale/DPI enter the first implementation as an explicit argument on measurement APIs, or as a defaulted field on the text layout key?
