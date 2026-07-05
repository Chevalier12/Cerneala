# Cerneala Audit Fix Plan

This plan turns `ROADMAPv2_AUDIT.md` into an ordered execution checklist. Do not use it to add new features. Use it to harden the retained UI foundation.

## Step 0: Cleanup Commit

- [ ] Commit the current cleanup work:
  - [ ] OpenSpec removal from the repo.
  - [ ] `ROADMAPv2_AUDIT.md`.
  - [ ] OpenSpec references removed from roadmap/audit.
- [ ] Restart/reload Codex so the Superpowers plugin is picked up.
- [ ] Work from `ROADMAPv2_AUDIT.md`, `ROADMAPv2.md`, `architecture.md`, and `docs/architecture-v2.md`.
- [ ] Do not add new controls/media/markup/accessibility/animation features until the Must Fix phases are closed.

## Phase 1: Retained Update/Draw Contract

Goal: `Update` does retained work. `Draw` only submits previously committed cached output.

### Plan 1: `fix-retained-render-frame-contract`

Detailed plan: `docs/superpowers/plans/2026-07-03-fix-retained-render-frame-contract.md`

- [x] Make `RenderQueueProcessor` the only production path that can rebuild local element render caches.
- [x] Make `DrawCommandListBuilder` compose only already-valid local caches.
- [x] Remove the `ElementRenderCache.Ensure(...)` backdoor from root composition.
- [x] Make root command-list composition an explicit update commit step or a counted frame phase.
- [x] Make `UiHost.Draw(...)` submit only the last committed root command list.
- [x] Remove per-draw command-list copying or replace it with a clear read-only backend contract.
- [x] Add `tests/Cerneala.Tests/UI/Rendering/RenderBackdoorContractTests.cs`.
- [x] Add `tests/Cerneala.Tests/UI/Rendering/RetainedRendererDrawPurityTests.cs`.
- [x] Add `tests/Cerneala.Tests/UI/Hosting/UiHostFrameStatsIntegrityTests.cs`.

### Plan 2: `fix-tree-mutation-invalidation`

Detailed plan: `docs/superpowers/plans/2026-07-03-fix-tree-mutation-invalidation.md`

- [x] Visual child add invalidates measure, arrange, render, and hit-test work.
- [x] Visual child remove invalidates measure, arrange, render, and hit-test work.
- [x] `UIRoot` is not skipped for visual child mutation invalidation.
- [x] Tree version increments remain bookkeeping, not a substitute for dirty work.
- [x] Add shared helper for visual child mutation invalidation.
- [x] Add `tests/Cerneala.Tests/UI/Elements/UIElementCollectionInvalidationTests.cs`.
- [x] Add `tests/Cerneala.Tests/UI/Hosting/UiHostLateTreeMutationTests.cs`.

### Phase 1 Gate

- [x] `dotnet test Cerneala.slnx` passes.
- [x] No-work-frame tests still pass.
- [x] Draw path cannot call element render hooks.
- [x] Late tree mutation is processed during update, not lazily during draw.
- [x] Commit Phase 1.

## Phase 2: Scheduler Ownership

Goal: retained systems are owned by the frame scheduler/root, not side services or per-frame rebuilds.

### Plan 3: `integrate-style-phase`

Detailed plan: `docs/superpowers/plans/2026-07-03-integrate-style-phase.md`

- [x] Map `UiPropertyOptions.AffectsStyle` to `InvalidationFlags.Style`, not `Render`.
- [x] Add scheduler-owned style processing before measure/arrange/render.
- [x] Decide whether style work uses `StyleQueue` or a typed style processor over dirty elements.
- [x] Make `UIRoot` or `UiHost` own style/theme scope for an attached tree.
- [x] Remove string property-name pseudo-class detection.
- [x] Add explicit pseudo-class registration or provider contract.
- [x] Add `tests/Cerneala.Tests/UI/Styling/StyleSchedulerIntegrationTests.cs`.

### Plan 4: `cache-input-route-hit-test`

Detailed plan: `docs/superpowers/plans/2026-07-04-cache-input-route-hit-test.md`

