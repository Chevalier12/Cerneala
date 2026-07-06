using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.UI.Controls;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;
using Cerneala.UI.Text;

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
    public void CaretUsesTextLineVerticalBounds()
    {
        TextBox textBox = ArrangedSystemFontTextBox("a", width: 160, fontSize: 16);
        textBox.IsKeyboardFocused = true;
        textBox.CaretColor = CaretColor;
        textBox.MoveCaret(textBox.Text.Length);

        DrawCommand caret = Render(textBox).Single(command => command.Kind == DrawCommandKind.FillRectangle && command.Color == CaretColor);
        TextCaretVerticalMetrics expectedMetrics = TextCaretLayout.Default.GetCaretVerticalMetrics(
            CreateTextStyle(textBox),
            new FontResolver(textBox.ResourceProvider!));

        Assert.Equal(ContentY(textBox) + expectedMetrics.OffsetY, caret.Rect.Y, precision: 2);
        Assert.Equal(ContentY(textBox), caret.Rect.Y, precision: 2);
        Assert.Equal(expectedMetrics.Height, caret.Rect.Height, precision: 2);
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
    public void CaretAtEndUsesShapedTextMetrics()
    {
        TextBox textBox = ArrangedSystemFontTextBox("iiiiWWWW", width: 220);
        textBox.IsKeyboardFocused = true;
        textBox.CaretColor = CaretColor;
        textBox.MoveCaret(textBox.Text.Length);

        DrawCommand caret = Render(textBox).Single(command => command.Kind == DrawCommandKind.FillRectangle && command.Color == CaretColor);
        float expectedX = ContentX(textBox) + TextCaretLayout.Default.GetCaretX(
            textBox.Text,
            textBox.Text.Length,
            CreateTextStyle(textBox),
            new FontResolver(textBox.ResourceProvider!));

        Assert.Equal(expectedX, caret.Rect.X, precision: 2);
    }

    [Fact]
    public void HorizontalViewportKeepsCaretAlignedToShapedMetrics()
    {
        TextBox textBox = ArrangedSystemFontTextBox("iiiiWWWW", width: 56);
        textBox.IsKeyboardFocused = true;
        textBox.CaretColor = CaretColor;
        textBox.MoveCaret(textBox.Text.Length);
        textBox.Editor.MoveCaret(textBox.Text.Length - 1);

        DrawCommand caret = Render(textBox).Single(command => command.Kind == DrawCommandKind.FillRectangle && command.Color == CaretColor);
        float contentX = ContentX(textBox);
        float contentWidth = ContentWidth(textBox);
        float fullWidth = TextCaretLayout.Default.GetCaretX(textBox.Text, textBox.Text.Length, CreateTextStyle(textBox), new FontResolver(textBox.ResourceProvider!));
        float caretWidth = TextCaretLayout.Default.GetCaretX(textBox.Text, textBox.Caret.Position, CreateTextStyle(textBox), new FontResolver(textBox.ResourceProvider!));
        float expectedX = contentX + caretWidth - MathF.Max(0, fullWidth - contentWidth);

        Assert.InRange(caret.Rect.X, contentX, contentX + contentWidth);
        Assert.Equal(expectedX, caret.Rect.X, precision: 2);
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

    private static TextBox ArrangedSystemFontTextBox(string text, float width, float fontSize = 20)
    {
        ResourceStore store = new();
        ResourceId<FontResource> fontId = new("Input");
        store.SetResource(fontId, new FontResource(new SystemFontSource().LoadFont("Arial", fontSize)));
        TextBox textBox = new()
        {
            Text = text,
            FontSize = fontSize,
            FontResourceId = fontId,
            ResourceProvider = store
        };
        textBox.Measure(new MeasureContext(new LayoutSize(width, 40)));
        textBox.Arrange(new ArrangeContext(new LayoutRect(0, 0, width, 32)));
        textBox.DirtyState.ClearAll();
        return textBox;
    }

    private static TextRunStyle CreateTextStyle(TextBox textBox)
    {
        return new TextRunStyle(textBox.FontFamily, textBox.FontSize, color: textBox.Foreground, fontResourceId: textBox.FontResourceId);
    }

    private static float ContentX(TextBox textBox)
    {
        return textBox.ArrangedBounds.X + textBox.BorderThickness.Left + textBox.Padding.Left;
    }

    private static float ContentWidth(TextBox textBox)
    {
        return textBox.ArrangedBounds.Width - textBox.BorderThickness.Left - textBox.Padding.Left - textBox.BorderThickness.Right - textBox.Padding.Right;
    }

    private static float ContentY(TextBox textBox)
    {
        return textBox.ArrangedBounds.Y + textBox.BorderThickness.Top + textBox.Padding.Top;
    }

    private static float ContentHeight(TextBox textBox)
    {
        return textBox.ArrangedBounds.Height - textBox.BorderThickness.Top - textBox.Padding.Top - textBox.BorderThickness.Bottom - textBox.Padding.Bottom;
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
