using Cerneala.Drawing;
using SkiaSharp;

namespace Cerneala.Text;

public sealed class SkiaFont : IDrawFont
{
    public SkiaFont(SKTypeface typeface, float size)
    {
        Typeface = typeface ?? throw new ArgumentNullException(nameof(typeface));
        Size = size;
    }

    public SKTypeface Typeface { get; }

    public float Size { get; }
}
