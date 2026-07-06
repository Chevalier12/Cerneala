# Close Retained Lifecycle Subscription Leaks Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, RED test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Make retained attach/detach lifecycle trustworthy. Cerneala now has command subscriptions, typed bindings, template bindings, observable item sources, resource dependencies, image caches, and scheduler queues. Developer Preview needs detached UI to stop receiving notifications and detached queued elements to stop processing.

**Architecture:** Keep ownership local and explicit. Add cleanup at the owning layer: controls unsubscribe from their sources on detach, root removes resource dependencies for detached elements, queues ignore/remove detached work, and element-owned bindings remain the only automatic binding lifetime. Do not add a global lifetime container or framework-wide disposable graph.

**Tech Stack:** C#/.NET 8, xUnit, existing `UI/Elements`, `UI/Controls`, `UI/Data`, `UI/Resources`, `UI/Invalidation`, `UI/Input`.

---

## File Structure

- Modify: `UI/Elements/ElementLifecycle.cs`
  - Clear root-owned per-element state when an element detaches.
- Modify: `UI/Elements/UIElement.cs`
  - Add tiny lifecycle hooks only if existing `OnDetached` is insufficient.
- Modify: `UI/Controls/ItemsControl.cs`
  - Unsubscribe observable `ItemsSource` on detach.
- Modify: `UI/Resources/ResourceDependencyTracker.cs`
  - Add explicit `RemoveOwner(UIElement owner)` and/or stronger cleanup across resource maps.
- Modify: `UI/Invalidation/LayoutQueue.cs`
- Modify: `UI/Invalidation/RenderQueue.cs`
- Modify: `UI/Invalidation/StyleQueue.cs`
- Modify: `UI/Invalidation/HitTestQueue.cs`
- Modify: `UI/Invalidation/CommandStateQueue.cs`
- Modify: `UI/Invalidation/InheritedPropertyQueue.cs`
  - Ignore or remove detached queued elements consistently.
- Modify only if tests expose leaks:
  - `UI/Controls/TemplateInstance.cs`
  - `UI/Controls/TemplateBinding{T}.cs`
  - `UI/Controls/ContentPresenter.cs`
  - `UI/Controls/Primitives/ButtonBase.cs`
  - `UI/Data/BindingSubscriptionCollection.cs`
- Create: `tests/Cerneala.Tests/UI/Elements/RetainedLifecycleCleanupTests.cs`
- Create: `tests/Cerneala.Tests/UI/Resources/DetachedResourceDependencyCleanupTests.cs`
- Create: `tests/Cerneala.Tests/UI/Invalidation/DetachedQueuedElementTests.cs`

## Important Existing Behavior

- `UIElement.DetachFromRoot()` calls `OnDetached()` and clears element-owned `Bindings`.
- `ButtonBase.OnDetached()` unsubscribes from observable command changes.
- `ItemsControl` subscribes to observable `ItemsSource`, but detach behavior needs an explicit test gate.
- `ResourceDependencyTracker.NotifyResourceChanged(...)` lazily cleans detached owners for the changed resource only.
- Scheduler queues may hold elements that detach before the next frame.

Target behavior:

- Detached `ItemsControl` no longer receives observable source changes.
- Detached image/text/resource dependents are removed from root resource dependency tracking without waiting for every individual resource key to change.
- Detached command sources do not process command-state queue work.
- Detached style/layout/render/hit-test/inherited queue entries are skipped or purged deterministically.
- Template replacement/disposal and content presentation do not leak generated children or template binding subscriptions.
- Lifecycle cleanup does not add per-frame scans.

## Rules

- [ ] Do not add a global dependency injection/lifetime container.
- [ ] Do not make every `UIElement` implement `IDisposable` unless tests prove it is unavoidable.
- [ ] Do not dispose user-owned content elements merely because they were removed from a tree.
- [ ] Do not clear bindings that were not explicitly added to `UIElement.Bindings`.
- [ ] Do not add finalizers or GC-dependent behavior.
- [ ] Do not scan the whole tree every frame for leaks.

---

### Task 1: Add RED Lifecycle Leak Tests

**Files:**
- Create: `tests/Cerneala.Tests/UI/Elements/RetainedLifecycleCleanupTests.cs`
- Create: `tests/Cerneala.Tests/UI/Resources/DetachedResourceDependencyCleanupTests.cs`
- Create: `tests/Cerneala.Tests/UI/Invalidation/DetachedQueuedElementTests.cs`

- [ ] **Step 1: Add subscription cleanup tests**

Create tests:

```csharp
DetachedItemsControlUnsubscribesObservableItemsSource()
DetachedButtonUnsubscribesObservableCommandCanExecuteChanged()
DetachedElementOwnedBindingStopsReceivingSourceChanges()
TemplateReplacementDisposesTemplateBindingsExactlyOnce()
ContentPresenterContentReplacementDetachesGeneratedChildParents()
```

Use simple recording sources/commands/bindings with counters.

- [ ] **Step 2: Add resource dependency cleanup tests**

Create tests:

```csharp
DetachedImageResourceDependentIsRemovedFromRootTracker()
DetachedTextResourceDependentIsRemovedFromRootTracker()
ResourceChangeAfterDependentDetachDoesNotInvalidateDetachedElement()
RootResourceDependencyTrackerDoesNotRetainDetachedElementAfterDetach()
```

