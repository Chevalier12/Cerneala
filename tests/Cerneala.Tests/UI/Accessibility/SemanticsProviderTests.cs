using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

namespace Cerneala.Tests.UI.Accessibility;

public sealed class SemanticsProviderTests
{
    [Fact]
    public void ExplicitAccessibleNameOverridesContentText()
    {
        UIRoot root = new();
        Button button = new() { Content = "Content" };
        AccessibleName.SetName(button, "Explicit");
        root.VisualChildren.Add(button);

        SemanticsNode node = new SemanticsProvider().Build(root).Root.Children.Single();

        Assert.Equal("Explicit", node.Name);
        Assert.Equal(SemanticsRole.Button, node.Role);
    }

    [Fact]
    public void ItemsControlReportsItemCount()
    {
        UIRoot root = new();
        ItemsControl items = new();
        items.SetItems(new[] { "one", "two", "three" });
        root.VisualChildren.Add(items);

        SemanticsNode node = new SemanticsProvider().Build(root).Root.Children.Single();

        Assert.Equal(SemanticsRole.List, node.Role);
        Assert.Equal(3, node.GetProperty<int>(SemanticsProperty.ItemCount));
    }
}
