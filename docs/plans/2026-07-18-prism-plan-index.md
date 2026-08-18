# Prism — deployment plan index

## Purpose

This index transforms decisions from
[`docs/prism-technical-design.md`](../prism-technical-design.md) and
[`docs/prism-markup-syntax-proposal.md`](../prism-markup-syntax-proposal.md)
in a verifiable order of implementation. Prism remains visual processing:
does not change layout, hitbox or input routing.

The plan is split for SourceGen, the retained runtime, the GPU executor, and
the backdrop can have its own tests and gates. It does not implement "everything
into a large class', and the independent stages are not forced into one
logical branch.

## Binding decisions

- The public syntax has only the directives `@prism`, `@parameter`, `@layer`,
  `@group`, `@filter`, `@style`, `@mask` and `@backdrop`.
- The reusable resource is called `PrismComposition`; instances are created
  per element, and compiled definitions are immutable and shareable.
- The control image is the default source. A `@layer` is leaf, a
  `@group` can contain layers or groups, and the first element declared is
  in front. Composition evaluation is done from the bottom up.
- A `@backdrop` is optional, unique, and the last direct child of Prism.
- Catalog of types, properties, default values, identifiers and
  capabilities has a single machine-readable source.
- The first implementation does not expose the public SDK for third-party filters and does not compile
  shaders at runtime.
- Stable GPU Prism results are preserved and reused between frames per
  the basis of a complete dependency stamp, under explicit budget and without references to UI.
- Invisible, `Hidden`, `Collapsed` or detached means zero Prism work and
  cancellation of the associated Motion.
- Backends that don't implement Prism ignore visual scope and render
  normal internal contents.

## Orders and dependencies

1. [Foundation and catalog](2026-07-18-prism-foundation-and-catalog.md) — without
   dependencies. DONE

   **Model:** `gpt-5.6-sol` · **Reasoning:** `xhigh`

2. [Markup, Motion and lifecycle](2026-07-18-prism-markup-motion-and-lifecycle.md)
   — depends on the foundation. DONE

   **Model:** `gpt-5.6-sol` · **Reasoning:** `max`

3. [Retained rendering and composition graph](2026-07-18-prism-retained-composition-graph.md)
   — depends on the foundation; can advance in parallel with the markup. DONE

   **Model:** `gpt-5.6-sol` · **Reasoning:** `ultra`

4. [MonoGame](2026-07-18-prism-monogame-compositor.md) Composer — depends
   of the composition graph. DONE

   **Model:** `gpt-5.6-sol` · **Reasoning:** `max`

5. [Colour, blending and styles](2026-07-18-prism-color-blend-and-styles.md) —
   it depends on the markup and composer. DONE

   **Model:** `gpt-5.6-sol` · **Reasoning:** `max`

6. [](2026-07-18-prism-filter-catalog.md) filter catalog — depends on
markup and compositor; can advance in parallel with the styles. DONE

   **Model:** `gpt-5.6-sol` · **Reasoning:** `xhigh`

7. [Backdrop and host integration](2026-07-18-prism-backdrop-hosting.md) —
   depends on composition graph and composer. DONE

   **Model:** `gpt-5.6-sol` · **Reasoning:** `max`

8. [Cache retained GPU](2026-07-18-prism-retained-pixel-cache.md) — depends on
   graph, compositor, full visual catalog and backdrop. DONE

   **Model:** `gpt-5.6-sol` · **Reasoning:** `ultra`

9. [Integration and hardening](2026-07-18-prism-integration-and-hardening.md) —
   depends on all previous plans. DONE

   **Model:** `gpt-5.6-sol` · **Reasoning:** `max`

## Global gates

- [x] Before the first Prism code, harmonize the two source documents:
  retained cross-frame cache is mandatory, and third-party public extensions
  are explicitly postponed, without changing the approved grammar.
- [x] Do not start a dependent plan until all its plan gates
  prerequisites are checked and their target tests are green.
- [x] For each C# or project change, run immediately:
  `dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- index .\Cerneala.slnx --json`.
- [x] For each new or changed public API, update to the same
  stage `docs-site/documentation/classes/` with the skill
  `writing-api-documentation` and sync
  `docs-site/documentation/manifest.json`.
- [x] No GPU stage starts before CPU model tests and ale
  backend-neutral graph to be green; a screenshot does not replace a test
  semantically.
- [x] No cross-frame hit cache is supported based on a hash only;
  the cache-on output must be identical to the cache-off, and the dependency stamp
  must include every pixel-affecting input.
- [x] All visual checks use the existing capture API
  `IWindowPlatform.RenderPng`/Automation Presentation, not screenshots
  made by hand.
- [x] Any workaround in `CernealaPresentation` for a problem a
  the framework blocks the gate; the invariant is fixed in the layer that
  owns
- [x] At the end of each plan run `git diff --check` and check explicitly
  no spawn files, shader binaries, or ownerless changes.

## Stop conditions

Implementation stops and the decision returns to the design documents if:

- the requested contract cannot be expressed through the eight approved directives;
- an optimization changes the order of Photoshop, the alpha or the result of the masks;
- a third party public API would only be needed to avoid an internal extension
  simple;
- a numerical budget must be guessed before there are measurements;
- the host cannot provide the backdrop without GPU synchronization or CPU readback.

## The definition of done

- [x] All nine plans are fully checked, in order of dependencies.
- [x] `dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj`,
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj` and
  `dotnet test .\Cerneala.slnx` are green.
- [x] Generated matrix catalog → parser → runtime → kernel → test → documentation
  contains no missing entries.
- [x] Lifecycle, memory, device reset, performance and conformance tests
  visual are green on supported WindowsDX configuration.
- [x] A static Prism produces hit retained without recapture or effect passes,
  and any changed pixel-affecting input produces misses and correct output.
- [x] RoslynIndexer `doctor` and full reindexing are green, documentation
  public is synchronized and `git diff --check` reports no problems.
