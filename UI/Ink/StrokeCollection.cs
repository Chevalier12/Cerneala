namespace Cerneala.UI.Ink;

public sealed class StrokeCollection : IReadOnlyList<Stroke>
{
    private readonly List<Stroke> strokes = [];

    public event EventHandler<StrokeCollectionChangedEventArgs>? Changed;

    public int Count => strokes.Count;

    public Stroke this[int index] => strokes[index];

    public void Add(Stroke stroke)
    {
        ArgumentNullException.ThrowIfNull(stroke);
        strokes.Add(stroke);
        Changed?.Invoke(this, new StrokeCollectionChangedEventArgs(StrokeCollectionChangeKind.Added, stroke));
    }

    public bool Remove(Stroke stroke)
    {
        ArgumentNullException.ThrowIfNull(stroke);
        if (!strokes.Remove(stroke))
        {
            return false;
        }

        Changed?.Invoke(this, new StrokeCollectionChangedEventArgs(StrokeCollectionChangeKind.Removed, stroke));
        return true;
    }

    public IEnumerator<Stroke> GetEnumerator()
    {
        return strokes.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
