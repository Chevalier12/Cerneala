using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismCloudsFilter
{
    public static Vector4 ApplyPixel(
        PrismCatalogFilterPlan plan,
        Vector4 center,
        int x,
        int y,
        bool difference = false)
    {
        float scale = MathF.Max(
            0.0001f,
            PrismCatalogFilterMath.Option(plan, "Scale", 1));
        float noise = PrismWaveNoise.Sample(
            plan.WaveNoiseTable,
            new Vector2(
                (x + 0.5f) / scale,
                (y + 0.5f) / scale),
            plan.WaveNoiseSeed,
            (int)PrismCatalogFilterMath.Option(
                plan,
                "DirectionCount",
                20),
            PrismCatalogFilterMath.Option(
                plan,
                "SliceThickness",
                4),
            PrismCatalogFilterMath.OptionVector(
                plan,
                "Anisotropy",
                new Vector4(0, 1, 0, 0)));
        Vector4 foreground = PrismCatalogFilterMath.OptionVector(
            plan,
            "Foreground",
            new Vector4(0, 0, 0, 1));
        Vector4 background = PrismCatalogFilterMath.OptionVector(
            plan,
            "Background",
            Vector4.One);
        Vector3 pattern = Vector3.Lerp(
            new Vector3(background.X, background.Y, background.Z),
            new Vector3(foreground.X, foreground.Y, foreground.Z),
            noise);
        if (difference)
        {
            pattern = Vector3.Abs(
                PrismCatalogFilterMath.Unpremultiply(center) - pattern);
        }
        return PrismCatalogFilterMath.Associated(pattern, center.W);
    }
}
