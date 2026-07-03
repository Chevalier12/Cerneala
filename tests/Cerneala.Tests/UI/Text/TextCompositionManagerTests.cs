using Cerneala.UI.Text;

namespace Cerneala.Tests.UI.Text;

public sealed class TextCompositionManagerTests
{
    [Fact]
    public void UpdateStoresPreviewWithoutMutatingDocument()
    {
        TextEditor editor = new(new TextDocument("a"));
        TextCompositionManager manager = new();

        manager.Begin(1);
        manager.Update("b");

        Assert.True(manager.State.IsActive);
        Assert.Equal("b", manager.State.Text);
        Assert.Equal("a", editor.Document.Text);
    }

    [Fact]
    public void CommitToEditorInsertsTextAndClearsComposition()
    {
        TextEditor editor = new(new TextDocument("a"));
        editor.MoveCaret(1);
        TextCompositionManager manager = new();
        manager.Begin(1, "b");

        manager.CommitTo(editor);

        Assert.False(manager.State.IsActive);
        Assert.Equal("ab", editor.Document.Text);
    }

    [Fact]
    public void CommitToEditorInsertsTextAtCompositionStart()
    {
        TextEditor editor = new(new TextDocument("ac"));
        editor.MoveCaret(2);
        TextCompositionManager manager = new();
        manager.Begin(1, "b");

        manager.CommitTo(editor);

        Assert.False(manager.State.IsActive);
        Assert.Equal("abc", editor.Document.Text);
        Assert.Equal(2, editor.Caret.Position);
    }

    [Fact]
    public void CancelKeepsDocumentUnchanged()
    {
        TextEditor editor = new(new TextDocument("a"));
        TextCompositionManager manager = new();
        manager.Begin(1, "b");

        manager.Cancel();

        Assert.False(manager.State.IsActive);
        Assert.Equal("a", editor.Document.Text);
    }
}
