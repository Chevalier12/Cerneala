using Cerneala.UI.Controls;

namespace Cerneala.Tests.Controls;

public sealed class TabItemTests
{
    [Fact]
    public void TabItemExposesHeaderAndSelectedState()
    {
        TabItem tabItem = new();

        tabItem.Header = "Settings";
        tabItem.IsSelected = true;

        Assert.Equal("Settings", tabItem.Header);
        Assert.True(tabItem.IsSelected);
    }

    [Fact]
    public void TabItemTracksPreparedItemContainerState()
    {
        TabControl tabControl = new();
        TabItem tabItem = new();
        tabControl.SetItems(new[] { tabItem });
        tabControl.SelectedIndex = 0;

        tabControl.ItemContainerGenerator.Realize();

        Assert.Equal(0, tabItem.ItemIndex);
        Assert.Same(tabItem, tabItem.Item);
        Assert.True(tabItem.IsSelected);
    }
}
