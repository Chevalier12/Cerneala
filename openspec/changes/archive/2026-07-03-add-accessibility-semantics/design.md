## Context

Cerneala's retained UI tree now has enough control structure to expose semantic meaning. Accessibility should not be bolted directly onto drawing commands or concrete platform adapters; it needs a platform-neutral semantics model that can be consumed by tests today and native adapters later.

The initial target is semantic inspection for retained controls, not a full OS accessibility bridge. The tree should be deterministic, backend-neutral, and derived from retained element structure and control-specific semantic providers.

## Goals / Non-Goals

**Goals:**

- Add platform-neutral semantics primitives: role, property, node, tree, provider, and accessible-name helpers.
- Add control adapters for button, textbox, and items controls so common interactive controls expose useful names, roles, values, enabled state, and children.
- Add a platform boundary interface for future native accessibility adapters.
- Keep semantics generation deterministic and testable without OS accessibility services.

**Non-Goals:**

- Do not implement Windows UI Automation, macOS accessibility, browser ARIA, or native adapter registration in this phase.
- Do not add reflection-heavy property browsing.
- Do not make drawing commands, layout, or input depend on accessibility.
- Do not require markup for accessibility.

## Decisions

- `SemanticsNode` is an immutable snapshot of a retained element's semantic role, name, properties, and child nodes.
- `SemanticsProvider` builds a tree from retained visual children and lets control-specific providers override role/name/value metadata.
- `AccessibleName` resolves explicit names first, then useful control content text when available.
- Automation peer naming is used only as a small adapter concept because it is familiar and maps well to per-control semantic providers.
- `IAccessibilityPlatform` accepts a semantics tree snapshot and remains adapter-only. Platform-specific bridge implementations are deferred.

## Risks / Trade-offs

- Semantics can get stale if treated as mutable state -> generate snapshots from retained state instead of caching by default.
- Control content can be arbitrary retained UI -> accessible-name fallback should be conservative and deterministic.
- Native accessibility APIs differ heavily -> keep this phase to a core semantics tree and one platform boundary.
- Automation peer naming carries WPF baggage -> use it only for simple per-control semantic adapters, not as a full WPF clone.
