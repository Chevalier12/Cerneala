using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.UI.Servo;

public sealed class ServoInputTests
{
    [Fact]
    public async Task HoverAndClickUseHitTestingRoutedInputAndRetainedCommit()
    {
        Button button = new() { Content = "Click", Width = 160, Height = 48 };
        ServoApi.SetId(button, "button");
        UiHost host = CreateHost(button);
        ServoApi servo = new(host);
        UiFrame? frameBeforeInput = host.LastFrame;
        int clicks = 0;
        button.Click += (_, _) => clicks++;

        await servo.HoverAsync(ServoTarget.ById("button"));

        Assert.True(button.IsMouseOver);
        Assert.NotSame(frameBeforeInput, host.LastFrame);

        await servo.ClickAsync(ServoTarget.ById("button"));

        Assert.Equal(1, clicks);
        Assert.True(button.IsKeyboardFocused);
        Assert.False(host.LastFrame!.Input.Pointer.IsDown(InputMouseButton.Left));
    }

    [Fact]
    public async Task DragUsesTheTargetCenterAndAbsoluteClientDestination()
    {
        Thumb thumb = new() { Width = 80, Height = 40 };
        ServoApi.SetId(thumb, "thumb");
        UiHost host = CreateHost(thumb);
        ServoApi servo = new(host);
        LayoutRect bounds = thumb.ArrangedBounds;
        float destinationX = bounds.X + (bounds.Width / 2) + 37;
        float destinationY = bounds.Y + (bounds.Height / 2) + 11;

        await servo.DragAsync(
            ServoTarget.ById("thumb"),
            new ServoPoint(destinationX, destinationY),
            steps: 4);

        Assert.False(thumb.IsDragging);
        Assert.Equal(37, thumb.TotalHorizontalChange);
        Assert.Equal(11, thumb.TotalVerticalChange);
        Assert.False(host.LastFrame!.Input.Pointer.IsDown(InputMouseButton.Left));
    }

    [Fact]
    public async Task ScrollUsesTheRealWheelRoute()
    {
        ScrollViewer viewer = new()
        {
            Width = 160,
            Height = 100,
            Content = new Border { Width = 120, Height = 500 }
        };
        ServoApi.SetId(viewer, "viewer");
        UiHost host = CreateHost(viewer, width: 200, height: 120);
        ServoApi servo = new(host);

        await servo.ScrollAsync(ServoTarget.ById("viewer"), -120);

        Assert.True(viewer.Presenter.VerticalOffset > 0);
        Assert.Equal(-120, host.LastFrame!.Input.Pointer.WheelDelta);
    }

    [Fact]
    public async Task TextActionsComposeFocusKeyAndTextInputWithoutDirectAssignment()
    {
        TextBox editor = new() { Text = "DEFAULT", Width = 200, Height = 40 };
        ServoApi.SetId(editor, "editor");
        UiHost host = CreateHost(editor, width: 240, height: 80);
        ServoApi servo = new(host);

        await servo.TypeIntoAsync(ServoTarget.ById("editor"), "!");
        Assert.True(editor.IsKeyboardFocused);
        Assert.Equal("DEFAULT!", editor.Text);

        await servo.ReplaceTextAsync(ServoTarget.ById("editor"), "replacement");
        Assert.Equal("replacement", editor.Text);

        await servo.PressKeyAsync(InputKey.A, ServoModifiers.Control);
        await servo.SendTextAsync("X");
        Assert.Equal("X", editor.Text);
    }

    [Theory]
    [InlineData("hidden")]
    [InlineData("disabled")]
    [InlineData("zero-bounds")]
    [InlineData("not-hit-testable")]
    public async Task TargetActionsRejectNonActionableElements(string state)
    {
        Button button = new() { Content = "Target", Width = 160, Height = 48 };
        ServoApi.SetId(button, "target");
        if (state == "hidden")
        {
            button.Visibility = Visibility.Hidden;
        }
        else if (state == "disabled")
        {
            button.IsEnabled = false;
        }
        else if (state == "not-hit-testable")
        {
            button.IsHitTestVisible = false;
        }

        UiHost host = CreateHost(button);
        if (state == "zero-bounds")
        {
            button.Arrange(new ArrangeContext(new LayoutRect(0, 0, 0, 48)));
        }

        ServoApi servo = new(host);

        await Assert.ThrowsAsync<ServoTargetNotActionableException>(
            () => servo.ClickAsync(ServoTarget.ById("target")));
    }

