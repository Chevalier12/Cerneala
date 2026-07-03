using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

public sealed class CheckBoxTests
{
    [Fact]
    public void CheckBoxMeasuresBoxAndTextContent()
    {
        CheckBox checkBox = new() { Content = "Agree", FontSize = 10 };

        LayoutSize size = checkBox.Measure(new MeasureContext(new LayoutSize(200, 100)));

        Assert.Equal(new LayoutSize(45, 14), size);
    }

    [Fact]
    public void CheckBoxRendersCheckedBoxAndText()
    {
        UIRoot root = new();
        CheckBox checkBox = new()
        {
            Content = "Agree",
            IsChecked = true,
            Foreground = DrawColor.Black
        };
        checkBox.Arrange(new ArrangeContext(new LayoutRect(0, 0, 80, 20)));
        root.VisualChildren.Add(checkBox);

        DrawCommandList commands = root.RetainedRenderer.Render(root);

        Assert.Contains(commands, command => command.Kind == DrawCommandKind.FillRectangle && command.Color == DrawColor.White);
        Assert.Contains(commands, command => command.Kind == DrawCommandKind.DrawText);
    }
}
