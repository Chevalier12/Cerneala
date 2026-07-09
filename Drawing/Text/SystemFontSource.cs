using Cerneala.Drawing;
using SkiaSharp;

namespace Cerneala.Drawing.Text;

public sealed class SystemFontSource : IFontSource
{
    public IDrawFont LoadFont(string familyName, float size)
    {
        ArgumentNullException.ThrowIfNull(familyName);
        if (string.IsNullOrWhiteSpace(familyName))
        {
            throw new ArgumentException("Font family name cannot be empty.", nameof(familyName));
        }

        DrawArgument.ThrowIfNotValidTextSize(size, nameof(size));

        SKTypeface typeface = SKFontManager.Default.MatchFamily(familyName) ?? SKTypeface.Default;
        return new SkiaFont(typeface, familyName, size);
    }
}
