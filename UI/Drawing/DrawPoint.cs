namespace Cerneala.Drawing;

public readonly record struct DrawPoint
{
    public DrawPoint(float x, float y)
    {
        DrawArgument.ThrowIfNotFinite(x, nameof(x));
        DrawArgument.ThrowIfNotFinite(y, nameof(y));

        X = x;
        Y = y;
    }

    public float X { get; }

    public float Y { get; }
}
