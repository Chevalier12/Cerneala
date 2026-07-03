namespace Cerneala.UI.Layout.Panels;

public sealed class ColumnDefinition
{
    private GridLength width = GridLength.Star;

    public ColumnDefinition()
    {
    }

    public ColumnDefinition(GridLength width)
    {
        Width = width;
    }

    public GridLength Width
    {
        get => width;
        set
        {
            value.Validate();
            width = value;
        }
    }
}
