## 1. Text Editing Model

- [x] 1.1 Add `TextDocument`, `TextCaret`, `TextSelection`, and range validation under `UI/Text`.
- [x] 1.2 Add `UndoRedoStack` and editor edit records for document/caret/selection state.
- [x] 1.3 Add `TextEditor` operations for insert, replace selection, backspace/delete, caret movement, undo, and redo.

## 2. IME and Platform Boundaries

- [x] 2.1 Add `TextCompositionState` and `TextCompositionManager` with begin/update/commit/cancel behavior.
- [x] 2.2 Add `ClipboardAdapter` and `ITextInputPlatform` abstractions without concrete OS dependencies.

## 3. Retained Controls

- [x] 3.1 Add `TextBoxBase` retained control integrating `TextEditor`, focus, text input, invalidation, and text services.
- [x] 3.2 Add `TextBox` with editable plain text display.
- [x] 3.3 Add `PasswordBox` with masked display and explicit password access.

## 4. Tests and Roadmap

- [x] 4.1 Add focused tests for `TextEditor`, `TextCompositionManager`, and `UndoRedoStack`.
- [x] 4.2 Add retained control tests for `TextBox` and `PasswordBox`.
- [x] 4.3 Add or update boundary/roadmap tests proving text editing remains backend-neutral and roadmap section 20 is complete.
- [x] 4.4 Mark `ROADMAPv2.md` section 20 files, tests, and implementation order item complete.
- [x] 4.5 Run `openspec validate add-text-editing-ime --strict` and `dotnet test`.
