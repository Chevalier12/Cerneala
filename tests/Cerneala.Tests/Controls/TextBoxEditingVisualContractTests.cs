using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.Controls;

public sealed class TextBoxEditingVisualContractTests
{
    private static readonly DrawColor CaretColor = new(250, 10, 20);
    private static readonly DrawColor SelectionColor = new(20, 120, 250);

    [Fact]
    public void FocusedTextBoxRendersCaretCommand()
    {
        TextBox textBox = ArrangedTextBox("abc");
        textBox.IsKeyboardFocused = true;
        textBox.CaretColor = CaretColor;
        textBox.MoveCaret(1);

        DrawCommand caret = Render(textBox).Single(command => command.Kind == DrawCommandKind.FillRectangle && command.Color == CaretColor);

        Assert.True(caret.Rect.Height > 0);
        Assert.True(caret.Rect.Width > 0);
    }

    [Fact]
    public void UnfocusedTextBoxDoesNotRenderCaretCommand()
    {
        TextBox textBox = ArrangedTextBox("abc");
        textBox.CaretColor = CaretColor;
        textBox.MoveCaret(1);

        Assert.DoesNotContain(Render(textBox), command => command.Kind == DrawCommandKind.FillRectangle && command.Color == CaretColor);
    }

    [Fact]
    public void CaretMoveInvalidatesRenderWithoutMeasure()
    {
        TextBox textBox = ArrangedTextBox("abc");

        textBox.MoveCaret(1);

        Assert.True(textBox.DirtyState.Has(InvalidationFlags.Render));
        Assert.False(textBox.DirtyState.Has(InvalidationFlags.Measure));
    }

    [Fact]
    public void SelectionRangeRendersHighlightBeforeText()
    {
        TextBox textBox = ArrangedTextBox("abcd");
        textBox.SelectionBackground = SelectionColor;
        textBox.Select(1, 3);

        DrawCommand[] commands = Render(textBox).ToArray();
        int selectionIndex = Array.FindIndex(commands, command => command.Kind == DrawCommandKind.FillRectangle && command.Color == SelectionColor);
        int textIndex = Array.FindIndex(commands, command => command.Kind == DrawCommandKind.DrawText);

        Assert.InRange(selectionIndex, 0, textIndex - 1);
    }

    [Fact]
    public void SelectionChangeInvalidatesRenderWithoutMeasure()
    {
        TextBox textBox = ArrangedTextBox("abcd");

        textBox.Select(1, 3);

        Assert.True(textBox.DirtyState.Has(InvalidationFlags.Render));
        Assert.False(textBox.DirtyState.Has(InvalidationFlags.Measure));
    }

    [Fact]
    public void ReplacingSelectedTextUpdatesTextAndClearsSelectionPredictably()
    {
        TextBox textBox = ArrangedTextBox("abcd");
        textBox.Select(1, 3);

        textBox.ReceiveTextInput("X");

        Assert.Equal("aXd", textBox.Text);
        Assert.True(textBox.Selection.IsEmpty);
        Assert.Equal(2, textBox.Caret.Position);
    }

    [Fact]
    public void CaretScrollsIntoViewWhenTextExceedsContentWidth()
    {
        TextBox textBox = ArrangedTextBox("abcdefghijklmnop", width: 48);
        textBox.IsKeyboardFocused = true;
        textBox.CaretColor = CaretColor;
        textBox.MoveCaret(textBox.Text.Length);

        DrawCommand caret = Render(textBox).Single(command => command.Kind == DrawCommandKind.FillRectangle && command.Color == CaretColor);

        Assert.InRange(caret.Rect.X, textBox.ArrangedBounds.X, textBox.ArrangedBounds.X + textBox.ArrangedBounds.Width);
    }

    [Fact]
    public void BackspaceNearStartScrollsCaretBackIntoView()
    {
        TextBox textBox = ArrangedTextBox("abcdefghijklmnop", width: 48);
        textBox.IsKeyboardFocused = true;
        textBox.CaretColor = CaretColor;
        textBox.MoveCaret(textBox.Text.Length);
        textBox.MoveCaret(1);

        DrawCommand caret = Render(textBox).Single(command => command.Kind == DrawCommandKind.FillRectangle && command.Color == CaretColor);

        Assert.InRange(caret.Rect.X, textBox.ArrangedBounds.X, textBox.ArrangedBounds.X + textBox.ArrangedBounds.Width);
    }

    [Fact]
    public void ProgrammaticTextResetResetsHorizontalViewportWhenCaretAtStart()
    {
        TextBox textBox = ArrangedTextBox("abcdefghijklmnop", width: 48);
        textBox.IsKeyboardFocused = true;
        textBox.CaretColor = CaretColor;
        textBox.MoveCaret(textBox.Text.Length);

        textBox.Text = "a";
        textBox.MoveCaret(0);
        DrawCommand caret = Render(textBox).Single(command => command.Kind == DrawCommandKind.FillRectangle && command.Color == CaretColor);

        Assert.True(caret.Rect.X <= textBox.ArrangedBounds.X + 6);
    }

    private static TextBox ArrangedTextBox(string text, float width = 160)
    {
        TextBox textBox = new() { Text = text };
        textBox.Measure(new MeasureContext(new LayoutSize(width, 40)));
        textBox.Arrange(new ArrangeContext(new LayoutRect(0, 0, width, 32)));
        textBox.DirtyState.ClearAll();
        return textBox;
    }

    private static DrawCommandList Render(TextBox textBox)
    {
        DrawCommandList commands = new();
        textBox.Render(new RenderContext(
            textBox,
            new DrawingContext(commands),
            textBox.ArrangedBounds,
            RenderLayer.Default,
            new RenderCounters()));
        return commands;
    }
}
