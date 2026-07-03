namespace Cerneala.UI.Text;

public sealed class UndoRedoStack
{
    private readonly Stack<TextEditorSnapshot> undo = new();
    private readonly Stack<TextEditorSnapshot> redo = new();

    public int UndoCount => undo.Count;

    public int RedoCount => redo.Count;

    public void PushUndo(TextEditorSnapshot snapshot)
    {
        undo.Push(snapshot);
        redo.Clear();
    }

    public bool TryPopUndo(TextEditorSnapshot current, out TextEditorSnapshot snapshot)
    {
        if (undo.Count == 0)
        {
            snapshot = default;
            return false;
        }

        redo.Push(current);
        snapshot = undo.Pop();
        return true;
    }

    public bool TryPopRedo(TextEditorSnapshot current, out TextEditorSnapshot snapshot)
    {
        if (redo.Count == 0)
        {
            snapshot = default;
            return false;
        }

        undo.Push(current);
        snapshot = redo.Pop();
        return true;
    }

    public void Clear()
    {
        undo.Clear();
        redo.Clear();
    }
}

public readonly record struct TextEditorSnapshot(string Text, TextCaret Caret, TextSelection Selection);
