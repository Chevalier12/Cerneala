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

    // As WPF's SelectedIndex is the first item of SelectedItems, SelectedIndex stays the first index in selection
    // order when another index is added.
    [Fact]
    public void AddKeepsSelectedIndicesInSelectionOrder()
    {
        SelectionModel model = new();
        model.Select(2);
        List<SelectionChangeResult> raised = [];
        model.SelectionChanged += (_, args) => raised.Add(args.Change);

        SelectionChangeResult result = model.Add(0);

        Assert.Equal([2, 0], model.SelectedIndices);
        Assert.Equal(2, model.SelectedIndex);
        Assert.True(model.IsSelected(0));
        Assert.True(model.IsSelected(2));
        Assert.Equal(new SelectionChangeResult(2, 2, true), result);
        Assert.Equal([result], raised);
    }

    [Fact]
    public void AddOfSelectedIndexChangesNothing()
    {
        SelectionModel model = new();
        model.Select(1);
        int raised = 0;
        model.SelectionChanged += (_, _) => raised++;

        SelectionChangeResult result = model.Add(1);

        Assert.False(result.Changed);
        Assert.Equal([1], model.SelectedIndices);
        Assert.Equal(0, raised);
    }

    [Fact]
    public void RemoveOfFirstSelectedIndexMakesTheNextOneSelectedIndex()
    {
        SelectionModel model = new();
        model.Select(2);
        model.Add(0);

        SelectionChangeResult result = model.Remove(2);

        Assert.Equal([0], model.SelectedIndices);
        Assert.Equal(new SelectionChangeResult(2, 0, true), result);
        Assert.False(model.IsSelected(2));
    }

    [Fact]
    public void SelectReplacesEverySelectedIndex()
    {
        SelectionModel model = new();
        model.Select(1);
        model.Add(3);

        SelectionChangeResult result = model.Select(3);

        Assert.Equal([3], model.SelectedIndices);
        Assert.Equal(new SelectionChangeResult(1, 3, true), result);

        model.Clear();

        Assert.Empty(model.SelectedIndices);
        Assert.Equal(-1, model.SelectedIndex);
        Assert.False(model.HasSelection);
    }

    // As WPF's range selection, the selected indices inside the range keep their order and the others of the range
    // follow in index order; the indices outside the range are deselected.
    [Fact]
    public void SelectRangeReplacesSelectionWithTheRange()
    {
        SelectionModel model = new();
        model.Select(3);
        model.Add(0);

        SelectionChangeResult result = model.SelectRange(3, 1);

        Assert.Equal([3, 1, 2], model.SelectedIndices);
        Assert.Equal(new SelectionChangeResult(3, 3, true), result);
    }

    [Fact]
    public void AddRangeKeepsTheSelectionOutsideTheRange()
    {
        SelectionModel model = new();
        model.Select(5);

        model.AddRange(0, 1);

        Assert.Equal([5, 0, 1], model.SelectedIndices);
        Assert.Equal(5, model.SelectedIndex);
    }

    [Fact]
    public void MultipleSelectionOperationsRejectNegativeIndices()
    {
        SelectionModel model = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => model.Add(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => model.Remove(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => model.SelectRange(-1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => model.AddRange(0, -1));
        Assert.Empty(model.SelectedIndices);
    }

    [Fact]
    public void TypedSelectionModelDropsSelectedIndicesPastTheNewItems()
    {
        SelectionModel<string> model = new(["a", "b", "c"]);
        model.Select(2);
        model.Add(0);

        model.SetItems(["x", "y"]);

        Assert.Equal([0], model.SelectedIndices);
        Assert.Equal("x", model.SelectedItem);
    }
}
