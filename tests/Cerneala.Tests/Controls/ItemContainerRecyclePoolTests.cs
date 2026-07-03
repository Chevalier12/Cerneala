using Cerneala.UI.Controls;

namespace Cerneala.Tests.Controls;

public sealed class ItemContainerRecyclePoolTests
{
    [Fact]
    public void RecyclePoolReturnsContainersByExactType()
    {
        ItemContainerRecyclePool pool = new();
        ListBoxItem listBoxItem = new();
        TabItem tabItem = new();

        pool.Push(listBoxItem);
        pool.Push(tabItem);

        Assert.Same(listBoxItem, pool.Pop(typeof(ListBoxItem)));
        Assert.Same(tabItem, pool.Pop(typeof(TabItem)));
        Assert.Null(pool.Pop(typeof(ContentPresenter)));
    }

    [Fact]
    public void RecyclePoolTracksCountAndClear()
    {
        ItemContainerRecyclePool pool = new();
        pool.Push(new ListBoxItem());
        pool.Push(new ListBoxItem());

        Assert.Equal(2, pool.Count);

        pool.Clear();

        Assert.Equal(0, pool.Count);
        Assert.Null(pool.Pop(typeof(ListBoxItem)));
    }
}
