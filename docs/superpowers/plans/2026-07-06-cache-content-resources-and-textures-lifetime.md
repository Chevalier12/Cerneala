# Cache Content Resources And Textures Lifetime Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, RED test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Make path-backed image resources and adapter-owned texture resources usable in real runtime scenarios. `ImageResource` can describe a path, `MonoGameImageLoader` exists, and retained resource invalidation exists, but runtime loading/caching/lifetime must be root/adapter-owned and deterministic.

**Architecture:** Keep resources typed and explicit. Controls may ask the root/resource services to resolve an `ImageResource`, but controls must not know about `Texture2D`, MonoGame, file streams, or content manager details. Path-backed image loading should cache by resource identity/path and invalidate dependents through the existing resource dependency tracker.

**Tech Stack:** C#/.NET 8, xUnit, existing `UI/Resources`, `UI/Resources/MonoGame`, `UI/Controls/Image`, `UI/Elements/UIRoot`, retained invalidation/resource tests.

---

## File Structure

- Modify: `UI/Resources/ImageResource.cs`
  - Expose safe identity/path metadata needed by cache without leaking mutable fields.
- Create: `UI/Resources/ImageResourceCache.cs`
  - Loader-backed cache for path-backed images.
- Modify: `UI/Resources/IImageLoader.cs`
  - Keep interface small; add no platform types.
- Modify: `UI/Resources/MonoGame/MonoGameImageLoader.cs`
  - Ensure returned `MonoGameImage` participates in disposal if supported.
- Modify: `Drawing/MonoGame/MonoGameImage.cs`
  - Implement `IDisposable` if needed for owned texture lifetime.
- Modify: `UI/Hosting/MonoGame/MonoGameContentServices.cs`
  - Own and expose `IImageLoader` and image cache service.
- Modify: `UI/Hosting/MonoGame/MonoGameUiHostOptions.cs`
  - Accept optional image loader/content services if needed.
- Modify: `UI/Hosting/MonoGame/MonoGameUiHost.cs`
  - Attach content/resource services to the root.
- Modify: `UI/Elements/UIRoot.cs`
  - Own image loader/cache service behind resource abstractions.
- Modify: `UI/Controls/Image.cs`
  - Resolve path-backed `ImageResource` through root-owned cache/loader.
- Create: `tests/Cerneala.Tests/UI/Resources/ImageResourceCacheTests.cs`
- Create: `tests/Cerneala.Tests/UI/Resources/PathBackedImageResourceIntegrationTests.cs`
- Create: `tests/Cerneala.Tests/UI/Hosting/MonoGameContentServicesLifetimeTests.cs`

## Important Existing Behavior

- `ImageResource` can wrap either an `IDrawImage` or a path.
- `ImageResource.Resolve(IImageLoader?)` currently requires a loader for path-backed images.
- `Image` control calls `resource.Resolve()` without passing a loader, so path-backed resources are not truly integrated.
- `ResourceDependencyTracker` and `UIRoot.OnResourceChanged(...)` already invalidate dependent elements.
- `MonoGameImageLoader` loads a texture from a file and returns `MonoGameImage`.
- `IDrawImage` does not require disposal, but MonoGame texture-backed images do own native/GPU resources.

Target behavior:

- Path-backed `ImageResource` resolves through root-owned resource services.
- Loading the same image resource repeatedly does not reload it every measure/render.
- Replacing a resource invalidates dependents and can load a new image on next retained work.
- Detaching controls does not leak resource dependencies.
- Disposing MonoGame host/content services disposes owned cached images/textures where applicable.
- Resource behavior remains backend-neutral above adapter folders.

## Rules

- [ ] Do not put `Texture2D`, `GraphicsDevice`, `SpriteBatch`, Skia, or HarfBuzz types in controls/core resource APIs.
- [ ] Do not introduce WPF `ResourceDictionary` semantics.
- [ ] Do not add global hidden resource lookup.
- [ ] Do not reload path-backed resources on every render.
- [ ] Do not dispose externally supplied `IDrawImage` instances unless ownership is explicit.
- [ ] Do not make `IDrawImage` inherit `IDisposable`; use optional disposal checks instead.

---

### Task 1: Add RED Image Resource Cache Tests

**Files:**
- Create: `tests/Cerneala.Tests/UI/Resources/ImageResourceCacheTests.cs`
- Create: `tests/Cerneala.Tests/UI/Resources/PathBackedImageResourceIntegrationTests.cs`

- [ ] **Step 1: Add cache behavior tests**

Create tests:

```csharp
PathBackedImageResourceLoadsOncePerIdentity()
PathBackedImageResourceReturnsCachedImageOnMeasureAndRender()
DifferentPathsLoadDifferentImages()
CacheClearDisposesOwnedDisposableImages()
ExternallySuppliedImageIsNotDisposedByCache()
MissingLoaderThrowsClearRuntimeError()
```

Use fake `IImageLoader` and fake `IDrawImage`/`IDisposable` images.

- [ ] **Step 2: Add retained integration tests**

Create tests:

```csharp
ImageControlResolvesPathBackedResourceThroughRootLoader()
ImageControlPathResourceLoadInvalidatesMeasureWhenIntrinsicSizeIsUsed()
ReplacingPathBackedImageResourceInvalidatesDependentImageRender()
SecondUnchangedFrameDoesNotReloadPathBackedImage()
DetachedImageIsRemovedFromResourceDependencyTrackingAfterResourceChange()
```

