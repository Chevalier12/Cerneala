using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
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
            Foreground = DrawColor.White
        };
        progress.Arrange(new ArrangeContext(new LayoutRect(0, 0, 80, 10)));
        root.VisualChildren.Add(progress);

        DrawCommandList commands = root.RetainedRenderer.Render(root);

        Assert.Equal(DrawCommandKind.FillRectangle, commands[1].Kind);
        Assert.Equal(new DrawRect(0, 0, 20, 10), commands[1].Rect);
        Assert.Equal(0.25f, progress.ValueRatio);
    }
}
