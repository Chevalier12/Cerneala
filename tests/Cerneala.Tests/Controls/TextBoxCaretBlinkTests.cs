using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;

namespace Cerneala.Tests.Controls;

public sealed class TextBoxCaretBlinkTests
{
    private static readonly DrawColor CaretColor = new(12, 220, 80);

    [Fact]
    public void FocusedCaretIsVisibleAtBlinkStart()
    {
        UIRoot root = RootWithTextBox(focused: true);
        UiHost host = HostFor(root);

        Update(host, 0);

        Assert.Equal(1, CountCaretCommands(root));
    }

    [Fact]
    public void FocusedCaretTurnsOffAfterHalfBlinkPeriod()
    {
        UIRoot root = RootWithTextBox(focused: true);
        UiHost host = HostFor(root);

        Update(host, 0);
        Update(host, 500);

        Assert.Equal(0, CountCaretCommands(root));
    }

    [Fact]
    public void FocusedCaretTurnsOffAfterAccumulatedElapsedFrames()
    {
        UIRoot root = RootWithTextBox(focused: true);
        UiHost host = HostFor(root);

        Update(host, 0);
        for (int i = 0; i < 32; i++)
        {
            Update(host, 16);
        }

        Assert.Equal(0, CountCaretCommands(root));
    }

    [Fact]
    public void FocusedCaretTurnsBackOnAfterFullBlinkPeriod()
    {
        UIRoot root = RootWithTextBox(focused: true);
        UiHost host = HostFor(root);

        Update(host, 0);
        Update(host, 500);
        Update(host, 500);

        Assert.Equal(1, CountCaretCommands(root));
    }

    [Fact]
    public void BlinkPhaseChangeInvalidatesRetainedRenderWithoutInput()
    {
        UIRoot root = RootWithTextBox(focused: true);
        UiHost host = HostFor(root);

        Update(host, 0);
        Assert.Equal(1, CountCaretCommands(root));

        UiFrame samePhaseFrame = Update(host, 250);
        Assert.Equal(1, CountCaretCommands(root));

        UiFrame offFrame = Update(host, 300);
        Assert.Equal(0, CountCaretCommands(root));
        Assert.True(offFrame.Stats.RenderedElements > samePhaseFrame.Stats.RenderedElements);

        Update(host, 500);
        Assert.Equal(1, CountCaretCommands(root));
    }

    [Fact]
    public void UnfocusedTextBoxDoesNotScheduleBlinkRenderWork()
    {
        UIRoot root = RootWithTextBox(focused: false);
        UiHost host = HostFor(root);

        Update(host, 0);
        UiFrame frame = Update(host, 550);

        Assert.Equal(0, CountCaretCommands(root));
        Assert.Equal(0, frame.Stats.RenderedElements);
        Assert.Equal(1, frame.Stats.NoWorkFrames);
    }

    private static UIRoot RootWithTextBox(bool focused)
    {
        UIRoot root = new(200, 80);
        TextBox textBox = new()
        {
            Text = "blink",
            CaretColor = CaretColor,
            IsKeyboardFocused = focused
        };
        textBox.MoveCaret(2);
        root.VisualChildren.Add(textBox);
        return root;
    }

    private static UiHost HostFor(UIRoot root)
    {
        return new UiHost(new UiHostOptions
        {
            Root = root,
            Clock = new Cerneala.Tests.UI.Hosting.FakeUiClock()
        });
    }

    private static UiFrame Update(UiHost host, int milliseconds)
    {
        return host.Update(EmptyInputFrame(), new UiViewport(200, 80), TimeSpan.FromMilliseconds(milliseconds));
    }

    private static int CountCaretCommands(UIRoot root)
    {
        return root.RetainedRenderer
            .Render(root)
            .Count(command => command.Kind == DrawCommandKind.FillRectangle && command.Color == CaretColor);
    }

    private static InputFrame EmptyInputFrame()
    {
        return new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }
}
