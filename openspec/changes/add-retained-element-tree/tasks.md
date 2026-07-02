## 1. Roadmap And Contract Alignment

- [x] 1.1 Update `ROADMAPv2.md` section 3; done when the stale single-tree wording is replaced with the confirmed separate logical and visual tree decision.
- [x] 1.2 Add section 3 planning checklist entries in `ROADMAPv2.md`; done when proposal, design, specs, tasks, and validation entries for `add-retained-element-tree` are visible and accurately checked.

## 2. Core Element Types

- [x] 2.1 Add `UI/Elements/UIElement.cs`; done when it derives from `UiObject`, exposes logical/visual parentage, child collections, root attachment state, enabled/visibility state needed by routing policy, and lifecycle hooks.
- [x] 2.2 Add `UI/Elements/UIElementCollection.cs`; done when it manages owned logical or visual children, assigns the matching parent, rejects duplicate adds, and rejects reparenting without removal.
- [x] 2.3 Add `UI/Elements/UIRoot.cs`; done when it owns root attachment, viewport/scaling placeholders, tree versioning, element id ownership, and input route ownership.
- [x] 2.4 Add `UI/Elements/ElementTreeChange.cs`; done when tree mutations can report parent, child, relationship kind, and change kind.

## 3. Lifecycle, Identity, And Traversal

- [x] 3.1 Add `UI/Elements/ElementLifecycle.cs`; done when attach/detach helpers run deterministic tree-order lifecycle behavior and update root/tree version state.
- [x] 3.2 Add `UI/Elements/ElementIdProvider.cs`; done when attached elements receive stable `UiElementId` values and detached elements are removed from active route ownership.
- [x] 3.3 Add `UI/Elements/ElementTreeWalker.cs`; done when pre-order, post-order, ancestor, and descendant traversal helpers are deterministic.
- [x] 3.4 Add `UI/Elements/IElementChildHost.cs`; done when controls that own generated children have an explicit future-facing contract without implementing controls in this change.
- [x] 3.5 Add `UI/Elements/IElementHost.cs`; done when `UIRoot` and future platform hosts can expose the retained root contract.

## 4. Routed Event Handler Storage And Input Route Bridge

- [x] 4.1 Add `UI/Elements/ElementHandlerStore.cs`; done when retained elements can store routed event handlers by `RoutedEvent`.
- [x] 4.2 Keep `UI/Input/UiInputTree.cs` as a low-level route table only; done when this change does not make it the permanent retained tree source.
- [x] 4.3 Add `UI/Input/ElementInputRouteMap.cs`; done when attached retained elements can map to `UiElementId` and ids can resolve back to elements.
- [x] 4.4 Add `UI/Input/ElementInputRouteBuilder.cs`; done when it can build or update a low-level route table from the retained visual tree and handler store.

## 5. Tests

- [x] 5.1 Add `tests/Cerneala.Tests/UI/Elements/UIElementTreeTests.cs`; done when logical/visual parentage, reparent rejection, and separate relationship behavior are covered.
- [x] 5.2 Add `tests/Cerneala.Tests/UI/Elements/UIElementCollectionTests.cs`; done when duplicate add, removal, parent clearing, and collection change behavior are covered.
- [x] 5.3 Add `tests/Cerneala.Tests/UI/Elements/UIRootTests.cs`; done when root attachment, tree versioning, and stable ids while attached are covered.
- [x] 5.4 Add `tests/Cerneala.Tests/UI/Elements/ElementLifecycleTests.cs`; done when attach/detach order and subtree behavior are covered.
- [x] 5.5 Add `tests/Cerneala.Tests/UI/Elements/ElementTreeWalkerTests.cs`; done when pre-order, post-order, ancestor, and descendant traversal are covered.
- [x] 5.6 Add `tests/Cerneala.Tests/UI/Elements/ElementHandlerStoreTests.cs`; done when routed event handler registration and retrieval are covered.
- [x] 5.7 Add `tests/Cerneala.Tests/Input/ElementInputRouteBuilderTests.cs`; done when retained visual ancestry builds the expected route order and disabled/invisible policy can exclude elements.

## 6. Validation

- [x] 6.1 Run `dotnet test`; done when the full test suite passes.
- [x] 6.2 Run `openspec validate add-retained-element-tree --strict`; done when the change validates successfully.
- [x] 6.3 Run `openspec validate --all --strict`; done when active changes and main specs validate successfully.
- [x] 6.4 Review `git status --short`; done when changed files are understood and no unrelated edits were made.
