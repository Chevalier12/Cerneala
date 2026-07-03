using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls.Primitives;

public sealed class SelectorTests
{
    [Fact]
    public void SelectorClickSelectsItemContainer()
    {
        ListBox listBox = CreateArrangedListBox();
        ElementInputBridge bridge = new();

        bridge.Dispatch(listBox.Root!, PointerFrame(5, 15, currentDown: true));
        bridge.Dispatch(listBox.Root!, PointerFrame(5, 15, previousDown: true));

        Assert.Equal(1, listBox.SelectedIndex);
    }

    [Fact]
    public void SelectionInvalidatesAffectedContainersOnly()
    {
        ListBox listBox = CreateArrangedListBox();
        ListBoxItem first = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[0]);
        ListBoxItem second = Assert.IsType<ListBoxItem>(listBox.ItemContainerGenerator.RealizedContainers[1]);
        int firstVersion = first.RenderVersion;
        int secondVersion = second.RenderVersion;

        listBox.SelectedIndex = 0;
        listBox.SelectedIndex = 1;

        Assert.True(first.RenderVersion > firstVersion);
        Assert.True(second.RenderVersion > secondVersion);
    }

    [Fact]
    public void SelectionReprepareDoesNotDuplicateContainerClickHandlers()
    {
        ListBox listBox = CreateArrangedListBox();
        UIElement first = listBox.ItemContainerGenerator.RealizedContainers[0];

        listBox.SelectedIndex = 0;

        Assert.Single(first.Handlers.GetHandlers(InputEvents.MouseUpEvent));
    }

    [Fact]
    public void ReusedContainerClickSelectsCurrentSelectorOnly()
    {
        ListBoxItem shared = new();
        ListBox first = new();
        first.SetItems(new[] { shared });
        first.ItemContainerGenerator.GetOrCreate(0);
        first.ItemContainerGenerator.Recycle(0);

        ListBox second = new();
        second.SetItems(new[] { shared });
        second.ItemContainerGenerator.GetOrCreate(0);

        MouseButtonEventArgs args = new(InputEvents.MouseUpEvent, shared, InputMouseButton.Left, 0, 0, 1);
        foreach (RoutedEventHandler handler in shared.Handlers.GetHandlers(InputEvents.MouseUpEvent))
        {
            handler(new UiElementId("shared"), args);
        }

        Assert.Equal(-1, first.SelectedIndex);
        Assert.Equal(0, second.SelectedIndex);
    }

    private static ListBox CreateArrangedListBox()
    {
        UIRoot root = new(100, 100);
        ListBox listBox = new();
        listBox.SetItems(new UIElement[]
        {
            new FixedElement(new LayoutSize(20, 10)),
            new FixedElement(new LayoutSize(20, 10))
        });
        root.VisualChildren.Add(listBox);
        listBox.Measure(new MeasureContext(new LayoutSize(100, 100)));
        listBox.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));
        return listBox;
    }

    private static InputFrame PointerFrame(float x, float y, bool previousDown = false, bool currentDown = false)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(x, y);
        if (previousDown)
        {
            previous = previous.WithButton(InputMouseButton.Left, true);
        }

        if (currentDown)
        {
            current = current.WithButton(InputMouseButton.Left, true);
        }

        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }
}
