using Cerneala.Drawing.Text;

namespace Cerneala.UI.Text;

internal static class TextLineMetrics
{
    public static float MeasureLineHeight(TextAspect aspect, ResolvedTextFont font)
    {
        ArgumentNullException.ThrowIfNull(font);

        if (TextShaper.Default.TryMeasureLineHeight(aspect.ToDrawTextRun(font, "Ag"), out float lineHeight))
        {
            return lineHeight;
        }

        return aspect.FontSize * aspect.Scale;
    }
}
