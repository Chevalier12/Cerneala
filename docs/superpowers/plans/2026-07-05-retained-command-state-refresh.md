# Retained Command State Refresh Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, RED test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Make command enablement refresh through retained frame processing. `ButtonBase.RefreshCommandState(...)` already exists, but command state is still mostly manual. Cerneala needs explicit, testable command state refresh without cloning WPF's global `CommandManager` magic.

**Architecture:** Add a small retained command-state queue/processor owned by the root/scheduler. Do not add global requery, ambient command manager, reflection scanning, or compatibility behavior. Command sources opt in explicitly. Command state refresh runs before style/layout/render so disabled/hover/focus visual state is coherent in the same update.

**Tech Stack:** C#/.NET 8, xUnit, existing `UI/Input`, `UI/Controls/Primitives/ButtonBase`, retained scheduler, `ElementInputCache`, `CommandRouter`.

---

## File Structure

- Create: `UI/Input/ICommandStateSource.cs`
  - Small opt-in interface for retained command sources that can refresh enablement.
- Create: `UI/Input/IObservableCommand.cs`
  - Optional command notification contract. Do not modify `ICommand` to avoid broad breaking changes.
- Modify: `UI/Input/ActionCommand.cs`
  - Implement `IObservableCommand` and add explicit `RaiseCanExecuteChanged()`.
- Create: `UI/Invalidation/CommandStateQueue.cs`
  - Root-owned queue for command source refresh work.
- Modify: `UI/Invalidation/FramePhase.cs`
  - Add `CommandState` before `Style`.
- Modify: `UI/Invalidation/FramePhaseProcessors.cs`
  - Add `CommandState` processor delegate.
- Modify: `UI/Invalidation/UiFrameScheduler.cs`
  - Process one deterministic command-state snapshot before style.
- Modify: `UI/Invalidation/FrameStats.cs`
  - Count command-state refreshed elements.
- Modify: `UI/Elements/UIRoot.cs`
  - Own `CommandStateQueue`; create command-state processor using `InputCache.EnsureCurrent(root)` and `CommandRouter`.
- Modify: `UI/Elements/UIElement.cs`
  - Expose an internal/public helper to queue command-state refresh for attached command sources.
- Modify: `UI/Input/CommandBindingCollection.cs`
  - Let binding add/remove notify the owning element/root so routed command state can refresh.
- Modify: `UI/Controls/Primitives/ButtonBase.cs`
  - Implement `ICommandStateSource`, subscribe/unsubscribe observable command changes, and queue refresh on command/parameter changes.
- Create: `tests/Cerneala.Tests/Input/CommandStateSchedulerTests.cs`
- Create: `tests/Cerneala.Tests/Controls/Primitives/ButtonBaseCommandStateIntegrationTests.cs`

## Important Existing Behavior

- `ButtonBase.CommandProperty` and `CommandParameterProperty` exist.
- `ButtonBase.RefreshCommandState(CommandRouter, ElementInputRouteMap)` exists and toggles `IsEnabled` based on `CanExecute`.
- `ButtonBase.ExecuteCommand(...)` already respects `IsEnabled` and direct/routed command state.
- `CommandRouter` already performs explicit route-based `CanExecute`/`Execute`.
- `UiFrameScheduler` currently has no command-state phase.
- `ICommand` intentionally has no event. Do not break that interface.

Target behavior:

- Setting a command or command parameter queues command-state refresh when attached.
- `ActionCommand.RaiseCanExecuteChanged()` queues refresh for attached command sources that use it.
- Adding/removing command bindings on an ancestor can refresh routed command sources in that subtree.
- Command-state refresh happens before style so disabled visual-state rules can apply in the same update.
- Unchanged frames do not refresh command state again.

## Rules

