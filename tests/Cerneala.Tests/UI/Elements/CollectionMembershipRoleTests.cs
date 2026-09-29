using Cerneala.UI.Elements;

namespace Cerneala.Tests.UI.Elements;

public sealed class CollectionMembershipRoleTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MembershipAndRemovalUseTheMatchingParentRole(bool logicalFirst)
    {
        UIElement owner = new();
        UIElement child = new();
        UIElementCollection first = logicalFirst ? owner.LogicalChildren : owner.VisualChildren;
        UIElementCollection second = logicalFirst ? owner.VisualChildren : owner.LogicalChildren;
        first.Add(child);
        Assert.False(second.Remove(child));
        second.Add(child);
        Assert.Throws<InvalidOperationException>(() => second.Add(child));
        Assert.True(first.Remove(child));
        Assert.False(first.Remove(child));
        Assert.Same(child, Assert.Single(second));
        Assert.True(second.Remove(child));
        Assert.Null(child.LogicalParent);
        Assert.Null(child.VisualParent);
    }

    [Fact]
    public void ChangedHandlerCanRemoveAndReparentTheAddedChild()
    {
        UIElement first = new();
        UIElement second = new();
        UIElement child = new();
        bool moved = false;
        first.LogicalChildren.Changed += (_, change) =>
        {
            if (moved) { return; }
            moved = true;
            Assert.Same(first, child.LogicalParent);
            Assert.True(first.LogicalChildren.Remove(child));
            second.LogicalChildren.Add(child);
        };

        first.LogicalChildren.Add(child);

        Assert.True(moved);
        Assert.Empty(first.LogicalChildren);
        Assert.Same(child, Assert.Single(second.LogicalChildren));
        Assert.Same(second, child.LogicalParent);
        Assert.False(first.LogicalChildren.Remove(child));
        Assert.True(second.LogicalChildren.Remove(child));
    }
}
