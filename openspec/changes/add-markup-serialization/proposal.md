## Why

Cerneala is now strongly code-first, retained, and typed enough to support an optional markup layer for tooling, samples, and designer workflows. The markup layer should describe retained trees and compile/load into the same typed object creation paths as handwritten code, without becoming a WPF-style runtime dependency or bypassing validation.

## What Changes

- Add a small backend-neutral markup document model, reader, writer, schema, type registry, diagnostics, and load options under `UI/Markup`.
- Add a `UiFactory` that creates retained `UIElement` trees from markup through explicit registered element/property factories.
- Add a `GeneratedUiFactory` runtime abstraction so generated or precompiled factories can produce retained trees through the same invalidation/render-cache paths as code-created trees.
- Add ergonomic metadata attributes for content properties and design-time-only members where they are useful to registries or future tooling.
- Add focused tests for markup reading, writing, diagnostics, and typed factory behavior.
- Defer the standalone source-generator project until runtime reflection or build-time generation becomes a proven cost; this change only creates the runtime seam for generated factories.

## Capabilities

### New Capabilities

- `markup-serialization`: Optional retained UI markup documents, deterministic serialization, explicit type/property registration, diagnostics, and typed factory creation.

### Modified Capabilities

- `typed-state-model`: Markup-created values must flow through the existing `UiObject.SetValue` typed property validation and coercion path.
- `retained-rendering-cache`: Trees produced by markup factories must invalidate and render through the same retained render-cache pipeline as code-created trees.

## Impact

- Adds `UI/Markup/*` runtime APIs.
- Adds tests under `tests/Cerneala.Tests/UI/Markup`.
- Updates `ROADMAPv2.md` section 25 as implemented runtime items are completed and explicitly leaves source-generation project files deferred.
- No new external dependencies are expected; parsing/serialization should use built-in .NET APIs.
