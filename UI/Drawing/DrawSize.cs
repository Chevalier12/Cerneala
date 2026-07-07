namespace Cerneala.Drawing;

public readonly record struct DrawSize
{
    public DrawSize(float width, float height)
    {
        DrawArgument.ThrowIfNotFinite(width, nameof(width));
        DrawArgument.ThrowIfNotFinite(height, nameof(height));

        Width = width;
        Height = height;
    }

    public float Width { get; }

    public float Height { get; }
}