- [x] Add root-owned retained input route / hit-test cache.
- [x] Rebuild route/hit-test data only when dirty.
- [x] Invalidate cache for tree changes, layout bounds changes, visibility, enabled state, handlers, and relevant capture changes.
- [x] Make `ElementInputBridge.Dispatch(...)` consume retained cache instead of rebuilding route maps every frame.
- [x] Move button command execution out of `ElementInputBridge`.
- [x] Move thumb drag behavior behind handlers or an input-level interface.
- [x] Add `tests/Cerneala.Tests/UI/Input/ElementInputCacheInvalidationTests.cs`.
- [x] Add `tests/Cerneala.Tests/UI/Input/HitTestCacheInvalidationTests.cs`.
- [x] Add `tests/Cerneala.Tests/UI/Input/InputControlBoundaryTests.cs`.

### Phase 2 Gate

- [x] `dotnet test Cerneala.slnx` passes.
- [x] Style invalidation is scheduler-owned.
- [x] Input route data is retained and invalidation-driven.
- [x] `UI/Input` no longer depends directly on `UI/Controls`.
- [x] Commit Phase 2.

## Phase 3: Roadmap Honesty And Deferred Scope

Goal: stop the roadmap from claiming maturity for descriptor-level or experimental work.

### Plan 5: `freeze-later-experimental-scope`

Detailed plan: `docs/superpowers/plans/2026-07-05-freeze-later-experimental-scope.md`

- [x] Mark advanced media/rendering as experimental/frozen until drawing command/backend semantics exist.
- [x] Mark advanced input categories as experimental/frozen until platform behavior exists.
- [x] Mark markup/source generation as optional/frozen until retained core contracts are stable.
- [x] Mark accessibility platform adapters as later; keep semantic tree architecture.
- [x] Mark animation/storyboard expansion as later until scheduler/render invalidation is proven under animation stress.
- [x] Update `ROADMAPv2.md` without deleting useful history.

### Plan 6: `clarify-text-services-mvp`

Detailed plan: `docs/superpowers/plans/2026-07-05-clarify-text-services-mvp.md`

- [x] Mark current line breaking as deterministic MVP approximation.
- [x] Do not claim production wrapping/trimming/multiline rendering until measurement and rendering align.
- [x] Make controls with text content use shared text services or a content presenter path.
- [x] Add `tests/Cerneala.Tests/UI/Text/TextRendererWrapContractTests.cs`.
- [x] Add `tests/Cerneala.Tests/Controls/ButtonContentArchitectureTests.cs`.

### Plan 7: `clarify-package-boundary-dependencies`

Detailed plan: `docs/superpowers/plans/2026-07-05-clarify-package-boundary-dependencies.md`

- [x] Keep `Cerneala.Core.csproj`, `Cerneala.MonoGame.csproj`, `Cerneala.Tests.Core.csproj`, and `Cerneala.Tests.MonoGame.csproj` deferred.
- [x] Record that `Cerneala.csproj` currently carries MonoGame/Skia/HarfBuzz dependencies.
- [x] Define future acceptance criteria for splitting core/adapters.
- [x] Add package-shape tests only when the split criteria are ready.

### Phase 3 Gate

- [ ] `ROADMAPv2.md` no longer overclaims Later/Optional maturity.
- [ ] Deferred work is explicitly marked as deferred, experimental, or frozen.
- [ ] `dotnet test Cerneala.slnx` passes.
- [ ] Commit Phase 3.

## Execution Order

1. [ ] Commit current cleanup.
2. [ ] Restart/reload Codex for Superpowers.
3. [x] Execute `fix-retained-render-frame-contract`.
4. [x] Execute `fix-tree-mutation-invalidation`.
5. [x] Run full tests and commit Phase 1.
6. [x] Execute `integrate-style-phase`.
7. [x] Execute `cache-input-route-hit-test`.
8. [x] Run full tests and commit Phase 2.
9. [x] Execute roadmap honesty pass: `freeze-later-experimental-scope`.
10. [x] Execute `clarify-text-services-mvp`.
11. [x] Execute `clarify-package-boundary-dependencies`.
12. [ ] Run full tests and commit Phase 3.

## Stop Conditions

- [ ] Stop adding features until Phase 1 and Phase 2 are complete.
- [ ] Stop if a fix requires changing public architecture assumptions; update this plan first.
- [ ] Stop if tests reveal the audit finding is wrong; document why in `ROADMAPv2_AUDIT.md` or this plan.
- [ ] Stop if a plan starts becoming a rewrite instead of a scoped hardening pass.
