namespace Cerneala.UI.Layout.Panels;

public sealed class ColumnDefinition
{
    private GridLength width = GridLength.Star;
    private Grid? owner;

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
            if (width == value)
            {
                return;
            }

            width = value;
            owner?.InvalidateDefinitions("Grid column definition width changed");
        }
    }

    internal void Attach(Grid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        if (owner is not null && !ReferenceEquals(owner, grid))
        {
            throw new InvalidOperationException("ColumnDefinition cannot be shared across grids.");
        }

        owner = grid;
    }

    internal void Detach(Grid grid)
    {
        if (ReferenceEquals(owner, grid))
        {
            owner = null;
        }
    }
}
