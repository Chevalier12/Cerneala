namespace Cerneala.UI.Text;

internal sealed class UndoRedoStack
{
    // Both lists keep the newest unit at the end, so the top of a stack is the last element
    // and trimming the oldest units removes from the front.
    private readonly List<TextEditorSnapshot> undo = new();
    private readonly List<TextEditorSnapshot> redo = new();
    private int limit = -1;
    private bool isEnabled = true;

    public int UndoCount => undo.Count;

    public int RedoCount => redo.Count;

    public bool CanUndo => undo.Count > 0;

    public bool CanRedo => redo.Count > 0;

    /// <summary>
    /// Maximum number of units each stack keeps. -1 is unlimited, 0 keeps nothing, n keeps the newest n units.
    /// </summary>
    public int Limit
    {
        get => limit;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, -1);
            limit = value;
            Trim(undo);
            Trim(redo);
        }
    }

    /// <summary>
    /// When false both stacks are cleared and nothing is recorded.
    /// </summary>
    public bool IsEnabled
    {
        get => isEnabled;
        set
        {
            isEnabled = value;
            if (!value)
            {
                Clear();
            }
        }
    }

    public void PushUndo(TextEditorSnapshot snapshot)
    {
        if (!isEnabled)
        {
            return;
        }

        undo.Add(snapshot);
        Trim(undo);
        redo.Clear();
    }

    /// <summary>
    /// Removes the newest undo unit when it holds the given text, because undoing it would then change no text.
    /// </summary>
    public bool DiscardNewestUndoWithText(string text)
    {
        if (undo.Count == 0 || undo[^1].Text != text)
        {
            return false;
        }

        undo.RemoveAt(undo.Count - 1);
        return true;
    }

    public bool TryPopUndo(TextEditorSnapshot current, out TextEditorSnapshot snapshot)
    {
        if (undo.Count == 0)
        {
            snapshot = default;
            return false;
        }

        redo.Add(current);
        Trim(redo);
        snapshot = Pop(undo);
        return true;
    }

    public bool TryPopRedo(TextEditorSnapshot current, out TextEditorSnapshot snapshot)
    {
        if (redo.Count == 0)
        {
            snapshot = default;
            return false;
        }

        undo.Add(current);
        Trim(undo);
        snapshot = Pop(redo);
        return true;
    }

    public void Clear()
    {
        undo.Clear();
        redo.Clear();
    }

    private static TextEditorSnapshot Pop(List<TextEditorSnapshot> stack)
    {
        int last = stack.Count - 1;
        TextEditorSnapshot snapshot = stack[last];
        stack.RemoveAt(last);
        return snapshot;
    }

    private void Trim(List<TextEditorSnapshot> stack)
    {
        if (limit >= 0 && stack.Count > limit)
        {
            stack.RemoveRange(0, stack.Count - limit);
        }
    }
}
