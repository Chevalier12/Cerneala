using Cerneala.Playground;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Input;
using Cerneala.UI.Elements;
using Grid = Cerneala.UI.Layout.Panels.Grid;

namespace Cerneala.Tests.Playground;

public sealed class MainWindowContractTests
{
    [Fact]
    public void GeneratedWindowBuildsHeaderNavigationAndShowcaseHost()
    {
        MainWindow window = new();

        Assert.Equal("Cerneala Playground", window.Title);
        Assert.Equal(800, window.Width);
        Assert.Equal(600, window.Height);

        Grid root = Assert.IsType<Grid>(window.Content);
        Assert.Equal(2, root.RowDefinitions.Count);
        Border header = Assert.IsType<Border>(root.VisualChildren[0]);
        Grid body = Assert.IsType<Grid>(root.VisualChildren[1]);
        Assert.IsType<Grid>(header.Child);
        Assert.Equal(2, body.ColumnDefinitions.Count);

        Border navigationBorder = Assert.IsType<Border>(body.VisualChildren[0]);
        ShowcaseNavigation navigation = Assert.IsType<ShowcaseNavigation>(navigationBorder.Child);
        StackPanel navigationItems = navigation.NavigationPanel;
        Grid showcaseHost = Assert.IsType<Grid>(body.VisualChildren[1]);

        Assert.Equal(0, Grid.GetColumn(navigationBorder));
        Assert.Equal(1, Grid.GetColumn(showcaseHost));
        Assert.True(navigationItems.VisualChildren.OfType<Button>().Count() >= 40);
        Assert.Contains(navigationItems.VisualChildren.OfType<TextBlock>(), item => item.Text == "CONTROALE");
        Assert.Contains(navigationItems.VisualChildren.OfType<TextBlock>(), item => item.Text == "LAYOUT");
        Assert.Contains(navigationItems.VisualChildren.OfType<TextBlock>(), item => item.Text == "ASPECT");
        Assert.Contains(navigationItems.VisualChildren.OfType<TextBlock>(), item => item.Text == "MOTION");
        Assert.Contains(navigationItems.VisualChildren.OfType<TextBlock>(), item => item.Text == "SISTEME");
    }

    [Fact]
    public void NavigationClickSelectsTheFutureShowcaseSlot()
    {
        MainWindow window = new();
        Grid root = Assert.IsType<Grid>(window.Content);
        Border header = Assert.IsType<Border>(root.VisualChildren[0]);
        Grid headerGrid = Assert.IsType<Grid>(header.Child);
        TextBlock selectedShowcase = headerGrid.VisualChildren.OfType<TextBlock>()
            .Single(item => item.Text == "Selecteaza un showcase");

        Grid body = Assert.IsType<Grid>(root.VisualChildren[1]);
        Border navigationBorder = Assert.IsType<Border>(body.VisualChildren[0]);
        ShowcaseNavigation navigation = Assert.IsType<ShowcaseNavigation>(navigationBorder.Child);
        StackPanel navigationItems = navigation.NavigationPanel;
        Button layoutMotion = navigationItems.VisualChildren.OfType<Button>()
            .Single(button => Equals(button.Content, "Layout motion"));

        layoutMotion.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, layoutMotion));

        Grid showcaseHost = Assert.IsType<Grid>(body.VisualChildren[1]);
        StackPanel emptyState = Assert.IsType<StackPanel>(Assert.Single(showcaseHost.VisualChildren));
        TextBlock[] emptyStateText = emptyState.VisualChildren.OfType<TextBlock>().ToArray();
        Assert.Equal("Layout motion", selectedShowcase.Text);
        Assert.Equal("Layout motion", emptyStateText[0].Text);
        Assert.Contains("montat aici", emptyStateText[1].Text, StringComparison.Ordinal);
    }

    [Fact]
    public void DiagnosticsHeaderStartsReadyForTheFirstRenderedFrame()
    {
        MainWindow window = new();
        Grid root = Assert.IsType<Grid>(window.Content);
        Border header = Assert.IsType<Border>(root.VisualChildren[0]);
        Grid headerGrid = Assert.IsType<Grid>(header.Child);

        TextBlock diagnostics = headerGrid.VisualChildren.OfType<TextBlock>()
            .Single(item => item.Text == "Astept primul frame...");

        Assert.Equal("Consolas", diagnostics.FontFamily);
        Assert.Null(window.LastFrame);
    }

    [Fact]
    public void WrappedDiagnosticsUpdateDoesNotRemeasureTheWholeWindow()
    {
        UIRoot root = new(800, 600);
        MainWindow window = new();
        root.LogicalChildren.Add(window);
        root.VisualChildren.Add(window);
        while (root.Scheduler.HasWork)
        {
            root.ProcessFrame();
        }

        TextBlock diagnostics = Descendants(root)
            .OfType<TextBlock>()
            .Single(element => element.Text == "Astept primul frame...");
        Assert.Equal(Cerneala.UI.Text.TextWrapping.Wrap, diagnostics.TextWrapping);

        diagnostics.Text = DiagnosticsText(1);
        root.ProcessFrame();
        diagnostics.Text = DiagnosticsText(2);

        Cerneala.UI.Invalidation.FrameStats stats = root.ProcessFrame();

        Assert.Equal(1, stats.MeasureCalls);
    }

    private static IEnumerable<UIElement> Descendants(UIElement element)
    {
        foreach (UIElement child in element.VisualChildren)
        {
            yield return child;
            foreach (UIElement descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    private static string DiagnosticsText(int frame)
    {
        return $"{frame}.54 ms | frame queuedMeasure=6, queuedArrange=8, measureCalls=568, arrangeCalls=17, renderCache=5, hitTest=0, reusedCaches=0, noWork=0, motion=0, sampled=0, motionValues=0, motionWrites=0, completed=0, motionRender=0, motionLayout=0, reduced=0, hasWork=True";
    }
}
