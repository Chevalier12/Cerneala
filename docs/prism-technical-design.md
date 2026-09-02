# Prism Technical Design Document

## Status

This document describes the technical architecture implemented for Prism in Cerneala.
The markup compiler, lifecycle, composition graph, MonoGame and SDL_GPU executors,
shared HLSL color pipelines/blending/masks/styles, backdrop, diagnostics and
cross-frame GPU retained caches are implemented and tested. The two desktop
executors are covered by native smoke and pixel-difference gates.
The final results are in
[`2026-07-21-prism-integration-hardening.md`](../benchmarks/Cerneala.Benchmarks/results/2026-07-21-prism-integration-hardening.md),
and the user agreement is summarized in [`prism-guide.md`](prism-guide.md).

Prism composers beyond the delivered MonoGame/WindowsDX and SDL_GPU backends, the
public SDK for third-party operations, compilation of shaders at runtime, adaptive
quality, async compute and a generic GPU scheduler are explicitly deferred.
Formulations about them are design ideas, not delivered behavior or hidden work.

The markup contract and mental model are defined in
[`prism-markup-syntax-proposal.md`](prism-markup-syntax-proposal.md). The catalogue
standard of filters, styles, blend modes, color profiles and sampling is
machine-readable file
[`prism-catalog.json`](../Cerneala.SourceGen/Prism/Catalog/prism-catalog.json).
This TDD explains how that contract is compiled, executed, and
diagnosed.

In case of contradiction:

- the proposal has priority for the syntax and behavior observable by the author;
- this TDD takes priority for segregation of responsibilities and implementation
  internal;
- a contradiction must be resolved in both documents before implementation.

## Classification of decisions

The document explicitly separates four types of claims:

| Type | It means |
| --- | --- |
| Confirmed Requirement | behavior required by the proposal and the Photoshop model |
| Technical decision | chosen internal solution for confirmed requirements |
| Hypothesis to be validated | choice that needs a prototype, benchmark or conformance test |
| Conditional Optimization | not implemented until profiling demonstrates the need |

`BeginPrism`/`EndPrism`, immutable definitions, render graph, backend-owned GPU
execution and cross-frame retained GPU caches are technical decisions. The planning
parallel, asynchronous execution and public API for third-party filters are
optimizations or conditional extensions, not requirements of the first implementation.

For the first implementation, the normative scope is explicit: stable GPU results
can be reused between frames based on a complete dependency stamp, and
the built-in catalog is closed to public record. Semantic form
`@filter Name` does not promise discovery or application-provided kernels. These
decisions do not change the Prism grammar.
An assumption does not become default and a conditional optimization does not enter the criteria of
acceptance without measurable proof and an updated decision in this document.

## Executive summary

Prism is a declarative visual compositor for an element's local visual
UI. The control's own commands are captured only once as an image by
base, processed through a composition of layers and then drawn without changing
layout or hitbox. Visual descendants are not captured.

The implementation is divided into four areas:

1. **Source generator**: parses markup, validates types and generates
   immutable definitions and typed Motion targets.
2. **Runtime UI**: create a lightweight instance per element, keep the parameters,
   versioning and lifecycle, but does not own GPU resources.
3. **Drawing composition**: transport through `DrawCommandList` Prism scopes
   backend-neutral and builds a tidy render graph.
4. **GPU backend executors**: MonoGame/WindowsDX and SDL_GPU execute the same graph,
   own backend-specific shaders, temporary surfaces, retained caches, color
   management and backdrop resources.

The main structural decision is the use of two balanced commands:
```text
BeginPrism
    comenzile locale ale elementului
EndPrism
comenzile descendenților vizuali
```
The backend can thus capture the local visual of the control without
`OnRender` custom, screenshots, recursion in view or knowledge of the UI tree.

## Objectives

- Full implementation of `PrismComposition` and `@prism` syntax.
- Layer model mentally compatible with Photoshop.
- All standard filters, styles and blend modes in the proposal.
- Masks, clipping chains, groups, Blend If and advanced blending.
- Real game backdrop and UI rendered underneath.
- Typed integration with Motion and binding.
- No changes to measurement, arrangement or hit testing.
- No GPU resources owned by UI elements.
- Zero CPU readback for captures and backdrop.
- Local retained controls reusable during Prism animations.
- Sufficient diagnostics for CPU, GPU, memory and cache cost.
- Deterministic results between frames and predictable between backends.

## Non-objectives

- Prism is not an image editor.
- Prism does not execute the shader source entered in the markup.
- Prism does not implement Neural Filters, Camera Raw, Digimarc or cloud operations.
- Prism does not change the layout to accommodate shadows or blur.
- Prism does not create independent input elements for layers.
- Prism does not allow a layer to arbitrarily read another layer via `Source`.
- Prism does not guarantee identical byte-for-byte results to Adobe's proprietary algorithms.
- Prism does not move rendering responsibilities to controls or code-behind.

## Language surface

TDD implements exactly the seven directives defined by the proposal:

| Directive | Technical role |
| --- | --- |
| `@prism` | attaches a reusable or inline definition to an element |
| `@parameter` | declares a typed overwritable and addressable slot |
| `@layer` | declares a leaf node that processes the lower result |
| `@group` | declares an explicit container for layers and groups |
| `@filter` | adds a pixel processing operation |
| `@style` | adds a Photoshop decoration derived from content |
| `@mask` | limits the full contribution of a scope |

`PrismComposition` is the reusable resource. No additional directive is
required for the implementation described here.

## Existing constraints

Cerneala is currently using:

- `ElementRenderCache` for the local commands of each element;
- `DrawCommandListBuilder` for composing subtrees in a flat list;
- `RetainedRenderer` for commit and submit;
- `IDrawingBackend` as backend-neutral boundary;
- `MonoGameDrawingBackend` and `SdlGpuDrawingBackend` as concrete desktop backends;
- `UiHost.Update` and `UiHost.Draw` as a frame contract;
- Motion and markup generator for animated properties and static targets.

Prism needs to extend this architecture, not create a second UI renderer.

## Semantic contract consumed by runtime

The full normative contract is the section
`Foundation Rendering Contract` from the proposal. The runtime, the analyzer and the backend
must keep the same rules, without local interpretations:

- the declared order is front-to-back as in the Photoshop panel, and the evaluation
  normal is bottom-up;
- the default source is a single immutable capture of the local visual of
  control, without visual descendants, and node names cannot become sources;
- the layer is the leaf, the group is the only container, and the mask is applied
  contribution prepared before opacity and blend;
- `ClipToBelow`, `PassThrough`, `Visible`, `Fill`, `Opacity` and `BlendIf` keep
  exactly the order and semantics defined in the proposal;
- the default profile is `LinearSrgb`;
- Prism does not participate in layout, hit testing or input.

This summary is an implementation constraint, not a second definition of
behavior. Any semantic change is first made in the normative contract
from the proposal and is reflected here in the same amendment.

## Mandatory principles

### The UI describes, the backend processes

The UI element knows what `PrismComposition` has attached and what the current values are.
It doesn't know what a `RenderTarget2D`, `Effect`, shader pass or texture pool is.

### Shared definition, state per instance