- [ ] Do not add a WPF-style global `CommandManager`.
- [ ] Do not add automatic periodic requery.
- [ ] Do not walk the whole tree every frame.
- [ ] Do not modify `ICommand` to add an event.
- [ ] Do not make input routing depend on concrete controls.
- [ ] Keep route-based command execution through `CommandRouter` only.

---

### Task 1: Add RED Command-State Scheduler Tests

**Files:**
- Create: `tests/Cerneala.Tests/Input/CommandStateSchedulerTests.cs`
- Create: `tests/Cerneala.Tests/Controls/Primitives/ButtonBaseCommandStateIntegrationTests.cs`

- [ ] **Step 1: Add command queue/scheduler tests**

Create tests:

```csharp
CommandSourceAttachedWithCannotExecuteCommandDisablesBeforeStyleAndRender()
CommandPropertyChangeQueuesSingleCommandStateRefresh()
CommandParameterChangeQueuesSingleCommandStateRefresh()
ObservableCommandCanExecuteChangedQueuesRefreshWithoutGlobalRequery()
UnchangedSecondFrameDoesNotRefreshCommandStateAgain()
```

Test intent:

- Attach a `Button`/`ButtonBase` under `UIRoot`.
- Give it an `ActionCommand` with `CanExecute` returning false.
- Run `root.ProcessFrame()` or `UiHost.Update(...)`.
- Assert `button.IsEnabled == false` and `FrameStats.CommandStateElements > 0`.
- Run a second unchanged update and assert command-state count is zero.

- [ ] **Step 2: Add routed command integration tests**

Create tests:

```csharp
RoutedCommandBindingAddRefreshesAffectedButtonState()
RoutedCommandBindingCanExecuteFalseDisablesButtonBeforeKeyboardActivation()
DisabledByCommandStateDoesNotExecuteMouseClick()
DisabledByCommandStateDoesNotExecuteKeyboardActivation()
```

Test intent:

- Use a routed command with a binding on root or ancestor.
- Change binding behavior from can-execute true to false by replacing/adding binding in a controlled way.
- Assert the button state refreshes through the retained frame before execution paths run.

- [ ] **Step 3: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~CommandStateSchedulerTests|FullyQualifiedName~ButtonBaseCommandStateIntegrationTests"
```

Expected: RED because command-state queue/phase and observable command notifications do not exist yet.

- [ ] **Step 4: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\Input\CommandStateSchedulerTests.cs tests\Cerneala.Tests\Controls\Primitives\ButtonBaseCommandStateIntegrationTests.cs
git commit -m "test: capture retained command state refresh contract"
```

---

### Task 2: Add Explicit Command-State Contracts

**Files:**
- Create: `UI/Input/ICommandStateSource.cs`
- Create: `UI/Input/IObservableCommand.cs`
- Modify: `UI/Input/ActionCommand.cs`
- Modify: `UI/Controls/Primitives/ButtonBase.cs`

- [ ] **Step 1: Add command source refresh interface**

Add a small interface in `UI/Input`:

```csharp
public interface ICommandStateSource
{
    bool RefreshCommandState(CommandRouter router, ElementInputRouteMap routeMap);
}
```

Then have `ButtonBase` implement it using the existing `RefreshCommandState(...)` method.

- [ ] **Step 2: Add optional observable command contract**

```csharp
public interface IObservableCommand : ICommand
{
    event EventHandler? CanExecuteChanged;
}
```

Do not alter `ICommand`.

- [ ] **Step 3: Update `ActionCommand`**

Implement `IObservableCommand` and add:

```csharp
public event EventHandler? CanExecuteChanged;

public void RaiseCanExecuteChanged()
{
    CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
```

- [ ] **Step 4: Subscribe in `ButtonBase`**

When `Command` changes:

- Unsubscribe old `IObservableCommand.CanExecuteChanged`.
- Subscribe new observable command.
- Queue retained command-state refresh if attached.

When `CommandParameter` changes, queue retained command-state refresh.

---

