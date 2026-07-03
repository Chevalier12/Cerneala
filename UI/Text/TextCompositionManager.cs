namespace Cerneala.UI.Text;

public sealed class TextCompositionManager
{
    public TextCompositionState State { get; private set; } = TextCompositionState.Inactive;

    public void Begin(int start, string text = "")
    {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        State = new TextCompositionState(true, start, text ?? string.Empty);
    }

    public void Update(string text)
    {
        if (!State.IsActive)
        {
            throw new InvalidOperationException("Cannot update inactive text composition.");
        }

        State = State with { Text = text ?? string.Empty };
    }

    public string Commit()
    {
        if (!State.IsActive)
        {
            return string.Empty;
        }

        string text = State.Text;
        State = TextCompositionState.Inactive;
        return text;
    }

    public void Cancel()
    {
        State = TextCompositionState.Inactive;
    }

    public void CommitTo(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        int start = State.Start;
        string text = Commit();
        if (text.Length > 0)
        {
            editor.MoveCaret(start);
            editor.InsertText(text);
        }
    }
}