`PrismCompositionDefinition` is immutable and can be shared by all
elements that use the same resource. Each element gets its own
`PrismInstance` and own parameter values.
### No recapture per layer

The control's local visual is executed only once in an area of
basis. Layers process intermediate results, do not redraw the control.

### GPU-only for pixels

Captures, filters, masks, blend modes and the backdrop remain on the GPU. Reading
pixels back to the CPU is forbidden in the normal rendering path.

### No textual lookup per frame

The generator turns names into numeric identifiers and typed keys.
Strings remain only for diagnostics and tooling.

### Safe degradation

An unavailable capability should not make the UI invisible. In the worst case,
Prism is bypassed and the control is drawn normally.

## Application of DRY, YAGNI and SOLID

### DRY

- The machine-readable catalog is the only source for identifiers, properties,
  defaults, limits and capabilities. The generator, runtime, backend and
  documentation consumes artifacts generated from it.
- Visibility, culling, structural bounds and backdrop requirement are calculated o
  only once in `PrismFrameAnalysis`. The graph builder consumes the result; no
  repeat the analysis.
- Fallback rules are centralized in a `PrismFallbackPolicy`, no
  scattered in kernels, planner and host.

### YAGNI

The mandatory implementation contains only mechanisms required by the confirmed syntax and
the current MonoGame and SDL_GPU backends. It does not include:

- public discovery for third-party filters;
- parallel planning;
- a generic model of GPU fences or asynchronous execution;
- automatic adaptive quality degradation.

These remain possible through stable identifiers and internal borders, but se
design only after the occurrence of a real case and measurements. It doesn't build
a station for the train that may someday pass through the commune.

Retained cross-frame cache is not hypothetical flexibility: it is a requirement
fixed so that a static Prism doesn't recapture and redraw the same pixels
in every frame. Its implementation remains strict and specialized for results
GPU Prism; it does not justify a generic caching or scheduling framework.

### SOLID no mania interface

- **SRP**: the analyzer analyzes, the graph builder builds, the optimizer
  optimizes, the executor executes, and the pool owns surfaces.
- **OCP**: built-in operations are registered via descriptors and schedulers
  specialized; adding one does not expand a giant central switch.
- **LSP**: Any backend without Prism can ignore scopes and draw content
  normally, without changing its semantics.
- **ISP**: the backdrop remains a separate contract; `IDrawingBackend` does not receive
  methods for each filter, style, or resource.
- **DIP**: UI and drawing composition depend on backend-neutral contracts;
  MonoGame and SDL_GPU implement those contracts and each owns its GPU details.

Interfaces are introduced only at boundaries with real substitution, different lifecycle or
clear need for test doubles. Simple inner classes do not get an interface of
for the sake of the suit and tie.

## High level architecture
```text
.crn
    |
    v
Prism parser + semantic binder
    |
    v
cod generat
    |
    +--> PrismCompositionDefinition partajată
    +--> factory PrismInstance
    +--> chei Motion tipizate
    |
    v
UIElement + PrismAttachment
    |
    v
BeginPrism / local commands / EndPrism / descendant commands
    |
    v
PrismFrameAnalyzer
    |
    v
PrismGraphBuilder
    |
    v
PrismGraphOptimizer
    |
    v
PrismGraphExecutor
    |
    +--> MonoGame executor / MGFX artifacts
    +--> SDL_GPU executor / SPIR-V, DXIL or MSL artifacts
    +--> backend-owned transient surface pool and retained cache
    +--> shared diagnostics contract
    |
    v
GraphicsDevice
```
## Code organization

The recommended structure is:
```text
UI/Prism/
    Definitions/
    Runtime/
    Motion/
    Diagnostics/

Drawing/Prism/
    Commands/
    Graph/
    Catalog/
    Color/
    Hosting/

Drawing/MonoGame/Prism/
    Execution/
    Kernels/
    Shaders/
    Surfaces/
    Diagnostics/

Cerneala.Backends.SdlGpu/Prism/
    executor and device resources
    Shaders/

Cerneala.SourceGen/Prism/
    Syntax/
    Parsing/
    Binding/
    Emission/

tests/
    Cerneala.Tests/Prism/
    Cerneala.Tests.SourceGen/Prism/
    Cerneala.Tests.MonoGame/Prism/
    Cerneala.Tests.SdlGpu/Prism/
```
Responsibilities should not be moved between these directories just for convenience.
In particular, `UI/Prism` cannot reference MonoGame or SDL.

The backend-neutral shader math lives under `Drawing/Prism/Shaders/Hlsl/`.
MonoGame keeps only its `.fx` wrappers and techniques: `CopyComposite.fx`, the
independent `Styles.fx` package, and the specialized catalog wrappers. SDL_GPU
keeps its entry-point wrappers and versioned SPIR-V/DXIL/MSL artifacts under
`Cerneala.Backends.SdlGpu`. Both build paths consume the shared HLSL modules.
Their build targets track the precise dependency set of each output so a style
module rebuilds the style package without forcing an unrelated filter package.

## The definitions model

### PrismCompositionDefinition

The reusable definition contains:

- stable identifier;
- declared parameters and default values;
- working color profiles;
- global light angle and altitude;
- ordered list of layers and groups;
- implicit destination-backdrop requirements inferred from visible operations;
- table of addressable names;
- table of property slots;
- structural hash for pipeline cache;
- bounds maximum static expansion, when it can be calculated.

The definition is immutable after creation. It does not contain a reference to the element that o
use and do not implement `IDisposable`.

### Knots

The definition tree uses distinct types:

- `PrismLayerDefinition`;
- `PrismGroupDefinition`;
- `PrismFilterDefinition`;
- `PrismStyleDefinition`;
- `PrismMaskDefinition`.

`PrismLayerDefinition` is always leaf. Only `PrismGroupDefinition` can
contains layers or other groups.

Each node receives a stable numeric `PrismNodeId` within the definition. The name
optionally it is kept separately for Motion and diagnostics.

### Properties and parameters

Each value is represented by a typed slot:
```text
PrismPropertyKey<T>
PrismParameterKey<T>
```
Slots are dense and grouped by common types:

- `bool`;
- `int`;
- `float`;
- vectors and matrices;
- colors;
- enums;
- immutable references to images, gradients, patterns, LUTs or curves.

Parameters are not dictionaries by `string -> object`. Complex values are
typed and validated references.

## PrismInstance

An instance is created for each `@prism` application.

Contains:

- the reference to the shared definition;
- typed states and overridden parameter values;
- structural and value versions.

`PrismAttachment` holds the attachment state and subscriptions created by the binding
factories. `MarkupMotionSession` owns the Motion executions and handles. Separation
this makes `PrismInstance` a lightweight value model with no delegates or
element references.

Does not contain:

- textures;
- render targets;
- shader instances;
- filtered results;
- references to the MonoGame backend.

### PrismRenderState

`PrismRenderState` is the backend-neutral handle referenced by the `BeginPrism` command.
It is stable during attachment and contains:

- the immutable definition;
- the dense buffer of values;
- `ValueVersion`;
- `VisibilityVersion`;
- `ResourceVersion`.

Values ​​can only be changed on the UI thread between update and draw. The backend
reads them synchronously in `Render`. The current implementation plans and executes on
the draw thread; does not introduce an asynchronous CPU pipeline or mutable state
shared between frames.

