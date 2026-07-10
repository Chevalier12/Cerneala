using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

public sealed class WpfEventSurfaceTests
{
    [Fact]
    public void RoutedEventAddOwnerKeepsOneCanonicalIdentity()
    {
        RoutedEvent routedEvent = RoutedEventRegistry.Register("Owned", typeof(WpfEventSurfaceTests), RoutingStrategy.Bubble, typeof(RoutedEventArgs));

        RoutedEvent result = routedEvent.AddOwner(typeof(Button));

        Assert.Same(routedEvent, result);
        Assert.True(routedEvent.IsOwnedBy(typeof(WpfEventSurfaceTests)));
        Assert.True(routedEvent.IsOwnedBy(typeof(Button)));
        Assert.Equal(typeof(StylusEventArgs), UIElement.StylusDownEvent.ArgsType);
        Assert.Equal(typeof(TouchEventArgs), UIElement.TouchDownEvent.ArgsType);
        Assert.Equal(typeof(DragEventArgs), UIElement.DropEvent.ArgsType);
    }

    [Fact]
    public void HandledEventsTooRunsWithoutResumingOrdinaryHandlers()
    {
        UiInputTree tree = new();
        UiElementId root = new("root");
        UiElementId child = new("child");
        List<string> calls = [];
        tree.Add(root, null);
        tree.Add(child, root);
        tree.AddHandler(child, UIElement.MouseDownEvent, (_, args) => { calls.Add("child"); args.Handled = true; });
        tree.AddHandler(root, UIElement.MouseDownEvent, (_, _) => calls.Add("ordinary"));
        tree.AddHandler(root, UIElement.MouseDownEvent, (_, _) => calls.Add("handled-too"), handledEventsToo: true);

        RoutedEventRouter.Raise(tree, child, new MouseButtonEventArgs(UIElement.MouseDownEvent, child, InputMouseButton.Left, 0, 0, 1));

        Assert.Equal(["child", "handled-too"], calls);
    }

    [Fact]
    public void HandledPreviewStillOffersBubbleToHandledEventsToo()
    {
        UiInputTree tree = new();
        UiElementId root = new("root");
        UiElementId child = new("child");
        bool observed = false;
        tree.Add(root, null);
        tree.Add(child, root);
        tree.AddHandler(root, UIElement.PreviewMouseDownEvent, (_, args) => args.Handled = true);
        tree.AddHandler(root, UIElement.MouseDownEvent, (_, _) => observed = true, handledEventsToo: true);

        RoutedEventRouter.RaisePair(
            tree,
            child,
            new MouseButtonEventArgs(UIElement.PreviewMouseDownEvent, child, InputMouseButton.Left, 0, 0, 1),
            new MouseButtonEventArgs(UIElement.MouseDownEvent, child, InputMouseButton.Left, 0, 0, 1));

        Assert.True(observed);
    }

    [Fact]
    public void ButtonClickAndToggleEventsUseRoutedEventIdentity()
    {
        ToggleButton button = new();
        List<string> calls = [];
        button.Checked += (_, args) => calls.Add(args.RoutedEvent.Name);
        button.Click += (_, args) => calls.Add(args.RoutedEvent.Name);

        button.RaiseEvent(new MouseButtonEventArgs(UIElement.MouseUpEvent, button, InputMouseButton.Left, 0, 0, 1));

        Assert.True(button.IsChecked);
        Assert.Equal(["Checked", "Click"], calls);
    }

    [Fact]
    public void PropertyAndLayoutEventsCarryOldAndNewValues()
    {
        UIElement element = new();
        object oldContext = new();
        object newContext = new();
        element.DataContext = oldContext;
        object? observedOld = null;
        object? observedNew = null;
        SizeChangedEventArgs? sizeArgs = null;
        element.DataContextChanged += (_, args) => { observedOld = args.OldValue; observedNew = args.NewValue; };
        element.SizeChanged += (_, args) => sizeArgs = Assert.IsType<SizeChangedEventArgs>(args);

        element.DataContext = newContext;
        element.Arrange(new ArrangeContext(new LayoutRect(0, 0, 20, 10), LayoutRounding.Disabled));

        Assert.Same(oldContext, observedOld);
        Assert.Same(newContext, observedNew);
        Assert.Equal(new LayoutSize(20, 10), sizeArgs!.NewSize);
    }

    [Fact]
    public void ControlSpecificPropertyChangesRaiseCanonicalEvents()
    {
        RangeBase range = new();
        RoutedPropertyChangedEventArgs<float>? valueArgs = null;
        range.ValueChanged += (_, args) => valueArgs = args;
        TextBox textBox = new();
        TextChangedEventArgs? textArgs = null;
        textBox.TextChanged += (_, args) => textArgs = args;

        range.Value = 0.75f;
        textBox.Text = "cerneala";

        Assert.Equal(0f, valueArgs!.OldValue);
        Assert.Equal(0.75f, valueArgs.NewValue);
        Assert.Equal(string.Empty, textArgs!.OldText);
        Assert.Equal("cerneala", textArgs.NewText);
    }

    [Fact]
    public void LoadedAndUnloadedFollowRetainedTreeAttachment()
    {
        UIRoot root = new();
        UIElement child = new();
        List<string> calls = [];
        child.Initialized += (_, _) => calls.Add("Initialized");
        child.Loaded += (_, _) => calls.Add("Loaded");
        child.Unloaded += (_, _) => calls.Add("Unloaded");

        root.VisualChildren.Add(child);
        root.VisualChildren.Remove(child);

        Assert.Equal(["Initialized", "Loaded", "Unloaded"], calls);
        Assert.True(child.IsInitialized);
        Assert.False(child.IsLoaded);
    }

    [Fact]
    public void ControlEventBubblesThroughRetainedTree()
    {
        UIRoot root = new();
        RangeBase range = new();
        bool observed = false;
        root.VisualChildren.Add(range);
        root.AddHandler(RangeBase.ValueChangedEvent, (_, args) => observed = args.Source is UiElementId);

        range.Value = 0.5f;

        Assert.True(observed);
    }

    [Fact]
    public void SelectableContainersExposeSelectedAndUnselectedEvents()
    {
        ListBoxItem item = new();
        List<string> calls = [];
        item.Selected += (_, _) => calls.Add("Selected");
        item.Unselected += (_, _) => calls.Add("Unselected");

        item.IsSelected = true;
        item.IsSelected = false;

        Assert.Equal(["Selected", "Unselected"], calls);
    }
}
