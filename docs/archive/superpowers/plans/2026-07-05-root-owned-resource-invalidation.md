# Root-Owned Resource Invalidation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move resource change observation out of individual controls and into the retained root. `TextBlock`, `Image`, styles, and future templates should record dependencies; `UIRoot` should observe resource changes and enqueue the correct invalidation work.

**Architecture:** Resource lookup remains explicit and strongly typed through `ResourceId<T>` and `IResourceProvider`. Mutability is represented by a separate observable provider contract. Controls may hold local provider overrides for MVP compatibility, but direct `ResourceStore` subscriptions in controls must go away. `UIRoot` owns a `ResourceDependencyTracker` for the attached tree and invalidates dependent elements when observed resources change.

**Tech Stack:** C#/.NET 8, xUnit, Cerneala retained resource system, existing invalidation flags.

---

## File Structure

- Create: `UI/Resources/IObservableResourceProvider.cs`
  - Exposes `ResourceChanged` for providers that can notify changes.
- Modify: `UI/Resources/ResourceStore.cs`
  - Implement `IObservableResourceProvider`.
- Modify: `UI/Resources/ResourceDependencyTracker.cs`
  - Track `UIElement` dependents, resource ids, invalidation effects, intrinsic-size behavior, and dependency versions.
  - Add cleanup for detached elements or dead owners.
- Modify: `UI/Elements/UIRoot.cs`
  - Add root-owned `ResourceProvider` and `ResourceDependencyTracker`.
  - Add `SetResourceProvider(IResourceProvider? provider)`.
  - Subscribe only when provider implements `IObservableResourceProvider`.
  - Invalidate dependent elements on resource changes.
- Modify: `UI/Controls/TextBlock.cs`
  - Remove direct `ResourceStore` subscription fields and event handling.
  - Resolve provider through local override or `Root?.ResourceProvider`.
  - Record font resource dependencies through root-owned tracker when attached.
- Modify: `UI/Controls/Image.cs`
  - Remove direct `ResourceStore` subscription fields and event handling.
  - Resolve provider through local override or `Root?.ResourceProvider`.
  - Record image resource dependencies through root-owned tracker with intrinsic-size metadata.
- Modify: `Playground/Cerneala.Playground/Game1.cs`
  - Set the root resource provider once through `uiRoot.SetResourceProvider(_resources)`.
- Modify: `Playground/Cerneala.Playground/Samples/PlaygroundText.cs`
  - Keep local provider support, but prefer root provider for new samples after this plan.
- Create: `tests/Cerneala.Tests/UI/Resources/HostResourceInvalidationIntegrationTests.cs`
  - Prove root-owned resource changes invalidate dependent text/image controls.
- Modify: `tests/Cerneala.Tests/UI/Resources/ResourceDependencyTrackerTests.cs`
  - Cover effect metadata and cleanup.
- Modify: `tests/Cerneala.Tests/Controls/TextBlockInvalidationTests.cs`
  - Update expectations from direct subscription to root-owned invalidation.
- Modify: `tests/Cerneala.Tests/Controls/ImageTests.cs`
  - Update expectations from direct subscription to root-owned invalidation.

## Important Existing Behavior

`TextBlock` and `Image` currently subscribe to `ResourceStore` directly:

```csharp
if (value is ResourceStore store)
{
    subscribedStore = store;
    subscribedStore.ResourceChanged += OnResourceChanged;
}
```

That hard-codes resource observation to one concrete provider and repeats lifecycle logic per control. It will not scale to styles, templates, theme resources, or custom resource providers.

Target behavior:

```csharp
root.SetResourceProvider(resources);
textBlock.FontResourceId = bodyFontId;
root.ProcessFrame();

resources.SetResource(bodyFontId, updatedFont);
root.ProcessFrame();

Assert.True(root.RenderQueue processed textBlock);
```

Controls record dependencies when resolving resources; root observes provider changes and turns those changes into retained invalidation requests.

---

### Task 1: Add RED Tests For Root-Owned Resource Invalidation

**Files:**
- Create: `tests/Cerneala.Tests/UI/Resources/HostResourceInvalidationIntegrationTests.cs`
- Modify: `tests/Cerneala.Tests/UI/Resources/ResourceDependencyTrackerTests.cs`
- Modify: `tests/Cerneala.Tests/Controls/TextBlockInvalidationTests.cs`
- Modify: `tests/Cerneala.Tests/Controls/ImageTests.cs`