This separation allows Prism animations to change parameters without regeneration
local control commands.

## Attachment and lifecycle

`PrismAttachment` implements the standard element lifecycle behavior.

### Attach

When attaching:

1. Deterministically replaces and disposes any previous Prism attachment;
2. record a single `PrismAttachment` as lifecycle behavior;
3. create `PrismInstance` from the factory generated when the element enters the tree;
4. connect binding factories only if the element is actually renderable;
5. return clean if the instance factory or a binding factory fails.

Further integration with drawing attaches the same transition
`PrismRenderState`, allocates `PrismCacheOwnerToken` without dereference to element
and invalidates only the root composition structure. These responsibilities
they are not moved to `PrismAttachment`.

### Detachment

On posting:

1. the Motion session cancels the executions associated with the subtree;
2. the attachment removes bindings and subscriptions in reverse order;
3. remove the current instance and all lifecycle references to disposal;
4. the drawing integration publishes the token to a backend-neutral invalidation queue
   consumed on the next submit;
5. does not directly contact the backend and does not directly release GPU resources.

The backend indexes the retained entries by token/generation and does not hold a reference
strong at `PrismInstance` or `UIElement`. If there is no more submit, dispose of it
backend flushes the entire cache anyway.

### Visibility

When the element or an ancestor becomes `Hidden`, `Collapsed` or
ZZZINCERNEALA19ZZZ:

- Motion for Prism is canceled synchronously and only once per lifecycle
  existing of the subtree;
- subscriptions generated by binding factories are disconnected;
- no Prism commands are executed;
- no backdrop is purchased;
- no surfaces are allocated;
- the cache token is marked only once as immediately evictable;
- the old instance can only remain as an inert state and no longer receives writes.

When the item becomes effectively returnable:

- the generated factory creates a new instance;
- binding factories reconnect and reapply core values and values
  current sources;
- Canceled Motion executions do not revive and must be explicitly triggered again.

When `Visible=false` on a layer or group:

- the node and its subtree are bypassed;
- filters, styles and mask don't work;
- Active motion for that scope's properties is cancelled;
- an external write or bind can set `Visible=true` again.

A filter or style with `Visible=false` is removed from the plan without pass and without
intermediate surface. A backdrop with `Visible=false` does not contribute to the requirements
frame for backdrop acquisition.

`Visible=false` is not equivalent to `Opacity=0`. The first stops work; the second
preserves composition semantics and can be used for transitions.

## Invalidation

Prism introduces a presentation override that does not rebuild local commands.

| Change | Effect |
| --- | --- |
| Attachment, detachment or other definition | structural recompilation of root command list |
| Numeric parameter, color, `Opacity`, `Visible` | increment `ValueVersion`, redraw |
| Changed auxiliary resource | increment `ResourceVersion`, redraw |
| Bounds or transform element | normal recomposition via layout/render scope |
| Change control content | rebuild existing local cache |

The usual `Render` invalidation for each tick of a parameter is not used
Prism, as this would unnecessarily rebuild `ElementRenderCache`.

A `Composition` category is added to the scheduler or an equivalent signal of
presentation-only. Hosts that draw each frame may treat it as a statistic;
on-demand hosts use it to schedule draw.

## Integration into DrawCommandList

### New orders

`DrawCommandKind` receives:
```text
BeginPrism
EndPrism
```
`BeginPrism` contains a backend-neutral `PrismDrawScope`:
```text
PrismRenderState
PrismCacheOwnerToken
ControlBounds
EffectiveTransform
PixelScale
StructuralVersion
ValueVersion
VisualContentVersion
```
`EndPrism` has no payload. Scopes must be balanced and can be nested.

### Composing the local visual

`DrawCommandListBuilder.AppendElement` issues:
```text
clip-uri de ancestor deja active
 PushClip for the element, if present
 BeginPrism, if the element has Prism
comenzile locale
 EndPrism, if the element has Prism
copiii vizuali
copiii Presence aflați în exit
PopClip
```
Thus:

- Prism captures only local control commands;
- descendants are normally composed over the Prism result and do not receive default
  the effects of control;
- explicit clips limit the final result;
- the effects can extend the result beyond the arranged bounds if there is no clip;
- Prism attached to descendants is evaluated independently;
- the backend must not know `UIElement`.

A backend without Prism support ignores `BeginPrism` and `EndPrism` but executes the commands
among them. The control remains visible with no effects.

## The frame contract

`IDrawingBackend` receives explicit context:
```csharp
public interface IDrawingBackend
{
    void Render(
        DrawCommandList commands,
        in DrawingFrameContext frame);
}
```
`DrawingFrameContext` contains:
```text
UiFrameId
ViewportSize
CoordinateScale
OutputColorProfile
PrismFrameAnalysis?
PrismCacheInvalidations
BackdropFrameLease?
DiagnosticsSink
```
The existing context-free signature is replaced. All backends and test
doubles must be updated in the same change.

Before the backdrop acquisition, `PrismFrameAnalyzer` makes the only structural pass
over controls, clip stack and Prism states. It produces a `PrismFrameAnalysis`
immutable containing visible scopes, culling results, bounds
structural, dependency stamps, cacheable nodes and `RequiresBackdrop`.

The host uses `RequiresBackdrop` for acquisition, then passes the same analysis
by `DrawingFrameContext`. The graph builder consumes it without recomputing
visibility, culling or backdrop requirement. In debug, command list versions
and analysis are checked to prevent using a stale analysis.
Owner/resource invalidations are drained only once in the context of the frame
and consumed by the backend before the lookup.

## SpriteBatch Ownership

`MonoGameUiHost` no longer opens a single `SpriteBatch.Begin` around the whole
UI. Prism must be able to:

- change the render target;
- finish a batch before a filter pass;
- perform one or more full-screen passes;
- resume drawing normal commands;
- restore GraphicsDevice state.

Therefore, `MonoGameDrawingBackend.Render` becomes the full owner of
`SpriteBatch.Begin/End` and transitions between passes.

The backend saves and restores the state that the host contract declares:

- render targets;
- viewport;
- rectangular scissors;
- blend, rasterizer, depth-stencil and sampler states.

Restore is done in `finally`, including when a kernel fails.

## Building the render graph

The pipeline has separate responsibilities:

1. `PrismFrameAnalyzer` parses the structure of scopes, resolves visibility,
   culling and frame requirements.
2. `PrismGraphBuilder` transforms commands and analysis into a semantic graph
   immutable.
3. `PrismGraphOptimizer` removes no-ops and merges passes without changing
   semantic order.
4. `PrismGraphExecutor` runs the graph and manages GPU resources exclusively.

The semantics of layer styles is translated by `PrismStylePlanner` into a common plane of
sampling and composition. The generated catalog descriptor provides the slots,
defaults, determinism, cacheability and dependency versioning. The backend
consume the plane with a single technique `LayerStyle`; the ten families do not have
shader sources copied separately.

The main nodes are:

- batch primitives;
- clip;
- capture control;
- filter pass;
- style pass;
- mask pass;
- blend pass;
- color conversion;
- background input;
- composite finish.

The analyzer, builder and optimizer are backend-neutral. Work with
catalog descriptors and does not reference MonoGame.

