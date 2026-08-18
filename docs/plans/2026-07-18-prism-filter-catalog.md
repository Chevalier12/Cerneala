# Prism — the complete filter catalog

## Purpose

Implements all approved Photoshop filters in the Prism catalog and links them
of parser, runtime, optimizer, kernels, diagnostics and tests without lists
manually duplicated.

**Dependencies:** `2026-07-18-prism-markup-motion-and-lifecycle.md` and
`2026-07-18-prism-monogame-compositor.md`.

## Stage 0 — completeness contract

- [x] Generate from `prism-catalog.json` the filter matrix → properties/defaults
  → planner → kernel → semantic test → golden → documentation.
- [x] Add a RED test that fails for each filter or property
  no implementation; do not maintain parallel allowlists in tests.
- [x] Classifies each filter by reusable primitives, extension
  bounds, sampling, format/color space, GPU capabilities, determinism,
  cacheability and resources to be versioned.
- [x] Defines for randomness filters an explicit seed and output
  deterministic; disallow current time or global RNG as hidden input.
- [x] Defines the policy for formats/capabilities unavailable via
  `PrismFallbackPolicy`, with observable diagnosis, no silent substitution.

### Gate stage 0

- [x] The full catalog automatically produces a finite list to deploy and build
  cannot turn green with a forgotten entry.

## Stage 1 — primitives and adjustment filters

- [x] Implements common primitives for matrix/curve/LUT, channel mapping,
  thresholds, histogram-free levels and color conversions.
- [x] Implements all adjustment/color filters declared in the catalog,
  using the same primitives and the same linear/premultiplied convention.
- [x] Generates parameter binding and domain validation in the catalog;
  the planner does not repeat defaults.
- [x] Add analytic vectors for opaque/transparent pixels, boundary values,
  individual channels and selectable color profiles.
- [x] Add goldens only for interactions that cannot be validated
  enough through vectors. (It was not necessary: all interactions of this
  families are sufficiently covered by analytical vectors.)

### Gate stage 1

- [x] All adjustments in the catalog have implementation, test and documentation, no
  duplicate gamma or alpha conversions.

## Stage 2 — blur, sharpen and noise

- [x] Implements common separable primitives for blur, convolution,
  neighborhood sampling and deterministic noise.
- [x] Implements all the Blur, Sharpen and Noise filters declared in the catalog,
  including variants that require a specialized kernel.
- [x] Calculate radius/bounds only once in planner and pass to kernel
  prepared parameters; the shader does not reinterpret the markup semantics.
- [x] Choose the sampling/passes strategy according to capabilities and size without
hidden quality degradation or non-benchmarked numerical thresholds.
- [x] Test edge sampling, alpha edges, zero/maximum radii, seed, small images
  and nested color profiles.

### Gate stage 2

- [x] The result is deterministic, bounds do not cut pixels, and the optimizer
  only removes math no-op filters.

## Step 3 — distort, transform and resampling

- [x] Implements common primitives for coordinate mapping, displacement,
  polar/cartesian transform, wrap/clamp/mirror and sampling quality.
- [x] Implements all the Distort filters and transformations in the catalog,
  including entries that require multiple passes.
- [x] Keep visual transform in Prism: don't propagate new dimensions into
  measure/arrange and doesn't change the hitbox.
- [x] Validate auxiliary resources defined by the approved syntax without a
  reintroduces a generic property `Source` or shader filename.
- [x] Test negative coordinates, extreme scales, edges, transparency,
  nested transforms and clipped/masked compositions.

### Gate stage 3

- [x] All distortion filters have mapping and sampling verified, and the input
  control remains the default source.

## Stage 4 — stylize, pixelate, render and the rest of the catalog

- [x] Implement the missing primitives for edge detection, morphology,
  quantization, tiling, procedural patterns and the necessary multi-pass operations.
- [x] Implements all Stylize, Pixelate, Render and any other filters
  approved family in the proposal/catalogue; no entry remains "TODO".
- [x] Reuse style/filter primitives when the math operation is
  identical, but keep separate layouts when the public semantics differ.
- [x] Test procedural determinism, alpha, bounds, chaining order,
  group isolation and interaction with mask/clipping/blend.
- [x] Generate a conformance gallery from the same catalog list without
  manually written views per filter.

### Gate stage 4

- [x] Catalog matrix reports zero filters/properties without planner,
  kernel, test and documentation.

## Stage 5 — optimizer and performance

- [x] Mark only the safely mergeable operations in the catalog and check
  differential fused output versus separate passes.
- [x] Remove no-op filters by actual typed values and keep order
  for non-commutative operations.
- [x] Profiles simple, chained and nested representative scenes; measure passes,
  peak surfaces, CPU submit, GPU time, hit/miss retained and allocations after warmup.
- [x] Enter thresholds or public limits only based on benchmarks and
  document the reason; do not add unwanted quality presets/adaptive quality.
  (It wasn't necessary: the measurements justify structural gates, but they don't
  public time limits portable between GPUs.)
- [x] Check thousands of animated frames for bounded surface reuse and lack
  recompiling the shaders or building the graph per non-structural change.

### Gate stage 5

- [x] Optimization preserves conformance and static and animated scenarios
  respects the budgets established by measurements.

## Stage 6 — documentation and verification

- [x] Generates the reference of filters/properties/defaults from the catalog and
  keep handwritten conceptual explanations separate from generated data.
- [x] Use the skill `writing-api-documentation` for any public type and
  synchronize `docs-site/documentation/manifest.json`. (Audit performed with
  the skill; no change required: batch does not change the public API,
  and the manifesto has all 926 existing pages.)
- [x] Run reindexing after every C# batch/project.
- [x] Running
  `dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj --filter Prism`,
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter PrismFilter`
  and `dotnet test .\Cerneala.slnx`.
- [x] Runs the gallery via the automated capture API and `git diff --check`.

## The definition of done

- [x] Each approved filter and property in the catalog has full path from
  kernel markup, diagnostics, test and documentation.
- [x] No parallel list, third party public extension or runtime shader source
  was introduced.
- [x] Conformance and benchmarks are green.