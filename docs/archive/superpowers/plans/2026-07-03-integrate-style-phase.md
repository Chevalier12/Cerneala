# Integrate Style Phase Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make style invalidation scheduler-owned so pseudo-class and theme changes are applied during `Update` before layout/render work.

**Architecture:** Add a retained `StyleQueue` beside the layout/render/hit-test queues, process it as `FramePhase.Style` before measure/arrange/render, and let `UIRoot` own the active `StyleSheet`, `ThemeProvider`, and `StyleApplicator`. Property changes that affect style enqueue `InvalidationFlags.Style`; the style phase applies setters, and those setters enqueue their own measure/render/hit-test work through normal property metadata.

**Tech Stack:** C#/.NET 8, xUnit, Cerneala retained UI runtime, existing `UiProperty<T>`, `DirtyPropagation`, `UiFrameScheduler`, `StyleApplicator`, `StyleSheet`, `ThemeProvider`, and `UiHost`.

---

## File Structure

- Create: `UI/Invalidation/StyleQueue.cs`
  - Retained queue of elements needing style recomputation.
  - Mirrors `RenderQueue`/`HitTestQueue` shape and uses `ElementQueueOrder.Sort(...)`.
- Create: `UI/Styling/StyleProcessor.cs`
  - Applies the root-owned stylesheet/theme to an element during `FramePhase.Style`.
- Create: `UI/Styling/PseudoClassRegistry.cs`
  - Explicit `UiProperty -> PseudoClass` mapping for style-affecting pseudo-state properties.
  - Replaces string property-name checks in `StyleInvalidation`.
- Modify: `UI/Elements/UIRoot.cs`
  - Owns `StyleQueue`, `StyleApplicator`, active `StyleSheet`, active `ThemeProvider`, and `StyleProcessor`.
  - Exposes `SetStyleSheet(...)` and `SetThemeProvider(...)`.
  - Subscribes to `ThemeProvider.ThemeChanged`.
  - Provides style processor to `UiFrameScheduler`.
- Modify: `UI/Elements/UIElement.cs`
  - Maps `UiPropertyOptions.AffectsStyle` to `InvalidationFlags.Style`, not `Render`.
  - Adds `AffectsStyle` to built-in pseudo-state properties.
- Modify: `UI/Elements/UIElementCollection.cs`
  - Adds `InvalidationFlags.Style` to added visual subtree invalidation so newly attached elements are styled during update.
- Modify: `UI/Invalidation/DirtyPropagation.cs`
  - Accepts `StyleQueue`.
  - Queues style work when effective flags include `Style`.
  - Propagates `Style|Subtree` to descendants.
- Modify: `UI/Invalidation/FramePhaseProcessors.cs`
  - Adds `Action<UIElement>? Style`.
- Modify: `UI/Invalidation/FrameStats.cs`
  - Adds `StyledElements`, includes it in `HasWork`, and counts `FramePhase.Style`.
- Modify: `UI/Invalidation/UiFrameScheduler.cs`
  - Owns/processes style queue before measure.
  - Includes style queue in `HasWork`.
  - Clears `Style` as concrete work, not as a passive specialized flag.
- Modify: `UI/Controls/Primitives/ButtonBase.cs`
  - Adds `AffectsStyle` to `IsPressedProperty`.
- Modify: `UI/Controls/ListBoxItem.cs`
  - Adds `AffectsStyle` to `IsSelectedProperty`.
- Modify: `UI/Controls/TabItem.cs`
  - Adds `AffectsStyle` to `IsSelectedProperty`.
- Modify: `UI/Styling/StyleInvalidation.cs`
  - Removes `property.Name == "IsPressed"` / `"IsSelected"`.
  - Uses `PseudoClassRegistry`.
  - Keeps current synchronous manual behavior for detached/manual tests.
- Create: `tests/Cerneala.Tests/UI/Styling/StyleSchedulerIntegrationTests.cs`
  - Scheduler-owned style phase integration tests.
- Modify: `tests/Cerneala.Tests/UI/Elements/UIElementInvalidationTests.cs`
  - Update `AffectsStyle` expectations to style queue, not render queue.
- Modify: `tests/Cerneala.Tests/UI/Invalidation/UiFrameSchedulerTests.cs`
  - Add style phase ordering and failed-style-phase retention coverage.
- Modify: `tests/Cerneala.Tests/UI/Styling/StyleInvalidationTests.cs`
  - Update string-name pseudo-class coverage to registry-based coverage.
- Modify: `tests/Cerneala.Tests/UI/Styling/PseudoClassTests.cs`
  - Add registry coverage for pressed/selected pseudo properties.
- Modify: `AUDIT_FIX_PLAN.md`
  - Link this detailed plan after it is written.
- Modify: `ROADMAPv2_AUDIT.md`
  - Add implementation note only after execution and full verification pass.

## Important Existing Behavior

Current broken `AffectsStyle` mapping:

```csharp
if (options.HasFlag(UiPropertyOptions.AffectsStyle))
{
    flags |= InvalidationFlags.Render;
}
```

That makes style-affecting properties look like render-only work. No style recomputation happens unless a caller manually uses `StyleInvalidation.Track(...)`.

Current manual style invalidation path:

```csharp
private static bool AffectsPseudoClass(UiProperty property)
{
    return ReferenceEquals(property, UIElement.IsPointerOverProperty) ||
        ReferenceEquals(property, UIElement.IsKeyboardFocusedProperty) ||
        ReferenceEquals(property, UIElement.IsKeyboardFocusWithinProperty) ||
        ReferenceEquals(property, UIElement.IsEnabledProperty) ||
        property.Name == "IsPressed" ||
        property.Name == "IsSelected" ||
        property.Options.HasFlag(UiPropertyOptions.AffectsStyle);
}
```

