using Cerneala.Drawing;

namespace Cerneala.UI.Media;

public sealed record SolidColorBrush(DrawColor Color) : Brush
{
    public override DrawColor? SolidColor => Color;
}