    [Fact]
    public async Task CancellationDuringDragReleasesPointerBeforeTheNextAction()
    {
        Thumb thumb = new() { Width = 100, Height = 40 };
        ServoApi.SetId(thumb, "thumb");
        UiHost host = CreateHost(thumb);
        ServoApi servo = new(host);
        using CancellationTokenSource cancellation = new();
        thumb.DragStarted += (_, _) => cancellation.Cancel();
        LayoutRect bounds = thumb.ArrangedBounds;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => servo.DragAsync(
            ServoTarget.ById("thumb"),
            new ServoPoint(bounds.X + bounds.Width - 1, bounds.Y + (bounds.Height / 2)),
            steps: 4,
            cancellation.Token));

        Assert.False(thumb.IsDragging);
        Assert.False(host.LastFrame!.Input.Pointer.IsDown(InputMouseButton.Left));

        await servo.ClickAsync(ServoTarget.ById("thumb"));
        Assert.False(host.LastFrame!.Input.Pointer.IsDown(InputMouseButton.Left));
    }

    [Fact]
    public async Task ExceptionDuringChordReleasesModifiersBeforeTheNextAction()
    {
        TextBox editor = new() { Width = 200, Height = 40 };
        ServoApi.SetId(editor, "editor");
        UiHost host = CreateHost(editor, width: 240, height: 80);
        ServoApi servo = new(host);
        bool throwOnce = true;
        KeyEventArgs? nextKey = null;
        editor.AddHandler(
            InputEvents.KeyDownEvent,
            (_, args) =>
            {
                KeyEventArgs key = Assert.IsType<KeyEventArgs>(args);
                if (throwOnce && key.Key == InputKey.A)
                {
                    throwOnce = false;
                    throw new InvalidOperationException("Injected key failure.");
                }

                if (key.Key == InputKey.B)
                {
                    nextKey = key;
                }
            },
            handledEventsToo: true);
        await servo.ClickAsync(ServoTarget.ById("editor"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servo.PressKeyAsync(InputKey.A, ServoModifiers.Control | ServoModifiers.Shift));

        await servo.PressKeyAsync(InputKey.B);
        Assert.NotNull(nextKey);
        Assert.False(nextKey.IsControlDown);
        Assert.False(nextKey.IsShiftDown);
        Assert.False(nextKey.IsAltDown);
        Assert.False(host.LastFrame!.Input.Keyboard.IsDown(InputKey.LeftCtrl));
        Assert.False(host.LastFrame.Input.Keyboard.IsDown(InputKey.LeftShift));
    }

    [Fact]
    public async Task ActionsValidateArgumentsBeforeInput()
    {
        Button button = new() { Content = "Target" };
        ServoApi.SetId(button, "target");
        ServoApi servo = new(CreateHost(button));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => servo.DragAsync(
            ServoTarget.ById("target"),
            new ServoPoint(10, 10),
            steps: 0));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => servo.PressKeyAsync(InputKey.None));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => servo.PressKeyAsync(InputKey.A, (ServoModifiers)int.MaxValue));
        await Assert.ThrowsAsync<ArgumentNullException>(() => servo.SendTextAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => servo.TypeIntoAsync(ServoTarget.ById("target"), null!));
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
            new InputFrame(
                PointerSnapshot.Empty,
                PointerSnapshot.Empty,
                KeyboardSnapshot.Empty,
                KeyboardSnapshot.Empty,
                []),
            host.Viewport,
            TimeSpan.Zero);
        return host;
    }
}
