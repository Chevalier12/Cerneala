using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Layout.Virtualization;

namespace Cerneala.Tests.Controls;

public sealed class ItemsControlTests
{
    [Fact]
    public void ObservableCollectionItemsSourceRefreshesRealizedContainers()
    {
        ObservableCollection<string> source = ["one"];
        ItemsControl control = new()
        {
            ItemsSource = source
        };
        MeasureContext context = new(new LayoutSize(100, 100));
        control.Measure(context);
        Assert.Single(control.ItemsPresenter.LayoutPanelRoot!.VisualChildren);

        source.Add("two");
        control.Measure(context);

        Assert.Equal(2, control.ItemContainerGenerator.RealizedContainers.Count);
        Assert.Equal(2, control.ItemsPresenter.LayoutPanelRoot!.VisualChildren.Count);
    }

    [Fact]
    public void ObservableCollectionInsertBeforeRealizedItemsRemapsContainers()
    {
        ObservableCollection<string> source = ["one", "two"];
        ItemsControl control = new()
        {
            ItemsSource = source
        };
        MeasureContext context = new(new LayoutSize(100, 100));
        control.Measure(context);

        source.Insert(0, "zero");
        control.Measure(context);

        Assert.Equal("zero", ItemContainerGenerator.GetItem(
            control.ItemsPresenter.LayoutPanelRoot!.VisualChildren[0]));
        Assert.Equal("one", ItemContainerGenerator.GetItem(
            control.ItemsPresenter.LayoutPanelRoot.VisualChildren[1]));
        Assert.Equal("two", ItemContainerGenerator.GetItem(
            control.ItemsPresenter.LayoutPanelRoot.VisualChildren[2]));
    }

    [Fact]
    public void LazyItemsSourceIsMaterializedOnlyOncePerSourceVersion()
    {
        CountingEnumerable<int> source = new([1, 2, 3]);
        ItemsControl control = new()
        {
            ItemsSource = source
        };

        Assert.Equal(3, control.ItemCount);
        Assert.Equal(2, control.GetItemAt(1));
        control.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(1, source.EnumerationCount);
    }

