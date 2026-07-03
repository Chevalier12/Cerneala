## Context

Cerneala currently records backend-neutral drawing through `DrawCommandList` and submits it through `IDrawingBackend`. The retained UI stack already has typed properties, retained layout, render invalidation, render caches, styling, resources, controls, text, images, input, diagnostics, and accessibility. The drawing layer is intentionally small: rectangles, text, images, and clips.

Section 22 of `ROADMAPv2.md` expands this surface with media descriptors and shape controls. The important constraint is that advanced media must not become a second rendering system next to `UI/Drawing`. New concepts either translate into tested `DrawCommand` additions or remain backend-neutral descriptors with clear responsibilities such as identity, bounds, resource participation, or future adapter data.

## Goals / Non-Goals

**Goals:**
- Add backend-neutral media descriptors for brushes, pens, geometries, transforms, opacity layers, shadows, and image sources.
- Add drawing command support only for primitives used by the initial shape controls and covered by tests/backend adapter behavior.
- Add retained shape controls that measure, arrange, invalidate, and render through existing retained rendering.
- Keep controls and rendering core independent of MonoGame, Skia, HarfBuzz, `Texture2D`, and `SpriteBatch`.
- Update roadmap checkboxes as the implementation lands.

**Non-Goals:**
- A full vector path tessellator or general GPU vector renderer.
- A complete gradient renderer in the MonoGame backend if the current backend cannot support it cleanly without a larger adapter design.
- Arbitrary transform composition in retained layout.
- New package splits or new external production dependencies.

## Decisions

1. Media objects are immutable or controlled value descriptors.
   - Rationale: render caches need stable identities and equality-friendly data.
   - Alternative considered: mutable WPF-style media objects with change notifications. That would require a broader dependency invalidation model and is out of scope for this phase.

2. Brushes describe paint intent, while drawing commands carry either `DrawColor` or media values only when the backend can interpret them.
   - Rationale: `SolidColorBrush` can bridge to existing color commands, while gradient brushes still need explicit identity and tests before backend-specific rasterization exists.
   - Alternative considered: immediately replace all `DrawColor` command payloads with `Brush`. That would churn existing controls and make simple rendering more complex.

3. Shape controls own geometry and paint properties, then emit drawing commands through `RenderContext`.
   - Rationale: shape controls are retained UI elements, not drawing backend helpers.
   - Alternative considered: expose shapes only as low-level drawing helpers. That would not satisfy the retained controls roadmap.

4. The initial command expansion targets ellipse and path geometry metadata with backend-neutral tests and MonoGame adapter behavior where feasible.
   - Rationale: rectangle commands already exist; ellipse and path are the missing shape surfaces from the roadmap.
   - Alternative considered: add every possible command now. That violates YAGNI and increases backend risk.

5. Image source abstractions wrap identity, intrinsic size, and drawing-handle resolution instead of replacing `IDrawImage`.
   - Rationale: `IDrawImage` is still the backend handle; `ImageSource` adds UI/media identity and metadata.
   - Alternative considered: make controls depend directly on backend images. That would break existing architecture boundaries.

## Risks / Trade-offs

- [Risk] Path rendering can become a hidden vector renderer project. -> Mitigation: keep `PathGeometry` as parsed/structured geometry data and only add drawing support that is explicitly tested.
- [Risk] Gradients may imply backend features not yet implemented. -> Mitigation: give gradient brushes resource/style identity and validation now; defer rasterization unless command/backend support is added with tests.
- [Risk] Shape controls could bypass retained render caching. -> Mitigation: render exclusively through `RenderContext.DrawingContext` and existing invalidation.
- [Risk] Image source identity can duplicate resource services. -> Mitigation: keep `ImageSource` focused on decoding/resource identity/intrinsic metadata above `IDrawImage`, and keep resource versioning in resource services.
- [Risk] Controls may accidentally reference adapter types. -> Mitigation: add/extend architecture tests for `UI/Controls/Shapes` and media boundaries.
