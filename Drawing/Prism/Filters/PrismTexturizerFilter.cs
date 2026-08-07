using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismTexturizerFilter
{
    private const float MinimumAlpha = 0.000001f;

    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        ReadOnlySpan<Vector4> source,
        int width,
        int height,
        Func<Vector2, Vector4>? textureResource)
    {
        float scaling = Math.Clamp(MathF.Abs(plan.Options3.X), 0.125f, 16);
        float relief = Math.Clamp(MathF.Abs(plan.Options2.X), 0, 1);
        float deviceScale = MathF.Max(plan.Options6.X, 0.125f);
        int texture = Math.Clamp((int)plan.Options4.X, 0, 3);
        int lightDirection = Math.Clamp((int)plan.Options1.X, 0, 7);
        bool invert = plan.Options0.X >= 0.5f;
        Vector3 light = PrismSurfaceTexture.LightVector(lightDirection);
        float neutralIllumination = light.Z;
        Vector4[] output = new Vector4[source.Length];

        float HeightAt(int x, int y)
        {
            float value;
            if (textureResource is null)
            {
                value = PrismSurfaceTexture.Height(
                    x + 0.5f,
                    y + 0.5f,
                    scaling * deviceScale,
                    texture);
            }
            else
            {
                Vector2 uv = new(
                    Wrap((((x + 0.5f) / width) - 0.5f) / scaling + 0.5f),
                    Wrap((((y + 0.5f) / height) - 0.5f) / scaling + 0.5f));
                Vector4 sample = textureResource(uv);
                float alpha = Math.Clamp(sample.W, 0, 1);
                Vector3 straight = alpha > MinimumAlpha
                    ? new Vector3(sample.X, sample.Y, sample.Z) / alpha
                    : Vector3.Zero;
                value = Vector3.Dot(
                    Vector3.Clamp(straight, Vector3.Zero, Vector3.One),
                    new Vector3(0.2126f, 0.7152f, 0.0722f));
            }

            value = Math.Clamp(value, 0, 1);
            return invert ? 1 - value : value;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                Vector4 center = source[index];
                if (center.W <= MinimumAlpha)
                {
                    output[index] = Vector4.Zero;
                    continue;
                }


                float topLeft = HeightAt(x - 1, y - 1);
                float top = HeightAt(x, y - 1);
                float topRight = HeightAt(x + 1, y - 1);
                float left = HeightAt(x - 1, y);
                float right = HeightAt(x + 1, y);
                float bottomLeft = HeightAt(x - 1, y + 1);
                float bottom = HeightAt(x, y + 1);
                float bottomRight = HeightAt(x + 1, y + 1);
                float horizontal =
                    (3 * (topRight - topLeft)) +
                    (10 * (right - left)) +
                    (3 * (bottomRight - bottomLeft));
                float vertical =
                    (3 * (bottomLeft - topLeft)) +
                    (10 * (bottom - top)) +
                    (3 * (bottomRight - topRight));
                horizontal /= 16;
                vertical /= 16;

                float strength = relief * 24;
                Vector3 normal = Vector3.Normalize(
                    new Vector3(-horizontal * strength, -vertical * strength, 1));
                float illumination = Vector3.Dot(normal, light);
                float shade = Math.Clamp(
                    1 + ((illumination - neutralIllumination) * 1.6f),
                    0.25f,
                    1.75f);
                Vector3 straight = Vector3.Clamp(
                    new Vector3(center.X, center.Y, center.Z) / center.W * shade,
                    Vector3.Zero,
                    Vector3.One);
                output[index] = new Vector4(straight * center.W, center.W);
            }
        }

        return output;
    }

    private static float Wrap(float value) =>
        value - MathF.Floor(value);
}
