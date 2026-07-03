using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class RetainedRoutedEventIntegrationTests
{
    [Fact]
    public void BubbleAndPreviewRoutesFollowRetainedVisualParents()
    {
        UIRoot root = new();
        UIElement parent = new();
        UIElement child = new();
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        List<string> calls = [];
        root.Handlers.AddHandler(InputEvents.PreviewMouseDownEvent, (_, _) => calls.Add("preview-root"));
        parent.Handlers.AddHandler(InputEvents.PreviewMouseDownEvent, (_, _) => calls.Add("preview-parent"));
        child.Handlers.AddHandler(InputEvents.PreviewMouseDownEvent, (_, _) => calls.Add("preview-child"));
        child.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, _) => calls.Add("bubble-child"));
        parent.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, _) => calls.Add("bubble-parent"));
        root.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, _) => calls.Add("bubble-root"));
        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        Assert.True(map.TryGetId(child, out UiElementId childId));

        RoutedEventRouter.Raise(map.InputTree, childId, new MouseButtonEventArgs(InputEvents.PreviewMouseDownEvent, childId, InputMouseButton.Left, 0, 0, 1));
        RoutedEventRouter.Raise(map.InputTree, childId, new MouseButtonEventArgs(InputEvents.MouseDownEvent, childId, InputMouseButton.Left, 0, 0, 1));

        Assert.Equal(["preview-root", "preview-parent", "preview-child", "bubble-child", "bubble-parent", "bubble-root"], calls);
    }

    [Fact]
    public void ElementRoutedEventStoreUsesElementHandlerStore()
    {
        UIElement element = new();
        ElementRoutedEventStore store = new(element);
        bool called = false;

        store.AddHandler(InputEvents.MouseDownEvent, (_, _) => called = true);

        Assert.Single(store.GetHandlers(InputEvents.MouseDownEvent));
        store.GetHandlers(InputEvents.MouseDownEvent)[0](new UiElementId("x"), new RoutedEventArgs(InputEvents.MouseDownEvent, "x"));
        Assert.True(called);
    }
}
