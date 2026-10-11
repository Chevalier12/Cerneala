# Implement Inherited Property Tree Propagation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn `UiPropertyOptions.Inherits` from a store-level value source into explicit retained tree propagation. Font family, font size, and foreground should inherit through the retained UI tree without WPF-style global magic.

**Architecture:** Inheritance is a retained frame phase owned by `UIRoot`, not hidden reflection behavior inside property getters. Parent property changes and tree mutations enqueue inherited-property work. The propagator applies inherited-source values to descendants while preserving normal precedence: local, animation, style visual state, style base, template binding, inherited, default. MVP inheritance follows the retained visual tree because this repository currently uses `VisualChildren` as the authoritative retained tree for layout/render/input. Logical-tree inheritance can be added later when logical scopes are mature.

**Tech Stack:** C#/.NET 8, xUnit, Cerneala `UiProperty<T>`, retained invalidation scheduler.

---

## File Structure

- Modify: `UI/Core/UiPropertyRegistry.cs`
  - Add a way to enumerate registered properties, filtered by `UiPropertyOptions.Inherits`.
- Modify: `UI/Core/UiObject.cs`
  - Add internal untyped set/clear helpers for framework services that operate over `UiProperty` instances.
- Create: `UI/Elements/InheritedPropertyPropagator.cs`
  - Applies inherited values from parent to descendants.
- Create: `UI/Invalidation/InheritedPropertyQueue.cs`
  - Tracks roots whose descendant inherited values must be refreshed.
- Modify: `UI/Invalidation/InvalidationFlags.cs`
  - Add `Inherited` flag.
- Modify: `UI/Invalidation/FramePhase.cs`
  - Add `InheritedProperties` before `Style`.
- Modify: `UI/Invalidation/FramePhaseProcessors.cs`
  - Add inherited-property processor delegate.
- Modify: `UI/Invalidation/FrameStats.cs`
  - Add `InheritedElements` or `InheritedPropertyElements` counter.
- Modify: `UI/Invalidation/UiFrameScheduler.cs`
  - Process inherited-property work before style, measure, arrange, render, and hit-test.
- Modify: `UI/Invalidation/DirtyPropagation.cs`
  - Map `UiPropertyOptions.Inherits` changes to inherited-property subtree work.
- Modify: `UI/Elements/UIRoot.cs`
  - Own `InheritedPropertyQueue` and `InheritedPropertyPropagator`.
  - Include inherited processing in `CreatePhaseProcessors()`.
- Modify: `UI/Elements/UIElementCollection.cs` or the existing tree mutation helper
  - Enqueue inherited-property work when a subtree is attached or reparented.
- Modify: `UI/Controls/Control.cs`
  - Add `UiPropertyOptions.Inherits` to `ForegroundProperty`, `FontFamilyProperty`, and `FontSizeProperty`.
  - Do not make `Background`, `BorderBrush`, `Padding`, or `BorderThickness` inherit.
- Create: `tests/Cerneala.Tests/UI/Core/InheritedPropertyTreePropagationTests.cs`
  - Prove automatic tree propagation and precedence.
- Modify: `tests/Cerneala.Tests/UI/Invalidation/UiFrameSchedulerTests.cs`
  - Add inherited phase ordering coverage.
- Modify: `tests/Cerneala.Tests/UI/Invalidation/FrameStatsTests.cs`
  - Add inherited counter coverage.

## Important Existing Behavior

The store already supports an inherited source:

```csharp
owner.SetValue(property, "inherited", UiPropertyValueSource.Inherited);
```

But the tree does not automatically set that source. This means `UiPropertyOptions.Inherits` currently describes a property-store precedence feature, not a retained UI inheritance model.

Target behavior:

```csharp
parent.Foreground = Color.White;
root.ProcessFrame();

Assert.Equal(Color.White, child.GetValue(Control.ForegroundProperty));
Assert.Equal(UiPropertyValueSource.Inherited, child.GetValueSource(Control.ForegroundProperty));
```

Precedence must remain:

```text
Local > Animation > StyleVisualState > StyleBase > TemplateBinding > Inherited > Default
```

