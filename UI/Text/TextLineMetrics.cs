using Cerneala.Drawing.Text;

namespace Cerneala.UI.Text;

internal static class TextLineMetrics
{
    public static float MeasureLineHeight(TextRunStyle style, ResolvedTextFont font)
    {
        ArgumentNullException.ThrowIfNull(font);

        if (TextShaper.Default.TryMeasureLineHeight(style.ToDrawTextRun(font, "Ag"), out float lineHeight))
        {
            return lineHeight;
        }

        return style.FontSize * style.Scale;
    }
}
