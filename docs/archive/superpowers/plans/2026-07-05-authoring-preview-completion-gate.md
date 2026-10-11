# Authoring Preview Completion Gate Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Prove Cerneala has a coherent Authoring Preview after the preceding plans. This is not a new architecture phase. It is a single vertical slice and test gate that proves command state, typed binding, TextBox editing, templated theme controls, observable lists, semantics, retained invalidation, and draw purity work together.

**Architecture:** Reuse existing `UiHost`, `UIRoot`, retained scheduler, default theme/style system, `ObservableValue<T>`, `ObservableList<T>`, `TextBox`, `Button`, `ListBox`, `ScrollViewer`, and semantics APIs. Do not add new framework features to make this sample pass; fix owning-layer bugs exposed by integration tests.

**Tech Stack:** C#/.NET 8, xUnit, Playground sample, retained UI host, fake drawing backend/tests.

---

## File Structure

- Create: `Playground/Cerneala.Playground/Samples/AuthoringAppSample.cs`
  - Sample screen using typed data, two-way text, command state, templated button, observable list, and semantics-friendly controls.
- Modify: `Playground/Cerneala.Playground/Game1.cs`
  - Add sample registration only if sample list exists there.
- Create: `tests/Cerneala.Tests/Playground/Samples/AuthoringAppSampleContractTests.cs`
- Create: `tests/Cerneala.Tests/UI/Hosting/AuthoringPreviewContractTests.cs`
- Modify: `tests/Cerneala.Tests/UI/Hosting/CorePreviewContractTests.cs`
  - Only if shared helpers should be extracted to avoid duplication.
- Modify: `ROADMAPv2.md`
  - Mark only the precise contracts proven by this sequence, if project convention expects roadmap updates.

## Important Existing Behavior

- `RetainedAppSample` proves Core Preview rendering/input/list behavior.
- `DefaultTheme` and style scheduling already exist.
- Previous plans in this index should have added retained command state, typed UI property bindings, TextBox caret/selection, templated default button, observable items source, and retained semantics cache.

Target behavior:

- First authoring frame performs style/layout/render/hit-test work.
- Second unchanged frame performs no retained work.
- `Draw(...)` never creates retained work.
- TextBox two-way binding updates model and dependent text.
- Command state enables/disables button based on typed model state.
- Default themed button is templated and responds to hover/pressed/focus/disabled.
- Observable list mutation updates list UI without unrelated container churn.
- Semantics tree reports names/roles/values for controls in the sample.

## Rules

- [ ] Do not create new controls just for the sample.
- [ ] Do not hard-code local colors when default theme/style should provide them.
- [ ] Do not bypass retained update/draw contracts.
- [ ] Do not make the sample depend on native platform services.
- [ ] Do not add markup or source generation.
- [ ] Do not weaken existing Core Preview tests.

---

### Task 1: Add RED Authoring Sample Contract Tests

**Files:**
- Create: `tests/Cerneala.Tests/Playground/Samples/AuthoringAppSampleContractTests.cs`
- Create: `tests/Cerneala.Tests/UI/Hosting/AuthoringPreviewContractTests.cs`

- [ ] **Step 1: Add sample structure tests**

Create tests:

```csharp
AuthoringAppSampleBuildsTextBoxButtonStatusAndList()
AuthoringAppSampleUsesDefaultThemeTemplateForPrimaryButton()
AuthoringAppSampleUsesObservableListAsItemsSource()
AuthoringAppSampleUsesTypedBindingForTextEntry()
```

These tests can use public sample properties to avoid brittle tree searching. Add properties to the sample if needed:

```text
NameTextBox
SubmitButton
StatusText
Items
ListBox
RootElement
```

- [ ] **Step 2: Add retained frame tests**

Create tests:

```csharp
AuthoringPreviewFirstFrameDoesRetainedWork()
AuthoringPreviewSecondUnchangedFrameDoesNoRetainedWork()
AuthoringPreviewDrawDoesNotGenerateRetainedWork()
AuthoringPreviewViewportResizeReflowsWithoutBreakingBindings()
```

- [ ] **Step 3: Add interaction tests**

Create tests:

```csharp
AuthoringPreviewTextInputUpdatesObservableModelAndStatusText()
AuthoringPreviewCommandDisabledWhenInputIsEmpty()
AuthoringPreviewCommandEnabledWhenInputHasText()
AuthoringPreviewButtonMouseAndKeyboardActivationAddItems()
AuthoringPreviewObservableListMutationUpdatesListAndNextFrameDoesNoWork()
```

- [ ] **Step 4: Add semantics tests**

Create tests:

```csharp
AuthoringPreviewSemanticsIncludesTextBoxButtonAndList()
AuthoringPreviewSemanticsUpdatesTextBoxValueAfterTextInput()
AuthoringPreviewSemanticsUpdatesButtonEnabledAfterCommandStateRefresh()
AuthoringPreviewSemanticsUpdatesListItemCountAfterCommandAddsItem()
```

