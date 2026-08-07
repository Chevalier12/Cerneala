using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismCustomConvolutionFilter
{
    public static Vector4 ApplyPixel(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Func<Vector2, Vector4>? kernel)
    {
        Vector4 total = Vector4.Zero;
        int edgeMode = (int)PrismCatalogFilterMath.Option(
            plan,
            "EdgeMode",
            0);
        for (int kernelY = -1; kernelY <= 1; kernelY++)
        {
            for (int kernelX = -1; kernelX <= 1; kernelX++)
            {
                Vector2 kernelUv = new(
                    (kernelX + 1.5f) / 3,
                    (kernelY + 1.5f) / 3);
                float weight = kernel?.Invoke(kernelUv).X ??
                    (kernelX == 0 && kernelY == 0 ? 1 : 0);
                total += PrismCatalogFilterMath.SampleConvolutionPixel(
                    source,
                    width,
                    height,
                    x + kernelX,
                    y + kernelY,
                    edgeMode) * weight;
            }
        }

        float scale = PrismCatalogFilterMath.Option(
            plan,
            "Scale",
            1);
        Vector4 result = total * scale +
            new Vector4(
                PrismCatalogFilterMath.Option(
                    plan,
                    "Offset",
                    0));
        if (PrismCatalogFilterMath.Option(
                plan,
                "AffectAlpha",
                0) < 0.5f)
        {
            result.W = PrismCatalogFilterMath.SamplePixel(
                source,
                width,
                height,
                x,
                y).W;
        }
        return result;
    }
}
