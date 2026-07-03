namespace Cerneala.UI.Controls;

public class SelectionModel
{
    private int selectedIndex = -1;

    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    public int SelectedIndex => selectedIndex;

    public bool HasSelection => selectedIndex >= 0;

    public bool IsSelected(int index)
    {
        return selectedIndex == index;
    }

    public SelectionChangeResult Select(int index)
    {
        if (index < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        int oldIndex = selectedIndex;
        if (oldIndex == index)
        {
            return new SelectionChangeResult(oldIndex, index, false);
        }

        selectedIndex = index;
        SelectionChangeResult result = new(oldIndex, index, true);
        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(result));
        return result;
    }

    public SelectionChangeResult Clear()
    {
        return Select(-1);
    }
}

public readonly record struct SelectionChangeResult(int OldIndex, int NewIndex, bool Changed);

public sealed class SelectionChangedEventArgs : EventArgs
{
    public SelectionChangedEventArgs(SelectionChangeResult change)
    {
        Change = change;
    }

    public SelectionChangeResult Change { get; }
}
