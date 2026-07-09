# TextEditorSnapshot Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/UndoRedoStack.cs`

Represents a snapshot of text editor state for undo and redo operations.

```csharp
public readonly record struct TextEditorSnapshot(string Text, TextCaret Caret, TextSelection Selection)
```

Inheritance:
`ValueType` -> `TextEditorSnapshot`

## Examples

Store a snapshot in an undo stack:

```csharp
using Cerneala.UI.Text;

TextEditorSnapshot snapshot = new(
    Text: editor.Document.Text,
    Caret: editor.Caret,
    Selection: editor.Selection);

undoRedoStack.PushUndo(snapshot);
```

## Remarks

`TextEditorSnapshot` captures the document text, caret, and selection at a point in time.

`UndoRedoStack` stores these snapshots and returns them when undo or redo operations are requested.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Text` | `string` | Gets the captured document text. |
| `Caret` | `TextCaret` | Gets the captured caret position. |
| `Selection` | `TextSelection` | Gets the captured selection. |

## Applies To

Cerneala UI text editor undo and redo state.

## See Also

- `Cerneala.UI.Text.UndoRedoStack`
- `Cerneala.UI.Text.TextEditor`
- `Cerneala.UI.Text.TextCaret`
- `Cerneala.UI.Text.TextSelection`