- [ ] **Step 3: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~ImageResourceCacheTests|FullyQualifiedName~PathBackedImageResourceIntegrationTests"
```

Expected: RED because cache/root loader integration does not exist.

- [ ] **Step 4: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\UI\Resources\ImageResourceCacheTests.cs tests\Cerneala.Tests\UI\Resources\PathBackedImageResourceIntegrationTests.cs
git commit -m "test: capture path backed image resource lifetime"
```

---

### Task 2: Add Backend-Neutral Image Resource Cache

**Files:**
- Modify: `UI/Resources/ImageResource.cs`
- Create: `UI/Resources/ImageResourceCache.cs`

- [ ] **Step 1: Expose safe resource identity**

Add read-only metadata to `ImageResource` such as:

```csharp
public string Identity { get; }
public bool IsPathBacked { get; }
public string? Path { get; }
public bool HasEmbeddedImage { get; }
```

Use names consistent with existing code. Do not expose mutable internals.

- [ ] **Step 2: Implement `ImageResourceCache`**

Behavior:

- accepts `IImageLoader`;
- resolves embedded-image resources directly;
- caches path-backed images by stable identity/path;
- optionally tracks owned loaded images;
- `Clear()` disposes owned images that implement `IDisposable`;
- `Remove(ImageResource)` or `RemovePath(...)` if tests need targeted invalidation.

- [ ] **Step 3: Keep `ImageResource.Resolve(...)` working**

Do not break existing callers. It may delegate to cache when a cache is supplied, but existing direct resolve tests should stay green.

---

### Task 3: Root And MonoGame Content Service Integration

**Files:**
- Modify: `UI/Elements/UIRoot.cs`
- Modify: `UI/Hosting/MonoGame/MonoGameContentServices.cs`
- Modify: `UI/Hosting/MonoGame/MonoGameUiHostOptions.cs`
- Modify: `UI/Hosting/MonoGame/MonoGameUiHost.cs`
- Create: `tests/Cerneala.Tests/UI/Hosting/MonoGameContentServicesLifetimeTests.cs`

- [ ] **Step 1: Add root-owned image resource services**

Add a small root API such as:

```csharp
public IImageLoader? ImageLoader { get; }
public ImageResourceCache? ImageResourceCache { get; }
public void SetImageLoader(IImageLoader? loader)
```

Changing loader should clear the old cache and invalidate resources/render where needed.

- [ ] **Step 2: MonoGame content services own image loader**

`MonoGameContentServices` should expose an `IImageLoader` created from `GraphicsDevice` or accepted through options.

- [ ] **Step 3: Host attaches services to root**

When root is set or host is constructed, attach image loader/cache services to the root. Keep the API explicit; do not use static globals.

- [ ] **Step 4: Add disposal tests**

Create tests:

```csharp
MonoGameContentServicesDisposesOwnedImageCache()
MonoGameUiHostDisposeDisposesContentOwnedResourcesOnce()
ReplacingRootReattachesContentServicesToNewRoot()
```

If real MonoGame texture construction is not reliable in tests, use fake content services or test root/cache behavior only.

---

### Task 4: Use Cache From `Image` Control

**Files:**
- Modify: `UI/Controls/Image.cs`

- [ ] **Step 1: Resolve through root cache when resource is path-backed**

When `SourceResourceId` is set:

- record dependency as today;
- retrieve `ImageResource` from the provider;
- resolve via `Root.ImageResourceCache` if available;
- fallback to direct `resource.Resolve()` for embedded image resources.

- [ ] **Step 2: Preserve intrinsic-size invalidation**

If the loaded image dimensions change after resource replacement, measure invalidation must occur when `UseIntrinsicSize == true`.

- [ ] **Step 3: Preserve fixed-size render-only invalidation**

If `UseIntrinsicSize == false`, resource replacement should invalidate render but not measure unless existing metadata says otherwise.

---

### Task 5: Verify GREEN And Regressions

- [ ] **Step 1: Run targeted resource tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~ImageResourceCacheTests|FullyQualifiedName~PathBackedImageResourceIntegrationTests|FullyQualifiedName~MonoGameContentServicesLifetimeTests"
```

Expected: GREEN.

- [ ] **Step 2: Run existing resource/render tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~ImageResourceInvalidationTests|FullyQualifiedName~HostResourceInvalidationIntegrationTests|FullyQualifiedName~ResourceRenderDependencyTests|FullyQualifiedName~ImageTests|FullyQualifiedName~AuthoringPreviewContractTests"
```

Expected: GREEN.

- [ ] **Step 3: Run boundary tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~ArchitectureBoundaryTests|FullyQualifiedName~MonoGameDependencyBoundaryTests"
```

Expected: GREEN.

- [ ] **Step 4: Run full suite**

```powershell
dotnet test Cerneala.slnx
```

Expected: GREEN.

- [ ] **Step 5: Commit implementation**

```powershell
git add UI\Resources Drawing\\MonoGame UI\Hosting\MonoGame UI\Elements\UIRoot.cs UI\Controls\Image.cs tests\Cerneala.Tests
git commit -m "feat: cache path backed image resources"
```
