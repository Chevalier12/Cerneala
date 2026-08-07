using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;



internal static class PrismPhotocopyFilter
{
    private const float MinimumWeight = 0.000001f;
    private const float Sharpen = 35;
    private const float Phi = 10;

    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        float sigma = Math.Clamp(plan.Options4.X, 0.5f, 3.75f);
        float extendedSigma = Math.Clamp(
            plan.Options4.Y,
            sigma + 0.25f,
            4);
        int extendedRadius = Math.Clamp(
            (int)MathF.Round(plan.Options4.Z),
            1,
            12);
        int narrowRadius = Math.Clamp(
            (int)MathF.Ceiling(sigma * 3),
            1,
            extendedRadius);
        float epsilon = Math.Clamp(plan.Options4.W, 0, 1);
        (Vector2[] narrow, Vector2[] extended) =
            PrismXDogLuminance.Build(
            source,
            width,
            height,
            sigma,
            narrowRadius,
            extendedSigma,
            extendedRadius);

        Vector4 foregroundOption = plan.Options5;
        Vector4 backgroundOption = plan.Options6;
        Vector3 foreground = Vector3.Clamp(
            new Vector3(
                foregroundOption.X,
                foregroundOption.Y,
                foregroundOption.Z),
            Vector3.Zero,
            Vector3.One);
        Vector3 background = Vector3.Clamp(
            new Vector3(
                backgroundOption.X,
                backgroundOption.Y,
                backgroundOption.Z),
            Vector3.Zero,
            Vector3.One);
        Vector4[] result = new Vector4[source.Length];
        for (int index = 0; index < source.Length; index++)
        {
            float alpha = Math.Clamp(source[index].W, 0, 1);
            if (alpha <= MinimumWeight)
            {
                result[index] = Vector4.Zero;
                continue;
            }

            float narrowLuminance =
                PrismXDogLuminance.Resolve(narrow[index]);
            float extendedLuminance =
                PrismXDogLuminance.Resolve(extended[index]);
            float response =
                ((Sharpen + 1) * narrowLuminance) -
                (Sharpen * extendedLuminance);
            float paper = response >= epsilon
                ? 1
                : Math.Clamp(
                    1 + MathF.Tanh(Phi * (response - epsilon)),
                    0,
                    1);
            Vector3 color = Vector3.Lerp(foreground, background, paper);
            result[index] = new Vector4(color * alpha, alpha);
        }
        return result;
    }
}
