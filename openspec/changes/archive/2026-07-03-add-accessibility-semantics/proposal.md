## Why

Cerneala can now build retained controls, text editing, input, styling, diagnostics, and item controls, but those controls do not expose meaning to assistive technologies or automated semantic testing. This change adds a platform-neutral semantics tree first, with platform adapter integration left behind an explicit boundary.

## What Changes

- Add semantic roles, properties, nodes, tree building, and provider APIs under `UI/Accessibility`.
- Add accessible name support and automation peer-style adapters for button, textbox, and items controls where the naming is still useful.
- Add `IAccessibilityPlatform` as a platform boundary without native OS references in core UI code.
- Add tests for semantics tree construction, provider behavior, button semantics, and textbox semantics.

## Capabilities

### New Capabilities

- `accessibility-semantics`: Covers platform-neutral semantics nodes, semantic roles/properties, retained semantics tree building, control semantics providers, automation peer adapters, and accessibility platform boundaries.

### Modified Capabilities

None.

## Impact

- Affected code: `UI/Accessibility`, `UI/Platform`, retained controls, and tests.
- Affected tests: new tests under `tests/Cerneala.Tests/UI/Accessibility` plus roadmap/backend-boundary coverage.
- Dependencies: no new external dependencies; core accessibility APIs remain platform-neutral and must not reference native accessibility APIs directly.
