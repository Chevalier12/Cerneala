using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;
using StackPanel = Cerneala.UI.Layout.Panels.StackPanel;

namespace Cerneala.Tests.UI.Layout;

public sealed class CollapsedLayoutBreakerRegressionTests
{
    [Theory]
    [InlineData(Visibility.Visible, false)]
    [InlineData(Visibility.Hidden, false)]
    [InlineData(Visibility.Visible, true)]
    [InlineData(Visibility.Hidden, true)]
    public void CollapseAndRestoreReflowsNestedAncestors(Visibility initialVisibility, bool layoutBoundary)
    {
        UIRoot root = new(100, 200);
        StackPanel outer = new();
        StackPanel inner = new();
        Border first = new() { Height = 40, Visibility = initialVisibility, IsLayoutBoundary = layoutBoundary };
        Border second = new() { Height = 40 };
        inner.VisualChildren.Add(first);
        outer.VisualChildren.Add(inner);
        outer.VisualChildren.Add(second);
        root.VisualChildren.Add(outer);
        Settle();
        AssertLayout(80, 40);

        for (int cycle = 0; cycle < 8; cycle++)
        {
            first.Visibility = Visibility.Collapsed;
            Settle();
            AssertLayout(40, 0);
            first.Visibility = initialVisibility;
            Settle();
            AssertLayout(80, 40);
        }

        void Settle()
        {
            for (int frame = 0; frame < 4; frame++) { root.ProcessFrame(); }
            Assert.False(root.Scheduler.HasWork);
        }

        void AssertLayout(float height, float siblingY)
        {
            Assert.Equal(height, outer.DesiredSize.Height);
            Assert.Equal(siblingY, second.ArrangedBounds.Y);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CollapsingMeasuredChildReflowsSiblingsThroughScheduledFrames(bool throughInput)
    {
        UIRoot root = new(100, 200);
        StackPanel panel = new();
        Button first = new() { Height = 40 };
        Border second = new() { Height = 40 };
        first.Command = new ActionCommand(_ => first.Visibility = Visibility.Collapsed);
        ServoApi.SetId(first, "collapse-row");
        panel.VisualChildren.Add(first);
        panel.VisualChildren.Add(second);
        root.VisualChildren.Add(panel);
        UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(100, 200, 1) });
        host.Update(new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty,
            KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []), host.Viewport, TimeSpan.Zero);
        Assert.Equal(80, panel.DesiredSize.Height);
        Assert.Equal(40, second.ArrangedBounds.Y);

        if (throughInput)
        {
            ServoApi servo = new(host);
            await servo.ClickAsync(ServoTarget.ById("collapse-row"));
        }
        else
        {
            first.Visibility = Visibility.Collapsed;
        }

        Assert.Equal(Visibility.Collapsed, first.Visibility);
        for (int frame = 0; frame < 4; frame++) { root.ProcessFrame(); }
        Assert.False(root.Scheduler.HasWork);
        Assert.True(panel.DesiredSize.Height == 40 && second.ArrangedBounds.Y == 0,
            $"Expected panel height=40 and second.Y=0 after layout settled; " +
            $"panel={root.Detective.CaptureLayout(panel)}; second={root.Detective.CaptureLayout(second)}");
    }
}
