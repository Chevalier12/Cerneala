# Plan: Prism Studio in CernealaPresentation

> Date: 2026-07-22
> Status: completed
> Goal: Add after Motion a Prism chapter with a full Photoshop-like editor, powered by the public Prism catalog and verified end-to-end.

## Decisions and non-objectives

- The gallery has four local targets: mascot, typographic poster, geometric badge and UI card.
- The same stack moves between targets and starts from a deterministic preset of two layers.
- The layers contain separate lists of filters and styles; their intercalation is not simulated.
- The catalog displays all 134 filters and 10 styles; operations with mandatory resources remain visible, but blocked.
- The stack has no artificial limit; diagnostics shows the cost and fallbacks.
- Persistence, import of resources or change of Prism semantics are not implemented.

## Implementation stages

### Stage 0 - Public catalog and standardized access

- [x] Extends the catalog and generator with immutable public metadata for operation, parameter, type, default, unit, numeric range, symbol options and resource dependency.
- [x] Expose `PrismCatalog`, `PrismCatalogOperationInfo`, `PrismCatalogParameterInfo`, `PrismCatalogOperationKind` and `PrismCatalogValueKind` without duplicating the JSON in Presentation.
- [x] Add `GetValue<T>` and `SetValue<T>` to `PrismFilterState` and `PrismStyleState`, with validation for operation, descriptor, type and symbol.
- [x] Keep the generated helpers and the existing runtime behavior compatible.
- [x] Add SourceGen and runtime tests for 134 filters, 10 styles, all types, metadata, round-trip, versioning and expired states after `ReplaceDefinition`.
- [x] Document all new public APIs in `docs-site/documentation/classes/` and synchronize the manifest.

**Gate Stage 0**

- [x] The targeted Prism/SourceGen build and tests are green, and the public API does not ask for slots or magic strings from the consumer.

### Step 1 - Prism Studio Editor Model

- [x] Adds the Presentation model for layers, filters, styles, selection, typed values and the initial preset.
- [x] Build the Prism definition in the declared order, reapply the values ​​after structural changes and preserve the stack when changing the target.
- [x] Implements add/remove/reorder/visibility/reset without stack limit and blocks operations with mandatory resources.
- [x] Adds tests for order of filters/styles, reset, selection, unlimited stack, blocked operations and preservation of values.

**Gate stage 1**

- [x] The model produces valid compositions and all its tests are green without UI or GPU dependency.

### Stage 2 - The interactive view and the previews

- [x] Add `PrismChapterView` with preview, four-target gallery, layers panel and inspector/catalog responsive to 1320x860 and 1080x720.
- [x] Fully renders the four samples locally, including the mascot asset, for the correct Prism capture.
- [x] Connect the layer/filter/style, search, categories, tabs and reset actions to the model and `PrismInstance.ReplaceDefinition`.
- [x] Generate editors for number, integer, boolean, color, vector, symbol and read-only resource status.
- [x] Displays all operations, the `RESOURCE REQUIRED` mark and live diagnostics for passes, surfaces, bytes and fallback.
- [x] Explicitly manages attachment, detachment, target change and preview resources.

**Gate stage 2**

- [x] Interactions immediately change the preview, the layout does not overlap, and the view does not leave attachments or resources after detaching.

### Stage 3 - Integration into the tour and automation

- [x] Insert `PRISM` after `MOTION`, renumber Pipeline/Diagnostics and update pages, toggles, handlers and counter to 8 chapters.
- [x] Replaces magic indexes in frame callbacks and automation with stable semantic identification.
- [x] Extends the automation and smoke frame-budget for the new Prism chapter and diagnostics.
- [x] Add lifecycle regression for repeated navigations to/from Prism.

**Gate stage 3**

- [x] Full tour, direct capture of chapter 06 and repeated automation runs without leak, timeout or wrong chapter.

### Stage 4 - Final check

- [x] Dr. RoslynIndexer is also running reindexing.
- [x] Run the SourceGen tests, the Cerneala and `dotnet test .\Cerneala.slnx` tests in the final state.
- [x] Runs Presentation automation and a frame-budget smoke cycle.
- [x] Capture and visually inspect Prism at default and minimum size, including non-blank preview, text, clipping and diagnostics.
- [x] Run the documentation/API check, `git diff --check` and the final audit of the diff.

**Gate Stage 4**

- [x] All checks are green and there is no temporary code, accidentally generated churn or known visual regressions.

## The definition of ready

- [x] The Prism Studio chapter is fully functional after Motion, with all the filters/styles discovered from the catalog, typed editor, four targets and live diagnostics.
- [x] The public API, documentation, lifecycle, automation, smoke performance and the full suite are verified.
- [x] All stages and gates are checked, and the status of the plan is `finalizat`.