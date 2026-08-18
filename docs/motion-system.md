# Motion System

Cerneala motion is root-owned through `UIRoot.Motion`. A root owns the clock, graph, property bindings, transactions, layout FLIP coordinator, presence coordinator, scroll timelines, diagnostics, and reduced-motion policy. `MotionSystem` is also usable as a standalone owner for focused tests and non-root scenarios, but attached elements must use the owning root's clock and thread affinity.

The mental model is state-first: application/style/input state changes establish target values, and motion decides how values visually travel there. Render-only properties such as opacity, transform channels, layout correction, presence opacity/scale, drag translation, and scroll-linked opacity must not enqueue measure/arrange work.

Layout motion uses FLIP correction. Normal layout computes the final rect; an internal render correction preserves visual continuity and animates back to identity. Cross-parent relocation is intentionally same-parent-only for v1 unless coordinate conversion is added.

Presence exit removes the element from public layout collections immediately, keeps it attached in a render sidecar until exit completes, excludes it from input, then detaches exactly once.

## Composition And Lifecycle

`MotionGraph` composes `MotionNode` values through groups and sequences. A
`MotionHandle` or `MotionGroupHandle` is terminal after completion, cancellation,
or fault; the system removes terminal work instead of retaining it in the active
graph. Starting a new animation on the same channel uses the configured priority
and retarget policy rather than silently creating competing writers.

Specs are typed and include tween, spring, keyframes, decay, repeat and
ping-pong behavior. `MotionPropertyStore` classifies invalidation so transform,
opacity and other render-only values skip layout work, while layout properties
use the layout coordinator and its captured snapshot. `ReducedMotionPolicy`
changes sampling and start behavior without changing the target state.

Markup `.crn` documents lower Motion directives into the same runtime concepts;
the source generator validates target type, property type, composition and
lifecycle before emitting code. The imperative API remains the source of truth
for runtime ownership and cancellation.
