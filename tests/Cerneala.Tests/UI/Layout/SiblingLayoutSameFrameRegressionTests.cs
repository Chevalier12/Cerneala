using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Panel = Cerneala.UI.Layout.Panels.Panel;
using StackPanel = Cerneala.UI.Layout.Panels.StackPanel;

namespace Cerneala.Tests.UI.Layout;

public sealed class SiblingLayoutSameFrameRegressionTests
{
    [Theory]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void AutoColumnChildIsRearrangedWhenGridSiblingChangesInTheSameFrame(bool stackParent, bool changeSibling, bool siblingIsBorder)
    {
        UIRoot root = new(700, 200);
        Panel outer = stackParent ? new StackPanel() : CreateOuterGrid();
        Grid header = new();
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        if (stackParent)
        {
            header.Height = 28;
        }

        TextBlock state = new() { Text = "" };
        Grid.SetColumn(state, 2);
        header.VisualChildren.Add(new TextBlock { Text = "Back" });
        header.VisualChildren.Add(state);

        TextBlock siblingText = new() { Text = "" };
        Border siblingBorder = new() { Width = 10, Height = 10 };
        UIElement sibling = siblingIsBorder ? siblingBorder : siblingText;
        Grid.SetRow(sibling, 1);
        outer.VisualChildren.Add(header);
        outer.VisualChildren.Add(sibling);
        root.VisualChildren.Add(outer);
        Settle(root);

        state.Text = "working";
        if (changeSibling)
        {
            if (siblingIsBorder)
            {
                siblingBorder.Width = 50;
            }
            else
            {
                siblingText.Text = "PID 4242";
            }
        }

        Settle(root);

        Assert.True(state.DesiredSize.Width > 0);
        Assert.True(
            state.ArrangedBounds.Width == state.DesiredSize.Width &&
            state.ArrangedBounds.X + state.ArrangedBounds.Width == header.ArrangedBounds.X + header.ArrangedBounds.Width,
            $"Expected the Auto column child to fill its desired width at the header's right edge; " +
            $"state={root.Detective.CaptureLayout(state)}; header={root.Detective.CaptureLayout(header)}");
    }

    private static Grid CreateOuterGrid()
    {
        Grid grid = new();
        grid.RowDefinitions.Add(new RowDefinition(new GridLength(28)));
        grid.RowDefinitions.Add(new RowDefinition(new GridLength(38)));
        return grid;
    }

    private static void Settle(UIRoot root)
    {
        root.ProcessFrame();
        for (int frame = 0; frame < 20 && root.Scheduler.HasWork; frame++)
        {
            root.ProcessFrame();
        }

        Assert.False(root.Scheduler.HasWork);
    }
}
