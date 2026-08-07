using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismGraphicPenFilter
{
    private const float MinimumAlpha = 0.000001f;

    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        ReadOnlySpan<Vector4> source,
        int width,
        int height)
    {
        PrismFlowXDogAnalysis flow = PrismCharcoalFilter.Analyze(
            source,
            width,
            height,
            etfRadius: Math.Clamp((int)plan.Options6.X, 2, 4),
            refinements: Math.Clamp((int)plan.Options6.Y, 1, 3),
            sigma: Math.Clamp(plan.Options5.X, 0.5f, 4),
            extendedSigma: Math.Clamp(plan.Options5.Y, 0.75f, 6.4f),
            normalRadius: Math.Clamp((int)plan.Options5.Z, 2, 8),
            rho: Math.Clamp(plan.Options6.Z, 0.9f, 1),
            flowRadius: Math.Clamp((int)plan.Options5.W, 3, 8));

        float strokeLength = Math.Clamp(plan.Options4.X, 1, 96);
        float balance = Math.Clamp(plan.Options2.X / 100, 0, 1);
        Vector2 direction = StrokeDirection((int)plan.Options3.X);
        Vector2 normal = new(-direction.Y, direction.X);
        Vector3 foreground = Vector3.Clamp(
            new Vector3(plan.Options1.X, plan.Options1.Y, plan.Options1.Z),
            Vector3.Zero,
            Vector3.One);
        Vector3 background = Vector3.Clamp(
            new Vector3(plan.Options0.X, plan.Options0.Y, plan.Options0.Z),
            Vector3.Zero,
            Vector3.One);
        float edgeThreshold = float.Lerp(0.12f, 0.045f, balance);
        float toneThreshold = float.Lerp(0.72f, 0.18f, balance);
        Vector4[] output = new Vector4[source.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                float alpha = flow.Alpha[index];
                if (alpha <= MinimumAlpha)
                {
                    output[index] = Vector4.Zero;
                    continue;
                }

                float line = SmoothStep(
                    edgeThreshold * 0.3f,
                    edgeThreshold,
                    MathF.Max(-flow.Response[index], 0));
                float darkness = 1 - flow.Luminance[index];
                float tone = SmoothStep(
                    toneThreshold,
                    toneThreshold + 0.22f,
                    darkness);
                float hatch = FiniteHatch(
                    new Vector2(x + 0.5f, y + 0.5f),
                    direction,
                    normal,
                    strokeLength);
                float coverage = Math.Clamp(
                    MathF.Max(line, hatch * tone),
                    0,
                    1);
                Vector3 straight = Vector3.Lerp(
                    background,
                    foreground,
                    coverage);
                output[index] = new Vector4(straight * alpha, alpha);
            }
        }

        return output;
    }

    private static float FiniteHatch(
        Vector2 pixel,
        Vector2 direction,
        Vector2 normal,
        float strokeLength)
    {
        const float spacing = 3.25f;
        float along = Vector2.Dot(pixel, direction);
        float across = Vector2.Dot(pixel, normal);
        float row = MathF.Floor(across / spacing);
        float rowCenter = (row + 0.5f) * spacing;
        float acrossDistance = MathF.Abs(across - rowCenter);
        float widthMask = 1 - SmoothStep(0.45f, 1.05f, acrossDistance);

        float gap = MathF.Max(2.5f, strokeLength * 0.3f);
        float period = strokeLength + gap;
        float phase = Fraction(row * 0.6180339f) * period;
        float segmentCoordinate = Fraction((along + phase) / period);
        float alongDistance = MathF.Abs(segmentCoordinate - 0.5f) * period;
        float segmentMask = 1 - SmoothStep(
            MathF.Max((strokeLength * 0.5f) - 0.75f, 0),
            (strokeLength * 0.5f) + 0.75f,
            alongDistance);
        return widthMask * segmentMask;
    }

    private static Vector2 StrokeDirection(int direction)
    {
        const float diagonal = 0.7071068f;
        return direction switch
        {
            1 => Vector2.UnitX,
            2 => new Vector2(diagonal, -diagonal),
            3 => Vector2.UnitY,
            _ => new Vector2(diagonal, diagonal)
        };
    }

    private static float Fraction(float value) =>
        value - MathF.Floor(value);

    private static float SmoothStep(float start, float end, float value)
    {
        float amount = Math.Clamp((value - start) / (end - start), 0, 1);
        return amount * amount * (3 - (2 * amount));
    }
}
