using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.UI.Servo;

public sealed class ServoInputTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TargetActionResolvesAfterPendingLayoutMovesTheTarget(bool click)
    {
        Border header = new() { Height = 16 };
        Button button = new() { Content = "Target", Width = 160, Height = 32 };
        ServoApi.SetId(button, "target");
        StackPanel panel = new();
        panel.VisualChildren.Add(header);
        panel.VisualChildren.Add(button);
        UiHost host = CreateHost(panel);
        ServoApi servo = new(host);
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        float initialY = button.ArrangedBounds.Y;

        header.Height = 80;
        Assert.Equal(initialY, button.ArrangedBounds.Y);
        Assert.True(host.Root!.Scheduler.HasWork);

        if (click) await servo.ClickAsync(ServoTarget.ById("target"));
        else await servo.HoverAsync(ServoTarget.ById("target"));

        Assert.True(button.ArrangedBounds.Y > initialY + button.ArrangedBounds.Height);
        Assert.True(button.IsMouseOver);
        Assert.Equal(click ? 1 : 0, clicks);
        Assert.False(host.LastFrame!.Input.Pointer.IsDown(InputMouseButton.Left));
    }

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
    [InlineData("DEFAULT", "replacement")]
    [InlineData("", "replacement")]
    [InlineData("DEFAULT", "")]
    [InlineData("line1\nline2", "x")]
    public async Task ReplaceTextLeavesExactlyTheRequestedValue(string previous, string text)
    {
        TextBox editor = new() { Text = previous, Width = 200, Height = 40 };
        ServoApi.SetId(editor, "editor");
        UiHost host = CreateHost(editor, width: 240, height: 80);
        ServoApi servo = new(host);
        int keyDowns = 0;
        editor.AddHandler(InputEvents.KeyDownEvent, (_, _) => keyDowns++, handledEventsToo: true);

        await servo.ReplaceTextAsync(ServoTarget.ById("editor"), text);

        Assert.Equal(text, editor.Text);
        Assert.Equal(text, (await servo.FindAsync(ServoTarget.ById("editor"))).Value);
        Assert.True(editor.IsKeyboardFocused);
        Assert.True(keyDowns > 0);
    }

    [Theory]
    [InlineData("read-only")]
    [InlineData("multi-line-into-single-line")]
    [InlineData("rebound-select-all")]
    public async Task ReplaceTextFailsWhenTheTargetDoesNotEndWithTheRequestedValue(string scenario)
    {
        TextBox editor = new()
        {
            Text = "abc",
            IsReadOnly = scenario == "read-only",
            Width = 200,
            Height = 40
        };
        ServoApi.SetId(editor, "editor");
        if (scenario == "rebound-select-all")
        {
            editor.AddHandler(
                InputEvents.PreviewKeyDownEvent,
                (_, args) =>
                {
                    KeyEventArgs key = Assert.IsType<KeyEventArgs>(args);
                    if (key.Key == InputKey.A && key.IsControlDown)
                    {
                        key.Handled = true;
                    }
                });
        }

        UiHost host = CreateHost(editor, width: 240, height: 80);
        ServoApi servo = new(host);
        string text = scenario == "multi-line-into-single-line" ? "x\ny" : "xyz";

        ServoException exception = await Assert.ThrowsAsync<ServoException>(
            () => servo.ReplaceTextAsync(ServoTarget.ById("editor"), text));

        Assert.NotEqual(text, editor.Text);
        Assert.Contains(text, exception.Message);
        Assert.Contains(editor.Text, exception.Message);
    }

    [Fact]
    public async Task ReplaceTextRejectsATargetWithoutATextValueBeforeAnyInput()
    {
        TextBox editor = new() { Text = "abc", Width = 120, Height = 40 };
        Button button = new() { Content = "Target", Width = 120, Height = 40 };
        ServoApi.SetId(editor, "editor");
        ServoApi.SetId(button, "button");
        StackPanel panel = new();
        panel.VisualChildren.Add(editor);
        panel.VisualChildren.Add(button);
        UiHost host = CreateHost(panel, width: 240, height: 120);
        ServoApi servo = new(host);
        await servo.ClickAsync(ServoTarget.ById("editor"));
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        UiFrame before = host.LastFrame!;

        ServoException exception = await Assert.ThrowsAsync<ServoException>(
            () => servo.ReplaceTextAsync(ServoTarget.ById("button"), "x"));

        Assert.Contains("text value", exception.Message);
        Assert.Same(before, host.LastFrame);
        Assert.Equal(0, clicks);
        Assert.True(editor.IsKeyboardFocused);
        Assert.Equal("abc", editor.Text);
    }

    [Fact]
    public async Task ReplaceTextReplacesAndVerifiesAPasswordWithoutRevealingIt()
    {
        PasswordBox password = new() { Password = "old-secret", Width = 200, Height = 40 };
        ServoApi.SetId(password, "password");
        UiHost host = CreateHost(password, width: 240, height: 80);
        ServoApi servo = new(host);

        await servo.ReplaceTextAsync(ServoTarget.ById("password"), "new-secret");
        Assert.Equal("new-secret", password.Password);

        await servo.ReplaceTextAsync(ServoTarget.ById("password"), "");
        Assert.Equal("", password.Password);
    }

    [Fact]
    public async Task ReplaceTextFailureOnAPasswordDoesNotRevealTheRequestedOrActualPassword()
    {
        DigitsOnlyPasswordBox password = new() { Password = "1234", Width = 200, Height = 40 };
        ServoApi.SetId(password, "password");
        UiHost host = CreateHost(password, width: 240, height: 80);
        ServoApi servo = new(host);

        ServoException exception = await Assert.ThrowsAsync<ServoException>(
            () => servo.ReplaceTextAsync(ServoTarget.ById("password"), "pass987"));

        Assert.Equal("987", password.Password);
        Assert.DoesNotContain("pass987", exception.Message);
        Assert.DoesNotContain("987", exception.Message);
        Assert.DoesNotContain("1234", exception.Message);
    }

    [Theory]
    [InlineData("key")]
    [InlineData("text")]
    public async Task KeyboardOnlyActionBeforeAnyPointerActionLeavesTheHostPointerWhereItIs(string action)
    {
        Button button = new() { Content = "Target", Width = 160, Height = 48 };
        UiHost host = CreateHost(button);
        LayoutRect bounds = button.ArrangedBounds;
        PointerSnapshot appPointer = PointerSnapshot.Empty.WithPosition(
            bounds.X + (bounds.Width / 2),
            bounds.Y + (bounds.Height / 2));
        host.Update(new InputFrame(PointerSnapshot.Empty, appPointer, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []), host.Viewport, TimeSpan.Zero);
        host.Update(new InputFrame(appPointer, appPointer, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []), host.Viewport, TimeSpan.Zero);
        Assert.True(button.IsMouseOver);
        int enters = 0;
        int leaves = 0;
        button.AddHandler(UIElement.MouseEnterEvent, (_, _) => enters++, handledEventsToo: true);
        button.AddHandler(UIElement.MouseLeaveEvent, (_, _) => leaves++, handledEventsToo: true);
        ServoApi servo = new(host);

        if (action == "key") await servo.PressKeyAsync(InputKey.F2);
        else await servo.SendTextAsync("ab");

        Assert.Equal((appPointer.X, appPointer.Y), (host.LastFrame!.Input.Pointer.X, host.LastFrame.Input.Pointer.Y));
        Assert.True(button.IsMouseOver);
        host.Update(new InputFrame(appPointer, appPointer, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []), host.Viewport, TimeSpan.Zero);
        Assert.True(button.IsMouseOver);
        Assert.Equal((0, 0), (enters, leaves));
    }

    [Fact]
    public async Task KeyboardOnlyActionAfterAPointerActionStaysAtTheServoPointer()
    {
        Button left = new() { Content = "Left", Width = 100, Height = 48 };
        Button right = new() { Content = "Right", Width = 100, Height = 48 };
        ServoApi.SetId(right, "right");
        StackPanel panel = new() { Orientation = Orientation.Horizontal };
        panel.VisualChildren.Add(left);
        panel.VisualChildren.Add(right);
        UiHost host = CreateHost(panel);
        LayoutRect leftBounds = left.ArrangedBounds;
        PointerSnapshot appPointer = PointerSnapshot.Empty.WithPosition(
            leftBounds.X + (leftBounds.Width / 2),
            leftBounds.Y + (leftBounds.Height / 2));
        host.Update(new InputFrame(PointerSnapshot.Empty, appPointer, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []), host.Viewport, TimeSpan.Zero);
        Assert.True(left.IsMouseOver);
        ServoApi servo = new(host);
        await servo.HoverAsync(ServoTarget.ById("right"));
        float servoX = host.LastFrame!.Input.Pointer.X;
        float servoY = host.LastFrame.Input.Pointer.Y;

        await servo.PressKeyAsync(InputKey.F2);

        Assert.Equal((servoX, servoY), (host.LastFrame!.Input.Pointer.X, host.LastFrame.Input.Pointer.Y));
        Assert.True(right.IsMouseOver);
        Assert.False(left.IsMouseOver);
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

    [Fact]
    public async Task HeldKeyRejectsDirectHostBeforeProducingInput()
    {
        UiHost host = CreateHost(new TextBox { Width = 200, Height = 40 });
        ServoApi servo = new(host);
        UiFrame before = host.LastFrame!;

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            servo.HoldKeyAsync(InputKey.A, TimeSpan.FromMilliseconds(10)));

        Assert.Same(before, host.LastFrame);
        Assert.False(host.LastFrame!.Input.Keyboard.IsDown(InputKey.A));
    }

    [Fact]
    public async Task PlatformInputRejectsDirectHostBeforeProducingInput()
    {
        UiHost host = CreateHost(new TextBox { Width = 200, Height = 40 });
        ServoApi servo = new(host);
        UiFrame before = host.LastFrame!;

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            servo.SendPlatformInputAsync(ServoPlatformInput.KeyDown(InputKey.A)));

        Assert.Same(before, host.LastFrame);
    }

    [Fact]
    public void PlatformInputValidatesItsEventsBeforeTouchingAnyPlatform()
    {
        UiHost host = CreateHost(new TextBox { Width = 200, Height = 40 });
        ServoApi servo = new(host);

        Assert.ThrowsAny<ArgumentException>(() => { _ = servo.SendPlatformInputAsync(); });
        Assert.ThrowsAny<ArgumentException>(() => { _ = servo.SendPlatformInputAsync([]); });
        Assert.ThrowsAny<ArgumentException>(() =>
        {
            _ = servo.SendPlatformInputAsync([ServoPlatformInput.KeyDown(InputKey.A), null!]);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = servo.SendPlatformInputAsync((IReadOnlyList<ServoPlatformInput>)null!);
        });
        Assert.ThrowsAny<ArgumentException>(() => ServoPlatformInput.KeyDown(InputKey.None));
        Assert.ThrowsAny<ArgumentException>(() => ServoPlatformInput.KeyDown(InputKey.Unknown));
        Assert.ThrowsAny<ArgumentException>(() => ServoPlatformInput.KeyUp((InputKey)int.MaxValue));
        Assert.ThrowsAny<ArgumentException>(() => ServoPlatformInput.Text(""));
        Assert.ThrowsAny<ArgumentException>(() => ServoPlatformInput.Text(null!));
        Assert.ThrowsAny<ArgumentException>(() => ServoPlatformInput.PointerDown(
            new ServoPoint(1, 1), InputMouseButton.None));
        Assert.ThrowsAny<ArgumentException>(() => ServoPlatformInput.PointerUp(
            new ServoPoint(1, 1), InputMouseButton.None));
        Assert.ThrowsAny<ArgumentException>(() => ServoPlatformInput.Wheel(new ServoPoint(1, 1), 0));
    }

    [Theory]
    [InlineData("hover")]
    [InlineData("click")]
    [InlineData("drag")]
    [InlineData("scroll")]
    public async Task TargetActionsAimWhereAScaledAncestorDrawsTheTarget(string action)
    {
        Thumb thumb = new() { Width = 100, Height = 40 };
        ServoApi.SetId(thumb, "target");
        UiHost host = CreateHost(CreateScaledPanel(thumb), width: 320, height: 240);
        ServoApi servo = new(host);
        MouseEventArgs? firstMove = null;
        MouseEventArgs? down = null;
        MouseEventArgs? wheel = null;
        thumb.AddHandler(InputEvents.MouseMoveEvent, (_, args) => firstMove ??= (MouseEventArgs)args, handledEventsToo: true);
        thumb.AddHandler(InputEvents.MouseDownEvent, (_, args) => down ??= (MouseEventArgs)args, handledEventsToo: true);
        thumb.AddHandler(InputEvents.MouseWheelEvent, (_, args) => wheel ??= (MouseEventArgs)args, handledEventsToo: true);
        Assert.Equal(new LayoutRect(0, 40, 100, 40), thumb.ArrangedBounds);

        ServoTarget target = ServoTarget.ById("target");
        if (action == "hover") await servo.HoverAsync(target);
        else if (action == "click") await servo.ClickAsync(target);
        else if (action == "drag") await servo.DragAsync(target, new ServoPoint(130, 130), steps: 4);
        else await servo.ScrollAsync(target, -120);

        Assert.NotNull(firstMove);
        Assert.Equal((100, 120), (firstMove.X, firstMove.Y));
        if (action == "click")
        {
            Assert.NotNull(down);
            Assert.Equal((100, 120), (down.X, down.Y));
        }
        else if (action == "drag")
        {
            // The 30 x 10 root-pixel drag is 15 x 5 in the 2x panel that arranges the thumb.
            Assert.Equal(15, thumb.TotalHorizontalChange);
            Assert.Equal(5, thumb.TotalVerticalChange);
        }
        else if (action == "scroll")
        {
            Assert.NotNull(wheel);
            Assert.Equal((100, 120), (wheel.X, wheel.Y));
        }
    }

    [Fact]
    public async Task TargetActionsAimWhereATranslateRenderTransformDrawsTheTarget()
    {
        Button button = new()
        {
            Content = "Target",
            Width = 100,
            Height = 40,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            RenderTransform = new Transform(Matrix3x2.CreateTranslation(150, 30))
        };
        ServoApi.SetId(button, "target");
        UiHost host = CreateHost(button, width: 320, height: 160);
        ServoApi servo = new(host);
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        Assert.Equal(new LayoutRect(0, 0, 100, 40), button.ArrangedBounds);

        await servo.ClickAsync(ServoTarget.ById("target"));

        Assert.Equal(1, clicks);
        Assert.Equal(200, host.LastFrame!.Input.Pointer.X);
        Assert.Equal(50, host.LastFrame.Input.Pointer.Y);
    }

    [Fact]
    public async Task LayoutElementBoundsAndScreenshotRegionUseTheDrawnBounds()
    {
        Thumb thumb = new() { Width = 100, Height = 40 };
        ServoApi.SetId(thumb, "target");
        UiHost host = CreateHost(CreateScaledPanel(thumb), width: 320, height: 240);
        ServoApi servo = new(host);
        ServoTarget target = ServoTarget.ById("target");

        Assert.Equal(new LayoutRect(0, 80, 200, 80), (await servo.FindAsync(target)).Bounds);
        Assert.Equal(
            new WindowScreenshotRegion(0, 80, 200, 80),
            new ServoCaptureEngine(new ServoQueryEngine()).ResolveRegion(host.Root!, target));
    }

    private sealed class DigitsOnlyPasswordBox : PasswordBox
    {
        protected override string NormalizeTextInput(string text) =>
            new(base.NormalizeTextInput(text).Where(char.IsDigit).ToArray());
    }

    private static StackPanel CreateScaledPanel(UIElement target)
    {
        StackPanel panel = new()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            RenderTransform = new Transform(Matrix3x2.CreateScale(2, 2)),
            RenderTransformOrigin = new LayoutPoint(0, 0)
        };
        panel.VisualChildren.Add(new Border { Height = 40 });
        panel.VisualChildren.Add(target);
        return panel;
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
