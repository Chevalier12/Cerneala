## Context

Cerneala already has:

- a typed state model under `UI/Core`;
- backend-neutral drawing commands under `UI/Drawing`;
- input snapshots, routed event metadata, command primitives, and a simple `UiInputTree` route table under `UI/Input`;
- project memory that confirms MVP uses separate logical and visual trees.

`ROADMAPv2.md` section 3 is the next runtime slice. Its introductory paragraph still mentions a modern single tree, but later decisions and `openspec/project.md` confirm separate logical and visual trees for MVP. This change follows the confirmed decision and should update the roadmap wording during implementation.

## Goals / Non-Goals

**Goals:**

- Add `UIElement` as the retained element base built on `UiObject`.
- Represent logical and visual parent/child relationships explicitly.
- Enforce parent ownership rules and reject invalid reparenting.
- Add root ownership through `UIRoot`.
- Add attach/detach lifecycle hooks and tree versioning.
- Assign stable `UiElementId` values while elements remain attached.
- Provide deterministic tree walking helpers.
- Store routed event handlers on retained elements.
- Build or update a low-level input route map from the retained tree.
- Preserve `UI/Core`, `UI/Drawing`, and `UI/Input` boundaries.

**Non-Goals:**

- Do not implement layout measurement or arrangement.
- Do not implement retained invalidation queues or frame scheduling.
- Do not implement render caches.
- Do not implement controls, templates, styling, resources, binding, or markup.
- Do not replace all routed event routing behavior in this change.
- Do not split projects.

## Decisions

### Decision: `UIElement` derives from `UiObject`

`UIElement` should reuse the typed property system for enabled/visible state and future layout/render/input properties.

Alternative considered: keep `UIElement` separate from `UiObject` for now. That would avoid early coupling, but every later retained type would need duplicated state behavior.

### Decision: Model logical and visual parentage separately

Each element can have at most one logical parent and at most one visual parent. Logical relationships own semantic/content relationships. Visual relationships own layout, render, hit-test, and route participation.

Alternative considered: a single parent plus generated children. That is simpler, but conflicts with the confirmed MVP decision and makes later templates/control composition harder.

### Decision: Reuse `UIElementCollection` with an explicit role

`UIElementCollection` should be the owned child collection implementation for both logical and visual children. The collection must know whether it is managing logical or visual parentage.

Alternative considered: separate `LogicalElementCollection` and `VisualElementCollection`. That avoids a role enum but duplicates validation, parent assignment, collection change, and tests.

### Decision: Reparenting is explicit removal then add

Adding a child that already has the relevant parent kind must throw unless it is first removed from its previous parent.

Alternative considered: automatic reparenting. That is convenient, but hides tree mutation, lifecycle, dirty propagation, and route changes.

### Decision: `UIRoot` owns attachment, ids, and route ownership

Elements receive stable `UiElementId` values when attached to a root. The ids remain stable while attached and are released or invalidated when detached.

Alternative considered: assign ids at construction time. That is simpler, but makes ids global and detached elements look routable when they are not.

### Decision: Element lifecycle is internal and deterministic

Attach/detach hooks run in tree order and update root, parentage, tree version, and ids before external consumers can observe the new attached state.

Alternative considered: fire collection notifications first. That exposes half-mutated trees to consumers.

### Decision: Retained input route map is generated from the visual tree

The first route bridge maps `UIElement` to `UiElementId` and can feed existing low-level routing tests. Visual ancestry is the route source because hit testing and routed pointer events are visual concerns.

Logical ancestry remains important for command/focus ownership later, but this change should not overload the existing `UiInputTree` as a second retained tree.

### Decision: Visibility and enabled input policy is represented but not fully laid out

This change can represent enabled/visible state needed for route inclusion. Full layout semantics for `Hidden` and `Collapsed` belong to the layout phase.

## Risks / Trade-offs

- [Risk] Separate logical and visual trees increase complexity. -> Mitigation: keep parent kinds explicit and test each ownership rule directly.
- [Risk] Route bridge may make `UiInputTree` look permanent. -> Mitigation: name new types around retained elements and document `UiInputTree` as a low-level route table only.
- [Risk] Lifecycle hooks can become event soup. -> Mitigation: keep hooks minimal and deterministic; defer public event surfaces unless tests require them.
- [Risk] Element ids can leak after detach. -> Mitigation: test id stability while attached and id removal after detach.
- [Risk] Layout/render placeholders may invite premature implementation. -> Mitigation: store slots/flags only when needed by tree contracts; leave actual layout/render behavior to later changes.

## Migration Plan

1. Add retained element production types under `UI/Elements`.
2. Add retained input bridge types under `UI/Input`.
3. Add focused tests for tree ownership, collections, root attachment, lifecycle, walking, and route building.
4. Update `ROADMAPv2.md` section 3 to remove stale single-tree wording and add OpenSpec planning checklist entries.
5. Run `dotnet test` and `openspec validate add-retained-element-tree --strict`.

Rollback is removing the new OpenSpec change and any implementation files from this slice before archive.

## Open Questions

- Should detached elements keep their last `UiElementId` for diagnostics, or expose no id until reattached?
- Should route building include logical ancestors for command routing in this slice, or defer command route merging to the input/focus/command bridge phase?
- Should generated visual children be visible through public `VisualChildren`, or only through `IElementChildHost` diagnostics until controls exist?