- [ ] **Step 1: Create host integration tests**

Create `tests/Cerneala.Tests/UI/Resources/HostResourceInvalidationIntegrationTests.cs` with tests covering:

- `TextBlock` with `FontResourceId` records a root-owned dependency.
- Changing the font resource enqueues measure and render work for that `TextBlock`.
- `Image` with `SourceResourceId` and `UseIntrinsicSize=true` enqueues measure and render work.
- `Image` with `UseIntrinsicSize=false` enqueues render work only.
- A custom provider implementing `IObservableResourceProvider` works without being `ResourceStore`.
- A non-observable `IResourceProvider` can be resolved but does not subscribe.

Skeleton:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Resources;

public sealed class HostResourceInvalidationIntegrationTests
{
    [Fact]
    public void FontResourceChangeInvalidatesDependentTextBlockThroughRoot()
    {
        ResourceId<FontResource> id = new("Body");
        ResourceStore store = new();
        store.SetResource(id, new FontResource(TestFont.Instance));
        UIRoot root = new();
        root.SetResourceProvider(store);
        TextBlock text = new() { Text = "Hello", FontResourceId = id };
        root.VisualChildren.Add(text);
        root.ProcessFrame();
        text.DirtyState.Clear(InvalidationFlags.Measure | InvalidationFlags.Render);

        store.SetResource(id, new FontResource(TestFont.Instance));
        var stats = root.ProcessFrame();

        Assert.True(stats.MeasuredElements > 0);
        Assert.True(stats.RenderedElements > 0);
    }
}
```

Use existing test font/image helpers if present. If not present, create minimal fake `IDrawFont` / `IDrawImage` test types inside the test file.

- [ ] **Step 2: Add tracker tests for invalidation metadata**

Extend `ResourceDependencyTrackerTests` to prove a dependency records:

- owner element
- resource key/type
- invalidation effects
- intrinsic-size behavior
- version increments on resource change

- [ ] **Step 3: Run targeted resource tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~HostResourceInvalidationIntegrationTests|FullyQualifiedName~ResourceDependencyTrackerTests|FullyQualifiedName~TextBlockInvalidationTests|FullyQualifiedName~ImageTests"
```

Expected: root-owned resource tests fail because `UIRoot.SetResourceProvider(...)` and observable provider contracts do not exist yet.

- [ ] **Step 4: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\UI\Resources\HostResourceInvalidationIntegrationTests.cs tests\Cerneala.Tests\UI\Resources\ResourceDependencyTrackerTests.cs tests\Cerneala.Tests\Controls\TextBlockInvalidationTests.cs tests\Cerneala.Tests\Controls\ImageTests.cs
git commit -m "test: capture root-owned resource invalidation"
```

---

### Task 2: Add Observable Resource Provider Contract

**Files:**
- Create: `UI/Resources/IObservableResourceProvider.cs`
- Modify: `UI/Resources/ResourceStore.cs`

- [ ] **Step 1: Create provider interface**

Create `UI/Resources/IObservableResourceProvider.cs`:

```csharp
namespace Cerneala.UI.Resources;

public interface IObservableResourceProvider : IResourceProvider
{
    event EventHandler<ResourceChangedEventArgs>? ResourceChanged;
}
```

- [ ] **Step 2: Make `ResourceStore` implement the interface**

Change declaration:

```csharp
public sealed class ResourceStore : IObservableResourceProvider
```

No behavior change should be required because `ResourceStore` already exposes `ResourceChanged`.

- [ ] **Step 3: Run resource store tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~ResourceStoreTests|FullyQualifiedName~ResourceIdTests"
```

Expected: existing resource tests pass.

- [ ] **Step 4: Commit observable provider contract**

```powershell
git add UI\Resources\IObservableResourceProvider.cs UI\Resources\ResourceStore.cs
git commit -m "feat: add observable resource provider contract"
```

---

### Task 3: Upgrade Resource Dependency Tracking

**Files:**
- Modify: `UI/Resources/ResourceDependencyTracker.cs`
- Modify: `tests/Cerneala.Tests/UI/Resources/ResourceDependencyTrackerTests.cs`

- [ ] **Step 1: Track element dependencies with invalidation metadata**

Change dependency tracking from `object owner` to `UIElement owner` for retained UI dependencies. Keep overloads if older tests need object-level behavior, but new root-owned invalidation should use `UIElement`.

