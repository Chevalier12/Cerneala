## Context

Cerneala now has typed state, retained elements, retained invalidation queues, and a frame scheduler. `ROADMAPv2.md` section 5 is the next slice: a WPF-inspired measure/arrange layout system without copying WPF's historical complexity.

The current retained tree can mark layout work dirty, but there is no layout geometry, no `Measure`/`Arrange` contract, no cached desired size, no arranged bounds, and no panel behavior. Rendering and hit testing are later phases, but they need stable layout output.

## Goals / Non-Goals

**Goals:**

- Add layout-specific geometry: `LayoutSize`, `LayoutPoint`, `LayoutRect`, and `Thickness`.
- Add layout policy types: alignment, visibility, layout rounding, measure context, arrange context, and layout result.
- Extend `UIElement` with layout state and overridable measure/arrange hooks.
- Add `LayoutManager` that consumes `LayoutQueue`, caches measure/arrange results, and invalidates render/hit-test work when layout output changes.
- Add `LayoutBoundary` so upward propagation can stop at explicit roots or subtrees.
- Add MVP panels: `Panel`, `Canvas`, and `StackPanel`.
- Prove no-op property sets and unchanged constraints avoid unnecessary layout work.

**Non-Goals:**

- Do not implement retained rendering or render cache composition.
- Do not implement real hit-test geometry beyond layout output required by later phases.
- Do not implement styling, templates, or markup.
- Do not implement full WPF layout behavior, dependency properties, attached properties, layout transforms, virtualization, or infinite measure loops.
- Do not implement `Grid`, `GridLength`, `ColumnDefinition`, or `RowDefinition` unless the implementation proves they are required for MVP.

## Decisions

### Decision: Layout geometry is separate from drawing geometry

`LayoutSize`, `LayoutPoint`, and `LayoutRect` live under `UI/Layout` and do not alias `DrawSize`, `DrawPoint`, or `DrawRect`.

Rationale: layout has different semantics, including unconstrained available size and cached desired sizes. Drawing command geometry is backend-neutral command data, not layout state.

Alternative considered: reuse drawing primitives for layout. Rejected because it would blur ownership and make rendering boundaries harder to audit.

### Decision: `UIElement` owns layout state, panels override layout behavior

Base `UIElement` stores desired size, arranged bounds, layout version, visibility, margin, alignment, and optional layout boundary state. It exposes protected virtual measure/arrange hooks. Panel types override those hooks to measure and arrange visual children.

Rationale: controls need a common layout contract, but panel behavior belongs in panel subclasses, not in the base element.

Alternative considered: make `LayoutManager` contain all layout algorithms externally. Rejected because panel-specific behavior would become a giant switch or registry too early.

### Decision: `LayoutManager` consumes existing `LayoutQueue`

The scheduler already has `LayoutQueue` for measure/arrange work. This change gives that queue real processing through `LayoutManager`, while preserving scheduler phase order.

Rationale: invalidation already owns what is dirty; layout should own how layout work is processed.

Alternative considered: create a second layout queue. Rejected because duplicate queues would create sync bugs and stale dirty work.

### Decision: Layout caches are keyed by constraints and versions

Measure results are cached by available size and element layout-relevant version. Arrange results are cached by final rect and layout-relevant version. A no-op property set must not invalidate layout.

Rationale: this is the retained-mode performance promise. Repeating frames or parent work should not force unchanged children to recalculate when constraints did not change.

Alternative considered: always remeasure subtree when parent is dirty. Rejected because it defeats the retained model immediately.

### Decision: `Collapsed` removes elements from layout and hit-test participation

Visibility values are `Visible`, `Hidden`, and `Collapsed`. `Hidden` keeps layout space but is not visible/input-targetable. `Collapsed` contributes zero size, is not arranged as visible content, and is excluded from hit testing.

Rationale: this keeps WPF-familiar behavior while remaining explicit and testable.

Alternative considered: only support a boolean `IsVisible`. Rejected because layout-reserved invisible elements are a real UI need and already documented in architecture v2.

### Decision: Rendering integration is invalidation only

Layout output can schedule render and hit-test invalidation when arranged bounds change, but it does not generate draw commands and does not depend on `UI/Drawing`.

Rationale: section 5 must provide layout output for section 6 without dragging rendering into this change.

Alternative considered: generate placeholder render bounds or drawing commands from layout. Rejected as cross-phase scope creep.

### Decision: `Grid` stays planned, not required

`Grid`, `GridLength`, `ColumnDefinition`, and `RowDefinition` remain roadmap entries but are not required in this OpenSpec change.

Rationale: `Canvas` and `StackPanel` are enough to prove the core layout contract, caching, invalidation, and panels. Grid is a large feature with many edge cases and can be added after core layout is solid.

Alternative considered: implement Grid in the same slice. Rejected as too much blast radius for MVP layout foundation.

## Risks / Trade-offs

- [Risk] Layout caching can produce stale results if versioning is too narrow. -> Mitigation: include tests for no-op sets, changed constraints, and changed child layout state.
- [Risk] Layout invalidation can over-propagate and remeasure too much. -> Mitigation: add layout boundary behavior and tests for unchanged child constraints.
- [Risk] Visibility semantics can conflict with existing `UIElement.IsVisible`. -> Mitigation: introduce explicit `Visibility` while preserving or mapping existing visibility behavior with focused tests.
- [Risk] Panels can accidentally depend on rendering or input details. -> Mitigation: keep panel tests strictly in `UI/Layout` and scan for backend/drawing dependencies.
- [Risk] Deferring Grid may feel incomplete. -> Mitigation: keep Grid checklist entries in `ROADMAPv2.md` unchecked and document that Grid is not required for this change.

## Migration Plan

1. Add layout primitives and policy types with focused unit tests.
2. Extend `UIElement` and `UIRoot` with layout state and root layout manager ownership.
3. Implement `LayoutManager` against existing `LayoutQueue`.
4. Add `Panel`, `Canvas`, and `StackPanel` behavior and tests.
5. Update `ROADMAPv2.md` section 5 checkboxes as implementation tasks complete.
6. Validate with `dotnet test`, `openspec validate add-layout-system --strict`, and `openspec validate --all --strict`.

Rollback before archive is simple: remove `UI/Layout`, remove layout state additions from retained elements/root, and remove the OpenSpec change directory.

## Open Questions

- Should `UIElement.IsVisible` remain as a bool alias for `Visibility != Collapsed && Visibility != Hidden`, or should it become obsolete after `Visibility` exists?
- Should layout rounding default to off for exact tests, or on for pixel-friendly rendering? MVP should pick one explicit default and test it.
