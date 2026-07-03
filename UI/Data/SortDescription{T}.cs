namespace Cerneala.UI.Data;

public sealed class SortDescription<T>
{
    public SortDescription(Func<T, IComparable?> keySelector, bool descending = false)
    {
        KeySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
        Descending = descending;
    }

    public Func<T, IComparable?> KeySelector { get; }

    public bool Descending { get; }
}
