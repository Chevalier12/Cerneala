using Cerneala.UI.Text;

namespace Cerneala.Tests.UI.Text;

public sealed class TextEditorTests
{
    [Fact]
    public void InsertTextReplacesSelectionAndMovesCaret()
    {
        TextEditor editor = new(new TextDocument("hello"));
        editor.Select(1, 4);

        editor.InsertText("i");

        Assert.Equal("hio", editor.Document.Text);
        Assert.Equal(2, editor.Caret.Position);
        Assert.True(editor.Selection.IsEmpty);
    }

    [Fact]
    public void BackspaceDeletesPreviousCharacter()
    {
        TextEditor editor = new(new TextDocument("abc"));
        editor.MoveCaret(2);

        editor.Backspace();

        Assert.Equal("ac", editor.Document.Text);
        Assert.Equal(1, editor.Caret.Position);
    }

    [Fact]
    public void BackspaceDeletesPreviousTextElement()
    {
        TextEditor editor = new(new TextDocument("a😀b"));
        editor.MoveCaret(3);

        editor.Backspace();

        Assert.Equal("ab", editor.Document.Text);
        Assert.Equal(1, editor.Caret.Position);
    }

    [Fact]
    public void DeleteRemovesNextTextElement()
    {
        TextEditor editor = new(new TextDocument("a😀b"));
        editor.MoveCaret(1);

        editor.Delete();

        Assert.Equal("ab", editor.Document.Text);
        Assert.Equal(1, editor.Caret.Position);
    }

    [Fact]
    public void UndoAndRedoRestoreDocumentCaretAndSelection()
    {
        TextEditor editor = new(new TextDocument("ab"));
        editor.MoveCaret(2);
        editor.InsertText("c");

        Assert.True(editor.Undo());
        Assert.Equal("ab", editor.Document.Text);
        Assert.Equal(2, editor.Caret.Position);

        Assert.True(editor.Redo());
        Assert.Equal("abc", editor.Document.Text);
        Assert.Equal(3, editor.Caret.Position);
    }

    [Fact]
    public void TextSelectionNormalizesRange()
    {
        TextSelection selection = new(5, 2);

        Assert.Equal(2, selection.Start);
        Assert.Equal(5, selection.End);
        Assert.Equal(3, selection.Length);
    }
}
