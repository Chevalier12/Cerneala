# Create Retained UI MVP Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Define and implement one product-level Cerneala MVP sample that proves the retained UI framework works as a coherent system: tree, layout, rendering, input, focus, commands, resources, text, simple list/scroll, and no-work retained frames.

**Architecture:** This is not a new framework layer. It is a vertical slice over existing layers. The sample must not rebuild UI every frame. It must create a retained tree once, mutate state through commands/input/resources, and rely on invalidation-driven update phases. Tests must prove `Update` performs dirty work and `Draw` only submits committed cached commands.

**Tech Stack:** C#/.NET 8, xUnit, Cerneala retained UI runtime, existing Playground MonoGame sample infrastructure, existing fake host/backend tests.

---

## File Structure

- Create: `Playground/Cerneala.Playground/Samples/RetainedAppSample.cs`
  - A larger retained app sample with header, card, image, button command, focusable input/control, scroll/list area, and status text.
  - Must build the UI tree once in `Build()`.
  - Must expose small test hooks without making the sample architecture weird.
- Modify: `Playground/Cerneala.Playground/Samples/SampleSelector.cs`
  - Add `RetainedAppSample` to the default sample list.
- Modify: `Playground/Cerneala.Playground/Samples/PlaygroundText.cs`
  - Add helper overloads only if the sample needs them.
- Create: `tests/Cerneala.Tests/Playground/RetainedAppSampleContractTests.cs`
  - Prove sample builds, is added to selector, and does not use per-frame rebuild patterns.
- Create: `tests/Cerneala.Tests/UI/Hosting/RetainedVerticalSliceTests.cs`
  - Prove first frame does work, unchanged second frame does no retained work, draw is pure, command mutation invalidates only needed work, resource mutation invalidates dependent work.
- Modify: `tests/Cerneala.Tests/Playground/Game1SourceTests.cs`
  - Prove the new sample is discoverable through `SampleSelector.CreateDefault(...)`.
- Modify: `ROADMAPv2.md`
  - Add or check an MVP vertical-slice acceptance item.

## Important Existing Behavior

The playground currently has focused samples like:

- `RetainedButtonSample`
- `LayoutSample`
- `TextSample`
- `DiagnosticsSample`

Those are useful but each proves a narrow slice. The next completion gate should be one cohesive retained app sample.

The vertical slice must satisfy:

```text
Frame 1: layout/render/input caches are built.
Frame 2 unchanged: no layout/render/hit-test rebuilds.
Draw every frame: no element render hooks run.
State mutation: command or resource change invalidates specific retained work.
Tree mutation: handled during Update, not lazily during Draw.
```

Do not include the existing stats overlay inside this sample's tested tree if it updates text every frame, because that would intentionally prevent no-work frames.

---

### Task 1: Add RED Tests For The MVP Vertical Slice

**Files:**
- Create: `tests/Cerneala.Tests/Playground/RetainedAppSampleContractTests.cs`
- Create: `tests/Cerneala.Tests/UI/Hosting/RetainedVerticalSliceTests.cs`
- Modify: `tests/Cerneala.Tests/Playground/Game1SourceTests.cs`

- [ ] **Step 1: Create playground sample contract tests**

Create `tests/Cerneala.Tests/Playground/RetainedAppSampleContractTests.cs` with tests proving:

- `RetainedAppSample.Build()` returns a non-null retained root element.
- `SampleSelector.CreateDefault(...)` includes a sample named `Retained App` or similar.
- Calling `Build()` does not require MonoGame objects.
- The sample does not call `Build()` from an update loop.

Example:

```csharp
using Cerneala.Playground.Samples;
using Cerneala.UI.Elements;

namespace Cerneala.Tests.Playground;

public sealed class RetainedAppSampleContractTests
{
    [Fact]
    public void RetainedAppSampleBuildsRetainedTree()
    {
        RetainedAppSample sample = new();

        UIElement root = sample.Build();

        Assert.NotNull(root);
        Assert.NotEmpty(root.VisualChildren);
    }

    [Fact]
    public void DefaultSampleSelectorIncludesRetainedAppSample()
    {
        SampleSelector selector = SampleSelector.CreateDefault();

        Assert.Contains(selector.Samples, sample => sample.Name == "Retained App");
    }
}
```

