using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Text;

namespace Cerneala.Tests.Controls;

// Issue #683, review round 2: a TextBox that wraps with overflow keeps a word that is wider than the viewport on one line,
// so that word has to be reachable by horizontal scrolling.
public sealed class TextBoxWrapWithOverflowTests
{
    private const string LongWord = "abcdefghijklmnopqrstuvwxyz";

    [Fact]
    public void TypingAWordWiderThanTheViewportScrollsHorizontallyToTheCaret()
    {
        TextBox textBox = new() { TextWrapping = TextWrapping.WrapWithOverflow, Width = 100, VerticalAlignment = VerticalAlignment.Top };
        UIRoot root = Host(textBox, out ElementInputBridge bridge);

        bridge.Dispatch(root, TextFrame(LongWord));
        root.ProcessFrame();

        ScrollContentPresenter presenter = ContentHost(textBox).Presenter;
        Assert.Equal(LongWord, textBox.Text);
        Assert.Equal(1, textBox.LineCount);
        Assert.True(presenter.ExtentWidth > presenter.ViewportWidth, $"extent {presenter.ExtentWidth} viewport {presenter.ViewportWidth}");
        Assert.True(presenter.HorizontalOffset > 0, $"horizontal offset {presenter.HorizontalOffset}");
    }

    [Fact]
    public void PlainWrapStillBreaksTheLongWordAndDoesNotScrollHorizontally()
    {
        TextBox textBox = new() { TextWrapping = TextWrapping.Wrap, Width = 100, VerticalAlignment = VerticalAlignment.Top };
        UIRoot root = Host(textBox, out ElementInputBridge bridge);

        bridge.Dispatch(root, TextFrame(LongWord));
        root.ProcessFrame();

        ScrollContentPresenter presenter = ContentHost(textBox).Presenter;
        Assert.True(textBox.LineCount > 1);
        Assert.Equal(0, presenter.HorizontalOffset);
    }

    // Issue #915: WrapWithOverflow breaks short words at the viewport width, as Wrap does.
    [Theory]
    [InlineData(TextWrapping.Wrap)]
    [InlineData(TextWrapping.WrapWithOverflow)]
    public void ShortWordsWrapIntoSeveralLines(TextWrapping wrapping)
    {
        TextBox textBox = new() { TextWrapping = wrapping, Width = 100, VerticalAlignment = VerticalAlignment.Top };
        UIRoot root = Host(textBox, out ElementInputBridge bridge);

        bridge.Dispatch(root, TextFrame("one two three four five six seven"));
        root.ProcessFrame();

        Assert.True(textBox.LineCount > 1, $"{wrapping}: LineCount {textBox.LineCount}.");
    }

    // Issue #915: the short words around a word wider than the viewport still wrap at the viewport width, and only the
    // long word's line makes the text wider than the viewport.
    [Fact]
    public void ShortWordsAroundALongWordWrapAtTheViewportWidth()
    {
        TextBox textBox = new() { TextWrapping = TextWrapping.WrapWithOverflow, Width = 100, VerticalAlignment = VerticalAlignment.Top };
        UIRoot root = Host(textBox, out ElementInputBridge bridge);

        bridge.Dispatch(root, TextFrame($"one two {LongWord} three four five six"));
        root.ProcessFrame();

        ScrollContentPresenter presenter = ContentHost(textBox).Presenter;
        Assert.True(textBox.LineCount > 3, $"LineCount {textBox.LineCount}.");
        Assert.Equal(LongWord, textBox.GetLineText(1).TrimEnd());
        Assert.True(presenter.ExtentWidth > presenter.ViewportWidth, $"extent {presenter.ExtentWidth} viewport {presenter.ViewportWidth}");
    }

    // Issue #915, review of PR #1049: after the width goes to 120 and back to 100, the text wraps at 100 again. The view's
    // measure for 100 comes from its measure cache and its arranged rectangle (the long word's width) is the same at both
    // widths, so its arrange can come from the cache too; the lines must still be the ones of the 100 wide viewport.
    [Fact]
    public void LinesWrapAtTheViewportWidthAgainAfterTheWidthChangesAndComesBack()
    {
        TextBox textBox = new()
        {
            TextWrapping = TextWrapping.WrapWithOverflow,
            Width = 100,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        UIRoot root = Host(textBox, out ElementInputBridge bridge);
        bridge.Dispatch(root, TextFrame($"a b c d {LongWord} e f g h i j k l m n o p q r s"));
        root.ProcessFrame();
        string expected = Lines(textBox);

        textBox.Width = 120;
        root.ProcessFrame();
        string wider = Lines(textBox);
        textBox.Width = 100;
        root.ProcessFrame();

        Assert.NotEqual(expected, wider);
        Assert.Equal(expected, Lines(textBox));
    }

    // Issue #915, review of PR #1049: the long word keeps the text view's bounds the same at the widths 100 and 120, but
    // the other lines wrap differently, so the retained drawing of the old lines must not be reused.
    [Fact]
    public void TheTextIsDrawnAgainWhenItsLinesWrapAtANewWidthInTheSameBounds()
    {
        TextBox textBox = new()
        {
            TextWrapping = TextWrapping.WrapWithOverflow,
            Width = 100,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        UIRoot root = Host(textBox, out ElementInputBridge bridge);
        bridge.Dispatch(root, TextFrame($"a b c d {LongWord} e f g h i j k l m n o p q r s"));
        root.ProcessFrame();
        UIElement view = Assert.IsAssignableFrom<UIElement>(ContentHost(textBox).Presenter.Content);
        LayoutRect bounds = view.ArrangedBounds;
        string lines = Lines(textBox);
        int renderVersion = view.RenderVersion;

        textBox.Width = 120;
        root.ProcessFrame();

        Assert.Equal(bounds.Size, view.ArrangedBounds.Size);
        Assert.NotEqual(lines, Lines(textBox));
        Assert.True(view.RenderVersion > renderVersion, $"RenderVersion {renderVersion} -> {view.RenderVersion}.");
    }

    private static string Lines(TextBox textBox)
    {
        return string.Join(" | ", Enumerable.Range(0, textBox.LineCount).Select(i => textBox.GetLineText(i).TrimEnd()));
    }

    private static ScrollViewer ContentHost(Control control)
    {
        return Assert.IsType<ScrollViewer>(control.ComponentTemplateInstance!.Parts["PART_ContentHost"]);
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

    private static InputFrame TextFrame(string text)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            [new TextInputSnapshotEvent(text)]);
    }
}
