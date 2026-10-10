# Cerneala Next Authoring Preview Plan Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to execute these plans in order. This file is an index, not a substitute for the detailed plan files. Each detailed plan has RED/GREEN steps, concrete files, and verification commands.

**Goal:** Move Cerneala from Core Preview to Authoring Preview: a developer can build a small real retained UI screen with typed data flow, command enablement, text entry, themed templated controls, observable lists, and platform-neutral semantics without WPF legacy magic.

**Architecture Direction:** Keep the ROADMAPv2 architecture. Do not replace the retained tree, typed property system, scheduler, drawing layer, input routing, or style/theme model. This sequence closes authoring contracts that are already implied by existing code: command state refresh, explicit typed binding lifetime, TextBox editing visuals, code-first templates, observable item sources, and retained semantics. Keep all changes small, local, and invalidation-driven.

---

## Execution Order

1. [ ] `2026-07-05-retained-command-state-refresh.md`
   - Must land first because later binding/text samples need command enablement to update predictably without a WPF-style global `CommandManager`.
2. [ ] `2026-07-05-typed-binding-lifetime-and-two-way-text.md`
   - Land before TextBox/sample work so text input can update model state through explicit typed bindings.
3. [ ] `2026-07-05-textbox-editing-viewport-and-caret-contract.md`
   - Land before authoring sample work because text entry must be visibly usable, not only state-mutating.
4. [ ] `2026-07-05-template-content-presenter-state-contract.md`
   - Land before list/template sample work so default themed controls can be templated without losing content, state, or retained caching.
5. [ ] `2026-07-05-observable-items-source-and-recycling-stability.md`
   - Land before the final gate so list UI can consume observable data without copying everything or rebuilding unrelated containers.
6. [ ] `2026-07-05-retained-semantics-tree-core-contract.md`
   - Land after controls/data/text contracts so the platform-neutral semantics tree describes real authoring scenarios.
7. [ ] `2026-07-05-authoring-preview-completion-gate.md`
   - Final integration gate proving the authoring slice is coherent and still retained/game-loop-friendly.

## Throughput Rules

- [ ] Use subagents aggressively for independent file inspection, RED test drafts, implementation patches, and verification.
- [ ] Keep dependency order strict: do not implement a later plan before the current one is GREEN.
- [ ] Keep patches small and local.
- [ ] Write RED tests first unless a step explicitly says otherwise.
- [ ] Run targeted tests after each task and full `dotnet test Cerneala.slnx` after each plan.
- [ ] Prefer fixing the owning layer over adding compatibility shims.
- [ ] Update diagnostics/tests when adding a scheduler phase or invalidation flag.

## Do Not Build During This Sequence

- [ ] Do not expand `UI/Markup` or source generation.
- [ ] Do not add string-path binding to the hot path.
- [ ] Do not implement full MVVM, reflection property walking, or WPF `DataContext` inheritance.
- [ ] Do not implement full IME, multiline text editing, text shaping-perfect caret geometry, or clipboard integration.
- [ ] Do not build native accessibility platform adapters.
- [ ] Do not expand animation/storyboard behavior.
- [ ] Do not add gradients, shadows, geometry/path rendering, render targets, or new drawing backend primitives.
- [ ] Do not split projects/packages.
- [ ] Do not add new control families.
- [ ] Do not introduce WPF compatibility behavior only because a name is familiar.
- [ ] Do not replace retained invalidation with immediate-mode rebuilds.

## Completion Gate

- [ ] `dotnet test Cerneala.slnx` passes.
- [ ] `dotnet test` passes.
- [ ] Command enablement refreshes through explicit retained work, not a global requery loop.
- [ ] Typed UI property bindings are disposable and element-owned bindings do not leak after detach.
- [ ] `TextBox` supports visible single-line caret/selection behavior and two-way text updates without full IME scope creep.
- [ ] Default themed `Button` can be templated through `ControlTemplate`, `ContentPresenter`, and template bindings.
- [ ] `ContentPresenter` can present string content through a retained `TextBlock` without per-frame child recreation.
- [ ] `ItemsControl`/`ListBox` can consume observable item sources and keep unrelated realized containers stable.
- [ ] Platform-neutral semantics are cacheable/invalidation-driven and describe Button, TextBox, TextBlock, ItemsControl/ListBox, and selected list items.
- [ ] Authoring preview sample proves typed binding, text input, command state, templated button, observable list mutation, semantics, first-frame work, unchanged no-work frame, and draw purity.
