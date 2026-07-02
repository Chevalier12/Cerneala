using Cerneala.Drawing;
using SkiaSharp;

namespace Cerneala.Text;

public sealed class SystemFontSource : IFontSource
{
    public IDrawFont LoadFont(string familyName, float size)
    {
        ArgumentNullException.ThrowIfNull(familyName);

        SKTypeface typeface = SKFontManager.Default.MatchFamily(familyName) ?? SKTypeface.Default;
        return new SkiaFont(typeface, size);
    }
}
