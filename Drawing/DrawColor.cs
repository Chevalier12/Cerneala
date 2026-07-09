namespace Cerneala.Drawing;

public readonly record struct DrawColor(byte R, byte G, byte B, byte A = 255)
{
    public static DrawColor Transparent { get; } = new(0, 0, 0, 0);

    public static DrawColor White { get; } = new(255, 255, 255);

    public static DrawColor Black { get; } = new(0, 0, 0);
}
