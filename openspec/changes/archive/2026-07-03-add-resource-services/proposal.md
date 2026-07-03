## Why

Cerneala now has retained controls, text services, and render caches, but fonts and images are still passed as direct drawing handles or fallback font lookups. The next MVP step needs explicit typed resource identity and change notification so replacing an image or font invalidates only the dependent layout/render work.

## What Changes

- Add `UI.Resources` typed resource identity and provider/store APIs for fonts and images.
- Add observable resource change events and dependency tracking for retained elements.
- Add font and image resource wrappers that resolve to existing drawing handles without leaking backend objects into controls.
- Add an image loader abstraction and a MonoGame adapter that returns `IDrawImage`/`MonoGameImage` without exposing `Texture2D` to controls.
- Integrate resource dependencies with retained render dependency tracking, image controls, and text services.
- Preserve explicit host/service lookup and avoid WPF-style global resource dictionaries as core machinery.

## Capabilities

### New Capabilities
- `resource-services`: Typed resource ids, resource provider/store, resource change notification, dependency tracking, font/image resource wrappers, and image loader abstractions.

### Modified Capabilities
- `first-controls-panels`: `Image` and `TextBlock` shall support explicit resource-backed image/font dependencies and invalidate retained layout/render correctly when resources change.
- `text-services`: font resolution shall be able to resolve resource-backed fonts through explicit services without hidden global lookup.
- `retained-rendering-cache`: retained render dependencies shall include resource dependency versions so cached commands are invalidated when dependent font/image resources are replaced.

## Impact

- Affected code: `UI/Resources`, `UI/Controls/Image.cs`, `UI/Controls/TextBlock.cs`, `UI/Text/FontResolver.cs`, `UI/Rendering/RenderDependency.cs`, and MonoGame resource loading adapters.
- Affected drawing abstractions: `IDrawImage` and `IDrawFont` remain draw-level handles; controls and resource services must not depend on `Texture2D`, Skia, HarfBuzz, or `SpriteBatch`.
- Tests added under `tests/Cerneala.Tests/UI/Resources` with focused control invalidation coverage for image and font resource replacement.
