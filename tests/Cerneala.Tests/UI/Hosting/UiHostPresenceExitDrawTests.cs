using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Presence;
using Cerneala.Tests.UI.Motion.Core;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Hosting;

// Issue #651: the window frame path (UiHost.Update commits, UiHost.Draw hands the committed list to
// the drawing backend) must draw an element whose presence exit runs, at its last bounds, until the exit ends.
public sealed class UiHostPresenceExitDrawTests
{
    private static readonly UiViewport Viewport = new(500, 200);
    private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(16);

    [Fact]
    public void ExitingElementsOfEveryOwnerReachTheDrawingBackendUntilTheirExitEnds()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(500, 200, motionClock: clock);
        UiHost host = new(new UiHostOptions { Root = root });
        FakeDrawingBackend backend = new();
        ContentPresenter presenter = new() { Width = 90, Height = 70 };
        ContentControl control = new() { Width = 90, Height = 70 };
        StackPanel panel = new() { Width = 90, Height = 70 };
        Border decorator = new() { Width = 90, Height = 70 };
        StackPanel row = new() { Orientation = Orientation.Horizontal };
        row.Children.Add(presenter);
        row.Children.Add(control);
        row.Children.Add(panel);
        row.Children.Add(decorator);
        root.VisualChildren.Add(row);
        Color[] colors =
        [
            new(0x2E, 0x86, 0xFF),
            new(0x2E, 0x86, 0xFE),
            new(0x2E, 0x86, 0xFD),
            new(0x2E, 0x86, 0xFC)
        ];
        Border[] boxes = colors.Select(CreateBox).ToArray();
        presenter.Content = boxes[0];
        control.Content = boxes[1];
        panel.Children.Add(boxes[2]);
        decorator.Child = boxes[3];
        Frame();
        RunFrames(TimeSpan.FromMilliseconds(400));
        LayoutRect[] lastBounds = boxes.Select(box => box.ArrangedBounds).ToArray();
        Assert.All(boxes, box => Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(box)));

        presenter.Content = null;
        control.Content = null;
        Assert.True(panel.Children.Remove(boxes[2]));
        decorator.Child = null;
        Frame();

        float[] removedOpacity = AssertDrawnAtLastBounds();
        Assert.All(removedOpacity, opacity => Assert.True(opacity > 0.99f, $"opacity {opacity}"));

        RunFrames(TimeSpan.FromSeconds(1));

        float[] midExitOpacity = AssertDrawnAtLastBounds();
        for (int index = 0; index < boxes.Length; index++)
        {
            Assert.InRange(midExitOpacity[index], float.Epsilon, removedOpacity[index] - 0.01f);
        }

        RunFrames(TimeSpan.FromSeconds(1.1));

        Assert.All(boxes, box => Assert.Equal(PresenceState.Detached, root.Motion.Presence.GetState(box)));
        Assert.All(colors, color => Assert.Empty(FindFills(color)));

        void Frame()
        {
            host.Update(FakeInputSource.CreateFrame(), Viewport, clock.Now);
            host.Draw(backend);
        }

        // A window renders a frame about every 16 ms while motion runs.
        void RunFrames(TimeSpan duration)
        {
            for (TimeSpan elapsed = TimeSpan.Zero; elapsed < duration; elapsed += FrameInterval)
            {
                clock.Advance(FrameInterval);
                Frame();
            }
        }

        float[] AssertDrawnAtLastBounds()
        {
            float[] opacities = new float[boxes.Length];
            for (int index = 0; index < boxes.Length; index++)
            {
                Assert.Equal(PresenceState.Exiting, root.Motion.Presence.GetState(boxes[index]));
                DrawCommand fill = Assert.Single(FindFills(colors[index]));
                LayoutRect bounds = lastBounds[index];
                Assert.Equal(new DrawRect(bounds.X, bounds.Y, bounds.Width, bounds.Height), fill.Rect);
                opacities[index] = fill.BrushOpacity;
            }

            return opacities;
        }

        DrawCommand[] FindFills(Color color)
        {
            DrawCommandList commands = Assert.IsType<DrawCommandList>(backend.LastCommands);
            return commands
                .Where(command =>
                    command.Kind == DrawCommandKind.FillRectangle &&
                    command.Brush is SolidColorBrush brush &&
                    brush.Color == color)
                .ToArray();
        }
    }

    private static Border CreateBox(Color color) => new()
    {
        Width = 80,
        Height = 60,
        Background = new SolidColorBrush(color),
        Presence = PresenceOptions.FadeAndScale(
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(300)),
            MotionFactory.Tween<float>(TimeSpan.FromSeconds(2)))
    };
}
