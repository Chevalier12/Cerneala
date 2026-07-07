# Motion System

Cerneala motion is root-owned through `UIRoot.Motion`. A root owns the clock, graph, property bindings, transactions, layout FLIP coordinator, presence coordinator, scroll timelines, diagnostics, and reduced-motion policy.

The mental model is state-first: application/style/input state changes establish target values, and motion decides how values visually travel there. Render-only properties such as opacity, transform channels, layout correction, presence opacity/scale, drag translation, and scroll-linked opacity must not enqueue measure/arrange work.

Layout motion uses FLIP correction. Normal layout computes the final rect; an internal render correction preserves visual continuity and animates back to identity. Cross-parent relocation is intentionally same-parent-only for v1 unless coordinate conversion is added.

Presence exit removes the element from public layout collections immediately, keeps it attached in a render sidecar until exit completes, excludes it from input, then detaches exactly once.