This is not scheduler-owned and uses string property-name detection. The target behavior is:

```csharp
// Property metadata produces scheduler-owned style work.
child.SetValue(styleAffectingProperty, value);
Assert.Contains(child, root.StyleQueue.Snapshot());

// Frame order:
// Style phase applies matching setters.
// Setter property invalidation queues measure/render/hit-test.
// Measure/arrange/render/hit-test phases process that work in the same Update.
root.ProcessFrame();
```

The plan intentionally does not implement a full style service framework, cascading inheritance, or stylesheet mutation notifications. It adds a root-owned style scope and a scheduler phase, which is the smallest fix for the audit finding.

---

### Task 1: Add RED Tests For Scheduler-Owned Style Work

**Files:**
- Create: `tests/Cerneala.Tests/UI/Styling/StyleSchedulerIntegrationTests.cs`
- Modify: `tests/Cerneala.Tests/UI/Elements/UIElementInvalidationTests.cs`
- Modify: `tests/Cerneala.Tests/UI/Invalidation/UiFrameSchedulerTests.cs`

- [ ] **Step 1: Create integration tests for root-owned style phase**

Create `tests/Cerneala.Tests/UI/Styling/StyleSchedulerIntegrationTests.cs`:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Styling;

namespace Cerneala.Tests.UI.Styling;

public sealed class StyleSchedulerIntegrationTests
{
    [Fact]
    public void StyleSheetAppliedDuringStylePhaseBeforeRender()
    {
        UIRoot root = new(100, 100);
        Button button = new();
        StyleSheet sheet = new StyleSheet().Add(new StyleRule(StyleSelector.ForType<Button>())
            .Add(new Setter<Color>(Control.BackgroundProperty, Color.White)));
        root.SetStyleSheet(sheet);
        root.VisualChildren.Add(button);

        FrameStats stats = root.ProcessFrame();

        Assert.Equal(Color.White, button.Background);
        Assert.True(stats.StyledElements > 0);
        Assert.True(stats.RenderedElements > 0);
        Assert.False(button.DirtyState.Has(InvalidationFlags.Style));
        Assert.False(root.Scheduler.HasWork);
    }

    [Fact]
    public void PseudoClassChangeIsAppliedDuringNextStylePhase()
    {
        UIRoot root = new(100, 100);
        Button button = new();
        StyleSheet sheet = new StyleSheet().Add(new StyleRule(
                StyleSelector.ForType<Button>(),
                new VisualStateRule(PseudoClass.Hover))
            .Add(new Setter<Color>(Control.BackgroundProperty, Color.Black)));
        root.SetStyleSheet(sheet);
        root.VisualChildren.Add(button);
        root.ProcessFrame();

        button.IsPointerOver = true;

        Assert.Contains(button, root.StyleQueue.Snapshot());

        FrameStats stats = root.ProcessFrame();

        Assert.Equal(Color.Black, button.Background);
        Assert.True(stats.StyledElements > 0);
        Assert.True(stats.RenderedElements > 0);
        Assert.False(button.DirtyState.Has(InvalidationFlags.Style));
    }

    [Fact]
    public void ThemeChangeQueuesStyleForAttachedTree()
    {
        ThemeKey<Color> key = new("Accent");
        ThemeProvider provider = new(new Theme().Set(key, Color.White));
        UIRoot root = new(100, 100);
        Button button = new();
        StyleSheet sheet = new StyleSheet().Add(new StyleRule(StyleSelector.ForType<Button>())
            .Add(new Setter<Color>(Control.BackgroundProperty, new ThemeResource<Color>(key))));
        root.SetThemeProvider(provider);
        root.SetStyleSheet(sheet);
        root.VisualChildren.Add(button);
        root.ProcessFrame();

        provider.Theme = new Theme().Set(key, Color.Black);

        Assert.Contains(button, root.StyleQueue.Snapshot());

        FrameStats stats = root.ProcessFrame();

        Assert.Equal(Color.Black, button.Background);
        Assert.True(stats.StyledElements > 0);
        Assert.True(stats.RenderedElements > 0);
    }

    [Fact]
    public void StylePhaseQueuesMeasureWorkForMeasureAffectingSetterInSameFrame()
    {
        UIRoot root = new(100, 100);
        Button button = new();
        StyleSheet sheet = new StyleSheet().Add(new StyleRule(StyleSelector.ForType<Button>())
            .Add(new Setter<Thickness>(Control.PaddingProperty, new Thickness(4))));
        root.SetStyleSheet(sheet);
        root.VisualChildren.Add(button);

        FrameStats stats = root.ProcessFrame();

        Assert.Equal(new Thickness(4), button.Padding);
        Assert.True(stats.StyledElements > 0);
        Assert.True(stats.MeasuredElements > 0);
        Assert.True(stats.ArrangedElements > 0);
        Assert.False(root.Scheduler.HasWork);
    }

