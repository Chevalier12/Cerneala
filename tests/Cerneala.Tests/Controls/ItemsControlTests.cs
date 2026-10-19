using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Data;
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VirtualizingStackPanelSetAfterFirstLayoutRealizesOnlyTheViewport(bool itemsBeforeFirstLayout)
    {
        string[] items = Enumerable.Range(0, 2000).Select(index => index.ToString()).ToArray();
        int measures = 0;
        ItemsControl control = new()
        {
            ItemTemplate = new ContentTemplate<string>(
                "row",
                key: null,
                priority: 0,
                _ => new CountingElement(new LayoutSize(80, 10), () => measures++))
        };
        if (itemsBeforeFirstLayout)
        {
            control.ItemsSource = items;
        }

        ScrollViewer viewer = new()
        {
            Content = control,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible
        };
        UIRoot root = new(100, 30);
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();

        measures = 0;
        control.ItemsPanel = new VirtualizingStackPanel();
        control.ItemsSource = items;
        root.ProcessFrame();

        Assert.IsType<VirtualizingStackPanel>(control.ItemsPresenter.LayoutPanelRoot);
        Assert.InRange(measures, 1, 20);
        Assert.InRange(control.ItemContainerGenerator.RealizedContainers.Count, 1, 20);
        Assert.Contains(0, control.ItemContainerGenerator.RealizedContainers.Keys);

        root.ProcessFrame();
        root.ProcessFrame();

        Assert.InRange(measures, 1, 20);
        Assert.InRange(control.ItemContainerGenerator.RealizedContainers.Count, 1, 20);
        Assert.Contains(0, control.ItemContainerGenerator.RealizedContainers.Keys);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void VirtualizingStackPanelSetAfterScrollViewerStopsDrivingTheItemsRealizesEveryItem(bool moveOutOfScrollViewer)
    {
        string[] items = Enumerable.Range(0, 2000).Select(index => index.ToString()).ToArray();
        ItemsControl control = new()
        {
            ItemTemplate = CreateRowTemplate(),
            ItemsSource = items
        };
        ScrollViewer viewer = new()
        {
            Content = control,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible
        };
        UIRoot root = new(100, 200);
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();

        if (moveOutOfScrollViewer)
        {
            viewer.Content = null;
            root.VisualChildren.Remove(viewer);
            root.VisualChildren.Add(control);
        }
        else
        {
            viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }

        root.ProcessFrame();
        Assert.Equal(items.Length, control.ItemContainerGenerator.RealizedContainers.Count);

        control.ItemsPanel = new VirtualizingStackPanel();
        root.ProcessFrame();
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.IsType<VirtualizingStackPanel>(control.ItemsPresenter.LayoutPanelRoot);
        Assert.Equal(items.Length, control.ItemContainerGenerator.RealizedContainers.Count);
    }

    [Fact]
    public void VirtualizingStackPanelSetAfterVirtualizationContextIsClearedRealizesTheScrolledViewport()
    {
        string[] items = Enumerable.Range(0, 2000).Select(index => index.ToString()).ToArray();
        ItemsControl control = new()
        {
            ItemTemplate = CreateRowTemplate(),
            ItemsSource = items
        };
        ScrollViewer viewer = new()
        {
            Content = control,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible
        };
        UIRoot root = new(100, 200);
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();

        control.ItemsPresenter.VirtualizationContext = new VirtualizationContext(items.Length, 20, 200, 0, 0);
        root.ProcessFrame();
        viewer.ScrollInfo.SetVerticalOffset(20000);
        root.ProcessFrame();
        control.ItemsPresenter.VirtualizationContext = null;
        control.ItemsPanel = new VirtualizingStackPanel();
        root.ProcessFrame();
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.Equal(20000, viewer.ScrollInfo.VerticalOffset);
        Assert.IsType<VirtualizingStackPanel>(control.ItemsPresenter.LayoutPanelRoot);
        Assert.Contains(1000, control.ItemContainerGenerator.RealizedContainers.Keys);
        Assert.InRange(control.ItemContainerGenerator.RealizedContainers.Count, 1, 20);
    }

    [Fact]
    public void VirtualizingStackPanelSetAfterItemsPresenterIsActivatedAgainRealizesTheScrolledItems()
    {
        string[] items = Enumerable.Range(0, 2000).Select(index => index.ToString()).ToArray();
        PresenterSwappingItemsControl control = new()
        {
            ItemTemplate = CreateRowTemplate(),
            ItemsSource = items
        };
        ScrollViewer viewer = new()
        {
            Content = control,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible
        };
        UIRoot root = new(100, 200);
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();

        control.UsePresenter(new ItemsPresenter());
        root.ProcessFrame();
        viewer.ScrollInfo.SetVerticalOffset(20000);
        root.ProcessFrame();
        control.UsePresenter(null);
        root.ProcessFrame();
        control.ItemsPanel = new VirtualizingStackPanel();
        root.ProcessFrame();
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.Equal(20000, viewer.ScrollInfo.VerticalOffset);
        Assert.IsType<VirtualizingStackPanel>(control.ItemsPresenter.LayoutPanelRoot);
        Assert.Contains(1000, control.ItemContainerGenerator.RealizedContainers.Keys);
    }

    [Fact]
    public void ScrollViewerVirtualizationRealizesItemsAddedToInitiallyEmptySource()
    {
        ObservableCollection<FixedElement> source = [];
        ItemsControl control = new()
        {
            ItemsSource = source,
            ItemsPanel = new VirtualizingStackPanel()
        };
        ScrollViewer viewer = new()
        {
            Content = control,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();
        VirtualizingStackPanel panel = Assert.IsType<VirtualizingStackPanel>(control.ItemsPresenter.LayoutPanelRoot);
        Assert.Empty(panel.VisualChildren);

        source.Insert(0, new FixedElement("added 1", new LayoutSize(80, 10)));
        root.ProcessFrame();
        source.Insert(0, new FixedElement("added 2", new LayoutSize(80, 20)));
        root.ProcessFrame();

        Assert.Equal(2, panel.VisualChildren.Count);
        Assert.Equal("added 2", Assert.IsType<FixedElement>(panel.VisualChildren[0]).Value);
        Assert.Equal("added 1", Assert.IsType<FixedElement>(panel.VisualChildren[1]).Value);
        Assert.Equal(30, panel.DesiredSize.Height);

        source.Clear();
        root.ProcessFrame();

        Assert.Empty(panel.VisualChildren);
        Assert.Equal(0, panel.DesiredSize.Height);

        source.Add(new FixedElement("added again", new LayoutSize(80, 10)));
        root.ProcessFrame();

        Assert.Equal("added again", Assert.IsType<FixedElement>(Assert.Single(panel.VisualChildren)).Value);
        Assert.Equal(10, panel.DesiredSize.Height);
    }

    [Fact]
    public void ScrollViewerVirtualizationRealizesAppendedItemsOnlyInsideViewport()
    {
        ObservableCollection<FixedElement> source = [new FixedElement("0", new LayoutSize(80, 10))];
        ItemsControl control = new()
        {
            ItemsSource = source,
            ItemsPanel = new VirtualizingStackPanel()
        };
        ScrollViewer viewer = new()
        {
            Content = control,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        UIRoot root = new(100, 30);
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();
        Assert.Equal([0], control.ItemContainerGenerator.RealizedContainers.Keys.Order());

        source.Add(new FixedElement("1", new LayoutSize(80, 10)));
        root.ProcessFrame();

        Assert.Equal([0, 1], control.ItemContainerGenerator.RealizedContainers.Keys.Order());

        for (int index = 2; index < 10; index++)
        {
            source.Add(new FixedElement(index.ToString(), new LayoutSize(80, 10)));
        }

        root.ProcessFrame();
        int[] realizedBeforeAppend = [.. control.ItemContainerGenerator.RealizedContainers.Keys.Order()];
        Assert.DoesNotContain(9, realizedBeforeAppend);

        source.Add(new FixedElement("10", new LayoutSize(80, 10)));
        root.ProcessFrame();

        Assert.Equal(realizedBeforeAppend, control.ItemContainerGenerator.RealizedContainers.Keys.Order());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ScrollViewerVirtualizationRealizesVisibleRowsAfterScrolledSourceShrinks(bool replaceItemsSource)
    {
        ObservableCollection<FixedElement> source =
            [.. Enumerable.Range(0, 100).Select(index => new FixedElement(index.ToString(), new LayoutSize(80, 28)))];
        ItemsControl control = new()
        {
            ItemsSource = source,
            ItemsPanel = new VirtualizingStackPanel()
        };
        ScrollViewer viewer = new()
        {
            Content = control,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();
        viewer.ScrollInfo.SetVerticalOffset(2500);
        root.ProcessFrame();
        Assert.Contains(90, control.ItemContainerGenerator.RealizedContainers.Keys);

        if (replaceItemsSource)
        {
            control.ItemsSource = source.Take(5).ToArray();
        }
        else
        {
            while (source.Count > 5)
            {
                source.RemoveAt(source.Count - 1);
            }
        }

        root.ProcessFrame();
        root.ProcessFrame();

        Assert.Equal(40, viewer.ScrollInfo.VerticalOffset);
        Assert.Superset(new HashSet<int> { 1, 2, 3, 4 }, control.ItemContainerGenerator.RealizedContainers.Keys.ToHashSet());
        RealizationWindow shrunkWindow = control.ItemsPresenter.CurrentRealizationWindow;

        viewer.ScrollInfo.SetVerticalOffset(40);
        root.ProcessFrame();

        Assert.Equal(control.ItemsPresenter.CurrentRealizationWindow, shrunkWindow);
    }

    [Theory]
    [InlineData(2500, 40)]
    [InlineData(0, 0)]
    [InlineData(40, 40)]
    public void ScrollViewerVirtualizationArrangesUnmeasuredRowsAfterScrolledSourceReplacement(float scrollTo, float expectedOffset)
    {
        ObservableCollection<FixedElement> source =
            [.. Enumerable.Range(0, 100).Select(index => new FixedElement(index.ToString(), new LayoutSize(80, 28)))];
        ItemsControl control = new()
        {
            ItemsSource = source,
            ItemsPanel = new VirtualizingStackPanel()
        };
        ScrollViewer viewer = new()
        {
            Content = control,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();
        viewer.ScrollInfo.SetVerticalOffset(scrollTo);
        root.ProcessFrame();

        FixedElement[] rows = [.. Enumerable.Range(0, 5).Select(index => new FixedElement($"new {index}", new LayoutSize(80, 28)))];
        control.ItemsSource = rows;
        for (int frame = 0; frame < 20 && root.Scheduler.HasWork; frame++)
        {
            root.ProcessFrame();
        }

        Assert.False(root.Scheduler.HasWork);
        Assert.Equal(expectedOffset, viewer.ScrollInfo.VerticalOffset);
        Assert.Equal([28f, 28f, 28f, 28f, 28f], rows.Select(row => row.DesiredSize.Height));
        Assert.Equal([28f, 28f, 28f, 28f, 28f], rows.Select(row => row.ArrangedBounds.Height));
        Assert.Equal(
            Enumerable.Range(0, 5).Select(index => index * 28f - expectedOffset),
            rows.Select(row => row.ArrangedBounds.Y));
    }

    [Fact]
    public void RemovingMeasuredRowBeforeViewportDropsItsHeight()
    {
        ObservableCollection<FixedElement> source =
            [.. Enumerable.Range(0, 40).Select(index => new FixedElement(index.ToString(), new LayoutSize(80, index == 0 ? 200 : 10)))];
        VirtualizingStackPanel panel = new();
        ItemsControl control = new()
        {
            ItemsSource = source,
            ItemsPanel = panel
        };
        ScrollViewer viewer = new()
        {
            Content = control,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();
        viewer.ScrollInfo.SetVerticalOffset(200);
        root.ProcessFrame();
        viewer.ScrollInfo.SetVerticalOffset(300);
        root.ProcessFrame();

        source.RemoveAt(0);
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.Equal(390, panel.DesiredSize.Height);
    }

    [Theory]
    [InlineData("ObservableCollection")]
    [InlineData("ObservableList")]
    [InlineData("Items")]
    public void InsertingRowBeforeMeasuredRowsMovesTheirHeights(string sourceKind)
    {
        FixedElement[] rows =
            [.. Enumerable.Range(0, 40).Select(index => new FixedElement(index.ToString(), new LayoutSize(80, index == 39 ? 200 : 10)))];
        ObservableCollection<FixedElement> observableCollection = [.. rows];
        ObservableList<FixedElement> observableList = new(rows);
        VirtualizingStackPanel panel = new();
        ItemsControl control = new() { ItemsPanel = panel };
        switch (sourceKind)
        {
            case "ObservableCollection":
                control.ItemsSource = observableCollection;
                break;
            case "ObservableList":
                control.ItemsSource = observableList;
                break;
            default:
                control.SetItems(rows);
                break;
        }

        ScrollViewer viewer = new()
        {
            Content = control,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(viewer);
        MeasureEveryRowAndReturnToTop(root, viewer);
        Assert.Equal(590, panel.DesiredSize.Height);

        FixedElement inserted = new("inserted", new LayoutSize(80, 10));
        switch (sourceKind)
        {
            case "ObservableCollection":
                observableCollection.Insert(0, inserted);
                break;
            case "ObservableList":
                observableList.Insert(0, inserted);
                break;
            default:
                control.Items.Insert(0, inserted);
                break;
        }

        root.ProcessFrame();
        root.ProcessFrame();

        Assert.Equal(600, panel.DesiredSize.Height);
    }

    [Fact]
    public void MovingMeasuredRowMovesItsHeight()
    {
        ObservableCollection<FixedElement> source =
            [.. Enumerable.Range(0, 40).Select(index => new FixedElement(index.ToString(), new LayoutSize(80, index == 39 ? 200 : 10)))];
        VirtualizingStackPanel panel = new();
        ItemsControl control = new()
        {
            ItemsSource = source,
            ItemsPanel = panel
        };
        ScrollViewer viewer = new()
        {
            Content = control,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(viewer);
        MeasureEveryRowAndReturnToTop(root, viewer);

        source.Move(39, 20);
        root.ProcessFrame();
        viewer.ScrollInfo.SetVerticalOffset(490);
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.DoesNotContain(20, control.ItemContainerGenerator.RealizedContainers.Keys);
        Assert.Equal(590, panel.DesiredSize.Height);
    }

    [Fact]
    public void ReplacingMeasuredRowDropsItsHeight()
    {
        FixedElement[] rows =
            [.. Enumerable.Range(0, 40).Select(index => new FixedElement(index.ToString(), new LayoutSize(80, index == 39 ? 200 : 10)))];
        VirtualizingStackPanel panel = new();
        ItemsControl control = new() { ItemsPanel = panel };
        control.SetItems(rows);
        ScrollViewer viewer = new()
        {
            Content = control,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(viewer);
        MeasureEveryRowAndReturnToTop(root, viewer);

        control.Items[39] = new FixedElement("replacement", new LayoutSize(80, 10));
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.Equal(400, panel.DesiredSize.Height);
    }

    [Fact]
    public void PanelSwappedBackAfterRemoveDoesNotKeepRemovedRowHeight()
    {
        ObservableCollection<FixedElement> source =
            [.. Enumerable.Range(0, 40).Select(index => new FixedElement(index.ToString(), new LayoutSize(80, index == 0 ? 200 : 10)))];
        VirtualizingStackPanel panelA = new();
        (ItemsControl control, UIRoot root) = CreateScrolledPastTallFirstRow(source, panelA);

        control.ItemsPanel = new VirtualizingStackPanel();
        root.ProcessFrame();
        source.RemoveAt(0);
        root.ProcessFrame();
        control.ItemsPanel = panelA;
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.Equal(390, panelA.TotalExtent);
        Assert.Contains(38, control.ItemContainerGenerator.RealizedContainers.Keys);
    }

    [Fact]
    public void PanelSwappedBackAfterInsertDoesNotGiveInsertedRowPushedRowHeight()
    {
        ObservableCollection<FixedElement> source =
            [.. Enumerable.Range(0, 40).Select(index => new FixedElement(index.ToString(), new LayoutSize(80, index == 0 ? 200 : 10)))];
        VirtualizingStackPanel panelA = new();
        (ItemsControl control, UIRoot root) = CreateScrolledPastTallFirstRow(source, panelA);

        control.ItemsPanel = new VirtualizingStackPanel();
        root.ProcessFrame();
        source.Insert(0, new FixedElement("inserted", new LayoutSize(80, 10)));
        root.ProcessFrame();
        control.ItemsPanel = panelA;
        root.ProcessFrame();
        root.ProcessFrame();

        // Removing the 200 px row leaves 40 rows of 10 px; the inserted row must not keep its height.
        source.RemoveAt(1);
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.Equal(400, panelA.TotalExtent);
    }

    private static (ItemsControl Control, UIRoot Root) CreateScrolledPastTallFirstRow(
        ObservableCollection<FixedElement> source,
        VirtualizingStackPanel panel)
    {
        ItemsControl control = new()
        {
            ItemsSource = source,
            ItemsPanel = panel
        };
        ScrollViewer viewer = new()
        {
            Content = control,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();
        viewer.ScrollInfo.SetVerticalOffset(200);
        root.ProcessFrame();
        viewer.ScrollInfo.SetVerticalOffset(300);
        root.ProcessFrame();
        return (control, root);
    }

    private static void MeasureEveryRowAndReturnToTop(UIRoot root, ScrollViewer viewer)
    {
        root.ProcessFrame();
        for (float offset = 50; offset <= 600; offset += 50)
        {
            viewer.ScrollInfo.SetVerticalOffset(offset);
            root.ProcessFrame();
        }

        viewer.ScrollInfo.SetVerticalOffset(0);
        root.ProcessFrame();
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
        Assert.Equal(UiPropertyValueSource.TemplateBinding, generated.GetValueSource(UIElement.AspectProperty));

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
            UiPropertyValueSource.TemplateBinding));
    }

    // As in WPF's ContentPresenter.PrepareContentPresenter, preparing a generated ContentPresenter sets ContentTemplate
    // only when ItemTemplate is set, so a ContentTemplate from ItemContainerAspect applies otherwise.
    [Fact]
    public void ItemContainerAspectContentTemplateBuildsGeneratedContainersWithoutItemTemplate()
    {
        UIRoot root = new(300, 200);
        int calls = 0;
        ContentTemplate<string> template = new("aspect-content", key: null, priority: 0, context =>
        {
            calls++;
            return new FixedElement(context.Data!, new LayoutSize(20, 10));
        });
        ItemsControl control = new()
        {
            ItemsSource = new[] { "A", "B" },
            ItemContainerAspect = new ElementAspect([new ElementAspectValue(ContentPresenter.ContentTemplateProperty, template)])
        };
        root.VisualChildren.Add(control);
        root.ProcessFrame();

        Assert.Equal(2, calls);
        IReadOnlyDictionary<int, UIElement> realized = control.ItemContainerGenerator.RealizedContainers;
        foreach (ContentPresenter presenter in new[] { (ContentPresenter)realized[0], (ContentPresenter)realized[1] })
        {
            Assert.Same(template, presenter.ContentTemplate);
            Assert.Null(presenter.ContentTemplateKey);
            Assert.Equal(presenter.Content, Assert.IsType<FixedElement>(presenter.PresentedChild).Value);
        }
    }

    [Fact]
    public void ItemContainerAspectContentTemplateKeyBuildsGeneratedContainersWithoutItemTemplateKey()
    {
        UIRoot root = new(300, 200);
        List<string?> built = [];
        ContentTemplate<string> keyed = new("keyed", key: "k", priority: 0, context =>
        {
            built.Add(context.Data);
            return new FixedElement(context.Data!, new LayoutSize(40, 20));
        });
        ItemsControl control = new()
        {
            ItemsSource = new[] { "One", "Two" },
            ItemContainerAspect = new ElementAspect([new ElementAspectValue(ContentPresenter.ContentTemplateKeyProperty, "k")])
        };
        control.Templates.Add(keyed);
        root.VisualChildren.Add(control);
        root.ProcessFrame();

        Assert.Equal(["One", "Two"], built);
        IReadOnlyDictionary<int, UIElement> realized = control.ItemContainerGenerator.RealizedContainers;
        foreach (ContentPresenter presenter in new[] { (ContentPresenter)realized[0], (ContentPresenter)realized[1] })
        {
            Assert.Equal("k", presenter.ContentTemplateKey);
            Assert.Null(presenter.ContentTemplate);
        }
    }

    [Fact]
    public void ItemTemplateWinsOverItemContainerAspectContentTemplate()
    {
        UIRoot root = new(300, 200);
        int itemCalls = 0;
        int aspectCalls = 0;
        ContentTemplate<string> itemTemplate = new("item", key: null, priority: 0, context =>
        {
            itemCalls++;
            return new FixedElement(context.Data!, new LayoutSize(20, 10));
        });
        ContentTemplate<string> aspectTemplate = new("aspect-content", key: null, priority: 0, context =>
        {
            aspectCalls++;
            return new FixedElement(context.Data!, new LayoutSize(20, 10));
        });
        ItemsControl control = new()
        {
            ItemTemplate = itemTemplate,
            ItemsSource = new[] { "A", "B" },
            ItemContainerAspect = new ElementAspect([new ElementAspectValue(ContentPresenter.ContentTemplateProperty, aspectTemplate)])
        };
        root.VisualChildren.Add(control);
        root.ProcessFrame();

        Assert.Equal((2, 0), (itemCalls, aspectCalls));
        Assert.Same(itemTemplate, ((ContentPresenter)control.ItemContainerGenerator.RealizedContainers[0]).ContentTemplate);
    }

    // Clearing ItemTemplate clears and recycles the generated presenters; the recycled presenters are prepared again
    // without an ItemTemplate, so the value the previous preparation set is gone and the aspect's ContentTemplate applies.
    [Fact]
    public void RecycledGeneratedContainerUsesItemContainerAspectContentTemplateAfterItemTemplateIsCleared()
    {
        UIRoot root = new(300, 200);
        int aspectCalls = 0;
        ContentTemplate<string> itemTemplate = new("item", key: null, priority: 0, context => new FixedElement(context.Data!, new LayoutSize(20, 10)));
        ContentTemplate<string> aspectTemplate = new("aspect-content", key: null, priority: 0, context =>
        {
            aspectCalls++;
            return new FixedElement(context.Data!, new LayoutSize(20, 10));
        });
        ItemsControl control = new()
        {
            ItemTemplate = itemTemplate,
            ItemsSource = new[] { "A", "B" },
            ItemContainerAspect = new ElementAspect([new ElementAspectValue(ContentPresenter.ContentTemplateProperty, aspectTemplate)])
        };
        root.VisualChildren.Add(control);
        root.ProcessFrame();
        UIElement[] before = [control.ItemContainerGenerator.RealizedContainers[0], control.ItemContainerGenerator.RealizedContainers[1]];

        control.ItemTemplate = null;
        root.ProcessFrame();

        IReadOnlyDictionary<int, UIElement> realized = control.ItemContainerGenerator.RealizedContainers;
        UIElement[] after = [realized[0], realized[1]];
        Assert.All(after, presenter => Assert.Contains(presenter, before));
        Assert.All(after, presenter => Assert.Same(aspectTemplate, ((ContentPresenter)presenter).ContentTemplate));
        Assert.Equal(2, aspectCalls);
    }

    [Fact]
    public void TemplatedButtonMeasuresWithPaddingFromItsAspectInTheFirstFrame()
    {
        Thickness padding = new(16, 9, 16, 9);
        ElementAspect padded = new([new ElementAspectValue(Control.PaddingProperty, padding)]);
        (UIRoot aspectRoot, ItemsControl aspectItems) = RootWithTemplatedButton(() => new Button
        {
            Aspect = padded,
            Content = new FixedElement("aspect", new LayoutSize(40, 10))
        });
        (UIRoot localRoot, ItemsControl localItems) = RootWithTemplatedButton(() => new Button
        {
            Padding = padding,
            Content = new FixedElement("local", new LayoutSize(40, 10))
        });
        Button directButton = new()
        {
            Aspect = padded,
            Content = new FixedElement("direct", new LayoutSize(40, 10))
        };
        UIRoot directRoot = new(300, 200);
        directRoot.VisualChildren.Add(directButton);

        aspectRoot.ProcessFrame();
        localRoot.ProcessFrame();
        directRoot.ProcessFrame();

        Button aspectButton = PresentedButton(aspectItems);
        Button localButton = PresentedButton(localItems);
        Assert.Equal(padding, aspectButton.Padding);
        Assert.Equal(UiPropertyValueSource.AspectBase, aspectButton.GetValueSource(Control.PaddingProperty));
        Assert.Equal(localButton.DesiredSize, aspectButton.DesiredSize);
        Assert.Equal(directButton.DesiredSize, aspectButton.DesiredSize);
        Assert.Equal(localButton.ArrangedBounds.Size, aspectButton.ArrangedBounds.Size);
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
    public void ClearingVirtualizationContextRealizesEveryItemInVirtualizingStackPanel()
    {
        VirtualizingStackPanel panel = new();
        ItemsControl control = new()
        {
            ItemsPanel = panel
        };
        control.SetItems(Enumerable.Range(0, 100).Cast<object>());
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 0, CacheItems: 1));
        MeasureContext context = new(new LayoutSize(100, 30));
        control.Measure(context);
        Assert.Equal(4, panel.VisualChildren.Count);

        control.SetVirtualizationContext(null);
        control.Measure(context);

        Assert.Null(panel.VirtualizationContext);
        Assert.Equal(100, panel.VisualChildren.Count);
    }

    [Fact]
    public void ReplacedVirtualizingStackPanelDoesNotKeepPresenterVirtualizationContext()
    {
        VirtualizingStackPanel firstPanel = new();
        ItemsControl control = new()
        {
            ItemsPanel = firstPanel
        };
        control.SetItems(Enumerable.Range(0, 100).Cast<object>());
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 0));
        MeasureContext context = new(new LayoutSize(100, 30));
        control.Measure(context);

        control.ItemsPanel = new VirtualizingStackPanel();
        control.Measure(context);
        Assert.Null(firstPanel.VirtualizationContext);

        control.SetVirtualizationContext(null);
        control.ItemsPanel = firstPanel;
        control.Measure(context);

        Assert.Same(firstPanel, control.ItemsPresenter.LayoutPanelRoot);
        Assert.Equal(100, firstPanel.VisualChildren.Count);
    }

    [Fact]
    public void VirtualizingStackPanelReplacedByStackPanelDoesNotKeepPresenterVirtualizationContext()
    {
        VirtualizingStackPanel oldPanel = new();
        ItemsControl control = new()
        {
            ItemsPanel = oldPanel
        };
        control.SetItems(Enumerable.Range(0, 100).Cast<object>());
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 0, CacheItems: 1));
        MeasureContext context = new(new LayoutSize(100, 30));
        control.Measure(context);
        Assert.Equal(4, oldPanel.VisualChildren.Count);

        control.ItemsPanel = new Cerneala.UI.Layout.Panels.StackPanel();
        control.Measure(context);
        Assert.Null(oldPanel.VirtualizationContext);

        ItemsControl reuse = new()
        {
            ItemsPanel = oldPanel
        };
        reuse.SetItems(Enumerable.Range(0, 100).Cast<object>());
        reuse.Measure(new MeasureContext(new LayoutSize(100, 2000)));

        Assert.Same(oldPanel, reuse.ItemsPresenter.LayoutPanelRoot);
        Assert.Equal(100, oldPanel.VisualChildren.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FailedPanelSwapBeforeContainersAreAddedKeepsVirtualizationContextOnOldPanel(bool replacementVirtualizes)
    {
        VirtualizingStackPanel oldPanel = new();
        Cerneala.UI.Layout.Panels.Panel replacement = replacementVirtualizes
            ? new VirtualizingStackPanel()
            : new Cerneala.UI.Layout.Panels.StackPanel();
        FailingItemsControl control = new()
        {
            ItemsPanel = oldPanel
        };
        control.SetItems(Enumerable.Range(0, 100).Cast<object>());
        VirtualizationContext supplied = new(100, 10, 30, 0, CacheItems: 1);
        control.SetVirtualizationContext(supplied);
        control.Measure(new MeasureContext(new LayoutSize(100, 30)));
        Assert.Equal(4, oldPanel.VisualChildren.Count);

        control.Fail = true;
        control.ItemsPanel = replacement;
        Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 30))));

        Assert.Same(oldPanel, control.ItemsPresenter.LayoutPanelRoot);
        Assert.Equal(supplied, oldPanel.VirtualizationContext);
        Assert.Null((replacement as VirtualizingStackPanel)?.VirtualizationContext);

        // The failed refresh stays dirty, so the next measure retries it and the same cause throws again.
        Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 31))));
        Assert.Same(oldPanel, control.ItemsPresenter.LayoutPanelRoot);
        Assert.Equal(supplied, oldPanel.VirtualizationContext);
        oldPanel.Measure(new MeasureContext(new LayoutSize(100, 31)));
        Assert.Equal(1000, oldPanel.DesiredSize.Height);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FailedPanelSwapWhileAddingContainersKeepsVirtualizationContextOnOldPanel(bool replacementVirtualizes)
    {
        VirtualizingStackPanel oldPanel = new();
        Cerneala.UI.Layout.Panels.Panel replacement = replacementVirtualizes
            ? new VirtualizingStackPanel()
            : new Cerneala.UI.Layout.Panels.StackPanel();
        FixedElement[] items = Enumerable.Range(0, 100)
            .Select(index => new FixedElement(index.ToString(), new LayoutSize(100, 10)))
            .ToArray();
        ItemsControl control = new()
        {
            ItemsPanel = oldPanel
        };
        control.SetItems(items);
        VirtualizationContext supplied = new(100, 10, 30, 0, CacheItems: 1);
        control.SetVirtualizationContext(supplied);
        control.Measure(new MeasureContext(new LayoutSize(100, 30)));
        new Cerneala.UI.Layout.Panels.StackPanel().VisualChildren.Add(items[10]);
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 70, CacheItems: 1));

        control.ItemsPanel = replacement;
        Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 30))));

        Assert.Same(oldPanel, control.ItemsPresenter.LayoutPanelRoot);
        Assert.Equal(supplied, oldPanel.VirtualizationContext);
        Assert.Equal(0, oldPanel.FirstRealizedIndex);
        if (replacement is VirtualizingStackPanel virtualizingReplacement)
        {
            Assert.Null(virtualizingReplacement.VirtualizationContext);
            Assert.Equal(0, virtualizingReplacement.FirstRealizedIndex);
        }
    }

    [Fact]
    public void FailedInsertWhileWindowMovesOnRetainedPanelKeepsPreviousContainersAndIndex()
    {
        VirtualizingStackPanel panel = new();
        FixedElement[] items = Enumerable.Range(0, 100)
            .Select(index => new FixedElement(index.ToString(), new LayoutSize(100, 10)))
            .ToArray();
        ItemsControl control = new()
        {
            ItemsPanel = panel
        };
        control.SetItems(items);
        VirtualizationContext supplied = new(100, 10, 30, 0, CacheItems: 1);
        control.SetVirtualizationContext(supplied);
        control.Measure(new MeasureContext(new LayoutSize(100, 30)));
        Cerneala.UI.Layout.Panels.StackPanel otherParent = new();
        otherParent.VisualChildren.Add(items[5]);
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 20, CacheItems: 1));
        control.ItemsPresenter.MarkItemsDirty();

        Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 31))));

        Assert.Same(panel, control.ItemsPresenter.LayoutPanelRoot);
        Assert.Equal(["0", "1", "2", "3"], panel.VisualChildren.Cast<FixedElement>().Select(item => item.Value));
        Assert.Equal(supplied, panel.VirtualizationContext);
        Assert.Equal(0, panel.FirstRealizedIndex);
        Assert.Null(items[4].VisualParent);
        panel.Measure(new MeasureContext(new LayoutSize(100, 30)));
        panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 30)));
        Assert.Equal(new LayoutRect(0, 10, 100, 10), items[1].ArrangedBounds);

        Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 32))));
        Assert.Equal(["0", "1", "2", "3"], panel.VisualChildren.Cast<FixedElement>().Select(item => item.Value));

        otherParent.VisualChildren.Remove(items[5]);
        control.Measure(new MeasureContext(new LayoutSize(100, 33)));

        Assert.Equal(["1", "2", "3", "4", "5"], panel.VisualChildren.Cast<FixedElement>().Select(item => item.Value));
        Assert.Equal(1, panel.FirstRealizedIndex);
    }

    [Fact]
    public void FailedInsertWhileWindowMovesOnRetainedPanelKeepsGeneratedContainersOnPreviousItems()
    {
        VirtualizingStackPanel panel = new();
        FixedElement element5 = new("element5", new LayoutSize(100, 10));
        object[] items = Enumerable.Range(0, 100)
            .Select(index => index == 5 ? (object)element5 : index.ToString())
            .ToArray();
        ItemsControl control = new()
        {
            ItemsPanel = panel
        };
        control.SetItems(items);
        VirtualizationContext supplied = new(100, 10, 30, 0, CacheItems: 1);
        control.SetVirtualizationContext(supplied);
        control.Measure(new MeasureContext(new LayoutSize(100, 30)));
        Cerneala.UI.Layout.Panels.StackPanel otherParent = new();
        otherParent.VisualChildren.Add(element5);
        control.SetVirtualizationContext(new VirtualizationContext(100, 10, 30, 20, CacheItems: 1));

        Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 31))));

        // The generator describes the window the panel still presents, not the window that failed.
        Assert.Equal(-1, ItemContainerGenerator.GetItemIndex(element5));
        Assert.Equal(4, control.RealizedItemCount);
        AssertPresentsItems(control, panel, ["0", "1", "2", "3"]);
        Assert.Equal(supplied, panel.VirtualizationContext);
        Assert.Equal(0, panel.FirstRealizedIndex);
        Assert.Same(otherParent, element5.VisualParent);

        Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 32))));
        Assert.Equal(-1, ItemContainerGenerator.GetItemIndex(element5));
        Assert.Equal(4, control.RealizedItemCount);
        AssertPresentsItems(control, panel, ["0", "1", "2", "3"]);
        Assert.Same(otherParent, element5.VisualParent);

        otherParent.VisualChildren.Remove(element5);
        control.Measure(new MeasureContext(new LayoutSize(100, 33)));

        Assert.Equal(
            ["1", "2", "3", "4"],
            panel.VisualChildren.Take(4).Cast<ContentPresenter>().Select(container => container.Content));
        Assert.Same(element5, panel.VisualChildren[4]);
        Assert.Equal(1, panel.FirstRealizedIndex);
        Assert.Equal(5, ItemContainerGenerator.GetItemIndex(element5));
        Assert.Equal(5, control.RealizedItemCount);
        Assert.Equal([1, 2, 3, 4, 5], control.ItemContainerGenerator.RealizedContainers.Keys.Order());
        Assert.All(
            panel.VisualChildren,
            child => Assert.Same(
                child,
                control.ItemContainerGenerator.RealizedContainers[ItemContainerGenerator.GetItemIndex(child)]));

        static void AssertPresentsItems(ItemsControl control, VirtualizingStackPanel panel, string[] expected)
        {
            ContentPresenter[] containers = [.. panel.VisualChildren.Cast<ContentPresenter>()];
            Assert.Equal(expected, containers.Select(container => container.Content));
            Assert.Equal(
                Enumerable.Range(0, expected.Length),
                containers.Select(Cerneala.UI.Controls.Items.ItemContainerGenerator.GetItemIndex));
            Assert.Equal(
                Enumerable.Range(0, expected.Length),
                control.ItemContainerGenerator.RealizedContainers.Keys.Order());
            Assert.All(
                containers,
                container => Assert.Same(
                    container,
                    control.ItemContainerGenerator.RealizedContainers[
                        Cerneala.UI.Controls.Items.ItemContainerGenerator.GetItemIndex(container)]));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedInsertOfElementWithAnotherParentDoesNotKeepItRealized(bool swapsPanel)
    {
        // The default panel is retained by every refresh, and a changed ItemsPanel is swapped in.
        ItemsControl control = new();
        control.Items.Add("a");
        control.Items.Add("b");
        control.Measure(new MeasureContext(new LayoutSize(100, 100)));
        Cerneala.UI.Layout.Panels.Panel previousPanel = control.ItemsPresenter.LayoutPanelRoot!;
        Cerneala.UI.Layout.Panels.StackPanel otherParent = new();
        FixedElement element = new("x", new LayoutSize(100, 10));
        otherParent.VisualChildren.Add(element);
        UIElement? logicalParent = element.LogicalParent;
        control.Items.Add(element);
        if (swapsPanel)
        {
            control.ItemsPanel = new Cerneala.UI.Layout.Panels.StackPanel();
        }

        Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 101))));

        AssertPresentsOnlyPreviousItems();
        Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 102))));
        AssertPresentsOnlyPreviousItems();

        control.Items.RemoveAt(2);

        Assert.Same(otherParent, element.VisualParent);
        Assert.Same(logicalParent, element.LogicalParent);
        Assert.Same(element, Assert.Single(otherParent.VisualChildren));

        control.Items.Add(element);
        otherParent.VisualChildren.Remove(element);
        control.Measure(new MeasureContext(new LayoutSize(100, 103)));

        UIElement[] children = [.. control.ItemsPresenter.LayoutPanelRoot!.VisualChildren];
        Assert.Equal(["a", "b"], children.Take(2).Cast<ContentPresenter>().Select(container => container.Content));
        Assert.Same(element, children[2]);
        Assert.Equal(2, ItemContainerGenerator.GetItemIndex(element));
        Assert.Equal(3, control.RealizedItemCount);

        // The panel that stays presents the window it presented, realized again for the current items, without the
        // element it could not take. The ItemsPanel change cleared the containers, so a swap realizes them again too.
        void AssertPresentsOnlyPreviousItems()
        {
            Assert.Same(previousPanel, control.ItemsPresenter.LayoutPanelRoot);
            UIElement[] presented = [.. previousPanel.VisualChildren];
            Assert.Equal(["a", "b"], presented.Cast<ContentPresenter>().Select(container => container.Content));
            Assert.Equal(2, control.RealizedItemCount);
            Assert.DoesNotContain(element, control.ItemContainerGenerator.RealizedContainers.Values);
            Assert.Equal(-1, ItemContainerGenerator.GetItemIndex(element));
            Assert.Same(otherParent, element.VisualParent);
            Assert.Same(logicalParent, element.LogicalParent);
        }
    }

    [Fact]
    public void FailedFirstPresentationOfElementWithAnotherParentDoesNotKeepItRealized()
    {
        ItemsControl control = new();
        Cerneala.UI.Layout.Panels.StackPanel otherParent = new();
        FixedElement element = new("x", new LayoutSize(100, 10));
        otherParent.VisualChildren.Add(element);
        control.Items.Add("a");
        control.Items.Add(element);

        Assert.Throws<InvalidOperationException>(() => control.Measure(new MeasureContext(new LayoutSize(100, 100))));

        Assert.Null(control.ItemsPresenter.LayoutPanelRoot);
        Assert.Equal(0, control.RealizedItemCount);
        Assert.Equal(-1, ItemContainerGenerator.GetItemIndex(element));

        control.Items.RemoveAt(1);

        Assert.Same(element, Assert.Single(otherParent.VisualChildren));
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
    public void VirtualizingStackPanelItemsPanelWithoutScrollViewerRealizesAndStacksEveryItem()
    {
        UIRoot root = new(200, 200);
        ItemsControl control = new()
        {
            ItemsPanel = new VirtualizingStackPanel()
        };
        control.SetItems(new[] { "a", "b", "c" });
        root.VisualChildren.Add(control);

        root.ProcessFrame();
        root.ProcessFrame();

        VirtualizingStackPanel panel = Assert.IsType<VirtualizingStackPanel>(control.ItemsPresenter.LayoutPanelRoot);
        Assert.Equal(3, panel.VisualChildren.Count);
        Assert.Equal(
            ["a", "b", "c"],
            panel.VisualChildren.Select(child => Assert.IsType<ContentPresenter>(child).Content));
        for (int index = 0; index < panel.VisualChildren.Count; index++)
        {
            LayoutRect bounds = panel.VisualChildren[index].ArrangedBounds;
            Assert.True(bounds.Height > 0);
            if (index > 0)
            {
                LayoutRect previous = panel.VisualChildren[index - 1].ArrangedBounds;
                Assert.Equal(previous.Y + previous.Height, bounds.Y);
            }
        }
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

    [Fact]
    public void RemovingItemTemplateHostsElementItemThatTheTemplateWrapped()
    {
        UIRoot root = new(200, 200);
        FixedElement item = new("item", new LayoutSize(40, 10));
        ItemsControl control = new() { ItemTemplate = WrapElementTemplate() };
        control.SetItems(new object[] { item });
        root.VisualChildren.Add(control);
        root.ProcessFrame();
        Border wrapper = Assert.IsType<Border>(item.VisualParent);

        control.ItemTemplate = null;
        root.ProcessFrame();

        Assert.Same(item, control.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Same(control.ItemsPresenter.LayoutPanelRoot, item.VisualParent);
        Assert.Empty(wrapper.VisualChildren);
    }

    [Fact]
    public void ReplacingItemTemplateWrapsElementItemInTheNewTemplate()
    {
        UIRoot root = new(200, 200);
        FixedElement item = new("item", new LayoutSize(40, 10));
        ItemsControl control = new() { ItemTemplate = WrapElementTemplate() };
        control.SetItems(new object[] { item });
        root.VisualChildren.Add(control);
        root.ProcessFrame();
        Border first = Assert.IsType<Border>(item.VisualParent);

        control.ItemTemplate = WrapElementTemplate();
        root.ProcessFrame();

        Border second = Assert.IsType<Border>(item.VisualParent);
        Assert.NotSame(first, second);
        Assert.Same(item, second.Child);
        Assert.Empty(first.VisualChildren);
        ContentPresenter container = Assert.IsType<ContentPresenter>(control.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Same(second, container.PresentedChild);
    }

    // Clearing a realized container is not presenting an item, so it runs no ItemTemplate; only the new items are
    // presented, on the next layout pass (#535).
    [Fact]
    public void ReplacingItemsSourceRunsTheItemTemplateOnlyForTheNewItems()
    {
        List<string?> calls = [];
        ItemsControl control = new()
        {
            ItemTemplate = new ContentTemplate<string>("item", key: null, priority: 0, context =>
            {
                calls.Add(context.Data);
                return new TextBlock { Text = context.Data ?? string.Empty };
            }),
            ItemsSource = new[] { "A", "B", "C" }
        };
        UIRoot root = new(300, 200);
        root.VisualChildren.Add(control);
        root.ProcessFrame();
        Assert.Equal(["A", "B", "C"], calls);

        calls.Clear();
        control.ItemsSource = new[] { "D" };
        Assert.Empty(calls);
        root.ProcessFrame();

        Assert.Equal(["D"], calls);
        ContentPresenter container = Assert.IsType<ContentPresenter>(control.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Equal("D", Assert.IsType<TextBlock>(container.PresentedChild).Text);
    }

    [Theory]
    [InlineData(nameof(ItemsControl.ItemsSource))]
    [InlineData(nameof(ItemsControl.ItemTemplate))]
    public void ReplacingItemsSourceOrItemTemplateDoesNotRunATemplateWithoutItsItem(string change)
    {
        ItemsControl control = new()
        {
            ItemTemplate = new ContentTemplate<TemplatedPerson>("person", key: null, priority: 0,
                context => new TextBlock { Text = context.Data!.Name }),
            ItemsSource = new[] { new TemplatedPerson("Ana"), new TemplatedPerson("Bob") }
        };
        UIRoot root = new(300, 200);
        root.VisualChildren.Add(control);
        root.ProcessFrame();

        if (change == nameof(ItemsControl.ItemsSource))
        {
            control.ItemsSource = new[] { new TemplatedPerson("Cleo") };
        }
        else
        {
            control.ItemTemplate = new ContentTemplate<TemplatedPerson>("other", key: null, priority: 0,
                context => new TextBlock { Text = "x" + context.Data!.Name });
        }

        root.ProcessFrame();

        string[] expected = change == nameof(ItemsControl.ItemsSource) ? ["Cleo"] : ["xAna", "xBob"];
        Assert.Equal(
            expected,
            control.ItemContainerGenerator.RealizedContainers.OrderBy(pair => pair.Key)
                .Select(pair => Assert.IsType<TextBlock>(Assert.IsType<ContentPresenter>(pair.Value).PresentedChild).Text));
    }

    private sealed record TemplatedPerson(string Name);

    private static ContentTemplate WrapElementTemplate()
    {
        return new ContentTemplate("wrap", typeof(object), key: null, priority: 0,
            context => new Border { Child = (UIElement?)context.Data });
    }

    private static (UIRoot Root, ItemsControl Items) RootWithTemplatedButton(Func<Button> createButton)
    {
        ItemsControl control = new()
        {
            ItemTemplate = new ContentTemplate<string>("button", key: null, priority: 0, _ => createButton())
        };
        control.SetItems(new[] { "card" });
        UIRoot root = new(300, 200);
        root.VisualChildren.Add(control);
        return (root, control);
    }

    private static Button PresentedButton(ItemsControl control)
    {
        ContentPresenter container = Assert.IsType<ContentPresenter>(
            control.ItemsPresenter.LayoutPanelRoot!.VisualChildren[0]);
        return Assert.IsType<Button>(container.PresentedChild);
    }

    private static ContentTemplate<string> CreateRowTemplate()
    {
        return new ContentTemplate<string>("row", key: null, priority: 0, _ => new FixedElement("row", new LayoutSize(80, 20)));
    }

    private sealed class PresenterSwappingItemsControl : ItemsControl
    {
        // A template part presenter is a child of the control before the control activates it.
        public void UsePresenter(ItemsPresenter? presenter)
        {
            if (presenter is not null)
            {
                LogicalChildren.Add(presenter);
                VisualChildren.Add(presenter);
            }

            ActivateItemsPresenter(presenter);
        }
    }

    private sealed class FixedElement(string value, LayoutSize size) : UIElement
    {
        public string Value { get; } = value;

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }

    private sealed class CountingElement(LayoutSize size, Action measured) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            measured();
            return size;
        }
    }

    private sealed class FailingItemsControl : ItemsControl
    {
        public bool Fail { get; set; }

        protected internal override void PrepareItemContainer(UIElement container, int index, object? item)
        {
            if (Fail)
            {
                throw new InvalidOperationException("container failed");
            }

            base.PrepareItemContainer(container, index, item);
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
