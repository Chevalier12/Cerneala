## 1. Resource Core

- [x] 1.1 Create `UI/Resources/ResourceId{T}.cs` with typed key identity and equality semantics.
- [x] 1.2 Create `UI/Resources/IResourceProvider.cs` with explicit typed lookup APIs.
- [x] 1.3 Create `UI/Resources/ResourceChangedEventArgs.cs` carrying resource id, old value, new value, and version.
- [x] 1.4 Create `UI/Resources/ResourceStore.cs` with typed set/get/try-get behavior and no-op replacement handling.
- [x] 1.5 Add `tests/Cerneala.Tests/UI/Resources/ResourceIdTests.cs`.
- [x] 1.6 Add `tests/Cerneala.Tests/UI/Resources/ResourceStoreTests.cs`.

## 2. Resource Dependency Tracking

- [x] 2.1 Create `UI/Resources/ResourceDependencyTracker.cs` for recording dependent retained elements or dependency owners.
- [x] 2.2 Ensure resource replacement updates dependency versions for affected resource ids.
- [x] 2.3 Extend `UI/Rendering/RenderDependency.cs` so retained rendering can include resource dependency identity/version without backend objects.
- [x] 2.4 Add `tests/Cerneala.Tests/UI/Resources/ResourceDependencyTrackerTests.cs`.
- [x] 2.5 Add retained rendering tests for resource dependency cache staleness and cache reuse.

## 3. Font Resources

- [x] 3.1 Create `UI/Resources/FontResource.cs` that resolves to `IDrawFont`.
- [x] 3.2 Update `UI/Text/FontResolver.cs` to resolve resource-backed fonts through explicit resource services.
- [x] 3.3 Ensure replacing a font resource invalidates dependent text measurement and render work.
- [x] 3.4 Add `tests/Cerneala.Tests/UI/Resources/FontResourceInvalidationTests.cs`.
- [x] 3.5 Extend text service tests for font resource cache identity changes.

## 4. Image Resources

- [x] 4.1 Create `UI/Resources/ImageResource.cs` that resolves to `IDrawImage`.
- [x] 4.2 Create `UI/Resources/IImageLoader.cs` for explicit image loading.
- [x] 4.3 Create `UI/Resources/MonoGame/MonoGameImageLoader.cs` that returns `IDrawImage`/`MonoGameImage` without exposing `Texture2D` to controls.
- [x] 4.4 Update `UI/Controls/Image.cs` to support explicit resource-backed image usage while preserving direct `IDrawImage` source support.
- [x] 4.5 Ensure fixed-size image resource replacement invalidates render only.
- [x] 4.6 Ensure intrinsic-size image resource replacement invalidates measure and render.
- [x] 4.7 Add `tests/Cerneala.Tests/UI/Resources/ImageResourceInvalidationTests.cs`.

## 5. Architecture Boundaries

- [x] 5.1 Extend architecture boundary tests so `UI/Resources` core does not reference MonoGame, Skia, HarfBuzz, `Texture2D`, or `SpriteBatch`.
- [x] 5.2 Extend architecture boundary tests so `UI/Controls` still does not reference backend resource types.
- [x] 5.3 Ensure MonoGame-specific image loading lives only under `UI/Resources/MonoGame` or existing drawing MonoGame adapters.

## 6. Roadmap And Verification

- [x] 6.1 Update `ROADMAPv2.md` section 12 checklist as files, tests, and acceptance items are completed.
- [x] 6.2 Verify `openspec validate add-resource-services --strict` passes.
- [x] 6.3 Verify `dotnet build Cerneala.slnx -warnaserror` passes.
- [x] 6.4 Verify `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj` passes.
