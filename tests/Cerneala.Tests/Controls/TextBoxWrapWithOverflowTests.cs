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