- [ ] **Step 5: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~AuthoringAppSampleContractTests|FullyQualifiedName~AuthoringPreviewContractTests"
```

Expected: RED because `AuthoringAppSample` does not exist yet and integration contracts are not wired into one sample.

- [ ] **Step 6: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\Playground\Samples\AuthoringAppSampleContractTests.cs tests\Cerneala.Tests\UI\Hosting\AuthoringPreviewContractTests.cs
git commit -m "test: add authoring preview completion gate"
```

---

### Task 2: Create `AuthoringAppSample`

**Files:**
- Create: `Playground/Cerneala.Playground/Samples/AuthoringAppSample.cs`
- Modify: `Playground/Cerneala.Playground/Game1.cs` only if needed.

- [ ] **Step 1: Define sample state**

Use explicit typed state:

```text
ObservableValue<string> Name
ObservableValue<string> Status
ObservableList<string> Items
ActionCommand SubmitCommand
```

Command can-execute should be true only when trimmed name text is non-empty.

- [ ] **Step 2: Build UI with existing controls**

Use only existing controls:

- `StackPanel`
- `Border`
- `TextBlock`
- `TextBox`
- `Button`
- `ListBox`
- `ScrollViewer`

- [ ] **Step 3: Use typed bindings**

Bind:

- `TextBox.TextProperty` two-way to `Name`.
- `TextBlock.TextProperty` one-way to `Status`.
- optional derived text via explicit source update, not converter complexity unless already available.

- [ ] **Step 4: Use command state**

`SubmitCommand` should:

- add the current name to `Items`;
- update `Status`;
- clear `Name` or leave it, whichever tests specify;
- raise can-execute changed when `Name` changes.

- [ ] **Step 5: Use observable list source**

Set `ListBox.ItemsSource = Items` or equivalent from the previous plan.

- [ ] **Step 6: Use default theme/style instead of hard-coded local colors**

The primary button should get its template/chrome from `DefaultTheme.CreateStyleSheet()`.

Local layout values like margin/padding are fine. Avoid local visual colors unless they are content-specific and tests allow them.

---

### Task 3: Wire Sample Into Playground

**Files:**
- Modify: `Playground/Cerneala.Playground/Game1.cs`
- Modify only if needed: sample registration files.

- [ ] **Step 1: Add sample registration**

Register `AuthoringAppSample` alongside `RetainedAppSample` if the playground has a sample list.

- [ ] **Step 2: Keep Core Preview sample intact**

Do not remove or weaken `RetainedAppSample`.

- [ ] **Step 3: Ensure resource/theme setup works**

If `Game1` creates root theme/resources, authoring sample should use those same services.

---

### Task 4: Fix Owning-Layer Bugs Exposed By The Gate

**Files:**
- Modify only files owned by the failing contract.

- [ ] **Step 1: If command state fails, fix command-state processor**

Do not work around it in the sample.

- [ ] **Step 2: If binding fails, fix `BindingOperations`/binding lifetime**

Do not manually sync sample state outside the binding path except for command behavior.

- [ ] **Step 3: If TextBox visuals/input fail, fix `TextBoxBase`**

Do not fake text input in the sample tests.

- [ ] **Step 4: If list mutation fails, fix `ItemsControl`/`ItemContainerGenerator`**

Do not replace the whole list in the sample.

- [ ] **Step 5: If semantics fails, fix `UI/Accessibility`**

Do not build a sample-specific semantics tree.

---

### Task 5: Verify GREEN And Full Completion

- [ ] **Step 1: Run authoring gate tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~AuthoringAppSampleContractTests|FullyQualifiedName~AuthoringPreviewContractTests"
```

Expected: GREEN.

- [ ] **Step 2: Run previous plan gates**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~CommandStateSchedulerTests|FullyQualifiedName~UiPropertyBindingTests|FullyQualifiedName~TextBoxEditingVisualContractTests|FullyQualifiedName~TemplatedButtonStateContractTests|FullyQualifiedName~ItemsSourceObservableTests|FullyQualifiedName~RetainedSemanticsCacheTests|FullyQualifiedName~CorePreviewContractTests"
```

Expected: GREEN.

- [ ] **Step 3: Run full solution**

```powershell
dotnet test Cerneala.slnx
```

Expected: GREEN.

- [ ] **Step 4: Run root test command**

```powershell
dotnet test
```

Expected: GREEN.

- [ ] **Step 5: Update project memory if convention requires it**

Update `ROADMAPv2.md` only for precise contracts proven by tests. Do not mark broad areas scenario-complete if this gate only proves the authoring slice.

- [ ] **Step 6: Commit implementation**

```powershell
git add Playground\Cerneala.Playground tests\Cerneala.Tests ROADMAPv2.md
git commit -m "feat: add authoring preview completion gate"
```
