## Context

Cerneala now has the retained UI foundation needed by real controls: typed properties, retained trees, invalidation, layout, render caches, host integration, input routing, and command routing. The existing `UI/Layout/Panels` namespace already contains layout-capable `Panel`, `Canvas`, and `StackPanel`, while `UI/Controls` only contains the primitive `ButtonBase`.

This change adds the first usable controls above the retained foundation. It keeps the design code-first and direct-rendered for MVP. Templates, markup, full styling, full text layout caching, and resource dictionaries stay out of this slice.

## Goals / Non-Goals

**Goals:**

- Add `Control` with common visual and text typed properties.
- Add retained content/decorator controls: `ContentControl`, `Decorator`, `Border`, `TextBlock`, `Image`, and `Button`.
- Add controls-facing `Panel`, `Canvas`, and `StackPanel` types that reuse existing layout panel behavior.
- Keep controls backend-neutral: no MonoGame, Skia, HarfBuzz, `Texture2D`, or `SpriteBatch` references in controls.
- Render `Border`, `TextBlock`, `Image`, and `Button` through existing retained render-cache paths.
- Let `Button` participate in retained hit testing, hover, pressed state, focus, click, and command execution.
- Update `ROADMAPv2.md` as implementation progresses.

**Non-Goals:**

- No full styling engine or selector/pseudo-class engine.
- No `ControlTemplate`, `TemplatePart`, `ToggleButton`, or `CheckBox` implementation in this MVP slice.
- No markup or source generation.
- No full text services from roadmap section 11; `TextBlock` may use the smallest higher-level text seam needed for measurement/render tests.
- No image loading/resource system from roadmap section 12; `Image` may consume an existing `IDrawImage`.

## Decisions

### Controls inherit retained behavior instead of duplicating framework paths

`Control` derives from `UIElement`, and concrete controls override `MeasureCore`, `ArrangeCore`, and `OnRender` only where they add behavior. Invalidation remains driven by typed property metadata.

Alternative considered: build a separate control tree. That would duplicate retained tree, layout, input, and render infrastructure, so controls stay normal retained elements.

### Controls-facing panels wrap existing layout panels

`UI/Controls/Panel`, `Canvas`, and `StackPanel` will reuse the behavior already proven under `UI/Layout/Panels`. They provide ergonomic namespaces for app code without replacing the lower-level layout implementation.

Alternative considered: move panel implementations from `UI/Layout/Panels`. That risks churn and namespace breakage without adding behavior.

### MVP controls render directly

`Border`, `TextBlock`, `Image`, and `Button` will render directly through `RenderContext.DrawingContext`. `ControlTemplate` is deferred because the first control set needs correctness and retained integration before templating.

Alternative considered: implement templates now. That widens the change into roadmap section 15 and makes debugging the first controls harder.

### TextBlock uses a minimal text measurement/render seam

`TextBlock` needs to measure and render text, but the complete text-service cache belongs to roadmap section 11. This change should introduce only the smallest service or adapter needed to keep `TextBlock` testable and backend-neutral.

Alternative considered: block `TextBlock` until section 11. That would prevent MVP acceptance from proving a usable button/text UI path.

### Button remains built on ButtonBase

`Button` derives from `ButtonBase` and adds content/visual rendering behavior while reusing command, pressed, hover, and focus state already present in the input bridge.

Alternative considered: make `Button` a separate `ContentControl` with duplicated pressed/command behavior. That violates DRY and splits command semantics.

## Risks / Trade-offs

- [Risk] `TextBlock` could grow into the full text system too early -> Mitigation: keep the text seam minimal and leave caching/service expansion to section 11.
- [Risk] Controls-facing panel wrappers could diverge from layout panel behavior -> Mitigation: wrappers should inherit/delegate and tests should prove behavior matches existing panels.
- [Risk] Direct-rendered MVP controls might later need templates -> Mitigation: keep render hooks small so future templates can replace visual generation without changing tree/input contracts.
- [Risk] `Image` without resource tracking may not invalidate on external image replacement -> Mitigation: only support explicit `Source` property invalidation here; resource dependency tracking stays in section 12.
