using System.Collections;

namespace Cerneala.UI.Input;

public sealed class CommandBindingCollection : IReadOnlyList<CommandBinding>
{
    private readonly List<CommandBinding> bindings = [];

    public int Count => bindings.Count;

    public CommandBinding this[int index] => bindings[index];

    public void Add(CommandBinding binding)
    {
        bindings.Add(binding ?? throw new ArgumentNullException(nameof(binding)));
    }

    public void InvokeCanExecute(UiElementId sender, CanExecuteRoutedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        int count = bindings.Count;
        for (int i = 0; i < count; i++)
        {
            bindings[i].OnCanExecute(sender, args);
            if (args.Handled)
            {
                return;
            }
        }
    }

    public void InvokeExecuted(UiElementId sender, ExecutedRoutedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        int count = bindings.Count;
        for (int i = 0; i < count; i++)
        {
            bindings[i].OnExecuted(sender, args);
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
}