    [Fact]
    public void StylePropertyInvalidationDoesNotQueueRenderUntilStyleAppliesASetter()
    {
        UiProperty<int> property = UiProperty<int>.Register(
            $"{nameof(StyleSchedulerIntegrationTests)}_{Guid.NewGuid():N}",
            typeof(StyleSchedulerIntegrationTests),
            new UiPropertyMetadata<int>(0, UiPropertyOptions.AffectsStyle));
        UIRoot root = new(100, 100);
        UIElement element = new();
        root.VisualChildren.Add(element);
        root.ProcessFrame();

        element.SetValue(property, 1);

        Assert.Contains(element, root.StyleQueue.Snapshot());
        Assert.DoesNotContain(element, root.RenderQueue.Snapshot());
        Assert.True(element.DirtyState.Has(InvalidationFlags.Style));
    }
}
```

- [ ] **Step 2: Update existing style invalidation expectation to style queue**

In `tests/Cerneala.Tests/UI/Elements/UIElementInvalidationTests.cs`, replace `StylePropertyInvalidationSchedulesFrameWorkAndClearsAfterFrame()` with:

```csharp
[Fact]
public void StylePropertyInvalidationQueuesStyleWorkAndClearsAfterFrame()
{
    UiProperty<int> property = UiProperty<int>.Register(
        UniqueName(),
        typeof(UIElementInvalidationTests),
        new UiPropertyMetadata<int>(0, UiPropertyOptions.AffectsStyle));
    UIRoot root = new();
    UIElement child = new();
    root.VisualChildren.Add(child);
    root.ProcessFrame();

    child.SetValue(property, 1);

    Assert.Contains(child, root.StyleQueue.Snapshot());
    Assert.DoesNotContain(child, root.RenderQueue.Snapshot());

    root.ProcessFrame();

    Assert.False(child.DirtyState.IsDirty);
}
```

- [ ] **Step 3: Add style phase order and failure tests**

In `tests/Cerneala.Tests/UI/Invalidation/UiFrameSchedulerTests.cs`, add:

```csharp
[Fact]
public void ProcessesStyleBeforeLayoutAndRender()
{
    UIRoot root = new();
    UIElement child = new();
    root.VisualChildren.Add(child);
    root.ProcessFrame();
    List<FramePhase> phases = [];
    child.Invalidate(InvalidationFlags.Style | InvalidationFlags.Measure | InvalidationFlags.HitTest, "style and layout");

    root.ProcessFrame(new FramePhaseProcessors
    {
        Style = _ => phases.Add(FramePhase.Style),
        Measure = _ => phases.Add(FramePhase.Measure),
        Arrange = _ => phases.Add(FramePhase.Arrange),
        RenderCache = _ => phases.Add(FramePhase.RenderCache),
        HitTest = _ => phases.Add(FramePhase.HitTest)
    });

    Assert.Equal(
        [FramePhase.Style, FramePhase.Measure, FramePhase.Measure, FramePhase.Arrange, FramePhase.Arrange, FramePhase.RenderCache, FramePhase.HitTest],
        phases);
}

[Fact]
public void FailedStylePhaseKeepsDirtyFlagsAndQueuedWork()
{
    UIRoot root = new();
    UIElement child = new();
    root.VisualChildren.Add(child);
    root.ProcessFrame();
    child.Invalidate(InvalidationFlags.Style, "style");

    Assert.Throws<InvalidOperationException>(() => root.ProcessFrame(new FramePhaseProcessors
    {
        Style = _ => throw new InvalidOperationException("boom")
    }));

    Assert.True(child.DirtyState.Has(InvalidationFlags.Style));
    Assert.Equal(1, root.StyleQueue.Count);
}
```

- [ ] **Step 4: Run RED tests**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~StyleSchedulerIntegrationTests|FullyQualifiedName~StylePropertyInvalidationQueuesStyleWorkAndClearsAfterFrame|FullyQualifiedName~ProcessesStyleBeforeLayoutAndRender|FullyQualifiedName~FailedStylePhaseKeepsDirtyFlagsAndQueuedWork"
```

Expected: build fails because `UIRoot.StyleQueue`, `UIRoot.SetStyleSheet(...)`, `UIRoot.SetThemeProvider(...)`, `FrameStats.StyledElements`, and `FramePhaseProcessors.Style` do not exist. This is a valid RED for the new scheduler contract.

- [ ] **Step 5: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\UI\Styling\StyleSchedulerIntegrationTests.cs tests\Cerneala.Tests\UI\Elements\UIElementInvalidationTests.cs tests\Cerneala.Tests\UI\Invalidation\UiFrameSchedulerTests.cs
git commit -m "test: capture scheduler-owned style phase gaps"
```

---

### Task 2: Add Style Queue And Scheduler Phase

**Files:**
- Create: `UI/Invalidation/StyleQueue.cs`
- Modify: `UI/Invalidation/DirtyPropagation.cs`
- Modify: `UI/Invalidation/FramePhaseProcessors.cs`
- Modify: `UI/Invalidation/FrameStats.cs`
- Modify: `UI/Invalidation/UiFrameScheduler.cs`
- Modify: `UI/Elements/UIRoot.cs`

- [ ] **Step 1: Create `StyleQueue`**

Create `UI/Invalidation/StyleQueue.cs`:

```csharp
using Cerneala.UI.Elements;

namespace Cerneala.UI.Invalidation;

public sealed class StyleQueue
{
    private readonly UIRoot root;
    private readonly HashSet<UIElement> elements = new(ReferenceEqualityComparer.Instance);
    private readonly List<UIElement> order = [];

