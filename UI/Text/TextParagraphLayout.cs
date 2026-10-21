using Cerneala.Drawing;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Text;

// The laid out lines of a paragraph. Layout is the Drawing engine result; the brush of a layout run is a placeholder,
// the renderer takes the brush of the span that DrawTextLayoutRun.SpanIndex names.
public sealed class TextParagraphLayout
{
    public TextParagraphLayout(LayoutSize size, int lineCount, DrawTextLayout layout, string renderIdentity)
    {
        if (lineCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lineCount), "Line count cannot be negative.");
        }

        Size = size.ClampNonNegative();
        LineCount = lineCount;
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        RenderIdentity = string.IsNullOrWhiteSpace(renderIdentity)
            ? throw new ArgumentException("Render identity cannot be empty.", nameof(renderIdentity))
            : renderIdentity;
    }

    public LayoutSize Size { get; }

    public int LineCount { get; }

    public DrawTextLayout Layout { get; }

    internal string RenderIdentity { get; }
}
