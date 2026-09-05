using System.Collections;
using System.Collections.ObjectModel;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class SceneItems2DIncrementalContractTests
{
    [Fact]
    public void ObservableListAddAtEndCreatesOnlyTheAddedNode()
    {
        ObservableList<Item> items = [new Item("a"), new Item("b")];
        Fixture fixture = Attach(items);
        SceneNode2D[] before = fixture.Nodes;
        int creationsBefore = fixture.Created.Count;

        items.Add(new Item("c"));

        Assert.Equal(creationsBefore + 1, fixture.Created.Count);
        Assert.Same(before[0], fixture.Nodes[0]);
        Assert.Same(before[1], fixture.Nodes[1]);
        Assert.Equal(["a", "b", "c"], fixture.Nodes.Select(node => node.Item.Value));
        Assert.Equal([0, 1, 2], fixture.Nodes.Select(node => node.Index));
        AssertDataContexts(fixture.Nodes);
    }

    [Fact]
    public void AppendToTenThousandItemsCreatesAndAttachesExactlyOneNode()
    {
        ObservableList<Item> items = new(
            Enumerable.Range(0, 10_000)
                .Select(index => new Item(index.ToString())));
        Fixture fixture = Attach(items);
        TrackingNode previousLast = fixture.Nodes[^1];
        SceneItems2DUpdateSnapshot before =
            fixture.SceneItems.UpdateCounters.Snapshot();

        items.Add(new Item("appended"));

        SceneItems2DUpdateSnapshot after =
            fixture.SceneItems.UpdateCounters.Snapshot();
        Assert.Equal(10_001, fixture.SceneItems.RealizedItemCount);
        Assert.Same(previousLast, fixture.Nodes[^2]);
        Assert.Equal(1, after.CreatedNodes - before.CreatedNodes);
        Assert.Equal(1, after.AttachedNodes - before.AttachedNodes);
        Assert.Equal(0, after.RemovedNodes - before.RemovedNodes);
        Assert.Equal(0, after.MovedNodes - before.MovedNodes);
    }

    [Fact]
    public void ObservableListInsertAndRemovePreserveUnaffectedPrefix()
    {
        ObservableList<Item> items =
            [new Item("a"), new Item("b"), new Item("c")];
        Fixture fixture = Attach(items);
        TrackingNode first = fixture.Nodes[0];

        items.Insert(1, new Item("x"));

        Assert.Same(first, fixture.Nodes[0]);
        Assert.Equal(["a", "x", "b", "c"], fixture.Nodes.Select(node => node.Item.Value));
        Assert.Equal([0, 1, 2, 3], fixture.Nodes.Select(node => node.Index));

        items.RemoveAt(2);

        Assert.Same(first, fixture.Nodes[0]);
        Assert.Equal(["a", "x", "c"], fixture.Nodes.Select(node => node.Item.Value));
        Assert.Equal([0, 1, 2], fixture.Nodes.Select(node => node.Index));
        AssertDataContexts(fixture.Nodes);
    }

    [Fact]
    public void ObservableListMoveTouchesOnlyTheChangedRange()
    {
        ObservableList<Item> items =
            [new Item("a"), new Item("b"), new Item("c"), new Item("d")];
        Fixture fixture = Attach(items);
        TrackingNode first = fixture.Nodes[0];
        TrackingNode last = fixture.Nodes[3];

        items.Move(1, 2);

        Assert.Same(first, fixture.Nodes[0]);
        Assert.Same(last, fixture.Nodes[3]);
        Assert.Equal(["a", "c", "b", "d"], fixture.Nodes.Select(node => node.Item.Value));
        Assert.Equal([0, 1, 2, 3], fixture.Nodes.Select(node => node.Index));
        AssertDataContexts(fixture.Nodes);
    }

    [Fact]
    public void ObservableListReplacePreservesOtherNodesWithDuplicateValues()
    {
        Item duplicate = new("same");
        ObservableList<Item> items = [duplicate, duplicate, new Item("tail")];
        Fixture fixture = Attach(items);
        TrackingNode first = fixture.Nodes[0];
        TrackingNode third = fixture.Nodes[2];

        items[1] = new Item("replacement");

        Assert.Same(first, fixture.Nodes[0]);
        Assert.Same(third, fixture.Nodes[2]);
        Assert.NotSame(first, fixture.Nodes[1]);
        Assert.Equal(
            ["same", "replacement", "tail"],
            fixture.Nodes.Select(node => node.Item.Value));
        Assert.Equal([0, 1, 2], fixture.Nodes.Select(node => node.Index));
        AssertDataContexts(fixture.Nodes);
    }

    [Fact]
    public void ObservableListResetIntentionallyRebuildsEveryNode()
    {
        ObservableList<Item> items = [new Item("a"), new Item("b")];
        Fixture fixture = Attach(items);
        TrackingNode[] before = fixture.Nodes;

        items.ReplaceWith([new Item("x"), new Item("y")]);

        Assert.Equal(["x", "y"], fixture.Nodes.Select(node => node.Item.Value));
        Assert.All(before, node => Assert.DoesNotContain(node, fixture.Nodes));
        Assert.All(before, node => Assert.False(node.IsAttached));
    }

    [Fact]
    public void NotifyCollectionChangedAddAtEndCreatesOnlyTheAddedNode()
    {
        ObservableCollection<Item> items = [new Item("a"), new Item("b")];
        Fixture fixture = Attach(items);
        SceneNode2D[] before = fixture.Nodes;
        int creationsBefore = fixture.Created.Count;

        items.Add(new Item("c"));

        Assert.Equal(creationsBefore + 1, fixture.Created.Count);
        Assert.Same(before[0], fixture.Nodes[0]);
        Assert.Same(before[1], fixture.Nodes[1]);
    }

    [Fact]
    public void NotifyCollectionChangedMoveReplaceRemoveAndResetTouchOnlyRequiredRanges()
    {
        ObservableCollection<Item> items =
            [new Item("a"), new Item("b"), new Item("c"), new Item("d")];
        Fixture fixture = Attach(items);
        TrackingNode first = fixture.Nodes[0];
        TrackingNode last = fixture.Nodes[3];
        SceneItems2DUpdateSnapshot beforeMove =
            fixture.SceneItems.UpdateCounters.Snapshot();

        items.Move(1, 2);

        SceneItems2DUpdateSnapshot afterMove =
            fixture.SceneItems.UpdateCounters.Snapshot();
        Assert.Same(first, fixture.Nodes[0]);
        Assert.Same(last, fixture.Nodes[3]);
        Assert.Equal(["a", "c", "b", "d"], fixture.Nodes.Select(node => node.Item.Value));
        Assert.Equal(2, afterMove.CreatedNodes - beforeMove.CreatedNodes);
        Assert.Equal(2, afterMove.RemovedNodes - beforeMove.RemovedNodes);
        Assert.Equal(0, afterMove.MovedNodes - beforeMove.MovedNodes);

        TrackingNode firstAfterMove = fixture.Nodes[0];
        TrackingNode lastAfterMove = fixture.Nodes[3];
        items[1] = new Item("replacement");
        Assert.Same(firstAfterMove, fixture.Nodes[0]);
        Assert.Same(lastAfterMove, fixture.Nodes[3]);
        Assert.Equal("replacement", fixture.Nodes[1].Item.Value);

        TrackingNode prefix = fixture.Nodes[0];
        items.RemoveAt(1);
        Assert.Same(prefix, fixture.Nodes[0]);
        Assert.Equal(["a", "b", "d"], fixture.Nodes.Select(node => node.Item.Value));
        Assert.Equal([0, 1, 2], fixture.Nodes.Select(node => node.Index));

        TrackingNode[] beforeReset = fixture.Nodes;
        items.Clear();
        Assert.Empty(fixture.Nodes);
        Assert.All(beforeReset, node => Assert.False(node.IsAttached));
    }

    [Fact]
    public void TemplateChangeRebuildsEveryRealizedNode()
    {
        CountingObservableList items = new([new Item("a"), new Item("b")]);
        Fixture fixture = Attach(items);
        TrackingNode[] before = fixture.Nodes;

        fixture.SceneItems.Templates[0] = CreateTemplate(fixture.Created, "replacement");

        Assert.All(before, node => Assert.DoesNotContain(node, fixture.Nodes));
        Assert.All(before, node => Assert.False(node.IsAttached));
        Assert.Equal(2, fixture.Nodes.Length);
        Assert.Equal(1, items.AddedHandlers);
        Assert.Equal(0, items.RemovedHandlers);
        Assert.Equal(1, items.ActiveHandlers);
    }

    [Fact]
    public void AttachDetachReattachMaintainsExactlyOneItemsSubscription()
    {
        CountingObservableList source = new([new Item("a")]);
        SceneItems2D sceneItems = new();
        List<TrackingNode> created = [];
        sceneItems.Templates.Add(CreateTemplate(created, "tracking"));
        sceneItems.ItemsSource = source;
        Scene2D scene = new();
        scene.Children.Add(sceneItems);
        RenderSurface2D surface = new() { Scene = scene };
        UIRoot root = new();

        root.VisualChildren.Add(surface);
        Assert.Equal(1, source.AddedHandlers);
        Assert.Equal(0, source.RemovedHandlers);
        Assert.Equal(1, source.ActiveHandlers);

        root.VisualChildren.Remove(surface);
        Assert.Equal(1, source.RemovedHandlers);
        Assert.Equal(0, source.ActiveHandlers);

        root.VisualChildren.Add(surface);
        Assert.Equal(2, source.AddedHandlers);
        Assert.Equal(1, source.RemovedHandlers);
        Assert.Equal(1, source.ActiveHandlers);

        source.Add(new Item("b"));
        Assert.Equal(2, sceneItems.RealizedItemCount);
    }

    [Fact]
    public void ChangingItemsSourceUnsubscribesTheOldSourceExactlyOnce()
    {
        CountingObservableList first = new([new Item("a")]);
        CountingObservableList second = new([new Item("b")]);
        SceneItems2D sceneItems = new();
        List<TrackingNode> created = [];
        sceneItems.Templates.Add(CreateTemplate(created, "tracking"));
        sceneItems.ItemsSource = first;
        Scene2D scene = new();
        scene.Children.Add(sceneItems);
        RenderSurface2D surface = new() { Scene = scene };
        UIRoot root = new();
        root.VisualChildren.Add(surface);

        sceneItems.ItemsSource = second;

        Assert.Equal(0, first.ActiveHandlers);
        Assert.Equal(1, first.RemovedHandlers);
        Assert.Equal(1, second.ActiveHandlers);
        Assert.Equal(["b"], sceneItems.LogicalChildren.Cast<TrackingNode>().Select(node => node.Item.Value));
    }

    private static Fixture Attach(IEnumerable items)
    {
        List<TrackingNode> created = [];
        SceneItems2D sceneItems = new();
        sceneItems.Templates.Add(CreateTemplate(created, "tracking"));
        sceneItems.ItemsSource = items;
        Scene2D scene = new();
        scene.Children.Add(sceneItems);
        RenderSurface2D surface = new() { Scene = scene };
        UIRoot root = new();
        root.VisualChildren.Add(surface);
        return new Fixture(root, surface, sceneItems, created);
    }

    private static void AssertDataContexts(IEnumerable<TrackingNode> nodes)
    {
        Assert.All(nodes, node => Assert.Same(node.Item, node.DataContext));
    }

    private static ContentTemplate<Item> CreateTemplate(
        List<TrackingNode> created,
        string name)
    {
        return new ContentTemplate<Item>(
            name,
            key: null,
            priority: 0,
            context =>
            {
                TrackingNode node = new(context.Data!, context.Index)
                {
                    DataContext = context.Data
                };
                created.Add(node);
                return node;
            });
    }

    private sealed record Item(string Value);

    private sealed class TrackingNode(Item item, int index) : SceneNode2D
    {
        public Item Item { get; } = item;

        public int Index { get; } = index;

        public int AttachCount { get; private set; }

        public int DetachCount { get; private set; }

        protected override void OnAttached()
        {
            AttachCount++;
            base.OnAttached();
        }

        protected override void OnDetached()
        {
            DetachCount++;
            base.OnDetached();
        }

        internal override void Record(Scene2DRecordContext context)
        {
        }

        internal override SceneBounds2D GetVisibleLocalBounds() =>
            SceneBounds2D.Empty;
    }

    private sealed class Fixture(
        UIRoot root,
        RenderSurface2D surface,
        SceneItems2D sceneItems,
        List<TrackingNode> created)
    {
        public UIRoot Root { get; } = root;

        public RenderSurface2D Surface { get; } = surface;

        public SceneItems2D SceneItems { get; } = sceneItems;

        public List<TrackingNode> Created { get; } = created;

        public TrackingNode[] Nodes => SceneItems.LogicalChildren.Cast<TrackingNode>().ToArray();
    }

    private sealed class CountingObservableList(IEnumerable<Item> items) : IObservableList
    {
        private readonly List<Item> values = [.. items];
        private EventHandler<ObservableListChangedEventArgs>? changed;

        public int AddedHandlers { get; private set; }

        public int RemovedHandlers { get; private set; }

        public int ActiveHandlers { get; private set; }

        public event EventHandler<ObservableListChangedEventArgs>? Changed
        {
            add
            {
                AddedHandlers++;
                ActiveHandlers++;
                changed += value;
            }
            remove
            {
                RemovedHandlers++;
                ActiveHandlers--;
                changed -= value;
            }
        }

        public int Count => values.Count;

        public object this[int index] => values[index];

        public void Add(Item item)
        {
            int index = values.Count;
            values.Add(item);
            changed?.Invoke(
                this,
                new ObservableListChangedEventArgs(
                    ObservableListChangeKind.Add,
                    index,
                    item: item,
                    items: [item]));
        }

        public IEnumerator GetEnumerator()
        {
            return values.GetEnumerator();
        }
    }
}
