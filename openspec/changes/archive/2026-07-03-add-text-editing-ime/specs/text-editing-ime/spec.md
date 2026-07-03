## ADDED Requirements

### Requirement: Text document stores editable plain text
Cerneala SHALL provide a backend-neutral text document model that stores plain text content, exposes length/version state, and applies explicit replacement operations.

#### Scenario: Replace range updates content and version
- **WHEN** a valid range is replaced with new text
- **THEN** the document content changes and the document version advances

#### Scenario: Invalid range is rejected
- **WHEN** a text replacement uses indexes outside the document bounds
- **THEN** the operation fails before mutating content

### Requirement: Text caret and selection are explicit
Cerneala SHALL provide explicit caret and selection value types that represent logical text positions and selected ranges.

#### Scenario: Selection normalizes range
- **WHEN** selection anchor and active positions are supplied in reverse order
- **THEN** the selected range exposes normalized start and length values

#### Scenario: Caret clamps to document
- **WHEN** editor code moves the caret beyond document bounds
- **THEN** the resulting caret position is clamped to the nearest valid text position

### Requirement: Text editor mutates document deterministically
Cerneala SHALL provide a text editor service that coordinates text insertion, deletion, selection replacement, caret movement, undo, and redo without rendering dependencies.

#### Scenario: Insert text replaces current selection
- **WHEN** text is inserted while a selection is active
- **THEN** the selected text is replaced and the caret moves to the end of inserted text

#### Scenario: Backspace deletes previous text
- **WHEN** backspace is requested with no active selection and the caret is not at the start
- **THEN** the previous text unit is removed and the caret moves backward

#### Scenario: Undo and redo restore document states
- **WHEN** an edit is undone and then redone
- **THEN** document content, caret, and selection return to the expected states

### Requirement: Composition manager tracks IME lifecycle
Cerneala SHALL provide a composition manager that tracks composition text, start position, active state, commit, and cancellation independently from committed document content.

#### Scenario: Composition update is preview state
- **WHEN** composition text is updated before commit
- **THEN** committed document text remains unchanged while composition state reports the preview text

#### Scenario: Composition commit inserts text
- **WHEN** active composition is committed through the editor
- **THEN** committed text is inserted and composition state becomes inactive

#### Scenario: Composition cancel preserves document text
- **WHEN** active composition is cancelled
- **THEN** committed document text remains unchanged and composition state becomes inactive

### Requirement: Textbox controls edit retained text
Cerneala SHALL provide `TextBoxBase`, `TextBox`, and `PasswordBox` controls that use retained text editing state, existing text services, focus/text input routing, and retained invalidation.

#### Scenario: Text input updates TextBox text
- **WHEN** a focused retained `TextBox` receives text input
- **THEN** its text document updates and measure/render invalidation is requested

#### Scenario: PasswordBox masks display text
- **WHEN** a `PasswordBox` contains password text
- **THEN** retained rendering uses mask characters while the explicit password value remains available to control code

#### Scenario: Textbox controls remain retained
- **WHEN** textbox controls render
- **THEN** they emit retained drawing commands through existing text rendering services and render caches

### Requirement: Text input platform boundary is adapter-only
Cerneala SHALL provide platform-neutral clipboard and IME boundary abstractions without concrete OS dependencies in core UI code.

#### Scenario: Clipboard adapter is explicit
- **WHEN** editor code performs copy, cut, or paste
- **THEN** text is exchanged through an explicit clipboard adapter abstraction rather than hidden global state

#### Scenario: Platform abstractions remain backend-neutral
- **WHEN** text editing code is compiled
- **THEN** it does not reference MonoGame, Skia, HarfBuzz, `Texture2D`, `SpriteBatch`, or native OS clipboard/IME APIs

### Requirement: Text editing and IME are tested
Cerneala SHALL include focused tests for text document operations, editor mutations, composition lifecycle, undo/redo, textbox controls, password masking, and backend-neutral boundaries.

#### Scenario: Required text editing tests exist
- **WHEN** this implementation phase is complete
- **THEN** tests exist for `TextBox`, `PasswordBox`, `TextEditor`, `TextCompositionManager`, and `UndoRedoStack`

#### Scenario: Full tests pass
- **WHEN** this implementation phase is complete
- **THEN** `dotnet test` passes
