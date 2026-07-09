using Cerneala.Drawing;
using SkiaSharp;

namespace Cerneala.Drawing.Text;

public sealed class SkiaFont : IDrawFont
{
    public SkiaFont(SKTypeface typeface, string familyName, float size)
    {
        DrawArgument.ThrowIfNotValidTextSize(size, nameof(size));

        Typeface = typeface ?? throw new ArgumentNullException(nameof(typeface));
        FamilyName = familyName ?? throw new ArgumentNullException(nameof(familyName));
        if (string.IsNullOrWhiteSpace(FamilyName))
        {
            throw new ArgumentException("Font family name cannot be empty.", nameof(familyName));
        }

        Size = size;
    }

    public SKTypeface Typeface { get; }

    public string FamilyName { get; }

    public float Size { get; }
}
