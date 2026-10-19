using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.UI.Servo;

public sealed class ServoDragToTargetTests
{
    // An application-level drag and drop: the card is a Thumb, and the application drops it on whatever element
    // is under the pointer when the left button is released.
    [Fact]
    public async Task DragToTargetDropsTheSourceOnTheDestinationCenter()
    {
        DragScene scene = DragScene.Create();
        ServoApi servo = new(scene.Host);

        await servo.DragAsync(ServoTarget.ById("card"), ServoTarget.ById("bin"), steps: 4);

        Assert.Equal(new LayoutRect(220, 20, 80, 60), scene.Bin.ArrangedBounds);
        Assert.Equal((260f, 50f), scene.ReleasePoint);
        Assert.Same(scene.Bin, scene.DropTarget);
        Assert.Equal(260 - 50, scene.Card.TotalHorizontalChange);
        Assert.Equal(50 - 80, scene.Card.TotalVerticalChange);
        Assert.False(scene.Card.IsDragging);
        Assert.False(scene.Host.LastFrame!.Input.Pointer.IsDown(InputMouseButton.Left));
    }

    [Fact]
    public async Task DragToTargetResolvesTheDestinationAfterPendingLayoutMovesIt()
    {
        DragScene scene = DragScene.Create();
        ServoApi servo = new(scene.Host);
        Canvas.SetLeft(scene.Bin, 120);
        Assert.Equal(220, scene.Bin.ArrangedBounds.X);

        await servo.DragAsync(ServoTarget.ById("card"), ServoTarget.ById("bin"), steps: 4);

        Assert.Equal(120, scene.Bin.ArrangedBounds.X);
        Assert.Equal((160f, 50f), scene.ReleasePoint);
        Assert.Same(scene.Bin, scene.DropTarget);
    }

    // The destination is waited for like the source: no attempt presses the pointer while it is not actionable,
    // and the timeout names both targets and the destination's last failure.
    [Theory]
    [InlineData("disabled", "The Servo target Id=\"bin\" is disabled.")]
    [InlineData("missing", "The Servo target Id=\"missing-bin\" did not match any element.")]
    public async Task DragToANonActionableDestinationTimesOutWithoutPressingThePointer(string state, string lastFailure)
    {
        DragScene scene = DragScene.Create();
        ServoApi servo = new(scene.Host);
        int dragsStarted = 0;
        scene.Card.DragStarted += (_, _) => dragsStarted++;
        ServoTarget destination = ServoTarget.ById(state == "missing" ? "missing-bin" : "bin");
        if (state == "disabled")
        {
            scene.Bin.IsEnabled = false;
        }

        ServoTimeoutException failure = await Assert.ThrowsAsync<ServoTimeoutException>(
            () => servo.DragAsync(ServoTarget.ById("card"), destination, 12, TimeSpan.FromMilliseconds(200)));

        Assert.Equal(
            $"The Servo operation on targets Id=\"card\" and {destination} exceeded its 00:00:00.2000000 timeout. " +
            $"Last failure: {lastFailure}",
            failure.Message);
        Assert.Equal(lastFailure, failure.InnerException!.Message);
        Assert.IsType(
            state == "missing" ? typeof(ServoTargetNotFoundException) : typeof(ServoTargetNotActionableException),
            failure.InnerException);
        Assert.Equal(0, dragsStarted);
        Assert.Null(scene.ReleasePoint);
        Assert.False(scene.Host.LastFrame!.Input.Pointer.IsDown(InputMouseButton.Left));
    }

    [Fact]
    public async Task DragToTargetWaitsForADestinationThatBecomesEnabledInALaterFrame()
    {
        DragScene scene = DragScene.Create();
        ServoApi servo = new(scene.Host);
        int dragsStarted = 0;
        scene.Card.DragStarted += (_, _) => dragsStarted++;
        scene.Bin.IsEnabled = false;

        Task drag = servo.DragAsync(ServoTarget.ById("card"), ServoTarget.ById("bin"), steps: 4);
        Assert.False(drag.IsCompleted);
        scene.PumpFrame();
        Thread.Sleep(10);
        scene.PumpFrame();
        Assert.False(drag.IsCompleted);
        Assert.Equal(0, dragsStarted);

        scene.Bin.IsEnabled = true;
        for (int frame = 0; frame < 32 && !drag.IsCompleted; frame++)
        {
            scene.PumpFrame();
            Thread.Sleep(1);
        }

        Assert.True(drag.IsCompleted, "The Servo drag did not complete within 32 frames.");
        await drag;
        Assert.Equal(1, dragsStarted);
        Assert.Equal((260f, 50f), scene.ReleasePoint);
        Assert.Same(scene.Bin, scene.DropTarget);
    }

    [Fact]
    public void DragToTargetValidatesItsArguments()
    {
        DragScene scene = DragScene.Create();
        ServoApi servo = new(scene.Host);

        Assert.Throws<ArgumentNullException>(
            () => { _ = servo.DragAsync(ServoTarget.ById("card"), (ServoTarget)null!); });
        Assert.Throws<ArgumentNullException>(
            () => { _ = servo.DragAsync(null!, ServoTarget.ById("bin")); });
        Assert.Throws<ArgumentOutOfRangeException>(
            () => { _ = servo.DragAsync(ServoTarget.ById("card"), ServoTarget.ById("bin"), steps: 0); });
    }

    private sealed class DragScene
    {
        private DragScene(UiHost host, Thumb card, Border bin)
        {
            Host = host;
            Card = card;
            Bin = bin;
        }

        internal UiHost Host { get; }

        internal Thumb Card { get; }

        internal Border Bin { get; }

        internal (float X, float Y)? ReleasePoint { get; private set; }

        internal UIElement? DropTarget { get; private set; }

        internal void PumpFrame() =>
            Host.Update(
                new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []),
                Host.Viewport,
                TimeSpan.FromMilliseconds(16));

        internal static DragScene Create()
        {
            // The card's center is (50, 80); the bin's center is (260, 50).
            Thumb card = new() { Width = 60, Height = 40 };
            ServoApi.SetId(card, "card");
            Canvas.SetLeft(card, 20);
            Canvas.SetTop(card, 60);
            Border bin = new() { Width = 80, Height = 60, Background = new SolidColorBrush(new Color(40, 120, 40)) };
            ServoApi.SetId(bin, "bin");
            Canvas.SetLeft(bin, 220);
            Canvas.SetTop(bin, 20);
            Canvas canvas = new() { Width = 320, Height = 160 };
            canvas.VisualChildren.Add(card);
            canvas.VisualChildren.Add(bin);
            UIRoot root = new(320, 160);
            root.VisualChildren.Add(canvas);
            UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(320, 160) });
            DragScene scene = new(host, card, bin);
            card.AddHandler(
                InputEvents.MouseUpEvent,
                (_, args) =>
                {
                    MouseEventArgs release = (MouseEventArgs)args;
                    scene.ReleasePoint = (release.X, release.Y);
                    scene.DropTarget = root.InputCache.HitTest(root, release.X, release.Y)?.Element;
                },
                handledEventsToo: true);
            host.Update(
                new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []),
                host.Viewport,
                TimeSpan.Zero);
            return scene;
        }
    }
}
