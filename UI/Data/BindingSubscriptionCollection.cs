namespace Cerneala.UI.Data;

public sealed class BindingSubscriptionCollection
{
    private readonly List<IDisposable> bindings = [];

    public int Count => bindings.Count;

    public void Add(IDisposable binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        if (bindings.Contains(binding))
        {
            return;
        }

        bindings.Add(binding);
    }

    public bool Remove(IDisposable binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        bool removed = bindings.Remove(binding);
        if (removed)
        {
            binding.Dispose();
        }

        return removed;
    }

    public void Clear()
    {
        if (bindings.Count == 0)
        {
            return;
        }

        IDisposable[] snapshot = bindings.ToArray();
        bindings.Clear();
        foreach (IDisposable binding in snapshot)
        {
            binding.Dispose();
        }
    }
}