    public StyleQueue(UIRoot root)
    {
        this.root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public int Count => elements.Count;

    public bool HasWork => elements.Count > 0;

    public void Enqueue(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (elements.Add(element))
        {
            order.Add(element);
        }
    }

    public IReadOnlyList<UIElement> Snapshot()
    {
        ElementQueueOrder.RemoveElementsOutsideRoot(root, elements, order);
        return ElementQueueOrder.Sort(root, order.Where(elements.Contains));
    }

    public void Remove(UIElement element)
    {
        if (elements.Remove(element))
        {
            order.RemoveAll(candidate => ReferenceEquals(candidate, element));
        }
    }
}
```

- [ ] **Step 2: Wire style queue through dirty propagation**

Change `DirtyPropagation.Propagate(...)` signature in `UI/Invalidation/DirtyPropagation.cs` to include `StyleQueue styleQueue` between `LayoutQueue layoutQueue` and `RenderQueue renderQueue`:

```csharp
public void Propagate(
    InvalidationRequest request,
    UIRoot root,
    LayoutQueue layoutQueue,
    StyleQueue styleQueue,
    RenderQueue renderQueue,
    HitTestQueue hitTestQueue,
    InvalidationTrace trace)
```

Add null checking:

```csharp
ArgumentNullException.ThrowIfNull(styleQueue);
```

Change `MarkAndQueue(...)` signature to accept `StyleQueue styleQueue`:

```csharp
private static void MarkAndQueue(
    UIElement element,
    InvalidationFlags flags,
    LayoutQueue layoutQueue,
    StyleQueue styleQueue,
    RenderQueue renderQueue,
    HitTestQueue hitTestQueue,
    InvalidationTrace trace,
    string reason)
```

Update every call to `MarkAndQueue(...)` in the file to pass `styleQueue`.

Inside `MarkAndQueue(...)`, after `element.DirtyState.Mark(flags);` and before measure queueing, add:

```csharp
if (flags.HasFlag(InvalidationFlags.Style))
{
    styleQueue.Enqueue(element);
    trace.RecordQueue(element, InvalidationFlags.Style, reason);
}
```

Keep `GetEffectiveFlags(...)` unchanged for `Style`: style work is concrete work and should not automatically become render work.

- [ ] **Step 3: Add style processor hook**

In `UI/Invalidation/FramePhaseProcessors.cs`, add:

```csharp
public Action<UIElement>? Style { get; init; }
```

Update `Process(...)`:

```csharp
case FramePhase.Style:
    Style?.Invoke(element);
    break;
```

- [ ] **Step 4: Add styled element stats**

In `UI/Invalidation/FrameStats.cs`, add:

```csharp
public int StyledElements { get; private set; }
```

Update `HasWork`:

```csharp
public bool HasWork =>
    StyledElements > 0 ||
    MeasuredElements > 0 ||
    ArrangedElements > 0 ||
    RenderedElements > 0 ||
    HitTestElements > 0;
```

Update `Count(...)`:

```csharp
case FramePhase.Style:
    StyledElements++;
    break;
```

- [ ] **Step 5: Process style before measure**

In `UI/Invalidation/UiFrameScheduler.cs`, add the field:

```csharp
private readonly StyleQueue styleQueue;
```

Change the constructor to:

```csharp
public UiFrameScheduler(
    LayoutQueue layoutQueue,
    StyleQueue styleQueue,
    RenderQueue renderQueue,
    HitTestQueue hitTestQueue,
    InvalidationTrace? trace = null)
{
    this.layoutQueue = layoutQueue ?? throw new ArgumentNullException(nameof(layoutQueue));
    this.styleQueue = styleQueue ?? throw new ArgumentNullException(nameof(styleQueue));
    this.renderQueue = renderQueue ?? throw new ArgumentNullException(nameof(renderQueue));
    this.hitTestQueue = hitTestQueue ?? throw new ArgumentNullException(nameof(hitTestQueue));
    this.trace = trace ?? InvalidationTrace.Disabled;
}
```

Update `HasWork`:

```csharp
public bool HasWork => styleQueue.HasWork || layoutQueue.HasWork || renderQueue.HasWork || hitTestQueue.HasWork;
```

Add `InvalidationFlags.Style` to `ConcreteWorkFlags`:

```csharp
private const InvalidationFlags ConcreteWorkFlags =
    InvalidationFlags.Style |
    InvalidationFlags.Measure |
    InvalidationFlags.Arrange |
    InvalidationFlags.Render |
    InvalidationFlags.HitTest;
```

Remove `InvalidationFlags.Style` from `SpecializedWorkFlags`:

```csharp
private const InvalidationFlags SpecializedWorkFlags =
    InvalidationFlags.Text |
    InvalidationFlags.Image |
    InvalidationFlags.Resource |
    InvalidationFlags.InputVisual |
    InvalidationFlags.Subtree;
```

Call `ProcessStyle(...)` before `ProcessMeasure(...)`:

```csharp
ProcessStyle(processors, stats);
ProcessMeasure(processors, stats);
```

Add:

```csharp
private void ProcessStyle(FramePhaseProcessors processors, FrameStats stats)
{
    IReadOnlyList<Elements.UIElement> snapshot = styleQueue.Snapshot();
    foreach (Elements.UIElement element in snapshot)
    {
        processors.Process(FramePhase.Style, element);
        InvalidationFlags cleared = ClearProcessedFlags(element, InvalidationFlags.Style);
        styleQueue.Remove(element);
        stats.Count(FramePhase.Style);
        trace.RecordPhase(FramePhase.Style, element, InvalidationFlags.Style);
        trace.RecordClear(element, cleared);
    }

    trace.RecordPhaseSummary(FramePhase.Style, snapshot.Count);
}
```

- [ ] **Step 6: Add `StyleQueue` to root construction**

In `UI/Elements/UIRoot.cs`, add property:

```csharp
public StyleQueue StyleQueue { get; }
```

In the constructor, after `LayoutQueue = new LayoutQueue(this);`, add:

```csharp
StyleQueue = new StyleQueue(this);
```

Change scheduler construction:

```csharp
Scheduler = new UiFrameScheduler(LayoutQueue, StyleQueue, RenderQueue, HitTestQueue, Trace);
```

Change `Invalidate(...)` propagation call:

```csharp
DirtyPropagation.Default.Propagate(request, this, LayoutQueue, StyleQueue, RenderQueue, HitTestQueue, Trace);
```

- [ ] **Step 7: Run queue/scheduler tests**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~StylePropertyInvalidationQueuesStyleWorkAndClearsAfterFrame|FullyQualifiedName~ProcessesStyleBeforeLayoutAndRender|FullyQualifiedName~FailedStylePhaseKeepsDirtyFlagsAndQueuedWork|FullyQualifiedName~UiFrameSchedulerTests"
```

Expected: style queue and scheduler tests pass. The integration tests that need root-owned stylesheet APIs still fail until Task 3.

- [ ] **Step 8: Commit style queue and scheduler phase**

```powershell
git add UI\Invalidation\StyleQueue.cs UI\Invalidation\DirtyPropagation.cs UI\Invalidation\FramePhaseProcessors.cs UI\Invalidation\FrameStats.cs UI\Invalidation\UiFrameScheduler.cs UI\Elements\UIRoot.cs
git commit -m "feat: add scheduler-owned style phase"
```

---

### Task 3: Add Root-Owned Style Scope

**Files:**
- Create: `UI/Styling/StyleProcessor.cs`
- Modify: `UI/Elements/UIRoot.cs`
- Modify: `UI/Elements/UIElementCollection.cs`

- [ ] **Step 1: Create style processor**

Create `UI/Styling/StyleProcessor.cs`:

```csharp
using Cerneala.UI.Elements;

namespace Cerneala.UI.Styling;

public sealed class StyleProcessor
{
    private readonly StyleApplicator applicator;
    private readonly Func<StyleSheet?> styleSheetProvider;
    private readonly Func<ThemeProvider?> themeProviderProvider;

    public StyleProcessor(
        StyleApplicator applicator,
        Func<StyleSheet?> styleSheetProvider,
        Func<ThemeProvider?> themeProviderProvider)
    {
        this.applicator = applicator ?? throw new ArgumentNullException(nameof(applicator));
        this.styleSheetProvider = styleSheetProvider ?? throw new ArgumentNullException(nameof(styleSheetProvider));
        this.themeProviderProvider = themeProviderProvider ?? throw new ArgumentNullException(nameof(themeProviderProvider));
    }

    public void Process(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        StyleSheet? styleSheet = styleSheetProvider();
        if (styleSheet is null)
        {
            return;
        }

        applicator.Apply(element, styleSheet, themeProviderProvider());
    }
}
```

- [ ] **Step 2: Add root stylesheet/theme ownership**

In `UI/Elements/UIRoot.cs`, add `using Cerneala.UI.Styling;`.

Add fields:

```csharp
private readonly StyleApplicator styleApplicator;
private ThemeProvider? themeProvider;
```

Add properties:

```csharp
public StyleSheet? StyleSheet { get; private set; }

public ThemeProvider? ThemeProvider => themeProvider;

public StyleProcessor StyleProcessor { get; }
```

In the constructor, after `RenderQueueProcessor = ...`, add:

```csharp
styleApplicator = new StyleApplicator();
StyleProcessor = new StyleProcessor(styleApplicator, () => StyleSheet, () => themeProvider);
```

Add methods:

```csharp
public void SetStyleSheet(StyleSheet? styleSheet)
{
    if (ReferenceEquals(StyleSheet, styleSheet))
    {
        return;
    }

    StyleSheet = styleSheet;
    Invalidate(InvalidationFlags.Style | InvalidationFlags.Subtree, "Style sheet changed");
}

public void SetThemeProvider(ThemeProvider? provider)
{
    if (ReferenceEquals(themeProvider, provider))
    {
        return;
    }

    if (themeProvider is not null)
    {
        themeProvider.ThemeChanged -= OnThemeChanged;
    }

    themeProvider = provider;
    if (themeProvider is not null)
    {
        themeProvider.ThemeChanged += OnThemeChanged;
    }

    Invalidate(InvalidationFlags.Style | InvalidationFlags.Subtree, "Theme provider changed");
}

private void OnThemeChanged(object? sender, ThemeChangedEventArgs args)
{
    Invalidate(InvalidationFlags.Style | InvalidationFlags.Subtree, "Theme changed");
}
```

Update `CreatePhaseProcessors()`:

```csharp
return new FramePhaseProcessors
{
    Style = StyleProcessor.Process,
    Measure = layoutProcessors.Measure,
    Arrange = layoutProcessors.Arrange,
    RenderCache = RenderQueueProcessor.Process
};
```

- [ ] **Step 3: Queue style for newly attached visual subtrees**

In `UI/Elements/UIElementCollection.cs`, update the add-child subtree invalidation flags in `InvalidateForVisualChildMutation(...)`.

Keep owner invalidation flags unchanged:

```csharp
InvalidationFlags flags =
    InvalidationFlags.Measure |
    InvalidationFlags.Arrange |
    InvalidationFlags.Render |
    InvalidationFlags.HitTest;
```

Change the add subtree invalidate call to:

```csharp
if (kind == ElementTreeChangeKind.Added && child.Root is not null)
{
    child.Invalidate(flags | InvalidationFlags.Style | InvalidationFlags.Subtree, reason);
}
```

- [ ] **Step 4: Run root style scope integration tests**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~StyleSchedulerIntegrationTests"
```

Expected: `StyleSheetAppliedDuringStylePhaseBeforeRender`, `ThemeChangeQueuesStyleForAttachedTree`, and `StylePhaseQueuesMeasureWorkForMeasureAffectingSetterInSameFrame` pass. `PseudoClassChangeIsAppliedDuringNextStylePhase` may still fail until Task 4 adds pseudo-state metadata.

- [ ] **Step 5: Commit root style scope**

```powershell
git add UI\Styling\StyleProcessor.cs UI\Elements\UIRoot.cs UI\Elements\UIElementCollection.cs
git commit -m "feat: add root-owned style scope"
```

---

### Task 4: Map Style-Affecting Properties To Style Work

**Files:**
- Modify: `UI/Elements/UIElement.cs`
- Modify: `UI/Controls/Primitives/ButtonBase.cs`
- Modify: `UI/Controls/ListBoxItem.cs`
- Modify: `UI/Controls/TabItem.cs`

- [ ] **Step 1: Map `AffectsStyle` to `InvalidationFlags.Style`**

In `UI/Elements/UIElement.cs`, replace:

```csharp
if (options.HasFlag(UiPropertyOptions.AffectsStyle))
{
    flags |= InvalidationFlags.Render;
}
```

with:

```csharp
if (options.HasFlag(UiPropertyOptions.AffectsStyle))
{
    flags |= InvalidationFlags.Style;
}
```

Do not add `IncrementRenderVersion()` for `AffectsStyle` alone. Style setters will change real visual/layout properties and trigger their own version increments.

- [ ] **Step 2: Mark built-in pseudo-state properties as style-affecting**

In `UI/Elements/UIElement.cs`, update these metadata options:

```csharp
new UiPropertyMetadata<bool>(true, UiPropertyOptions.AffectsHitTest | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsStyle)
```

for `IsEnabledProperty`.

```csharp
new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsStyle)
```

for `IsPointerOverProperty`, `IsKeyboardFocusedProperty`, and `IsKeyboardFocusWithinProperty`.

In `UI/Controls/Primitives/ButtonBase.cs`, update `IsPressedProperty` metadata:

```csharp
new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsStyle)
```

In `UI/Controls/ListBoxItem.cs`, update `IsSelectedProperty` metadata:

```csharp
new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsStyle)
```

In `UI/Controls/TabItem.cs`, update `IsSelectedProperty` metadata:

```csharp
new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsStyle)
```

- [ ] **Step 3: Run pseudo-state scheduler tests**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~PseudoClassChangeIsAppliedDuringNextStylePhase|FullyQualifiedName~StylePropertyInvalidationDoesNotQueueRenderUntilStyleAppliesASetter|FullyQualifiedName~UIElementInvalidationTests"
```