- [ ] **Step 2: Create retained vertical slice host tests**

Create `tests/Cerneala.Tests/UI/Hosting/RetainedVerticalSliceTests.cs` with tests proving:

- first frame processes layout/render/hit-test
- unchanged second frame reports `NoWorkFrames == 1` and zero layout/render/hit-test work
- two draws after one update do not re-render elements
- executing the sample button command mutates status text and invalidates render/layout through retained update
- resource update invalidates dependent text/image if root-owned resources plan has landed

Use `UiHost`, `UIRoot`, `FakeDrawingBackend`, and empty `InputFrame` helpers from existing host tests.

- [ ] **Step 3: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RetainedAppSampleContractTests|FullyQualifiedName~RetainedVerticalSliceTests|FullyQualifiedName~Game1SourceTests"
```

Expected: tests fail because `RetainedAppSample` does not exist yet.

- [ ] **Step 4: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\Playground\RetainedAppSampleContractTests.cs tests\Cerneala.Tests\UI\Hosting\RetainedVerticalSliceTests.cs tests\Cerneala.Tests\Playground\Game1SourceTests.cs
git commit -m "test: capture retained MVP vertical slice"
```

---

### Task 2: Implement `RetainedAppSample`

**Files:**
- Create: `Playground/Cerneala.Playground/Samples/RetainedAppSample.cs`
- Modify: `Playground/Cerneala.Playground/Samples/PlaygroundText.cs` only if needed

- [ ] **Step 1: Create sample class**

Create `Playground/Cerneala.Playground/Samples/RetainedAppSample.cs`:

```csharp
#nullable enable

using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;
using PanelOrientation = Cerneala.UI.Layout.Panels.Orientation;

namespace Cerneala.Playground.Samples;

public sealed class RetainedAppSample : IPlaygroundSample
{
    private readonly PlaygroundText text;
    private int clickCount;

    public RetainedAppSample(IResourceProvider? resourceProvider = null, ResourceId<FontResource>? fontResourceId = null)
    {
        text = new PlaygroundText(resourceProvider, fontResourceId);
    }

    public string Name => "Retained App";

    public TextBlock? StatusText { get; private set; }

    public Button? PrimaryButton { get; private set; }

    public UIElement Build()
    {
        StatusText = text.Create("Ready. No retained work should run on unchanged frames.", 14, new DrawColor(51, 65, 85));
        PrimaryButton = new Button
        {
            Content = text.Create("Run retained command", 14, new DrawColor(15, 23, 42)),
            Padding = new Thickness(12, 8, 12, 8),
            Background = new DrawColor(248, 250, 252),
            BorderColor = new DrawColor(100, 116, 139),
            BorderThickness = new Thickness(1),
            Command = new ActionCommand(_ =>
            {
                clickCount++;
                StatusText.Text = $"Command executed {clickCount} time(s).";
            })
        };

        StackPanel root = new()
        {
            Margin = new Thickness(32, 24, 32, 24),
            Orientation = PanelOrientation.Vertical
        };

        root.VisualChildren.Add(text.Create("Cerneala retained app", 26, new DrawColor(15, 23, 42)));
        root.VisualChildren.Add(text.Create("Retained tree, invalidation-driven layout/render, explicit input.", 15, new DrawColor(71, 85, 105)));
        root.VisualChildren.Add(BuildCard());
        root.VisualChildren.Add(BuildListCard());
        return root;
    }

    private UIElement BuildCard()
    {
        StackPanel content = new() { Orientation = PanelOrientation.Vertical };
        content.VisualChildren.Add(text.Create("Interactive state", 18, new DrawColor(30, 41, 59)));
        content.VisualChildren.Add(StatusText!);
        content.VisualChildren.Add(PrimaryButton!);

        return new Border
        {
            Margin = new Thickness(0, 16, 0, 0),
            Padding = new Thickness(14),
            Background = new DrawColor(241, 245, 249),
            BorderColor = new DrawColor(148, 163, 184),
            BorderThickness = new Thickness(1),
            Child = content
        };
    }

    private UIElement BuildListCard()
    {
        StackPanel list = new() { Orientation = PanelOrientation.Vertical };
        for (int i = 1; i <= 8; i++)
        {
            list.VisualChildren.Add(text.Create($"Retained row {i}", 14, new DrawColor(51, 65, 85)));
        }

        return new Border
        {
            Margin = new Thickness(0, 12, 0, 0),
            Padding = new Thickness(14),
            Background = new DrawColor(255, 255, 255),
            BorderColor = new DrawColor(203, 213, 225),
            BorderThickness = new Thickness(1),
            Child = list
        };
    }
}
```

