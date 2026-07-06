namespace Cerneala.UI.Layout.Panels;

public sealed class RowDefinition
{
    private GridLength height = GridLength.Star;
    private Grid? owner;

    public RowDefinition()
    {
    }

    public RowDefinition(GridLength height)
    {
        Height = height;
    }

    public GridLength Height
    {
        get => height;
        set
        {
            value.Validate();
            if (height == value)
            {
                return;
            }

            height = value;
            owner?.InvalidateDefinitions("Grid row definition height changed");
        }
    }

    internal void Attach(Grid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        if (owner is not null && !ReferenceEquals(owner, grid))
        {
            throw new InvalidOperationException("RowDefinition cannot be shared across grids.");
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
