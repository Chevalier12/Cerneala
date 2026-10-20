using System.Collections.ObjectModel;

namespace Cerneala.UI.Controls.Selection;

public class SelectionModel
{
    // The selected indices in selection order, without duplicates. SelectedIndex is the first of them, as WPF's
    // SelectedIndex is the first item of SelectedItems.
    private readonly List<int> selectedIndices = [];
    private readonly HashSet<int> selectedSet = [];
    private readonly ReadOnlyCollection<int> readOnlySelectedIndices;

    public SelectionModel()
    {
        readOnlySelectedIndices = selectedIndices.AsReadOnly();
    }

    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    public IReadOnlyList<int> SelectedIndices => readOnlySelectedIndices;

    public int SelectedIndex => selectedIndices.Count > 0 ? selectedIndices[0] : -1;

    public bool HasSelection => selectedIndices.Count > 0;

    public bool IsSelected(int index)
    {
        return selectedSet.Contains(index);
    }

    // The selection becomes index alone; -1 clears it.
    public SelectionChangeResult Select(int index)
    {
        if (index < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return SetSelectedIndices(index < 0 ? [] : [index]);
    }

    // Adds index after the selected indices.
    public SelectionChangeResult Add(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        return selectedSet.Contains(index) ? Unchanged() : SetSelectedIndices([.. selectedIndices, index]);
    }

    public SelectionChangeResult Remove(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        return selectedSet.Contains(index)
            ? SetSelectedIndices([.. selectedIndices.Where(selected => selected != index)])
            : Unchanged();
    }

    // The selection becomes the indices from fromIndex to toIndex, in either direction. As WPF's range selection, the
    // selected indices in the range keep their order, and the other indices of the range follow in index order.
    public SelectionChangeResult SelectRange(int fromIndex, int toIndex)
    {
        (int first, int last) = ValidateRange(fromIndex, toIndex);
        return SetSelectedIndices(MergeRange(selectedIndices.Where(index => index >= first && index <= last), first, last));
    }

    // Adds the indices from fromIndex to toIndex, in either direction, in index order after the selected indices.
    public SelectionChangeResult AddRange(int fromIndex, int toIndex)
    {
        (int first, int last) = ValidateRange(fromIndex, toIndex);
        return SetSelectedIndices(MergeRange(selectedIndices, first, last));
    }

    public SelectionChangeResult Clear()
    {
        return Select(-1);
    }

    // The selection becomes exactly indices, in that order. The selection owner uses it to move every selected index
    // with its item when the items change.
    internal SelectionChangeResult SetSelectedIndices(IReadOnlyList<int> indices)
    {
        int oldIndex = SelectedIndex;
        if (selectedIndices.SequenceEqual(indices))
        {
            return Unchanged();
        }

        HashSet<int> newSet = [.. indices];
        List<int> removed = [.. selectedIndices.Where(index => !newSet.Contains(index))];
        List<int> added = [.. indices.Where(index => !selectedSet.Contains(index))];
        selectedIndices.Clear();
        selectedIndices.AddRange(indices);
        selectedSet.Clear();
        selectedSet.UnionWith(newSet);
        SelectionChangeResult result = new(oldIndex, SelectedIndex, true);
        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(result)
        {
            RemovedIndices = removed,
            AddedIndices = added
        });
        return result;
    }

    private SelectionChangeResult Unchanged()
    {
        return new SelectionChangeResult(SelectedIndex, SelectedIndex, false);
    }

    private static (int First, int Last) ValidateRange(int fromIndex, int toIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fromIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(toIndex);
        return (Math.Min(fromIndex, toIndex), Math.Max(fromIndex, toIndex));
    }

    private static List<int> MergeRange(IEnumerable<int> kept, int first, int last)
    {
        List<int> indices = [.. kept];
        HashSet<int> keptSet = [.. indices];
        for (int index = first; index <= last; index++)
        {
            if (!keptSet.Contains(index))
            {
                indices.Add(index);
            }
        }

        return indices;
    }
}
