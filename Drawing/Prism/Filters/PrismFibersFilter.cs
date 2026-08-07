using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismFibersFilter
{
    public static Vector4 ApplyPixel(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4 center,
        int x,
        int y)
    {
        uint seed = PrismCatalogFilterMath.Seed(plan, "Seed") +
            unchecked((uint)pass.Iteration * 0x9e3779b9u);
        float variance = MathF.Max(
            0.0001f,
            PrismCatalogFilterMath.Option(plan, "Variance", 16));
        float strength = PrismCatalogFilterMath.Option(
            plan,
            "Strength",
            4);
        float noise = PrismFibersNoise.Sample(
            x + 0.5f,
            y + 0.5f,
            variance,
            strength,
            seed);
        Vector4 foreground = PrismCatalogFilterMath.OptionVector(
            plan,
            "Foreground",
            new Vector4(0, 0, 0, 1));
        Vector4 background = PrismCatalogFilterMath.OptionVector(
            plan,
            "Background",
            Vector4.One);
        return PrismCatalogFilterMath.Associated(
            Vector3.Lerp(
                new Vector3(background.X, background.Y, background.Z),
                new Vector3(foreground.X, foreground.Y, foreground.Z),
                noise),
            center.W);
    }
}
