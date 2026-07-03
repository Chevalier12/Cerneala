using Cerneala.UI.Data;

namespace Cerneala.Tests.UI.Data;

public sealed class CollectionViewTests
{
    [Fact]
    public void CollectionViewFiltersSourceItems()
    {
        CollectionView<int> view = new([1, 2, 3, 4])
        {
            Filter = value => value % 2 == 0
        };

        view.Refresh();

        Assert.Equal([2, 4], view.ToArray());
    }

    [Fact]
    public void CollectionViewSortsSourceItemsDeterministically()
    {
        CollectionView<Person> view = new(
            [
                new Person("b", 2),
                new Person("a", 3),
                new Person("a", 1)
            ]);
        view.SortDescriptions.Add(new SortDescription<Person>(person => person.Name));
        view.SortDescriptions.Add(new SortDescription<Person>(person => person.Rank, descending: true));

        view.Refresh();

        Assert.Equal([3, 1, 2], view.Select(person => person.Rank).ToArray());
    }

    [Fact]
    public void ObservableSourceChangesRefreshViewAndEmitReset()
    {
        ObservableList<int> source = new([1, 2]);
        CollectionView<int> view = new(source)
        {
            Filter = value => value > 1
        };
        ObservableListChangedEventArgs<int>? observed = null;
        view.Changed += (_, args) => observed = args;

        source.Add(3);

        Assert.Equal([2, 3], view.ToArray());
        Assert.NotNull(observed);
        Assert.Equal(ObservableListChangeKind.Reset, observed.Kind);
    }

    private sealed record Person(string Name, int Rank);
}
