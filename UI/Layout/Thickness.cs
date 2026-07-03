namespace Cerneala.UI.Layout;

public readonly record struct Thickness(float Left, float Top, float Right, float Bottom)
{
    public static Thickness Zero { get; } = new(0, 0, 0, 0);

    public Thickness(float uniform)
        : this(uniform, uniform, uniform, uniform)
    {
    }

    public float Horizontal => Left + Right;

    public float Vertical => Top + Bottom;
}
