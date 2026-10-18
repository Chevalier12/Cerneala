using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using LayoutPanel = Cerneala.UI.Layout.Panels.Panel;

namespace Cerneala.Tests.UI.Layout;

// Issue #405: one public call adds a panel child to both trees, as WPF Panel.Children does.
public sealed class PanelChildrenTests
{
    public static TheoryData<string> Panels => new() { nameof(StackPanel), nameof(Cerneala.UI.Layout.Panels.Grid) };

    [Theory]
    [MemberData(nameof(Panels))]
    public void ChildrenAddMakesTheChildALogicalAndVisualChildThatThePanelLaysOut(string panelType)
    {
        LayoutPanel panel = CreatePanel(panelType);
        Border child = new() { Width = 50, Height = 20 };

        panel.Children.Add(child);
        panel.Measure(new MeasureContext(new LayoutSize(200, 200), default));
        panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 200, 200), default));

        Assert.Same(panel, child.LogicalParent);
        Assert.Same(panel, child.VisualParent);
        Assert.Equal(1, panel.Children.Count);
        Assert.Same(child, panel.Children[0]);
        if (panelType == nameof(StackPanel))
        {
            Assert.Equal(new LayoutSize(50, 20), panel.DesiredSize);
            Assert.Equal(new LayoutRect(75, 0, 50, 20), child.ArrangedBounds);
        }
        else
        {
            // A grid without definitions has one star row and column that take the finite available size.
            Assert.Equal(new LayoutSize(200, 200), panel.DesiredSize);
            Assert.Equal(new LayoutRect(75, 90, 50, 20), child.ArrangedBounds);
        }
    }

    [Theory]
    [MemberData(nameof(Panels))]
    public void ChildrenRemoveClearsBothParents(string panelType)
    {
        LayoutPanel panel = CreatePanel(panelType);
        Border child = new() { Width = 50, Height = 20 };
        panel.Children.Add(child);

        bool removed = panel.Children.Remove(child);

        Assert.True(removed);
        Assert.Null(child.LogicalParent);
        Assert.Null(child.VisualParent);
        Assert.Equal(0, panel.Children.Count);
        Assert.Empty(panel.LogicalChildren);
        Assert.Empty(panel.VisualChildren);
        Assert.False(panel.Children.Remove(child));
    }

    [Theory]
    [MemberData(nameof(Panels))]
    public void ChildrenInsertKeepsTheVisualAndLogicalOrder(string panelType)
    {
        LayoutPanel panel = CreatePanel(panelType);
        Border first = new();
        Border second = new();
        panel.Children.Add(first);

        panel.Children.Insert(0, second);

        Assert.Equal([second, first], panel.Children);
        Assert.Equal([second, first], panel.VisualChildren);
        Assert.Equal([second, first], panel.LogicalChildren);
    }

    [Theory]
    [MemberData(nameof(Panels))]
    public void LogicalChildrenAddOfAChildThatIsNotAVisualChildThrowsAndNamesChildren(string panelType)
    {
        LayoutPanel panel = CreatePanel(panelType);
        Border child = new() { Width = 50, Height = 20 };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => panel.LogicalChildren.Add(child));

        Assert.Contains("Panel.Children", exception.Message, StringComparison.Ordinal);
        Assert.Null(child.LogicalParent);
        Assert.Empty(panel.LogicalChildren);
    }

    [Fact]
    public void LogicalChildrenAddOfAVisualChildOfThePanelIsAllowed()
    {
        StackPanel panel = new();
        Border child = new();
        panel.VisualChildren.Add(child);

        panel.LogicalChildren.Add(child);

        Assert.Same(panel, child.LogicalParent);
        Assert.Same(panel, child.VisualParent);
    }

    private static LayoutPanel CreatePanel(string panelType)
    {
        return panelType switch
        {
            nameof(StackPanel) => new StackPanel(),
            _ => new Cerneala.UI.Layout.Panels.Grid()
        };
    }
}
