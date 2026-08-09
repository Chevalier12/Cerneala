using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Definitions;
using static Cerneala.Drawing.Prism.Filters.PrismCatalogFilterMath;
using static Cerneala.Drawing.Prism.Filters.PrismCatalogQuantizationMath;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismCatalogPainterlyMath
{
    internal static Vector4[] Fresco(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        const float tensorScale = 1f / 48f;
        Vector4[] tensor = new Vector4[source.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                (float horizontal, float cross, float vertical) =
                    FacetStructureTensor(
                        source,
                        width,
                        height,
                        x,
                        y);
                tensor[(y * width) + x] = Vector4.Clamp(
                    new Vector4(
                        horizontal * tensorScale,
                        (cross * tensorScale * 0.5f) + 0.5f,
                        vertical * tensorScale,
                        1),
                    Vector4.Zero,
                    Vector4.One);
            }
        }

        tensor = BlurFrescoTensor(
            tensor,
            width,
            height,
            plan.Passes[1].RadiusX,
            horizontal: true);
        tensor = BlurFrescoTensor(
            tensor,
            width,
            height,
            plan.Passes[2].RadiusY,
            horizontal: false);

        Vector4[] output = new Vector4[source.Length];
        float radius = plan.Passes[3].RadiusX;
        float detail = plan.GetOption("BrushDetail").X;
        float texture = plan.GetOption("Texture").X;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                output[index] = FrescoKuwahara(
                    source,
                    tensor[index],
                    width,
                    height,
                    x,
                    y,
                    radius,
                    detail,
                    texture);
            }
        }

        return output;
    }

    private static Vector4[] BlurFrescoTensor(
        Vector4[] source,
        int width,
        int height,
        float requestedRadius,
        bool horizontal)
    {
        float radius = Math.Clamp(requestedRadius, 1, 4);
        float sigma = MathF.Max(radius * 0.5f, 0.75f);
        float divisor = 2 * sigma * sigma;
        Vector4[] output = new Vector4[source.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector4 total = Vector4.Zero;
                float totalWeight = 0;
                for (int offset = -4; offset <= 4; offset++)
                {
                    if (MathF.Abs(offset) > radius)
                    {
                        continue;
                    }

                    float weight = MathF.Exp(
                        -(offset * offset) / divisor);
                    total += SamplePixel(
                        source,
                        width,
                        height,
                        x + (horizontal ? offset : 0),
                        y + (horizontal ? 0 : offset)) * weight;
                    totalWeight += weight;
                }

                output[(y * width) + x] =
                    total / MathF.Max(totalWeight, 0.000001f);
            }
        }

        return output;
    }

    private static Vector4 FrescoKuwahara(
        Vector4[] source,
        Vector4 encodedTensor,
        int width,
        int height,
        int x,
        int y,
        float requestedRadius,
        float requestedDetail,
        float requestedTexture)
    {
        const float diagonal = 0.7071067811865476f;
        const float gamma = 0.5890486225480862f;
        Vector4 center = SamplePixel(source, width, height, x, y);
        if (center.W <= 0)
        {
            return Vector4.Zero;
        }

        float tensorX = encodedTensor.X;
        float tensorCross = (encodedTensor.Y - 0.5f) * 2;
        float tensorY = encodedTensor.Z;
        float difference = tensorX - tensorY;
        float discriminant = MathF.Sqrt(MathF.Max(
            (difference * difference) +
                (4 * tensorCross * tensorCross),
            0));
        float lambda1 =
            0.5f * (tensorX + tensorY + discriminant);
        float lambda2 =
            0.5f * (tensorX + tensorY - discriminant);
        float tensorEnergy = lambda1 + lambda2;
        float anisotropy = tensorEnergy <= 0.000001f
            ? 0
            : Math.Clamp(
                (lambda1 - lambda2) / tensorEnergy,
                0,
                1);
        float angle =
            (0.5f * MathF.Atan2(
                2 * tensorCross,
                difference)) +
            (MathF.PI * 0.5f);
        float cosine = MathF.Cos(angle);
        float sine = MathF.Sin(angle);
        float radius = Math.Clamp(requestedRadius, 1, 6);
        float majorRadius = radius * (1 + anisotropy);
        float minorRadius = radius / (1 + anisotropy);
        int sampleRadius = Math.Min(
            (int)MathF.Ceiling(majorRadius),
            12);
        float zeta = 2 / radius;
        float eta =
            (zeta + MathF.Cos(gamma)) /
            MathF.Max(
                MathF.Sin(gamma) * MathF.Sin(gamma),
                0.000001f);

        Span<Vector3> colorSums = stackalloc Vector3[8];
        Span<Vector3> squareSums = stackalloc Vector3[8];
        Span<float> weightSums = stackalloc float[8];
        Span<float> sectorWeights = stackalloc float[8];
        for (int offsetY = -sampleRadius;
            offsetY <= sampleRadius;
            offsetY++)
        {
            for (int offsetX = -sampleRadius;
                offsetX <= sampleRadius;
                offsetX++)
            {
                float localX =
                    ((cosine * offsetX) +
                        (sine * offsetY)) /
                    majorRadius;
                float localY =
                    ((-sine * offsetX) +
                        (cosine * offsetY)) /
                    minorRadius;
                float distanceSquared =
                    (localX * localX) +
                    (localY * localY);
                if (distanceSquared > 1)
                {
                    continue;
                }

                FacetSectorWeights(
                    localX,
                    localY,
                    zeta,
                    eta,
                    diagonal,
                    sectorWeights);
                float sectorTotal = 0;
                for (int sector = 0; sector < 8; sector++)
                {
                    sectorTotal += sectorWeights[sector];
                }
                if (sectorTotal <= 0.000001f)
                {
                    continue;
                }

                Vector4 sample = SamplePixel(
                    source,
                    width,
                    height,
                    x + offsetX,
                    y + offsetY);
                if (sample.W <= 0.000001f)
                {
                    continue;
                }

                Vector3 color = Vector3.Clamp(
                    Unpremultiply(sample),
                    Vector3.Zero,
                    Vector3.One);
                float gaussian =
                    MathF.Exp(-3.125f * distanceSquared) /
                    sectorTotal;
                float alphaStop =
                    sample.W *
                    MathF.Exp(-MathF.Abs(sample.W - center.W) * 8);
                for (int sector = 0; sector < 8; sector++)
                {
                    float weight =
                        sectorWeights[sector] *
                        gaussian *
                        alphaStop;
                    colorSums[sector] += color * weight;
                    squareSums[sector] +=
                        color * color * weight;
                    weightSums[sector] += weight;
                }
            }
        }

        float detail = Math.Clamp(requestedDetail, 0, 16);
        float hardness = 250 + (93.75f * detail);
        float exponent = MathF.Max(0.5f, detail * 0.5f);
        Vector3 result = Vector3.Zero;
        float resultWeight = 0;
        for (int sector = 0; sector < 8; sector++)
        {
            if (weightSums[sector] <= 0.000001f)
            {
                continue;
            }

            Vector3 mean = colorSums[sector] / weightSums[sector];
            Vector3 variance = Vector3.Max(
                (squareSums[sector] / weightSums[sector]) -
                    (mean * mean),
                Vector3.Zero);
            float varianceSum =
                variance.X + variance.Y + variance.Z;
            float confidence = 1 /
                (1 + MathF.Pow(
                    MathF.Max(hardness * varianceSum, 0),
                    exponent));
            result += mean * confidence;
            resultWeight += confidence;
        }

        result = resultWeight <= 0.000001f
            ? Vector3.Clamp(
                Unpremultiply(center),
                Vector3.Zero,
                Vector3.One)
            : result / resultWeight;
        float textureStrength =
            Math.Clamp(requestedTexture, 0, 8) * 0.02f;
        if (textureStrength > 0)
        {
            float coarse = Hash(
                x / 2,
                y / 2,
                0x51ed270bu);
            float fine = Hash(x, y, 0x68bc21ebu);
            float roughness =
                (((coarse * 0.65f) + (fine * 0.35f)) - 0.5f) *
                textureStrength;
            float luminance = Vector3.Dot(
                result,
                new Vector3(0.2126f, 0.7152f, 0.0722f));
            result = Vector3.Clamp(
                result +
                    new Vector3(
                        roughness *
                        (0.4f + (0.6f * (1 - luminance)))),
                Vector3.Zero,
                Vector3.One);
        }

        return Associated(result, center.W);
    }

    internal static Vector4[] ColoredPencil(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        Vector4[] tensor = new Vector4[source.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 gradient = ColoredPencilGradient(
                    source,
                    width,
                    height,
                    x,
                    y);
                tensor[(y * width) + x] = new Vector4(
                    gradient.X * gradient.X,
                    gradient.X * gradient.Y,
                    gradient.Y * gradient.Y,
                    1);
            }
        }

        int blurRadius = Math.Clamp(
            (int)MathF.Ceiling(plan.Passes[1].RadiusX),
            1,
            4);
        tensor = BlurColoredPencilTensor(
            tensor,
            width,
            height,
            blurRadius,
            horizontal: true);
        tensor = BlurColoredPencilTensor(
            tensor,
            width,
            height,
            blurRadius,
            horizontal: false);

        Vector4[] output = new Vector4[source.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                output[(y * width) + x] =
                    ComposeColoredPencil(
                        plan,
                        source,
                        tensor,
                        width,
                        height,
                        x,
                        y);
            }
        }
        return output;
    }

    private static Vector2 ColoredPencilGradient(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        float topLeft = Luminance(
            SamplePixel(source, width, height, x - 1, y - 1));
        float top = Luminance(
            SamplePixel(source, width, height, x, y - 1));
        float topRight = Luminance(
            SamplePixel(source, width, height, x + 1, y - 1));
        float left = Luminance(
            SamplePixel(source, width, height, x - 1, y));
        float right = Luminance(
            SamplePixel(source, width, height, x + 1, y));
        float bottomLeft = Luminance(
            SamplePixel(source, width, height, x - 1, y + 1));
        float bottom = Luminance(
            SamplePixel(source, width, height, x, y + 1));
        float bottomRight = Luminance(
            SamplePixel(source, width, height, x + 1, y + 1));
        return new Vector2(
            (-topLeft + topRight -
                (2 * left) + (2 * right) -
                bottomLeft + bottomRight) *
                0.25f,
            (-topLeft - (2 * top) - topRight +
                bottomLeft + (2 * bottom) + bottomRight) *
                0.25f);
    }

    private static Vector4[] BlurColoredPencilTensor(
        Vector4[] source,
        int width,
        int height,
        int radius,
        bool horizontal)
    {
        Vector4[] output = new Vector4[source.Length];
        float sigma = MathF.Max(radius * 0.5f, 0.75f);
        float divisor = 2 * sigma * sigma;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector4 sum = Vector4.Zero;
                float total = 0;
                for (int offset = -radius;
                    offset <= radius;
                    offset++)
                {
                    float weight = MathF.Exp(
                        -(offset * offset) / divisor);
                    sum += SamplePixel(
                        source,
                        width,
                        height,
                        horizontal ? x + offset : x,
                        horizontal ? y : y + offset) *
                        weight;
                    total += weight;
                }
                output[(y * width) + x] = sum / total;
            }
        }
        return output;
    }

    private static Vector4 ComposeColoredPencil(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        Vector4[] tensor,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = source[(y * width) + x];
        if (center.W <= 0)
        {
            return Vector4.Zero;
        }

        Vector3 centerColor = Unpremultiply(center);
        float centerLuminance = StraightLuminance(centerColor);
        Vector4 centerTensor = tensor[(y * width) + x];
        Vector2 tangent = ColoredPencilTangent(
            centerTensor,
            x,
            y);
        float pencilWidth = Math.Clamp(
            Option(plan, "PencilWidth", 3),
            0,
            12);
        int radius = Math.Clamp(
            (int)MathF.Ceiling(plan.Passes[3].RadiusX),
            0,
            12);
        float pressure = Math.Clamp(
            Option(plan, "StrokePressure", 8) / 16,
            0,
            1);
        float edgeStop = 0.08f + ((1 - pressure) * 0.12f);
        Vector3 accumulated = centerColor;
        float totalWeight = 1;

        for (int sign = -1; sign <= 1; sign += 2)
        {
            Vector2 position = new(x, y);
            Vector2 direction = tangent * sign;
            for (int step = 1; step <= 12; step++)
            {
                if (step > radius)
                {
                    break;
                }

                Vector4 localTensor = SamplePixelBilinear(
                    tensor,
                    width,
                    height,
                    position.X,
                    position.Y);
                Vector2 localDirection = ColoredPencilTangent(
                    localTensor,
                    (int)MathF.Round(position.X),
                    (int)MathF.Round(position.Y));
                if (Vector2.Dot(localDirection, direction) < 0)
                {
                    localDirection = -localDirection;
                }
                direction = Vector2.Normalize(
                    Vector2.Lerp(direction, localDirection, 0.6f));

                float coherence = ColoredPencilCoherence(localTensor);
                float phase = Hash(
                    x / 4,
                    y / 4,
                    unchecked((uint)plan.Filter));
                float swing =
                    MathF.Sin((step + phase) * 1.7f) *
                    (1 - coherence) *
                    0.18f;
                Vector2 normal = new(-direction.Y, direction.X);
                position += Vector2.Normalize(
                    direction + (normal * swing));

                Vector4 sample = SamplePixelBilinear(
                    source,
                    width,
                    height,
                    position.X,
                    position.Y);
                if (sample.W <= 0)
                {
                    continue;
                }
                Vector3 sampleColor = Unpremultiply(sample);
                float luminanceDelta = MathF.Abs(
                    StraightLuminance(sampleColor) -
                    centerLuminance);
                float spatial = step / MathF.Max(radius, 1);
                float weight =
                    MathF.Exp(-2 * spatial * spatial) *
                    MathF.Exp(-luminanceDelta / edgeStop) *
                    MathF.Exp(-MathF.Abs(sample.W - center.W) * 8);
                accumulated += sampleColor * weight;
                totalWeight += weight;
            }
        }

        Vector3 licColor = accumulated / totalWeight;
        float licLuminance = StraightLuminance(licColor);
        float tensorEnergy = Math.Clamp(
            MathF.Sqrt(
                MathF.Max(
                    centerTensor.X + centerTensor.Z,
                    0)),
            0,
            1);
        Vector4 paperOption = OptionVector(
            plan,
            "PaperColor",
            Vector4.One);
        float paperBrightness = Math.Clamp(
            Option(plan, "PaperBrightness", 0.25f),
            0,
            1);
        Vector3 paperColor = Vector3.Clamp(
            new Vector3(
                paperOption.X,
                paperOption.Y,
                paperOption.Z) *
                (0.75f + (0.25f * paperBrightness)),
            Vector3.Zero,
            Vector3.One);
        float coverage = Math.Clamp(
            ((1 - licLuminance) *
                (0.45f + (0.9f * pressure))) +
            (tensorEnergy * (0.2f + (0.3f * pressure))),
            0,
            1);
        float lineCoordinate =
            ((x * -tangent.Y) + (y * tangent.X)) /
            MathF.Max(0.75f, pencilWidth * 0.4f);
        float stroke = 0.82f +
            (0.18f * (0.5f +
                (0.5f * MathF.Cos(
                    (lineCoordinate * MathF.Tau) +
                    (Hash(x / 3, y / 3, 77) * MathF.PI)))));
        float grain = 0.88f +
            (0.12f * Hash(x, y, 0x4f1bbcdcu));
        coverage *= stroke * grain;
        Vector3 pigment = Vector3.Clamp(
            licColor * (0.3f + (0.55f * licLuminance)),
            Vector3.Zero,
            Vector3.One);
        return Associated(
            Vector3.Lerp(
                paperColor,
                pigment,
                Math.Clamp(coverage, 0, 1)),
            center.W);
    }

    private static Vector2 ColoredPencilTangent(
        Vector4 tensor,
        int x,
        int y)
    {
        if (ColoredPencilCoherence(tensor) < 0.02f)
        {
            float flatAngle =
                (Hash(x / 8, y / 8, 0x2c9277b5u) - 0.5f) *
                MathF.PI;
            return new Vector2(
                MathF.Cos(flatAngle),
                MathF.Sin(flatAngle));
        }

        float gradientAngle = 0.5f * MathF.Atan2(
            2 * tensor.Y,
            tensor.X - tensor.Z);
        return new Vector2(
            -MathF.Sin(gradientAngle),
            MathF.Cos(gradientAngle));
    }

    private static float ColoredPencilCoherence(Vector4 tensor)
    {
        float difference = tensor.X - tensor.Z;
        float discriminant = MathF.Sqrt(
            (difference * difference) +
            (4 * tensor.Y * tensor.Y));
        return Math.Clamp(
            discriminant /
                MathF.Max(tensor.X + tensor.Z, 0.000001f),
            0,
            1);
    }

    private static float StraightLuminance(Vector3 color) =>
        Vector3.Dot(
            color,
            new Vector3(0.2126f, 0.7152f, 0.0722f));
}
