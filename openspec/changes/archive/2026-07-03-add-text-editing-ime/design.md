## Context

Cerneala already has retained controls, text measuring/rendering services, routed keyboard/text input, focus tracking, diagnostics, styling, and resource-backed fonts. What is missing is an editing layer that turns text input into retained document mutations, caret/selection state, undo/redo, and composition state.

Text editing must build on existing `TextInputSnapshotEvent`, `TextCompositionEventArgs`, `TextBlock`, retained invalidation, and focus/input routing. Platform-specific clipboard and IME services are still adapter boundaries, so the MVP must be testable without global OS calls.

## Goals / Non-Goals

**Goals:**

- Add backend-neutral text document, caret, selection, editor, composition manager, undo/redo, and clipboard platform boundary types.
- Add `TextBoxBase`, `TextBox`, and `PasswordBox` retained controls that participate in layout, render cache, focus, text input, styling, and invalidation.
- Support deterministic editing operations: insert, delete, replace selection, caret movement, selection updates, undo, redo, and composition commit/cancel.
- Keep password masking out of document storage: the document stores real text, while rendering/display APIs expose masked text.

**Non-Goals:**

- Do not implement a native OS IME adapter, system clipboard adapter, spellcheck, bidi editing, rich text, multiline scrolling, or accessibility peers in this phase.
- Do not replace existing text measuring/rendering services.
- Do not add platform-specific references to `UI/Text`, `UI/Controls`, or `UI/Platform` abstractions.
- Do not make markup or reflection part of text editing.

## Decisions

- `TextDocument` owns plain string content and versioning. It exposes explicit replace operations so editor logic can be tested without controls.
- `TextCaret` and `TextSelection` are immutable value types. The editor clamps indexes against the current document so controls do not duplicate range logic.
- `TextEditor` coordinates document mutations, caret/selection updates, and undo/redo records. It does not render and does not know about retained controls.
- `TextCompositionManager` tracks composition lifecycle independently from committed document text until commit. Composition preview text can be surfaced to controls without corrupting undo history.
- `TextBoxBase` adapts retained routed input to `TextEditor`, exposes typed retained properties, and invalidates measure/render when display text or caret/selection state changes.
- `TextBox` displays the editor document text. `PasswordBox` stores the same document model but renders a mask character and exposes password access explicitly.
- `ITextInputPlatform` and `ClipboardAdapter` are abstractions only. Tests use fakes; platform adapters can be added later without changing editor behavior.

## Risks / Trade-offs

- Full text editing can sprawl quickly -> keep MVP to plain text, single document, deterministic operations, and no OS adapter implementation.
- IME behavior differs by platform -> model lifecycle as begin/update/commit/cancel and leave native event mapping to later adapters.
- Password controls can accidentally leak text through render paths -> test that display text is masked while document/password value remains explicit.
- Caret geometry is approximate without full glyph hit testing -> MVP tracks logical caret index; pixel-accurate hit testing can come later.
