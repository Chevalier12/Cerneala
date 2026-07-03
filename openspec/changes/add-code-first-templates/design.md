## Context

Cerneala already has a retained element tree, typed properties, layout, retained rendering, hit testing, input routing, commands, first controls, styling, and themes. Section 15 of `ROADMAPv2.md` adds the missing composition layer: controls should be able to generate reusable retained visual structure instead of embedding all visual logic directly in each concrete control.

The existing tree model is the core constraint. Template-generated children must be real `UIElement` instances in the retained logical/visual tree, not transient render objects. Existing layout, rendering, hit testing, routed input, invalidation, and styling should keep working because they already operate on retained elements.

## Goals / Non-Goals

**Goals:**
- Add code-first typed templates for controls, content, item panels, and data values.
- Keep generated template children retained across frames until the template or modeled content changes.
- Provide typed template binding without string property paths or runtime reflection in the hot path.
- Add `ContentPresenter` and `ItemsPresenter` foundations that compose retained children through templates.
- Keep template part metadata diagnostic-only through attributes; do not introduce hidden runtime lookup magic.
- Reuse existing retained child ownership, property precedence, invalidation, layout, rendering, hit testing, and input routing behavior.

**Non-Goals:**
- No XAML, markup parser, serializer, or source generator.
- No full WPF trigger, namescope, resource lookup, or binding engine clone.
- No virtualization or selection behavior; those belong to later roadmap sections.
- No animation or transition system.
- No backend-specific rendering code inside template APIs.

## Decisions

### Templates create retained element instances

`ControlTemplate<TControl>` should wrap a typed factory such as `Func<TemplateContext<TControl>, UIElement?>`. Applying the template creates a `TemplateInstance` that owns the generated root and attaches it through the owner's retained child collections.

Rationale: retained children automatically participate in existing layout, rendering, hit testing, input routing, root attachment, dirty propagation, and style behavior. A separate template visual store would duplicate the tree system and turn into a swamp.

Alternative considered: represent templates as draw command factories. Rejected because that would bypass layout, input, hit testing, styling, and retained child ownership.

### Template instances are reused until invalidated

Each templated control should keep the current `TemplateInstance`. Reapplying the same template should reuse the generated root. Changing the template should detach the old generated children, create the new instance once, and invalidate the subtree through existing retained invalidation.

Rationale: section 15 requires generated children to be retained across frames and template changes to invalidate once, not rebuild every frame.

Alternative considered: rebuild template output on each measure/render pass. Rejected because it breaks retained performance and creates unstable element identity.

### Template binding is typed and explicit

`TemplateBinding<T>` should bind a source `UiProperty<T>` from the templated owner to a target `UiProperty<T>` on a generated child. It should subscribe to owner property changes and update the target through an explicit value source or local template-owned assignment chosen by the implementation.

Rationale: this keeps binding cheap, typed, and testable. It also avoids string paths and reflection in the hot path.

Alternative considered: string property paths, expression parsing, or reflection property lookup. Rejected because the roadmap explicitly wants modern typed APIs first.

### Presenters are minimal composition controls

`ContentPresenter` should materialize content using this priority: existing `UIElement` content, matching `DataTemplate`, text-like fallback where already supported by existing controls, then no child for null content. `ItemsPresenter` should use an `ItemsPanelTemplate` to create a retained panel root and then materialize item children through `DataTemplate` where provided.

Rationale: presenters provide the minimum composition surface required for useful templates without prematurely building a full items control, selection, or virtualization system.

Alternative considered: delay presenters until items controls. Rejected because control templates need a way to host content now.

### Template parts are diagnostics, not magic

`TemplatePartAttribute` should document expected named part types on a control class. The MVP should expose enough metadata for tests and diagnostics, but should not require runtime namescope lookup or throw just because a template omits a part.

Rationale: this keeps the API familiar without inheriting WPF's hidden runtime coupling.

Alternative considered: mandatory runtime part discovery. Rejected as too much magic for the current architecture.

## Risks / Trade-offs

- [Risk] Generated child ownership can conflict with existing content ownership. -> Use existing logical/visual child validation and define one owner for generated roots.
- [Risk] Template bindings can leak event subscriptions. -> Make `TemplateInstance` disposable/detachable and test that old bindings stop updating after template replacement.
- [Risk] Presenters can accidentally become a full binding/data system. -> Keep this phase limited to explicit content, data templates, and items panel templates.
- [Risk] Button conversion to template-backed visuals can change existing playground behavior. -> Add integration tests for layout, render, hit test, input, and command behavior before replacing hand-coded visuals.
- [Risk] Template-generated trees may over-invalidate. -> Route all changes through existing typed property invalidation and assert template replacement queues one subtree update, not repeated frame work.