### Scope Prism

At `BeginPrism`, the graph builder memorizes the underlying composition node and starts
capture control. At `EndPrism`:

1. completes control capture;
2. prepare the backdrop, if any;
3. evaluate the bottom-up control stack;
4. compose the processed control over the backdrop;
5. replace scope with end result.

### Nested Prism

An inner scope is resolved before the outer scope. The interior backdrop
it sees the composite node immediately before its control, including the game, content
parent and lower siblings. It doesn't see its own result or the top UI.

## Composition evaluation

The order in the markup is the Photoshop panel order:

- the first statement is visually in front;
- the last statement is visually behind;
- the execution starts from the bottom and goes up.

For a layer:
```text
rezultatul acumulat dedesubt
    -> filtre bottom-up
    -> prepared content
    -> Fill
    -> styles bottom-up
    -> mask
    -> ClipToBelow
    -> Opacity
    -> Blend If
     -> blend with the lower result
```
### Groups

`PassThrough` allows children to interact directly with the external output.
Any other blend mode isolates the group in a surface, applies the filters, styles,
mask and opacity of the group, then blends it as a single image.

### Clipping chains

A layer with `ClipToBelow=true` uses the alpha of the lower base-layer of the same
scope. The chain ends at the first unclipped lower sibling. The generator rejects
baseless chains.

### Fill and Opacity

`Fill` only reduces prepared content. Styles remain visible.
`Opacity` applies to the full content-plus-styles result.

### Blend If

`ThisLayerRange` and `UnderlyingRange` are transformed into feathered ramps. Evaluation
it is done in working color profile, on the selected channel, before the final blend.

## The catalog of operations

The full list and defaults are kept in the proposal and are not duplicated in the
this document.

The internal implementation groups operations by reusable GPU primitives:

- color and LUT conversions;
- color matrix and curves;
- convolutions;
- separable blur;
- neighborhood and morphology;
- resampling and transform;
- displacement and distortion;
- noise and procedural generation;
- edge detection;
- alpha derivation;
- distance field for styles;
- blend kernels.

A semantic filter can produce one or more passes. Markup does not expose
this difference.

### Built-in registry

`PrismBuiltinCatalog` is the single source for:

- the semantic name;
- the stable identifier;
- filter or style category;
- their properties and types;
- the defaults;
- the valid intervals;
- the necessary capabilities;
- the bounds expansion strategy;
- backend kernel key.
The catalog is described in a JSON file validated by JSON Schema and included as
`AdditionalFile` for source generator. Runtime descriptors are generated from it
type, the backend registry, and the catalog tables in the proposal. CI regenerates
artifacts and fail if diff. Do not manually maintain separate lists in
documentation, generator and backend.

### Public extensibility deferred

The syntax reserves the simple form:
```text
@filter ChromaticAberration
```
The first implementation does not publish attributes, assembly discovery, kernel factories or
a third-party SDK. The internal registry serves the built-in catalog exclusively.
Stable identifiers and syntax do not block a future extension, but the API does
public is only projected when there is at least one real filter outside
framework and we know its packaging, backend and lifecycle requirements.

The markup does not support shader source, `Program`, effect filenames or `$Filter`.

## Compiling the markup

The source generator pipeline is:
```text
XML + directive text
    -> Prism lexer/parser
    -> Prism syntax tree
    -> semantic binder
    -> catalog validation
    -> symbol table
    -> C# emitter
```
### Parser

The Prism parser is separate from the Motion parser, but reuses the infrastructure
existing for:

- locations in the file;
- literal values;
- references `$`;
- assignments with `=`;
- blocks with `{}`;
- diagnostic reporting.

The AST preserves the exact location of each directive, property, and value.

### Binder

Binder solves:

- resources `PrismComposition`;
- parameters and overrides;
- filter and style types;
- properties and their types;
- the layer/group/backdrop namescope;
- the order and legality of the nodes;
- references to images, masks, LUTs, gradients and patterns;
- Motion targets `.prism`.

All structural validation in the proposal is build-time.

### Generated code

For a reusable resource, generate:

- a shared static definition;
- a court factory;
- a typed structure for overrides;
- numeric identifiers for nodes and parameters.

For `@prism $Name(...)` it generates:

1. creation of the court;
2. application of typed overrides;
3. attachment to the element;
4. registration of the cleanup in the lifecycle.

No reflection, string parsing or dictionaries are generated per frame.

## Motion integration

The target:
```text
$self.prism.Highlights.SoftGlow.GlowRadius
```
is resolved at build-time in:
```text
element target
PrismNodeId path
PrismPropertyKey<float>
```
The generator validates:

- the existence of Prism on the element;
- each group/layer/backdrop segment;
- the final property or parameter;
- value type;
- the existence of the Motion mixer.

The prefixes `$self`, `$owner`, and `$Name` are statically resolved. `$owner` is valid
only in a template component and uses the owner element kept by the context
of emission; `$Name` must be in the same namescope. The issued code accesses
directly the instance, `PrismNodeId` and the typed slot, without reflection or dispatch
textually.

### Discreet writings

`Visible`, supported booleans, integers, and enums use writes
discreet. They can be set by Motion, but are not interpolated. The numbers and
colors use existing continuous Motion mixers. For fade it animates
`Opacity`.

The Motion Prism binding is identified by the generated ID element and property.
Replacing the instance removes the old binding and creates one for the instance
current. An identical write does not change `ValueVersion` and produces zero invalidations
presentation.

### Cancellation

- detaching the element cancels all Prism targets;
- `Hidden`, `Collapsed` and `IsVisible=false`, including an ancestor, cancel
  synchronous Motion for subtree;
- returning to the runnable state does not restart a canceled execution;
- hiding a group cancels Motion for its descendants;
- hiding a layer or backdrop cancels Motion from that scope;
- Motion is not kept alive just because the definition is reusable.

## Color management

The default is `LinearSrgb`.

The pipeline is:
```text
 source profile
    -> working profile Prism
     -> filters, styles and blending
    -> output profile al hostului
```
The final conversion is done only once.

### Representation of pixels

- alpha premultiplied;
- intermediate floating point calculations;
- zero-alpha guard to unpremultiply;
- HSL blend modes work on non-associated color;
- masks are treated as scalar data, not as reinterpreted color images.

The contract uses the profile declared by `PrismRendererOptions.HostColorProfile`
both for the pixels captured from the host and for the presentation destination.
The default value is `Srgb`. The generic conversion goes from the host profile to the
working profile and back without intermediate clamp. A nested composition is
presented once in the host's profile, then the parent's input conversion
run once; don't apply gamma twice and don't reinterpret by default
scRGB as sRGB.

All passes receive and output premultiplied RGBA. Conversions do
unpremultiply only applies the transfer or matrix, and produces alpha zero
mandatory RGB zero. `Fill` scales content before layer styles;
`Opacity` scales the full result by styles and mask.

### MonoGame formats

The order implemented is:

1. `HalfVector4` for all Prism intermediates;
2. host destination format for presentation (default `Color` for SDR,
   floating-point RGBA for scRGB);
3. scalar format for masks when the platform supports it;
4. `Color` for masks when there is no renderable scalar format.

