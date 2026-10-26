using System.Runtime.ExceptionServices;

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
        // A failing Dispose does not skip the later subscriptions: each one is disposed, then the first failure
        // propagates.
        ExceptionDispatchInfo? failure = null;
        foreach (IDisposable binding in snapshot)
        {
            try { binding.Dispose(); }
            catch (Exception exception) { failure ??= ExceptionDispatchInfo.Capture(exception); }
        }

        failure?.Throw();
    }
}
