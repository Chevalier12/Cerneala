namespace Cerneala.UI.Layout.Panels;

public sealed class RowDefinition
{
    private GridLength height = GridLength.Star;

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
            height = value;
        }
    }
}
