namespace Cerneala.UI.Layout;

public readonly record struct LayoutRect(float X, float Y, float Width, float Height)
{
    public static LayoutRect Empty { get; } = new(0, 0, 0, 0);

    public LayoutPoint Location => new(X, Y);

    public LayoutSize Size => new(Width, Height);
}
