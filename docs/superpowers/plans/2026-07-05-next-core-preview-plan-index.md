# Cerneala Next Core Preview Plan Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to execute these plans in order. This file is an index, not a substitute for the detailed plan files. Each detailed plan has RED/GREEN steps, concrete files, and verification commands.

**Goal:** Move Cerneala from MVP vertical slice to a credible Core Preview without growing WPF legacy surface area. The target remains a modern retained, game-loop-friendly, strongly typed UI runtime where `Update(...)` performs invalidation-driven work and `Draw(...)` submits cached draw commands only.

**Architecture Direction:** Keep the existing ROADMAPv2 architecture. Do not redesign the retained tree, property system, input router, drawing layer, or scheduler. Close the contracts a developer will hit immediately: viewport/input ordering, text layout API, keyboard activation, retained input bindings, theme/style use, list/scroll retained behavior, and one final Core Preview gate.

---

## Execution Order

1. [ ] `2026-07-05-fix-viewport-and-pre-input-frame-contract.md`
   - Must land first because input hit-testing and focus require committed layout/hit-test cache before dispatch.
2. [ ] `2026-07-05-complete-textblock-layout-contract.md`
   - Land before theme/list samples depend on real text wrapping and stable text layout identity.
3. [ ] `2026-07-05-wire-keyboard-control-activation.md`
   - Land before retained input bindings so handled key semantics and default control activation are explicit.
4. [ ] `2026-07-05-wire-minimal-retained-input-bindings.md`
   - Land before sample/core gate work depends on keyboard shortcuts and command routing.
5. [ ] `2026-07-05-default-theme-and-style-vertical-slice.md`
   - Land before final sample gate so visuals prove style/theme architecture, not hard-coded colors.
6. [ ] `2026-07-05-items-scroll-data-vertical-slice.md`
   - Land before Core Preview gate so list rendering, scrolling, selection, and retained invalidation are proven together.
7. [ ] `2026-07-05-core-preview-completion-gate.md`
   - Final integration gate proving Cerneala behaves like a coherent retained UI framework.

## Throughput Rules

- [ ] Use subagents aggressively for independent file inspection, RED test drafts, implementation patch review, and targeted test verification.
- [ ] Keep dependency order strict: do not implement a later plan before the current one is GREEN.
- [ ] Keep patches small and local.
- [ ] Write RED tests first unless a step explicitly says otherwise.
- [ ] Run targeted tests after each task and full `dotnet test Cerneala.slnx` after each plan.
- [ ] Prefer fixing the owning layer over adding compatibility shims.

## Do Not Build During This Sequence

- [ ] Do not expand `UI/Markup` or source generation.
- [ ] Do not implement full TextBox/IME/rich text.
- [ ] Do not build accessibility platform adapters.
- [ ] Do not expand `UI/Animation`, transitions, or `Storyboard` behavior.
- [ ] Do not implement gradients, shadows, geometry/path rendering, render targets, or new drawing primitives.
- [ ] Do not split projects/packages.
- [ ] Do not add new control families.
- [ ] Do not introduce WPF compatibility behavior only because a name is familiar.
- [ ] Do not replace retained invalidation with immediate-mode rebuilds.

## Completion Gate

- [ ] `dotnet test Cerneala.slnx` passes.
- [ ] `dotnet test` passes.
- [ ] `UiHost.Update(...)` processes pre-existing layout/render/hit-test work before input dispatch when needed.
- [ ] `UiHost.Draw(...)` never creates retained layout/render/hit-test work.
- [ ] Viewport changes invalidate measure, arrange, render, and hit-test coherently.
- [ ] `TextBlock` exposes and uses `TextWrapping`/`TextTrimming` in measure and render.
- [ ] Focused `Button` works with Enter and Space without a new gesture framework.
- [ ] `UIElement` owns minimal retained `InputBindings` wired through focus route and command router.
- [ ] Default theme/style is used by the retained sample for core button/text/surface visuals.
- [ ] `ItemsControl`/`ListBox` + `ScrollViewer` are proven by retained vertical-slice tests.
- [ ] Core Preview test proves first-frame work, unchanged no-work frame, draw purity, mouse/keyboard command execution, theme mutation, text wrapping resize, list mutation, and scroll invalidation.
