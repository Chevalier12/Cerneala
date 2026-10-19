using Cerneala.Drawing;
using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Media;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.Controls;

public sealed class TabItemTests
{
    [Fact]
    public void TabItemExposesHeaderAndSelectedState()
    {
        TabItem tabItem = new();

        tabItem.Header = "Settings";
        tabItem.IsSelected = true;

        Assert.Equal("Settings", tabItem.Header);
        Assert.True(tabItem.IsSelected);
    }

    [Fact]
    public void TabItemTracksPreparedItemContainerState()
    {
        TabControl tabControl = new();
        TabItem tabItem = new();
        tabControl.SetItems(new[] { tabItem });
        tabControl.SelectedIndex = 0;

        tabControl.ItemContainerGenerator.Realize();

        Assert.Equal(0, tabItem.ItemIndex);
        Assert.Same(tabItem, tabItem.Item);
        Assert.True(tabItem.IsSelected);
    }

    [Fact]
    public void TabItemWithoutComponentTemplateFillsItsArrangedBoundsWithBackground()
    {
        UIRoot root = new(400, 200);
        SolidColorBrush red = new(new Color(255, 0, 0));
        TabItem first = new() { Header = "Page 1", Background = red };
        TabItem second = new() { Header = "Page 2", Background = red };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second });
        tabControl.SelectedIndex = 1;
        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();

        foreach (TabItem tab in new[] { first, second })
        {
            DrawCommandList commands = Render(tab);

            DrawCommand fill = Assert.Single(commands, command => command.Kind == DrawCommandKind.FillRectangle);
            Assert.Same(red, fill.Brush);
            Assert.Equal(
                new DrawRect(tab.ArrangedBounds.X, tab.ArrangedBounds.Y, tab.ArrangedBounds.Width, tab.ArrangedBounds.Height),
                fill.Rect);
        }

        Assert.False(first.IsSelected);
        Assert.True(second.IsSelected);
    }

    [Fact]
    public void TabItemWithoutBackgroundDrawsNoFill()
    {
        UIRoot root = new(400, 200);
        TabItem tab = new() { Header = "Page 1" };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { tab });
        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();

        Assert.Empty(Render(tab));
    }

    [Fact]
    public void TabItemWithComponentTemplateLeavesItsBackgroundToTheTemplate()
    {
        UIRoot root = new(400, 200);
        TabItem tab = new()
        {
            Header = "Page 1",
            Background = new SolidColorBrush(new Color(255, 0, 0)),
            ComponentTemplate = new ComponentTemplate<TabItem>("tab", _ => new Border())
        };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { tab });
        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();

        Assert.Empty(Render(tab));
    }

    private static DrawCommandList Render(UIElement element)
    {
        DrawCommandList commands = new();
        element.Render(new RenderContext(element, new DrawingContext(commands), element.ArrangedBounds, RenderLayer.Default, new RenderCounters()));
        return commands;
    }
}