A child with a local/style/template value must keep its effective value while still allowing the inherited source to be updated underneath it.

---

### Task 1: Add RED Tests For Tree-Level Inheritance

**Files:**
- Create: `tests/Cerneala.Tests/UI/Core/InheritedPropertyTreePropagationTests.cs`
- Modify: `tests/Cerneala.Tests/UI/Invalidation/UiFrameSchedulerTests.cs`
- Modify: `tests/Cerneala.Tests/UI/Invalidation/FrameStatsTests.cs`

- [ ] **Step 1: Create tree propagation tests**

Create `tests/Cerneala.Tests/UI/Core/InheritedPropertyTreePropagationTests.cs` with tests covering at least:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.UI.Core;

public sealed class InheritedPropertyTreePropagationTests
{
    [Fact]
    public void ParentForegroundPropagatesToDescendantDuringFrame()
    {
        UIRoot root = new();
        Control parent = new() { Foreground = Color.White };
        TextBlock child = new() { Text = "child" };
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(parent);

        root.ProcessFrame();

        Assert.Equal(Color.White, child.Foreground);
        Assert.Equal(UiPropertyValueSource.Inherited, child.GetValueSource(Control.ForegroundProperty));
    }

    [Fact]
    public void LocalChildValueWinsOverInheritedValue()
    {
        UIRoot root = new();
        Control parent = new() { FontSize = 22 };
        TextBlock child = new() { FontSize = 11 };
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(parent);

        root.ProcessFrame();

        Assert.Equal(11, child.FontSize);
        Assert.Equal(UiPropertyValueSource.Local, child.GetValueSource(Control.FontSizeProperty));
    }

    [Fact]
    public void ChangingInheritedParentValueInvalidatesDescendantRender()
    {
        UIRoot root = new();
        Control parent = new() { Foreground = Color.Black };
        TextBlock child = new() { Text = "child" };
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(parent);
        root.ProcessFrame();
        child.DirtyState.Clear(Cerneala.UI.Invalidation.InvalidationFlags.Render);

        parent.Foreground = Color.White;
        var stats = root.ProcessFrame();

        Assert.Equal(Color.White, child.Foreground);
        Assert.True(stats.InheritedElements > 0);
        Assert.True(child.RenderVersion > 0);
    }

    [Fact]
    public void NewlyAttachedSubtreeReceivesInheritedValuesOnNextFrame()
    {
        UIRoot root = new();
        Control parent = new() { FontFamily = "Body" };
        root.VisualChildren.Add(parent);
        root.ProcessFrame();
        TextBlock child = new();

        parent.VisualChildren.Add(child);
        root.ProcessFrame();

        Assert.Equal("Body", child.FontFamily);
        Assert.Equal(UiPropertyValueSource.Inherited, child.GetValueSource(Control.FontFamilyProperty));
    }
}
```

- [ ] **Step 2: Add scheduler ordering RED test**

In `UiFrameSchedulerTests`, add a test proving `InheritedProperties` runs before `Style`, because inherited font/foreground values can affect style/template decisions.

- [ ] **Step 3: Add frame stats RED test**

In `FrameStatsTests`, add a test for the inherited-property counter.

- [ ] **Step 4: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~InheritedPropertyTreePropagationTests|FullyQualifiedName~UiFrameSchedulerTests|FullyQualifiedName~FrameStatsTests"
```

Expected: tests fail because no retained inherited-property phase exists.

- [ ] **Step 5: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\UI\Core\InheritedPropertyTreePropagationTests.cs tests\Cerneala.Tests\UI\Invalidation\UiFrameSchedulerTests.cs tests\Cerneala.Tests\UI\Invalidation\FrameStatsTests.cs
git commit -m "test: capture retained inherited property propagation"
```

---

### Task 2: Add Property Enumeration And Untyped Framework Setters

**Files:**
- Modify: `UI/Core/UiPropertyRegistry.cs`
- Modify: `UI/Core/UiObject.cs`

- [ ] **Step 1: Enumerate registered properties**

In `UiPropertyRegistry`, add safe enumeration helpers:

```csharp
public static IReadOnlyList<UiProperty> GetRegisteredProperties()
{
    return Properties.Values.OrderBy(property => property.Id).ToArray();
}

