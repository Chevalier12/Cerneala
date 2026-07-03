## Why

Cerneala has retained text rendering, focus, routed text input, and diagnostics, but it cannot edit text yet. This change adds the retained text editing layer needed for textbox controls, selection/caret state, undo/redo, clipboard boundaries, and IME composition without turning text rendering services into an editor.

## What Changes

- Add retained text editing primitives for document content, caret movement, selection ranges, text mutations, undo/redo, and composition state.
- Add `TextBoxBase`, `TextBox`, and `PasswordBox` controls that use existing retained layout, input, text services, styling, and invalidation paths.
- Add a platform-neutral text input abstraction for clipboard and IME integration boundaries, with MVP behavior remaining adapter-safe and testable without platform globals.
- Add tests for editing operations, composition lifecycle, undo/redo, textbox behavior, password masking, and roadmap/boundary coverage.

## Capabilities

### New Capabilities

- `text-editing-ime`: Covers retained text editing controls, text document/editor state, selection/caret behavior, undo/redo, clipboard boundaries, and IME composition lifecycle.

### Modified Capabilities

None.

## Impact

- Affected code: `UI/Controls`, `UI/Text`, `UI/Platform`, retained input integration, and tests.
- Affected tests: new tests under `tests/Cerneala.Tests/Controls` and `tests/Cerneala.Tests/UI/Text`, plus boundary/roadmap coverage.
- Dependencies: no new external dependencies; text editing and IME abstractions remain backend/platform-neutral, with platform-specific adapter implementations deferred.
