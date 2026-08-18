# Prism — markup, Motion and lifecycle

## Purpose

Compiles the Prism syntax into type definitions, attaches the instance to the element, and
allows Motion to animate Prism properties without reflection or lookup string to
runtime.

**Dependency:** `2026-07-18-prism-foundation-and-catalog.md`.

## Stage 0 — RED contracts

- [x] Add RED fixtures in `tests/Cerneala.Tests.SourceGen/Prism/` for
  resources `<PrismComposition>`, `@prism $Resource(...)`, inline compositions and
  exactly the eight approved directives.
- [x] Covers the full syntax: typed parameters, layer/group bottom-up,
  filter/style/mask, ultimate backdrop, photoshop properties and color profile.
- [x] Add RED cases for unknown directives, wrong properties,
  duplicate names, missing parameters, layer with child layer, multiple backdrops
  and the backdrop which is not the last.
- [x] Add RED fixtures for Motion paths
  `$self.prism.Layer.Property`, `$owner.prism.Layer.Property` and
  `$Name.prism.Layer.Property`, including non-existent names/properties.

### Gate stage 0

- [x] Tests separate parse, binding and emission errors, check spans
  accurate and do not fail due to invalid unrelated CUI fixtures.

## Stage 1 — parser and AST

- [x] Extends the existing infrastructure `UiMarkupDirectiveParser` for
  block delimitation, common expressions and values; keep Prism grammar in
  `Cerneala.SourceGen/Prism/Syntax/`, not in a second general parser.
- [x] Creates an internal and small Prism AST that preserves the declared order,
  source spans and form expressions, without importing runtime types.
- [x] Only supports Prism language catalog directives and only children
  legal for each context.
- [x] Add stable diagnostics `PRISM1xxx` for lexer/parse and tests
  snapshot for the text and location of each diagnosis.

### Gate stage 1

- [x] Parser recovers from an error without unnecessary cascades and reuses
  common CUI syntax instead of duplicating rules for `{}`, `=` and expressions.

## Stage 2 — static binder

- [x] Add in `Cerneala.SourceGen/Prism/Binding/` resource resolution,
  parameters, layer/group/backdrop names and properties generated from
  catalogue.
- [x] Validate types and conversions at compile time; emits no reflection,
  dictionary string or `dynamic` for valid cases.
- [x] Resolves filter/style/mask properties directly to typed keys and emits
  `PRISM2xxx` for invalid symbols, types, domains, and capabilities.
- [x] Validate nesting, backdrop order, `ClipToBelow` no layer
  lower and name collisions before emission.
- [x] Test access from template/resource scope and two instances of
  the same resource with different parameters.

### Gate stage 2
- [x] All approved examples from the proposal compile, and all examples
  declared illegal receive a single useful diagnosis at the correct source span.

## Stage 3 — emission and attachment

- [x] Generates in `Cerneala.SourceGen/Prism/Emission/` immutable definitions
  shared and factories by `PrismInstance`; does not generate GPU graph or MonoGame code.
- [x] Add an internal `PrismAttachment` to `UI/Prism/Runtime/` that implements
  `IElementLifecycleBehavior`, model `MarkupMotionSession`.
- [x] Attach only one Prism instance per element, handle replacement
  deterministic and removes all references to detach/dispose.
- [x] Binds dynamic expressions to typed slots and unbinds
  when the element or template is removed from the tree.
- [x] Add generated-source and runtime tests for attach, detach, reattach,
  replacement, template recycling and two different roots.

### Gate stage 3

- [x] The generated code does not contain the dispatch string in the hot path, and 10,000
  attach/detach cycles do not keep Prism elements or instances alive.

## Stage 4 — Motion integration

- [x] Extends the existing Motion resolver with the segment `.prism` and generates
  static instance access, named node, and property key.
- [x] Reuses the Motion scheduler, specs, cancellation and lifecycle;
  Prism does not introduce a second animation engine.
- [x] Allows animation of numeric, color, bool/Visible and properties
  enums only where the catalog defines interpolation or shift
  discreet.
- [x] Invalidates presentation/composition only for value changes;
  it does not rebuild the layout, hitbox, `ElementRenderCache` or topology
  graph if the structural version has not changed.
- [x] Add RED/green tests for `$self`, `$owner`, named element, undo,
  replace, Hidden/Collapsed and writing an already current value.

### Gate stage 4

- [x] A Prism animation runs without structural rebuild and is canceled o
  only when the owner becomes unprofitable or leaves.

## Stage 5 — visibility and memory

- [x] Connects `Visible`, `Hidden`, `Collapsed`, detach and disposal to the same
  lifecycle policy; the unrenderable state produces zero tick Motion and zero
  Prism invalidation.
- [x] Ensures deterministic resuming when returning to the tree: base values and
  bindings are reapplied, but aborted executions do not revive themselves.
- [x] Add repeated navigation tests between chapters, hide/unhide, resource
  replacement and GC with `WeakReference`.
- [x] Measure allocations after warmup for an animated parameter and prove that
  no closures or collections occur per frame.

### Gate stage 5

- [x] Tests do not find Motion left active, references held or Prism working
after hiding or detaching the owner.

## Stage 6 — documentation and verification

- [x] Update the proposal and TDD only where the implementation clarified
  semantics already approved; any change in language goes back to design,
  it is not slipped through the code.
- [x] Document all new/changed public APIs in
  `docs-site/documentation/classes/` with the skill
  `writing-api-documentation` and synchronize the manifest.
- [x] Run mandatory reindexing after every C# batch/project.
- [x] Running
  `dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj`,
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "Prism|Motion"`
  and `git diff --check`.

## The definition of done

- [x] Approved syntax compiles to typed code, with accurate diagnostics for
  all invalid forms.
- [x] Motion, bindings and lifecycle use the existing infrastructure,
  no parallel engine or workaround in view.
- [x] Source generation, invalidation, memory and lifecycle tests are green.