Expected: all filtered tests pass.

- [ ] **Step 4: Commit style property mapping**

```powershell
git add UI\Elements\UIElement.cs UI\Controls\Primitives\ButtonBase.cs UI\Controls\ListBoxItem.cs UI\Controls\TabItem.cs
git commit -m "fix: route style-affecting properties through style phase"
```

---

### Task 5: Replace String Pseudo-Class Detection With Explicit Registry

**Files:**
- Create: `UI/Styling/PseudoClassRegistry.cs`
- Modify: `UI/Styling/StyleInvalidation.cs`
- Modify: `tests/Cerneala.Tests/UI/Styling/PseudoClassTests.cs`
- Modify: `tests/Cerneala.Tests/UI/Styling/StyleInvalidationTests.cs`

- [ ] **Step 1: Add registry tests**

In `tests/Cerneala.Tests/UI/Styling/PseudoClassTests.cs`, add `using Cerneala.UI.Controls.Primitives;`, `using Cerneala.UI.Core;`, and:

```csharp
[Fact]
public void RegistryReportsBuiltInPseudoClassProperties()
{
    PseudoClassRegistry registry = PseudoClassRegistry.Default;

    Assert.True(registry.AffectsPseudoClass(UIElement.IsPointerOverProperty));
    Assert.True(registry.AffectsPseudoClass(UIElement.IsKeyboardFocusedProperty));
    Assert.True(registry.AffectsPseudoClass(UIElement.IsKeyboardFocusWithinProperty));
    Assert.True(registry.AffectsPseudoClass(UIElement.IsEnabledProperty));
    Assert.True(registry.AffectsPseudoClass(ButtonBase.IsPressedProperty));
    Assert.True(registry.AffectsPseudoClass(ListBoxItem.IsSelectedProperty));
    Assert.True(registry.AffectsPseudoClass(TabItem.IsSelectedProperty));
}

[Fact]
public void RegistryCanRegisterCustomPseudoClassProperty()
{
    UiProperty<bool> selectedProperty = UiProperty<bool>.Register(
        $"{nameof(PseudoClassTests)}_{Guid.NewGuid():N}",
        typeof(PseudoClassTests),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsStyle));
    PseudoClassRegistry registry = new();

    registry.Register(selectedProperty, PseudoClass.Selected);

    Assert.True(registry.AffectsPseudoClass(selectedProperty));
    Assert.Equal(PseudoClass.Selected, registry.GetPseudoClasses(selectedProperty));
}
```

