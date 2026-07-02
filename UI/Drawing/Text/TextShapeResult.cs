using Cerneala.Drawing;

namespace Cerneala.Drawing.Text;

public readonly record struct TextShapeResult
{
    public TextShapeResult(string text, int glyphCount)
        : this(text, glyphCount, Array.Empty<ushort>(), Array.Empty<DrawPoint>())
    {
    }

    public TextShapeResult(string text, int glyphCount, ushort[] glyphIds, DrawPoint[] glyphPositions)
    {
        Text = text;
        GlyphCount = glyphCount;
        GlyphIds = glyphIds ?? throw new ArgumentNullException(nameof(glyphIds));
        GlyphPositions = glyphPositions ?? throw new ArgumentNullException(nameof(glyphPositions));
    }

    public string Text { get; }

    public int GlyphCount { get; }

    public ushort[] GlyphIds { get; }

    public DrawPoint[] GlyphPositions { get; }
}
