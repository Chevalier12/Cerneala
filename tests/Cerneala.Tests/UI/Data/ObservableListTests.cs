using Cerneala.UI.Data;

namespace Cerneala.Tests.UI.Data;

public sealed class ObservableListTests
{
    [Fact]
    public void AddAndRemoveNotifyWithIndexesAndItems()
    {
        ObservableList<string> list = new();
        List<ObservableListChangedEventArgs<string>> changes = [];
        list.Changed += (_, args) => changes.Add(args);

        list.Add("one");
        list.Remove("one");

        Assert.Equal(2, changes.Count);
        Assert.Equal(ObservableListChangeKind.Add, changes[0].Kind);
        Assert.Equal(0, changes[0].Index);
        Assert.Equal("one", changes[0].Item);
        Assert.Equal(ObservableListChangeKind.Remove, changes[1].Kind);
        Assert.Equal(0, changes[1].Index);
        Assert.Equal("one", changes[1].Item);
    }

    [Fact]
    public void InsertRemoveReplaceMoveNotifyWithOldAndNewItemData()
    {
        ObservableList<string> list = new(["a", "b", "c"]);
        List<ObservableListChangedEventArgs<string>> changes = [];
        list.Changed += (_, args) => changes.Add(args);

        list.Insert(1, "x");
        list.RemoveAt(2);
        list[1] = "y";
        list.Move(2, 0);

        Assert.Equal(["c", "a", "y"], list.ToArray());

        Assert.Equal(ObservableListChangeKind.Add, changes[0].Kind);
        Assert.Equal(1, changes[0].Index);
        Assert.Equal("x", changes[0].Item);
        Assert.Equal(["x"], changes[0].Items);

        Assert.Equal(ObservableListChangeKind.Remove, changes[1].Kind);
        Assert.Equal(2, changes[1].Index);
        Assert.Equal("b", changes[1].Item);
        Assert.Equal("b", changes[1].OldItem);
        Assert.Equal(["b"], changes[1].OldItems);

        Assert.Equal(ObservableListChangeKind.Replace, changes[2].Kind);
        Assert.Equal(1, changes[2].Index);
        Assert.Equal("y", changes[2].Item);
        Assert.Equal("x", changes[2].OldItem);
        Assert.Equal(["y"], changes[2].Items);
        Assert.Equal(["x"], changes[2].OldItems);

        Assert.Equal(ObservableListChangeKind.Move, changes[3].Kind);
        Assert.Equal(0, changes[3].Index);
        Assert.Equal(2, changes[3].OldIndex);
        Assert.Equal("c", changes[3].Item);
        Assert.Equal("c", changes[3].OldItem);
        Assert.Equal(["c"], changes[3].Items);
        Assert.Equal(["c"], changes[3].OldItems);
    }

    [Fact]
    public void ReplaceMoveClearAndResetNotify()
    {
        ObservableList<string> list = new(["a", "b", "c"]);
        List<ObservableListChangeKind> kinds = [];
        list.Changed += (_, args) => kinds.Add(args.Kind);

        list[1] = "bb";
        list.Move(2, 0);
        list.Clear();
        list.ReplaceWith(["x", "y"]);

        Assert.Equal(
            [
                ObservableListChangeKind.Replace,
                ObservableListChangeKind.Move,
                ObservableListChangeKind.Clear,
                ObservableListChangeKind.Reset
            ],
            kinds);
        Assert.Equal(["x", "y"], list.ToArray());
    }

    [Fact]
    public void ClearAndResetNotifyWithSnapshots()
    {
        ObservableList<string> list = new(["a", "b"]);
        List<ObservableListChangedEventArgs<string>> changes = [];
        list.Changed += (_, args) => changes.Add(args);

        list.Clear();
        list.ReplaceWith(["x", "y"]);

        Assert.Equal(ObservableListChangeKind.Clear, changes[0].Kind);
        Assert.Equal(["a", "b"], changes[0].OldItems);
        Assert.Empty(changes[0].Items);

        Assert.Equal(ObservableListChangeKind.Reset, changes[1].Kind);
        Assert.Empty(changes[1].OldItems);
        Assert.Equal(["x", "y"], changes[1].Items);
        Assert.Equal(["x", "y"], list.ToArray());
    }

    [Fact]
    public void ReplaceWithSnapshotsNewItemsBeforeMutatingCurrentList()
    {
        ObservableList<string> list = new(["a", "b"]);
        List<ObservableListChangedEventArgs<string>> changes = [];
        list.Changed += (_, args) => changes.Add(args);

        list.ReplaceWith(list);

        Assert.Equal(["a", "b"], list.ToArray());
        ObservableListChangedEventArgs<string> change = Assert.Single(changes);
        Assert.Equal(ObservableListChangeKind.Reset, change.Kind);
        Assert.Equal(["a", "b"], change.OldItems);
        Assert.Equal(["a", "b"], change.Items);
    }

    [Fact]
    public void EmptyAndNoOpOperationsDoNotNotify()
    {
        ObservableList<string> list = new(["a"]);
        int notificationCount = 0;
        list.Changed += (_, _) => notificationCount++;

        Assert.False(list.Remove("missing"));
        list[0] = "a";
        list.Move(0, 0);
        list.Clear();
        list.Clear();

        Assert.Equal(1, notificationCount);
        Assert.Empty(list);
    }

    [Fact]
    public void InvalidMoveIndexThrowsWithoutMutatingOrNotifying()
    {
        ObservableList<string> list = new(["a", "b", "c"]);
        int notificationCount = 0;
        list.Changed += (_, _) => notificationCount++;

        Assert.Throws<ArgumentOutOfRangeException>(() => list.Move(0, list.Count));

        Assert.Equal(["a", "b", "c"], list.ToArray());
        Assert.Equal(0, notificationCount);
    }

    [Fact]
    public void InvalidMoveOnEmptyListThrowsWithoutNotifying()
    {
        ObservableList<string> list = new();
        int notificationCount = 0;
        list.Changed += (_, _) => notificationCount++;

        Assert.Throws<ArgumentOutOfRangeException>(() => list.Move(0, 0));

        Assert.Empty(list);
        Assert.Equal(0, notificationCount);
    }

    [Fact]
    public void ImplementsMutableIListSemantics()
    {
        IList<string> list = new ObservableList<string>();

        Assert.False(list.IsReadOnly);
        list.Add("a");
        list.Insert(1, "b");
        list[0] = "aa";

        string[] copy = new string[3];
        list.CopyTo(copy, 1);

        Assert.Equal(["aa", "b"], list.ToArray());
        Assert.Contains("b", list);
        Assert.Equal(0, list.IndexOf("aa"));
        Assert.Null(copy[0]);
        Assert.Equal("aa", copy[1]);
        Assert.Equal("b", copy[2]);
        Assert.True(list.Remove("aa"));
        Assert.False(list.Remove("missing"));
        Assert.Equal(["b"], list.ToArray());
    }
}
