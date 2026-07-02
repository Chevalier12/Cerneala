## Context

Cerneala already has `UiObject`/`UiProperty` for typed state and `UIElement`/`UIRoot` for a retained logical and visual tree. `UiObject` can report metadata options such as `AffectsMeasure`, `AffectsArrange`, `AffectsRender`, `AffectsHitTest`, and `AffectsStyle` through `IUiPropertyOwner`, but `UIElement` does not yet translate those hooks into retained dirty state.

`ROADMAPv2.md` section 4 defines the next architecture slice: retained invalidation and a frame scheduler. The game loop may call update and draw every frame, but unchanged retained trees must not remeasure, rearrange, rebuild render commands, or rebuild hit-test data.

This change provides the invalidation contracts and deterministic queues that later layout, rendering, and hit-testing phases will consume. It must remain backend-neutral and must not introduce MonoGame or drawing backend references into retained UI core code.

## Goals / Non-Goals

**Goals:**

- Add explicit invalidation flags for measure, arrange, render, text, image, resource, style, input visual, hit-test, and subtree work.
- Add compact per-element dirty state with version stamps for queue/scheduler decisions.
- Add invalidation requests and propagation rules that map state/resource/input/style changes to concrete dirty work.
- Add stable layout, render, and hit-test queues that deduplicate elements by reference and process in deterministic retained-tree order.
- Add `UiFrameScheduler` that processes dirty work through named phases and reports `FrameStats`.
- Add `InvalidationTrace` diagnostics for tests and future tooling.
- Connect `UIElement` property invalidation hooks to retained invalidation without coupling `UI/Core` to invalidation implementation.
- Update `ROADMAPv2.md` section 4 checkboxes as tasks complete.

**Non-Goals:**

- Do not implement the real layout system from roadmap section 5.
- Do not implement retained rendering or render-cache composition from roadmap section 6.
- Do not implement real hit-test geometry or focus routing.
- Do not implement frame-budget deferral; `FrameBudget` is represented as an explicit MVP no-deferral policy.
- Do not introduce backend-specific dependencies.

## Decisions

### Decision: Keep `UiPropertyOptions` and `InvalidationFlags` separate

`UiPropertyOptions` describes metadata attached to typed state. `InvalidationFlags` describes retained dirty work. `UIElement` is the translation boundary between them.

Rationale: `UI/Core` must stay independent of layout, rendering, input, and retained scheduler types. A direct dependency from `UiProperty` metadata to invalidation queues would make the core property system harder to reuse and test.

Alternative considered: reuse `UiPropertyOptions` as the dirty flag enum. Rejected because resource, text, image, input visual, and subtree invalidations are retained/UI concepts, not generic property metadata concepts.

### Decision: Dirty state lives on `UIElement`

Each `UIElement` owns a `DirtyState` instance that records active flags and version stamps. Public invalidation entry points live on `UIElement`, while queue ownership and scheduling live at `UIRoot`.

Rationale: retained invalidation is per element, and future layout/rendering systems need a single source of truth for whether an element is dirty. Keeping queue ownership at the root prevents disconnected subtrees from being scheduled accidentally.

Alternative considered: store dirty state only in queues. Rejected because flags need to survive queue rebuilds and diagnostics need to inspect element state directly.

### Decision: `UIRoot` is the invalidation sink for attached elements

`UIElement` implements the property invalidation owner hook and creates `InvalidationRequest` values. If the element is attached, requests are sent to its root. If it is detached, the element keeps local dirty state but does not enqueue root work until attached.

Rationale: this keeps unattached elements usable and deterministic while making attached work run through one scheduler.

Alternative considered: throw when detached elements are invalidated. Rejected because controls are commonly configured before being added to a tree.

### Decision: Queues deduplicate by reference and process in deterministic order

`LayoutQueue`, `RenderQueue`, and `HitTestQueue` deduplicate `UIElement` instances by reference. Processing order follows retained visual-tree order where tree order matters. Layout work keeps measure before arrange.

Rationale: retained element identity is reference identity. Deterministic processing avoids flaky tests and makes diagnostics useful.

Alternative considered: use insertion order only. Rejected because parent/child invalidation order matters for later layout and render phases.

### Decision: MVP scheduler processes all dirty work

`UiFrameScheduler.ProcessFrame()` processes all queued work until the scheduler is stable. `FrameBudget` exists as a value object or policy placeholder but does not defer work in MVP.

Rationale: correctness and observability matter more than partial scheduling before large-tree performance problems exist.

Alternative considered: implement a real time/operation budget now. Rejected as premature and likely to hide correctness bugs.

### Decision: Phase processors are replaceable hooks

The scheduler can process queue entries through small delegates or interfaces for measure, arrange, render-cache, and hit-test phases. MVP tests can use fake processors that count work. Later layout/rendering changes can replace the fakes with real systems without rewriting the scheduler.

Rationale: section 4 must prove scheduling and no-work-frame behavior without implementing sections 5 and 6.

Alternative considered: make the scheduler directly call future `LayoutManager` and `RetainedRenderer` types. Rejected because those types do not exist yet and would force cross-phase implementation.

### Decision: Dirty flags clear only after successful phase processing

The queue processor clears flags for the phase only after the configured processor succeeds. If a processor throws, the dirty flags and queued work remain available for retry or diagnostics.

Rationale: clearing before success would lose work and make failures corrupt retained state.

Alternative considered: clear before processing to avoid duplicate work. Rejected because it turns exceptions into silent stale UI.

## Risks / Trade-offs

- [Risk] Placeholder processors could become too abstract and awkward for real layout/rendering. -> Mitigation: keep the processor contract narrow: input element, phase, and stats update only.
- [Risk] Dirty propagation rules may need refinement once real layout boundaries exist. -> Mitigation: implement a minimal `LayoutBoundary` stop hook or boundary predicate placeholder, and cover current behavior with tests.
- [Risk] Text/image/resource flags may not have full systems yet. -> Mitigation: map them to conservative measure/render/hit-test effects now and keep specialized resource systems for later phases.
- [Risk] Detached dirty state can surprise callers if it enqueues work on attach. -> Mitigation: document and test attach behavior explicitly.
- [Risk] Frame stats can become misleading if processors are fake. -> Mitigation: stats count scheduled/processed phase work, not real rendering output, until rendering exists.

## Migration Plan

1. Add invalidation primitives and tests without changing public drawing/input behavior.
2. Connect `UIElement` property invalidation to dirty state and root queues.
3. Add scheduler and fake phase-processor tests proving no-work-frame behavior.
4. Update `ROADMAPv2.md` section 4 checkboxes when implementation tasks are complete.
5. Validate with `dotnet test`, `openspec validate add-retained-invalidation-frame-scheduler --strict`, and `openspec validate --all --strict`.

Rollback is simple while this change is unarchived: remove the new invalidation files and revert the `UIElement`/`UIRoot` integration.

## Open Questions

- Should detached dirty state enqueue automatically on attach, or should attach mark a conservative initial measure/render/hit-test invalidation? MVP should choose one behavior and test it.
- Should `InputVisual` always map to render-only by default, or should it require explicit metadata before doing anything? Roadmap says render-only unless a control maps it to layout-affecting properties; MVP should encode that default.
