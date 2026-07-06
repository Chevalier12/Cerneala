# Retained Semantics Tree Core Contract Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, RED test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Make the platform-neutral semantics tree a real retained-core contract, not just an on-demand full rebuild helper. Authoring Preview needs accessible roles/names/values for Button, TextBlock, TextBox, ItemsControl/ListBox, and selected list items without building native platform adapters yet.

**Architecture:** Keep `UI/Accessibility` platform-neutral. Do not build native accessibility platform adapters. Add semantics invalidation/caching at the retained UI layer so semantics changes are explicit, testable, and separate from render/layout when possible. WPF-like `AutomationPeer` names can remain internal vocabulary for now, but public direction should be `Semantics*`.

**Tech Stack:** C#/.NET 8, xUnit, existing `UI/Accessibility`, `UI/Elements`, `UI/Core`, `UI/Controls`, retained invalidation.

---

## File Structure

- Modify: `UI/Core/UiPropertyOptions.cs`
  - Add `AffectsSemantics` only if chosen as the cleanest property metadata path.
- Modify: `UI/Invalidation/InvalidationFlags.cs`
  - Add `Semantics` as a specialized invalidation flag if needed.
- Modify: `UI/Invalidation/DirtyPropagation.cs`
  - Map semantics invalidation without forcing layout/render.
- Modify: `UI/Elements/UIRoot.cs`
  - Own cached semantics tree/dirty version or a root-owned semantics provider cache.
- Modify: `UI/Accessibility/SemanticsProvider.cs`
  - Add cache-aware API such as `GetOrBuild(UIRoot root)` while keeping `Build(UIRoot root)` available for explicit full rebuild tests.
- Modify: `UI/Accessibility/SemanticsTree.cs`
- Modify: `UI/Accessibility/SemanticsNode.cs`
  - Add diagnostic/version identity only if tests need it.
- Modify: `UI/Accessibility/AccessibleName.cs`
  - Mark name changes as semantics-affecting.
- Modify: `UI/Accessibility/AutomationPeer.cs`
- Modify: `UI/Accessibility/ButtonAutomationPeer.cs`
- Modify: `UI/Accessibility/TextBoxAutomationPeer.cs`
- Modify: `UI/Accessibility/ItemsControlAutomationPeer.cs`
- Modify only if needed: `UI/Controls/TextBlock.cs`, `UI/Controls/TextBoxBase.cs`, `UI/Controls/ListBoxItem.cs`, `UI/Controls/Primitives/ButtonBase.cs`
- Create: `tests/Cerneala.Tests/UI/Accessibility/RetainedSemanticsCacheTests.cs`
- Create: `tests/Cerneala.Tests/UI/Accessibility/AuthoringSemanticsContractTests.cs`

## Important Existing Behavior

- `SemanticsProvider.Build(UIRoot root)` walks the visual tree and creates a semantics tree every call.
- `AutomationPeer.Create(...)` maps common controls to roles.
- `AccessibleName.NameProperty` exists but has `UiPropertyOptions.None`.
- `ButtonAutomationPeer` derives name from explicit accessible name or content.
- `TextBoxAutomationPeer` and `ItemsControlAutomationPeer` already exist.
- Platform adapters are explicitly later/frozen.

Target behavior:

- Semantics tree can be retrieved from a root-owned cache.
- Unchanged semantics queries return cached data without rebuilding.
- Accessible name changes invalidate semantics without forcing measure/render.
- Text/value/selection/enabled/focus changes update semantics through explicit invalidation.
- Hidden/collapsed/non-rendering elements are excluded consistently.
- Semantics exposes useful roles/properties for Button, TextBlock, TextBox, ItemsControl/ListBox, and selected list items.

## Rules

- [ ] Do not build native accessibility adapters.
- [ ] Do not expose WPF `AutomationPeer` as the primary public API in new docs/tests.
- [ ] Do not make semantics rebuild every frame.
- [ ] Do not force layout/render for accessible name-only changes.
- [ ] Do not add reflection-based content/name lookup.
- [ ] Do not promise full screen-reader behavior.

---

### Task 1: Add RED Retained Semantics Tests

**Files:**
- Create: `tests/Cerneala.Tests/UI/Accessibility/RetainedSemanticsCacheTests.cs`
- Create: `tests/Cerneala.Tests/UI/Accessibility/AuthoringSemanticsContractTests.cs`

- [ ] **Step 1: Add cache/invalidation tests**

Create tests:

```csharp
SemanticsProviderCachesUnchangedRootSemantics()
AccessibleNameChangeInvalidatesSemanticsWithoutLayoutOrRender()
TreeMutationInvalidatesSemanticsCache()
VisibilityCollapsedElementIsExcludedFromSemantics()
UnchangedSecondSemanticsQueryDoesNotRebuildTree()
```

Use a test provider counter or diagnostic hook if necessary to assert rebuild counts.

- [ ] **Step 2: Add authoring semantics tests**

Create tests:

```csharp
ButtonSemanticsIncludesRoleNameEnabledAndFocusState()
TextBlockSemanticsUsesTextAsName()
TextBoxSemanticsIncludesEditableTextRoleAndValue()
ListBoxSemanticsIncludesItemCountAndSelectedItemState()
ObservableListMutationInvalidatesListSemantics()
```

- [ ] **Step 3: Add retained frame interaction tests**

Create tests:

```csharp
CommandStateRefreshUpdatesButtonEnabledSemanticsAfterFrame()
TextBoxTextInputUpdatesSemanticsValueAfterFrame()
SelectionChangeUpdatesListItemSemanticsWithoutFullLayoutWhenPossible()
```