In `tests/Cerneala.Tests/UI/Styling/StyleInvalidationTests.cs`, add:

```csharp
[Fact]
public void ManualStyleInvalidationUsesRegistryInsteadOfPropertyNameStrings()
{
    string source = File.ReadAllText(Path.Combine(
        AppContext.BaseDirectory,
        "..",
        "..",
        "..",
        "..",
        "..",
        "UI",
        "Styling",
        "StyleInvalidation.cs"));

    Assert.DoesNotContain("property.Name ==", source, StringComparison.Ordinal);
    Assert.DoesNotContain("\"IsPressed\"", source, StringComparison.Ordinal);
    Assert.DoesNotContain("\"IsSelected\"", source, StringComparison.Ordinal);
}
```

- [ ] **Step 2: Run registry tests and verify RED**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RegistryReportsBuiltInPseudoClassProperties|FullyQualifiedName~RegistryCanRegisterCustomPseudoClassProperty|FullyQualifiedName~ManualStyleInvalidationUsesRegistryInsteadOfPropertyNameStrings"
```

Expected: build fails because `PseudoClassRegistry` does not exist, or source scan fails because `StyleInvalidation` still uses string property-name checks.

- [ ] **Step 3: Create `PseudoClassRegistry`**

Create `UI/Styling/PseudoClassRegistry.cs`:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Styling;

public sealed class PseudoClassRegistry
{
    private readonly Dictionary<UiProperty, PseudoClass> properties = new(ReferenceEqualityComparer.Instance);

    public static PseudoClassRegistry Default { get; } = CreateDefault();

    public void Register(UiProperty property, PseudoClass pseudoClass)
    {
        ArgumentNullException.ThrowIfNull(property);
        if (pseudoClass == PseudoClass.None)
        {
            throw new ArgumentOutOfRangeException(nameof(pseudoClass), "Pseudo class registration requires a non-empty pseudo class.");
        }

        properties[property] = pseudoClass;
    }

    public bool AffectsPseudoClass(UiProperty property)
    {
        ArgumentNullException.ThrowIfNull(property);
        return properties.ContainsKey(property);
    }

    public PseudoClass GetPseudoClasses(UiProperty property)
    {
        ArgumentNullException.ThrowIfNull(property);
        return properties.TryGetValue(property, out PseudoClass pseudoClass)
            ? pseudoClass
            : PseudoClass.None;
    }

    private static PseudoClassRegistry CreateDefault()
    {
        PseudoClassRegistry registry = new();
        registry.Register(UIElement.IsPointerOverProperty, PseudoClass.Hover);
        registry.Register(UIElement.IsKeyboardFocusedProperty, PseudoClass.Focus);
        registry.Register(UIElement.IsKeyboardFocusWithinProperty, PseudoClass.FocusWithin);
        registry.Register(UIElement.IsEnabledProperty, PseudoClass.Disabled);
        registry.Register(ButtonBase.IsPressedProperty, PseudoClass.Pressed);
        registry.Register(ListBoxItem.IsSelectedProperty, PseudoClass.Selected);
        registry.Register(TabItem.IsSelectedProperty, PseudoClass.Selected);
        return registry;
    }
}
```

