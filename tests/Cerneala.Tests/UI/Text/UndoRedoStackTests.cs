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

    private static TextEditorSnapshot Snap(string text)
    {
        return new TextEditorSnapshot(text, new TextCaret(text.Length), TextSelection.Caret(text.Length));
    }

    [Fact]
    public void DefaultsAreUnlimitedAndEnabled()
    {
        UndoRedoStack stack = new();

        Assert.Equal(-1, stack.Limit);
        Assert.True(stack.IsEnabled);
        Assert.False(stack.CanUndo);
        Assert.False(stack.CanRedo);
    }

    [Fact]
    public void CanUndoAndCanRedoTrackStacks()
    {
        UndoRedoStack stack = new();

        stack.PushUndo(Snap("a"));
        Assert.True(stack.CanUndo);
        Assert.False(stack.CanRedo);

        Assert.True(stack.TryPopUndo(Snap("ab"), out _));
        Assert.False(stack.CanUndo);
        Assert.True(stack.CanRedo);
    }

    [Fact]
    public void LimitKeepsNewestUnitsAndDropsOldest()
    {
        UndoRedoStack stack = new() { Limit = 2 };

        stack.PushUndo(Snap("1"));
        stack.PushUndo(Snap("2"));
        stack.PushUndo(Snap("3"));

        Assert.Equal(2, stack.UndoCount);
        Assert.True(stack.TryPopUndo(Snap("x"), out TextEditorSnapshot first));
        Assert.Equal("3", first.Text);
        Assert.True(stack.TryPopUndo(Snap("y"), out TextEditorSnapshot second));
        Assert.Equal("2", second.Text);
        Assert.False(stack.TryPopUndo(Snap("z"), out _));
    }

    [Fact]
    public void LoweringLimitTrimsOldestImmediately()
    {
        UndoRedoStack stack = new();
        stack.PushUndo(Snap("1"));
        stack.PushUndo(Snap("2"));
        stack.PushUndo(Snap("3"));

        stack.Limit = 1;

        Assert.Equal(1, stack.UndoCount);
        Assert.True(stack.TryPopUndo(Snap("x"), out TextEditorSnapshot snapshot));
        Assert.Equal("3", snapshot.Text);
    }

    [Fact]
    public void LoweringLimitTrimsRedoOldestImmediately()
    {
        UndoRedoStack stack = new();
        stack.PushUndo(Snap("1"));
        stack.PushUndo(Snap("2"));
        stack.PushUndo(Snap("3"));
        Assert.True(stack.TryPopUndo(Snap("4"), out _));
        Assert.True(stack.TryPopUndo(Snap("3"), out _));
        Assert.True(stack.TryPopUndo(Snap("2"), out _));
        Assert.Equal(3, stack.RedoCount);

        stack.Limit = 1;

        Assert.Equal(1, stack.RedoCount);
    }

    [Fact]
    public void LimitZeroKeepsNothing()
    {
        UndoRedoStack stack = new() { Limit = 0 };

        stack.PushUndo(Snap("1"));

        Assert.Equal(0, stack.UndoCount);
        Assert.False(stack.CanUndo);
    }

    [Fact]
    public void LimitBelowMinusOneThrows()
    {
        UndoRedoStack stack = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => stack.Limit = -2);
    }

    [Fact]
    public void DisablingClearsBothStacksAndRecordsNothing()
    {
        UndoRedoStack stack = new();
        stack.PushUndo(Snap("1"));
        stack.PushUndo(Snap("2"));
        Assert.True(stack.TryPopUndo(Snap("3"), out _));

        stack.IsEnabled = false;

        Assert.Equal(0, stack.UndoCount);
        Assert.Equal(0, stack.RedoCount);
        stack.PushUndo(Snap("4"));
        Assert.False(stack.CanUndo);

        stack.IsEnabled = true;
        stack.PushUndo(Snap("5"));
        Assert.True(stack.CanUndo);
    }
}
