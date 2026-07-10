using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls.Templates;

public sealed class ItemsContentTemplateIntegrationTests
{
    [Fact]
    public void ItemsControlUsesRegistryTemplateWhenItemTemplateIsNull()
    {
        TestItemsControl items = new();
        items.ContentTemplateRegistry.Register(new ContentTemplate<string>("string", key: null, priority: 0, _ => new FixedElement()));
        ContentPresenter presenter = (ContentPresenter)items.CreateContainer(0, "hello");

        items.PrepareContainer(presenter, 0, "hello");
        presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.IsType<FixedElement>(presenter.PresentedChild);
    }

    [Fact]
    public void ExplicitItemTemplateOverridesRegistryTemplate()
    {
        TestItemsControl items = new();
        items.ContentTemplateRegistry.Register(new ContentTemplate<string>("string", key: null, priority: 0, _ => new FixedElement()));
        items.ItemTemplate = new ContentTemplate<string>("explicit", key: null, priority: 0, _ => new ExplicitElement());
        ContentPresenter presenter = (ContentPresenter)items.CreateContainer(0, "hello");

        items.PrepareContainer(presenter, 0, "hello");
        presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.IsType<ExplicitElement>(presenter.PresentedChild);
    }

    [Fact]
    public void VirtualizedItemsReuseContainersWithoutLeakingOldDataContext()
    {
        TemplateRecyclePool pool = new();
        ContentPresenter presenter = new() { Content = "old" };

        pool.Release(new TemplateRecycleKey(typeof(string), typeof(ContentPresenter), "item"), presenter);
        ContentPresenter reused = Assert.IsType<ContentPresenter>(pool.Rent(new TemplateRecycleKey(typeof(string), typeof(ContentPresenter), "item")));

        Assert.Null(reused.Content);
    }

    [Fact]
    public void TemplateContextReceivesItemIndex()
    {
        int observedIndex = -1;
        TestItemsControl items = new();
        items.ContentTemplateRegistry.Register(new ContentTemplate<string>(
            "string",
            key: null,
            priority: 0,
            context =>
            {
                observedIndex = context.Index;
                return new FixedElement();
            }));
        ContentPresenter presenter = (ContentPresenter)items.CreateContainer(3, "hello");

        items.PrepareContainer(presenter, 3, "hello");
        presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(3, observedIndex);
    }

    [Fact]
    public void ChangingTemplateRegistryInvalidatesRealizedItems()
    {
        TestItemsControl items = new();
        long before = items.DirtyState.Version;

        items.ContentTemplateRegistry = new ContentTemplateRegistry();

        Assert.True(items.DirtyState.Version > before);
    }

    private sealed class TestItemsControl : ItemsControl
    {
        public UIElement CreateContainer(int index, object? item) => CreateItemContainer(index, item);

        public void PrepareContainer(UIElement container, int index, object? item) => PrepareItemContainer(container, index, item);
    }

    private sealed class FixedElement : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context) => new(1, 1);
    }

    private sealed class ExplicitElement : UIElement;
}
