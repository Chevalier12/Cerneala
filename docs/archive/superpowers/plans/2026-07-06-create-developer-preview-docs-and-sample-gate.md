# Create Developer Preview Docs And Sample Gate Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, RED test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Make the supported code-first path obvious. Developer Preview needs a getting-started document and a small sample that uses the actual supported stack: retained root/host, default theme, typed binding, command state, TextBox, Button, ListBox/ItemsSource, Grid/StackPanel, Tab focus navigation, and retained no-work/draw-purity invariants.

**Architecture:** Write docs against the current code-first API. Do not introduce markup, source generation, XAML, reflection binding, package split, or new controls. The docs must describe what works now and what is deferred. The sample must compile and be covered by tests.

**Tech Stack:** Markdown docs, Playground sample, xUnit docs/sample contract tests, existing UI framework APIs.

---

## File Structure

- Create: `docs/getting-started.md`
- Create: `docs/developer-preview-checklist.md`
- Create: `Playground/Cerneala.Playground/Samples/GettingStartedSample.cs`
- Modify: `Playground/Cerneala.Playground/Samples/SampleSelector.cs` or `Playground/Cerneala.Playground/Game1.cs` only if sample registration requires it.
- Create: `tests/Cerneala.Tests/Docs/GettingStartedDocsTests.cs`
- Create: `tests/Cerneala.Tests/Playground/Samples/GettingStartedSampleContractTests.cs`

## Important Existing Behavior

- `AuthoringAppSample` proves typed binding, commands, TextBox, list, and default theme behavior.
- `RuntimePreviewSample` proves viewport scaling, resources, platform services, diagnostics, and runtime invariants.
- There is no first-class getting-started doc in `docs/`.
- Preview scope docs from the previous plan should now describe supported/deferred surfaces.

Target behavior:

- `docs/getting-started.md` shows a minimal supported code-first UI path.
- `docs/developer-preview-checklist.md` explains how to validate a local repo and run the preview gates.
- `GettingStartedSample` builds a compact UI using existing controls and APIs.
- Tests assert docs mention required concepts and do not recommend frozen/deferred paths.
- Tests assert the sample builds, updates, draws, supports Tab focus, handles text input/commands/list mutation, and has no retained work on unchanged frames.

## Rules

- [ ] Do not add markup/sourcegen examples.
- [ ] Do not add string-path binding examples.
- [ ] Do not add new controls for the sample.
- [ ] Do not hard-code every visual color locally when default theme/style should provide control chrome.
- [ ] Do not weaken existing Authoring/Runtime samples.
- [ ] Do not write docs that claim package split/native accessibility/full IME/advanced rendering are complete.

---

### Task 1: Add RED Docs And Sample Tests

**Files:**
- Create: `tests/Cerneala.Tests/Docs/GettingStartedDocsTests.cs`
- Create: `tests/Cerneala.Tests/Playground/Samples/GettingStartedSampleContractTests.cs`

- [ ] **Step 1: Add docs tests**

Create tests:

```csharp
GettingStartedDocumentExists()
GettingStartedDocumentMentionsRetainedUpdateDrawContract()
GettingStartedDocumentUsesUiHostUiRootDefaultThemeAndBindingOperations()
GettingStartedDocumentShowsCodeFirstPathWithoutMarkupOrXaml()
GettingStartedDocumentMentionsDeferredScopeForMarkupPackageSplitAndFullIme()
DeveloperPreviewChecklistNamesTargetedAndFullTestCommands()
DeveloperPreviewChecklistNamesArchiveCommand()
```

- [ ] **Step 2: Add sample structure tests**

Create tests:

```csharp
GettingStartedSampleBuildsRootTextBoxButtonListAndStatusText()
GettingStartedSampleUsesObservableListItemsSource()
GettingStartedSampleUsesTypedTwoWayTextBinding()
GettingStartedSampleUsesDefaultThemeForButtonChrome()
GettingStartedSampleUsesGridOrStackPanelWithoutUnsupportedLayoutApis()
```

- [ ] **Step 3: Add sample behavior tests**

Create tests:

