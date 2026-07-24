using Cerneala.UI.Input;

namespace Cerneala.UI.Text;

internal sealed class TextEditingController
{
    public TextEditingController(TextEditor editor)
    {
        Editor = editor ?? throw new ArgumentNullException(nameof(editor));
    }

    public TextEditor Editor { get; }

    public bool InsertText(string text)
    {
        string before = Editor.Document.Text;
        TextSelection selection = Editor.Selection;
        Editor.InsertText(text ?? string.Empty);
        return before != Editor.Document.Text || selection != Editor.Selection;
    }

    public bool HandleKey(InputKey key, bool extendSelection = false)
    {
        string before = Editor.Document.Text;
        TextSelection beforeSelection = Editor.Selection;
        TextCaret beforeCaret = Editor.Caret;

        switch (key)
        {
            case InputKey.Back:
                Editor.Backspace();
                break;
            case InputKey.Delete:
                Editor.Delete();
                break;
            case InputKey.Left:
                Editor.MoveCaretByTextElement(-1, extendSelection);
                break;
            case InputKey.Right:
                Editor.MoveCaretByTextElement(1, extendSelection);
                break;
            case InputKey.Home:
                Editor.MoveCaret(0, extendSelection);
                break;
            case InputKey.End:
                Editor.MoveCaret(Editor.Document.Length, extendSelection);
                break;
            default:
                return false;
        }

        return before != Editor.Document.Text ||
            beforeSelection != Editor.Selection ||
            beforeCaret != Editor.Caret;
    }
}