- [ ] **Step 4: Update manual style invalidation**

In `UI/Styling/StyleInvalidation.cs`, add field:

```csharp
private readonly PseudoClassRegistry pseudoClassRegistry;
```

Change the constructor signature to:

```csharp
public StyleInvalidation(
    StyleApplicator applicator,
    StyleSheet styleSheet,
    ThemeProvider? themeProvider = null,
    PseudoClassRegistry? pseudoClassRegistry = null)
```

Set field:

```csharp
this.pseudoClassRegistry = pseudoClassRegistry ?? PseudoClassRegistry.Default;
```

Replace `AffectsPseudoClass(...)` with:

```csharp
private bool AffectsPseudoClass(UiProperty property)
{
    return pseudoClassRegistry.AffectsPseudoClass(property) ||
        property.Options.HasFlag(UiPropertyOptions.AffectsStyle);
}
```

This deliberately preserves `StyleInvalidation` as a synchronous manual helper for tests and standalone use; root-owned attached trees use `UIRoot.StyleQueue`.

- [ ] **Step 5: Run registry and style invalidation tests**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~PseudoClassTests|FullyQualifiedName~StyleInvalidationTests"
```

Expected: all filtered tests pass.

- [ ] **Step 6: Commit pseudo-class registry**

```powershell
git add UI\Styling\PseudoClassRegistry.cs UI\Styling\StyleInvalidation.cs tests\Cerneala.Tests\UI\Styling\PseudoClassTests.cs tests\Cerneala.Tests\UI\Styling\StyleInvalidationTests.cs
git commit -m "fix: replace pseudo-class string detection"
```

---

### Task 6: Verify Style Phase Integration Against Existing Contracts

**Files:**
- Modify test baselines only if they expected `AffectsStyle` to behave as render-only work.

- [ ] **Step 1: Run styling, invalidation, hosting, and rendering tests**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~Styling|FullyQualifiedName~Invalidation|FullyQualifiedName~Hosting|FullyQualifiedName~Rendering|FullyQualifiedName~Elements"
```

Expected: all filtered tests pass.

- [ ] **Step 2: Search for render-only style assumptions**

Run:

```powershell
rg -n "AffectsStyle|InvalidationFlags\\.Style|FramePhase\\.Style|StyleInvalidation|property\\.Name ==|\"IsPressed\"|\"IsSelected\"" UI tests\Cerneala.Tests
```

Expected:

- `UIElement.MapInvalidationOptions(...)` maps `AffectsStyle` to `InvalidationFlags.Style`.
- `UiFrameScheduler` processes `FramePhase.Style` before `FramePhase.Measure`.
- `StyleInvalidation` has no `property.Name ==` checks.
- `"IsPressed"` / `"IsSelected"` may appear in tests, property declarations, or user-facing property names, but not as pseudo-class invalidation string checks.

