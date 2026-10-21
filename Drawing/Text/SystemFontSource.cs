using Cerneala.Drawing;
using SkiaSharp;
using System.Collections.Concurrent;

namespace Cerneala.Drawing.Text;

public sealed class SystemFontSource : IFontSource
{
    private static readonly ConcurrentDictionary<string, SKTypeface> Typefaces = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<StyledTypefaceKey, SKTypeface> StyledTypefaces = new();
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

    public IDrawFont LoadFont(string familyName, float size, FontWeight weight, FontStyle style, FontStretch stretch)
    {
        ArgumentNullException.ThrowIfNull(familyName);
        if (!Enum.IsDefined(weight))
        {
            throw new ArgumentOutOfRangeException(nameof(weight));
        }

        if (!Enum.IsDefined(style))
        {
            throw new ArgumentOutOfRangeException(nameof(style));
        }

        if (!Enum.IsDefined(stretch))
        {
            throw new ArgumentOutOfRangeException(nameof(stretch));
        }

        if (weight == FontWeight.Normal && style == FontStyle.Normal && stretch == FontStretch.Normal)
        {
            return LoadFont(familyName, size);
        }

        if (string.IsNullOrWhiteSpace(familyName))
        {
            throw new ArgumentException("Font family name cannot be empty.", nameof(familyName));
        }

        DrawArgument.ThrowIfNotValidTextSize(size, nameof(size));

        SKTypeface typeface = StyledTypefaces.GetOrAdd(
            new StyledTypefaceKey(familyName, weight, style, stretch),
            static key => ResolveStyledTypeface(key.FamilyName, key.Weight, key.Style, key.Stretch));
        return new SkiaFont(typeface, familyName, size);
    }

    // The cache key of a styled typeface; the family name compares without regard to case, like the other caches here.
    private readonly record struct StyledTypefaceKey(string FamilyName, FontWeight Weight, FontStyle Style, FontStretch Stretch)
    {
        public bool Equals(StyledTypefaceKey other) =>
            Weight == other.Weight &&
            Style == other.Style &&
            Stretch == other.Stretch &&
            string.Equals(FamilyName, other.FamilyName, StringComparison.OrdinalIgnoreCase);

        public override int GetHashCode() =>
            HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(FamilyName), Weight, Style, Stretch);
    }

    private static SKTypeface ResolveStyledTypeface(string name, FontWeight weight, FontStyle style, FontStretch stretch)
    {
        SKFontStyleSlant slant = style switch
        {
            FontStyle.Italic => SKFontStyleSlant.Italic,
            FontStyle.Oblique => SKFontStyleSlant.Oblique,
            _ => SKFontStyleSlant.Upright
        };
        SKFontStyle fontStyle = new((int)weight, (int)stretch, slant);
        SKTypeface? exactFamily = SKFontManager.Default.MatchFamily(name, fontStyle);
        if (exactFamily is not null)
        {
            return exactFamily;
        }

        // The system does not know the name as a family: LoadFont(name, size) uses the typeface of a weighted name
        // ("Segoe UI Bold") or the default typeface. The requested style applies to the family of that typeface; a
        // weight that the name already carries stays when the request leaves the weight at Normal.
        SKTypeface baseTypeface = ResolveTypeface(name) ?? SKTypeface.Default;
        if (weight == FontWeight.Normal)
        {
            fontStyle = new SKFontStyle(baseTypeface.FontStyle.Weight, (int)stretch, slant);
        }

        return SKFontManager.Default.MatchFamily(baseTypeface.FamilyName, fontStyle) ?? baseTypeface;
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
