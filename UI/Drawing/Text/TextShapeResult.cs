using Cerneala.Drawing;

namespace Cerneala.Drawing.Text;

public readonly record struct TextShapeResult
{
    private readonly string? _text;
    private readonly ushort[] _glyphIds;
    private readonly DrawPoint[] _glyphPositions;

    public TextShapeResult(string text, int glyphCount)
        : this(text, glyphCount, CreateGlyphIds(glyphCount), CreateGlyphPositions(glyphCount))
    {
    }

    public TextShapeResult(string text, int glyphCount, ushort[] glyphIds, DrawPoint[] glyphPositions)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(glyphCount);

        _text = text ?? throw new ArgumentNullException(nameof(text));
        ArgumentNullException.ThrowIfNull(glyphIds);
        ArgumentNullException.ThrowIfNull(glyphPositions);

        if (glyphIds.Length != glyphCount)
        {
            throw new ArgumentException("Glyph id count must match glyph count.", nameof(glyphIds));
        }

        if (glyphPositions.Length != glyphCount)
        {
            throw new ArgumentException("Glyph position count must match glyph count.", nameof(glyphPositions));
        }

        _glyphIds = (ushort[])glyphIds.Clone();
        _glyphPositions = (DrawPoint[])glyphPositions.Clone();
        GlyphCount = glyphCount;
    }

    public string Text => _text ?? string.Empty;

    public int GlyphCount { get; }

    public ushort[] GlyphIds => _glyphIds is null ? Array.Empty<ushort>() : (ushort[])_glyphIds.Clone();

    public DrawPoint[] GlyphPositions => _glyphPositions is null ? Array.Empty<DrawPoint>() : (DrawPoint[])_glyphPositions.Clone();

    private static ushort[] CreateGlyphIds(int glyphCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(glyphCount);
        ThrowIfGlyphCountCannotBeAllocated(glyphCount);
        return new ushort[glyphCount];
    }

    private static DrawPoint[] CreateGlyphPositions(int glyphCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(glyphCount);
        ThrowIfGlyphCountCannotBeAllocated(glyphCount);
        return new DrawPoint[glyphCount];
    }

    private static void ThrowIfGlyphCountCannotBeAllocated(int glyphCount)
    {
        if (glyphCount > Array.MaxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(glyphCount), "Glyph count is too large.");
        }
    }
}