Recommended shape:

```csharp
public void RecordDependency<T>(
    UIElement owner,
    ResourceId<T> id,
    InvalidationFlags effects,
    bool affectsIntrinsicSize = true)
```

Store a record:

```csharp
private sealed record ResourceDependency(
    UIElement Owner,
    ResourceKey Key,
    InvalidationFlags Effects,
    bool AffectsIntrinsicSize);
```

- [ ] **Step 2: Add method to collect dependents on change**

Add:

```csharp
public IReadOnlyList<ResourceDependencyChange> NotifyResourceChanged(ResourceChangedEventArgs args)
```

Return dependency changes for live attached owners. Each change should include owner, effects, and intrinsic-size behavior.

- [ ] **Step 3: Keep version behavior**

Maintain `GetDependencyVersion(owner)` and `GetResourceVersion(id)` so render dependencies can include resource version identity.

- [ ] **Step 4: Add cleanup**

When notifying, ignore owners where `Owner.IsAttached == false`. Optionally remove them from internal sets.

- [ ] **Step 5: Run tracker tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~ResourceDependencyTrackerTests"
```

Expected: tracker tests pass.

- [ ] **Step 6: Commit tracker upgrade**

```powershell
git add UI\Resources\ResourceDependencyTracker.cs tests\Cerneala.Tests\UI\Resources\ResourceDependencyTrackerTests.cs
git commit -m "feat: track retained resource dependencies"
```

---

### Task 4: Make `UIRoot` Own Resource Observation

**Files:**
- Modify: `UI/Elements/UIRoot.cs`

- [ ] **Step 1: Add root resource properties**

In `UIRoot`, add:

```csharp
private IObservableResourceProvider? observableResourceProvider;

public IResourceProvider? ResourceProvider { get; private set; }

public ResourceDependencyTracker ResourceDependencyTracker { get; }
```

Initialize `ResourceDependencyTracker` in the constructor.

- [ ] **Step 2: Add `SetResourceProvider`**

```csharp
public void SetResourceProvider(IResourceProvider? provider)
{
    if (ReferenceEquals(ResourceProvider, provider))
    {
        return;
    }

    if (observableResourceProvider is not null)
    {
        observableResourceProvider.ResourceChanged -= OnResourceChanged;
    }

    ResourceProvider = provider;
    observableResourceProvider = provider as IObservableResourceProvider;
    if (observableResourceProvider is not null)
    {
        observableResourceProvider.ResourceChanged += OnResourceChanged;
    }

    Invalidate(InvalidationFlags.Resource | InvalidationFlags.Subtree, "Root resource provider changed");
}
```

- [ ] **Step 3: Invalidate dependents on resource changes**

Add handler:

```csharp
private void OnResourceChanged(object? sender, ResourceChangedEventArgs args)
{
    foreach (ResourceDependencyChange change in ResourceDependencyTracker.NotifyResourceChanged(args))
    {
        change.Owner.Invalidate(new InvalidationRequest(
            change.Owner,
            InvalidationFlags.Resource,
            "Resource changed",
            resourceEffects: change.Effects,
            affectsIntrinsicSize: change.AffectsIntrinsicSize));
    }
}
```

Use the exact change type created in Task 3.

- [ ] **Step 4: Run root tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UIRootTests|FullyQualifiedName~HostResourceInvalidationIntegrationTests"
```

Expected: root resource provider tests start passing after controls are updated in Task 5.

- [ ] **Step 5: Commit root resource ownership**

```powershell
git add UI\Elements\UIRoot.cs
git commit -m "feat: make root own resource observation"
```

---

### Task 5: Remove Control-Specific ResourceStore Subscriptions

**Files:**
- Modify: `UI/Controls/TextBlock.cs`
- Modify: `UI/Controls/Image.cs`
- Modify: `tests/Cerneala.Tests/Controls/TextBlockInvalidationTests.cs`
- Modify: `tests/Cerneala.Tests/Controls/ImageTests.cs`

- [ ] **Step 1: Update provider resolution helpers**

In `TextBlock` and `Image`, replace direct use of the local `ResourceProvider` field with:

```csharp
private IResourceProvider? ResolveResourceProvider()
{
    return ResourceProvider ?? Root?.ResourceProvider;
}

private ResourceDependencyTracker? ResolveResourceDependencyTracker()
{
    return ResourceDependencyTracker ?? Root?.ResourceDependencyTracker;
}
```