`ScRgb` is linear BT.709/D65 and retains negative RGB values or above `1` in
`HalfVector4`; alpha remains premultiplied linear coverage. When the host profile
is `ScRgb`, the host must provide and present floating-point RGBA and
configure the scRGB swapchain (`1.0` corresponds to 80 nits). The Prism option declares
pixel semantics, but cannot convert an 8-bit backbuffer to an HDR one.
A backend without floating-point format must report the missing capability and
to bypass, not silently compress HDR to SDR.

### Numerical tolerance

The CPU reference uses `double`; the shaders use `float`, the intermediates
`HalfVector4`, and the golden output is `R8G8B8A8_UNorm`. The WindowsDX gate
supports a maximum of `2/255` per channel. Threshold covers half-float rounding,
transfer evaluation on GPU and final UNorm quantization, but it is enough
strictly to detect halos, a missing conversion or double application of gamma.
Alpha zero and associated RGB cleanup remain accurate, not just within tolerance.

## Blend modes

The register builds one `${BlendMode}Blend` technique for each symbol
generated from the catalog. Missing technique is package initialization error of
shader-e; the executor does not remap any unknown modes to `Normal`.

The shader uses common primitives for separable modes, brightness,
saturation, `ClipColor`, `SetLuminosity` and `SetSaturation`. Technique wrappers
I choose the primitive, without independent copies of the whole shader. HSL mods do
unpremultiply guard to alpha zero and reassociate the result before writing.

For premultiplied source and background, with straight values `Cs`, `Cb` and
alpha `As`, `Ab`, the common composition is:
```text
Ao = As + Ab - As * Ab
Co = Cs * As * (1 - Ab)
   + Cb * Ab * (1 - As)
   + B(Cb, Cs) * As * Ab
```
The intermediates remain `HalfVector4`, and the pass writes all premultiplied RGBA.
The full set of blend kernels requires profile `ps_4_0` and feature level `10_0`;
these values ​​are part of the WindowsDX conformance manifest.

`BlendChannels` independently selects straight RGB and alpha channels between
composite result and background. `Knockout` replaces the overlapping contribution with
straight color of the source; the structural difference between `Shallow` and `Deep` is
stored in the graph and becomes observable when traversing groups. The flags which
affect styles, masks and clipping are snapshot-look at the end node of
composition, not reread from UI state in executor.

`BlendIf` produces two linear ramps for each interval
`(blackStart, blackEnd, whiteStart, whiteEnd)`: rise from zero to one between
black thresholds, plateau, then drop from one to zero between white thresholds.
Ramps `ThisLayerRange` and `UnderlyingRange`, evaluated on the selected channel in
working profile, multiply and scale the source contribution before
final composition.

All mods should be tested with:

- alpha zero and one;
- partial alpha;
- black, white and values ​​above one in HDR;
- source and destination with different profiles;
- `Fill`, `Opacity`, masks and clipping chains.

`Dissolve` uses the deterministic hash of the pixel coordinate, the identifier
layer and `DissolveSeed`. The normalized seed is sent explicitly to the shader,
and the same input produces the same pattern between frames. He is not allowed to
flicker.

The order remains bottom-up. A group with `PassThrough` transmits the external background
children; any other blend mode is an isolation boundary and composes the group as a
single image. `Fill` scales content before styles, and `Opacity`
scale the full contribution before the blend.

## Styles

Styles use alpha prepared content and do not recapture control. The graph
preserve this pre-`Fill` input by the edge `StyleSource`, separated from `Content`,
so `Fill=0` hides the content, not the styles. `Opacity` stays behind
the full content-plus-styles result.

`PrismStylePlanner` directly consumes the generated typed slots and produces blueprints
for `DropShadow`, `InnerShadow`, `OuterGlow`, `InnerGlow`, `BevelEmboss`,
`Satin`, `ColorOverlay`, `GradientOverlay`, `PatternOverlay` and `Stroke`.
The executor packs the plan for a single GPU technique `LayerStyle`, and
the registry validates all ten catalog identifiers to the same kernel.

Internal primitives include:

- alpha dilation and erosion;
- an approximately common edge/distance field;
- common blur and offset sampling;
- contour lookup;
- gradient and pattern sampling;
- highlight/shadow lighting;
- premultiplied RGBA composition with blend modes.

`BevelEmboss` remains a single semantic style; Contour and Texture are
subcomponents of the plane, not hidden layers. Gradient/pattern resources go into
dependency stamp with their version; an active resource without a stable version does
non-cacheable node.

The same function `PrismStylePlanner.ExpandBounds` is consumed by the optimizer for
shadow, glow, bevel and stroke, and the executor uses the same geometry of
sampling. Formulas are not duplicated in the analyzer or backend.

Multiple instances of the same style are stored and executed bottom-up in
the order stated.

## Masks

The mask is evaluated by content and styles, before opacity and blend.

The steps are:

1. solving the image;
2. conversion of channel `Alpha` or `Luminance`;
3. `Invert`;
4. feathers;
5. density;
6. multiplying the full contribution.

Feather increases the sampling bounds, but not the layout.

The missing resource produces diagnostic and a completely opaque mask so that the control
it doesn't disappear by accident.

## Backdrop

Backdrop is an internal destination input, not an authoring node. The frame analyzer
requests it lazily when a visible layer, group, or style uses a non-normal blend.
The graph keeps each node's captured control source separate from its accumulated
destination. Pass-through groups inherit that destination; isolated groups expose
it only at their outer composition boundary.

### Hosting contract

The host exhibits:
```csharp
public interface IBackdropFrameSource
{
    bool TryAcquire(
        in BackdropFrameRequest request,
        out BackdropFrameLease frame);
}
```
The lease contains:

- `IBackdropSurface`;
- `ContentVersion`;
- size in pixels;
- screen-to-surface transformation;
- the color profile.

`MonoGameUiHostOptions` gets a `IBackdropFrameSource?`.
### Purchase

`UiHost.Draw`:

1. run once `PrismFrameAnalyzer`;
2. if the analysis requires backdrop, call `TryAcquire` at most once;
3. enter the lease in `DrawingFrameContext`;
4. run the backend;
5. release the lease in `finally`.

A hidden backdrop, clipped-out or belonging to an invisible element does not trigger
purchase.

### Composition

The host surface is imported read-only into the render graph. The lower UI is
added to paint order. Each backdrop reads the bottom node exactly from the point
its scope.

Feedback is impossible because a node can only depend on created nodes
previously.

### Host without backdrop

When the source is missing or refuses to purchase:

- only the backdrop plan is omitted;
- the control stack runs normally;
- `BackdropUnavailable` is issued only once per definition and host status;
- pixels from the previous frame are not used.

## Render targets and pooling

`PrismSurfacePool` is owned by the backend.

A surface key includes:

- width and height;
- format;
- mip count;
- sample count;
- usage flags;
- color profile class.

Surfaces are returned to the pool after the last use in the graph. Reuse se
do only after the MonoGame backend guarantees that the GPU is no longer using them,
using the safe recycling policy provided by current MonoGame capabilities.
TDD does not introduce a generic abstraction of fences for hypothetical backends.

`PrismRendererOptions` exposes the implemented configuration for:

- the total hard budget of all Prism surfaces;
- the soft budget and the maximum number of entries for the retained cache;
- activation of development dependency-diff diagnostics.

