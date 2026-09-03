namespace Cerneala.UI.Servo;

public readonly record struct ServoPoint
{
    public ServoPoint(float x, float y)
    {
        if (!float.IsFinite(x))
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, "Servo coordinates must be finite.");
        }

        if (!float.IsFinite(y))
        {
            throw new ArgumentOutOfRangeException(nameof(y), y, "Servo coordinates must be finite.");
        }

        X = x;
        Y = y;
    }

    public float X { get; }

    public float Y { get; }
}
