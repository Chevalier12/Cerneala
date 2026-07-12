using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

public sealed class ProgressBarTests
{
    [Fact]
    public void ProgressBarRendersValueRatio()
    {
        UIRoot root = new();
        ProgressBar progress = new()
        {
            Minimum = 0,
            Maximum = 100,
            Value = 25,
            Foreground = new Cerneala.UI.Media.SolidColorBrush(Color.White)
        };
        root.VisualChildren.Add(progress);
        root.ProcessFrame();
        progress.Arrange(new ArrangeContext(new LayoutRect(0, 0, 80, 10)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Equal(DrawCommandKind.FillRectangle, commands[1].Kind);
        Assert.Equal(new DrawRect(0, 0, 20, 10), commands[1].Rect);
        Assert.Equal(0.25f, progress.ValueRatio);
    }

    [Fact]
    public void MarkupValueRemainsEffectiveAfterRangeIsConfigured()
    {
        ProgressBar progress = new();

        progress.SetValue(RangeBase.MinimumProperty, 0f, UiPropertyValueSource.MarkupBase);
        progress.SetValue(RangeBase.MaximumProperty, 100f, UiPropertyValueSource.MarkupBase);
        progress.SetValue(RangeBase.ValueProperty, 68f, UiPropertyValueSource.MarkupBase);

        Assert.Equal(68f, progress.Value);
        Assert.Equal(0.68f, progress.ValueRatio);
    }
}
