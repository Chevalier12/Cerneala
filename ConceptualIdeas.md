# Conceptual Ideas

Possible direction for Cerneala: a retained-mode UI framework for MonoGame, with invalidation, debugging and scheduling inspired more by game engines than by classic desktop frameworks.

## 1. UI as a dependency graph

Instead of everything being modeled only as a visual/logical tree, the layout, input, render, resources and state can be seen as an explicit incremental graph.

Each node could declare what it consumes and what it produces:

- `Measure` depends on font metrics, content, constraints and style.
- `Arrange` depends on the measure result and the parent layout.
- `Render` depends on style, geometry, text shaping and assets.
- `HitTest` depends on bounds, visibility, enabled state and clipping.

The invalidation would become propagation through the graph, not just flags propagated up and down the tree.

## 2. Frame scheduler with real budget

For MonoGame, the scheduler can be a major difference. Instead of always processing all the work, it can decide what fits in the frame budget.

Examples:

- Critical layout processed immediately.
- Text rasterization postponed when the frame is crowded.
- Image decode done in the background.
- Hit-test cache partially redone.
- Render cache temporarily degraded and rebuilt later.

The goal would be a UI that remains responsive even when it has a lot of work to do.

## 3. Declarative retained UI over game loop immediate

Cerneala can combine retained UI for menus, toolbars, inspectors and editors with the predictability of a game loop.

The central idea:

- The UI structure is retained.
- Status can be declarative.
- Input and rendering are processed explicitly per frame.
- The integration with gameplay/runtime editor remains direct, without a magic layer difficult to control.

## 4. Diagnosis-first UI

`InvalidationTrace` can become a main feature, not just a debugging helper.

The framework could directly answer questions such as:

- Why was this element relayed?
- What property caused render?
- Which handler consumed the input?
- Which frame exceeded the budget?
- What cache was unnecessarily invalidated?

A UI framework where debugging is first-class would have a strong identity, especially for tooling and games.

## 5. Layout based on constraints and priorities

Besides the classic model `Measure` / `Arrange`, Cerneala could have layout primitives based on relations.

Examples:

- Alignment according to the baseline.
- Keeping aspect ratio.
- Pin to safe area.
- Size by content, but limited by viewport.
- Distribution according to priorities.
- Relationships between elements that are not necessarily direct parent-child.

You don't need a complete solver from scratch. It is important to have room for relational layout where `Measure` / `Arrange` becomes cumbersome.

## 6. Unified input as a timeline
Input can be treated as a stream/timeline, not just as isolated events.

Possible sources:

- Mouse.
- Keyboard.
- Text composition.
- Touch.
- Stylus.
- Gamepad.
- Focus transitions.
- Gestures.

For MonoGame, consistent support for mouse, keyboard, touch and gamepad would be a real advantage over classic desktop frameworks.

## 7. Styles as compilable data

Instead of very dynamic and hard-to-follow styling, styles can be data validated and eventually compiled.

The important advantage: the framework can know in advance the effects of a change.

Examples:

- `Background` is render-only.
- `FontSize` affects measure, arrange and render.
- `IsEnabled` affects hit-test and visual input state.

That would fit well with the existing system of `UiPropertyOptions`.

## 8. Render cache explicitly on elements

Elements can explicitly state how they participate in caching.

Examples of metadata:

- Cacheable.
- Volatile.
- Depends on transformation.
- It depends on clipping.
- Can be atlased.
- Text cache key.
- Partial redraw region.

The goal is not to need a complete redraw when only a small piece of the UI has changed.

## Positioning

Cerneala doesn't have to be just a smaller clone of WPF or Avalonia. A more interesting positioning:

**A retained-mode UI framework for MonoGame, with game engine invalidation, diagnostics and scheduling.**

This direction keeps the good ideas from desktop frameworks, but adapts them for interactive runtimes where frame predictability, debugging and explicit control matter more.