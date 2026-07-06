# Typed Binding Lifetime And Two-Way Text Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, RED test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Make Cerneala's existing typed binding primitives usable in retained UI authoring. A developer should be able to bind an `ObservableValue<T>` to a typed `UiProperty<T>`, optionally two-way, with deterministic disposal tied to element lifetime.

**Architecture:** Keep data flow explicit and typed. Do not add WPF `DataContext`, reflection-heavy property paths, or XAML binding syntax. Bindings should be ordinary disposable subscriptions that write through existing `UiProperty<T>` invalidation. Element-owned bindings exist only to manage lifetime, not to create hidden magic.

**Tech Stack:** C#/.NET 8, xUnit, existing `UI/Data`, `UI/Core`, `UI/Elements`, `TextBoxBase.TextProperty`, retained invalidation.

---

## File Structure

- Create: `UI/Data/UiPropertyBinding{T}.cs`
  - Disposable typed subscription connecting `ObservableValue<T>` and `UiObject`/`UiProperty<T>`.
- Create: `UI/Data/BindingOperations.cs`
  - Small factory for one-way/two-way typed UI property bindings.
- Create: `UI/Data/BindingSubscriptionCollection.cs`
  - Owned collection for `IDisposable` bindings.
- Modify: `UI/Elements/UIElement.cs`
  - Add `Bindings` or `BindingSubscriptions` collection and dispose owned bindings on detach.
- Modify: `UI/Controls/TextBoxBase.cs`
  - No binding logic should be hard-coded here, but tests use its `TextProperty` and `PropertyChanged` notifications.
- Modify: `UI/Data/Binding.cs`
- Modify: `UI/Data/Binding{T}.cs`
  - Keep existing facade intact; add helpers only if needed.
- Create: `tests/Cerneala.Tests/UI/Data/UiPropertyBindingTests.cs`
- Create: `tests/Cerneala.Tests/Controls/TextBoxTwoWayBindingTests.cs`

## Important Existing Behavior

- `ObservableValue<T>` already publishes typed old/new changes.
- `Binding<T>` already supports explicit source-to-target updates and manual two-way commit.
- `PropertyAdapter.ForUiProperty(...)` already writes through `UiProperty<T>` and therefore triggers existing invalidation.
- `TextBoxBase.TextProperty` already raises property changed and invalidates measure/render on text changes.
- `UIElement` has attach/detach lifecycle hooks but no binding lifetime collection.

Target behavior:

- One-way typed property binding updates a UI property and uses existing invalidation metadata.
- Two-way typed property binding updates the source when the target property changes.
- Element-owned bindings are disposed when the element detaches from root.
- Replacing/removing bindings does not leak source subscriptions.
- No string property path participates in this core path.

## Rules

- [ ] Do not add WPF-style `DataContext`.
- [ ] Do not add string path binding to hot paths.
- [ ] Do not add reflection property walking.
- [ ] Do not add binding precedence changes unless tests prove they are required.
- [ ] Do not add `UiPropertyValueSource.Binding` in this plan unless Shadow explicitly approves the value-source precedence design.
- [ ] Binding writes may use the normal local value path for now.
- [ ] Keep binding errors fail-fast and unsubscribed on failure.

---

### Task 1: Add RED Typed UI Property Binding Tests

**Files:**
- Create: `tests/Cerneala.Tests/UI/Data/UiPropertyBindingTests.cs`
- Create: `tests/Cerneala.Tests/Controls/TextBoxTwoWayBindingTests.cs`

- [ ] **Step 1: Add one-way and invalidation tests**

Create tests:

```csharp
OneWayUiPropertyBindingUpdatesTargetImmediately()
OneWayUiPropertyBindingInvalidatesThroughUiPropertyMetadata()
SourceChangeAfterDisposeDoesNotUpdateTarget()
FailedInitialTargetWriteUnsubscribesSource()
BindingOperationsRejectsReadOnlyTargetProperty()
```

Test intent:

- Bind `ObservableValue<string>` to `TextBlock.TextProperty` or a small test `UiProperty<T>`.
- Assert initial update occurs.
- Assert source change updates target and enqueues retained invalidation.
- Assert disposal prevents further updates.

- [ ] **Step 2: Add element-owned lifetime tests**

Create tests:

```csharp
ElementOwnedBindingDisposesOnDetachFromRoot()
ElementOwnedBindingSurvivesUnchangedFramesWithoutExtraWork()
ReplacingElementOwnedBindingDisposesPreviousSubscription()
```

Test intent:

- Attach an element to `UIRoot`, add a binding to its binding collection, detach the element, then mutate source.
- Assert target no longer changes.

- [ ] **Step 3: Add two-way TextBox tests**

Create tests:

```csharp
TextBoxTwoWayBindingInitializesTextFromSource()
TextBoxTextInputCommitsToObservableSource()
SourceChangeUpdatesTextBoxWithoutRecursiveLoop()
DisposedTextBoxBindingStopsBothDirections()
```

