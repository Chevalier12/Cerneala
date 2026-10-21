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
    public void InsertTextExpandsSelectionAwayFromTextElementMiddle()
    {
        TextEditor editor = new(new TextDocument("a😀b"));
        editor.Select(1, 2);

        editor.InsertText("x");

        Assert.Equal("axb", editor.Document.Text);
        Assert.Equal(2, editor.Caret.Position);
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

    [Theory]
    [InlineData("hello world", 0, 6)]
    [InlineData("hello world", 6, 11)]
    [InlineData("hello world", 11, 11)]
    [InlineData("hello world", 3, 6)]
    [InlineData("a.b", 0, 1)]
    [InlineData("a.b", 1, 2)]
    [InlineData("a.b", 2, 3)]
    [InlineData("one\r\ntwo", 3, 5)]
    [InlineData("one\ntwo", 3, 4)]
    [InlineData("one  \r\ntwo", 0, 5)]
    [InlineData("a😀b", 0, 1)]
    [InlineData("a😀b", 1, 3)]
    [InlineData("", 0, 0)]
    public void GetNextWordStartFollowsWordClasses(string text, int position, int expected)
    {
        Assert.Equal(expected, TextWordBoundaries.GetNextWordStart(text, position));
    }

    [Theory]
    [InlineData("hello world", 11, 6)]
    [InlineData("hello world", 6, 0)]
    [InlineData("hello world", 8, 6)]
    [InlineData("hello world", 0, 0)]
    [InlineData("a.b", 3, 2)]
    [InlineData("a.b", 2, 1)]
    [InlineData("a.b", 1, 0)]
    [InlineData("one\r\ntwo", 5, 3)]
    [InlineData("one\ntwo", 4, 3)]
    [InlineData("one\n  two", 6, 4)]
    [InlineData("a😀b", 4, 3)]
    [InlineData("a😀b", 3, 1)]
    [InlineData("", 0, 0)]
    public void GetPreviousWordStartFollowsWordClasses(string text, int position, int expected)
    {
        Assert.Equal(expected, TextWordBoundaries.GetPreviousWordStart(text, position));
    }

    [Theory]
    [InlineData("hello world", 7, 6, 11)]
    [InlineData("hello world", 2, 0, 6)]
    [InlineData("hello world", 11, 6, 11)]
    [InlineData("hello world", 5, 5, 6)]
    [InlineData("a   b", 2, 1, 4)]
    [InlineData("a.b", 1, 1, 2)]
    [InlineData("one\ntwo", 3, 0, 3)]
    [InlineData("one\ntwo", 4, 4, 7)]
    [InlineData("one \ntwo", 2, 0, 4)]
    [InlineData("one \ntwo", 3, 3, 4)]
    [InlineData("", 0, 0, 0)]
    public void GetWordRangeIncludesTrailingWhitespace(string text, int position, int start, int end)
    {
        Assert.Equal((start, end), TextWordBoundaries.GetWordRange(text, position));
    }

    [Fact]
    public void GetWordRangeOnLineBreakIsEmpty()
    {
        Assert.Equal((3, 3), TextWordBoundaries.GetWordRange("\n\n\n", 3));
        Assert.Equal((1, 1), TextWordBoundaries.GetWordRange("\n\n\n", 1));
    }

    [Theory]
    [InlineData("one two\nfour", 2, 0, 7)]
    [InlineData("one two\nfour", 9, 8, 12)]
    [InlineData("one two\nfour", 7, 0, 7)]
    [InlineData("one two\nfour", 8, 8, 12)]
    [InlineData("one\r\ntwo", 1, 0, 3)]
    [InlineData("one\r\ntwo", 6, 5, 8)]
    [InlineData("a\n\nb", 2, 2, 2)]
    [InlineData("", 0, 0, 0)]
    public void GetLineRangeExcludesLineBreaks(string text, int position, int start, int end)
    {
        Assert.Equal((start, end), TextWordBoundaries.GetLineRange(text, position));
    }

    [Fact]
    public void MoveCaretByWordMovesCaretAndCollapsesSelection()
    {
        TextEditor editor = new(new TextDocument("hello big world"));
        editor.MoveCaret(0);

        editor.MoveCaretByWord(1);
        Assert.Equal(6, editor.Caret.Position);
        Assert.True(editor.Selection.IsEmpty);

        editor.MoveCaretByWord(1);
        Assert.Equal(10, editor.Caret.Position);

        editor.MoveCaretByWord(-1);
        Assert.Equal(6, editor.Caret.Position);

        editor.MoveCaretByWord(-1);
        Assert.Equal(0, editor.Caret.Position);
    }

    [Fact]
    public void MoveCaretByWordExtendsSelectionFromAnchor()
    {
        TextEditor editor = new(new TextDocument("hello big world"));
        editor.MoveCaret(0);

        editor.MoveCaretByWord(1, extendSelection: true);
        editor.MoveCaretByWord(1, extendSelection: true);

        Assert.Equal(new TextSelection(0, 10), editor.Selection);
        Assert.Equal(10, editor.Caret.Position);

        editor.MoveCaretByWord(-1, extendSelection: true);

        Assert.Equal(new TextSelection(0, 6), editor.Selection);
    }

    [Fact]
    public void MoveCaretByWordWithZeroDirectionDoesNothing()
    {
        TextEditor editor = new(new TextDocument("hello world"));
        editor.MoveCaret(3);

        editor.MoveCaretByWord(0);

        Assert.Equal(3, editor.Caret.Position);
    }

    [Fact]
    public void TypedInsertionsMergeIntoOneUndoUnit()
    {
        TextEditor editor = new(new TextDocument(string.Empty));

        editor.InsertTypedText("a");
        editor.InsertTypedText("b");
        editor.InsertTypedText("c");

        Assert.Equal("abc", editor.Document.Text);
        Assert.Equal(1, editor.UndoRedo.UndoCount);

        Assert.True(editor.Undo());
        Assert.Equal(string.Empty, editor.Document.Text);
        Assert.Equal(0, editor.Caret.Position);

        Assert.True(editor.Redo());
        Assert.Equal("abc", editor.Document.Text);
        Assert.Equal(3, editor.Caret.Position);
    }

    [Fact]
    public void CaretMoveBreaksTypingMerge()
    {
        TextEditor editor = new(new TextDocument(string.Empty));

        editor.InsertTypedText("a");
        editor.MoveCaret(0);
        editor.InsertTypedText("b");

        Assert.Equal("ba", editor.Document.Text);
        Assert.Equal(2, editor.UndoRedo.UndoCount);
        Assert.True(editor.Undo());
        Assert.Equal("a", editor.Document.Text);
    }

    [Fact]
    public void CaretMoveAndBackBreaksTypingMerge()
    {
        TextEditor editor = new(new TextDocument(string.Empty));

        editor.InsertTypedText("a");
        editor.MoveCaret(0);
        editor.MoveCaret(1);
        editor.InsertTypedText("b");

        Assert.Equal(2, editor.UndoRedo.UndoCount);
    }

    [Fact]
    public void SelectDeleteReplaceAndUndoBreakTypingMerge()
    {
        TextEditor select = new(new TextDocument(string.Empty));
        select.InsertTypedText("a");
        select.Select(0, 1);
        select.Select(1, 1);
        select.InsertTypedText("b");
        Assert.Equal(2, select.UndoRedo.UndoCount);

        TextEditor backspace = new(new TextDocument(string.Empty));
        backspace.InsertTypedText("a");
        backspace.Backspace();
        backspace.InsertTypedText("b");
        Assert.Equal(3, backspace.UndoRedo.UndoCount);

        TextEditor replace = new(new TextDocument(string.Empty));
        replace.InsertTypedText("a");
        replace.ReplaceSelection("x");
        replace.InsertTypedText("b");
        Assert.Equal(3, replace.UndoRedo.UndoCount);
        Assert.Equal("axb", replace.Document.Text);

        TextEditor undo = new(new TextDocument(string.Empty));
        undo.InsertTypedText("a");
        undo.InsertTypedText("b");
        Assert.True(undo.Undo());
        Assert.True(undo.Redo());
        undo.InsertTypedText("c");
        Assert.Equal(2, undo.UndoRedo.UndoCount);
        Assert.Equal("abc", undo.Document.Text);

        TextEditor setText = new(new TextDocument(string.Empty));
        setText.InsertTypedText("a");
        setText.SetText("a");
        setText.InsertTypedText("b");
        Assert.Equal(2, setText.UndoRedo.UndoCount);
    }

    [Fact]
    public void TypedInsertionAfterNonTypedInsertionStartsNewUnit()
    {
        TextEditor editor = new(new TextDocument(string.Empty));

        editor.InsertText("a");
        editor.InsertTypedText("b");
        editor.InsertTypedText("c");

        Assert.Equal("abc", editor.Document.Text);
        Assert.Equal(2, editor.UndoRedo.UndoCount);
        Assert.True(editor.Undo());
        Assert.Equal("a", editor.Document.Text);
    }

    [Fact]
    public void NonTypedEditsKeepOneUnitEach()
    {
        TextEditor editor = new(new TextDocument(string.Empty));

        editor.InsertText("a");
        editor.InsertText("b");
        editor.Backspace();

        Assert.Equal(3, editor.UndoRedo.UndoCount);
    }

    [Fact]
    public void TypedInsertionDoesNotMergeWhenHistoryWasDisabledAndReenabled()
    {
        TextEditor editor = new(new TextDocument(string.Empty));
        editor.InsertTypedText("a");
        editor.UndoRedo.IsEnabled = false;
        editor.UndoRedo.IsEnabled = true;

        editor.InsertTypedText("b");

        Assert.Equal("ab", editor.Document.Text);
        Assert.Equal(1, editor.UndoRedo.UndoCount);
        Assert.True(editor.Undo());
        Assert.Equal("a", editor.Document.Text);
    }

    [Fact]
    public void TypedInsertionHonorsUndoLimit()
    {
        TextEditor editor = new(new TextDocument(string.Empty));
        editor.UndoRedo.Limit = 1;

        editor.InsertText("a");
        editor.InsertText("b");

        Assert.Equal(1, editor.UndoRedo.UndoCount);
        Assert.True(editor.Undo());
        Assert.Equal("a", editor.Document.Text);
        Assert.False(editor.Undo());
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
