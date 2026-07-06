# Cerneala Next Developer Preview Hardening Plan Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to execute these plans in order. This file is an index, not a substitute for the detailed plan files. Each detailed plan has RED/GREEN steps, concrete files, and verification commands.

**Goal:** Move Cerneala from Runtime Preview to Developer Preview Hardening: a developer can use the current retained UI stack with predictable keyboard navigation, mutation-safe layout authoring, clean lifecycle/subscription behavior, honest API scope guardrails, deterministic stress budgets, and useful getting-started docs/samples.

**Architecture Direction:** Keep the ROADMAPv2 architecture. Do not replace the retained tree, typed property system, scheduler, drawing command model, input routing, style/theme model, resource model, or MonoGame runtime seams. This batch hardens the product surface around existing capabilities. It intentionally avoids package split, native accessibility adapters, full IME, markup/sourcegen expansion, and advanced rendering/effects.

---

## Recommended Next Direction

Developer Preview Hardening is the highest-leverage next step because Core, Authoring, and Runtime Preview gates prove the framework works internally. The next failure mode is not missing a flashy feature; it is developer trust: keyboard traversal must feel normal, layout mutations must invalidate correctly, detached UI must not leak subscriptions, public/deferred scope must be explicit, stress behavior must stay bounded, and docs must show the supported code-first path.

This sequence is deliberately practical. It adds small missing contracts and quality gates without broad redesign.

## Execution Order

1. [ ] `2026-07-06-wire-tab-focus-navigation-contract.md`
   - Land first because usable keyboard focus is a visible developer/user feature and later samples/docs should dogfood it.
2. [ ] `2026-07-06-harden-layout-authoring-mutation-contracts.md`
   - Land second because code-first layout authoring currently needs mutation-safe Grid definition behavior before docs can recommend it.
3. [ ] `2026-07-06-close-retained-lifecycle-subscription-leaks.md`
   - Land before stress/docs gates so detached controls, resources, commands, bindings, templates, and observable lists do not keep hidden subscriptions alive.
4. [ ] `2026-07-06-add-preview-api-scope-guardrails.md`
   - Land after lifecycle/layout contracts so the documented Developer Preview surface can be named honestly and frozen/deferred areas stay fenced.
5. [ ] `2026-07-06-add-retained-stress-budget-tests.md`
   - Land after the API/lifecycle cleanup because stress tests should measure the hardened contracts, not known leaks or missing invalidation.
6. [ ] `2026-07-06-create-developer-preview-docs-and-sample-gate.md`
   - Land after behavior is stable so docs and the getting-started sample describe real supported behavior.
7. [ ] `2026-07-06-developer-preview-completion-gate.md`
   - Final integration gate proving the hardened Developer Preview is coherent and still retained/game-loop-friendly.

## Throughput Rules

- [ ] Use subagents aggressively for independent inspection, RED test drafting, implementation patches, and verification.
- [ ] Keep dependency order strict: do not implement a later plan before the current one is GREEN.
- [ ] Keep patches small and local.
- [ ] Write RED tests first unless a step explicitly says otherwise.
- [ ] Run targeted tests after each task and `dotnet test Cerneala.slnx` after each plan.
- [ ] Prefer fixing the owning layer over adding compatibility shims.
- [ ] Follow `AGENTS.md`: run `./Tools/scripts/New-FileTree.ps1`, read `FileTree.md`, use RoslynIndexer for C# search/navigation when available, and re-index after code/project-file changes.
- [ ] If Roslyn MCP transport fails, use the RoslynIndexer CLI path and record that fallback in the final notes.

## Do Not Build During This Sequence

- [ ] Do not split packages/projects.
- [ ] Do not expand `UI/Markup` or source generation.
- [ ] Do not add XAML compatibility.
- [ ] Do not add string-path binding to the hot path.
- [ ] Do not implement full IME, multiline text editing, spellcheck, rich text, or text shaping-perfect caret geometry.
- [ ] Do not build native accessibility adapters.
- [ ] Do not expand animation/storyboard behavior.
- [ ] Do not add gradients, shadows, path rendering, render targets, or new drawing primitives.
- [ ] Do not add new control families.
- [ ] Do not replace retained invalidation with immediate-mode rebuilds.
- [ ] Do not introduce WPF compatibility behavior only because a name is familiar.

## Completion Gate

- [ ] `dotnet test Cerneala.slnx` passes.
- [ ] `dotnet test` passes.
- [ ] Tab and Shift+Tab move focus through retained visual tree order, skip invalid targets, and respect handled key events.
- [ ] Grid row/column definition add/remove/clear and width/height changes invalidate retained layout/render/hit-test correctly and produce no work on unchanged frames.
- [ ] Detaching UI clears command, binding, item-source, template, resource-dependency, and queue state that would otherwise leak or process detached elements.
- [ ] Developer Preview public/deferred scope is documented and guarded by architecture tests.
- [ ] Stress-budget tests prove bounded retained work for large static trees, large lists, theme/resource changes, command refresh, focus traversal, and draw loops.
- [ ] Getting-started docs and sample build a supported code-first UI without markup/sourcegen and preserve retained no-work/draw-purity invariants.
- [ ] Developer Preview completion gate proves all of the above in one slice.

## Archive step

After all plans are GREEN and both full test commands pass, archive the repository with the existing script from the repository root. Do not invent a new archive script.

```powershell
cd C:\Users\Shadow\Desktop\Cerneala
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\scripts\Archive-Repo.ps1 -RepoRoot .
```