Adjust the sample if `Border.Child`, `StackPanel`, or `Button.Content` semantics have changed after previous plans.

- [ ] **Step 2: Keep the sample retained**

Do not add an `Update()` method that rebuilds the tree. The only runtime mutation in this sample should be property changes, command execution, or resource updates.

- [ ] **Step 3: Run sample contract tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RetainedAppSampleContractTests"
```

Expected: build contract tests pass after Task 3 adds selector registration.

- [ ] **Step 4: Commit sample file**

```powershell
git add Playground\Cerneala.Playground\Samples\RetainedAppSample.cs
git commit -m "feat: add retained app playground sample"
```

---

### Task 3: Register Sample In Playground Selector

**Files:**
- Modify: `Playground/Cerneala.Playground/Samples/SampleSelector.cs`
- Modify: `tests/Cerneala.Tests/Playground/Game1SourceTests.cs`

- [ ] **Step 1: Add sample to default list**

In `SampleSelector.CreateDefault(...)`, add the new sample near the top:

```csharp
new RetainedAppSample(resourceProvider, fontResourceId),
```

Recommended order:

1. Retained App
2. Button
3. Layout
4. Text
5. Diagnostics

- [ ] **Step 2: Update source tests**

Update `Game1SourceTests` or sample selector tests to assert the sample exists.

- [ ] **Step 3: Run playground tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RetainedAppSampleContractTests|FullyQualifiedName~Game1SourceTests"
```

Expected: sample is discoverable.

- [ ] **Step 4: Commit selector registration**

```powershell
git add Playground\Cerneala.Playground\Samples\SampleSelector.cs tests\Cerneala.Tests\Playground\Game1SourceTests.cs
git commit -m "feat: register retained app sample"
```

---

### Task 4: Prove Retained Frame Behavior With The Sample

**Files:**
- Modify: `tests/Cerneala.Tests/UI/Hosting/RetainedVerticalSliceTests.cs`
- Modify: `tests/Cerneala.Tests/UI/Hosting/FakeDrawingBackend.cs` only if more instrumentation is needed

- [ ] **Step 1: Test first and second frame behavior**

Add a test:

```csharp
[Fact]
public void RetainedAppSampleSecondUnchangedFrameDoesNoRetainedWork()
{
    RetainedAppSample sample = new();
    UIRoot root = new();
    root.VisualChildren.Add(sample.Build());
    UiHost host = new(new UiHostOptions { Root = root });

    UiFrame first = host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);
    UiFrame second = host.Update(EmptyFrame(), new UiViewport(800, 600), TimeSpan.Zero);

    Assert.True(first.Stats.MeasuredElements > 0);
    Assert.True(first.Stats.RenderedElements > 0);
    Assert.Equal(0, second.Stats.MeasuredElements);
    Assert.Equal(0, second.Stats.ArrangedElements);
    Assert.Equal(0, second.Stats.RenderedElements);
    Assert.Equal(1, second.Stats.NoWorkFrames);
}
```

