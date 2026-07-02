using Cerneala.UI.Elements;

namespace Cerneala.Tests.UI.Elements;

public sealed class UIElementTreeTests
{
    [Fact]
    public void LogicalAndVisualParentageAreSeparate()
    {
        UIElement logicalParent = new();
        UIElement visualParent = new();
        UIElement child = new();

        logicalParent.LogicalChildren.Add(child);
        visualParent.VisualChildren.Add(child);

        Assert.Same(logicalParent, child.LogicalParent);
        Assert.Same(visualParent, child.VisualParent);
        Assert.Contains(child, logicalParent.LogicalChildren);
        Assert.Contains(child, visualParent.VisualChildren);
    }

    [Fact]
    public void ReparentingSameRelationshipWithoutRemovalIsRejected()
    {
        UIElement firstParent = new();
        UIElement secondParent = new();
        UIElement child = new();
        firstParent.LogicalChildren.Add(child);

        Assert.Throws<InvalidOperationException>(() => secondParent.LogicalChildren.Add(child));
        Assert.Same(firstParent, child.LogicalParent);
    }

    [Fact]
    public void RemovingLogicalChildDoesNotClearVisualParent()
    {
        UIElement logicalParent = new();
        UIElement visualParent = new();
        UIElement child = new();
        logicalParent.LogicalChildren.Add(child);
        visualParent.VisualChildren.Add(child);

        bool removed = logicalParent.LogicalChildren.Remove(child);

        Assert.True(removed);
        Assert.Null(child.LogicalParent);
        Assert.Same(visualParent, child.VisualParent);
    }
}
