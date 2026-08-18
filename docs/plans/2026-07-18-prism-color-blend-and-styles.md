# Prism — color, blending and Photoshop styles

## Purpose

It delivers color semantics, blending, masks and the ten layer families
approved styles, over working GPU composer.

**Dependencies:** `2026-07-18-prism-markup-motion-and-lifecycle.md` and
`2026-07-18-prism-monogame-compositor.md`.

## Stage 0 — coverage matrix

- [x] Generates the complete matrix for color profiles, blend from the catalog
  modes, advanced blending, `BlendIf`, masks, clipping and style types.
- [x] Add for each entry at least one RED semantic test and one case
  visually representative; the tests do not manually repeat defaults from the catalog.
- [x] Defines the golden contract: size, format, premultiplied alpha,
  color profile, seed, tolerance per channel and supported hardware/driver.
- [x] Add small analytical images for cases where the result can be
  calculated exactly, not just "look good" screenshots.

### Gate stage 0

- [x] Array automatically fails if a catalog entry has no kernel, test and
  associated documentation.

## Stage 1 — color and alpha pipeline

- [x] Implements the conversions generated for `LinearSrgb` by default and
  approved selectable profiles at composition entry and exit.
- [x] Defines a single internal convention for premultiplied alpha and
  apply it in capture, filters, styles, masks, blends and present.
- [x] Separates `Opacity` from `Fill` so that layer styles follow the semantics
  Photoshop.
- [x] Test transparent colors, edge pixels, zero/one alpha, round-trip,
  nested compositions with different profile and lack of double conversion.
- [x] Check CPU/GPU differences on reference vectors and document
  justified numerical tolerance.

### Gate stage 1

- [x] All fundamental kernels have the same color/alpha convention, and
  tests detect halos and double application of gamma.

## Stage 2 — Photoshop blending

- [x] Implements all blend modes declared in the catalog, grouped by
  common primitives generated, not by separate shader copied for each mode.
- [x] Implements advanced blending and `BlendIf` with channels, thresholds and
  the transitions defined in the proposal/catalogue.
- [x] Respects group isolation and `PassThrough`, bottom-up order and
  the distinct combination of layer opacity and fill.
- [x] Add analytical tests for each blend mode on opaque pixels,
  transparent and partially transparent.
- [x] Adds visual conformance for group, mask, clipping chain combinations,
  nontrivial styles and blend modes.

### Gate stage 2

- [x] Each blend mode in the catalog has kernel and green test, without fallback
  silent at `Normal`.

## Step 3 — masks and clipping

- [x] Implements the real layer mask, its transform, opacity/density,
invert and feather according to catalog properties.
- [x] Implements `ClipToBelow` as lower layer alpha chain,
  independent of the mask and without turning the layer into a container.
- [x] Optimize the mask identity/zero cases and clipping absent only after
  equivalence tests.
- [x] Extend bounds for feather and check sampling at edges without a
  change the layout or hitbox.
- [x] Add goldens for mask + style, mask + transform, clipping chain and
  nested groups.

### Gate stage 3

- [x] Mask and clipping have distinct, correct and stable results, including at
  partial alpha and extended bounds.

## Stage 4 — layer styles

- [x] Implements common internal primitives for distance/edge field,
  contour, gradient/pattern sampling and compositing; each style declares itself
  only the specific plan.
- [x] Declare determinism, cacheability and resource versions for each
  style/primitives, generated from the catalog and consumed by the stamp dependency.
- [x] Implement from catalog `DropShadow`, `InnerShadow`, `OuterGlow`,
  `InnerGlow`, `BevelEmboss`, `Satin`, `ColorOverlay`, `GradientOverlay`,
  `PatternOverlay` and `Stroke`.
- [x] Respect the Photoshop order between styles, `Fill`, layer content and
  opacity, including multiple styles of the same type if the proposal them
  allows.
- [x] Calculate bounds for shadow/glow/bevel/stroke using the same primitives
  used by the optimizer; don't duplicate formulas in backend and analyzer.
- [x] Add tests for all generated properties/defaults and goldens
  for each family, plus mask/clipping/blend combinations.

### Gate stage 4

- [x] All ten families in the catalog are implemented, animated by
  slots typed and covered by tests without unnecessary duplicate shader/source.

## Stage 5 — performance and verification

- [x] Profiles scenes with many layer styles and proves the reuse of surfaces,
  absence of readback and zero managed allocations after warmup.
- [x] Check optimizer: remove invisible/no-op styles but keep
  order and alpha for all combinations tested.
- [x] Updates the public documentation with the skill
  `writing-api-documentation` and the manifest for the types/properties
  exposed.
- [x] Run reindexing after every C# batch/project.
- [x] Running
  ZZZ BLACK30ZZZ
  and `dotnet test .\Cerneala.slnx`.
- [x] Run all captures through the automated API and `git diff --check`.

## The definition of done

- [x] The catalog matrix is complete for color, blending, masks,
  clipping and all styles.
- [x] Analytical and visual conformance, performance, API docs and gates are
  green