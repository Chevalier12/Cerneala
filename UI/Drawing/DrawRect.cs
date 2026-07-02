namespace Cerneala.Drawing;

public readonly record struct DrawRect
{
    public DrawRect(float x, float y, float width, float height)
    {
        DrawArgument.ThrowIfNotValidPixelCoordinate(x, nameof(x));
        DrawArgument.ThrowIfNotValidPixelCoordinate(y, nameof(y));
        DrawArgument.ThrowIfNegativeOrNotValidPixelSize(width, nameof(width));
        DrawArgument.ThrowIfNegativeOrNotValidPixelSize(height, nameof(height));
        DrawArgument.ThrowIfNotValidPixelCoordinate(x + width, nameof(width));
        DrawArgument.ThrowIfNotValidPixelCoordinate(y + height, nameof(height));

        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public float X { get; }

    public float Y { get; }

    public float Width { get; }

    public float Height { get; }

    public float Right => X + Width;

    public float Bottom => Y + Height;
}
