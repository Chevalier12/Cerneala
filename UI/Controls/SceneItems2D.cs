using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls;

public sealed class SceneItems2D : SceneNode2D
{
    public static readonly UiProperty<IEnumerable?> ItemsSourceProperty =
        UiProperty<IEnumerable?>.Register(
            nameof(ItemsSource),
            typeof(SceneItems2D),
            new UiPropertyMetadata<IEnumerable?>(null, UiPropertyOptions.AffectsRender));

    private readonly List<SceneNode2D> realizedNodes = [];
    private ContentTemplateRegistry templateRegistry = new();
    private IObservableList? observableItemsSource;
    private INotifyCollectionChanged? notifyingItemsSource;
    private bool isSubscribed;
    private bool hasEverAttached;

    public SceneItems2D()
    {
        Templates = new TemplateCollection(RebuildTemplates);
    }

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public Collection<ContentTemplate> Templates { get; }

    public int RealizedItemCount => realizedNodes.Count;

    internal SceneItems2DUpdateCounters UpdateCounters { get; } = new();

    protected override void OnAttached()
    {
        base.OnAttached();
        hasEverAttached = true;
        SubscribeToItemsSource();
        RebuildRealizedNodes();
    }

    protected override void OnDetached()
    {
        UnsubscribeFromItemsSource();
        base.OnDetached();
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (!ReferenceEquals(args.Property, ItemsSourceProperty))
        {
            return;
        }

        UnsubscribeFromItemsSource();
        if (!hasEverAttached || IsAttached)
        {
            SubscribeToItemsSource();
        }

        RebuildRealizedNodes();
    }

    internal override void AttachSurface(RenderSurface2D? surface)
    {
        base.AttachSurface(surface);
        foreach (SceneNode2D node in realizedNodes)
        {
            node.AttachSurface(surface);
        }
    }

    internal override void Record(Scene2DRecordContext context)
    {
        if (!UIElementVisibility.ParticipatesInRendering(this))
        {
            return;
        }

        for (int index = 0; index < realizedNodes.Count; index++)
        {
            realizedNodes[index].Record(context.WithSourceIndex(index));
        }
    }

    internal override SceneBounds2D GetVisibleLocalBounds()
    {
        SceneBounds2D result = SceneBounds2D.Empty;
        foreach (SceneNode2D node in realizedNodes)
        {
            SceneBounds2D nodeBounds = SceneGeometry2D.TransformBounds(
                node.GetLocalBounds(),
                node.GetLocalTransform());
            result = SceneGeometry2D.Union(result, nodeBounds);
            if (result.Kind == SceneBoundsKind.Unknown)
            {
                break;
            }
        }

        return result;
    }

    private void RebuildTemplates()
    {
        templateRegistry = new ContentTemplateRegistry();
        foreach (ContentTemplate template in Templates)
        {
            templateRegistry.Register(template);
        }

        RebuildRealizedNodes();
    }

    private void RebuildRealizedNodes()
    {
        RebuildFrom(0);
    }

    private SceneNode2D CreateNode(object? item, int index)
    {
        ContentTemplateMatchContext match = new(item, owner: this, index: index);
        if (templateRegistry.TryResolve(match, out ContentTemplate template))
        {
            SceneNode2D created = template.Create(
                new ContentTemplateContext(item, index: index, owner: this))
                as SceneNode2D
                ?? throw new InvalidOperationException(
                    $"Content template '{template.Name}' must create a {nameof(SceneNode2D)} for {nameof(SceneItems2D)}.");
            UpdateCounters.CountCreated();
            return created;
        }

        if (item is SceneNode2D node)
        {
            return node;
        }

        throw new InvalidOperationException(
            $"No content template matches item type '{item?.GetType().FullName ?? "null"}' in {nameof(SceneItems2D)}.");
    }

    private void SubscribeToItemsSource()
    {
        if (isSubscribed)
        {
            return;
        }

        observableItemsSource = ItemsSource as IObservableList;
        notifyingItemsSource = ItemsSource as INotifyCollectionChanged;
        if (observableItemsSource is not null)
        {
            observableItemsSource.Changed += OnObservableItemsChanged;
        }
        else if (notifyingItemsSource is not null)
        {
            notifyingItemsSource.CollectionChanged += OnCollectionChanged;
        }

        isSubscribed = observableItemsSource is not null || notifyingItemsSource is not null;
    }