The reference benchmark fixes 512 MiB hard, 256 MiB retained soft and 256
entries. Unit tests use injected small bounds. Transient pressure
first evacuates non-forgotten retained entries; if the hard limit still can't
admits the necessary area, the executor reports `PRISM7006` with
`SurfaceAllocationFailed`, restore target and host state, release
leases and continues the remaining gross internal orders. There is no override
hidden or quality adaptive system.

## Caches

### Pipeline cache

The key contains:

- the structural hash of the definition;
- backend and capability set;
- working/output profile class;
- quality level;
- shader package version.

Changing parameter values ​​does not recompile the pipeline.

### Share backdrop in frame

Within a single frame, the shared key contains:

- the identity of the lower node;
- `ContentVersion`;
- lower UI versions;
- the extended region;
- pixel scale and downsample level;
- color profiles;
- the filter prefix and its values;
- mask, if it affects the cache-look result.
A downsample pyramid or a common blur prefix can be reused. The tint, the mask
or different opacity are not mistakenly pushed to the same input. All entries
and leases of this structure expire at the end of the current draw.

### Cache retained cross-frame

The first implementation preserves GPU Prism results between frames when all
dependencies that can change pixels are stable. The cache is backend-owned and
separate from the transient pool:

- the transient pool recycles scratch surfaces without keeping the content;
- the retained cache keeps the content of a surface and its dependency stamp;
- a surface can be promoted from transient to retained only after completion
  successfully of the node;
- the eviction returns the surface to the pool or releases it according to the budget.

`PrismFrameAnalyzer` produces a compact `PrismDependencyStamp` with no references to
UI elements. The stamp includes:

- the structural version of the composition and the stable identity of the node;
- the version of the Prism values ​​or the fingerprint of the pixel-affecting values;
- the unique, unused token of the attachment and the version of the visual result
  local captured;
- for backdrop, provider identity, `ContentVersion` and all versions
  lower UI nodes;
- the identities and versions of images, masks, LUTs, patterns and
  auxiliary resources;
- rasterized bounds, pixel scales and the transformations that change the sampling;
- working/output color profile, surface format and sampling quality;
- backend capability set and shader-e package version.

The local visual version is maintained incrementally in retainedUI: a property
render-affecting, Motion, or an element resource increments the local generation.
Descendant changes do not invalidate the ancestor's Prism capture.

`PrismGraphOptimizer` explicitly marks cacheable nodes. A node is eligible
only if the operation is deterministic do all its resources have versions and the key
contains all dependencies. The current time, a variable default seed, a provider
no stable `ContentVersion` or an unknown capability make the node uncacheable.

The executor first checks the final result. A final hit jumps the capture
control and all passes covered. On miss, check intermediate nodes and
can only remove prefixes covered by valid hits. A new result is
promoted after complete execution; a failed frame does not pollute the cache.

Eviction is LRU byte-budgeted and only runs on entries that are not
pin-look at the current draw. Entries do not contain owner, binding, Motion handle or
delegates. Detachment and replacement of the composition invalidate the generation of the owner;
device loss, shader package, viewport, output profile and resource replacement
invalidate the affected entries. `Hidden`/`Collapsed` do zero lookup and zero
promotion, and their entries become immediately evictable.

A single accountant applies the cumulative hard budget for transient and retained.
The retained cache has a soft head and is the first to be evicted when a correct pass has
need transient memory. Frame correctness takes precedence over
hit-rate, without exceeding the hard cap.

No generic abstraction of cache, task graph or fences is introduced. The cache is
specialized for GPU Prism surfaces and adheres to the synchronous backend model
Current MonoGame.

### Implementation and budgets confirmed

The MonoGame implementation separates `PrismRetainedSurfaceCache` from `PrismSurfacePool` and
use a common accountant for transient and retained surfaces. The configuration
public is `PrismRendererOptions`, sent directly to the builder
`MonoGameDrawingBackend` or via `MonoGameUiHostOptions.PrismRendererOptions`.
The default measured values are:

- 512 MiB for `SurfaceHardByteLimit`, applied to all Prism surfaces;
- 256 MiB for `RetainedCacheSoftByteLimit`;
- 256 for `RetainedCacheEntryLimit`;
- dependency-diff diagnostics turned off by default.

Limits remain configurable and are validated before the executor is created.
The retained limit or entry limit set to zero prevents promotion. The mode
cache-off exists only internally for conformance, diagnostics and benchmark; no
add directive, layer property or dialect markup.

`PrismRendererDiagnostics` exposes immutable snapshots with final hits and
intermediate, miss/promotion/eviction and their reasons, bytes and current entries,
peak bytes, missed entries and catches/passes saved. Classification of the difference
de dependency stamp is calculated only when development diagnostics are turned on;
the default path doesn't build diffs and doesn't allocate for them per frame.

The Release WindowsDX benchmark ran three times on the NVIDIA RTX 2000 Ada la
256 x 144 and 640 x 360, for 12 scenarios with on/off cache and 96 frames each
measured after warmup. All work counters, cache, surfaces, allocations and
eviction were identical between runs. Static hits have `0 B` managed,
static control reduced the GPU upper limit from 2,332 ms to 0,340 ms, and
24 common instances from 46.503ms to 0.991ms at medium resolution. The cases
dynamics explicitly report bookkeeping allocations and do not claim hits.
Full matrix, scaling, justifying budgets and dogfood gate are in
[`2026-07-21-prism-integration-hardening.md`](../benchmarks/Cerneala.Benchmarks/results/2026-07-21-prism-integration-hardening.md).

## Bounds and clipping

Each kernel declares an expansion function:
```text
Expand(inputBounds, parameterValues) -> outputBounds
```
Examples:

- shadow: offset plus spread and blur radius;
- Gaussian blur: support radius;
- transform: bounds of transformed corners;
- displacement: maximum displacement;
- color adjustment: zero expansion.

Operation planners declare the expansion, and the graph builder propagates the bounds
bottom-up. The executor allocates only the resulting region. Clamping to the viewport se
do after expansion.

Prism does not change:

- `DesiredSize`;
- `ArrangedBounds`;
- the hitbox;
- the input route;
- focus or accessibility.

Explicit clips of the element and ancestors apply to the final result.

## Mandatory optimizations

### Fusion pass

Combine when the result remains the same:

- consecutive color matrices;
- opacity and color multiply;
- adjacent color conversions;
- certain blend and mask operations;
- no-op filters with default values.

Do not change the semantic order to save passes.

### Blur

- separable kernel;
- controlled downsample for large radii;
- correct padding;
- success of the pyramid;
- quality level included in the cache key.

### Batching

Consecutive primitive commands that do not cross a scope, clip, or dependency
barrier remain batch-uible.

### The no-op path

A composition where all nodes are hidden or no-op must reduce to
normal control drawing without offscreen capture.

For layer styles, the graph builder does not emit the `Visible=false` states anymore
the optimizer removes only passes proven no-op by the generated joint scheduler
from the catalog. `Opacity=0` is enough for every family with only one
contribution; `BevelEmboss` is no-op only when both highlight opacity and
shadow opacity are zero. Aliasing is done to input `Content`, remove
The `StyleSource` left without a consumer and keeps the order of the other styles,
`Fill`, layer opacity, mask, clipping and blend.

