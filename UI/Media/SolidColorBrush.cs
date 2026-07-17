using Cerneala.Drawing;

namespace Cerneala.UI.Media;

public sealed record SolidColorBrush : Brush
{
    private readonly SolidDrawBrushDescriptor descriptor;

    public SolidColorBrush(Color color, float opacity = 1)
        : base(opacity)
    {
        Color = color;
        descriptor = new SolidDrawBrushDescriptor(color, opacity);
    }

    public Color Color { get; }

    public override DrawBrushKind Kind => DrawBrushKind.SolidColor;

    public override Color? SolidColor => Color;

    protected override DrawBrushDescriptor CreateDescriptor()
    {
        return descriptor;
    }
}
