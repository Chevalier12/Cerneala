using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.UI.Servo;

// ClickAsync is one click: the input clock of the Servo immediate driver never advances between actions, so the
// click count must not be decided by the time between two ClickAsync calls (#682).
public sealed class ServoClickCountTests
{
    [Fact]
    public async Task TwoClickAsyncCallsOnTheSameButtonAreTwoSingleClicks()
    {
        Button button = new() { Content = "Target", Width = 160, Height = 48 };
        ServoApi.SetId(button, "button");
        UiHost host = CreateHost(button);
        ServoApi servo = new(host);
        List<int> downCounts = [];
        int doubleClicks = 0;
        button.AddHandler(
            InputEvents.MouseDownEvent,
            (_, args) => downCounts.Add(((MouseButtonEventArgs)args).ClickCount),
            handledEventsToo: true);
        button.AddHandler(InputEvents.MouseDoubleClickEvent, (_, _) => doubleClicks++, handledEventsToo: true);

        await servo.ClickAsync(ServoTarget.ById("button"));
        await servo.ClickAsync(ServoTarget.ById("button"));

        Assert.Equal([1, 1], downCounts);
        Assert.Equal(0, doubleClicks);
    }

    [Fact]
    public async Task ClickAsyncAfterOtherActionsDoesNotContinueAnEarlierClickSequence()
    {
        TextBox editor = new() { Text = "hello world", Width = 200, Height = 40 };
        Button button = new() { Content = "Target", Width = 160, Height = 48 };
        ServoApi.SetId(editor, "editor");
        ServoApi.SetId(button, "button");
        StackPanel panel = new();
        panel.VisualChildren.Add(editor);
        panel.VisualChildren.Add(button);
        UiHost host = CreateHost(panel, width: 240, height: 160);
        ServoApi servo = new(host);
        List<int> editorDownCounts = [];
        int doubleClicks = 0;
        editor.AddHandler(
            InputEvents.MouseDownEvent,
            (_, args) => editorDownCounts.Add(((MouseButtonEventArgs)args).ClickCount),
            handledEventsToo: true);
        button.AddHandler(InputEvents.MouseDoubleClickEvent, (_, _) => doubleClicks++, handledEventsToo: true);

        await servo.ClickAsync(ServoTarget.ById("editor"));
        await servo.PressKeyAsync(InputKey.End);
        await servo.ClickAsync(ServoTarget.ById("editor"));

        Assert.Equal([1, 1], editorDownCounts);
        Assert.Equal(string.Empty, editor.SelectedText);

        await servo.ClickAsync(ServoTarget.ById("button"));
        await servo.SendTextAsync("x");
        await servo.ClickAsync(ServoTarget.ById("button"));

        Assert.Equal(0, doubleClicks);
    }

    [Fact]
    public async Task ClickCountTwoSelectsTheWordOfATextBox()
    {
        TextBox editor = new() { Text = "hello world", Width = 200, Height = 40 };
        ServoApi.SetId(editor, "editor");
        UiHost host = CreateHost(editor, width: 240, height: 80);
        ServoApi servo = new(host);

        await servo.ClickAsync(ServoTarget.ById("editor"), clickCount: 2);

        Assert.Equal("world", editor.SelectedText);
    }

    [Fact]
    public async Task ClickCountThreeSelectsTheWholeLineOfATextBox()
    {
        TextBox editor = new() { Text = "hello world", Width = 200, Height = 40 };
        ServoApi.SetId(editor, "editor");
        UiHost host = CreateHost(editor, width: 240, height: 80);
        ServoApi servo = new(host);

        await servo.ClickAsync(ServoTarget.ById("editor"), clickCount: 3);

        Assert.Equal("hello world", editor.SelectedText);
    }

    [Fact]
    public async Task ClickCountTwoOnAButtonRaisesOneMouseDoubleClickWithCountsOneAndTwo()
    {
        Button button = new() { Content = "Target", Width = 160, Height = 48 };
        ServoApi.SetId(button, "button");
        UiHost host = CreateHost(button);
        ServoApi servo = new(host);
        List<int> downCounts = [];
        List<int> upCounts = [];
        int doubleClicks = 0;
        button.AddHandler(
            InputEvents.MouseDownEvent,
            (_, args) => downCounts.Add(((MouseButtonEventArgs)args).ClickCount),
            handledEventsToo: true);
        button.AddHandler(
            InputEvents.MouseUpEvent,
            (_, args) => upCounts.Add(((MouseButtonEventArgs)args).ClickCount),
            handledEventsToo: true);
        button.AddHandler(InputEvents.MouseDoubleClickEvent, (_, _) => doubleClicks++, handledEventsToo: true);

        await servo.ClickAsync(ServoTarget.ById("button"), clickCount: 2);

        Assert.Equal([1, 2], downCounts);
        Assert.Equal([1, 2], upCounts);
        Assert.Equal(1, doubleClicks);

        // The next ClickAsync starts a new sequence again.
        await servo.ClickAsync(ServoTarget.ById("button"));

        Assert.Equal([1, 2, 1], downCounts);
        Assert.Equal(1, doubleClicks);
    }

    [Fact]
    public async Task ClickCountOverloadWithTimeoutAlsoSendsTheWholeSequence()
    {
        Button button = new() { Content = "Target", Width = 160, Height = 48 };
        ServoApi.SetId(button, "button");
        UiHost host = CreateHost(button);
        ServoApi servo = new(host);
        int doubleClicks = 0;
        button.AddHandler(InputEvents.MouseDoubleClickEvent, (_, _) => doubleClicks++, handledEventsToo: true);

        await servo.ClickAsync(ServoTarget.ById("button"), 2, TimeSpan.FromSeconds(5));

        Assert.Equal(1, doubleClicks);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ClickCountBelowOneIsRejected(int clickCount)
    {
        Button button = new() { Content = "Target", Width = 160, Height = 48 };
        ServoApi.SetId(button, "button");
        ServoApi servo = new(CreateHost(button));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => servo.ClickAsync(ServoTarget.ById("button"), clickCount));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => servo.ClickAsync(ServoTarget.ById("button"), clickCount, TimeSpan.FromSeconds(5)));
    }


    private static UiHost CreateHost(UIElement content, float width = 320, float height = 160)
    {
        UIRoot root = new(width, height);
        root.VisualChildren.Add(content);
        UiHost host = new(new UiHostOptions
        {
            Root = root,
            Viewport = new UiViewport(width, height)
        });
        host.Update(
            new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []),
            host.Viewport,
            TimeSpan.Zero);
        return host;
    }
}