## Errors and fallbacks

| Situation | Behavior |
| --- | --- |
| Invalid markup | build-time diagnostic, no invalid Prism code |
| Kernel built-in missing | operation bypass, runtime diagnostic |
| Backdrop unavailable | omit backdrop, normal control |
| Custom profile unavailable | composition bypass, no silent reinterpretation |
| Hard surface limit exceeded | eviction retained nepin-uit; then `PRISM7006`/`SurfaceAllocationFailed`, restore host and retry remaining raw internal commands |
| Shader compilation/package missing | bypass operation and diagnosis only once |
| Device reset/loss | draining GPU resources and lazy recreation |
| Exception in Executor | restore states, normal control when it can be resumed safely |

No showing corrupt partial results or recycling an old backdrop.

## Diagnostics

### Build-time

Diagnostics use the prefix `PRISM`.

Recommended groups:

- `PRISM1xxx`: syntax;
- `PRISM2xxx`: layer/group/backdrop structure;
- `PRISM3xxx`: catalog and properties;
- `PRISM4xxx`: resources and color profiles;
- `PRISM5xxx`: Motion targets;
- `PRISM6xxx`: static limits and known capabilities.

Each diagnostic indicates the exact location and provides a message describing the fix,
not just that the parser got upset.

### Execution diagnostics

Internal operational view, enabled only for development diagnostics,
expose per frame:

- compositions encountered and performed;
- layers/groups visible and bypass-oite;
- number of planned, merged and executed passes;
- control captures;
- background acquisitions;
- cache hits and misses;
- surfaces in use and peak bytes;
- processed pixels;
- shader switches;
- fallbacks and missing capabilities;
- CPU time for planning;
- GPU time when the platform provides timestamp queries.

The `PrismRendererDiagnostics` public API exposes immutable snapshots with
cache counters, catches/passes saved and surface usage. The details
of graph, Motion, backdrop and failure path remain in the deterministic internal view,
which writes unstable GPU identifiers and does not keep UI elements alive.
The default path does not build dependency diffs and has zero allocations per frame
after warmup for static Prism.

### Graph dump

Diagnostics can produce a deterministic textual dump:
```text
Prism CardGlass
  Backdrop Glass
    Downsample x2
    GaussianBlur radius=24
    Color saturation=1.18
  Capture Control
  Layer SoftGlow
    OuterGlow size=18
  Composite
```
The dump does not include pointers or nondeterministic identifiers.

## Threading

- Markup definitions are immutable and thread-safe.
- `PrismInstance` only changes on UI thread.
- The backdrop source is only called in draw submission.
- The analyzer, graph builder and optimizer run on the draw thread.
- The executor and pool respect the GraphicsDevice thread.
- No locks are entered in the current UI hot path.
- No background tasks or cross-thread ownership are introduced in the first
  implementation.

## Testing

### Source generator

Mandatory tests for:

- all directives and legal combinations;
- each structural diagnosis in the proposal;
- types and defaults from the catalog;
- overrides and the independence of the courts;
- namescope and duplicate names;
- valid and invalid Motion targets;
- application/window/template resources;
- code generated without reflection and textual lookup.

### Runtime UI

Unit tests for:

- idempotent attach/detach;
- typified versions and slots;
- parameter changes without local rebuild;
- visibility and Motion cancellation;
- resource invalidation;
- lack of references after detachment.

### Analyze and render graph

The backend-neutral pipeline must be tested without a GPU:

- the analysis runs only once and is reused by the host and graph builder;
- bottom-up orders;
- PassThrough and isolated groups;
- clipping chains;
- masks;
- Fill versus Opacity;
- Blend If;
- bounds expansion;
- nested Prism;
- backdrop dependency without cycles;
- pass fusion without semantic change;
- complete cache keys.

An architecture test verifies that the analyzer, builder, and optimizer do not
references MonoGame. A build test verifies that the runtime descriptors,
the backend registry and documented tables come from the same catalog.

### MonoGame backend

Integration tests for:

- balanced scopes;
- restoring the GraphicsDevice state;
- surface pooling;
- device reset;
- lack of CPU readback;
- fallback for unavailable formats;
- determinism for noise and Dissolve.

### SDL_GPU backend

Unit, native smoke and cross-backend conformance tests cover:

- catalog-driven pipeline and binding selection without a duplicated planner;
- transient and retained resource ownership per device/window;
- offline SPIR-V, DXIL and MSL artifact verification;
- nested scopes, backdrops, resource-free catalog operations and deterministic
  fallback diagnostics;
- WindowsDX-to-SDL_GPU pixel differences at the repository-wide thresholds.

### Visual compliance

Each catalog entry has:

- a minimal scene;
- a reference image;
- declared profile and format;
- numerical tolerance;
- at least one case with partial alpha.

Blend modes and styles have common case arrays. Screenshots are captured
via the repository's automated harness/API, not manually.

### Background tests

Golden tests cover:

- static game;
- animated game with new `ContentVersion`;
- lower UI;
- nested Prism;
- more controls that share blur prefix;
- host without source;
- source that changes profile or size;
- different viewport scale.

### Cache retained tests

The tests always compare cache-on output to cache-off output and cover:

- the second identical frame produces the final hit and jump capture/effect passes;
- change of content, parameters, resources, lower UI, pixel scale,
  profile or shader package produce miss;
- a write with the same value does not invalidate the entry;
- two controls with common definition do not borrow their result when stamps
  differ;
- intermediate hit and final hit keep the same alpha, bounds and blend order;
- LRU eviction respects the byte budget and does not evict a forgotten pin entry;
- hide/collapse do zero lookup/promotion, and detach/replacement/device reset
  invalidate correctly;
- hash collision cannot validate a hit by itself without identity verification
  structural and of the complete dependency stamp.

### Memory leak and stress

The tests repeat:

- 10,000 attach/detach;
- navigation between view with Prism and view without Prism;
- repeated hide/unhide;
- resource composition change;
- device reset;
- background source replacement.

After cleanup:

- elements and instances must be collectable;
- the number of subscriptions returns to the base;
- GPU cache respects the budget;
- the pool does not increase after a stable warmup;
- Motion diagnostics does not report active orphan nodes.

## Performance budgets

Measured final gates:

| Scenario | Budget |
| --- | --- |
| Tree unchanged, Prism static | `0 B` allocations managed per draw after warmup |
| The second identical static frame | hit retained; zero capture and zero effect passes covered |
| Input pixel-affecting changed | mandatory miss and output identical to cache-off |
| Animated Prism parameter | without rebuild `ElementRenderCache` |
| Layer hidden | zero passes and zero surfaces for that layer |
| Background hidden | zero acquisition caused by that backdrop |
| Planning for standard scenes | baseline recorded and threshold approved before going |
| Stable pool | memory below configured limit after warmup |
| Cache retained stable | byte budget respected and hit rate reported |
| Attach/detach stress | no growth retained after GC and drain |
| Presentation Solar System | cold max 388.664 ms < 500 ms; warm p99 12.874 ms < 16.6667 ms |

