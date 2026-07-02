## 1. OpenSpec Project Memory

- [x] 1.1 Add `openspec/README.md`; done when it explains how Cerneala uses OpenSpec, where active changes live, and how future sessions should resume work.
- [x] 1.2 Add `openspec/project.md`; done when it records v2 product principles, scope bands, non-goals, and confirmed MVP decisions.

## 2. V2 Architecture Documentation

- [x] 2.1 Add `docs/architecture-v2.md`; done when it explains the architecture above `UI/Drawing` and `UI/Input`, including typed state, logical/visual trees, invalidation, layout, render cache, input routing, styling metadata, and MVP boundaries.
- [x] 2.2 Add `docs/diagrams/retained-frame-loop.md`; done when it diagrams the retained frame loop from state changes to dirty queues, layout, render-cache updates, and backend rendering.
- [x] 2.3 Add `docs/diagrams/ui-layer-boundaries.md`; done when it diagrams boundaries between UI core, Drawing, Input, and MonoGame adapters.

## 3. Roadmap Alignment

- [x] 3.1 Verify `ROADMAPv2.md` references the same confirmed decisions as the OpenSpec design; done when tree model, input routing, render cache, invalidation, styling, package, and testing decisions match.
- [x] 3.2 Keep runtime implementation out of this change; done when no new runtime files under `UI/Core`, `UI/Elements`, `UI/Layout`, `UI/Rendering`, `UI/Controls`, or `UI/Hosting` are required by this change.

## 4. Validation

- [x] 4.1 Run `openspec validate add-retained-ui-mvp-foundation`; done when the change validates successfully.
- [x] 4.2 Run `openspec status --change add-retained-ui-mvp-foundation`; done when proposal, design, specs, and tasks are complete.
- [x] 4.3 Review `git status --short`; done when changed files are understood and no unrelated edits were made.
