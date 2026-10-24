using System.Collections;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class CommandBindingCollection : IReadOnlyList<CommandBinding>
{
    private readonly List<CommandBinding> bindings = [];
    private readonly UIElement? owner;
    private CommandBinding[]? dispatchSnapshot;

    public CommandBindingCollection()
    {
    }

    internal CommandBindingCollection(UIElement owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public int Count => bindings.Count;

    public CommandBinding this[int index] => bindings[index];

    public void Add(CommandBinding binding)
    {
        bindings.Add(binding ?? throw new ArgumentNullException(nameof(binding)));
        NotifyChanged();
    }

    public bool Remove(CommandBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        bool removed = bindings.Remove(binding);
        if (removed)
        {
            NotifyChanged();
        }

        return removed;
    }

    public void Clear()
    {
        if (bindings.Count == 0)
        {
            return;
        }

        bindings.Clear();
        NotifyChanged();
    }

    public void InvokeCanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        foreach (CommandBinding binding in GetDispatchSnapshot())
        {
            binding.OnCanExecute(sender, args);
            if (args.Handled)
            {
                return;
            }
        }
    }

    public void InvokeExecuted(object sender, ExecutedRoutedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        foreach (CommandBinding binding in GetDispatchSnapshot())
        {
            binding.OnExecuted(sender, args);
            if (args.Handled)
            {
                return;
            }
        }
    }

    public IEnumerator<CommandBinding> GetEnumerator()
    {
        return bindings.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    // Dispatch walks the bindings that existed when the pass started, so a handler can add, remove or clear bindings.
    private CommandBinding[] GetDispatchSnapshot()
    {
        return dispatchSnapshot ??= bindings.ToArray();
    }

    private void NotifyChanged()
    {
        dispatchSnapshot = null;
        owner?.QueueDescendantCommandStateRefreshes();
    }
}
