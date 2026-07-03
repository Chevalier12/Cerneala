using System.Collections;

namespace Cerneala.UI.Data;

public sealed class CollectionView<T> : IReadOnlyList<T>, IDisposable
{
    private readonly IEnumerable<T> source;
    private readonly IObservableList<T>? observableSource;
    private readonly List<T> view = [];
    private bool disposed;

    public CollectionView(IEnumerable<T> source)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        observableSource = source as IObservableList<T>;
        if (observableSource is not null)
        {
            observableSource.Changed += OnSourceChanged;
        }

        Refresh(emitNotification: false);
    }

    public event EventHandler<ObservableListChangedEventArgs<T>>? Changed;

    public FilterPredicate<T>? Filter { get; set; }

    public IList<SortDescription<T>> SortDescriptions { get; } = new List<SortDescription<T>>();

    public int Count => view.Count;

    public T this[int index] => view[index];

    public void Refresh()
    {
        ThrowIfDisposed();
        Refresh(emitNotification: true);
    }

    public IEnumerator<T> GetEnumerator()
    {
        return view.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (observableSource is not null)
        {
            observableSource.Changed -= OnSourceChanged;
        }
    }

    private void Refresh(bool emitNotification)
    {
        List<T> oldItems = [.. view];
        IEnumerable<T> query = source;
        if (Filter is not null)
        {
            query = query.Where(item => Filter(item));
        }

        bool firstSort = true;
        IOrderedEnumerable<T>? ordered = null;
        foreach (SortDescription<T> sort in SortDescriptions)
        {
            if (firstSort)
            {
                ordered = sort.Descending
                    ? query.OrderByDescending(sort.KeySelector)
                    : query.OrderBy(sort.KeySelector);
                firstSort = false;
                continue;
            }

            ordered = sort.Descending
                ? ordered!.ThenByDescending(sort.KeySelector)
                : ordered!.ThenBy(sort.KeySelector);
        }

        view.Clear();
        view.AddRange((ordered ?? query).ToArray());

        if (emitNotification)
        {
            Changed?.Invoke(
                this,
                new ObservableListChangedEventArgs<T>(
                    ObservableListChangeKind.Reset,
                    items: [.. view],
                    oldItems: oldItems));
        }
    }

    private void OnSourceChanged(object? sender, ObservableListChangedEventArgs<T> args)
    {
        if (!disposed)
        {
            Refresh(emitNotification: true);
        }
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(CollectionView<T>));
        }
    }
}
