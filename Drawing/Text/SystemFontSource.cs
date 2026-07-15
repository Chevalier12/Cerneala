using Cerneala.Drawing;
using SkiaSharp;
using System.Collections.Concurrent;

namespace Cerneala.Drawing.Text;

public sealed class SystemFontSource : IFontSource
{
    private static readonly ConcurrentDictionary<string, SKTypeface> Typefaces = new(StringComparer.OrdinalIgnoreCase);
    private static readonly (string Suffix, SKFontStyleWeight Weight)[] NamedWeights =
    [
        ("Extra Light", SKFontStyleWeight.ExtraLight),
        ("ExtraLight", SKFontStyleWeight.ExtraLight),
        ("Semi Light", (SKFontStyleWeight)350),
        ("SemiLight", (SKFontStyleWeight)350),
        ("Semi Bold", SKFontStyleWeight.SemiBold),
        ("SemiBold", SKFontStyleWeight.SemiBold),
        ("Extra Bold", SKFontStyleWeight.ExtraBold),
        ("ExtraBold", SKFontStyleWeight.ExtraBold),
        ("Thin", SKFontStyleWeight.Thin),
        ("Light", SKFontStyleWeight.Light),
        ("Medium", SKFontStyleWeight.Medium),
        ("Bold", SKFontStyleWeight.Bold),
        ("Black", SKFontStyleWeight.Black)
    ];

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
            static name => ResolveTypeface(name) ?? SKTypeface.Default);
        return new SkiaFont(typeface, familyName, size);
    }

    private static SKTypeface? ResolveTypeface(string name)
    {
        SKTypeface? exactMatch = SKFontManager.Default.MatchFamily(name);
        if (exactMatch is not null)
        {
            return exactMatch;
        }

        foreach ((string suffix, SKFontStyleWeight weight) in NamedWeights)
        {
            string marker = $" {suffix}";
            if (!name.EndsWith(marker, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string familyName = name[..^marker.Length].TrimEnd();
            if (familyName.Length == 0)
            {
                continue;
            }

            SKFontStyle style = new(weight, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
            SKTypeface? weightedMatch = SKFontManager.Default.MatchFamily(familyName, style);
            if (weightedMatch is not null)
            {
                return weightedMatch;
            }
        }

        return null;
    }
}
