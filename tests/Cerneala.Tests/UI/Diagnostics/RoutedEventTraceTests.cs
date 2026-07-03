using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.Tests.UI.Diagnostics;

public sealed class RoutedEventTraceTests
{
    [Fact]
    public void BubbleTraceRunsFromTargetToRoot()
    {
        (UIRoot root, UIElement parent, UIElement child) = CreateTree();

        RoutedEventTraceSnapshot trace = RoutedEventTrace.Trace(child, InputEvents.MouseDownEvent);

        Assert.Equal(RoutingStrategy.Bubble, trace.RoutingStrategy);
        Assert.Equal(
            [$"UIElement#{child.ElementId}", $"UIElement#{parent.ElementId}", $"UIRoot#{root.ElementId}"],
            trace.Steps.Select(step => step.ToString()).ToArray());
    }

    [Fact]
    public void TunnelTraceRunsFromRootToTarget()
    {
        (UIRoot root, UIElement parent, UIElement child) = CreateTree();

        RoutedEventTraceSnapshot trace = RoutedEventTrace.Trace(child, InputEvents.PreviewMouseDownEvent);

        Assert.Equal(RoutingStrategy.Tunnel, trace.RoutingStrategy);
        Assert.Equal(
            [$"UIRoot#{root.ElementId}", $"UIElement#{parent.ElementId}", $"UIElement#{child.ElementId}"],
            trace.Steps.Select(step => step.ToString()).ToArray());
    }

    [Fact]
    public void BubbleTraceSkipsDisabledAncestorsLikeInputRoute()
    {
        UIRoot root = new();
        UIElement disabledParent = new() { IsEnabled = false };
        UIElement child = new();
        disabledParent.VisualChildren.Add(child);
        root.VisualChildren.Add(disabledParent);

        RoutedEventTraceSnapshot trace = RoutedEventTrace.Trace(child, InputEvents.MouseDownEvent);

        Assert.Equal(
            [$"UIElement#{child.ElementId}", $"UIRoot#{root.ElementId}"],
            trace.Steps.Select(step => step.ToString()).ToArray());
    }

    private static (UIRoot Root, UIElement Parent, UIElement Child) CreateTree()
    {
        UIRoot root = new();
        UIElement parent = new();
        UIElement child = new();
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        return (root, parent, child);
    }
}