- [ ] **Step 2: Test draw purity**

Use `FakeDrawingBackend` to draw twice after one update and assert backend render calls increase while retained render counters do not regenerate local render commands.

- [ ] **Step 3: Test command mutation**

Execute `sample.PrimaryButton!.Command!.Execute(null)` or route an input click if focus/hit-test coordinates are stable. Then call `host.Update(...)` and assert status text changed and retained work ran because a property changed.

Prefer direct command execution for this test unless hit-test coordinates become brittle. Input-path click coverage belongs in input tests.

- [ ] **Step 4: Run vertical slice tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RetainedVerticalSliceTests|FullyQualifiedName~UiHostFrameContractTests|FullyQualifiedName~RetainedRendererDrawPurityTests"
```

Expected: retained app sample obeys no-work and draw-purity contracts.

- [ ] **Step 5: Commit vertical slice tests**

```powershell
git add tests\Cerneala.Tests\UI\Hosting\RetainedVerticalSliceTests.cs tests\Cerneala.Tests\UI\Hosting\FakeDrawingBackend.cs
git commit -m "test: prove retained app frame behavior"
```

---

### Task 5: Add Optional Resource/Text Assertion If Resource Plan Has Landed

**Files:**
- Modify: `Playground/Cerneala.Playground/Samples/RetainedAppSample.cs`
- Modify: `tests/Cerneala.Tests/UI/Hosting/RetainedVerticalSliceTests.cs`

- [ ] **Step 1: Add resource-backed text or image only if root-owned resources exist**

If `UIRoot.SetResourceProvider(...)` and root-owned resource invalidation have landed, update the sample so `PlaygroundText` can resolve the root resource provider or pass the resource provider explicitly.

Do not block this vertical slice on image loading or MonoGame texture resources. A font resource dependency is sufficient.

- [ ] **Step 2: Add resource mutation test**

Add a test proving changing the sample font resource invalidates dependent text through root-owned resource invalidation.

- [ ] **Step 3: Skip this task if resource plan has not landed**

If root-owned resource invalidation is not implemented yet, leave this task unchecked and do not fake it. The sample can still be MVP without this optional assertion.

---

### Task 6: Roadmap Completion Gate

**Files:**
- Modify: `ROADMAPv2.md`
- Modify: `ROADMAPv2_AUDIT.md`
- Modify: `AUDIT_FIX_PLAN.md` if tracked

- [ ] **Step 1: Add an MVP acceptance item**

Add or update a roadmap checklist item similar to:

```markdown
- [x] `Playground/Cerneala.Playground/Samples/RetainedAppSample.cs` proves a retained app vertical slice: stable tree, layout, render cache, input command mutation, and no-work unchanged frames.
- [x] `tests/Cerneala.Tests/UI/Hosting/RetainedVerticalSliceTests.cs` proves unchanged retained app frames do not rebuild layout/render commands unnecessarily.
```

Only mark resource mutation included if Task 5 landed.

- [ ] **Step 2: Run full tests**

```powershell
dotnet test Cerneala.slnx
```

Expected: full suite passes.

- [ ] **Step 3: Commit roadmap update**

```powershell
git add ROADMAPv2.md ROADMAPv2_AUDIT.md AUDIT_FIX_PLAN.md
git commit -m "docs: add retained MVP vertical slice gate"
```

## Stop Conditions

- [ ] Stop if the sample starts rebuilding the UI tree every frame. That violates retained-mode intent.
- [ ] Stop if the sample requires MonoGame runtime objects to unit test its retained tree.
- [ ] Stop if diagnostics overlay text changes every frame inside the vertical-slice no-work test. That would intentionally invalidate the tree and make the test meaningless.
- [ ] Stop if this becomes a new control-building phase. The sample should use existing controls and expose missing foundation bugs, not hide them with new widgets.
