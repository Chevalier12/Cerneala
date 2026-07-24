using System.Globalization;

namespace Cerneala.UI.Text;

internal sealed class TextEditor
{
    private readonly bool recordsHistory;

    public TextEditor(TextDocument? document = null, bool recordsHistory = true)
    {
        Document = document ?? new TextDocument();
        this.recordsHistory = recordsHistory;
        Caret = TextCaret.At(Document.Length, Document.Length);
        Selection = TextSelection.Caret(Caret.Position);
    }

    public TextDocument Document { get; }

    public TextCaret Caret { get; private set; }

    public TextSelection Selection { get; private set; }

    public UndoRedoStack UndoRedo { get; } = new();

    public void SetText(string text)
    {
        ApplySnapshot(new TextEditorSnapshot(text ?? string.Empty, TextCaret.At((text ?? string.Empty).Length, (text ?? string.Empty).Length), TextSelection.Caret((text ?? string.Empty).Length)), recordUndo: true);
    }

    public void MoveCaret(int position, bool extendSelection = false)
    {
        int caretPosition = NormalizeCaretPosition(Document.Text, Caret.Position, position);
        TextCaret caret = TextCaret.At(caretPosition, Document.Length);
        Caret = caret;
        Selection = extendSelection
            ? new TextSelection(Selection.Anchor, caret.Position).Clamp(Document.Length)
            : TextSelection.Caret(caret.Position);
    }

    public void MoveCaretByTextElement(int direction, bool extendSelection = false)
    {
        if (direction < 0)
        {
            (int start, _) = GetPreviousTextElementRange(Document.Text, Caret.Position);
            MoveCaret(start, extendSelection);
            return;
        }

        if (direction > 0)
        {
            (int start, int length) = GetNextTextElementRange(Document.Text, Caret.Position);
            MoveCaret(start + length, extendSelection);
        }
    }

    public void Select(int anchor, int active)
    {
        Selection = NormalizeSelection(Document.Text, anchor, active);
        Caret = TextCaret.At(Selection.Active, Document.Length);
    }

    public void InsertText(string text)
    {
        string value = text ?? string.Empty;
        if (value.Length == 0 && Selection.IsEmpty)
        {
            return;
        }

        ReplaceSelection(value);
    }

    public void ReplaceSelection(string text)
    {
        Replace(Selection.Start, Selection.Length, text ?? string.Empty);
    }

    public void Backspace()
    {
        if (!Selection.IsEmpty)
        {
            ReplaceSelection(string.Empty);
            return;
        }

        if (Caret.Position == 0)
        {
            return;
        }

        (int start, int length) = GetPreviousTextElementRange(Document.Text, Caret.Position);
        Replace(start, length, string.Empty);
    }

    public void Delete()
    {
        if (!Selection.IsEmpty)
        {
            ReplaceSelection(string.Empty);
            return;
        }

        if (Caret.Position >= Document.Length)
        {
            return;
        }

        (int start, int length) = GetNextTextElementRange(Document.Text, Caret.Position);
        Replace(start, length, string.Empty);
    }

    public bool Undo()
    {
        if (!UndoRedo.TryPopUndo(Capture(), out TextEditorSnapshot snapshot))
        {
            return false;
        }

        ApplySnapshot(snapshot, recordUndo: false);
        return true;
    }

    public bool Redo()
    {
        if (!UndoRedo.TryPopRedo(Capture(), out TextEditorSnapshot snapshot))
        {
            return false;
        }

        ApplySnapshot(snapshot, recordUndo: false);
        return true;
    }

    public TextEditorSnapshot Capture()
    {
        return new TextEditorSnapshot(Document.Text, Caret, Selection);
    }

    private void Replace(int start, int length, string text)
    {
        Document.ValidateRange(start, length);
        TextEditorSnapshot before = Capture();
        Document.Replace(start, length, text);
        int caretPosition = start + text.Length;
        Caret = TextCaret.At(caretPosition, Document.Length);
        Selection = TextSelection.Caret(Caret.Position);

        if (recordsHistory &&
            (before.Text != Document.Text || before.Caret != Caret || before.Selection != Selection))
        {
            UndoRedo.PushUndo(before);
        }
    }

    private void ApplySnapshot(TextEditorSnapshot snapshot, bool recordUndo)
    {
        TextEditorSnapshot before = Capture();
        Document.SetText(snapshot.Text);
        Caret = TextCaret.At(snapshot.Caret.Position, Document.Length);
        Selection = snapshot.Selection.Clamp(Document.Length);
        if (recordsHistory && recordUndo && before != Capture())
        {
            UndoRedo.PushUndo(before);
        }
    }

    private static (int Start, int Length) GetPreviousTextElementRange(string text, int position)
    {
        int[] starts = StringInfo.ParseCombiningCharacters(text);
        for (int i = starts.Length - 1; i >= 0; i--)
        {
            int start = starts[i];
            if (start >= position)
            {
                continue;
            }

            int end = i + 1 < starts.Length ? starts[i + 1] : text.Length;
            return (start, end - start);
        }

        return (0, 0);
    }

    private static (int Start, int Length) GetNextTextElementRange(string text, int position)
    {
        int[] starts = StringInfo.ParseCombiningCharacters(text);
        for (int i = 0; i < starts.Length; i++)
        {
            int start = starts[i];
            int end = i + 1 < starts.Length ? starts[i + 1] : text.Length;
            if (end <= position)
            {
                continue;
            }

            return (start, end - start);
        }

        return (text.Length, 0);
    }

    private static int NormalizeCaretPosition(string text, int currentPosition, int requestedPosition)
    {
        int position = Math.Clamp(requestedPosition, 0, text.Length);
        if (position == 0 || position == text.Length)
        {
            return position;
        }

        int[] starts = StringInfo.ParseCombiningCharacters(text);
        for (int i = 0; i < starts.Length; i++)
        {
            int start = starts[i];
            int end = i + 1 < starts.Length ? starts[i + 1] : text.Length;
            if (position == start || position == end)
            {
                return position;
            }

            if (position > start && position < end)
            {
                return requestedPosition < currentPosition ? start : end;
            }
        }

        return position;
    }

    private TextSelection NormalizeSelection(string text, int anchor, int active)
    {
        if (anchor == active)
        {
            int caretPosition = NormalizeCaretPosition(text, Caret.Position, active);
            return TextSelection.Caret(caretPosition);
        }

        int start = Math.Min(anchor, active);
        int end = Math.Max(anchor, active);
        int normalizedStart = NormalizeCaretPosition(text, end, start);
        int normalizedEnd = NormalizeCaretPosition(text, start, end);

        return anchor <= active
            ? new TextSelection(normalizedStart, normalizedEnd)
            : new TextSelection(normalizedEnd, normalizedStart);
    }
}
