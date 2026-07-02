## Context

Cerneala already has two low-level foundations:

- `UI/Drawing`: backend-neutral draw commands plus MonoGame rendering.
- `UI/Input`: input snapshots, routed event metadata, basic routing, and command primitives.

`ROADMAPv2.md` changes the project direction from a WPF clone to a modern retained UI architecture: WPF-inspired names where useful, but typed, explicit, invalidation-driven, game-loop-friendly, and not XAML-first.

This change does not implement runtime code. It defines the MVP architecture contracts and documentation needed before implementation.

## Goals / Non-Goals

**Goals:**

- Capture the retained UI MVP as OpenSpec requirements.
- Document the v2 architecture above `UI/Drawing` and `UI/Input`.
- Define the frame loop: update every frame, recompute layout/render only when invalidated.
- Lock in confirmed design decisions from `ROADMAPv2.md`.
- Create a task list that can drive implementation later.

**Non-Goals:**

- Do not implement `UI/Core`, `UI/Elements`, `UI/Layout`, `UI/Rendering`, or controls yet.
- Do not rewrite `UI/Drawing`.
- Do not rewrite `UI/Input` in this change.
- Do not add markup, binding, templates, animation, accessibility, or advanced media yet.
- Do not split projects/packages yet.

## Decisions

### Decision: Treat `ROADMAPv2.md` as the active product roadmap

`ROADMAP.md` remains historical context. New implementation work should follow `ROADMAPv2.md` unless a later accepted change updates it.

Alternative considered: keep both roadmaps active. That creates conflicting guidance and forces every task to re-litigate WPF clone decisions.

### Decision: Add v2 project memory before runtime code

Add:

- `openspec/README.md`
- `openspec/project.md`
- `docs/architecture-v2.md`
- `docs/diagrams/retained-frame-loop.md`
- `docs/diagrams/ui-layer-boundaries.md`

These files make future sessions resumable without relying on chat history.

Alternative considered: jump directly into `UI/Core`. That risks implementing a typed property system without agreeing on retained tree, invalidation, and render-cache contracts.

### Decision: MVP uses logical and visual trees

The confirmed decision is to use separate logical and visual trees in MVP. The logical tree owns semantic composition and control ownership. The visual tree owns render/layout participation and visual children.

Alternative considered: a single retained tree. It is simpler, but the confirmed direction favors WPF-like composition semantics even though it increases MVP complexity.

### Decision: Replace `UiInputTree` as the future route table

The new retained tree model becomes the source of input routing. Existing `UI/Input` routed event concepts and tests remain valuable, but `UiInputTree` should not become a parallel permanent tree.

Alternative considered: feed `UiInputTree` from retained elements. That preserves more current code, but creates two tree sources that can drift.

### Decision: Use subtree render caches from the start

MVP render caching should support subtree caches, not only a root flattened command list. Unchanged frames must not recompute layout or regenerate render commands.

Alternative considered: root-only cache for MVP. It is simpler but conflicts with the confirmed performance direction.

### Decision: Invalidation is metadata-driven

Typed properties, resources, and style metadata declare whether a change affects measure, arrange, render, hit testing, style, or input visuals.

Style/value precedence is:

`local > animation > style visual state > style base > inherited > default`

Alternative considered: local/style/default only in MVP. Simpler, but it would force redesign once visual states and animation arrive.

### Decision: Full dirty work processing in MVP

MVP processes all dirty work deterministically each frame. `FrameBudget` exists only as a later optimization if profiling proves it is needed.

Alternative considered: budgeted scheduling from the start. It adds complexity and nondeterminism before there are large trees to justify it.

## Risks / Trade-offs

- [Risk] Logical and visual trees increase MVP complexity. -> Mitigation: document ownership rules before code and add tree consistency tests in implementation.
- [Risk] Replacing `UiInputTree` may discard useful routing behavior. -> Mitigation: preserve routed event metadata and port existing routing tests to the retained route model.
- [Risk] Subtree render caches can be over-engineered. -> Mitigation: require no-work-frame tests and keep cache invalidation rules minimal.
- [Risk] Style metadata affects invalidation before the styling system exists. -> Mitigation: define metadata contracts now, but implement only the subset required by MVP typed properties.
- [Risk] Golden-image tests can be fragile. -> Mitigation: pair them with command-list assertions and introduce golden images only after the first retained playground sample.

## Migration Plan

This change is documentation and planning only.

1. Add OpenSpec artifacts.
2. Add v2 project memory files.
3. Validate the OpenSpec change.
4. Later implementation changes can target the tasks produced here.

Rollback is removing the new OpenSpec change and v2 documentation files.

## Open Questions

- Should `ROADMAP.md` be explicitly marked as historical after `ROADMAPv2.md` is accepted?
- Should `architecture.md` link to `docs/architecture-v2.md` once it exists?