    private void UnsubscribeFromItemsSource()
    {
        if (observableItemsSource is not null)
        {
            observableItemsSource.Changed -= OnObservableItemsChanged;
        }

        if (notifyingItemsSource is not null)
        {
            notifyingItemsSource.CollectionChanged -= OnCollectionChanged;
        }

        observableItemsSource = null;
        notifyingItemsSource = null;
        isSubscribed = false;
    }

    private void OnObservableItemsChanged(object? sender, ObservableListChangedEventArgs args)
    {
        switch (args.Kind)
        {
            case ObservableListChangeKind.Add:
                ApplyAdd(args.Index, DeltaCount(args.Items, args.Item));
                break;
            case ObservableListChangeKind.Remove:
                ApplyRemove(args.Index, DeltaCount(args.OldItems, args.OldItem));
                break;
            case ObservableListChangeKind.Replace:
                ApplyReplace(
                    args.Index,
                    DeltaCount(args.OldItems, args.OldItem),
                    DeltaCount(args.Items, args.Item));
                break;
            case ObservableListChangeKind.Move:
                ApplyMove(
                    args.OldIndex,
                    args.Index,
                    DeltaCount(args.Items, args.Item));
                break;
            case ObservableListChangeKind.Reset:
            case ObservableListChangeKind.Clear:
            default:
                RebuildRealizedNodes();
                break;
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        switch (args.Action)
        {
            case NotifyCollectionChangedAction.Add:
                ApplyAdd(args.NewStartingIndex, args.NewItems?.Count ?? 0);
                break;
            case NotifyCollectionChangedAction.Remove:
                ApplyRemove(args.OldStartingIndex, args.OldItems?.Count ?? 0);
                break;
            case NotifyCollectionChangedAction.Replace:
                ApplyReplace(
                    args.NewStartingIndex,
                    args.OldItems?.Count ?? 0,
                    args.NewItems?.Count ?? 0);
                break;
            case NotifyCollectionChangedAction.Move:
                ApplyMove(
                    args.OldStartingIndex,
                    args.NewStartingIndex,
                    args.NewItems?.Count ?? 0);
                break;
            case NotifyCollectionChangedAction.Reset:
            default:
                RebuildRealizedNodes();
                break;
        }
    }

    private void ApplyAdd(int index, int count)
    {
        int currentCount = GetCurrentItemCount();
        if (index < 0 ||
            count <= 0 ||
            currentCount != realizedNodes.Count + count ||
            index > realizedNodes.Count)
        {
            RebuildRealizedNodes();
            return;
        }

        RebuildFrom(index);
    }

    private void ApplyRemove(int index, int count)
    {
        int currentCount = GetCurrentItemCount();
        if (index < 0 ||
            count <= 0 ||
            currentCount != realizedNodes.Count - count ||
            index > currentCount)
        {
            RebuildRealizedNodes();
            return;
        }

        RebuildFrom(index);
    }

    private void ApplyReplace(int index, int oldCount, int newCount)
    {
        int currentCount = GetCurrentItemCount();
        if (index < 0 ||
            oldCount <= 0 ||
            newCount <= 0 ||
            currentCount != realizedNodes.Count - oldCount + newCount ||
            index + newCount > currentCount)
        {
            RebuildRealizedNodes();
            return;
        }

        if (oldCount != newCount)
        {
            RebuildFrom(index);
            return;
        }

        RebuildRange(index, newCount);
    }

    private void ApplyMove(int oldIndex, int newIndex, int count)
    {
        int currentCount = GetCurrentItemCount();
        if (oldIndex < 0 ||
            newIndex < 0 ||
            count <= 0 ||
            currentCount != realizedNodes.Count ||
            oldIndex + count > currentCount ||
            newIndex + count > currentCount)
        {
            RebuildRealizedNodes();
            return;
        }

        int start = Math.Min(oldIndex, newIndex);
        int end = Math.Max(oldIndex, newIndex) + count;
        RebuildRange(start, end - start);
    }

    private void RebuildFrom(int index)
    {
        int currentCount = GetCurrentItemCount();
        int safeIndex = Math.Clamp(index, 0, Math.Min(realizedNodes.Count, currentCount));
        RemoveRealizedRange(safeIndex, realizedNodes.Count - safeIndex);
        InsertCurrentRange(safeIndex, currentCount);
        Surface?.InvalidateFrame();
    }

    private void RebuildRange(int index, int count)
    {
        int currentCount = GetCurrentItemCount();
        if (index < 0 || count < 0 || index + count > currentCount)
        {
            RebuildRealizedNodes();
            return;
        }

        RemoveRealizedRange(index, count);
        InsertCurrentRange(index, index + count);
        Surface?.InvalidateFrame();
    }

    private void RemoveRealizedRange(int index, int count)
    {
        for (int offset = count - 1; offset >= 0; offset--)
        {
            int removalIndex = index + offset;
            SceneNode2D node = realizedNodes[removalIndex];
            node.AttachSurface(null);
            realizedNodes.RemoveAt(removalIndex);
            LogicalChildren.Remove(node);
            UpdateCounters.CountRemoved();
        }
    }

    private void InsertCurrentRange(int startIndex, int endIndex)
    {
        if (ItemsSource is IObservableList observable)
        {
            for (int index = startIndex; index < endIndex; index++)
            {
                InsertRealizedNode(index, observable[index]);
            }

            return;
        }

        if (ItemsSource is IList list)
        {
            for (int index = startIndex; index < endIndex; index++)
            {
                InsertRealizedNode(index, list[index]);
            }

            return;
        }

        int sourceIndex = 0;
        if (ItemsSource is not null)
        {
            foreach (object? item in ItemsSource)
            {
                if (sourceIndex >= endIndex)
                {
                    break;
                }

                if (sourceIndex >= startIndex)
                {
                    InsertRealizedNode(sourceIndex, item);
                }

                sourceIndex++;
            }
        }
    }

    private void InsertRealizedNode(int index, object? item)
    {
        SceneNode2D node = CreateNode(item, index);
        realizedNodes.Insert(index, node);
        LogicalChildren.Insert(index, node);
        node.AttachSurface(Surface);
        if (node.IsAttached)
        {
            UpdateCounters.CountAttached();
        }
    }

    private int GetCurrentItemCount()
    {
        if (ItemsSource is IObservableList observable)
        {
            return observable.Count;
        }

        if (ItemsSource is ICollection collection)
        {
            return collection.Count;
        }

        int count = 0;
        if (ItemsSource is not null)
        {
            foreach (object? _ in ItemsSource)
            {
                count++;
            }
        }

        return count;
    }

    private static int DeltaCount(IReadOnlyList<object?> items, object? item) =>
        items.Count > 0 || item is null
            ? items.Count
            : 1;

    private sealed class TemplateCollection(Action changed) : Collection<ContentTemplate>
    {
        protected override void InsertItem(int index, ContentTemplate item)
        {
            ArgumentNullException.ThrowIfNull(item);
            base.InsertItem(index, item);
            changed();
        }

        protected override void SetItem(int index, ContentTemplate item)
        {
            ArgumentNullException.ThrowIfNull(item);
            base.SetItem(index, item);
            changed();
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);
            changed();
        }

        protected override void ClearItems()
        {
            base.ClearItems();
            changed();
        }
    }
}

internal sealed class SceneItems2DUpdateCounters
{
    internal int CreatedNodes { get; private set; }

    internal int AttachedNodes { get; private set; }

    internal int MovedNodes { get; private set; }

    internal int RemovedNodes { get; private set; }

    internal SceneItems2DUpdateSnapshot Snapshot() =>
        new(CreatedNodes, AttachedNodes, MovedNodes, RemovedNodes);

    internal void CountCreated() => CreatedNodes++;

    internal void CountAttached() => AttachedNodes++;

    internal void CountRemoved() => RemovedNodes++;
}

internal readonly record struct SceneItems2DUpdateSnapshot(
    int CreatedNodes,
    int AttachedNodes,
    int MovedNodes,
    int RemovedNodes);
