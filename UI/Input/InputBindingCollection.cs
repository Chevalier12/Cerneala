using System.Collections.ObjectModel;

namespace Cerneala.UI.Input;

public sealed class InputBindingCollection : Collection<InputBinding>
{
    protected override void InsertItem(int index, InputBinding item)
    {
        ArgumentNullException.ThrowIfNull(item);
        base.InsertItem(index, item);
    }

    protected override void SetItem(int index, InputBinding item)
    {
        ArgumentNullException.ThrowIfNull(item);
        base.SetItem(index, item);
    }
}