public static IReadOnlyList<UiProperty> GetPropertiesWithOptions(UiPropertyOptions options)
{
    return Properties.Values
        .Where(property => (property.Options & options) == options)
        .OrderBy(property => property.Id)
        .ToArray();
}
```

Do not expose mutable registry internals.

- [ ] **Step 2: Add internal untyped set/clear to `UiObject`**

Add internal framework helpers so inheritance can operate over `UiProperty`, not `UiProperty<T>`:

```csharp
internal object? SetValueUntyped(UiProperty property, object? value, UiPropertyValueSource source)
{
    ArgumentNullException.ThrowIfNull(property);
    object? oldValue = GetValue(property);
    object? coerced = property.CoerceUntyped(this, value);
    property.ValidateUntyped(coerced);
    propertyStore.SetValue(property, source, coerced);
    object? newValue = GetValue(property);
    if (!property.AreEqualUntyped(oldValue, newValue))
    {
        NotifyPropertyChangedUntyped(property, oldValue, newValue, GetValueSource(property));
    }

    return oldValue;
}

internal object? ClearValueUntyped(UiProperty property, UiPropertyValueSource source)
{
    ArgumentNullException.ThrowIfNull(property);
    object? oldValue = GetValue(property);
    propertyStore.ClearValue(property, source);
    object? newValue = GetValue(property);
    if (!property.AreEqualUntyped(oldValue, newValue))
    {
        NotifyPropertyChangedUntyped(property, oldValue, newValue, GetValueSource(property));
    }

    return oldValue;
}
```

Add a non-generic notification helper that preserves invalidation behavior:

```csharp
private void NotifyPropertyChangedUntyped(
    UiProperty property,
    object? oldValue,
    object? newValue,
    UiPropertyValueSource valueSource)
{
    UiPropertyChangedEventArgs args = new(this, property, oldValue, newValue, valueSource);
    OnPropertyChanged(args);

    UiPropertyOptions invalidationOptions = property.Options & (
        UiPropertyOptions.AffectsMeasure |
        UiPropertyOptions.AffectsArrange |
        UiPropertyOptions.AffectsRender |
        UiPropertyOptions.AffectsHitTest |
        UiPropertyOptions.AffectsStyle |
        UiPropertyOptions.AffectsInputVisual);
    if (invalidationOptions != UiPropertyOptions.None && this is IUiPropertyOwner owner)
    {
        owner.OnPropertyInvalidated(args, invalidationOptions);
    }
}
```

- [ ] **Step 3: Run existing property tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UiProperty|FullyQualifiedName~InheritedUiPropertyTests"
```

Expected: existing property precedence tests still pass.

- [ ] **Step 4: Commit property infrastructure**

```powershell
git add UI\Core\UiPropertyRegistry.cs UI\Core\UiObject.cs
git commit -m "feat: expose inherited property infrastructure"
```

---

### Task 3: Add Retained Inherited-Property Phase

**Files:**
- Create: `UI/Invalidation/InheritedPropertyQueue.cs`
- Create: `UI/Elements/InheritedPropertyPropagator.cs`
- Modify: `UI/Invalidation/InvalidationFlags.cs`
- Modify: `UI/Invalidation/FramePhase.cs`
- Modify: `UI/Invalidation/FramePhaseProcessors.cs`
- Modify: `UI/Invalidation/FrameStats.cs`
- Modify: `UI/Invalidation/UiFrameScheduler.cs`
- Modify: `UI/Elements/UIRoot.cs`
- Modify: `UI/Invalidation/DirtyPropagation.cs`

- [ ] **Step 1: Add invalidation flag and frame phase**

Add `Inherited = 1 << 10` to `InvalidationFlags`.

Add `InheritedProperties` before `Style` in `FramePhase`.

Add a delegate to `FramePhaseProcessors`:

```csharp
public Action<UIElement>? InheritedProperties { get; init; }
```

Route it in `Process(...)`.

- [ ] **Step 2: Add queue**