Use `TextInputBridge`/`UiHost.Update(...)` where possible so the test exercises the retained input path.

- [ ] **Step 4: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UiPropertyBindingTests|FullyQualifiedName~TextBoxTwoWayBindingTests"
```

Expected: RED because typed UI property binding operations and element-owned binding lifetime do not exist.

- [ ] **Step 5: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\UI\Data\UiPropertyBindingTests.cs tests\Cerneala.Tests\Controls\TextBoxTwoWayBindingTests.cs
git commit -m "test: capture typed ui property binding lifetime"
```

---

### Task 2: Add `UiPropertyBinding<T>` And `BindingOperations`

**Files:**
- Create: `UI/Data/UiPropertyBinding{T}.cs`
- Create: `UI/Data/BindingOperations.cs`

- [ ] **Step 1: Implement disposable one-way binding**

`UiPropertyBinding<T>` should:

- hold source `ObservableValue<T>`;
- hold target `UiObject` and `UiProperty<T>`;
- subscribe to `source.ValueChanged`;
- write target via `target.SetValue(property, value)`;
- unsubscribe on dispose;
- avoid writing when disposed.

- [ ] **Step 2: Implement optional two-way behavior**

For `BindingMode.TwoWay`, subscribe to `target.PropertyChanged` and when the changed property matches the bound target property, update the source.

Avoid loops with a simple reentrancy guard:

```text
source -> target update should not immediately commit target -> source again
```

- [ ] **Step 3: Add factory methods**

Add methods such as:

```csharp
BindingOperations.BindOneWay<T>(UiObject target, UiProperty<T> targetProperty, ObservableValue<T> source)
BindingOperations.BindTwoWay<T>(UiObject target, UiProperty<T> targetProperty, ObservableValue<T> source)
BindingOperations.Bind<T>(UiObject target, UiProperty<T> targetProperty, ObservableValue<T> source, BindingMode mode)
```

Return `IDisposable` or `UiPropertyBinding<T>`.

- [ ] **Step 4: Keep old `Binding<T>` API working**

Do not remove the existing `Binding.OneWay(...)`, `Binding.TwoWay(...)`, or converter tests.

---

### Task 3: Add Element-Owned Binding Lifetime

**Files:**
- Create: `UI/Data/BindingSubscriptionCollection.cs`
- Modify: `UI/Elements/UIElement.cs`

- [ ] **Step 1: Implement owned subscription collection**

The collection should support:

```csharp
Add(IDisposable binding)
Remove(IDisposable binding)
Clear()
Count
```

It should dispose removed/cleared bindings exactly once.

- [ ] **Step 2: Add collection to `UIElement`**

Add a property such as:

```csharp
public BindingSubscriptionCollection Bindings { get; }
```

Name can be `Bindings` if consistent with existing authoring style, or `BindingSubscriptions` if avoiding WPF ambiguity.

- [ ] **Step 3: Dispose on detach**

In `UIElement.OnDetached()` or the detach path, dispose element-owned bindings.

Be careful: `OnDetached()` is virtual and already used by derived classes. Preserve override behavior and call order.

- [ ] **Step 4: Do not auto-dispose external bindings**

Only bindings added to the element-owned collection are lifetime-managed by the element. Bindings returned directly to user code remain user-owned.

---

### Task 4: Verify Two-Way TextBox Path

**Files:**
- Modify only if tests expose bugs:
  - `UI/Controls/TextBoxBase.cs`
  - `UI/Input/TextInputBridge.cs`
  - `UI/Input/ElementInputBridge.cs`

- [ ] **Step 1: Ensure `TextBoxBase.Text` changes raise exactly one useful property change**

`ReceiveTextInput(...)`, backspace/delete, undo/redo, and direct `Text` assignment should all ultimately update `TextProperty`.

- [ ] **Step 2: Ensure two-way binding sees user edits**

The target-to-source side should observe `TextBoxBase.TextProperty` changes through `PropertyChanged`.

- [ ] **Step 3: Avoid binding-specific logic in `TextBoxBase`**

Do not add a binding field to `TextBoxBase`. The binding layer should be generic.

---

### Task 5: Verify GREEN And Regressions

- [ ] **Step 1: Run targeted binding tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UiPropertyBindingTests|FullyQualifiedName~TextBoxTwoWayBindingTests|FullyQualifiedName~TypedBindingTests|FullyQualifiedName~ObservableValueTests"
```

Expected: GREEN.

- [ ] **Step 2: Run text/input tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~TextBoxTests|FullyQualifiedName~TextInputBridgeTests|FullyQualifiedName~CorePreviewContractTests"
```

Expected: GREEN.

- [ ] **Step 3: Run full suite**

```powershell
dotnet test Cerneala.slnx
```

Expected: GREEN.

- [ ] **Step 4: Commit implementation**

```powershell
git add UI\Data UI\Elements\UIElement.cs UI\Controls\TextBoxBase.cs tests\Cerneala.Tests
git commit -m "feat: add typed ui property binding lifetime"
```
