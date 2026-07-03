using Cerneala.UI.Controls;

namespace Cerneala.Tests.Controls;

public sealed class SelectionModelTests
{
    [Fact]
    public void SelectionModelSelectsSingleIndex()
    {
        SelectionModel model = new();

        SelectionChangeResult first = model.Select(2);
        SelectionChangeResult second = model.Select(4);

        Assert.True(first.Changed);
        Assert.Equal(-1, first.OldIndex);
        Assert.Equal(2, first.NewIndex);
        Assert.False(model.IsSelected(2));
        Assert.True(model.IsSelected(4));
        Assert.Equal(2, second.OldIndex);
    }

    [Fact]
    public void TypedSelectionModelExposesSelectedItem()
    {
        SelectionModel<string> model = new(["one", "two"]);

        model.SelectItem("two");

        Assert.Equal(1, model.SelectedIndex);
        Assert.Equal("two", model.SelectedItem);
    }
}
