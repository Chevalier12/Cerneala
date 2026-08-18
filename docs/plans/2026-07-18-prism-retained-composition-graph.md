# Prism — retained rendering and composition graph

## Purpose

Inserts Prism scopes into the retained list of commands, parses a frame o
once and construct a backend-neutral graph. The stage does not run shaders
and has no GPU resources.

**Dependency:** `2026-07-18-prism-foundation-and-catalog.md`.

## Stage 0 — RED contracts

- [x] Add RED tests in `tests/Cerneala.Tests/Drawing/Prism/` for
  `BeginPrism`/`EndPrism`, nesting, clip, transform, opacity, Presence children
  and the fallback of backends without Prism.
- [x] Fix the order of `DrawCommandListBuilder` by tests: `PushClip`,
  `BeginPrism`, local orders/children/exiting children, `EndPrism`, `PopClip`.
- [x] Add RED tests for parsing: zero/one/many scopes, scopes
  invalid, stale version and backdrop request made at most once.
- [x] Add RED tests for Photoshop graph: bottom-up processing, groups,
  masks, clipping chain, invisible layer, `PassThrough` and separate backdrop.

### Gate stage 0

- [x] The tests describe backend-neutral semantics and do not make assertions on
  SpriteBatch, RenderTarget2D or the names of some shaders.

## Stage 1 — typed retained commands

- [x] Extends `Drawing/DrawCommandKind.cs` with `BeginPrism` and `EndPrism`.
- [x] Extends `Drawing/DrawCommand.cs` with a readonly payload and type
  `PrismDrawScope`, plus dedicated factories; do not use `object`, dictionary or
  string identifiers.
- [x] The payload contains only the state required for the frame: definition/instance,
  bounds, transform, pixel scale, structural/value versions and generation
  aggregated view of the captured subtree.
- [x] Includes a numerical `PrismCacheOwnerToken` and no reverse reference, thus
  so that the backend can index/invalidate entries without retaining the element.
- [x] Add in `Drawing/DrawCommandList.cs` a minimal structural version,
  deterministically updated to `Add`/`Clear`, to invalidate cached parsing.
- [x] Update all transform/translate switches and backends
  fake so that the scope is kept or explicitly ignored.

### Gate stage 1

- [x] The order list remains reusable and readonly for consumers,
  no generic metadata for a DAG that doesn't exist yet.

## Stage 2 — integration of the retained builder

- [x] Extends `UI/Rendering/DrawCommandListBuilder.cs` to output the exact scope
  around element rendering, after clip and before local controls.
- [x] Keep the transform and scope coordinates in sync with the same
  operations used for internal controls.
- [x] Completely excludes scope for non-Prism elements and states
  unprofitable; an invisible internal layer remains the graph's decision, not the builder's.
- [x] Checks the nesting of a Prism element in another Prism and Presence element
  exiting children without the wrong interleaving of Begin/End pairs.
- [x] If existing invalidation cannot separate composition from layout, add
  the smallest presentation-only category in the scheduler/retained cache and
  prove that it does not reconstruct `ElementRenderCache`. (It was not necessary:
  the existing Prism invalidation is already presentation-only, and the tests confirm
  reuse of `ElementRenderCache` and structural list.)

### Gate stage 2

- [x] Snapshots of orders are stable, pairs are balanced, and o
  parameter change Prism does not regenerate structural commands.

## Stage 3 — single frame analysis

- [x] Add `Drawing/Prism/Graph/PrismFrameAnalyzer` and the immutable result
  `PrismFrameAnalysis`, indexed by command position and list version.
- [x] The parser does a single pass, validates the nesting, and calculates
  active scopes, bounds, required surfaces, backdrops and capabilities.
- [x] Produces for each scope a backend-neutral `PrismDependencyStamp`,
  compact and without element references, using propagated retained versions
  incremental instead of an additional subtree traversal.
- [x] Reuses the same analysis in host and `PrismGraphBuilder`; forbids
  separate list scanning for backdrops or budgets.
- [x] Exchange `IDrawingBackend.Render` to get one
  `DrawingFrameContext` type carrying backdrop analysis and lease
  optional, no MonoGame dependency.
- [x] Update `RetainedRenderer`, `UiHost`, `IUiBackend`, backends,
  test doubles and call existing sites for the new contract.
- [x] Reject stale parsing when list version no longer matches and tests
  safe reuse of `DrawCommandList`.

### Gate stage 3

- [x] A frame is analyzed only once, the context does not hold resources over
  frame, and all non-Prism backends keep the previous output.

## Stage 4 — the semantic graph

- [x] Add `PrismGraphBuilder` with typed nodes/edges for capture, layer,
  group, filter, style, mask, clip-to-below, composite, color conversion and
  background input.
- [x] Assign each node stable structural identity and dependencies
  explicit pixel-affecting, necessary for the retained cross-frame cache.
- [x] Capture the control image once and process the results
  intermediate as explicit values; does not allow the layer an arbitrary source.
- [x] Build the children in bottom-up order, keeping the order declared for
  naming/diagnostics and isolating the non-`PassThrough` groups.
- [x] Model clipping chain and alpha mask separately; `Opacity` and `Fill` are
  distinct operations, according to Photoshop semantics.
- [x] Includes the backdrop node only as a separate input plane, no purchase or
  Host API at this stage.
- [x] Issue diagnostics with the name of the composition/node and the preserved source span
  of definition when a graph cannot be constructed.

### Gate stage 4

- [x] Golden graph snapshots confirm order, dependencies and count
  of captures for simple, nested, masked and clipped compositions.

## Stage 5 — secure optimization

- [x] Add `PrismGraphOptimizer` separately from builder; the optimizer does not modify
  definition or instance.
- [x] Mark deterministic nodes cacheable only when all resources and
  their values can participate in the dependency stamp; the others remain explicit
  uncacheable.
- [x] Removes proven no-op nodes, invisible layers and redundant conversions;
  merge steps only when the catalog declares equivalence.
- [x] Calculates extended bounds for blur, shadow, stroke and transform without
  affect layout/hitbox and without clipping needed pixels.
- [x] Estimate peak live surfaces through lifetime analysis and transmit a plan
  explicitly to the executor, without GPU allocation.
- [x] Add builder vs optimizer differential tests for alpha, blend order,
  masks, clipping and groups.

### Gate stage 5

- [x] The optimized graph is semantically equivalent to the raw one and no test is
  depends on the random order of the collections.

## Stage 6 — documentation and verification

- [x] Updates the pages with the `writing-api-documentation` skill
  `IDrawingBackend`, `IUiBackend` and all public frame context types;
  synchronize the manifest.
- [x] Run reindex after every C# batch/project.
- [x] Running
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "Prism|DrawCommand|RetainedRenderer"`
  and `dotnet test .\Cerneala.slnx`.
- [x] Runs `git diff --check` and the public API diff.

## The definition of done

- [x] The retained list expresses typed Prism scopes and remains compatible with
  backends that don't run effects.
- [x] The frame is analyzed only once, and the raw and optimized graph have
  non-GPU verified semantics.
- [x] Node identities and dependency stamps are stable, complete and
  ready to be consumed by cache without lookup string or UI references.
- [x] Tests, documentation and all gates are green.