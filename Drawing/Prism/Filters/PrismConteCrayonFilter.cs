using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismConteCrayonFilter
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
            etfRadius: 3,
            refinements: 1,
            sigma: Math.Clamp(plan.Options8.X, 0.5f, 4),
            extendedSigma: Math.Clamp(plan.Options8.Y, 0.75f, 6.4f),
            normalRadius: Math.Clamp((int)plan.Options8.Z, 2, 8),
            rho: 0.98f,
            flowRadius: Math.Clamp((int)plan.Options8.W, 3, 8));

        float foregroundLevel = Math.Clamp(plan.Options3.X / 20, 0, 1);
        float backgroundLevel = Math.Clamp(plan.Options1.X / 20, 0, 1);
        float scale = Math.Clamp(plan.Options6.X, 0.125f, 16);
        float relief = Math.Clamp(plan.Options5.X, 0, 2);
        int texture = Math.Clamp((int)plan.Options7.X, 0, 3);
        int lightDirection = Math.Clamp((int)plan.Options4.X, 0, 7);
        Vector3 foreground = Vector3.Clamp(
            new Vector3(plan.Options2.X, plan.Options2.Y, plan.Options2.Z),
            Vector3.Zero,
            Vector3.One);
        Vector3 background = Vector3.Clamp(
            new Vector3(plan.Options0.X, plan.Options0.Y, plan.Options0.Z),
            Vector3.Zero,
            Vector3.One);
        Vector3 light = PrismSurfaceTexture.LightVector(lightDirection);
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

                Vector2 pixel = new(x + 0.5f, y + 0.5f);
                Vector2 tangent = flow.Tangents[index];
                float darkness = 1 - flow.Luminance[index];
                float edgeThreshold = float.Lerp(
                    0.052f,
                    0.011f,
                    foregroundLevel);
                float lineMask = SmoothStep(
                    edgeThreshold * 0.28f,
                    edgeThreshold,
                    MathF.Abs(flow.Response[index]));
                float hatch = FourLayerHatch(
                    pixel,
                    tangent,
                    darkness,
                    backgroundLevel,
                    scale);
                float paper = PrismSurfaceTexture.Height(
                    pixel.X,
                    pixel.Y,
                    scale,
                    texture);
                float tooth = (paper - 0.5f) * 2;
                float lineCoverage = lineMask *
                    float.Lerp(0.48f, 1, foregroundLevel);
                float toneCoverage = hatch *
                    float.Lerp(0.42f, 0.95f, backgroundLevel);
                float coverage = Math.Clamp(
                    MathF.Max(lineCoverage, toneCoverage) *
                    (1 + (tooth * float.Lerp(0.12f, 0.34f, backgroundLevel))),
                    0,
                    1);

                float horizontal =
                    PrismSurfaceTexture.Height(
                        pixel.X + 1,
                        pixel.Y,
                        scale,
                        texture) -
                    PrismSurfaceTexture.Height(
                        pixel.X - 1,
                        pixel.Y,
                        scale,
                        texture);
                float vertical =
                    PrismSurfaceTexture.Height(
                        pixel.X,
                        pixel.Y + 1,
                        scale,
                        texture) -
                    PrismSurfaceTexture.Height(
                        pixel.X,
                        pixel.Y - 1,
                        scale,
                        texture);
                Vector3 normal = Vector3.Normalize(
                    new Vector3(
                        -horizontal * relief * 2.4f,
                        -vertical * relief * 2.4f,
                        1));
                float illumination = Math.Clamp(Vector3.Dot(normal, light), 0, 1);
                float paperShade = float.Lerp(
                    1,
                    0.78f + (0.36f * illumination),
                    Math.Clamp(relief, 0, 1));
                Vector3 straight = Vector3.Lerp(
                    background,
                    foreground,
                    coverage);
                straight = Vector3.Clamp(
                    straight * paperShade,
                    Vector3.Zero,
                    Vector3.One);
                output[index] = new Vector4(straight * alpha, alpha);
            }
        }

        return output;
    }

    private static float FourLayerHatch(
        Vector2 pixel,
        Vector2 tangent,
        float darkness,
        float level,
        float scale)
    {
        float bias = (level - 0.35f) * 0.2f;
        float first = HatchLayer(pixel, tangent, 0, scale * 4.6f, 0.2f, 11) *
            SmoothStep(0.12f - bias, 0.34f - bias, darkness);
        float second = HatchLayer(pixel, tangent, 0.7853982f, scale * 5.2f, 0.19f, 23) *
            SmoothStep(0.3f - bias, 0.5f - bias, darkness);
        float third = HatchLayer(pixel, tangent, 1.5707963f, scale * 5.8f, 0.18f, 37) *
            SmoothStep(0.5f - bias, 0.7f - bias, darkness);
        float fourth = HatchLayer(pixel, tangent, -0.7853982f, scale * 6.4f, 0.17f, 53) *
            SmoothStep(0.68f - bias, 0.88f - bias, darkness);
        return Math.Clamp(
            1 -
            ((1 - first) *
                (1 - second) *
                (1 - third) *
                (1 - fourth)),
            0,
            1);
    }

    private static float HatchLayer(
        Vector2 pixel,
        Vector2 tangent,
        float angle,
        float spacing,
        float width,
        uint seed)
    {
        float cosine = MathF.Cos(angle);
        float sine = MathF.Sin(angle);
        Vector2 direction = new(
            (tangent.X * cosine) - (tangent.Y * sine),
            (tangent.X * sine) + (tangent.Y * cosine));
        Vector2 normal = new(-direction.Y, direction.X);
        float phase = PrismSurfaceTexture.ValueNoise(
            pixel.X / (spacing * 3),
            pixel.Y / (spacing * 3),
            seed) * 0.45f;
        float coordinate = (Vector2.Dot(pixel, normal) / spacing) + phase;
        float distance = MathF.Abs(
            (coordinate - MathF.Floor(coordinate)) - 0.5f) * 2;
        return 1 - SmoothStep(width, width + 0.22f, distance);
    }

    private static float SmoothStep(float start, float end, float value)
    {
        float amount = Math.Clamp((value - start) / (end - start), 0, 1);
        return amount * amount * (3 - (2 * amount));
    }
}
