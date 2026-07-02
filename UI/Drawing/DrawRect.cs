namespace Cerneala.Drawing;

public readonly record struct DrawRect(float X, float Y, float Width, float Height)
{
    public float Right => X + Width;

    public float Bottom => Y + Height;
}
