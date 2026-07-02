using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class RoutedEventRouterTests
{
    [Fact]
    public void BubbleEventsInvokeTargetThenAncestors()
    {
        UiInputTree tree = new();
        UiElementId root = new("root");
        UiElementId panel = new("panel");
        UiElementId button = new("button");
        List<string> calls = new();

        tree.Add(root, null);
        tree.Add(panel, root);
        tree.Add(button, panel);
        tree.AddHandler(root, InputEvents.MouseDownEvent, (_, _) => calls.Add("root"));
        tree.AddHandler(panel, InputEvents.MouseDownEvent, (_, _) => calls.Add("panel"));
        tree.AddHandler(button, InputEvents.MouseDownEvent, (_, _) => calls.Add("button"));

        MouseButtonEventArgs args = new(InputEvents.MouseDownEvent, button, InputMouseButton.Left, 0, 0, 1);
        RoutedEventRouter.Raise(tree, button, args);

        Assert.Equal(["button", "panel", "root"], calls);
    }

    [Fact]
    public void TunnelEventsInvokeRootThenTarget()
    {
        UiInputTree tree = new();
        UiElementId root = new("root");
        UiElementId panel = new("panel");
        UiElementId button = new("button");
        List<string> calls = new();

        tree.Add(root, null);
        tree.Add(panel, root);
        tree.Add(button, panel);
        tree.AddHandler(root, InputEvents.PreviewMouseDownEvent, (_, _) => calls.Add("root"));
        tree.AddHandler(panel, InputEvents.PreviewMouseDownEvent, (_, _) => calls.Add("panel"));
        tree.AddHandler(button, InputEvents.PreviewMouseDownEvent, (_, _) => calls.Add("button"));

        MouseButtonEventArgs args = new(InputEvents.PreviewMouseDownEvent, button, InputMouseButton.Left, 0, 0, 1);
        RoutedEventRouter.Raise(tree, button, args);

        Assert.Equal(["root", "panel", "button"], calls);
    }

    [Fact]
    public void HandledStopsRoute()
    {
        UiInputTree tree = new();
        UiElementId root = new("root");
        UiElementId button = new("button");
        List<string> calls = new();

        tree.Add(root, null);
        tree.Add(button, root);
        tree.AddHandler(button, InputEvents.MouseDownEvent, (_, args) =>
        {
            calls.Add("button");
            args.Handled = true;
        });
        tree.AddHandler(root, InputEvents.MouseDownEvent, (_, _) => calls.Add("root"));

        MouseButtonEventArgs args = new(InputEvents.MouseDownEvent, button, InputMouseButton.Left, 0, 0, 1);
        RoutedEventRouter.Raise(tree, button, args);

        Assert.Equal(["button"], calls);
    }

    [Fact]
    public void DisabledElementsDoNotInvokeHandlers()
    {
        UiInputTree tree = new();
        UiElementId root = new("root");
        UiElementId panel = new("panel");
        UiElementId button = new("button");
        List<string> calls = new();

        tree.Add(root, null);
        tree.Add(panel, root, isEnabled: false);
        tree.Add(button, panel);
        tree.AddHandler(root, InputEvents.MouseDownEvent, (_, _) => calls.Add("root"));
        tree.AddHandler(panel, InputEvents.MouseDownEvent, (_, _) => calls.Add("panel"));
        tree.AddHandler(button, InputEvents.MouseDownEvent, (_, _) => calls.Add("button"));

        MouseButtonEventArgs args = new(InputEvents.MouseDownEvent, button, InputMouseButton.Left, 0, 0, 1);
        RoutedEventRouter.Raise(tree, button, args);

        Assert.Equal(["button", "root"], calls);
    }
}
