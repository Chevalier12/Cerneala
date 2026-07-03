## Context

The retained UI stack now has typed properties, retained tree ownership, layout, rendering caches, host integration, input bridge, command routing, controls, text services, and resources. `Game1` already creates a `MonoGameUiHost`, calls update/draw, and queues text input, but its visual content is a local `PlaygroundDemoElement` that bypasses the retained controls being built.

This change turns the playground into an MVP proof scene: a retained tree containing `StackPanel`, `TextBlock`, `Button`, and `Border`, selectable samples, and a diagnostics overlay that makes retained no-op frames visible.

## Goals / Non-Goals

**Goals:**
- Add retained sample classes under `Playground/Cerneala.Playground/Samples`.
- Wire a `SampleSelector` into `Game1` through `MonoGameUiHost`.
- Show button hover/click behavior, layout, text rendering, and retained render-cache reuse.
- Provide an `InvalidationStatsOverlay` backed by `UiFrame`/frame stats so no-op frames can be observed.
- Update `ROADMAPv2.md` section 13.

**Non-Goals:**
- Do not build a full playground framework or editor UI.
- Do not add runtime asset dependencies beyond what the existing MonoGame host can supply.
- Do not change core retained invalidation semantics only to make a demo easier.
- Do not require markup or styling systems before those roadmap phases exist.

## Decisions

### Build samples as retained tree factories

Each sample creates or updates a retained `UIRoot` subtree using existing controls and services. The selector swaps the active sample by replacing retained children rather than drawing immediate commands.

Rationale: this proves the real retained API surface and avoids another custom demo element that hides integration gaps.

Alternative considered: keep one custom `UIElement` sample. Rejected because it does not prove controls, input, command routing, or layout.

### Keep sample selector simple and explicit

`SampleSelector` owns a small list of sample descriptors and exposes next/previous or selected-index behavior. It can render selector controls as retained buttons/text and rebuild the active sample subtree when selection changes.

Rationale: the MVP needs selectable samples, not a full navigation framework.

Alternative considered: keyboard-only switching. Rejected because roadmap acceptance specifically includes mouse hover and click behavior.

### Use host frame stats for overlay diagnostics

`InvalidationStatsOverlay` reads the last `UiFrame` from the host and displays measure, arrange, render-cache, and draw/cache counters available from retained frame stats.

Rationale: unchanged frame behavior must be visible in the playground and testable without instrumenting MonoGame internals.

Alternative considered: print diagnostics to console. Rejected because it does not prove in-window retained UI composition.

### Game1 remains a thin MonoGame shell

`Game1` should only create graphics services, host, root, selector, and forward update/draw/text input. Sample construction belongs under `Samples`.

Rationale: keeping `Game1` small prevents playground code from becoming another architecture pile of spaghetti.

Alternative considered: build all sample UI directly in `Game1`. Rejected because it becomes harder to test and maintain.

## Risks / Trade-offs

- [Risk] Frame stats may not expose every counter needed by the overlay. -> Mitigation: use existing stats first; add narrow diagnostics only if required by tests.
- [Risk] MonoGame visual verification is hard in unit tests. -> Mitigation: test sample tree construction and host calls with fakes where possible; rely on build and boundary tests for MonoGame-specific glue.
- [Risk] Playground samples could leak into core APIs. -> Mitigation: keep all sample classes under the playground project and avoid changes to core unless a real bug is exposed.

## Migration Plan

1. Add sample interfaces/descriptors and retained sample classes.
2. Add `SampleSelector` and `InvalidationStatsOverlay`.
3. Replace `PlaygroundDemoElement` wiring in `Game1` with retained sample selector wiring.
4. Add focused playground tests where practical.
5. Update `ROADMAPv2.md` section 13 and run full verification.
