# Cerneala Architecture v2

This document describes the current retained UI architecture above the existing `Drawing` and `UI/Input` foundations. The roadmap sections below are deliberately limited to behavior that is implemented or has an explicit maturity marker; do not read this file as a promise that every WPF-shaped API exists.

Read `architecture.md` first. That file explains what drawing and input already do. This file explains how the retained UI layers should use them.

## Goals

- Build a modern retained UI framework, not a WPF compatibility clone.
- Let the game loop call update/draw every frame without forcing layout/render regeneration every frame.
- Keep layout, rendering, input, focus, commands, and styling explicit and testable.
- Preserve `Drawing` as the command rendering layer.
- Preserve useful `UI/Input` concepts while replacing `UiInputTree` as the future route table.

## Layer Model

```text
Application / Playground
        |
        v
UI Hosting
        |
        v
UIRoot
        |
        +--> Typed State
        +--> Logical Tree
        +--> Visual Tree
        +--> Invalidation
        +--> Layout
        +--> Render Cache
        +--> Hit Testing
        +--> Focus
        +--> Commands
        +--> Aspect / Motion / Prism
        +--> Markup and source generation
        |
        +--> Drawing
        |
        +--> UI/Input
        |
        v
MonoGame adapters
```

## Existing Foundations

`Drawing` remains the low-level drawing command layer:

- `DrawingContext` records commands.
- `DrawCommandList` stores ordered commands.
- `IDrawingBackend` renders command lists.
- `MonoGameDrawingBackend` is the current concrete renderer.

`UI/Input` remains the low-level input foundation:

- `IInputSource` produces `InputFrame`.
- `MonoGameInputSource` reads MonoGame input state.
- routed event metadata and args remain useful.
- existing command primitives remain useful.

## Typed State

Retained UI state uses typed properties rather than cloning WPF dependency properties.

Planned core types:

- `UI/Core/UiObject.cs`
- `UI/Core/UiProperty.cs`
- `UI/Core/UiProperty{T}.cs`
- `UI/Core/UiPropertyMetadata{T}.cs`
- `UI/Core/UiPropertyOptions.cs`
- `UI/Core/UiPropertyStore.cs`

Property metadata declares invalidation effects:

- measure
- arrange
- render
- hit test
- style
- input visual state

Value precedence is:

```text
local > animation > style visual state > style base > inherited > default
```

The implemented property store also tracks the value source explicitly. Aspect
application writes through `UiPropertyValueSource.AspectBase`, animation writes
through the motion source, and local values remain above those framework-owned
values. This source information is part of invalidation and is not inferred by
reflection at render time.

## Logical And Visual Trees

MVP uses separate logical and visual trees.

Logical tree:

- semantic ownership;
- content relationships;
- control composition;
- resource/style inheritance;
- command and focus ownership.

Visual tree:

- layout participation;
- render cache ownership;
- hit-test participation;
- visual ordering;
- clipping and render bounds.

This is more complex than a single tree, but it gives Cerneala a cleaner model for templates, generated visuals, and control composition.

## Invalidation

Invalidation is the heart of the retained model.

State changes do not immediately recompute the world. They mark affected elements dirty.

Dirty work is processed by the frame scheduler:

```text
state/resource/input/style change
        |
        v
dirty flags
        |
        +--> layout queue
        +--> render queue
        +--> hit-test queue
        |
        v
frame scheduler
```

Confirmed behavior:

- no state change means no measure/arrange/render-cache rebuild;
- measure invalidation can imply arrange and render;
- render invalidation does not imply measure;
- input visual invalidation is decided by style metadata;
- resource invalidation is decided by resource metadata;
- MVP processes one deterministic snapshot per frame phase.
- same-phase work enqueued while a phase is processing is deferred to a later frame.
- downstream phase work can run in the same frame if that phase snapshot has not yet been taken.
- `FrameStats.MeasuredElements` and `FrameStats.ArrangedElements` count queued scheduler phase items; `FrameStats.MeasureCalls` and `FrameStats.ArrangeCalls` count actual recursive layout method calls.

`FrameBudget` is a later optimization.

## Layout

Layout uses layout-specific geometry:

- `LayoutSize`
- `LayoutPoint`
- `LayoutRect`

