using Cerneala.Drawing;

namespace Cerneala.UI.Text;

public sealed class FontResolver
{
    private readonly IFontSource? fontSource;

    public FontResolver()
    {
    }

    public FontResolver(IFontSource fontSource)
    {
        this.fontSource = fontSource ?? throw new ArgumentNullException(nameof(fontSource));
    }

    public static FontResolver Default { get; } = new();

    public ResolvedTextFont Resolve(string familyName, float size)
    {
        if (string.IsNullOrWhiteSpace(familyName))
        {
            throw new ArgumentException("Font family cannot be empty.", nameof(familyName));
        }

        if (size <= 0 || !float.IsFinite(size))
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Font size must be positive and finite.");
        }

        IDrawFont font = fontSource is null
            ? new FallbackDrawFont(familyName, size)
            : fontSource.LoadFont(familyName, size);
        return new ResolvedTextFont(font);
    }

    private sealed class FallbackDrawFont : IDrawFont
    {
        public FallbackDrawFont(string familyName, float size)
        {
            FamilyName = familyName;
            Size = size;
        }

        public string FamilyName { get; }

        public float Size { get; }
    }
}