Values are from three Release runs of the 12-scenario array, two
resolutions and cache on/off. The WindowsDX dogfood ran eight cycles of 45
frames for each of the seven Presentation chapters. The two
samples Solar warm above target and maximum warm of 49.363 ms remain visible in
JSON; the gate uses p99 and does not hide the spikes through adaptive quality.
The setup and all values are in the integration hardening benchmark, not in a
conveniently chosen catch.

### Current proof for layer styles

The automatic WindowsDX gate uses a 48-pass scene `ColorOverlay`.
After eight warmup frames and one stabilization frame after GC, it measures 16
consecutive frames and simultaneous requests:

- `0 B` allocations managed on the draw thread;
- no new render target after warmup;
- increasing the counter of reused surfaces;
- zero active leases after each frame;
- a peak of live surfaces lower than the number of styles.

A separate architecture test scans the production path
`Drawing/MonoGame/Prism/**/*.cs` and reject calls `GetData` and
`GetBackBufferData`. Thus, surface reuse measurement and CPU-free contract
readbacks remain verifiable in CI, not just observations from a session of
profiling.

## Security and robustness

- No shader source from markup.
- No arbitrary file path sent directly to the backend.
- Sizes, radii, number of passes and kernels are limited.
- Arrays and floating-point values ​​reject `NaN` and infinity.
- The built-in catalog has stable identifiers and validated schema.
- The analyzer detects overflow bounds and impossible costs before
  allocation.
- Backdrop surfaces are read-only for Prism.

## API compatibility result

Final comparison with [`prism-public-api-baseline.md`](prism-public-api-baseline.md)
classify the changes as follows:

- `IDrawingBackend.Render(DrawCommandList)` becomes
  `Render(DrawCommandList, in DrawingFrameContext)`. It is a breaking change
  required for the unique context created by the host, the backdrop frame-scoped lease and
  analysis performed only once.
- `IUiBackend.BackdropFrameSource` is a default interface member that returns
  `null`; existing backends do not receive a new deployment obligation.
- `MonoGameUiHostOptions.BackdropFrameSource` and `PrismRendererOptions` are
  optional additions.
- `BeginPrism`/`EndPrism`, authoring/runtime/hosting public types and keys
  Typed motions are additive. Consumers who switch exhaustively on
  `DrawCommandKind` must have a default case for new values.
- `PrismImage`, `PrismPipeline`, their filter/style base types and the 144
  catalog-generated operation types are additive, strongly typed authoring APIs.
- Graph, analysis and planning types, context and requirement builders
  graph-bearing temporarily exposed during development have been internalized. It is
  a source/binary rip only for pre-release Prism surface consumers and
  is required for analysis ownership to remain in the host/framework.

The ApiCompat SDK run between the assembly from `HEAD` and the final assembly reports
exactly 28 `CP0001` for internalized graph/planning types and 5 `CP0002`
for graph-bearing constructors/properties/method removed. There is no other
unclassified break; the older signature change `IDrawingBackend` is
covered separately from the pre-Prism baseline above.

There is no public API for third-party extensions or runtime shader injection.
The completeness audit currently inventories 217 public Prism types and 10
existing types extended by Prism across the core and MonoGame assemblies. The
public types have pages in `docs-site/documentation/classes/` and manifest entries.

## Recommended order of implementation

1. Immutable model, catalog and catalog validation.
2. Parser, binder, diagnostics and generated code.
3. `PrismInstance`, lifecycle, parameter store and Motion targets.
4. `BeginPrism`/`EndPrism` and retained integration without GPU.
5. Frame analyzer, graph builder, optimizer, validation and bounds propagation.
6. Ownership of backend state, surface pools and the MonoGame executor.
7. Color pipeline, Normal blend, masks and layer/group structure.
8. All blend modes and all styles.
9. The complete catalog of filters and conformance images.
10. Backdrop source and ordered lower-UI composition.
11. Dependency stamps, cache retained GPU cross-frame, invalidation and eviction.
12. Diagnostics, benchmarks, stress, device loss and public documentation.
13. Shared HLSL extraction and the SDL_GPU executor with offline artifacts and
    cross-backend conformance.

A stage is not considered finished if it leaves workarounds in views or resources
without clear ownership.

Parallel planning and third-party public extensions are not hidden steps in
this list. Each requires a real case, measurements and a separate decision. The cache
cross-frame is a mandatory stage and cannot be moved to the backlog.

## Acceptance criteria

Prism is fully implemented when:

- the normative syntax compiles and all the examples of the proposal work;
- all filters, styles and blend modes in the catalog have implementation and tests;
- the source generator rejects all illegal structures;
- Motion can statically target Prism parameters and properties;
- hide/collapse/detach stop work and associated Motion;
- control is captured only once per assessment;
- no normal path does CPU readback;
- the backdrop sees the game and the lower UI without feedback;
- the layout and hitbox remain unchanged;
- the delivered MonoGame and SDL_GPU backends execute Prism, while a custom
  backend without Prism support can bypass scopes safely;
- structural analysis is unique and reused for backdrop and graph;
- a static Prism produces hit retained and skips capture/covered passes again
  any pixel-affecting change produces miss and correct result;
- the retained cache respects budget, eviction, lifecycle and device reset without
  keep UI elements alive;
- catalog generates descriptors, backend registry and unlisted documentation
  parallels;
- MonoGame and SDL_GPU respect their measured budgets, restore backend state and
  pass the native cross-backend conformance thresholds;
- golden, stress, memory and device reset tests are green;
- diagnostics explains passes, cache and memory;
- public documentation is synchronized.

## Main risks

### The explosion of the number of passes

The catalog is large. No pass fusion, regional bounds and downsampling, o
seemingly simple composition can become too expensive.

### GPU backend state management

Changing render targets, `SpriteBatch`, SDL_GPU passes or bindings can corrupt
subsequent rendering if backend state is not rigorously restored.

### Bad cache

An incomplete key produces old pixels or borrows the result of another control.
Correctness takes precedence over hit rate.

### Differences between platforms

Renderable, precision and shader profile formats differ. Capability checks and
conformance tolerances must be explicit.

### Duplicate catalog

If the generator, runtime, and backend maintain separate lists, they will diverge.
The unique catalog generated is mandatory.

### God object in planning

If analysis, semantics of operations, optimization and execution come into one
class, any new filter will modify the same fragile core. separation analyzer,
builder, optimizer and operation planners is mandatory.

### Super-architecture

Public extensibility and concurrency can produce a lot of code with no demonstrated value.
They remain out of deployment until a real case and profiling warrants
complexity separately. The required retained cache remains specialized for Prism and not
becomes a pretext for a generic framework.

### Lifecycle

Bindings, Motion handles or caches that keep the element alive would recreate exactly
the kind of memory leak that the architecture is supposed to prevent.

## Final decision

Prism is implemented as an extension of the retained pipeline and the backend of
drawing, not as an attached effect that renders itself.

The markup produces an immutable definition. The element holds only a lightweight instance.
The command list delimits the local visual of the element. The analyzer produces a
single description of the frame, the graph builder turns it into a graph, and
the optimizer simplifies it.
Each backend processes pixels on the GPU and owns all of its temporary and retained
resources; planner and semantic contracts remain shared in core.

This separation keeps the syntax simple, enables the power of the Photoshop model, and
it avoids turning every control into a little makeshift renderer.
