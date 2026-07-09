# UndoRedoStack Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/UndoRedoStack.cs`

Stores undo and redo snapshots for text editing.

```csharp
public sealed class UndoRedoStack
```

Inheritance:
`object` -> `UndoRedoStack`

## Examples

Push and restore editor snapshots:

```csharp
using Cerneala.UI.Text;

UndoRedoStack stack = new();
TextEditorSnapshot before = CaptureSnapshot();
TextEditorSnapshot current = CaptureSnapshot();

stack.PushUndo(before);

if (stack.TryPopUndo(current, out TextEditorSnapshot undoSnapshot))
{
    Restore(undoSnapshot);
}
```

## Remarks

`UndoRedoStack` keeps separate undo and redo stacks of `TextEditorSnapshot` values.

`PushUndo` pushes a snapshot onto the undo stack and clears redo history. `TryPopUndo` moves the current snapshot to redo before returning the previous undo snapshot. `TryPopRedo` moves the current snapshot to undo before returning the redo snapshot. `Clear` removes both stacks.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `UndoCount` | `int` | Gets the number of available undo snapshots. |
| `RedoCount` | `int` | Gets the number of available redo snapshots. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `PushUndo(TextEditorSnapshot snapshot)` | `void` | Pushes an undo snapshot and clears redo history. |
| `TryPopUndo(TextEditorSnapshot current, out TextEditorSnapshot snapshot)` | `bool` | Pops an undo snapshot and pushes the current snapshot to redo. |
| `TryPopRedo(TextEditorSnapshot current, out TextEditorSnapshot snapshot)` | `bool` | Pops a redo snapshot and pushes the current snapshot to undo. |
| `Clear()` | `void` | Clears undo and redo history. |

## Applies To

Cerneala UI text editor undo and redo state.

## See Also

- `Cerneala.UI.Text.TextEditorSnapshot`
- `Cerneala.UI.Text.TextEditor`
