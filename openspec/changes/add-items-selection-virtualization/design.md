## Context

Cerneala already has retained elements, typed properties, layout, retained rendering/cache invalidation, routed input, commands, styling, code-first templates, content/data presenters, scroll viewers, and scroll presenters. `ItemsPresenter` exists today as a simple retained materializer, but it recreates a panel and all item children when inputs change and has no container identity, selection state, recycling, or virtualization contract.

Section 17 builds the next layer: retained item controls that can represent large data sets without creating every child every frame. The work must remain backend-neutral and must integrate with existing templates, `ScrollViewer`/`IScrollInfo`, retained layout, hit testing, routed input, visual state, and invalidation.

## Goals / Non-Goals

**Goals:**
- Add item infrastructure: `ItemsControl`, `ItemCollection`, `ItemContainerGenerator`, `ItemContainerRecyclePool`, and an upgraded `ItemsPresenter`.
- Add selection infrastructure: `SelectionModel`, `SelectionModel<T>`, and `Primitives/Selector`.
- Add concrete retained controls: `ListBox`, `ListBoxItem`, `ComboBox`, `TabControl`, and `TabItem`.
- Add virtualization layout primitives: `VirtualizingStackPanel`, `VirtualizationContext`, and `RealizationWindow`.
- Realize only items inside the current realization window, with optional cache before/after the visible range.
- Preserve generated container identity for realized items and recycle unrealized containers when possible.
- Ensure scroll changes update the realization window and invalidate only affected layout/render/hit-test work.
- Ensure selection changes invalidate selected/unselected containers without rebuilding unrelated realized containers.
- Add focused tests matching the roadmap files and acceptance checklist.

**Non-Goals:**
- No data observation or binding-light APIs from section 18.
- No grouped collection views, sorting, filtering, or incremental observable list contracts.
- No advanced keyboard navigation polish beyond minimal retained click/selection behavior.
- No editable combo box text input, autocomplete, or popup-window implementation.
- No accessibility tree or UI automation support.
- No arbitrary-grid virtualization; this phase targets stack/list virtualization first.

## Decisions

### Item containers are generated and recycled explicitly

`ItemContainerGenerator` should map item indexes/data to retained item containers and preserve identity while an item stays realized. `ItemContainerRecyclePool` should store detached containers by container type or compatible key so unrealized containers can be reused without rebuilding templates every scroll tick.

Rationale: container lifetime is the core of retained item controls. If item generation is hidden inside `ItemsPresenter`, virtualization and selection will become tightly coupled and fragile.

Alternative considered: let `ItemsPresenter` create all item children directly as it does today. Rejected because it cannot satisfy retained realization or recycling requirements for large lists.

### ItemsControl owns item data and presentation policy

`ItemsControl` should expose typed properties for `Items`, `ItemTemplate`, and `ItemsPanel`, plus an `ItemContainerGenerator`. It should be the reusable base for `Selector`, `ListBox`, `ComboBox`, and `TabControl`.

Rationale: item ownership, item templates, and panel policy are shared. Keeping them in a base control avoids duplicating generation rules across every item control.

Alternative considered: each concrete control owns its own generator. Rejected because selection controls would diverge immediately.

### SelectionModel is independent of visual containers

`SelectionModel` and `SelectionModel<T>` should track selected indexes/items independently from generated containers. `Selector` should bridge selection changes into container visual state.

Rationale: selected state must survive virtualization. If selection lives only on realized containers, scrolling selected items out of view loses state.

Alternative considered: store selection only on `ListBoxItem`/`TabItem`. Rejected because virtualization detaches containers.

### Virtualization is expressed as layout state

`VirtualizationContext` and `RealizationWindow` should describe viewport offset, viewport size, item extent estimates, cache length, and realized index range. `VirtualizingStackPanel` should use that context to measure/arrange only realized children.

Rationale: virtualization is a layout concern with rendering/input consequences. Putting the realized window into layout primitives makes scroll-driven realization deterministic and testable.

Alternative considered: make `ScrollViewer` directly create/destroy item containers. Rejected because it couples scrolling to item controls and blocks reuse by future panels.

### Scrolling integration stays retained and explicit

`ItemsControl`/`ItemsPresenter` should integrate with existing `ScrollViewer`, `ScrollContentPresenter`, and `IScrollInfo` through explicit offset/extent/viewport state. Scroll offset changes should update realization windows and invalidate affected arrange/render/hit-test work, not force unrelated measure or full tree rebuilds.

Rationale: section 16 already established retained scrolling. Section 17 should extend that model, not create a second scroll pipeline.

Alternative considered: immediate-mode list drawing inside `ListBox`. Rejected because it bypasses retained hit testing, templates, styling, and input routing.

### Concrete controls are thin over shared primitives

`ListBox` should derive from `Selector` and use `ListBoxItem` containers. `ComboBox`, `TabControl`, and `TabItem` should reuse the same selection/container concepts, with minimal behavior needed for this phase.

Rationale: this keeps the phase focused on the shared item-control system. Rich combo box popups and tab layout polish can evolve later.

Alternative considered: fully featured concrete controls first. Rejected as too much scope and too easy to bake behavior into the wrong layer.

## Risks / Trade-offs

- [Risk] Recycling can accidentally reuse stale content or selection state. -> Reset container item/index/selection state during prepare/clear and add tests for recycled container hygiene.
- [Risk] Virtualization range math can go off by one near viewport boundaries. -> Centralize `RealizationWindow` calculation and test start/end/cache edge cases.
- [Risk] Selection and virtualization can fight when selected items scroll out and back in. -> Store selection in `SelectionModel`, then reapply selected visual state when containers are prepared.
- [Risk] Scroll offset changes can trigger full measure/rebuild work. -> Add tests proving unrelated realized containers are preserved and data updates do not rebuild unrelated containers.
- [Risk] Existing `ItemsPresenter` behavior may be broken by upgrade. -> Preserve the non-virtualized path and existing template/item panel behavior with regression tests.
- [Risk] `ComboBox` and `TabControl` can explode scope. -> Implement retained, selection-backed MVP behavior and defer rich popup/text/navigation behavior to later phases.
