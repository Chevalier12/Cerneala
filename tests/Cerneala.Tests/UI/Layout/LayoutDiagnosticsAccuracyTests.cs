using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

namespace Cerneala.Tests.UI.Layout;

public sealed class LayoutDiagnosticsAccuracyTests
{
    [Fact]
    public void FrameStatsCountActualRecursiveMeasureAndArrangeCalls()
    {
        UIRoot root = new();
        StackPanel panel = new() { Orientation = Orientation.Vertical };
        CountingElement first = new();
        CountingElement second = new();
        panel.VisualChildren.Add(first);
        panel.VisualChildren.Add(second);
        root.VisualChildren.Add(panel);
        UiHost host = new(new UiHostOptions { Root = root });

        UiFrame frame = host.Update(Frame(), new UiViewport(100, 100), TimeSpan.Zero);

        Assert.True(frame.Stats.MeasuredElements > 0);
        Assert.True(frame.Stats.ArrangedElements > 0);
        Assert.True(frame.Stats.MeasureCalls >= 3);
        Assert.True(frame.Stats.ArrangeCalls >= 3);
        Assert.True(first.MeasureCoreCalls > 0);
        Assert.True(second.MeasureCoreCalls > 0);
        Assert.True(first.ArrangeCoreCalls > 0);
        Assert.True(second.ArrangeCoreCalls > 0);
    }

    private static InputFrame Frame()
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            []);
    }

    private sealed class CountingElement : UIElement
    {
        public int MeasureCoreCalls { get; private set; }

        public int ArrangeCoreCalls { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureCoreCalls++;
            return new LayoutSize(10, 10);
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            ArrangeCoreCalls++;
            return new LayoutRect(context.FinalRect.X, context.FinalRect.Y, DesiredSize.Width, DesiredSize.Height);
        }
    }
}
