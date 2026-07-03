using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.UI.Accessibility;

public sealed class SemanticsTreeTests
{
    [Fact]
    public void NodeCopiesPropertiesIntoImmutableSnapshot()
    {
        Dictionary<SemanticsProperty, object?> properties = new()
        {
            [SemanticsProperty.IsEnabled] = true
        };
        SemanticsNode node = new(null, SemanticsRole.Button, properties: properties);

        properties[SemanticsProperty.IsEnabled] = false;

        Assert.True(node.GetProperty<bool>(SemanticsProperty.IsEnabled));
    }

    [Fact]
    public void NodeCopiesChildrenIntoImmutableSnapshot()
    {
        List<SemanticsNode> children =
        [
            new(null, SemanticsRole.Text, "Before")
        ];
        SemanticsNode node = new(null, SemanticsRole.Group, children: children);

        children.Add(new SemanticsNode(null, SemanticsRole.Text, "After"));

        SemanticsNode child = Assert.Single(node.Children);
        Assert.Equal("Before", child.Name);
    }

    [Fact]
    public void ProviderBuildsTreeInVisualOrderAndSkipsHiddenElements()
    {
        UIRoot root = new();
        Button first = new() { Content = "First" };
        Button hidden = new() { Content = "Hidden", Visibility = Visibility.Collapsed };
        TextBox second = new() { Text = "Second" };
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(hidden);
        root.VisualChildren.Add(second);

        SemanticsTree tree = new SemanticsProvider().Build(root);

        Assert.Equal(SemanticsRole.Root, tree.Root.Role);
        Assert.Equal([SemanticsRole.Button, SemanticsRole.EditableText], tree.Root.Children.Select(child => child.Role).ToArray());
        Assert.Equal("First", tree.Root.Children[0].Name);
        Assert.DoesNotContain(tree.Root.Children, child => child.Name == "Hidden");
    }
}
