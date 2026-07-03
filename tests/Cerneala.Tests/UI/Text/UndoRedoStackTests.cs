using Cerneala.UI.Text;

namespace Cerneala.Tests.UI.Text;

public sealed class UndoRedoStackTests
{
    [Fact]
    public void PushUndoClearsRedoHistory()
    {
        UndoRedoStack stack = new();
        TextEditorSnapshot first = new("a", new TextCaret(1), TextSelection.Caret(1));
        TextEditorSnapshot second = new("ab", new TextCaret(2), TextSelection.Caret(2));

        stack.PushUndo(first);
        Assert.True(stack.TryPopUndo(second, out _));
        Assert.Equal(1, stack.RedoCount);

        stack.PushUndo(second);

        Assert.Equal(0, stack.RedoCount);
        Assert.Equal(1, stack.UndoCount);
    }
}
