using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using System.Reflection;

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

    [Theory]
    [InlineData(Visibility.Hidden)]
    [InlineData(Visibility.Collapsed)]
    public void DefaultProjectionOmitsNonRenderedSubtrees(Visibility visibility)
    {
        UIRoot root = new();
        UIElement container = new() { Visibility = visibility };
        Button descendant = new() { Content = "Descendant" };
        container.VisualChildren.Add(descendant);
        root.VisualChildren.Add(container);

        SemanticsTree tree = new SemanticsProvider().Build(root);

        Assert.Empty(tree.Root.Children);
    }

    [Fact]
    public void DefaultProjectionPreservesVisualTreeOrder()
    {
        UIRoot root = new();
        root.VisualChildren.Add(new Button { Content = "First" });
        root.VisualChildren.Add(new Button { Content = "Second" });
        root.VisualChildren.Add(new Button { Content = "Third" });

        string?[] names = new SemanticsProvider()
            .Build(root)
            .Root
            .Children
            .Select(node => node.Name)
            .ToArray();

        Assert.Equal(new[] { "First", "Second", "Third" }, names);
    }

    [Fact]
    public void ElementIdResolvesOnlyWhileElementBelongsToRoot()
    {
        UIRoot root = new();
        Button button = new() { Content = "Target" };
        root.VisualChildren.Add(button);
        SemanticsNode node = new SemanticsProvider().Build(root).Root.Children.Single();
        Assert.True(node.ElementId.HasValue);
        var elementId = node.ElementId.Value;

        Assert.True(root.ElementIds.TryGetElement(elementId, out UIElement? attached));
        Assert.Same(button, attached);

        root.VisualChildren.Remove(button);

        Assert.False(root.ElementIds.TryGetElement(elementId, out UIElement? detached));
        Assert.Null(detached);
    }

    [Fact]
    public void ServoProjectionIncludesNonRenderedSubtreesInStableOrder()
    {
        UIRoot root = new();
        Button first = new() { Content = "First" };
        UIElement hiddenContainer = new() { Visibility = Visibility.Hidden };
        AccessibleName.SetName(hiddenContainer, "Hidden container");
        Button hiddenChild = new() { Content = "Hidden child" };
        hiddenContainer.VisualChildren.Add(hiddenChild);
        Button collapsed = new() { Content = "Collapsed", Visibility = Visibility.Collapsed };
        Button last = new() { Content = "Last" };
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(hiddenContainer);
        root.VisualChildren.Add(collapsed);
        root.VisualChildren.Add(last);

        MethodInfo? overload = typeof(SemanticsProvider)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .SingleOrDefault(method =>
                method.Name == nameof(SemanticsProvider.Build) &&
                method.GetParameters() is [_, { ParameterType.IsEnum: true }]);
        Assert.True(overload is not null, "SemanticsProvider is missing its internal projection-selecting Build overload.");
        Type projectionType = overload.GetParameters()[1].ParameterType;
        object servoProjection = Enum.Parse(projectionType, "Servo");
        SemanticsTree tree = Assert.IsType<SemanticsTree>(overload.Invoke(new SemanticsProvider(), [root, servoProjection]));

        Assert.Equal(
            new[] { "First", "Hidden container", "Collapsed", "Last" },
            tree.Root.Children.Select(node => node.Name));
        Assert.Equal("Hidden child", tree.Root.Children[1].Children.Single().Name);
    }
}