- [ ] **Step 3: Commit any focused test migrations**

If Step 1 reveals tests that expected style-affecting work to be counted as render-only work, update them to explicit style-phase expectations and commit:

```powershell
git add tests\Cerneala.Tests
git commit -m "test: update style phase expectations"
```

If no files changed, do not create an empty commit.

---

### Task 7: Update Audit Documentation

**Files:**
- Modify: `AUDIT_FIX_PLAN.md`
- Modify: `ROADMAPv2_AUDIT.md`
- Modify: `docs/superpowers/plans/2026-07-03-integrate-style-phase.md`

- [ ] **Step 1: Update `AUDIT_FIX_PLAN.md` Plan 3 checklist**

Under `### Plan 3: integrate-style-phase`, add this link if it is not already present:

```markdown
Detailed plan: `docs/superpowers/plans/2026-07-03-integrate-style-phase.md`
```

After implementation and focused verification pass, change these Plan 3 items from `[ ]` to `[x]`:

```markdown
- [x] Map `UiPropertyOptions.AffectsStyle` to `InvalidationFlags.Style`, not `Render`.
- [x] Add scheduler-owned style processing before measure/arrange/render.
- [x] Decide whether style work uses `StyleQueue` or a typed style processor over dirty elements.
- [x] Make `UIRoot` or `UiHost` own style/theme scope for an attached tree.
- [x] Remove string property-name pseudo-class detection.
- [x] Add explicit pseudo-class registration or provider contract.
- [x] Add `tests/Cerneala.Tests/UI/Styling/StyleSchedulerIntegrationTests.cs`.
```

- [ ] **Step 2: Add completion note to `ROADMAPv2_AUDIT.md`**

Only after `dotnet test Cerneala.slnx` passes, add this note under `## Must Fix` > `### 4. Styling is not a retained frame phase yet`, after the required changes list:

```markdown
Implementation note: fixed by `integrate-style-phase`; `AffectsStyle` now queues retained style work, `UIRoot` owns the active style/theme scope, the scheduler processes `FramePhase.Style` before layout/render phases, and pseudo-class invalidation uses explicit property registration instead of string property-name checks.
```

- [ ] **Step 3: Run markdown reference check**

Run:

```powershell
rg -n "integrate-style-phase|StyleSchedulerIntegrationTests|StyleQueue|PseudoClassRegistry" AUDIT_FIX_PLAN.md ROADMAPv2_AUDIT.md docs\superpowers\plans\2026-07-03-integrate-style-phase.md
```

Expected: all three docs reference the completed plan and the new scheduler-owned style pieces.

- [ ] **Step 4: Commit docs**

```powershell
git add AUDIT_FIX_PLAN.md ROADMAPv2_AUDIT.md docs\superpowers\plans\2026-07-03-integrate-style-phase.md
git commit -m "docs: plan scheduler-owned style phase"
```

---

### Task 8: Full Verification

**Files:**
- No production edits unless tests reveal a missed migration.

- [ ] **Step 1: Run full test suite**

Run:

```powershell
dotnet test Cerneala.slnx
```

Expected: all tests pass.

- [ ] **Step 2: Verify OpenSpec references did not return**

Run:

```powershell
rg -n "OpenSpec|openspec|opsx" ROADMAPv2.md ROADMAPv2_AUDIT.md tests UI
```

Expected: no matches.

- [ ] **Step 3: Inspect git status and diff**

Run:

```powershell
git status --short
git diff --stat
```

Expected: no uncommitted files after final commits.

- [ ] **Step 4: Final commit if verification finds a missed fix**

If Step 1 or Step 2 required code/test fixes:

```powershell
git add <fixed-paths>
git commit -m "fix: complete scheduler-owned style phase"
```

If no files changed, do not create an empty commit.

---

## Self-Review

### Spec Coverage

- Map `UiPropertyOptions.AffectsStyle` to `InvalidationFlags.Style`, not `Render`: Task 4.
- Add scheduler-owned style processing before measure/arrange/render: Tasks 1 and 2.
- Decide queue vs typed processor: Task 2 adds `StyleQueue`; Task 3 adds `StyleProcessor`.
- Make root/host own style/theme scope: Task 3 makes `UIRoot` own `StyleSheet` and `ThemeProvider`.
- Remove string property-name pseudo-class detection: Task 5.
- Add explicit pseudo-class registration/provider contract: Task 5 adds `PseudoClassRegistry` while keeping `IStylePseudoClassProvider`.
- Add `tests/Cerneala.Tests/UI/Styling/StyleSchedulerIntegrationTests.cs`: Task 1.

### Placeholder Scan

This plan contains no `TBD`, no placeholder implementation steps, and no "write tests for the above" without concrete test code. Conditional commits are limited to test migration or verification fixes and explicitly say not to create empty commits.

### Type Consistency

- `StyleQueue` follows existing queue types and uses `UIElement`, `UIRoot`, `ElementQueueOrder`, and `ReferenceEqualityComparer`.
- `FrameStats.StyledElements` matches `FramePhase.Style`.
- `FramePhaseProcessors.Style` matches `UiFrameScheduler.ProcessStyle(...)`.
- `UIRoot.SetStyleSheet(...)`, `UIRoot.SetThemeProvider(...)`, `UIRoot.StyleQueue`, and `StyleProcessor.Process(...)` are introduced before tests rely on them passing.
- `PseudoClassRegistry` uses existing `UiProperty`, `PseudoClass`, `ButtonBase.IsPressedProperty`, `ListBoxItem.IsSelectedProperty`, and `TabItem.IsSelectedProperty`.
