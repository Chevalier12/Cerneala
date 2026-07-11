using Cerneala.Drawing;

namespace Cerneala.UI.Media;

public sealed record SolidColorBrush(Color Color) : Brush
{
    public override Color? SolidColor => Color;
}