```csharp
GettingStartedSampleFirstFrameDoesRetainedWork()
GettingStartedSampleSecondUnchangedFrameDoesNoRetainedWork()
GettingStartedSampleDrawDoesNotGenerateRetainedWork()
GettingStartedSampleTabMovesFocusFromTextBoxToButton()
GettingStartedSampleTextInputEnablesCommandAndButtonAddsListItem()
GettingStartedSampleGridDefinitionMutationStillProducesNoWorkNextFrame()
```

- [ ] **Step 4: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~GettingStartedDocsTests|FullyQualifiedName~GettingStartedSampleContractTests"
```

Expected: RED because docs/sample do not exist yet.

- [ ] **Step 5: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\Docs\GettingStartedDocsTests.cs tests\Cerneala.Tests\Playground\Samples\GettingStartedSampleContractTests.cs
git commit -m "test: capture developer preview docs and sample gate"
```

---

### Task 2: Create Getting Started Docs

**Files:**
- Create: `docs/getting-started.md`
- Create: `docs/developer-preview-checklist.md`

- [ ] **Step 1: Write `docs/getting-started.md`**

Keep it concise and executable in spirit. Include sections:

```text
What Cerneala is
Retained update/draw contract
Create a root
Apply default theme
Build UI in code
Bind TextBox.Text with BindingOperations
Use ActionCommand and command state
Use ObservableList as ItemsSource
Run Update every frame and Draw every frame
What not to use yet
```

- [ ] **Step 2: Include minimal code snippets**

Snippets should use real API names from the current repo:

```text
UIRoot
UiHost
UiViewport
DefaultTheme.CreateStyleSheet()
ObservableValue<string>
ObservableList<string>
BindingOperations.BindTwoWay(...)
ActionCommand
TextBox
Button
ListBox
```

- [ ] **Step 3: Write `docs/developer-preview-checklist.md`**

Include commands:

```powershell
dotnet test Cerneala.slnx
dotnet test
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\scripts\Archive-Repo.ps1 -RepoRoot .
```

Also include targeted gate names for Core/Authoring/Runtime/Developer Preview.

---

### Task 3: Add Getting Started Playground Sample

**Files:**
- Create: `Playground/Cerneala.Playground/Samples/GettingStartedSample.cs`
- Modify sample registration only if needed.

- [ ] **Step 1: Build sample state**

Use explicit state:

```text
ObservableValue<string> EntryText
ObservableValue<string> StatusText
ObservableList<string> Items
ActionCommand AddCommand
```

- [ ] **Step 2: Build UI with existing controls**

Use a compact tree:

```text
Grid or StackPanel
TextBlock title
TextBox entry
Button add
TextBlock status
ListBox items
```

- [ ] **Step 3: Use typed bindings**

Bind `TextBoxBase.TextProperty` two-way to `EntryText` and `TextBlock.TextProperty` one-way to `StatusText`.

- [ ] **Step 4: Use command state**

`AddCommand.CanExecute` should be false for empty/whitespace text. Raise can-execute changed when `EntryText` changes.

- [ ] **Step 5: Use observable ItemsSource**

Set `ListBox.ItemsSource = Items`.

- [ ] **Step 6: Use default theme**

The sample should work under `DefaultTheme.CreateStyleSheet()` and not locally define all button chrome.

---

### Task 4: Verify GREEN And Regressions

- [ ] **Step 1: Run docs/sample tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~GettingStartedDocsTests|FullyQualifiedName~GettingStartedSampleContractTests"
```

Expected: GREEN.

- [ ] **Step 2: Run preview sample tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~AuthoringAppSampleContractTests|FullyQualifiedName~RuntimePreviewSampleContractTests|FullyQualifiedName~RuntimePreviewIntegrationTests|FullyQualifiedName~PlaygroundSampleTests"
```

Expected: GREEN.

- [ ] **Step 3: Run scope tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~DeveloperPreviewScopeTests"
```

Expected: GREEN.

- [ ] **Step 4: Run full suite**

```powershell
dotnet test Cerneala.slnx
```

Expected: GREEN.

- [ ] **Step 5: Commit implementation**

```powershell
git add docs Playground\Cerneala.Playground\Samples tests\Cerneala.Tests
git commit -m "docs: add developer preview getting started gate"
```
