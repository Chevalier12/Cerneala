using Cerneala.UI.Layout;

namespace Cerneala.UI.Text;

public sealed class TextMeasureResult
{
    public TextMeasureResult(LayoutSize size, int lineCount, TextLayoutKey cacheKey, string resolvedFontIdentity, IReadOnlyList<TextLine> lines)
    {
        if (lineCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lineCount), "Line count cannot be negative.");
        }

        Size = size.ClampNonNegative();
        LineCount = lineCount;
        CacheKey = cacheKey;
        ResolvedFontIdentity = string.IsNullOrWhiteSpace(resolvedFontIdentity)
            ? throw new ArgumentException("Resolved font identity cannot be empty.", nameof(resolvedFontIdentity))
            : resolvedFontIdentity;
        Lines = lines ?? throw new ArgumentNullException(nameof(lines));
        RenderIdentity = cacheKey.ToString();
    }

    public LayoutSize Size { get; }

    public int LineCount { get; }

    public TextLayoutKey CacheKey { get; }

    public string ResolvedFontIdentity { get; }

    public IReadOnlyList<TextLine> Lines { get; }

    internal string RenderIdentity { get; }
}