Create `UI/Invalidation/InheritedPropertyQueue.cs` mirroring the simple queue behavior of `StyleQueue`:

- reference-equality set
- insertion order list
- remove elements outside root before snapshot
- sort using `ElementQueueOrder.Sort(...)`

- [ ] **Step 3: Add propagator**

Create `UI/Elements/InheritedPropertyPropagator.cs`:

```csharp
using Cerneala.UI.Core;

namespace Cerneala.UI.Elements;

public sealed class InheritedPropertyPropagator
{
    public int PropagateFrom(UIElement root)
    {
        ArgumentNullException.ThrowIfNull(root);
        int changed = 0;
        foreach (UIElement child in root.VisualChildren)
        {
            changed += PropagateToSubtree(root, child);
        }

        return changed;
    }

    private static int PropagateToSubtree(UIElement parent, UIElement child)
    {
        int changed = ApplyInheritedValues(parent, child);
        foreach (UIElement grandchild in child.VisualChildren)
        {
            changed += PropagateToSubtree(child, grandchild);
        }

        return changed;
    }

    private static int ApplyInheritedValues(UIElement parent, UIElement child)
    {
        int changed = 0;
        foreach (UiProperty property in UiPropertyRegistry.GetPropertiesWithOptions(UiPropertyOptions.Inherits))
        {
            object? oldEffective = child.GetValue(property);
            UiPropertyValueSource parentSource = parent.GetValueSource(property);
            if (parentSource == UiPropertyValueSource.Default)
            {
                child.ClearValueUntyped(property, UiPropertyValueSource.Inherited);
            }
            else
            {
                child.SetValueUntyped(property, parent.GetValue(property), UiPropertyValueSource.Inherited);
            }

            if (!property.AreEqualUntyped(oldEffective, child.GetValue(property)))
            {
                changed++;
            }
        }

        return changed;
    }
}
```

Adjust access modifiers if `ClearValueUntyped`, `SetValueUntyped`, or `AreEqualUntyped` need internal visibility changes.

- [ ] **Step 4: Wire queue into scheduler**

Add `InheritedPropertyQueue` to the scheduler constructor and `HasWork`.

Process inherited queue before style:

```csharp
ProcessInheritedProperties(processors, stats);
ProcessStyle(processors, stats);
```

Clear only the inherited flag after processing.

- [ ] **Step 5: Wire root ownership**

In `UIRoot`, create and expose:

```csharp
public InheritedPropertyQueue InheritedPropertyQueue { get; }
public InheritedPropertyPropagator InheritedPropertyPropagator { get; }
```

Initialize them before `Scheduler`.

Add to `CreatePhaseProcessors()`:

```csharp
InheritedProperties = element => InheritedPropertyPropagator.PropagateFrom(element),
```

- [ ] **Step 6: Map inheritable property changes to queue work**

In `DirtyPropagation.GetEffectiveFlags(...)`, if `request.SourceProperty?.Options.HasFlag(UiPropertyOptions.Inherits) == true`, add:

```csharp
effective |= InvalidationFlags.Inherited | InvalidationFlags.Subtree;
```

In `MarkAndQueue(...)`, enqueue `InheritedPropertyQueue` when `flags.HasFlag(InvalidationFlags.Inherited)`.

- [ ] **Step 7: Run inherited tests and verify still RED only for inheriting controls**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~InheritedPropertyTreePropagationTests|FullyQualifiedName~UiFrameSchedulerTests|FullyQualifiedName~FrameStatsTests"
```

Expected: infrastructure compiles; tests that depend on `Control.Foreground/FontFamily/FontSize` inheriting may still fail until Task 4.

- [ ] **Step 8: Commit retained inherited phase**

```powershell
git add UI\Invalidation UI\Elements\InheritedPropertyPropagator.cs UI\Elements\UIRoot.cs
git commit -m "feat: add retained inherited property phase"
```

---

### Task 4: Mark MVP Control Text Properties As Inheritable

**Files:**
- Modify: `UI/Controls/Control.cs`

- [ ] **Step 1: Update metadata options**

Change `ForegroundProperty`, `FontFamilyProperty`, and `FontSizeProperty` metadata to include `UiPropertyOptions.Inherits`.

Example:

```csharp
new UiPropertyMetadata<Color>(
    Color.Black,
    UiPropertyOptions.Inherits | UiPropertyOptions.AffectsRender)
