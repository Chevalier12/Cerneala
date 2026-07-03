namespace Cerneala.UI.Layout;

public readonly record struct LayoutPoint(float X, float Y)
{
    public static LayoutPoint Zero { get; } = new(0, 0);
}
