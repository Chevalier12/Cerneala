# Cerneala Architecture

This document describes the architecture that exists in the repository now.
It explains ownership and flow. It is not a complete API reference and it is
not a promise that every WPF-shaped type has matching WPF behavior.

The canonical public API documentation lives under
[`docs-site/documentation/classes/`](../../docs-site/documentation/classes/).

The [architecture diagram](../assets/cerneala-architecture.png) summarizes
the retained runtime and the SDL3 + SDL_GPU desktop composition. The flow and
ownership below provide the detailed context. One correction: the diagram labels
Servo "external automation", but Servo runs in-process (see [Servo](servo.md)).

## The Short Version

Cerneala is a retained realtime UI framework.

The application creates a UI tree and mutates typed state. Cerneala tracks the
resulting invalidation, processes explicit frame phases, retains layout and
drawing work, and submits backend-independent commands to a selected renderer.

Traditional UI and game rendering are not separate products glued together.
`RenderSurface2D` is a `ContentControl`, so a realtime game view and its normal
retained HUD can live in the same UI tree.

## End-To-End Flow

```text
Build time

.crn markup
    -> Cerneala.Language syntax and semantics
    -> Cerneala.SourceGen
    -> typed C# application and UI tree

Runtime

Application / presentation
    -> window hosting and platform runtime
    -> UIRoot retained state and trees
    -> invalidation and frame scheduler
    -> layout and retained render cache
    -> DrawingContext and DrawCommandList
    -> IDrawingBackend
    -> SDL3 GPU presentation
```

The build-time language stack and the runtime UI stack are deliberately
separate. `Cerneala.Language` is not a runtime dependency of `Cerneala.UI`.

## Systems Map

Each system has one owner folder or project. The link points to the most
detailed architecture text that exists today; a section of this file is used
until a dedicated document exists.