```

For font properties, keep measure/render invalidation:

```csharp
UiPropertyOptions.Inherits | UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender
```

- [ ] **Step 2: Do not make visual container properties inherit**

Do not add inheritance to:

- `Background`
- `BorderBrush`
- `BorderThickness`
- `Padding`
- `Template`

These are local/control visual concerns, not ambient text state.

- [ ] **Step 3: Run inherited propagation tests and verify GREEN**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~InheritedPropertyTreePropagationTests|FullyQualifiedName~TextBlockTests|FullyQualifiedName~ButtonContentArchitectureTests"
```

Expected: text controls inherit ambient font/foreground where no local value is set.

- [ ] **Step 4: Commit inheritable control metadata**

```powershell
git add UI\Controls\Control.cs
git commit -m "feat: make core text properties inherit"
```

---

### Task 5: Invalidate Inheritance On Tree Mutation

**Files:**
- Modify: `UI/Elements/UIElementCollection.cs`
- Modify: existing visual mutation invalidation helper if present
- Modify: `tests/Cerneala.Tests/UI/Elements/UIElementCollectionInvalidationTests.cs`
- Modify: `tests/Cerneala.Tests/UI/Core/InheritedPropertyTreePropagationTests.cs`

- [ ] **Step 1: Add tests for late attach and reparent**

Extend inherited propagation tests:

- adding a child after first frame inherits parent values on the next frame
- removing a child clears inherited source or detaches it from further inherited updates
- reparenting from parent A to parent B updates inherited values

- [ ] **Step 2: Enqueue inherited work on add/remove**

When a visual child is added to an attached owner, invalidate:

```csharp
owner.Invalidate(InvalidationFlags.Inherited | InvalidationFlags.Subtree, "Visual child added");
```

When a visual child is removed, ensure stale root-owned inherited work does not process detached nodes. Queue cleanup is handled by `ElementQueueOrder.RemoveElementsOutsideRoot(...)`; if stale inherited source remains on detached elements, document and test the desired behavior.

Recommended MVP: detached elements keep their current effective values but no longer receive future inherited updates until attached elsewhere.

- [ ] **Step 3: Run tree mutation tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~InheritedPropertyTreePropagationTests|FullyQualifiedName~UIElementCollectionInvalidationTests"
```

Expected: late attach/reparent scenarios pass.

- [ ] **Step 4: Commit tree mutation inheritance invalidation**

```powershell
git add UI\Elements\UIElementCollection.cs tests\Cerneala.Tests\UI\Core\InheritedPropertyTreePropagationTests.cs tests\Cerneala.Tests\UI\Elements\UIElementCollectionInvalidationTests.cs
git commit -m "fix: invalidate inherited values on tree mutation"
```

---

### Task 6: Full Verification And Status Update

**Files:**
- Modify: `ROADMAPv2.md`
- Modify: `ROADMAPv2_AUDIT.md`
- Modify: `AUDIT_FIX_PLAN.md` if tracked

- [ ] **Step 1: Run full tests**

```powershell
dotnet test Cerneala.slnx
```

Expected: full suite passes.

- [ ] **Step 2: Update project memory**

Mark tree-level inherited property propagation as implemented. Keep logical-scope inheritance and flow direction deferred unless implemented.

- [ ] **Step 3: Commit status update**

```powershell
git add ROADMAPv2.md ROADMAPv2_AUDIT.md AUDIT_FIX_PLAN.md
git commit -m "docs: record inherited property propagation"
```

## Stop Conditions

- [ ] Stop if inheritance requires reflection-based property scanning per element per getter. Inheritance must remain explicit and frame-owned.
- [ ] Stop if adding logical-tree inheritance forces a rewrite of controls. Use visual-tree inheritance for MVP and record the limitation.
- [ ] Stop if the inherited phase starts processing the whole tree every unchanged frame. Unchanged frames must remain no-work frames.
