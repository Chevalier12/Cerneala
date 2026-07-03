using Cerneala.UI.Input;
using Cerneala.UI.Text;

namespace Cerneala.Tests.UI.Text;

public sealed class TextEditingControllerTests
{
    [Fact]
    public void InsertTextDelegatesToEditorAndReportsChange()
    {
        TextEditor editor = new(new TextDocument("ab"));
        editor.MoveCaret(1);
        TextEditingController controller = new(editor);

        bool changed = controller.InsertText("X");

        Assert.True(changed);
        Assert.Equal("aXb", editor.Document.Text);
        Assert.Equal(2, editor.Caret.Position);
    }

    [Fact]
    public void HandleKeyDeletesAndMovesCaret()
    {
        TextEditor editor = new(new TextDocument("abc"));
        editor.MoveCaret(2);
        TextEditingController controller = new(editor);

        Assert.True(controller.HandleKey(InputKey.Back));
        Assert.Equal("ac", editor.Document.Text);
        Assert.True(controller.HandleKey(InputKey.End));
        Assert.Equal(2, editor.Caret.Position);
    }

    [Fact]
    public void HandleKeyCanExtendSelection()
    {
        TextEditor editor = new(new TextDocument("abc"));
        editor.MoveCaret(1);
        TextEditingController controller = new(editor);

        bool changed = controller.HandleKey(InputKey.Right, extendSelection: true);

        Assert.True(changed);
        Assert.Equal(new TextSelection(1, 2), editor.Selection);
    }
}
