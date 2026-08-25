namespace Cerneala.Drawing;

public readonly record struct DrawCornerRadius
{
    public DrawCornerRadius(float uniformRadius)
        : this(uniformRadius, uniformRadius, uniformRadius, uniformRadius)
    {
    }

    public DrawCornerRadius(
        float topLeft,
        float topRight,
        float bottomRight,
        float bottomLeft)
    {
        Validate(topLeft, nameof(topLeft));
        Validate(topRight, nameof(topRight));
        Validate(bottomRight, nameof(bottomRight));
        Validate(bottomLeft, nameof(bottomLeft));
        TopLeft = topLeft;
        TopRight = topRight;
        BottomRight = bottomRight;
        BottomLeft = bottomLeft;
    }

    public float TopLeft { get; }

    public float TopRight { get; }

    public float BottomRight { get; }

    public float BottomLeft { get; }

    public DrawCornerRadius Normalize(DrawRect bounds)
    {
        float scale = 1;
        IncludeScale(bounds.Width, TopLeft + TopRight, ref scale);
        IncludeScale(bounds.Width, BottomLeft + BottomRight, ref scale);
        IncludeScale(bounds.Height, TopLeft + BottomLeft, ref scale);
        IncludeScale(bounds.Height, TopRight + BottomRight, ref scale);
        return scale >= 1
            ? this
            : new DrawCornerRadius(
                TopLeft * scale,
                TopRight * scale,
                BottomRight * scale,
                BottomLeft * scale);
    }

    private static void IncludeScale(float available, float requested, ref float scale)
    {
        if (requested > 0)
        {
            scale = MathF.Min(scale, available / requested);
        }
    }

    private static void Validate(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