    [Fact]
    public void ScrollViewerAutomaticallyDrivesItemsControlVirtualization()
    {
        PropertyInfo? itemsPanelProperty = typeof(ItemsControl).GetProperty(nameof(ItemsControl.ItemsPanel));
        Assert.NotNull(itemsPanelProperty);
        Assert.True(typeof(Cerneala.UI.Layout.Panels.Panel).IsAssignableFrom(itemsPanelProperty.PropertyType));
        FixedElement[] source = Enumerable.Range(0, 100)
            .Select(index => new FixedElement(
                index.ToString(),
                new LayoutSize(80, index % 2 == 0 ? 10 : 20)))
            .ToArray();
        ItemsControl control = new()
        {
            ItemsSource = source
        };
        itemsPanelProperty.SetValue(control, new VirtualizingStackPanel());
        ScrollViewer viewer = new()
        {
            Content = control,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        UIRoot root = new(100, 30);
        root.VisualChildren.Add(viewer);

        root.ProcessFrame();

        Assert.IsType<VirtualizingStackPanel>(control.ItemsPresenter.LayoutPanelRoot);
        Assert.InRange(control.ItemContainerGenerator.RealizedContainers.Count, 2, 6);
        Assert.Contains(0, control.ItemContainerGenerator.RealizedContainers.Keys);

        viewer.ScrollInfo.SetVerticalOffset(45);
        root.ProcessFrame();

        Assert.DoesNotContain(0, control.ItemContainerGenerator.RealizedContainers.Keys);
        Assert.Contains(3, control.ItemContainerGenerator.RealizedContainers.Keys);
    }

    [Fact]
    public void InfrastructureMembersAreNotPublicApi()
    {
        string[] memberNames =
        [
            nameof(ItemsControl.ContentTemplateRegistry),
            nameof(ItemsControl.ItemsPresenter),
            nameof(ItemsControl.ItemContainerGenerator),
            nameof(ItemsControl.SetVirtualizationContext),
            nameof(ItemsControl.UpdateVirtualizationFromScrollInfo)
        ];

        foreach (string memberName in memberNames)
        {
            Assert.Empty(typeof(ItemsControl)
                .GetMember(memberName, BindingFlags.Instance | BindingFlags.Public));
        }
    }

    [Fact]
    public void ItemsControlExposesRetainedItemCollection()
    {
        ItemsControl control = new();

        control.Items.Add("one");
        control.Items.Add("two");

        Assert.Equal(2, control.Items.Count);
        Assert.Equal("one", control.Items[0]);
        Assert.Equal("two", control.Items[1]);
    }

    [Fact]
    public void ItemsControlResolvesTemplatesOnlyFromItsOwnedCollection()
    {
        ItemsControl control = new();
        control.Templates.Add(new ContentTemplate<string>(
            "string",
            key: null,
            priority: 0,
            context => new FixedElement(context.Data!, new LayoutSize(10, 5))));
        control.Templates.Add(new ContentTemplate<int>(
            "integer",
            key: null,
            priority: 0,
            context => new FixedElement(context.Data.ToString(), new LayoutSize(10, 5))));
        control.SetItems(new object[] { "alpha", 42 });

        control.Measure(new MeasureContext(new LayoutSize(100, 100)));

        ContentPresenter first = Assert.IsType<ContentPresenter>(control.ItemsPresenter.LayoutPanelRoot!.VisualChildren[0]);
        ContentPresenter second = Assert.IsType<ContentPresenter>(control.ItemsPresenter.LayoutPanelRoot.VisualChildren[1]);
        Assert.Equal("alpha", Assert.IsType<FixedElement>(first.PresentedChild).Value);
        Assert.Equal("42", Assert.IsType<FixedElement>(second.PresentedChild).Value);
    }

    [Fact]
    public void ItemsControlRejectsDuplicateDataTypeAndKey()
    {
        ItemsControl control = new();
        control.Templates.Add(new ContentTemplate<string>("first", null, 0, _ => new UIElement()));

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
            control.Templates.Add(new ContentTemplate<string>("second", null, 0, _ => new UIElement())));

        Assert.Contains("System.String", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ItemsControlUsesContentTemplateAndItemsPanel()
    {
        ItemsControl control = new()
        {
            ItemTemplate = new ContentTemplate<string>("test", key: null, priority: 0, context => new FixedElement(context.Data!, new LayoutSize(10, 5))),
            ItemsPanel = new Cerneala.UI.Controls.Panel()
        };
        control.SetItems(new[] { "a", "b" });

        control.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.IsType<Cerneala.UI.Controls.Panel>(control.ItemsPresenter.PanelRoot);
        Assert.Equal(2, control.ItemsPresenter.PanelRoot!.VisualChildren.Count);
        ContentPresenter firstContainer = Assert.IsType<ContentPresenter>(control.ItemsPresenter.PanelRoot.VisualChildren[0]);
        Assert.IsType<FixedElement>(firstContainer.PresentedChild);
    }

    [Fact]
    public void DefaultItemsPanelStacksGeneratedContainersVertically()
    {
        ItemsControl control = new()
        {
            ItemTemplate = new ContentTemplate<string>(
                "test",
                key: null,
                priority: 0,
                context => new FixedElement(context.Data!, new LayoutSize(10, 5)))
        };
        control.SetItems(new[] { "a", "b" });

        control.Measure(new MeasureContext(new LayoutSize(100, 100)));
        control.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        Cerneala.UI.Controls.StackPanel panel = Assert.IsType<Cerneala.UI.Controls.StackPanel>(
            control.ItemsPresenter.LayoutPanelRoot);
        Assert.Equal(2, panel.VisualChildren.Count);
        Assert.Equal(0, panel.VisualChildren[0].ArrangedBounds.Y);
        Assert.Equal(5, panel.VisualChildren[0].ArrangedBounds.Height);
        Assert.Equal(5, panel.VisualChildren[1].ArrangedBounds.Y);
        Assert.Equal(5, panel.VisualChildren[1].ArrangedBounds.Height);
    }

    [Fact]
    public void ItemTemplateCreatesContainerForElementItem()
    {
        UIElement item = new();
        ItemsControl control = new()
        {
            ItemTemplate = new ContentTemplate<UIElement>("test", key: null, priority: 0, _ => new FixedElement("templated", new LayoutSize(10, 5))),
            ItemsPanel = new Cerneala.UI.Controls.Panel()
        };
        control.SetItems(new[] { item });

        control.Measure(new MeasureContext(new LayoutSize(100, 100)));

        ContentPresenter container = Assert.IsType<ContentPresenter>(control.ItemsPresenter.PanelRoot!.VisualChildren[0]);
        FixedElement child = Assert.IsType<FixedElement>(container.PresentedChild);
        Assert.Equal("templated", child.Value);
        Assert.Null(item.LogicalParent);
        Assert.Null(item.VisualParent);
    }

    [Fact]
    public void ItemContainerAspectAppliesToGeneratedContainersAndRespectsLocalAspect()
    {
        ElementAspect containerAspect = new(
            [new ElementAspectValue(UIElement.MarginProperty, new Thickness(3, 0, 3, 3))]);
        ItemsControl control = new()
        {
            ItemContainerAspect = containerAspect
        };
        control.SetItems(new[] { "generated" });
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(control);
        root.ProcessFrame();
        root.ProcessFrame();

        UIElement generated = control.ItemsPresenter.LayoutPanelRoot!.VisualChildren[0];
        Assert.Same(containerAspect, generated.Aspect);
        Assert.Equal(new Thickness(3, 0, 3, 3), generated.Margin);
        Assert.Equal(UiPropertyValueSource.AspectBase, generated.GetValueSource(UIElement.AspectProperty));

        ElementAspect localAspect = new(
            [new ElementAspectValue(UIElement.MarginProperty, new Thickness(9))]);
        UIElement explicitItem = new()
        {
            Aspect = localAspect
        };
        control.SetItems(new[] { explicitItem });
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.Same(localAspect, explicitItem.Aspect);
        Assert.Equal(new Thickness(9), explicitItem.Margin);
        Assert.Same(containerAspect, explicitItem.GetSourceValue(
            UIElement.AspectProperty,
            UiPropertyValueSource.AspectBase));
    }

    [Fact]
    public void ItemsControlVirtualizationRealizesOnlyWindow()
    {
        ItemsControl control = new()
        {
            ItemsPanel = new VirtualizingStackPanel()
        };
        control.SetItems(Enumerable.Range(0, 100).Cast<object>());
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 0, CacheItems: 1));

        control.Measure(new MeasureContext(new LayoutSize(100, 30)));

        Assert.Equal(new RealizationWindow(0, 4), control.ItemsPresenter.CurrentRealizationWindow);
        Assert.Equal(4, control.ItemsPresenter.LayoutPanelRoot!.VisualChildren.Count);
    }

