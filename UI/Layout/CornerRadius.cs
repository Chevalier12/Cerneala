namespace Cerneala.UI.Layout;

public readonly record struct CornerRadius(float TopLeft, float TopRight, float BottomRight, float BottomLeft)
{
    public static CornerRadius Zero { get; } = new(0, 0, 0, 0);

    public CornerRadius(float uniform)
        : this(uniform, uniform, uniform, uniform)
    {
    }
}
