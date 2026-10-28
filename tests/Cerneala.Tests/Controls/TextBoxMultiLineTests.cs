using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Text;

namespace Cerneala.Tests.Controls;

public sealed class TextBoxMultiLineTests
{
    // The TextBox default Padding is (4, 2, 4, 2) and its default BorderThickness is a positive 1, painted outside the
    // arranged bounds, so the vertical insets are 2 + 2 = 4.
    private const float VerticalInsets = 4;

    [Fact]
    public void AcceptsReturnEnterInsertsNewLineBetweenTypedText()
    {
        TextBox textBox = new() { AcceptsReturn = true, Width = 160, VerticalAlignment = VerticalAlignment.Top };
        UIRoot root = Host(textBox, out ElementInputBridge bridge);

        bridge.Dispatch(root, TextFrame("a"));
        bridge.Dispatch(root, KeyFrame(InputKey.Enter));
        bridge.Dispatch(root, TextFrame("b"));
        root.ProcessFrame();

        Assert.Equal("a" + Environment.NewLine + "b", textBox.Text);
        Assert.Equal(2, textBox.LineCount);
    }

    [Fact]
    public void EnterWithoutAcceptsReturnLeavesTextOnOneLine()
    {
        TextBox textBox = new() { Width = 160, VerticalAlignment = VerticalAlignment.Top };
        UIRoot root = Host(textBox, out ElementInputBridge bridge);
        bool rootSawEnter = false;
        root.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, args) =>
        {
            if (args is KeyEventArgs { Key: InputKey.Enter })
            {
                rootSawEnter = true;
            }
        });

        bridge.Dispatch(root, TextFrame("a"));
        bridge.Dispatch(root, KeyFrame(InputKey.Enter));
        bridge.Dispatch(root, TextFrame("b"));

        Assert.Equal("ab", textBox.Text);
        Assert.True(rootSawEnter);
    }

    [Fact]
    public void WrapBreaksNarrowTextIntoLinesAndDownMovesToNextLine()
    {
        TextBox textBox = new()
        {
            Text = "one two three",
            TextWrapping = TextWrapping.Wrap,
            Width = 60,
            VerticalAlignment = VerticalAlignment.Top
        };
        UIRoot root = Host(textBox, out ElementInputBridge bridge);
        textBox.MoveCaret(0);

        Assert.True(textBox.LineCount > 1);
        string firstLine = textBox.GetLineText(0);

        bridge.Dispatch(root, KeyFrame(InputKey.Down));

        Assert.Equal(firstLine.Length, textBox.Caret.Position);
        Assert.Equal("one ", firstLine);
    }

    [Fact]
    public void MinLinesSetsDesiredHeightToThatManyLines()
    {
        TextBox textBox = new() { MinLines = 3, Width = 160, VerticalAlignment = VerticalAlignment.Top };
        UIRoot root = Host(textBox, out _);

        Assert.InRange(textBox.ArrangedBounds.Height, (3 * LineHeight(textBox)) + VerticalInsets - 1, (3 * LineHeight(textBox)) + VerticalInsets + 1);
        GC.KeepAlive(root);
    }

    [Fact]
    public void FontSizeChangeGrowsMinLinesHeightInOneFrame()
    {
        TextBox textBox = new() { MinLines = 3, Width = 160, VerticalAlignment = VerticalAlignment.Top };
        UIRoot root = Host(textBox, out _);
        float before = textBox.ArrangedBounds.Height;

        textBox.FontSize = 32;
        root.ProcessFrame();

        Assert.True(textBox.ArrangedBounds.Height > before, $"Height {textBox.ArrangedBounds.Height} did not grow from {before}.");
        Assert.InRange(textBox.ArrangedBounds.Height, (3 * LineHeight(textBox)) + VerticalInsets - 1, (3 * LineHeight(textBox)) + VerticalInsets + 1);
    }

    [Fact]
    public void MaxLinesCapsHeightOfLongText()
    {
        TextBox textBox = new()
        {
            Text = TenLines(),
            MaxLines = 3,
            Width = 160,
            VerticalAlignment = VerticalAlignment.Top
        };
        UIRoot root = Host(textBox, out _);

        Assert.Equal(10, textBox.LineCount);
        Assert.InRange(textBox.ArrangedBounds.Height, (3 * LineHeight(textBox)) + VerticalInsets - 1, (3 * LineHeight(textBox)) + VerticalInsets + 1);
        GC.KeepAlive(root);
    }

    [Fact]
    public void MinLinesGreaterThanMaxLinesThrowsAtMeasure()
    {
        TextBox textBox = new() { MinLines = 4, MaxLines = 2 };

        Assert.Throws<InvalidOperationException>(() => textBox.Measure(new MeasureContext(new LayoutSize(100, 100))));
        Assert.Throws<ArgumentException>(() => textBox.MinLines = 0);
        Assert.Throws<ArgumentException>(() => textBox.MaxLines = 0);
    }

    [Fact]
    public void AutoVerticalScrollBarShowsAndWheelScrollsWithoutMovingCaretOnBarPress()
    {
        TextBox textBox = new()
        {
            Text = TenLines(),
            MaxLines = 3,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Width = 160,
            VerticalAlignment = VerticalAlignment.Top
        };
        UIRoot root = Host(textBox, out ElementInputBridge bridge);
        textBox.Select(2, 5);
        ScrollViewer contentHost = ContentHost(textBox);

        Assert.Equal(Visibility.Visible, contentHost.VerticalScrollBar.Visibility);

        LayoutRect presenter = contentHost.Presenter.ArrangedBounds;
        bridge.Dispatch(root, WheelFrame(presenter.X + 10, presenter.Y + 5, -120));
        root.ProcessFrame();

        Assert.True(contentHost.Presenter.VerticalOffset > 0);

        TextSelection selection = textBox.Selection;
        TextCaret caret = textBox.Caret;
        LayoutRect bar = contentHost.VerticalScrollBar.ArrangedBounds;
        Click(root, bar.X + (bar.Width / 2), bar.Y + (bar.Height / 2));
        root.ProcessFrame();

        Assert.Equal(selection, textBox.Selection);
        Assert.Equal(caret, textBox.Caret);
    }

    [Fact]
    public void WheelOverTextBoxThatCannotScrollStaysUnhandled()
    {
        TextBox textBox = new() { Text = "short", Width = 160, VerticalAlignment = VerticalAlignment.Top };
        UIRoot root = Host(textBox, out ElementInputBridge bridge);
        bool rootSawWheel = false;
        root.Handlers.AddHandler(InputEvents.MouseWheelEvent, (_, _) => rootSawWheel = true);

        bridge.Dispatch(root, WheelFrame(textBox.ArrangedBounds.X + 10, textBox.ArrangedBounds.Y + 8, -120));

        Assert.True(rootSawWheel);
    }

    [Fact]
    public void CursorIsIBeamOverTextAndArrowOverInnerScrollBar()
    {
        TextBox textBox = new()
        {
            Text = TenLines(),
            MaxLines = 3,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            Width = 160,
            VerticalAlignment = VerticalAlignment.Top
        };
        UIRoot root = Host(textBox, out _);
        ScrollViewer contentHost = ContentHost(textBox);
        LayoutRect presenter = contentHost.Presenter.ArrangedBounds;
        LayoutRect bar = contentHost.VerticalScrollBar.ArrangedBounds;
        CursorService cursors = new();

        Assert.Equal(Cursor.IBeam, cursors.Resolve(root, presenter.X + 5, presenter.Y + 5));
        Assert.Equal(Cursor.Arrow, cursors.Resolve(root, bar.X + (bar.Width / 2), bar.Y + (bar.Height / 2)));
    }

    [Fact]
    public void VerticalAndLineNavigationKeysMoveBetweenDrawnLines()
    {
        TextBox textBox = new() { Text = "aaa\naaaaa\naa", Width = 160, VerticalAlignment = VerticalAlignment.Top };
        UIRoot root = Host(textBox, out ElementInputBridge bridge);

        textBox.MoveCaret(8);
        bridge.Dispatch(root, KeyFrame(InputKey.Down));
        Assert.Equal(12, textBox.Caret.Position);
        bridge.Dispatch(root, KeyFrame(InputKey.Up));
        Assert.Equal(8, textBox.Caret.Position);

        bridge.Dispatch(root, KeyFrame(InputKey.Home));
        Assert.Equal(4, textBox.Caret.Position);
        bridge.Dispatch(root, KeyFrame(InputKey.End));
        Assert.Equal(9, textBox.Caret.Position);
        bridge.Dispatch(root, KeyFrame(InputKey.Home, InputKey.LeftCtrl));
        Assert.Equal(0, textBox.Caret.Position);
        bridge.Dispatch(root, KeyFrame(InputKey.End, InputKey.LeftCtrl));
        Assert.Equal(12, textBox.Caret.Position);

        textBox.MoveCaret(2);
        bridge.Dispatch(root, KeyFrame(InputKey.Up));
        Assert.Equal(0, textBox.Caret.Position);

        textBox.MoveCaret(10);
        bridge.Dispatch(root, KeyFrame(InputKey.Down));
        Assert.Equal(12, textBox.Caret.Position);

        textBox.MoveCaret(1);
        bridge.Dispatch(root, KeyFrame(InputKey.PageDown));
        Assert.Equal(11, textBox.Caret.Position);
    }

    [Fact]
    public void ShiftExtendsSelectionForLineNavigationKeys()
    {
        TextBox textBox = new() { Text = "aaa\naaaaa\naa", Width = 160, VerticalAlignment = VerticalAlignment.Top };
        UIRoot root = Host(textBox, out ElementInputBridge bridge);
        textBox.MoveCaret(1);

        bridge.Dispatch(root, KeyFrame(InputKey.Down, InputKey.LeftShift));

        Assert.Equal(1, textBox.Selection.Anchor);
        Assert.Equal(5, textBox.Selection.Active);

        bridge.Dispatch(root, KeyFrame(InputKey.End, InputKey.LeftShift));

        Assert.Equal(1, textBox.Selection.Anchor);
        Assert.Equal(9, textBox.Selection.Active);

        bridge.Dispatch(root, KeyFrame(InputKey.End, InputKey.LeftShift, InputKey.LeftCtrl));

        Assert.Equal(1, textBox.Selection.Anchor);
        Assert.Equal(12, textBox.Selection.Active);
    }

    [Fact]
    public void EnterRepeatedlyKeepsCaretLineInsideViewport()
    {
        TextBox textBox = new()
        {
            AcceptsReturn = true,
            MaxLines = 3,
            Width = 160,
            VerticalAlignment = VerticalAlignment.Top
        };
        UIRoot root = Host(textBox, out ElementInputBridge bridge);

        for (int i = 0; i < 8; i++)
        {
            bridge.Dispatch(root, KeyFrame(InputKey.Enter));
            root.ProcessFrame();
        }

        root.ProcessFrame();
        ScrollContentPresenter presenter = ContentHost(textBox).Presenter;
        float lineHeight = LineHeight(textBox);
        float caretTop = (textBox.LineCount - 1) * lineHeight;

        Assert.Equal(9, textBox.LineCount);
        Assert.Equal(textBox.Text.Length, textBox.Caret.Position);
        Assert.True(presenter.VerticalOffset > 0);
        Assert.InRange(caretTop, presenter.VerticalOffset - 0.5f, presenter.VerticalOffset + presenter.ViewportHeight);
        Assert.InRange(caretTop + lineHeight, presenter.VerticalOffset, presenter.VerticalOffset + presenter.ViewportHeight + 0.5f);
    }

    [Fact]
    public void GetLineTextIncludesLineBreakAndScrollToLineScrolls()
    {
        TextBox textBox = new()
        {
            Text = TenLines(),
            MaxLines = 3,
            Width = 160,
            VerticalAlignment = VerticalAlignment.Top
        };
        UIRoot root = Host(textBox, out _);

        Assert.Equal("line 0\n", textBox.GetLineText(0));
        Assert.Equal("line 9", textBox.GetLineText(9));
        Assert.Throws<ArgumentOutOfRangeException>(() => textBox.GetLineText(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => textBox.GetLineText(10));
        Assert.Throws<ArgumentOutOfRangeException>(() => textBox.ScrollToLine(10));

        textBox.ScrollToLine(9);
        root.ProcessFrame();

        ScrollContentPresenter presenter = ContentHost(textBox).Presenter;
        float lineHeight = LineHeight(textBox);
        Assert.True(presenter.VerticalOffset > 0);
        Assert.InRange((10 * lineHeight), presenter.VerticalOffset, presenter.VerticalOffset + presenter.ViewportHeight + 0.5f);
    }

    // #916: the caret placed at the end of the second, visible wrapped line is drawn on that line, so the least scroll that
    // shows it is none. The click in the middle of the line is the control.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClickPastTheEndOfAVisibleWrappedLineDoesNotScroll(bool clickPastTheEnd)
    {
        TextBox textBox = WrappedTextBoxShowingTwoOfThreeLines(out UIRoot root, out ScrollContentPresenter presenter);
        LayoutRect view = textBox.TextView.ArrangedBounds;
        float lineWidth = LineTextWidth(textBox, 1);
        float x = view.X + (clickPastTheEnd ? lineWidth + 10 : lineWidth / 2);
        float y = view.Y + (1.5f * LineHeight(textBox));
        Assert.True(x < view.X + view.Width, $"The click x {x} is outside the text view ({view.X}..{view.X + view.Width}).");

        Click(root, x, y);
        root.ProcessFrame();

        Assert.True(presenter.VerticalOffset == 0, $"VerticalOffset {presenter.VerticalOffset} after the click at ({x}, {y}).");
    }

    // #916: a drag that ends past the end of the second, visible wrapped line puts the caret at the end of that line,
    // which is in view, so the drag does not scroll.
    [Fact]
    public void DragToPastTheEndOfAVisibleWrappedLineDoesNotScroll()
    {
        TextBox textBox = WrappedTextBoxShowingTwoOfThreeLines(out UIRoot root, out ScrollContentPresenter presenter);
        LayoutRect view = textBox.TextView.ArrangedBounds;
        float lineHeight = LineHeight(textBox);
        float startX = view.X + (LineTextWidth(textBox, 0) / 2);
        float startY = view.Y + (0.5f * lineHeight);
        float endX = view.X + LineTextWidth(textBox, 1) + 10;
        float endY = view.Y + (1.5f * lineHeight);
        Assert.True(endX < view.X + view.Width, $"The drag end x {endX} is outside the text view ({view.X}..{view.X + view.Width}).");

        ElementInputBridge bridge = new();
        PointerSnapshot startUp = PointerSnapshot.Empty.WithPosition(startX, startY);
        PointerSnapshot startDown = startUp.WithButton(InputMouseButton.Left, true);
        PointerSnapshot endDown = PointerSnapshot.Empty.WithPosition(endX, endY).WithButton(InputMouseButton.Left, true);
        PointerSnapshot endUp = PointerSnapshot.Empty.WithPosition(endX, endY);
        bridge.Dispatch(root, new InputFrame(startUp, startDown, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
        bridge.Dispatch(root, new InputFrame(startDown, endDown, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
        Assert.True(presenter.VerticalOffset == 0, $"VerticalOffset {presenter.VerticalOffset} after the drag to ({endX}, {endY}).");
        bridge.Dispatch(root, new InputFrame(endDown, endUp, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
        root.ProcessFrame();

        Assert.False(textBox.Selection.IsEmpty);
        Assert.True(presenter.VerticalOffset == 0, $"VerticalOffset {presenter.VerticalOffset} after the release at ({endX}, {endY}).");
    }

    // #1199: the word "ddd " ends at the end of the second, visible wrapped line, which is also the start of the hidden third
    // line. The selection end is drawn at the end of the second line, so the double-click selects only that word and does
    // not scroll.
    [Fact]
    public void DoubleClickOnTheLastWordOfAVisibleWrappedLineDoesNotScroll()
    {
        TextBox textBox = WrappedTextBoxShowingTwoOfThreeLines(out UIRoot root, out ScrollContentPresenter presenter);
        Assert.Equal("ccc ddd ", textBox.GetLineText(1));
        (float x, float y) = CharacterPoint(textBox, 13, line: 1);

        ElementInputBridge bridge = new();
        PressAndRelease(root, bridge, x, y);
        PressAndRelease(root, bridge, x, y);
        root.ProcessFrame();

        Assert.Equal((12, 16), (textBox.Selection.Start, textBox.Selection.End));
        Assert.True(presenter.VerticalOffset == 0, $"VerticalOffset {presenter.VerticalOffset} after the double-click.");
    }

    // #1199: a drag after a double-click on "aaa" to "ddd" selects whole words up to the end of the visible second line.
    [Fact]
    public void WordDragToTheLastWordOfAVisibleWrappedLineDoesNotScroll()
    {
        TextBox textBox = WrappedTextBoxShowingTwoOfThreeLines(out UIRoot root, out ScrollContentPresenter presenter);
        (float startX, float startY) = CharacterPoint(textBox, 1, line: 0);
        (float endX, float endY) = CharacterPoint(textBox, 13, line: 1);

        ElementInputBridge bridge = new();
        PressAndRelease(root, bridge, startX, startY);
        PointerSnapshot startUp = PointerSnapshot.Empty.WithPosition(startX, startY);
        PointerSnapshot startDown = startUp.WithButton(InputMouseButton.Left, true);
        PointerSnapshot endDown = PointerSnapshot.Empty.WithPosition(endX, endY).WithButton(InputMouseButton.Left, true);
        PointerSnapshot endUp = PointerSnapshot.Empty.WithPosition(endX, endY);
        bridge.Dispatch(root, new InputFrame(startUp, startDown, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
        bridge.Dispatch(root, new InputFrame(startDown, endDown, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
        Assert.True(presenter.VerticalOffset == 0, $"VerticalOffset {presenter.VerticalOffset} after the move.");
        bridge.Dispatch(root, new InputFrame(endDown, endUp, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
        root.ProcessFrame();

        Assert.Equal((0, 16), (textBox.Selection.Anchor, textBox.Selection.Active));
        Assert.True(presenter.VerticalOffset == 0, $"VerticalOffset {presenter.VerticalOffset} after the release.");
    }

    // #1199: a drag after a triple-click from the first text line to the second, visible drawn line selects that text line
    // and does not scroll; the text line ends at its line break, on the second drawn line.
    [Fact]
    public void LineDragToAVisibleWrappedLineDoesNotScroll()
    {
        TextBox textBox = new()
        {
            TextWrapping = TextWrapping.Wrap,
            Width = 80,
            MaxLines = 2,
            Text = "aaa bbb ccc\nddd eee fff",
            VerticalAlignment = VerticalAlignment.Top
        };
        UIRoot root = Host(textBox, out ElementInputBridge keyboard);
        keyboard.Dispatch(root, KeyFrame(InputKey.Home, InputKey.LeftCtrl));
        root.ProcessFrame();
        ScrollContentPresenter presenter = ContentHost(textBox).Presenter;
        Assert.Equal("aaa bbb ", textBox.GetLineText(0));
        Assert.Equal("ccc\n", textBox.GetLineText(1));
        Assert.Equal(0, presenter.VerticalOffset);
        (float startX, float startY) = CharacterPoint(textBox, 1, line: 0);
        (float endX, float endY) = CharacterPoint(textBox, 9, line: 1);

        ElementInputBridge bridge = new();
        PressAndRelease(root, bridge, startX, startY);
        PressAndRelease(root, bridge, startX, startY);
        PointerSnapshot startUp = PointerSnapshot.Empty.WithPosition(startX, startY);
        PointerSnapshot startDown = startUp.WithButton(InputMouseButton.Left, true);
        PointerSnapshot endDown = PointerSnapshot.Empty.WithPosition(endX, endY).WithButton(InputMouseButton.Left, true);
        PointerSnapshot endUp = PointerSnapshot.Empty.WithPosition(endX, endY);
        bridge.Dispatch(root, new InputFrame(startUp, startDown, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
        bridge.Dispatch(root, new InputFrame(startDown, endDown, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
        Assert.True(presenter.VerticalOffset == 0, $"VerticalOffset {presenter.VerticalOffset} after the move.");
        bridge.Dispatch(root, new InputFrame(endDown, endUp, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
        root.ProcessFrame();

        Assert.Equal((0, 11), (textBox.Selection.Start, textBox.Selection.End));
        Assert.True(presenter.VerticalOffset == 0, $"VerticalOffset {presenter.VerticalOffset} after the release.");
    }

    [Fact]
    public void TextBoxAndPasswordBoxTemplatesHostContentInScrollViewerPart()
    {
        TextBox textBox = new();
        PasswordBox passwordBox = new();
        UIRoot root = new(200, 100);
        root.VisualChildren.Add(textBox);
        root.VisualChildren.Add(passwordBox);
        root.ProcessFrame();

        Assert.IsType<ScrollViewer>(textBox.ComponentTemplateInstance!.Parts["PART_ContentHost"]);
        Assert.IsType<ScrollViewer>(passwordBox.ComponentTemplateInstance!.Parts["PART_ContentHost"]);
    }

    private static ScrollViewer ContentHost(Control control)
    {
        return Assert.IsType<ScrollViewer>(control.ComponentTemplateInstance!.Parts["PART_ContentHost"]);
    }

    private static string TenLines()
    {
        return string.Join("\n", Enumerable.Range(0, 10).Select(index => $"line {index}"));
    }

    private static float LineHeight(TextBox textBox)
    {
        return TextMeasurer.Default.Measure(
            string.Empty,
            new TextAspect(textBox.FontFamily, textBox.FontSize),
            float.PositiveInfinity).Size.Height;
    }

    // A wrapping TextBox that shows its first 2 lines of at least 3, scrolled to the top, with the caret at the start.
    private static TextBox WrappedTextBoxShowingTwoOfThreeLines(out UIRoot root, out ScrollContentPresenter presenter)
    {
        TextBox textBox = new()
        {
            TextWrapping = TextWrapping.Wrap,
            Width = 80,
            MaxLines = 2,
            Text = "aaa bbb ccc ddd eee fff",
            VerticalAlignment = VerticalAlignment.Top
        };
        root = Host(textBox, out ElementInputBridge bridge);
        // The caret starts at the end of the text; Ctrl+Home goes back to the top.
        bridge.Dispatch(root, KeyFrame(InputKey.Home, InputKey.LeftCtrl));
        root.ProcessFrame();
        presenter = ContentHost(textBox).Presenter;
        Assert.True(textBox.LineCount >= 3, $"LineCount {textBox.LineCount}.");
        Assert.True(presenter.ExtentHeight > presenter.ViewportHeight, $"extent {presenter.ExtentHeight}, viewport {presenter.ViewportHeight}.");
        Assert.Equal(0, presenter.VerticalOffset);
        return textBox;
    }

    // The drawn width of a line's text without its trailing spaces.
    private static float LineTextWidth(TextBox textBox, int lineIndex)
    {
        return TextMeasurer.Default.Measure(
            textBox.GetLineText(lineIndex).TrimEnd(),
            new TextAspect(textBox.FontFamily, textBox.FontSize),
            float.PositiveInfinity).Size.Width;
    }

    private static UIRoot Host(TextBox textBox, out ElementInputBridge bridge)
    {
        UIRoot root = new(300, 200);
        root.VisualChildren.Add(textBox);
        root.ProcessFrame();
        bridge = new ElementInputBridge();
        Assert.True(bridge.FocusManager.Focus(textBox, root.InputCache.EnsureCurrent(root)));
        root.ProcessFrame();
        return root;
    }

    private static void Click(UIRoot root, float x, float y)
    {
        ElementInputBridge bridge = new();
        PointerSnapshot up = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot down = up.WithButton(InputMouseButton.Left, true);
        bridge.Dispatch(root, new InputFrame(up, down, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
        bridge.Dispatch(root, new InputFrame(down, up, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
    }

    // One press and release through a bridge the caller keeps, so that consecutive calls count as a double or triple click.
    private static void PressAndRelease(UIRoot root, ElementInputBridge bridge, float x, float y)
    {
        PointerSnapshot up = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot down = up.WithButton(InputMouseButton.Left, true);
        bridge.Dispatch(root, new InputFrame(up, down, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
        bridge.Dispatch(root, new InputFrame(down, up, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
    }

    // The middle of the character at textIndex on the given drawn line of an unscrolled text box.
    private static (float X, float Y) CharacterPoint(TextBox textBox, int textIndex, int line)
    {
        int lineStart = 0;
        for (int i = 0; i < line; i++)
        {
            lineStart += textBox.GetLineText(i).Length;
        }

        TextAspect aspect = new(textBox.FontFamily, textBox.FontSize);
        float before = TextMeasurer.Default.Measure(textBox.Text[lineStart..textIndex], aspect, float.PositiveInfinity).Size.Width;
        float character = TextMeasurer.Default.Measure(textBox.Text[textIndex].ToString(), aspect, float.PositiveInfinity).Size.Width;
        LayoutRect view = textBox.TextView.ArrangedBounds;
        return (view.X + before + (character / 2), view.Y + ((line + 0.5f) * LineHeight(textBox)));
    }

    private static InputFrame WheelFrame(float x, float y, int delta)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = previous.WithWheelValue(delta);
        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static InputFrame TextFrame(string text)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            [new TextInputSnapshotEvent(text)]);
    }

    private static InputFrame KeyFrame(InputKey key, params InputKey[] modifiers)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys(modifiers),
            KeyboardSnapshot.FromDownKeys([.. modifiers, key]),
            []);
    }
}
