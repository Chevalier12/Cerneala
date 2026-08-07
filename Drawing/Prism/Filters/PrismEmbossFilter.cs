using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismEmbossFilter
{
    public static Vector4 ApplyPixel(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Vector4 center)
    {
        float angle = PrismCatalogFilterMath.Option(
            plan,
            "Angle",
            135) * (MathF.PI / 180);
        Vector2 gradient =
            PrismCatalogFilterMath.DirectionalReliefGradient(
                source,
                width,
                height,
                x,
                y,
                pass.RadiusX,
                pass.RadiusY);
        float directionalRelief = Vector2.Dot(
            gradient,
            new Vector2(MathF.Cos(angle), MathF.Sin(angle)));
        float amount = PrismCatalogFilterMath.Option(
            plan,
            "Amount",
            1);
        return PrismCatalogFilterMath.Associated(
            new Vector3(
                Math.Clamp(
                    0.5f + (directionalRelief * amount),
                    0,
                    1)),
            center.W);
    }
}
