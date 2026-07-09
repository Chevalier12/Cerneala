# TextEditor Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/TextEditor.cs`

Coordinates document text, caret position, selection, and undo/redo state for text editing operations.

```csharp
public sealed class TextEditor
```

Inheritance:
`object` -> `TextEditor`

## Examples

Replace a selected range and inspect the resulting editor state:

```csharp
using Cerneala.UI.Text;

TextEditor editor = new(new TextDocument("hello"));
editor.Select(1, 4);
editor.InsertText("i");

string text = editor.Document.Text; // "hio"
int caret = editor.Caret.Position;  // 2
bool hasSelection = !editor.Selection.IsEmpty; // false
```

Use undo and redo after an edit:

```csharp
using Cerneala.UI.Text;

TextEditor editor = new(new TextDocument("ab"));
editor.MoveCaret(2);
editor.InsertText("c");

bool undone = editor.Undo(); // Document.Text is "ab"
bool redone = editor.Redo(); // Document.Text is "abc"
```

## Remarks

`TextEditor` is the stateful editing layer used by controls such as `TextBoxBase` and by helpers such as `TextEditingController` and `TextCompositionManager`. It owns a `TextDocument`, exposes the current `TextCaret` and `TextSelection`, and records undo snapshots through `UndoRedoStack`.

Positions are string indices into the document text. Caret movement, selection normalization, backspace, and delete use `StringInfo.ParseCombiningCharacters` so operations snap to text element boundaries instead of splitting a combining sequence or surrogate pair. If a requested caret position falls inside a text element, it is normalized toward the requested movement direction.

Text replacement methods treat `null` replacement text as an empty string. `SetText` also treats `null` as an empty string and records an undo snapshot when the captured editor state changes. `Backspace`, `Delete`, `Undo`, and `Redo` do nothing when there is no applicable edit and report that through their return value when they return `bool`.

Range validation for direct replacements is delegated to `TextDocument.ValidateRange`; invalid ranges throw `ArgumentOutOfRangeException`.

## Constructors

| Signature | Description |
| --- | --- |
| `TextEditor(TextDocument? document = null)` | Initializes an editor for the specified document, or creates an empty `TextDocument` when `document` is `null`. The caret and selection start at the end of the document. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Document` | `TextDocument` | Gets the document edited by this instance. |
| `Caret` | `TextCaret` | Gets the current caret. The setter is private; use movement or edit methods to change it. |
| `Selection` | `TextSelection` | Gets the current selection. The setter is private; use `Select`, `MoveCaret`, or edit methods to change it. |
| `UndoRedo` | `UndoRedoStack` | Gets the undo/redo history used by this editor. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `SetText(string text)` | `void` | Replaces the whole document text, moves the caret to the end, clears the selection, and records undo when state changes. |
| `MoveCaret(int position, bool extendSelection = false)` | `void` | Moves the caret to a normalized document position. When `extendSelection` is `true`, extends the current selection from its existing anchor. |
| `MoveCaretByTextElement(int direction, bool extendSelection = false)` | `void` | Moves left when `direction` is negative, right when `direction` is positive, and does nothing when `direction` is zero. Movement is by text element. |
| `Select(int anchor, int active)` | `void` | Sets a normalized selection and moves the caret to the active end. |
| `InsertText(string text)` | `void` | Inserts text at the caret or replaces the current selection. Empty text with an empty selection is ignored. |
| `ReplaceSelection(string text)` | `void` | Replaces the current selection with the specified text. |
| `Backspace()` | `void` | Deletes the current selection, or the previous text element when the selection is empty and the caret is not at the start. |
| `Delete()` | `void` | Deletes the current selection, or the next text element when the selection is empty and the caret is not at the end. |
| `Undo()` | `bool` | Restores the previous editor snapshot when available and returns `true`; otherwise returns `false`. |
| `Redo()` | `bool` | Restores the next redo snapshot when available and returns `true`; otherwise returns `false`. |
| `Capture()` | `TextEditorSnapshot` | Captures the current document text, caret, and selection. |

## Applies To

Cerneala UI text editing infrastructure.

## See Also

- `Cerneala.UI.Text.TextDocument`
- `Cerneala.UI.Text.TextCaret`
- `Cerneala.UI.Text.TextSelection`
- `Cerneala.UI.Text.UndoRedoStack`
- `Cerneala.UI.Text.TextEditingController`
- `Cerneala.UI.Controls.TextBoxBase`
