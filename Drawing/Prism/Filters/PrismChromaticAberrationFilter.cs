using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismChromaticAberrationFilter
{
    public static Vector4 ApplyPixel(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        float amount = PrismCatalogFilterMath.Option(
            plan,
            "Amount",
            0);
        Vector4 direction = PrismCatalogFilterMath.OptionVector(
            plan,
            "Direction",
            new Vector4(1, 0, 0, 0));
        Vector2 offset = new(direction.X, direction.Y);
        if (offset.LengthSquared() == 0)
        {
            offset = Vector2.UnitX;
        }
        else
        {
            offset = Vector2.Normalize(offset);
        }
        if (PrismCatalogFilterMath.Option(
                plan,
                "Radial",
                0) >= 0.5f)
        {
            Vector4 centerOption =
                PrismCatalogFilterMath.OptionVector(
                    plan,
                    "Center",
                    new Vector4(0.5f, 0.5f, 0, 0));
            Vector2 uv = new(
                (x + 0.5f) / width,
                (y + 0.5f) / height);
            float modulation = Vector2.Distance(
                uv,
                new Vector2(centerOption.X, centerOption.Y)) * 2;
            offset *= modulation;
        }
        offset *= amount;
        Vector4 red = PrismCatalogFilterMath.SamplePixelBilinear(
            source,
            width,
            height,
            x + offset.X,
            y + offset.Y);
        Vector4 center = PrismCatalogFilterMath.SamplePixel(
            source,
            width,
            height,
            x,
            y);
        Vector4 blue = PrismCatalogFilterMath.SamplePixelBilinear(
            source,
            width,
            height,
            x - offset.X,
            y - offset.Y);
        return new Vector4(
            red.X,
            center.Y,
            blue.Z,
            MathF.Max(center.W, MathF.Max(red.W, blue.W)));
    }
}
