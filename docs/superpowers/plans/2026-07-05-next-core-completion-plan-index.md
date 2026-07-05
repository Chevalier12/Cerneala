# Cerneala Next Core Completion Plan Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to execute these plans in order. This is an index, not a substitute for the detailed plan files. Each detailed plan has RED/GREEN steps and file-level instructions.

**Goal:** Reach a fast, credible Cerneala MVP/Core Preview without expanding WPF-legacy or optional surface area.

**Architecture Direction:** Do not add more controls/media/markup/accessibility/animation surface until these foundation plans are closed. The core target is a retained, game-loop-friendly, typed UI framework where `Update` does dirty work and `Draw` submits cached commands.

---

## Execution Order

1. [ ] `2026-07-05-fix-focus-visibility-semantics.md`
   - Must land first because focus and visibility affect input, text editing, controls, accessibility, and styling.
2. [ ] `2026-07-05-implement-inherited-property-tree-propagation.md`
   - Land before root resources and vertical slice so ambient font/foreground behavior is predictable.
3. [ ] `2026-07-05-root-owned-resource-invalidation.md`
   - Land before large samples rely on root-owned fonts/images/theme resources.
4. [ ] `2026-07-05-clarify-layout-scheduler-contract-and-diagnostics.md`
   - Land before claiming retained performance; diagnostics must not lie.
5. [ ] `2026-07-05-consolidate-button-content-composition.md`
   - Land before adding or polishing more controls; content ownership must be consistent.
6. [ ] `2026-07-05-create-retained-ui-mvp-vertical-slice.md`
   - Final gate proving the framework acts as one coherent retained UI runtime.

## Do Not Build During This Sequence

- [ ] Do not add new controls beyond what the vertical slice already needs.
- [ ] Do not expand `UI/Markup` or source generation.
- [ ] Do not expand `UI/Animation` or `Storyboard` semantics.
- [ ] Do not add WPF compatibility APIs.
- [ ] Do not implement advanced rendering/media descriptors unless they become real drawing commands and backend behavior.
- [ ] Do not split projects/packages until the core/adapters split criteria are ready.

## Completion Gate

- [ ] Full `dotnet test Cerneala.slnx` passes.
- [ ] `UiHost.Update(...)` processes dirty work and commits retained root commands.
- [ ] `UiHost.Draw(...)` submits only committed commands.
- [ ] Unchanged retained app sample frame reports no layout/render/hit-test rebuilds.
- [ ] Focus cannot land on arbitrary visuals.
- [ ] Visibility semantics are centralized and tested.
- [ ] Inherited font/foreground values propagate through the retained tree.
- [ ] Resource changes are observed by root, not by control-specific `ResourceStore` subscriptions.
- [ ] Layout diagnostics distinguish queued scheduler work from actual recursive layout calls.
- [ ] `Button` uses `ContentControl` content ownership.
