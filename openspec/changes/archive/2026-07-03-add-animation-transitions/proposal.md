## Why

Cerneala has retained invalidation, typed property precedence, styling, and a game-loop-driven host, but there is no explicit way to drive time-based property changes. Animation should use the existing retained property and frame scheduler model so render-only changes do not accidentally trigger layout, and layout-affecting changes schedule layout only when values actually change.

## What Changes

- Add a backend-neutral animation capability with clocks, schedulers, typed animations, transitions, easing, optional storyboard composition, and animated value source helpers.
- Drive animations from explicit game-loop time instead of hidden timers.
- Apply animated values through the existing typed property precedence layer, using animation value source semantics.
- Add style transition descriptors that styling can use without owning the animation runtime.
- Add focused tests for clocks, schedulers, typed animations, transitions, and invalidation behavior.
- Update `ROADMAPv2.md` section 23 checkboxes as implementation lands.

## Capabilities

### New Capabilities
- `animation-transitions`: Covers game-loop-friendly animation clocks, typed animations, transitions, easing, scheduling, animated property values, and transition descriptors.

### Modified Capabilities
- `typed-state-model`: Animation value source semantics become actively used by animated property updates and must preserve precedence below local values.
- `retained-invalidation-frame-scheduler`: Animation ticks must schedule retained work through the existing invalidation queues and avoid no-op frame churn.
- `styling-theme-engine`: Styling can describe transitions without directly driving animation internals.

## Impact

- Affected code: new `UI/Animation`, `UI/Styling/StyleTransition.cs`, typed property value usage, retained invalidation tests, and `ROADMAPv2.md`.
- APIs: new public animation and transition descriptors plus scheduler APIs that accept explicit elapsed time.
- Dependencies: no new production dependency; animation remains backend-neutral and game-loop driven.
