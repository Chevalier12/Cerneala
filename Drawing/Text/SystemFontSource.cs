using Cerneala.Drawing;
using SkiaSharp;
using System.Collections.Concurrent;

namespace Cerneala.Drawing.Text;

public sealed class SystemFontSource : IFontSource
{
    private static readonly ConcurrentDictionary<string, SKTypeface> Typefaces = new(StringComparer.OrdinalIgnoreCase);

    public IDrawFont LoadFont(string familyName, float size)
    {
        ArgumentNullException.ThrowIfNull(familyName);
        if (string.IsNullOrWhiteSpace(familyName))
        {
            throw new ArgumentException("Font family name cannot be empty.", nameof(familyName));
        }

        DrawArgument.ThrowIfNotValidTextSize(size, nameof(size));

        SKTypeface typeface = Typefaces.GetOrAdd(
            familyName,
            static name => SKFontManager.Default.MatchFamily(name) ?? SKTypeface.Default);
        return new SkiaFont(typeface, familyName, size);
    }
}