Use `WeakReference` only as a last resort. Prefer explicit tracker counts or dependents inspection.

- [ ] **Step 3: Add detached queue tests**

Create tests:

```csharp
DetachedMeasureQueuedElementIsNotMeasured()
DetachedRenderQueuedElementDoesNotRebuildRenderCache()
DetachedStyleQueuedElementIsNotStyled()
DetachedCommandStateQueuedElementIsNotRefreshed()
DetachedHitTestQueuedElementDoesNotRebuildRouteEntry()
```

- [ ] **Step 4: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RetainedLifecycleCleanupTests|FullyQualifiedName~DetachedResourceDependencyCleanupTests|FullyQualifiedName~DetachedQueuedElementTests"
```

Expected: RED for at least ItemsSource detach, explicit resource cleanup, or detached queue handling.

- [ ] **Step 5: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\UI\Elements\RetainedLifecycleCleanupTests.cs tests\Cerneala.Tests\UI\Resources\DetachedResourceDependencyCleanupTests.cs tests\Cerneala.Tests\UI\Invalidation\DetachedQueuedElementTests.cs
git commit -m "test: capture retained lifecycle cleanup contract"
```

---

### Task 2: Clean Control-Owned Subscriptions On Detach

**Files:**
- Modify: `UI/Controls/ItemsControl.cs`
- Modify only if needed:
  - `UI/Controls/Primitives/ButtonBase.cs`
  - `UI/Controls/TemplateInstance.cs`
  - `UI/Controls/TemplateBinding{T}.cs`
  - `UI/Controls/ContentPresenter.cs`

- [ ] **Step 1: Add `ItemsControl.OnDetached()` cleanup**

Unsubscribe `observableItemsSource.Changed` when the control detaches. Preserve `ItemsSource` value; only detach the subscription. On reattach, resubscribe to current `ItemsSource` if observable.

- [ ] **Step 2: Verify command unsubscription behavior**

If tests expose a bug in `ButtonBase`, fix there. Do not move command subscription ownership into a global manager.

- [ ] **Step 3: Verify template/content cleanup**

If replacing templates/content leaks template bindings or generated children, fix the template/content owner classes. Do not add sample-specific cleanup.

---

### Task 3: Add Explicit Resource Dependency Owner Removal

**Files:**
- Modify: `UI/Resources/ResourceDependencyTracker.cs`
- Modify: `UI/Elements/ElementLifecycle.cs`

- [ ] **Step 1: Add `RemoveOwner(UIElement owner)`**

Remove the owner from all resource dependency maps and owner version maps.

- [ ] **Step 2: Call on detach**

When `ElementLifecycle.DetachSingle(...)` detaches an element, call:

```csharp
root.ResourceDependencyTracker.RemoveOwner(element);
```

- [ ] **Step 3: Keep lazy cleanup too**

Do not remove existing lazy cleanup in `NotifyResourceChanged(...)`; it is still defensive.

- [ ] **Step 4: Add cheap diagnostics if useful**

If tests need it, add an internal/test-visible count method. Avoid public diagnostic surface unless it is clearly useful.

---

### Task 4: Make Queues Detached-Safe

**Files:**
- Modify queue files under `UI/Invalidation/`.

- [ ] **Step 1: Inspect queue snapshot behavior**

For each queue, determine whether it can process detached elements if an element is detached after enqueue and before `ProcessFrame()`.

- [ ] **Step 2: Skip detached elements at snapshot/processing boundary**

Prefer filtering when producing a deterministic snapshot:

```text
only include elements whose Root matches the owning queue root
```

- [ ] **Step 3: Do not break detached dirty state**

If an element is detached but later reattached, its local `DirtyState` should still cause needed work after attach where existing behavior expects that.

- [ ] **Step 4: Keep counts honest**

Detached skipped items should not increment `FrameStats` phase counts.

---

### Task 5: Verify GREEN And Regressions

- [ ] **Step 1: Run targeted lifecycle tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RetainedLifecycleCleanupTests|FullyQualifiedName~DetachedResourceDependencyCleanupTests|FullyQualifiedName~DetachedQueuedElementTests"
```

Expected: GREEN.

- [ ] **Step 2: Run adjacent ownership tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~ItemsSourceObservableTests|FullyQualifiedName~UiPropertyBindingTests|FullyQualifiedName~ButtonBaseCommandStateIntegrationTests|FullyQualifiedName~TemplateBindingTests|FullyQualifiedName~ResourceDependencyTrackerTests"
```

Expected: GREEN.

- [ ] **Step 3: Run preview gates**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~CorePreviewContractTests|FullyQualifiedName~AuthoringPreviewContractTests|FullyQualifiedName~RuntimePreviewContractTests"
```

Expected: GREEN.

- [ ] **Step 4: Run full suite**

```powershell
dotnet test Cerneala.slnx
```

Expected: GREEN.

- [ ] **Step 5: Commit implementation**

```powershell
git add UI\Elements UI\Controls UI\Resources UI\Invalidation tests\Cerneala.Tests
git commit -m "fix: close retained lifecycle subscription leaks"
```