| System | Code | Responsibility | Architecture text |
|---|---|---|---|
| Property system | `UI/Core` | Typed properties, value sources, effective value, change notification. | [property-system.md](property-system.md) |
| Invalidation and frame | `UI/Invalidation` | Dirty flags, the six work queues, and phase order in each frame. | [invalidation-and-frame.md](invalidation-and-frame.md), [frame loop diagram](diagrams/retained-frame-loop.md) |
| Layout | `UI/Layout` | Measure and arrange with cached results and layout boundaries. | [layout.md](layout.md) |
| Input | `UI/Input` | Hit testing, routed events, focus, capture, commands. | [input.md](input.md), [layer boundaries](diagrams/ui-layer-boundaries.md) |
| Rendering | `UI/Rendering` | Per-element render caches and the retained root command list. | [rendering.md](rendering.md) |
| Drawing | `Drawing` | Backend-neutral command recording and the backend interface. | [drawing.md](drawing.md) |
| Data | `UI/Data` | Bindings between observable sources and typed properties. | [markup data bindings](../reference/markup-data-bindings.md) |
| Markup and source generation | `UI/Markup`, `Cerneala.SourceGen` | Lowering `.crn` into typed C# and the generated-code runtime surface. | [markup-and-sourcegen.md](markup-and-sourcegen.md), [application markup](../guides/application-markup.md) |
| Language | `Cerneala.Language` | `.crn` syntax, semantics, diagnostics, and editor services. | [language-tooling.md](language-tooling.md#cernealalanguage) |
| Language server | `Cerneala.LanguageServer` | LSP host over `Cerneala.Language`. | [language-tooling.md](language-tooling.md#cernealalanguageserver), [language server guide](../guides/language-server.md) |
| Preview host | `Cerneala.PreviewHost` | Out-of-process compile, hot reload, and render of a `.crn` preview. | [language-tooling.md](language-tooling.md#cernealapreviewhost) |
| Visual Studio extension | `Cerneala.VisualStudio` | Starts the language server and the preview host inside Visual Studio. | [language-tooling.md](language-tooling.md#cernealavisualstudio), [Visual Studio guide](../guides/visual-studio-community.md) |
| Aspect | `UI/Aspect` | Styling and control composition rules. | [aspect.md](aspect.md) |
| Motion | `UI/Motion` | Animation under the root clock. | [motion.md](motion.md) |
| Prism | `UI/Prism`, `Drawing/Prism` | Retained local visual composition and GPU filters. | [prism-technical-design.md](prism-technical-design.md) |
| Relay | `UI/Relay` | Moving callbacks onto the root's UI thread. | [relay.md](relay.md) |
| Detective | `UI/Detective` | Runtime snapshots, traces, and counters. | [detective.md](detective.md) |
| Servo | `UI/Servo` | In-process UI automation through real input paths. | [servo.md](servo.md), [Servo guide](../guides/servo.md) |
| Text | `UI/Text`, `Drawing/Text` | Shaping, line breaking, text layout, fonts. | [text.md](text.md) |
| Theming | `UI/Theming` | Themes, theme keys, and the theme token bridge into Aspect. | [theming-and-resources.md](theming-and-resources.md) |
| Resources | `UI/Resources` | Resource lookup, dependency tracking, image resources. | [theming-and-resources.md](theming-and-resources.md) (image loading not covered) |
| Accessibility | `UI/Accessibility` | Semantics tree and automation peers; no platform adapter yet. | [accessibility.md](accessibility.md) |
| Ink | `UI/Ink` | Stroke data for `InkCanvas`; it does not draw. | [ink.md](ink.md) |
| Hosting and platform | `UI/Hosting`, `UI/Platform` | Application, window runtime, backend registration, platform services. | [hosting-and-platform.md](hosting-and-platform.md) |
| Timbre | `Timbre`, `UI/Timbre` | Audio runtime, mixer, decoding, memory budget. | [timbre.md](timbre.md), [Timbre guide](../guides/timbre-guide.md) |
| SDL backends | `Cerneala.Platforms.Sdl3`, `Cerneala.Backends.SdlGpu` | SDL3 windows, input, audio, and SDL_GPU drawing. | [sdl-desktop-backend.md](sdl-desktop-backend.md) |
| Scene2D packages | `Cerneala.Scene2D.Importers`, `Cerneala.Scene2D.Packages` | Tiled and LDtk import and the package format. | [scene2d.md](scene2d.md) |
| Build tools | `Tools` | Shader, SVG, and package compilers, PrismAudit, Roslyn MCP. | [build-tools.md](build-tools.md) |

## Build-Time Authoring

`.crn` is compile-time markup: nothing loads it at runtime. `Cerneala.Language`
parses and validates it once, with the same rules for the build and the editor.
The source generator lowers it into typed C#, and the language server, preview
host and Visual Studio extension provide editor support. Code-first
construction remains valid. See [markup-and-sourcegen.md](markup-and-sourcegen.md)
and [language-tooling.md](language-tooling.md).

## Application And Window Hosting

`Application` owns process-level lifecycle, services, resources, window
tracking, and shutdown policy. Generated `App.crn` declarations connect that
application model to a concrete startup window.

`WindowApplicationRuntime` owns the frame and native window lifecycle for the
desktop application. A platform implementation provides native windows, input
sources and platform services; the SDL platform fills only the cursor and text
input services. Backend selection is explicit through
`ApplicationBackendAttribute`. See [hosting-and-platform.md](hosting-and-platform.md).

## `UIRoot` And Retained Ownership

Each root owns the retained systems for one UI tree:

- typed property state;
- logical and visual relationships;
- Relay scheduling;
- inherited-property propagation;
- Aspect resolution;
- Motion state and the root clock;
- invalidation queues and frame scheduling;
- layout;
- retained rendering;
- hit testing, routed input, focus, capture, and commands;
- resources and Detective diagnostics.

This ownership matters. These systems are coordinated at the root instead of
running as unrelated global managers.

## Typed State

`UiObject` stores values through typed `UiProperty<T>` descriptors in a
`UiPropertyStore` with nine stored value sources, from `Local` down to
`Inherited`, plus the default. Only a change of the effective value invalidates
retained work, and the property's `UiPropertyOptions` decide which work. Details:
[property system](property-system.md).

## Logical And Visual Trees

Cerneala keeps logical and visual relationships separate.

The logical tree represents application ownership, content, resources,
commands, and semantic relationships.

The visual tree represents layout, rendering order, clipping, hit testing, and
generated template content.

A control can therefore own application content logically while Aspect and
templates generate a different visual subtree. Tree mutation is validated,
reparenting is explicit, and attach/detach lifecycle follows root ownership.

## Relay

Each root owns a `UiRelay` that moves callbacks from any thread onto the root's
UI thread. `UIRoot.BeginUpdate` drains one capped snapshot (1024 callbacks by
default) before the scheduler runs any phase; the drain is not a `FramePhase`.
Details: [Relay](relay.md).

## Input, Focus, And Commands

Platform input sources produce backend-neutral frame snapshots. `UiHost`
dispatches them between scheduler passes: hit testing finds the target, and
routed events travel tunnel, direct and bubble routes taken from a route tree
derived from the visual tree and rebuilt only when it is stale. Focus, capture
and commands use the same routes. Details: [Input](input.md).

## Invalidation And Frame Scheduling

State changes do not recompute the UI at once. They mark the affected work
dirty and put the element in one of six per-root queues. Each `UiHost` update
drains Relay, runs the scheduler around input dispatch in the fixed phase order
`InheritedProperties`, `CommandState`, `Aspect`, `InheritedProperties` again,
`Measure`, `Arrange`, `RenderCache`, `HitTest`, and then commits the root
command list. Details: [Invalidation And Frame Scheduling](invalidation-and-frame.md),
[frame loop diagram](diagrams/retained-frame-loop.md).

## Layout

Layout measures and arranges only the elements whose layout was invalidated,
using `LayoutSize`, `LayoutPoint` and `LayoutRect`, and reuses cached results
for the same constraint and `LayoutVersion`. Measure invalidation climbs to the
first layout boundary; a changed rectangle schedules render and hit-test work.
Details: [Layout](layout.md).

## Aspect

Aspect owns styling and control composition. Rules from code, markup,
resources and `ElementAspect` go through one resolver, `AspectEngine`, in
`FramePhase.Aspect`; winners are written through `UiPropertyValueSource.AspectBase`.
Aspect does not own time sampling or GPU filters. Details: [Aspect](aspect.md).

## Motion

Motion owns animation under the root clock. It samples at most once per frame
(before layout, or before render) and writes through
`UiPropertyValueSource.Animation`; the animated property's invalidation category
decides whether a sample costs layout or only rendering. Details: [Motion](motion.md).

## Retained Rendering

Each element's `Render` records a local command list that `ElementRenderCache`
keeps until the element's render state changes. At the end of each update,
`RetainedRenderer.Commit` builds one root command list from the local lists in
visual order. Draw only submits that committed list; it never calls `Render` or
changes the tree. Details: [Retained Rendering](rendering.md).

## `RenderSurface2D`

`RenderSurface2D` is the first-class realtime 2D surface inside the control
model. It inherits `ContentControl` and participates in normal layout,
invalidation, attachment, detachment, resources, and rendering order.

The control records a specialized 2D command stream through
`RenderSurface2DFrame`. Its retained `Content` is rendered above the game
surface, which allows ordinary controls to form the HUD or overlay.

Continuous mode evaluates drawing each Cerneala frame. On-demand mode retains
the previous surface until layout, a tracked drawable dependency, a relevant
property, or `InvalidateFrame()` marks it dirty.

Graphics-device resources and surface sessions belong to the backend and are
disposed when the control detaches.

Ordinary world composition has four roles: `Scene2D` groups, `TileMap2D` static
strata, individual `Sprite2D` nodes, and dynamic `SceneItems2D` collections.
`Tile` remains immutable data inside a map. Imported source strata become
independent map models; scene composition owns their ordering and shared effects.
A sprite is a scene peer, not a promoted child of a static map. Replacing a static
cell with a sprite requires an explicit immutable-model update by composition.

The scene root owns collision queries, while collision geometry belongs to the
object it represents. Each `Sprite2D` owns zero or one live
collider through its singular `Collider` property. Each static `Tile` placement,
imported tile definition, and imported entity likewise owns at most one immutable
descriptor, adapted by the tile map or composition layer. Generic UI tree
mutations cannot bypass collider ownership, and import/markup paths reject a
second shape instead of truncating it. Image resizing and animation do not manage
collision dimensions; applications own that geometry explicitly.

## Drawing Boundary

The `Drawing` layer records backend-neutral commands; it is not another UI
tree. `DrawingContext` writes `DrawCommand` values into a flat
`DrawCommandList`, and `IDrawingBackend.Render` receives the list with a
`DrawingFrameContext`. Controls never call SDL or GPU APIs directly. Details:
[Drawing](drawing.md).

## Prism

Prism owns retained local visual composition. Its definitions describe layers,
filters, styles, masks, blend operations, parameters, and resources. A
`PrismInstance` attaches that definition to a visual and tracks live state.

Prism can consume a rendered visual result or backdrop and produce composed
pixels. It does not change layout, hit testing, focus, or the logical tree.

Backend executors own GPU resources, shader execution, and retained Prism result
caches. Details: [Prism technical design](prism-technical-design.md).

## Backend Boundary

SDL3 + SDL_GPU is the sole maintained desktop composition. The MonoGame and
WindowsDX adapters, public APIs and active consumers have been removed without
a compatibility facade. Historical captures remain visual references, not
available runtime backends. Migration verification and outstanding failures are
recorded in the [removal audit](../audits/2026-09-05-monogame-removal.md).

The SDL3 path separates native platform ownership from GPU drawing ownership:

- `Cerneala.Platforms.Sdl3` owns SDL windowing, events, input, and native
  services;
- `Cerneala.Backends.SdlGpu` owns the graphics device, swapchain sessions,
  drawing resources, Prism execution, and presentation;
- Cerberus owns the GPU-oriented drawing compilation and execution path inside
  the SDL3 backend.

Core UI code remains unaware of the selected native backend.

## Detective And Evidence

Cerneala treats runtime behavior as something to measure, not something to
guess about. `UIRoot.Detective` exposes the runtime snapshots, traces, and
counters that the subsystems produce; it does not drive or invalidate anything.
Details: [Detective](detective.md).

Applicable changes are verified through combinations of:

- unit and contract tests;
- deterministic host tests;
- native runtime smokes;
- screenshots produced by `Window.SaveScreenshot`;
- golden images and pixel or color diffs;
- performance counters and BenchmarkDotNet results;
- API diffs and canonical documentation checks.

A green focused test proves only its focused contract. Backend or retained
runtime changes still require their wider conformance gates.

## Architecture Rules

- Fix the layer that owns the violated invariant.
- Do not patch a visible control when the scheduler, layout, input, or backend
  owns the contract.
- Do not create parallel state, input, tree, or rendering paths for convenience.
- Do not make `DrawCommandList` a scene graph.
- Do not make drawing own UI state.
- Do not let backend-specific types leak into the retained core.
- Do not claim backend parity from compilation alone.
- Do not update golden images until the intended visual contract is known.
- Do not expand WPF-compatible surface merely because a familiar name exists.
- Keep public API documentation synchronized with implementation and tests.

## Related Documents

- [Getting Started](../guides/getting-started.md)
- [Cerneala Markup Guide](../guides/markup-guide.md)
- [Roadmap](../../ROADMAP.md)
- [SDL desktop backend](sdl-desktop-backend.md)
- [Prism Guide](../guides/prism-guide.md)
- [Cerneala website](https://chevalier12.github.io/Cerneala/)
- [Discord](https://discord.gg/p6SbqByd59)
