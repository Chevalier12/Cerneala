namespace Cerneala.UI.Layout;

public readonly record struct LayoutSize(float Width, float Height)
{
    public static LayoutSize Zero { get; } = new(0, 0);

    public static LayoutSize Unconstrained { get; } = new(float.PositiveInfinity, float.PositiveInfinity);

    public bool IsWidthUnconstrained => float.IsPositiveInfinity(Width);

    public bool IsHeightUnconstrained => float.IsPositiveInfinity(Height);

    public LayoutSize ClampNonNegative()
    {
        return new LayoutSize(MathF.Max(0, Width), MathF.Max(0, Height));
    }
}
