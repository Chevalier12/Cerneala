using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Controls;

public sealed class WindowRenderingTests
{
    [Fact]
    public void WindowRendersConfiguredBackgroundAndBorder()
    {
        Window window = new()
        {
            Background = new SolidColorBrush(Color.Black),
            BorderBrush = new SolidColorBrush(Color.White),
            BorderThickness = new Thickness(2)
        };
        UIRoot root = new(100, 80);
        root.VisualChildren.Add(window);
        window.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 80)));

        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);
        Assert.Equal(2, commands.Count);
        Assert.Equal(DrawCommandKind.FillRectangle, commands[0].Kind);
        Assert.Equal(DrawCommandKind.DrawRectangle, commands[1].Kind);
        Assert.Equal(new DrawRect(0, 0, 100, 80), commands[0].Rect);
    }
}
