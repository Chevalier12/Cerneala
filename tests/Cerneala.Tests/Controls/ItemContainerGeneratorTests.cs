using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Virtualization;

namespace Cerneala.Tests.Controls;

public sealed class ItemContainerGeneratorTests
{
    [Fact]
    public void GeneratorPreservesContainerIdentityInsideSameWindow()
    {
        ItemsControl control = new();
        control.SetItems(new[] { "a", "b", "c" });

        IReadOnlyList<Cerneala.UI.Elements.UIElement> first = control.ItemContainerGenerator.Realize(new RealizationWindow(0, 2));
        IReadOnlyList<Cerneala.UI.Elements.UIElement> second = control.ItemContainerGenerator.Realize(new RealizationWindow(0, 2));

        Assert.Same(first[0], second[0]);
        Assert.Same(first[1], second[1]);
    }

    [Fact]
    public void GeneratorRecyclesUnrealizedContainersAndClearsStaleState()
    {
        ListBox listBox = new();
        listBox.SetItems(new[] { "a", "b", "c" });
        listBox.SelectedIndex = 0;
        IReadOnlyList<Cerneala.UI.Elements.UIElement> first = listBox.ItemContainerGenerator.Realize(new RealizationWindow(0, 1));
        ListBoxItem oldItem = Assert.IsType<ListBoxItem>(first[0]);
        Assert.True(oldItem.IsSelected);

        IReadOnlyList<Cerneala.UI.Elements.UIElement> second = listBox.ItemContainerGenerator.Realize(new RealizationWindow(1, 2));

        ListBoxItem recycled = Assert.IsType<ListBoxItem>(second[0]);
        Assert.Same(oldItem, recycled);
        Assert.Equal(1, recycled.ItemIndex);
        Assert.Equal("b", recycled.Item);
        Assert.False(recycled.IsSelected);
        Assert.Equal(0, listBox.ItemContainerGenerator.RecyclePool.Count);
    }

    [Fact]
    public void GeneratorDoesNotRecycleUiElementItemAsDifferentItemContainer()
    {
        ItemsControl control = new();
        FixedElement first = new("first");
        FixedElement second = new("second");
        control.SetItems(new UIElement[] { first, second });

        IReadOnlyList<UIElement> firstWindow = control.ItemContainerGenerator.Realize(new RealizationWindow(0, 1));
        IReadOnlyList<UIElement> secondWindow = control.ItemContainerGenerator.Realize(new RealizationWindow(1, 2));

        Assert.Same(first, firstWindow[0]);
        Assert.Same(second, secondWindow[0]);
        Assert.Same(second, control.ItemContainerGenerator.RealizedContainers[1]);
    }

    [Fact]
    public void GeneratorReplacesRealizedUiElementContainerWhenItemChanges()
    {
        ItemsControl control = new();
        FixedElement first = new("first");
        FixedElement second = new("second");
        control.SetItems(new UIElement[] { first });
        control.ItemContainerGenerator.Realize(new RealizationWindow(0, 1));

        control.Items[0] = second;
        IReadOnlyList<UIElement> updated = control.ItemContainerGenerator.Realize(new RealizationWindow(0, 1));

        Assert.Same(second, updated[0]);
        Assert.Same(second, control.ItemContainerGenerator.RealizedContainers[0]);
    }

    [Fact]
    public void GeneratorDetachesRecycledContainerBeforeReuse()
    {
        ItemsControl control = new();
        control.SetItems(new[] { "a", "b" });
        UIElement container = control.ItemContainerGenerator.GetOrCreate(0);
        Cerneala.UI.Layout.Panels.Panel oldPanel = new();
        oldPanel.LogicalChildren.Add(container);
        oldPanel.VisualChildren.Add(container);

        IReadOnlyList<UIElement> nextWindow = control.ItemContainerGenerator.Realize(new RealizationWindow(1, 2));

        Assert.Same(container, nextWindow[0]);
        Assert.Null(container.LogicalParent);
        Assert.Null(container.VisualParent);
        Cerneala.UI.Layout.Panels.Panel nextPanel = new();
        nextPanel.LogicalChildren.Add(container);
        nextPanel.VisualChildren.Add(container);
    }

    [Fact]
    public void PresenterPreservesUnrelatedRealizedContainersWhenDataOutsideWindowChanges()
    {
        ItemsControl control = new();
        control.SetItems(Enumerable.Range(0, 20).Cast<object>());
        control.SetVirtualizationContext(new VirtualizationContext(20, 10, 30, 0, 0));
        control.Measure(new MeasureContext(new LayoutSize(100, 30)));
        Cerneala.UI.Elements.UIElement first = control.ItemsPresenter.LayoutPanelRoot!.VisualChildren[0];

        control.Items[10] = 999;
        control.Measure(new MeasureContext(new LayoutSize(100, 30)));

        Assert.Same(first, control.ItemsPresenter.LayoutPanelRoot!.VisualChildren[0]);
    }

    private sealed class FixedElement(string value) : UIElement
    {
        public string Value { get; } = value;

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(10, 10);
        }
    }
}
