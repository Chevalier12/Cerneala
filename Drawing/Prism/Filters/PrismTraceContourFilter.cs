using System.Numerics;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismTraceContourFilter
{
    private const float AlphaEpsilon = 0.000001f;
    private static readonly int LowerEdge =
        PrismCatalogRuntime.ResolveSymbol("Edge", "Lower");

    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        float level = Math.Clamp(plan.GetOption("Level").X, 0, 1);
        bool lower = UnpackInteger(plan.GetOption("Edge")) == LowerEdge;
        Vector4[] result = new Vector4[source.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector4 center = source[(y * width) + x];
                bool selected = IsSelected(center, level, lower);
                bool boundary = center.W > AlphaEpsilon &&
                    selected &&
                    TouchesOppositeRegion(
                    source,
                    width,
                    height,
                    x,
                    y,
                    level,
                    lower);
                float value = boundary ? 0 : center.W;
                result[(y * width) + x] = new Vector4(
                    value,
                    value,
                    value,
                    center.W);
            }
        }

        return result;
    }

    private static bool TouchesOppositeRegion(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        float level,
        bool lower)
    {
        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            int sampleY = Math.Clamp(y + offsetY, 0, height - 1);
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                if (offsetX == 0 && offsetY == 0)
                {
                    continue;
                }

                int sampleX = Math.Clamp(x + offsetX, 0, width - 1);
                Vector4 sample = source[(sampleY * width) + sampleX];
                if (sample.W <= AlphaEpsilon)
                {
                    continue;
                }

                if (!IsSelected(
                        sample,
                        level,
                        lower))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsSelected(Vector4 color, float level, bool lower)
    {
        float luminance = Luminance(color);
        return lower ? luminance < level : luminance >= level;
    }

    private static float Luminance(Vector4 color)
    {
        if (color.W <= AlphaEpsilon)
        {
            return 0;
        }

        Vector3 straight = new(color.X, color.Y, color.Z);
        straight /= color.W;
        return
            (straight.X * 0.2126f) +
            (straight.Y * 0.7152f) +
            (straight.Z * 0.0722f);
    }

    private static int UnpackInteger(Vector4 value)
    {
        uint low = (uint)Math.Clamp(MathF.Round(value.X), 0, 65535);
        uint high = (uint)Math.Clamp(MathF.Round(value.Y), 0, 65535);
        return unchecked((int)(low | (high << 16)));
    }
}