Keep the local `ResourceProvider` property for explicit overrides, but remove `subscribedStore` fields and event handlers.

- [ ] **Step 2: Record dependencies through the tracker**

For `TextBlock.FontResourceId`, record:

```csharp
ResolveResourceDependencyTracker()?.RecordDependency(
    this,
    id,
    InvalidationFlags.Measure | InvalidationFlags.Render,
    affectsIntrinsicSize: true);
```

For `Image.SourceResourceId`, record:

```csharp
ResolveResourceDependencyTracker()?.RecordDependency(
    this,
    id,
    UseIntrinsicSize
        ? InvalidationFlags.Measure | InvalidationFlags.Render
        : InvalidationFlags.Render,
    affectsIntrinsicSize: UseIntrinsicSize);
```

- [ ] **Step 3: Use tracker versions for render dependencies**

Replace direct `ResourceStore.GetVersion(...)` fallback with:

```csharp
long version = ResolveResourceDependencyTracker()?.GetDependencyVersion(this) ?? 0;
```

If no tracker exists and the provider is a `ResourceStore`, keeping the old `GetVersion(...)` fallback is acceptable for detached/unit scenarios. Do not subscribe to the store.

- [ ] **Step 4: Remove direct event handlers**

Delete `OnResourceChanged` from `TextBlock` and `Image` if it is no longer used.

- [ ] **Step 5: Run control resource tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~HostResourceInvalidationIntegrationTests|FullyQualifiedName~TextBlockInvalidationTests|FullyQualifiedName~ImageTests|FullyQualifiedName~ResourceRenderDependencyTests"
```

Expected: root-owned invalidation works, render dependency identity changes when resources change, and controls no longer subscribe to `ResourceStore` directly.

- [ ] **Step 6: Commit control resource cleanup**

```powershell
git add UI\Controls\TextBlock.cs UI\Controls\Image.cs tests\Cerneala.Tests\Controls\TextBlockInvalidationTests.cs tests\Cerneala.Tests\Controls\ImageTests.cs
git commit -m "refactor: resolve resources through retained root"
```

---

### Task 6: Update Playground Resource Wiring

**Files:**
- Modify: `Playground/Cerneala.Playground/Game1.cs`
- Modify: `Playground/Cerneala.Playground/Samples/PlaygroundText.cs` only if needed
- Modify: `tests/Cerneala.Tests/Playground/Game1SourceTests.cs`

- [ ] **Step 1: Set root provider in `Game1.LoadContent()`**

After creating `_resources`, call:

```csharp
uiRoot.SetResourceProvider(_resources);
```

Keep existing sample constructor provider arguments for now to avoid a broad sample rewrite.

- [ ] **Step 2: Add source test**

Extend `Game1SourceTests` to verify `SetResourceProvider` is called.

- [ ] **Step 3: Run playground source tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~Game1SourceTests|FullyQualifiedName~HostResourceInvalidationIntegrationTests"
```

Expected: playground uses root resource provider.

- [ ] **Step 4: Commit playground resource wiring**

```powershell
git add Playground\Cerneala.Playground\Game1.cs tests\Cerneala.Tests\Playground\Game1SourceTests.cs
git commit -m "feat: wire playground resources through root"
```

---

### Task 7: Full Verification And Status Update

**Files:**
- Modify: `ROADMAPv2.md`
- Modify: `ROADMAPv2_AUDIT.md`
- Modify: `AUDIT_FIX_PLAN.md` if tracked

- [ ] **Step 1: Run full tests**

```powershell
dotnet test Cerneala.slnx
```

Expected: full suite passes.

- [ ] **Step 2: Update roadmap/audit status**

Mark root-owned resource invalidation implemented. Do not mark theme-resource/style-template resource propagation scenario-complete unless tests cover those paths.

- [ ] **Step 3: Commit final docs**

```powershell
git add ROADMAPv2.md ROADMAPv2_AUDIT.md AUDIT_FIX_PLAN.md
git commit -m "docs: record root-owned resource invalidation"
```

## Stop Conditions

- [ ] Stop if controls still subscribe to `ResourceStore` directly after this plan.
- [ ] Stop if every frame scans all resources or all elements. Resource invalidation must be event/dependency driven.
- [ ] Stop if custom providers require being `ResourceStore`. The observable contract must be provider-neutral.