### Task 3: Add Root-Owned Command-State Queue And Frame Phase

**Files:**
- Create: `UI/Invalidation/CommandStateQueue.cs`
- Modify: `UI/Invalidation/FramePhase.cs`
- Modify: `UI/Invalidation/FramePhaseProcessors.cs`
- Modify: `UI/Invalidation/UiFrameScheduler.cs`
- Modify: `UI/Invalidation/FrameStats.cs`
- Modify: `UI/Elements/UIRoot.cs`
- Modify: `UI/Elements/UIElement.cs`

- [ ] **Step 1: Implement `CommandStateQueue`**

Follow existing queue style:

- deterministic snapshot order by element tree order where possible;
- duplicate enqueue suppression;
- `Enqueue(UIElement element)`, `Remove(UIElement element)`, `Snapshot()`, `HasWork`, `Count`.

- [ ] **Step 2: Add scheduler phase**

Add `FramePhase.CommandState` and process it after inherited properties but before style:

```text
InheritedProperties -> CommandState -> Style -> InheritedProperties -> Measure -> Arrange -> RenderCache -> HitTest
```

Reason: command-state may set `IsEnabled`, which affects style, hit testing, and rendering.

- [ ] **Step 3: Add stats**

Add a counter such as `CommandStateElements` or use phase count naming consistent with `FrameStats`.

Update diagnostics code if it enumerates known phases.

- [ ] **Step 4: Root processor implementation**

In `UIRoot.CreatePhaseProcessors()`, command-state processing should:

- ensure the retained input cache/route map is current;
- call `RefreshCommandState(router, routeMap)` for elements implementing `ICommandStateSource`;
- not execute commands;
- not create work on unchanged command state.

- [ ] **Step 5: Queue helpers**

Add a helper such as `UIElement.QueueCommandStateRefresh()` or internal root method. It should:

- do nothing if detached except mark dirty enough to refresh on attach;
- enqueue only command-state sources;
- avoid whole-tree scans.

---

### Task 4: Wire CommandBinding Changes To Routed Command Refresh

**Files:**
- Modify: `UI/Input/CommandBindingCollection.cs`
- Modify: `UI/Elements/UIElement.cs`
- Modify: `UI/Elements/UIRoot.cs`

- [ ] **Step 1: Give `CommandBindingCollection` an owner**

`UIElement.CommandBindings` currently constructs a collection without owner context. Change construction carefully so add/remove can notify the owner.

- [ ] **Step 2: Add minimal remove support if missing**

If routed command refresh tests need removal/replacement, add `Remove(...)` and `Clear()` to `CommandBindingCollection` with notifications.

- [ ] **Step 3: Refresh affected command sources**

When bindings change on an element, queue command-state refresh for descendant command sources. This is one of the rare cases where a subtree scan is acceptable because binding changes are authoring-time or infrequent, not per-frame.

Do not scan every frame.

---

### Task 5: Verify GREEN And Regressions

- [ ] **Step 1: Run targeted command tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~CommandStateSchedulerTests|FullyQualifiedName~ButtonBaseCommandStateIntegrationTests|FullyQualifiedName~ButtonBaseCommandTests|FullyQualifiedName~CommandRouterTests|FullyQualifiedName~RoutedCommandExecutionTests"
```

Expected: GREEN.

- [ ] **Step 2: Run input/control integration tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~ButtonKeyboardActivationTests|FullyQualifiedName~RetainedInputBindingTests|FullyQualifiedName~CorePreviewContractTests"
```

Expected: GREEN.

- [ ] **Step 3: Run full suite**

```powershell
dotnet test Cerneala.slnx
```

Expected: GREEN.

- [ ] **Step 4: Commit implementation**

```powershell
git add UI\Input UI\Invalidation UI\Elements UI\Controls\Primitives\ButtonBase.cs tests\Cerneala.Tests
git commit -m "feat: add retained command state refresh"
```
