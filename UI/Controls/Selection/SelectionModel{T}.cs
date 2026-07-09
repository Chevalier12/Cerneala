namespace Cerneala.UI.Controls.Selection;

public sealed class SelectionModel<T> : SelectionModel
{
    private IReadOnlyList<T> items = [];

    public SelectionModel()
    {
    }

    public SelectionModel(IEnumerable<T> items)
    {
        SetItems(items);
    }

    public T? SelectedItem => SelectedIndex >= 0 && SelectedIndex < items.Count ? items[SelectedIndex] : default;

    public void SetItems(IEnumerable<T>? source)
    {
        items = source is null ? [] : [.. source];
        if (SelectedIndex >= items.Count)
        {
            Clear();
        }
    }

    public SelectionChangeResult SelectItem(T item)
    {
        int index = -1;
        for (int i = 0; i < items.Count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(items[i], item))
            {
                index = i;
                break;
            }
        }

        return Select(index);
    }
}
