# Prism — unique foundation and catalog

## Purpose

Build the backend-neutral model, machine-readable catalog and contracts
version on which all other plans are based. The stage does not draw on
GPU and does not modify the CUI parser.

**Dependencies:** none.

## Stage 0 — decisions and baselines

- [x] Fix contradictions between
  `docs/prism-technical-design.md` and
  `docs/prism-markup-syntax-proposal.md`: first implementation includes cache
  retained cross-frame, but has no public registration of filters or shaders
  loaded by arbitrary name; preserves Photoshop syntax and behavior.
- [x] Explicitly documents the render order, default source, rule of
  leaf for layer, `@group`, mask, `ClipToBelow`, `PassThrough`,
  `Visible`, `Fill`, `Opacity`, `BlendIf`, default color `LinearSrgb` and
  the fact that Prism does not influence layout or input.
- [x] Inventory the public APIs that later plans will touch
  (`IDrawingBackend`, `IUiBackend`, `MonoGameUiHostOptions`) and save
  API baseline for final diff.
- [x] Add RED tests in `tests/Cerneala.Tests/UI/Prism/` for definitions
  immutables, bottom-up ordering, name uniqueness and structure validation
  layer/group/backdrop without adding GPU execution yet.

### Gate stage 0

- [x] The two design documents no longer contradict each other, and the RED tests
  it fails exclusively because the Prism model is missing.

## Stage 1 — DRY catalog

- [x] Create a single machine-readable source in
  `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json` and its scheme; the catalog
  includes stable identifiers, types, properties, default values,
  domains, units, capabilities, determinism and cacheability for
  filters, styles, blend modes, color profiles and sampling.
- [x] Includes in the catalog all Photoshop families approved in the proposal, without
  to copy the same lists in C#, shaders and documentation.
- [x] Add a deterministic generator/validator that produces the typed artifacts
  SourceGen, runtime and backend required; consumers use the same file
  physically, not manually synchronized copies.
- [x] Generate a catalog → runtime → kernel → test → coverage matrix
  documentation that build can fail when an entry has no owner.
- [x] Test for duplicate identifiers, incompatible defaults,
  invalid ranges, unknown properties, and nondeterministic output.

### Gate stage 1

- [x] A single build command regenerates/verifies all artifacts, and o
  intentionally incomplete input causes the catalog test to fail with diagnostic
  precisely

## Stage 2 — definitions model

- [x] Add to `UI/Prism/Definitions/` the immutable types
  `PrismCompositionDefinition`, `PrismNodeDefinition`,
`PrismLayerDefinition`, `PrismGroupDefinition`,
  `PrismBackdropDefinition`, mask/filter/style definitions and typed keys
  of parameters generated from the catalog.
- [x] Model the layer's children as separate collections of filters, styles, and
  at most one mask; does not allow a layer to be layer/group children.
- [x] Models the order declared only once and provides bottom-up enumeration
  without moving or duplicating nodes.
- [x] Validates optional composition-scoped unique names for access
  Motion and prohibits the use of names as arbitrary sources.
- [x] Keep `@backdrop` in a separate logical plane, but include it in the model
  the "maximum one, last direct child" invariant.
- [x] Move degradation policy to one
  `Drawing/Prism/Catalog/PrismFallbackPolicy`; definitions do not know MonoGame.
- [x] Make RED tests green and add structural equality tests, snapshot
  deterministic and diagnostic serialization.

### Gate stage 2

- [x] The model does not refer to `Microsoft.Xna.Framework.Graphics`, it does not contain state
  mutable per control and expresses all structural invariants without `object`
  or string lookups in the hot path.

## Step 3 — instance per element

- [x] Add to `UI/Prism/Runtime/` `PrismInstance`, the dense and typed deposit of
  parameters, `PrismStructuralVersion`, `PrismValueVersion` and status
  layer/group/backdrop addressable by generated keys.
- [x] Separates shared definition from instance values; no GPU resources
  is owned by `UIElement` or `PrismInstance`.
- [x] Implements `Visible`, `Opacity`, `Fill`, blend mode, advanced blending,
  `BlendIf`, `ClipToBelow`, mask/style/filter values and color profile as
  typed properties, with defaults exclusively from the catalog.
- [x] Increment structural version only when topology changes and
  version values only when data changes; the writings are identical
  no op.
- [x] Add tests for isolation between two controls that use the same
  composition, replacement, reset to defaults and lack of allocations after warmup
  for repeated typed updates.

### Gate stage 3

- [x] Two instances share the definition without sharing state, and the benchmark
  updater does not do string lookups and does not create objects per frame.

## Stage 4 — documentation and verification

- [x] If the model exposes public types, use the skill
  `writing-api-documentation` for pages from
  `docs-site/documentation/classes/` and synchronize the manifest; don't add
  API documentation in `docs/documentation/`.
- [x] Run RoslynIndexer reindex after every C# batch/project and at
  final, `doctor`.
- [x] Running
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter Prism` and
  `dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj`.
- [x] Run `git diff --check` and compare API diff to stage 0 baseline.

## The definition of done

- [x] The catalog is the only source of truth and validates all entries
  approved.
- [x] Definitions are immutable, instances are isolated, and no type of
  foundation does not depend on the GPU backend.
- [x] Targeted tests, public documentation and stage gates are green.