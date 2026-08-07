using System.Numerics;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismLensFlareFilter
{
    public static Vector4[] Render(
        PrismCatalogFilterPlan plan,
        PrismLensProfileResource profile,
        int width,
        int height)
    {
        Vector4 center = plan.GetOption("Center");
        float brightness = MathF.Max(
            0,
            plan.GetOption("Brightness").X);
        return PrismLensFlareRenderer.Render(
            profile,
            width,
            height,
            new Vector2(center.X, center.Y),
            brightness);
    }

    public static Vector4[] Composite(
        Vector4[] source,
        Vector4[] flare)
    {
        if (source.Length != flare.Length)
        {
            throw new ArgumentException(
                "The lens-flare buffer must match the source buffer.",
                nameof(flare));
        }

        Vector4[] output = new Vector4[source.Length];
        for (int index = 0; index < output.Length; index++)
        {
            float alpha = Math.Clamp(source[index].W, 0, 1);
            Vector3 color = Vector3.Clamp(
                new Vector3(
                    source[index].X,
                    source[index].Y,
                    source[index].Z) +
                new Vector3(
                    flare[index].X,
                    flare[index].Y,
                    flare[index].Z) * alpha,
                Vector3.Zero,
                new Vector3(alpha));
            output[index] = new Vector4(color, alpha);
        }
        return output;
    }
}
