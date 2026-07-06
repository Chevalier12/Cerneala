# Cerneala Next Runtime Preview Plan Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to execute these plans in order. This file is an index, not a substitute for the detailed plan files. Each detailed plan has RED/GREEN steps, concrete files, and verification commands.

**Goal:** Move Cerneala from Authoring Preview to Runtime Preview: the framework should be trustworthy in a real MonoGame loop with explicit viewport/DPI semantics, adapter-owned rendering state, cached content resources, minimal platform service integration, runtime diagnostics, and an end-to-end gate proving draw/update purity still holds.

**Architecture Direction:** Keep the ROADMAPv2 architecture. Do not replace the retained tree, typed property system, scheduler, drawing command model, input routing, styling, resources, or authoring APIs. This batch hardens the adapter/runtime edge around the already working core. All platform-specific work stays under adapter/platform folders or behind `UI/Platform` seams.

---

## Recommended Next Direction

Runtime Preview is the highest-leverage next step because Authoring Preview proves that users can build a real retained UI tree, but the next failure mode is runtime trust: scaling, pointer coordinates, backend clipping/state, image/font loading lifetime, cursor/clipboard seams, and diagnostics visible in the Playground. These are the issues a developer hits immediately when moving from tests to an actual game/application window.

This is not a package split, markup push, animation expansion, or accessibility adapter push. Those remain deferred until Runtime Preview is stable.

## Execution Order

1. [ ] `2026-07-06-normalize-viewport-scale-pointer-and-render-coordinates.md`
   - Land first because every backend/runtime test needs one explicit coordinate contract.
2. [ ] `2026-07-06-harden-monogame-render-backend-state.md`
   - Land after coordinate contract so clipping, scissor rectangles, text/image commands, and render-state restoration follow the same scale rules.
3. [ ] `2026-07-06-cache-content-resources-and-textures-lifetime.md`
   - Land after backend hardening because image/text resource lifetime depends on adapter-owned texture behavior.
4. [ ] `2026-07-06-wire-platform-services-cursor-and-clipboard.md`
   - Land after input/render/resource runtime seams are stable; keep this minimal and optional.
5. [ ] `2026-07-06-runtime-diagnostics-and-playground-polish.md`
   - Land after runtime capabilities exist so the Playground can expose honest runtime diagnostics instead of sample-specific counters.
6. [ ] `2026-07-06-runtime-preview-completion-gate.md`
   - Final integration gate proving the runtime slice is coherent and still retained/game-loop-friendly.

## Throughput Rules

- [ ] Use subagents aggressively for independent inspection, RED test drafting, implementation patches, and verification.
- [ ] Keep dependency order strict: do not implement a later plan before the current one is GREEN.
- [ ] Keep patches small and local.
- [ ] Write RED tests first unless a step explicitly says otherwise.
- [ ] Run targeted tests after each task and `dotnet test Cerneala.slnx` after each plan.
- [ ] Prefer fixing the owning layer over adding compatibility shims.
- [ ] Re-index after code/project-file modifications according to `AGENTS.md`.
- [ ] Do not use shell search/navigation when RoslynIndexer is available, except for the exceptions allowed by `AGENTS.md`.

## Do Not Build During This Sequence

- [ ] Do not split packages/projects yet.
- [ ] Do not expand `UI/Markup` or source generation.
- [ ] Do not add XAML compatibility or string-path binding to the hot path.
- [ ] Do not implement full IME, multiline text editing, spellcheck, or rich text.
- [ ] Do not build native accessibility adapters.
- [ ] Do not expand animation/storyboard behavior.
- [ ] Do not add gradients, shadows, path rendering, render targets, or new drawing primitives.
- [ ] Do not add new control families.
- [ ] Do not replace retained invalidation with immediate-mode rebuilds.
- [ ] Do not introduce WPF compatibility behavior only because a name is familiar.

## Completion Gate

- [ ] `dotnet test Cerneala.slnx` passes.
- [ ] `dotnet test` passes.
- [ ] `UiViewport` has an explicit logical/physical coordinate contract.
- [ ] MonoGame pointer input maps physical pixels into the same coordinate space used by layout and hit testing.
- [ ] MonoGame rendering maps retained draw commands to the correct physical output scale without changing core drawing primitives.
- [ ] MonoGame clipping/scissor state is balanced and restored after draw.
- [ ] Path-backed image resources load through adapter-owned services, cache predictably, invalidate dependents, and dispose owned images when appropriate.
- [ ] Text texture/image cache lifetime is deterministic and test-covered.
- [ ] Cursor and clipboard platform services are optional, root/host-owned, and do not leak platform APIs into controls.
- [ ] Runtime diagnostics expose enough information to debug scale, retained work, input cache, render cache, and resource loads.
- [ ] Runtime Preview sample proves scale, image resource loading, TextBox input, command state, cursor, clipboard where available, retained no-work frames, and draw purity.

## Archive step

After all plans are GREEN and both full test commands pass, archive the repository with the existing script from the repository root. Do not invent a new archive script.

```powershell
cd C:\Users\Shadow\Desktop\Cerneala
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\scripts\Archive-Repo.ps1 -RepoRoot .
```