- [ ] **Step 4: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RetainedSemanticsCacheTests|FullyQualifiedName~AuthoringSemanticsContractTests"
```

Expected: RED because semantics caching/invalidation is not retained-core yet.

- [ ] **Step 5: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\UI\Accessibility\RetainedSemanticsCacheTests.cs tests\Cerneala.Tests\UI\Accessibility\AuthoringSemanticsContractTests.cs
git commit -m "test: capture retained semantics tree contract"
```

---

### Task 2: Add Semantics Invalidation Metadata

**Files:**
- Modify: `UI/Core/UiPropertyOptions.cs`
- Modify: `UI/Invalidation/InvalidationFlags.cs`
- Modify: `UI/Invalidation/DirtyPropagation.cs`
- Modify: `UI/Core/UiObject.cs`
- Modify: `UI/Elements/UIRoot.cs`

- [ ] **Step 1: Add semantics option/flag**

Add:

```text
UiPropertyOptions.AffectsSemantics
InvalidationFlags.Semantics
```

Only if tests cannot be satisfied cleanly through root tree version and explicit calls.

- [ ] **Step 2: Map property changes to semantics invalidation**

Update `UiObject` invalidation option mask and `UIElement.MapInvalidationOptions(...)`.

- [ ] **Step 3: Do not queue layout/render for semantics-only changes**

`DirtyPropagation.GetEffectiveFlags(...)` should not turn `Semantics` into layout/render/hit-test.

- [ ] **Step 4: Mark root semantics cache dirty**

When root receives semantics invalidation or tree version changes, mark the root semantics cache dirty.

---

### Task 3: Root-Owned Semantics Cache

**Files:**
- Modify: `UI/Elements/UIRoot.cs`
- Modify: `UI/Accessibility/SemanticsProvider.cs`
- Modify: `UI/Accessibility/SemanticsTree.cs`

- [ ] **Step 1: Add cache state**

Store:

```text
SemanticsTree? cachedSemanticsTree
int cachedTreeVersion
int semanticsVersion or dirty bool
```

- [ ] **Step 2: Add retrieval API**

Add one clear API. Example:

```csharp
public SemanticsTree GetSemanticsTree()
```

on `UIRoot`, or:

```csharp
SemanticsProvider.GetOrBuild(UIRoot root)
```

Prefer root-owned cache because invalidation already belongs to root.

- [ ] **Step 3: Keep explicit full build available**

`SemanticsProvider.Build(root)` may still force a new tree for diagnostics/tests. Cached API should be separate and named clearly.

- [ ] **Step 4: Add diagnostics only if cheap**

If `FrameDiagnostics` already has a pattern for counters, add semantics rebuild count. Do not overbuild devtools here.

---

### Task 4: Mark Semantics-Affecting Properties

**Files:**
- Modify: `UI/Accessibility/AccessibleName.cs`
- Modify: `UI/Elements/UIElement.cs`
- Modify: `UI/Controls/ContentControl.cs`
- Modify: `UI/Controls/TextBlock.cs`
- Modify: `UI/Controls/TextBoxBase.cs`
- Modify: `UI/Controls/ListBoxItem.cs`
- Modify only if needed: `UI/Controls/Primitives/ButtonBase.cs`

- [ ] **Step 1: Accessible name**

`AccessibleName.NameProperty` should affect semantics only.

- [ ] **Step 2: UI state**

Add semantics effect to properties that semantics exposes:

- `IsEnabled`
- `IsKeyboardFocused`
- `Visibility`
- `IsVisible` if still public and relevant

- [ ] **Step 3: Content/text/value**

Add semantics effect to:

- `ContentControl.ContentProperty`
- `TextBlock.TextProperty`
- `TextBoxBase.TextProperty`
- selected item/container state properties.

Be careful not to change existing layout/render effects.

---

### Task 5: Improve Authoring Semantics Output

**Files:**
- Modify: `UI/Accessibility/AutomationPeer.cs`
- Modify: `UI/Accessibility/ButtonAutomationPeer.cs`
- Modify: `UI/Accessibility/TextBoxAutomationPeer.cs`
- Modify: `UI/Accessibility/ItemsControlAutomationPeer.cs`
- Modify: `UI/Accessibility/SemanticsProperty.cs`

- [ ] **Step 1: TextBlock role/name**

Ensure `TextBlock` role is `Text` and name is its `Text` unless explicit accessible name overrides it.

- [ ] **Step 2: TextBox value**

Expose `SemanticsRole.EditableText` and `SemanticsProperty.Value` for `TextBoxBase.Text`.

- [ ] **Step 3: List/list item state**

Expose item count on list controls and selected state on realized selectable item containers.

Do not create semantic nodes for unrealized virtualized items in this plan.

- [ ] **Step 4: Button state**

Expose enabled/focused properties consistently through base peer.

---

### Task 6: Verify GREEN And Regressions

- [ ] **Step 1: Run targeted semantics tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RetainedSemanticsCacheTests|FullyQualifiedName~AuthoringSemanticsContractTests|FullyQualifiedName~SemanticsProviderTests|FullyQualifiedName~ButtonSemanticsTests|FullyQualifiedName~TextBoxSemanticsTests"
```

Expected: GREEN.

- [ ] **Step 2: Run retained authoring-adjacent tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~CommandStateSchedulerTests|FullyQualifiedName~TextBoxTwoWayBindingTests|FullyQualifiedName~ItemsSourceObservableTests|FullyQualifiedName~CorePreviewContractTests"
```

Expected: GREEN.

- [ ] **Step 3: Run full suite**

```powershell
dotnet test Cerneala.slnx
```

Expected: GREEN.

- [ ] **Step 4: Commit implementation**

```powershell
git add UI\Accessibility UI\Core UI\Invalidation UI\Elements UI\Controls tests\Cerneala.Tests
git commit -m "feat: add retained semantics tree core contract"
```
