## Context

Cerneala already has typed `UiProperty<T>` storage with a dedicated `Animation` value source, retained invalidation flags, a frame scheduler, host update/draw separation, styling, resources, and retained rendering caches. Section 23 adds time-based property changes on top of those systems.

Animation must remain game-loop friendly: callers provide elapsed time, the animation scheduler advances active animations, and animated values flow through existing property metadata so invalidation remains precise. Render-only properties must not trigger measure, while layout-affecting properties must trigger layout only when the effective animated value changes.

## Goals / Non-Goals

**Goals:**
- Add explicit animation clocks and scheduler APIs driven by game-loop time.
- Add typed animations and transitions that interpolate values through strongly typed delegates.
- Apply animated values through `UiPropertyValueSource.Animation`.
- Release animation values when animations complete or are stopped.
- Add style transition descriptors without making styling own scheduler execution.
- Add tests proving render/layout invalidation behavior.

**Non-Goals:**
- A designer timeline runtime.
- Reflection-based animation of arbitrary member paths.
- A full composition engine for parallel/serial timelines unless the minimal storyboard surface is needed by tests.
- Platform timers or background threads.

## Decisions

1. Animation time is explicit.
   - Rationale: Cerneala runs inside game loops; hidden timers would fight host scheduling.
   - Alternative considered: background timer-driven animations. That would be nondeterministic and harder to test.

2. Typed animations own interpolation delegates.
   - Rationale: `UiProperty<T>` is strongly typed, and not every type has a universal interpolation rule.
   - Alternative considered: object-based interpolation. That would move type errors to runtime and weaken the typed model.

3. Animated values are applied via `UiPropertyValueSource.Animation`.
   - Rationale: typed state already defines precedence where local values override animations.
   - Alternative considered: store animation values in a separate side table and force controls to read it. That would duplicate property precedence.

4. Scheduler ticks only active animations and removes completed entries.
   - Rationale: no-work frames must stay cheap and completed animations must release value source state.
   - Alternative considered: keep completed animation records for diagnostics. Diagnostics can be added later without retaining property values.

5. Transitions are descriptors over property changes.
   - Rationale: styling can request transitions without becoming the animation runtime.
   - Alternative considered: style applicator directly owns animation clocks. That couples styling to frame execution.

## Risks / Trade-offs

- [Risk] Generic animation can become reflection-heavy. -> Mitigation: use typed `Animation<T>` and explicit interpolation delegates.
- [Risk] Animation ticks can enqueue redundant work every frame. -> Mitigation: only set animated values when interpolation changes the effective value according to property metadata.
- [Risk] Completed animations can leave stale animated values that mask styles. -> Mitigation: completion clears the `Animation` value source.
- [Risk] Layout animation can be expensive. -> Mitigation: rely on existing property invalidation metadata and test render-only versus layout-affecting properties separately.
