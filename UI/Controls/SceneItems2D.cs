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

    internal override void Record(RenderSurface2DFrame frame)
    {
        if (!UIElementVisibility.ParticipatesInRendering(this))
        {
            return;
        }

        foreach (SceneNode2D node in realizedNodes)
        {
            node.Record(frame);
        }
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
        foreach (SceneNode2D node in realizedNodes)
        {
            node.AttachSurface(null);
            LogicalChildren.Remove(node);
        }

        realizedNodes.Clear();
        int index = 0;
        if (ItemsSource is not null)
        {
            foreach (object? item in ItemsSource)
            {
                SceneNode2D node = CreateNode(item, index);
                realizedNodes.Add(node);
                LogicalChildren.Add(node);
                node.AttachSurface(Surface);
                index++;
            }
        }

        Surface?.InvalidateFrame();
    }

    private SceneNode2D CreateNode(object? item, int index)
    {
        ContentTemplateMatchContext match = new(item, owner: this, index: index);
        if (templateRegistry.TryResolve(match, out ContentTemplate template))
        {
            return template.Create(new ContentTemplateContext(item, index: index, owner: this))
                as SceneNode2D
                ?? throw new InvalidOperationException(
                    $"Content template '{template.Name}' must create a {nameof(SceneNode2D)} for {nameof(SceneItems2D)}.");
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
        RebuildRealizedNodes();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        RebuildRealizedNodes();
    }

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
