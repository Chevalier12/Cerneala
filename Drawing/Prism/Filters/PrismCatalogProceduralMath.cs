using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Definitions;
using static Cerneala.Drawing.Prism.Filters.PrismCatalogFilterMath;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismCatalogProceduralMath
{
    internal static Vector4 Procedural(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Func<Vector2, Vector4>? primaryResource,
        Func<Vector2, Vector4>? auxiliaryResource,
        PrismLightingResource? lightingResource)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        Vector2 uv = new(
            (x + 0.5f) / width,
            (y + 0.5f) / height);
        switch (plan.Filter)
        {
            case PrismFilterId.HalftonePattern:
                return PrismHalftonePatternFilter.ApplyPixel(
                    plan, center, width, height, x, y);
            case PrismFilterId.Clouds:
                return PrismCloudsFilter.ApplyPixel(
                    plan, center, x, y);
            case PrismFilterId.DifferenceClouds:
                return PrismDifferenceCloudsFilter.ApplyPixel(
                    plan, center, x, y);
            case PrismFilterId.Fibers:
                return PrismFibersFilter.ApplyPixel(
                    plan, pass, center, x, y);
            case PrismFilterId.LensFlare:
                {
                    Vector4 centerOption = OptionVector(
                        plan,
                        "Center",
                        new Vector4(0.5f, 0.5f, 0, 0));
                    float distance = Vector2.Distance(
                        uv,
                        new Vector2(centerOption.X, centerOption.Y));
                    float flare = MathF.Pow(
                        Math.Clamp(1 - (distance * 3), 0, 1),
                        2) * Option(plan, "Brightness", 1);
                    Vector3 straight = Unpremultiply(center);
                    return Associated(
                        Vector3.Clamp(
                            straight +
                            new Vector3(
                                flare,
                                flare * 0.75f,
                                flare * 0.35f),
                            Vector3.Zero,
                            Vector3.One),
                        center.W);
                }
            case PrismFilterId.LightingEffects:
                return PrismLightingEffectsFilter.ApplyPixel(
                    plan,
                    center,
                    uv,
                    width,
                    height,
                    auxiliaryResource,
                    lightingResource!);
            case PrismFilterId.Diffuse:
                return PrismDiffuseFilter.Apply(
                    plan,
                    pass,
                    source,
                    width,
                    height,
                    x,
                    y);
            default:
                return center;
        }
    }

    internal static Vector4 HalftonePattern(
        PrismCatalogFilterPlan plan,
        Vector4 source,
        Vector2 pixel,
        Vector2 imageCenter)
    {
        float cellSize = MathF.Max(
            2,
            Option(plan, "Size", 2));
        float contrastScale = Math.Clamp(
            1 + (Option(plan, "Contrast", 0) * 0.1f),
            0,
            16);
        float coverage = Math.Clamp(
            ((1 - Luminance(source)) - 0.5f) *
                contrastScale +
                0.5f,
            0,
            1);
        int patternType = (int)MathF.Round(
            Option(plan, "PatternType", 0));
        float threshold = patternType switch
        {
            1 => HalftoneLineThreshold(pixel, cellSize),
            2 => HalftoneCircleThreshold(
                pixel,
                imageCenter,
                cellSize),
            _ => HalftoneDotThreshold(pixel, cellSize)
        };
        float ink;
        if (coverage <= 0)
        {
            ink = 0;
        }
        else if (coverage >= 1)
        {
            ink = 1;
        }
        else
        {
            float antialiasWidth = Math.Clamp(
                1 / cellSize,
                0.0001f,
                0.5f);
            ink = 1 - SmoothStep(
                coverage - antialiasWidth,
                coverage + antialiasWidth,
                threshold);
        }

        Vector4 foreground = OptionVector(
            plan,
            "Foreground",
            new Vector4(0, 0, 0, 1));
        Vector4 background = OptionVector(
            plan,
            "Background",
            Vector4.One);
        return Associated(
            Vector3.Lerp(
                new Vector3(
                    background.X,
                    background.Y,
                    background.Z),
                new Vector3(
                    foreground.X,
                    foreground.Y,
                    foreground.Z),
                ink),
            source.W);
    }

    private static float HalftoneDotThreshold(
        Vector2 pixel,
        float cellSize)
    {
        Vector2 local = pixel - new Vector2(
            MathF.Floor((pixel.X / cellSize) + 0.5f) * cellSize,
            MathF.Floor((pixel.Y / cellSize) + 0.5f) * cellSize);
        float halfCell = cellSize * 0.5f;
        float radius = Math.Clamp(
            local.Length() / halfCell,
            0,
            MathF.Sqrt(2));
        if (radius <= 1)
        {
            return MathF.PI * radius * radius * 0.25f;
        }

        float outsideAxis = MathF.Sqrt(
            MathF.Max(0, (radius * radius) - 1));
        return Math.Clamp(
            outsideAxis +
                (radius * radius *
                    (MathF.Asin(1 / radius) -
                        (MathF.PI * 0.25f))),
            0,
            1);
    }

    private static float HalftoneLineThreshold(
        Vector2 pixel,
        float cellSize)
    {
        float local = pixel.Y -
            (MathF.Floor((pixel.Y / cellSize) + 0.5f) *
                cellSize);
        return Math.Clamp(
            MathF.Abs(local) / (cellSize * 0.5f),
            0,
            1);
    }

    private static float HalftoneCircleThreshold(
        Vector2 pixel,
        Vector2 imageCenter,
        float cellSize)
    {
        float radius = Vector2.Distance(pixel, imageCenter);
        float ring = MathF.Floor(radius / cellSize);
        float innerRadius = ring * cellSize;
        float outerRadius = innerRadius + cellSize;
        float areaPhase =
            ((radius * radius) -
                (innerRadius * innerRadius)) /
            ((outerRadius * outerRadius) -
                (innerRadius * innerRadius));
        return MathF.Abs((areaPhase * 2) - 1);
    }

    internal static Vector4 LightingEffects(
        PrismCatalogFilterPlan plan,
        Vector4 center,
        Vector2 uv,
        int width,
        int height,
        Func<Vector2, Vector4>? heightResource,
        PrismLightingResource lighting)
    {
        const float minimumDenominator = 0.00001f;
        Vector3 baseColor = Vector3.Clamp(
            Unpremultiply(center),
            Vector3.Zero,
            Vector3.One);
        float metallic = Math.Clamp(
            Option(plan, "Metallic", 0),
            0,
            1);
        float gloss = Math.Clamp(
            Option(plan, "Gloss", 0.5f),
            0,
            1);
        float roughness = MathF.Max(
            0.045f,
            (1 - gloss) * (1 - gloss));
        float textureHeight = MathF.Max(
            0,
            Option(plan, "TextureHeight", 0));
        Vector3 normal = HeightNormal(
            heightResource,
            uv,
            width,
            height,
            textureHeight);
        Vector3 view = Vector3.UnitZ;
        Vector3 dielectricF0 = new(0.04f);
        Vector3 f0 = Vector3.Lerp(
            dielectricF0,
            baseColor,
            metallic);
        Vector3 diffuseColor =
            baseColor * (1 - metallic) / MathF.PI;
        Vector3 result =
            baseColor * MathF.Max(
                0,
                Option(plan, "Ambient", 0));
        Vector3 surfacePosition = new(uv, 0);

        foreach (PrismLight light in lighting.Lights)
        {
            Vector3 surfaceToLight;
            float attenuation;
            if (light.Kind == PrismLightKind.Directional)
            {
                surfaceToLight = light.Direction;
                attenuation = light.Intensity;
            }
            else
            {
                Vector3 delta =
                    light.Position - surfacePosition;
                float distanceSquared = MathF.Max(
                    delta.LengthSquared(),
                    minimumDenominator);
                surfaceToLight = delta /
                    MathF.Sqrt(distanceSquared);
                attenuation =
                    light.Intensity / distanceSquared;
            }

            float normalDotLight = MathF.Max(
                Vector3.Dot(normal, surfaceToLight),
                0);
            if (normalDotLight <= 0 || attenuation <= 0)
            {
                continue;
            }

            Vector3 radiance =
                light.LinearSrgb * attenuation;
            result +=
                (diffuseColor +
                    CookTorranceGgxSpecular(
                        f0,
                        normal,
                        view,
                        surfaceToLight,
                        roughness)) *
                radiance *
                normalDotLight;
        }

        float exposure = MathF.Pow(
            2,
            Option(plan, "Exposure", 0));
        return Associated(
            Vector3.Clamp(
                result * exposure,
                Vector3.Zero,
                Vector3.One),
            center.W);
    }

    internal static Vector3 CookTorranceGgxSpecular(
        Vector3 f0,
        Vector3 normal,
        Vector3 view,
        Vector3 surfaceToLight,
        float roughness)
    {
        const float minimumDenominator = 0.00001f;
        float normalDotLight = MathF.Max(
            Vector3.Dot(normal, surfaceToLight),
            0);
        float normalDotView = MathF.Max(
            Vector3.Dot(normal, view),
            0);
        if (normalDotLight <= 0 || normalDotView <= 0)
        {
            return Vector3.Zero;
        }

        Vector3 halfCandidate = view + surfaceToLight;
        if (halfCandidate.LengthSquared() <= minimumDenominator)
        {
            return Vector3.Zero;
        }

        Vector3 halfVector = Vector3.Normalize(halfCandidate);
        float normalDotHalf = MathF.Max(
            Vector3.Dot(normal, halfVector),
            0);
        float viewDotHalf = MathF.Max(
            Vector3.Dot(view, halfVector),
            0);
        float roughnessSquared = roughness * roughness;
        float distributionDenominator =
            (normalDotHalf * normalDotHalf *
                (roughnessSquared - 1)) + 1;
        float distribution =
            roughnessSquared /
            MathF.Max(
                MathF.PI *
                    distributionDenominator *
                    distributionDenominator,
                minimumDenominator);
        float lambdaView =
            normalDotLight *
            MathF.Sqrt(
                ((normalDotView -
                    (roughnessSquared * normalDotView)) *
                    normalDotView) +
                roughnessSquared);
        float lambdaLight =
            normalDotView *
            MathF.Sqrt(
                ((normalDotLight -
                    (roughnessSquared * normalDotLight)) *
                    normalDotLight) +
                roughnessSquared);
        float visibility =
            0.5f /
            MathF.Max(
                lambdaView + lambdaLight,
                minimumDenominator);
        float fresnelWeight = MathF.Pow(
            1 - viewDotHalf,
            5);
        Vector3 fresnel =
            f0 + ((Vector3.One - f0) * fresnelWeight);
        return fresnel * distribution * visibility;
    }

    private static Vector3 HeightNormal(
        Func<Vector2, Vector4>? heightResource,
        Vector2 uv,
        int width,
        int height,
        float textureHeight)
    {
        if (heightResource is null || textureHeight <= 0)
        {
            return Vector3.UnitZ;
        }

        Vector2 step = new(
            1f / width,
            1f / height);
        float left = ResourceHeight(
            heightResource,
            new Vector2(
                Math.Clamp(uv.X - step.X, 0, 1),
                uv.Y));
        float right = ResourceHeight(
            heightResource,
            new Vector2(
                Math.Clamp(uv.X + step.X, 0, 1),
                uv.Y));
        float top = ResourceHeight(
            heightResource,
            new Vector2(
                uv.X,
                Math.Clamp(uv.Y - step.Y, 0, 1)));
        float bottom = ResourceHeight(
            heightResource,
            new Vector2(
                uv.X,
                Math.Clamp(uv.Y + step.Y, 0, 1)));
        return Vector3.Normalize(
            new Vector3(
                -(right - left) * textureHeight * 0.5f,
                -(bottom - top) * textureHeight * 0.5f,
                1));
    }

    private static float ResourceHeight(
        Func<Vector2, Vector4> resource,
        Vector2 uv)
    {
        Vector4 sample = resource(uv);
        Vector3 straight = sample.W > 0
            ? new Vector3(sample.X, sample.Y, sample.Z) /
                sample.W
            : Vector3.Zero;
        return Math.Clamp(
            Vector3.Dot(
                straight,
                new Vector3(0.2126f, 0.7152f, 0.0722f)),
            0,
            1);
    }

    internal static Vector4 Video(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        if (plan.Filter == PrismFilterId.Deinterlace)
        {
            return PrismDeinterlaceFilter.ApplyPixel(
                plan,
                source,
                width,
                height,
                x,
                y,
                center);
        }
        if (plan.Filter == PrismFilterId.NtscColors)
        {
            return PrismNtscColorsFilter.ApplyPixel(center);
        }
        return PrismScanlinesFilter.ApplyPixel(
            plan, center, height, y);
    }

    internal static Vector4 NtscReduceLuminance(Vector4 color)
    {
        const float pedestal = 0.075f;
        const float activeVideoRange = 1 - pedestal;
        const float maximumChrominance =
            0.5f / activeVideoRange;
        const float maximumComposite =
            (1.1f - pedestal) / activeVideoRange;
        Vector3 encoded = EncodeNtscGamma(
            Vector3.Clamp(
                Unpremultiply(color),
                Vector3.Zero,
                Vector3.One));
        float luminance = Vector3.Dot(
            encoded,
            new Vector3(0.2989f, 0.5866f, 0.1144f));
        float inPhase = Vector3.Dot(
            encoded,
            new Vector3(0.5959f, -0.2741f, -0.3218f));
        float quadrature = Vector3.Dot(
            encoded,
            new Vector3(0.2113f, -0.5227f, 0.3113f));
        float chrominance = MathF.Sqrt(
            (inPhase * inPhase) +
            (quadrature * quadrature));
        float scale = 1;
        if (chrominance > maximumChrominance)
        {
            scale = maximumChrominance / chrominance;
        }

        float compositePeak = luminance + chrominance;
        if (compositePeak > maximumComposite)
        {
            scale = MathF.Min(
                scale,
                maximumComposite / compositePeak);
        }

        if (scale >= 1)
        {
            return color;
        }

        return Associated(
            DecodeNtscGamma(encoded * scale),
            color.W);
    }

    private static Vector3 EncodeNtscGamma(Vector3 color) =>
        new(
            EncodeNtscGamma(color.X),
            EncodeNtscGamma(color.Y),
            EncodeNtscGamma(color.Z));

    private static float EncodeNtscGamma(float value) =>
        MathF.Pow(value, 1f / 2.2f);

    private static Vector3 DecodeNtscGamma(Vector3 color) =>
        new(
            DecodeNtscGamma(color.X),
            DecodeNtscGamma(color.Y),
            DecodeNtscGamma(color.Z));

    private static float DecodeNtscGamma(float value) =>
        MathF.Pow(value, 2.2f);

    internal static Vector4 Deinterlace(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Vector4 center)
    {
        int replacedField = (int)Option(plan, "Field", 1);
        if ((y & 1) != replacedField)
        {
            return center;
        }

        int topY = y - 1;
        int bottomY = y + 1;
        if ((int)Option(plan, "Replacement", 0) == 1)
        {
            int sourceY = topY >= 0 ? topY : bottomY;
            return sourceY < height
                ? SamplePixel(source, width, height, x, sourceY)
                : center;
        }
        if (topY < 0)
        {
            return bottomY < height
                ? SamplePixel(source, width, height, x, bottomY)
                : center;
        }
        if (bottomY >= height)
        {
            return SamplePixel(source, width, height, x, topY);
        }

        const int searchRadius = 3;
        int bestSlope = 0;
        float bestCost = float.PositiveInfinity;
        for (int slope = -searchRadius;
            slope <= searchRadius;
            slope++)
        {
            float cost =
                DeinterlacePairCost(
                    source,
                    width,
                    height,
                    x - slope,
                    topY,
                    x + slope,
                    bottomY) +
                (0.5f * DeinterlacePairCost(
                    source,
                    width,
                    height,
                    x - slope - 1,
                    topY,
                    x + slope - 1,
                    bottomY)) +
                (0.5f * DeinterlacePairCost(
                    source,
                    width,
                    height,
                    x - slope + 1,
                    topY,
                    x + slope + 1,
                    bottomY)) +
                (0.02f * Math.Abs(slope));
            if (slope != 0)
            {
                Vector4 topCandidate = SamplePixel(
                    source,
                    width,
                    height,
                    x - slope,
                    topY);
                Vector4 bottomCandidate = SamplePixel(
                    source,
                    width,
                    height,
                    x + slope,
                    bottomY);
                Vector4 topCenter = SamplePixel(
                    source,
                    width,
                    height,
                    x,
                    topY);
                Vector4 bottomCenter = SamplePixel(
                    source,
                    width,
                    height,
                    x,
                    bottomY);
                cost -= 0.25f *
                    (MathF.Abs(
                        DeinterlaceLuminance(topCandidate) -
                        DeinterlaceLuminance(topCenter)) +
                    MathF.Abs(
                        DeinterlaceLuminance(bottomCandidate) -
                        DeinterlaceLuminance(bottomCenter)));
            }
            if (cost < bestCost)
            {
                bestCost = cost;
                bestSlope = slope;
            }
        }

        Vector4 nearTop = SamplePixel(
            source,
            width,
            height,
            x - bestSlope,
            topY);
        Vector4 nearBottom = SamplePixel(
            source,
            width,
            height,
            x + bestSlope,
            bottomY);
        Vector4 linear = (nearTop + nearBottom) * 0.5f;
        if (y < 3 || y + 3 >= height)
        {
            return linear;
        }

        Vector4 farTop = SamplePixel(
            source,
            width,
            height,
            x - (3 * bestSlope),
            y - 3);
        Vector4 farBottom = SamplePixel(
            source,
            width,
            height,
            x + (3 * bestSlope),
            y + 3);
        Vector4 fourPoint =
            ((nearTop + nearBottom) * 9 -
                farTop -
                farBottom) /
            16;
        return Vector4.Clamp(
            fourPoint,
            Vector4.Min(nearTop, nearBottom),
            Vector4.Max(nearTop, nearBottom));
    }

    private static float DeinterlacePairCost(
        Vector4[] source,
        int width,
        int height,
        int firstX,
        int firstY,
        int secondX,
        int secondY)
    {
        Vector4 first = SamplePixel(
            source,
            width,
            height,
            firstX,
            firstY);
        Vector4 second = SamplePixel(
            source,
            width,
            height,
            secondX,
            secondY);
        return MathF.Abs(
                DeinterlaceLuminance(first) -
                DeinterlaceLuminance(second)) +
            (0.25f * MathF.Abs(first.W - second.W));
    }

    private static float DeinterlaceLuminance(Vector4 color) =>
        Vector3.Dot(
            new Vector3(color.X, color.Y, color.Z),
            new Vector3(0.2126f, 0.7152f, 0.0722f));
}