These are separate from `DrawPoint` and `DrawRect` because layout can have different semantics, such as unconstrained available size.

Layout uses `float` to align with drawing and MonoGame boundaries.

Layout phases:

1. measure;
2. arrange;
3. layout result caching;
4. render invalidation if visual bounds changed.

## Rendering

Rendering is retained and cache-based.

Each visual subtree can own a render cache. The render cache stores drawing commands generated through `DrawingContext`.

The backend can render every frame, but command generation should happen only when invalidated.

```text
visual element render
        |
        v
DrawingContext
        |
        v
subtree render cache
        |
        v
root command list
        |
        v
IDrawingBackend.Render(...)
```

Controls must not call MonoGame, Skia, HarfBuzz, `SpriteBatch`, or `Texture2D` directly.

## Input, Hit Testing, And Focus

The retained tree becomes the future input route source.

`UiInputTree` is not the permanent route table for v2. Existing routed event metadata and tests should inform the new routing model, but routing should be based on the retained logical/visual tree.

Confirmed policies:

- disabled elements are skipped by hit testing;
- hidden elements are skipped by hit testing;
- collapsed elements are skipped by hit testing;
- invisible layout-reserved elements cannot receive focus or pointer input.

Hit testing uses visual tree order and arranged/render bounds.

Focus is retained UI state, not raw input state.

## Commands And Actions

Commanding should build on existing `ICommand`, `RoutedCommand`, `CommandEvents`, and command args, but it should complete route-based execution in the retained tree.

MVP command work should support:

- command target resolution;
- can-execute query;
- execution route;
- button command binding.

## Styling, Motion, And Markup

Aspect is the current styling and template composition layer. `AspectRegistry`
builds immutable catalogs, `AspectProcessor` resolves rules for attached roots,
and `AspectEngine` applies winning values with dependency diagnostics. Target
type, state, variant, slot, data and resource dependencies participate in the
resolution key; clearing an aspect removes only the values owned by the aspect
source.

Style metadata can decide whether a visual state change affects:

- render only;
- layout and render;
- hit testing;
- inherited values.

Motion owns animated property values, transactions, presence, layout correction,
scroll and gesture bindings under the root clock. Render-only motion changes
must not enqueue measure or arrange work. Prism remains a render-pipeline
extension and therefore does not change layout, hit testing or focus.

The `.crn` markup path is shared by the language core, source generator and
language server. The generator consumes validated semantic results and emits
typed C#; the editor uses the same syntax and semantic model for recovery,
completion and diagnostics. `.cui.xml` is retired and is not a supported alias.

## Hosting

`UiHost` and `MonoGameUiHost` integrate retained UI with the game loop. A root
owns the Relay, frame scheduler, layout manager, retained renderer, resources,
aspects and motion state; hosts submit input during update and drawing during
draw without rebuilding unchanged retained work.

Expected frame shape:

```text
Update:
  read input
  update retained input state
  drain Relay and process dirty work

Draw:
  get cached root command list
  render through `IDrawingBackend.Render(...)`
```

The host may call draw every frame. That must not imply layout/render command
regeneration every frame. A second unchanged frame is expected to be idle for
retained scheduling and command generation.

## Current Boundaries

- `Cerneala.UI` remains backend-neutral; MonoGame and WindowsDX types stay in
  hosting and drawing adapter folders.
- `Cerneala.Language` is editor/build infrastructure and is not a runtime UI
  dependency.
- Visual Studio support is an out-of-process `.crn` language-server host. It
  does not turn the runtime into a Visual Studio or XAML compatibility layer.
- The API reference under `docs-site/documentation/classes/` is the source of
  truth for public members; this architecture document explains ownership and
  flow rather than duplicating every API signature.

## MVP Acceptance Gates

MVP retained UI work must prove:

- unchanged trees do not re-measure;
- unchanged trees do not re-arrange;
- unchanged trees do not regenerate render commands;
- render-only invalidation does not run measure;
- layout invalidation eventually refreshes render caches;
- UI core does not reference backend-specific types;
- existing `Drawing` tests still pass;
- existing `UI/Input` tests still pass.

After the first retained playground sample, use both:

- command-list assertions;
- visual golden-image tests.