    [Fact]
    public void UpdatingVirtualizationContextRefreshesPresenterWhenMeasureSizeIsUnchanged()
    {
        ItemsControl control = new()
        {
            ItemsPanel = new VirtualizingStackPanel()
        };
        control.SetItems(Enumerable.Range(0, 100).Cast<object>());
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 0));
        MeasureContext context = new(new LayoutSize(100, 30));
        control.Measure(context);

        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 30));
        control.Measure(context);

        Assert.Equal(new RealizationWindow(3, 6), control.ItemsPresenter.CurrentRealizationWindow);
        Assert.Equal(3, ItemContainerGenerator.GetItemIndex(control.ItemsPresenter.LayoutPanelRoot!.VisualChildren[0]));
    }

    [Fact]
    public void UpdatingVirtualizationFromScrollInfoRefreshesPresenterWhenWindowChanges()
    {
        ItemsControl control = new()
        {
            ItemsPanel = new VirtualizingStackPanel()
        };
        TestScrollInfo scrollInfo = new()
        {
            ViewportHeight = 30
        };
        control.SetItems(Enumerable.Range(0, 100).Cast<object>());
        control.UpdateVirtualizationFromScrollInfo(scrollInfo, itemExtent: 10);
        MeasureContext context = new(new LayoutSize(100, 30));
        control.Measure(context);

        scrollInfo.SetVerticalOffset(30);
        control.UpdateVirtualizationFromScrollInfo(scrollInfo, itemExtent: 10);
        control.Measure(context);

        Assert.Equal(new RealizationWindow(3, 6), control.ItemsPresenter.CurrentRealizationWindow);
        Assert.Equal(3, ItemContainerGenerator.GetItemIndex(control.ItemsPresenter.LayoutPanelRoot!.VisualChildren[0]));
    }

    [Fact]
    public void ReplacingItemsImmediatelyClearsContainersFromThePreviousCollection()
    {
        ItemsControl control = new();
        control.SetItems(Enumerable.Range(0, 200).Cast<object>());
        control.Measure(new MeasureContext(new LayoutSize(100, 100)));
        Assert.Equal(200, control.ItemContainerGenerator.RealizedContainers.Count);

        control.SetItems(Enumerable.Range(0, 20).Cast<object>());

        Assert.Empty(control.ItemContainerGenerator.RealizedContainers);
    }

    private sealed class FixedElement(string value, LayoutSize size) : UIElement
    {
        public string Value { get; } = value;

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }

    private sealed class TestScrollInfo : IScrollInfo
    {
        public float HorizontalOffset { get; private set; }

        public float VerticalOffset { get; private set; }

        public float ExtentWidth { get; set; }

        public float ExtentHeight { get; set; }

        public float ViewportWidth { get; set; }

        public float ViewportHeight { get; set; }

        public bool CanHorizontallyScroll { get; set; }

        public bool CanVerticallyScroll { get; set; }

        public void SetHorizontalOffset(float offset)
        {
            HorizontalOffset = offset;
        }

        public void SetVerticalOffset(float offset)
        {
            VerticalOffset = offset;
        }
    }

    private sealed class CountingEnumerable<T>(IEnumerable<T> items) : IEnumerable<T>
    {
        public int EnumerationCount { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            EnumerationCount++;
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
