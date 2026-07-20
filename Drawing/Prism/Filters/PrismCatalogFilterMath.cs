using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismCatalogFilterMath
{
    public static PrismPremultipliedColor[] Apply(
        PrismCatalogFilterPlan plan,
        ReadOnlySpan<PrismPremultipliedColor> source,
        int width,
        int height,
        PrismColorProfile workingProfile,
        float opacity = 1,
        Func<Vector2, Vector4>? primaryResource = null,
        Func<Vector2, Vector4>? auxiliaryResource = null)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }
        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }
        if (source.Length != checked(width * height))
        {
            throw new ArgumentException(
                "The source pixel count does not match its dimensions.",
                nameof(source));
        }
        if (!float.IsFinite(opacity) || opacity is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(opacity));
        }
        if (plan.Passes.IsDefaultOrEmpty)
        {
            throw new ArgumentException(
                "A catalog filter plan must contain at least one pass.",
                nameof(plan));
        }
        if (plan.PrimaryResourceRequired && primaryResource is null)
        {
            throw new InvalidOperationException(
                $"Filter '{plan.Filter}' requires its prepared primary resource.");
        }
        if (plan.AuxiliaryResourceRequired && auxiliaryResource is null)
        {
            throw new InvalidOperationException(
                $"Filter '{plan.Filter}' requires its prepared auxiliary resource.");
        }

        Vector4[] original = new Vector4[source.Length];
        for (int index = 0; index < source.Length; index++)
        {
            original[index] = ToVector4(
                PrismAdjustmentMath.ConvertProfile(
                    source[index],
                    workingProfile,
                    PrismColorProfile.LinearSrgb));
        }

        Vector4[] current = original;
        foreach (PrismCatalogFilterPass pass in plan.Passes)
        {
            if (pass.IsNoOp)
            {
                continue;
            }

            Vector4[] output = new Vector4[current.Length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    output[(y * width) + x] = ApplyPixel(
                        plan,
                        pass,
                        current,
                        width,
                        height,
                        x,
                        y,
                        primaryResource,
                        auxiliaryResource);
                }
            }
            current = output;
        }

        PrismPremultipliedColor[] result =
            new PrismPremultipliedColor[current.Length];
        for (int index = 0; index < current.Length; index++)
        {
            Vector4 filtered = ClampAssociated(current[index]);
            Vector4 blended = ClampAssociated(
                Vector4.Lerp(original[index], filtered, opacity));
            result[index] = PrismAdjustmentMath.ConvertProfile(
                ToPremultiplied(blended),
                PrismColorProfile.LinearSrgb,
                workingProfile);
        }
        return result;
    }

    private static Vector4 ApplyPixel(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Func<Vector2, Vector4>? primaryResource,
        Func<Vector2, Vector4>? auxiliaryResource)
    {
        Vector4 center = source[(y * width) + x];
        return plan.Primitive switch
        {
            PrismCatalogFilterPrimitive.Morphology =>
                Morphology(plan, pass, source, width, height, x, y),
            PrismCatalogFilterPrimitive.Quantization =>
                Quantization(plan, source, width, height, x, y),
            PrismCatalogFilterPrimitive.Procedural =>
                Procedural(
                    plan,
                    pass,
                    source,
                    width,
                    height,
                    x,
                    y,
                    primaryResource,
                    auxiliaryResource),
            PrismCatalogFilterPrimitive.Video =>
                Video(plan, source, width, height, x, y),
            PrismCatalogFilterPrimitive.Artistic =>
                Artistic(plan, source, width, height, x, y),
            PrismCatalogFilterPrimitive.EdgeDetection =>
                EdgeDetection(plan, source, width, height, x, y),
            PrismCatalogFilterPrimitive.Tiling =>
                Tiling(plan, source, width, height, x, y),
            PrismCatalogFilterPrimitive.Texture =>
                Texture(plan, source, width, height, x, y, primaryResource),
            PrismCatalogFilterPrimitive.Convolution =>
                Convolution(
                    plan,
                    source,
                    width,
                    height,
                    x,
                    y,
                    primaryResource),
            PrismCatalogFilterPrimitive.Color =>
                Color(plan, source, width, height, x, y),
            _ => center
        };
    }

    private static Vector4 Morphology(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        float radius = MathF.Max(pass.RadiusX, pass.RadiusY);
        Vector4 result = SamplePixel(source, width, height, x, y);
        Vector4 negative = SamplePixel(
            source,
            width,
            height,
            x - pass.RadiusX,
            y - pass.RadiusY);
        Vector4 positive = SamplePixel(
            source,
            width,
            height,
            x + pass.RadiusX,
            y + pass.RadiusY);
        if (radius == 0)
        {
            return result;
        }

        return plan.Filter == PrismFilterId.Maximum
            ? Vector4.Max(result, Vector4.Max(negative, positive))
            : Vector4.Min(result, Vector4.Min(negative, positive));
    }

    private static Vector4 Quantization(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        switch (plan.Filter)
        {
            case PrismFilterId.ColorHalftone:
            {
                float radius = MathF.Max(
                    1,
                    Option(plan, "MaxRadius", 4));
                float dot = Hash(
                    (int)MathF.Floor(x / radius),
                    (int)MathF.Floor(y / radius),
                    0x63u);
                float level = Luminance(center) >= dot ? 1 : 0;
                return Associated(
                    new Vector3(level),
                    center.W);
            }
            case PrismFilterId.Crystallize:
            case PrismFilterId.Pointillize:
            {
                float cell = MathF.Max(
                    1,
                    Option(plan, "CellSize", 10));
                uint seed = Seed(plan, "Seed");
                int cellX = (int)MathF.Floor(x / cell);
                int cellY = (int)MathF.Floor(y / cell);
                float sampleX =
                    (cellX + Hash(cellX, cellY, seed)) * cell;
                float sampleY =
                    (cellY + Hash(cellX, cellY, seed + 1)) * cell;
                Vector4 sampled = SamplePixel(
                    source,
                    width,
                    height,
                    sampleX,
                    sampleY);
                if (plan.Filter == PrismFilterId.Pointillize)
                {
                    float distance = Vector2.Distance(
                        new Vector2(x, y),
                        new Vector2(sampleX, sampleY));
                    if (distance > cell * 0.45f)
                    {
                        return AssociatedColor(
                            OptionVector(
                                plan,
                                "Background",
                                Vector4.Zero),
                            center.W);
                    }
                }
                return sampled;
            }
            case PrismFilterId.Facet:
                return (
                    center +
                    SamplePixel(source, width, height, x - 1, y) +
                    SamplePixel(source, width, height, x + 1, y) +
                    SamplePixel(source, width, height, x, y - 1) +
                    SamplePixel(source, width, height, x, y + 1)) / 5;
            case PrismFilterId.Fragment:
            {
                float offset = MathF.Max(
                    0,
                    Option(plan, "Offset", 1));
                return (
                    SamplePixel(
                        source,
                        width,
                        height,
                        x - offset,
                        y - offset) +
                    SamplePixel(
                        source,
                        width,
                        height,
                        x + offset,
                        y - offset) +
                    SamplePixel(
                        source,
                        width,
                        height,
                        x - offset,
                        y + offset) +
                    SamplePixel(
                        source,
                        width,
                        height,
                        x + offset,
                        y + offset)) / 4;
            }
            case PrismFilterId.Mezzotint:
            {
                float threshold = Hash(x, y, Seed(plan, "Seed"));
                float value = Luminance(center) >= threshold ? 1 : 0;
                return Associated(new Vector3(value), center.W);
            }
            case PrismFilterId.Mosaic:
            {
                Vector4 cell = OptionVector(
                    plan,
                    "CellSize",
                    new Vector4(8, 8, 0, 0));
                float cellX = MathF.Max(1, cell.X);
                float cellY = MathF.Max(1, cell.Y);
                return SamplePixel(
                    source,
                    width,
                    height,
                    (MathF.Floor(x / cellX) + 0.5f) * cellX,
                    (MathF.Floor(y / cellY) + 0.5f) * cellY);
            }
            default:
                return center;
        }
    }

    private static Vector4 Procedural(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Func<Vector2, Vector4>? primaryResource,
        Func<Vector2, Vector4>? auxiliaryResource)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        Vector2 uv = new(
            (x + 0.5f) / width,
            (y + 0.5f) / height);
        uint seed = Seed(plan, "Seed") +
            unchecked((uint)pass.Iteration * 0x9e3779b9u);
        switch (plan.Filter)
        {
            case PrismFilterId.Clouds:
            case PrismFilterId.DifferenceClouds:
            {
                float scale = MathF.Max(
                    0.0001f,
                    Option(plan, "Scale", 1));
                float noise = FractalNoise(
                    x / scale,
                    y / scale,
                    seed);
                Vector4 foreground = OptionVector(
                    plan,
                    "Foreground",
                    new Vector4(0, 0, 0, 1));
                Vector4 background = OptionVector(
                    plan,
                    "Background",
                    Vector4.One);
                Vector3 pattern = Vector3.Lerp(
                    new Vector3(
                        background.X,
                        background.Y,
                        background.Z),
                    new Vector3(
                        foreground.X,
                        foreground.Y,
                        foreground.Z),
                    noise);
                if (plan.Filter == PrismFilterId.DifferenceClouds)
                {
                    pattern = Vector3.Abs(
                        Unpremultiply(center) - pattern);
                }
                return Associated(pattern, center.W);
            }
            case PrismFilterId.Fibers:
            {
                float variance = MathF.Max(
                    0.0001f,
                    Option(plan, "Variance", 16));
                float strength = Option(plan, "Strength", 4);
                float noise = FractalNoise(
                    x / variance,
                    y * 0.25f,
                    seed);
                noise = Math.Clamp(
                    0.5f + ((noise - 0.5f) * strength),
                    0,
                    1);
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
                        noise),
                    center.W);
            }
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
            {
                Vector4 light =
                    primaryResource?.Invoke(uv) ?? Vector4.One;
                Vector4 texture =
                    auxiliaryResource?.Invoke(uv) ?? Vector4.One;
                float ambient = Option(plan, "Ambient", 0);
                float exposure = MathF.Pow(
                    2,
                    Option(plan, "Exposure", 0));
                float intensity = Math.Clamp(
                    ambient +
                    Luminance(light) *
                    (0.5f + (0.5f * Luminance(texture))),
                    0,
                    2);
                return Associated(
                    Vector3.Clamp(
                        Unpremultiply(center) *
                        intensity *
                        exposure,
                        Vector3.Zero,
                        Vector3.One),
                    center.W);
            }
            case PrismFilterId.Diffuse:
            {
                float angle =
                    Hash(x, y, seed) * MathF.Tau;
                return SamplePixel(
                    source,
                    width,
                    height,
                    x + MathF.Cos(angle) * pass.RadiusX,
                    y + MathF.Sin(angle) * pass.RadiusY);
            }
            default:
                return center;
        }
    }

    private static Vector4 Video(
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
            if ((y & 1) == 0)
            {
                return center;
            }
            return (
                SamplePixel(source, width, height, x, y - 1) +
                SamplePixel(source, width, height, x, y + 1)) * 0.5f;
        }
        if (plan.Filter == PrismFilterId.NtscColors)
        {
            Vector3 straight = Unpremultiply(center);
            float luma = Vector3.Dot(
                straight,
                new Vector3(0.299f, 0.587f, 0.114f));
            float chromaLimit = MathF.Max(0, 1 - luma);
            Vector3 chroma = straight - new Vector3(luma);
            return Associated(
                Vector3.Clamp(
                    new Vector3(luma) +
                    Vector3.Clamp(
                        chroma,
                        new Vector3(-chromaLimit),
                        new Vector3(chromaLimit)),
                    Vector3.Zero,
                    Vector3.One),
                center.W);
        }

        float frequency = MathF.Max(
            1,
            Option(plan, "Frequency", 320));
        float phase = Option(plan, "Phase", 0);
        float thickness = Math.Clamp(
            Option(plan, "Thickness", 0.5f),
            0,
            1);
        float line =
            (y * frequency / MathF.Max(height, 1) + phase) % 1;
        if (line < 0)
        {
            line++;
        }
        float coverage = line < thickness
            ? Option(plan, "LineOpacity", 0.18f)
            : 0;
        Vector4 color = OptionVector(
            plan,
            "Color",
            new Vector4(0, 0, 0, 1));
        return Associated(
            Vector3.Lerp(
                Unpremultiply(center),
                new Vector3(color.X, color.Y, color.Z),
                Math.Clamp(coverage * color.W, 0, 1)),
            center.W);
    }

    private static Vector4 Artistic(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        Vector3 straight = Unpremultiply(center);
        float edge = Sobel(source, width, height, x, y);
        int variant = ((int)plan.Filter - 77) % 6;
        float amount = Math.Clamp(
            ParameterMagnitude(plan) * 0.01f,
            0.05f,
            0.85f);
        float noise = Hash(x, y, Seed(plan, "Seed")) - 0.5f;
        Vector3 styled = variant switch
        {
            0 => Quantize(straight, 6) -
                new Vector3(edge * amount),
            1 => Vector3.Lerp(
                straight,
                new Vector3(Luminance(center)),
                amount),
            2 => Quantize(
                straight + new Vector3(noise * amount),
                8),
            3 => Vector3.Lerp(
                straight,
                Vector3.One - new Vector3(edge),
                amount),
            4 => Vector3.Lerp(
                straight,
                new Vector3(
                    straight.X * 1.1f,
                    straight.Y * 0.95f,
                    straight.Z * 0.8f),
                amount),
            _ => straight +
                new Vector3(
                    edge * amount,
                    -edge * amount * 0.5f,
                    noise * amount)
        };
        return Associated(
            Vector3.Clamp(styled, Vector3.Zero, Vector3.One),
            center.W);
    }

    private static Vector4 EdgeDetection(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        float edge = Sobel(source, width, height, x, y);
        Vector3 straight = Unpremultiply(center);
        if (plan.Filter == PrismFilterId.Emboss)
        {
            float angle =
                Option(plan, "Angle", 135) * (MathF.PI / 180);
            float dx = MathF.Cos(angle);
            float dy = MathF.Sin(angle);
            float delta =
                Luminance(
                    SamplePixel(
                        source,
                        width,
                        height,
                        x + dx,
                        y + dy)) -
                Luminance(
                    SamplePixel(
                        source,
                        width,
                        height,
                        x - dx,
                        y - dy));
            float amount = Option(plan, "Amount", 1);
            return Associated(
                new Vector3(
                    Math.Clamp(0.5f + (delta * amount), 0, 1)),
                center.W);
        }
        if (plan.Filter == PrismFilterId.FindEdges)
        {
            float threshold = Option(plan, "Threshold", 0.1f);
            float value = Math.Clamp(
                (edge - threshold) /
                MathF.Max(1 - threshold, 0.0001f),
                0,
                1);
            return Associated(
                new Vector3(1 - value),
                center.W);
        }
        if (plan.Filter == PrismFilterId.GlowingEdges)
        {
            float brightness = Option(
                plan,
                "EdgeBrightness",
                6);
            return Associated(
                Vector3.Clamp(
                    new Vector3(
                        edge * 0.25f,
                        edge * 0.6f,
                        edge) *
                    brightness,
                    Vector3.Zero,
                    Vector3.One),
                center.W);
        }
        if (plan.Filter == PrismFilterId.TraceContour)
        {
            float level = Option(plan, "Level", 0.5f);
            float value = MathF.Abs(
                Luminance(center) - level) <=
                MathF.Max(edge, 0.02f)
                ? 0
                : 1;
            return Associated(new Vector3(value), center.W);
        }

        Vector4 foreground = OptionVector(
            plan,
            "Foreground",
            new Vector4(0, 0, 0, 1));
        Vector4 background = OptionVector(
            plan,
            "Background",
            new Vector4(1, 1, 1, 1));
        float mix = Math.Clamp(
            Luminance(center) + edge * 0.5f,
            0,
            1);
        Vector3 sketch = Vector3.Lerp(
            new Vector3(
                foreground.X,
                foreground.Y,
                foreground.Z),
            new Vector3(
                background.X,
                background.Y,
                background.Z),
            mix);
        return Associated(
            Vector3.Lerp(
                straight,
                sketch,
                Math.Clamp(
                    0.35f + (ParameterMagnitude(plan) * 0.01f),
                    0.35f,
                    1)),
            center.W);
    }

    private static Vector4 Tiling(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        if (plan.Filter == PrismFilterId.ChromaticAberration)
        {
            float amount = Option(plan, "Amount", 0);
            Vector4 direction = OptionVector(
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
            offset *= amount;
            Vector4 red = SamplePixel(
                source,
                width,
                height,
                x + offset.X,
                y + offset.Y);
            Vector4 center = SamplePixel(
                source,
                width,
                height,
                x,
                y);
            Vector4 blue = SamplePixel(
                source,
                width,
                height,
                x - offset.X,
                y - offset.Y);
            return new Vector4(
                red.X,
                center.Y,
                blue.Z,
                center.W);
        }
        if (plan.Filter == PrismFilterId.Wind)
        {
            float strength = MathF.Max(
                0,
                Option(plan, "Strength", 1));
            return (
                SamplePixel(source, width, height, x, y) * 2 +
                SamplePixel(
                    source,
                    width,
                    height,
                    x + strength,
                    y) +
                SamplePixel(
                    source,
                    width,
                    height,
                    x + (strength * 2),
                    y)) / 4;
        }

        float size = plan.Filter switch
        {
            PrismFilterId.Extrude =>
                Option(plan, "Size", 30),
            PrismFilterId.Tiles =>
                MathF.Max(
                    width,
                    height) /
                MathF.Max(1, Option(plan, "Tiles", 10)),
            _ => 4
        };
        size = MathF.Max(size, 1);
        uint seed = Seed(plan, "Seed");
        int cellX = (int)MathF.Floor(x / size);
        int cellY = (int)MathF.Floor(y / size);
        float offsetX =
            (Hash(cellX, cellY, seed) - 0.5f) *
            size *
            Option(plan, "MaximumOffset", 0.25f);
        float offsetY =
            (Hash(cellX, cellY, seed + 1) - 0.5f) *
            size *
            Option(plan, "MaximumOffset", 0.25f);
        return SamplePixel(
            source,
            width,
            height,
            (cellX + 0.5f) * size + offsetX,
            (cellY + 0.5f) * size + offsetY);
    }

    private static Vector4 Texture(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Func<Vector2, Vector4>? primaryResource)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        Vector2 uv = new(
            (x + 0.5f) / width,
            (y + 0.5f) / height);
        float texture = primaryResource is null
            ? Hash(x, y, Seed(plan, "Seed"))
            : Luminance(primaryResource(uv));
        float relief = Option(
            plan,
            "Relief",
            Option(plan, "Intensity", 20) * 0.01f);
        float edge = Sobel(source, width, height, x, y);
        float variant =
            (((int)plan.Filter - 123) % 4) * 0.08f;
        Vector3 straight = Unpremultiply(center);
        Vector3 textured = straight +
            new Vector3(
                (texture - 0.5f) * relief,
                (edge - 0.5f) * relief * 0.5f,
                (texture - edge) * (relief + variant) * 0.35f);
        return Associated(
            Vector3.Clamp(textured, Vector3.Zero, Vector3.One),
            center.W);
    }

    private static Vector4 Convolution(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Func<Vector2, Vector4>? kernel)
    {
        Vector4 total = Vector4.Zero;
        float weightTotal = 0;
        for (int kernelY = -1; kernelY <= 1; kernelY++)
        {
            for (int kernelX = -1; kernelX <= 1; kernelX++)
            {
                Vector2 kernelUv = new(
                    (kernelX + 1.5f) / 3,
                    (kernelY + 1.5f) / 3);
                float weight =
                    kernel?.Invoke(kernelUv).X ??
                    (kernelX == 0 && kernelY == 0 ? 1 : 0);
                total += SamplePixel(
                    source,
                    width,
                    height,
                    x + kernelX,
                    y + kernelY) * weight;
                weightTotal += weight;
            }
        }

        float scale = Option(plan, "Scale", 1);
        float divisor = MathF.Abs(weightTotal) < 0.000001f
            ? 1
            : weightTotal;
        Vector4 result =
            (total / divisor) * scale +
            new Vector4(Option(plan, "Offset", 0));
        if (Option(plan, "AffectAlpha", 0) < 0.5f)
        {
            result.W = SamplePixel(
                source,
                width,
                height,
                x,
                y).W;
        }
        return result;
    }

    private static Vector4 Color(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        Vector3 straight = Unpremultiply(center);
        if (plan.Filter == PrismFilterId.Solarize)
        {
            float threshold = Option(plan, "Threshold", 0.5f);
            straight = new Vector3(
                straight.X >= threshold
                    ? 1 - straight.X
                    : straight.X,
                straight.Y >= threshold
                    ? 1 - straight.Y
                    : straight.Y,
                straight.Z >= threshold
                    ? 1 - straight.Z
                    : straight.Z);
        }
        else if (plan.Filter == PrismFilterId.Color)
        {
            straight += new Vector3(
                Option(plan, "Brightness", 0));
            straight =
                (straight - new Vector3(0.5f)) *
                Option(plan, "Contrast", 1) +
                new Vector3(0.5f);
            straight *= MathF.Pow(
                2,
                Option(plan, "Exposure", 0));
            float luma = Vector3.Dot(
                straight,
                new Vector3(0.2126f, 0.7152f, 0.0722f));
            straight = Vector3.Lerp(
                new Vector3(luma),
                straight,
                Option(plan, "Saturation", 1));
            straight = RotateHue(
                straight,
                Option(plan, "Hue", 0));
            float temperature = Option(
                plan,
                "Temperature",
                0);
            straight += new Vector3(
                temperature,
                0,
                -temperature);
            Vector4 tint = OptionVector(
                plan,
                "Tint",
                Vector4.Zero);
            straight = Vector3.Lerp(
                straight,
                new Vector3(tint.X, tint.Y, tint.Z),
                tint.W);
        }
        else
        {
            int matrixSignature =
                IntegerBits(OptionVector(
                    plan,
                    "Matrix",
                    Vector4.Zero));
            if (matrixSignature != 0)
            {
                float shift =
                    ((matrixSignature & 0xff) / 255f - 0.5f) *
                    0.1f;
                straight += new Vector3(shift, -shift, shift * 0.5f);
            }
        }

        bool clamp = Option(plan, "Clamp", 1) >= 0.5f;
        if (clamp)
        {
            straight = Vector3.Clamp(
                straight,
                Vector3.Zero,
                Vector3.One);
        }
        return Associated(straight, center.W);
    }

    private static float ParameterMagnitude(
        PrismCatalogFilterPlan plan)
    {
        float total = 0;
        for (int index = 0; index < 9; index++)
        {
            Vector4 option = plan.GetOption(index);
            total += MathF.Abs(option.X) +
                MathF.Abs(option.Y) +
                MathF.Abs(option.Z) +
                MathF.Abs(option.W);
        }
        return total;
    }

    private static float Sobel(
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
        float horizontal =
            -topLeft + topRight -
            (2 * left) + (2 * right) -
            bottomLeft + bottomRight;
        float vertical =
            -topLeft - (2 * top) - topRight +
            bottomLeft + (2 * bottom) + bottomRight;
        return Math.Clamp(
            MathF.Sqrt(
                (horizontal * horizontal) +
                (vertical * vertical)),
            0,
            1);
    }

    private static Vector4 SamplePixel(
        Vector4[] source,
        int width,
        int height,
        float x,
        float y)
    {
        int sampleX = Math.Clamp(
            (int)MathF.Round(x),
            0,
            width - 1);
        int sampleY = Math.Clamp(
            (int)MathF.Round(y),
            0,
            height - 1);
        return source[(sampleY * width) + sampleX];
    }

    private static float Option(
        PrismCatalogFilterPlan plan,
        string name,
        float fallback) =>
        plan.TryGetOption(name, out Vector4 value)
            ? value.X
            : fallback;

    private static Vector4 OptionVector(
        PrismCatalogFilterPlan plan,
        string name,
        Vector4 fallback) =>
        plan.TryGetOption(name, out Vector4 value)
            ? value
            : fallback;

    private static uint Seed(
        PrismCatalogFilterPlan plan,
        string name) =>
        plan.TryGetOption(name, out Vector4 value)
            ? unchecked((uint)IntegerBits(value))
            : unchecked((uint)(int)plan.Filter * 0x9e3779b9u);

    private static int IntegerBits(Vector4 value) =>
        unchecked(
            (int)(
                ((uint)value.Y << 16) |
                ((uint)value.X & 0xffffu)));

    private static float FractalNoise(
        float x,
        float y,
        uint seed)
    {
        float total = 0;
        float weight = 0.5714286f;
        float scale = 1;
        for (int octave = 0; octave < 3; octave++)
        {
            total += Hash(
                (int)MathF.Floor(x * scale),
                (int)MathF.Floor(y * scale),
                seed + unchecked((uint)octave * 0x85ebca6bu)) *
                weight;
            scale *= 2;
            weight *= 0.5f;
        }
        return Math.Clamp(total, 0, 1);
    }

    private static float Hash(
        int x,
        int y,
        uint seed)
    {
        uint value =
            unchecked((uint)x * 0x9e3779b9u) ^
            unchecked((uint)y * 0x85ebca6bu) ^
            seed;
        value ^= value >> 16;
        value *= 0x7feb352du;
        value ^= value >> 15;
        value *= 0x846ca68bu;
        value ^= value >> 16;
        return (value & 0x00ffffffu) /
            16777215f;
    }

    private static Vector3 Quantize(
        Vector3 value,
        int levels)
    {
        float scale = MathF.Max(1, levels - 1);
        return new Vector3(
            MathF.Round(value.X * scale) / scale,
            MathF.Round(value.Y * scale) / scale,
            MathF.Round(value.Z * scale) / scale);
    }

    private static Vector3 RotateHue(
        Vector3 color,
        float degrees)
    {
        float angle = degrees * (MathF.PI / 180);
        float cosine = MathF.Cos(angle);
        float sine = MathF.Sin(angle);
        Vector3 axis = Vector3.Normalize(Vector3.One);
        return (color * cosine) +
            (Vector3.Cross(axis, color) * sine) +
            (axis * Vector3.Dot(axis, color) * (1 - cosine));
    }

    private static float Luminance(Vector4 color) =>
        Vector3.Dot(
            Unpremultiply(color),
            new Vector3(0.2126f, 0.7152f, 0.0722f));

    private static Vector4 AssociatedColor(
        Vector4 straight,
        float sourceAlpha) =>
        Associated(
            new Vector3(straight.X, straight.Y, straight.Z),
            sourceAlpha * straight.W);

    private static Vector4 Associated(
        Vector3 straight,
        float alpha) =>
        new(straight * alpha, alpha);

    private static Vector3 Unpremultiply(
        Vector4 color) =>
        color.W <= 0
            ? Vector3.Zero
            : new Vector3(
                color.X,
                color.Y,
                color.Z) / color.W;

    private static Vector4 ClampAssociated(Vector4 color)
    {
        float alpha = Math.Clamp(color.W, 0, 1);
        return new Vector4(
            Math.Clamp(color.X, 0, alpha),
            Math.Clamp(color.Y, 0, alpha),
            Math.Clamp(color.Z, 0, alpha),
            alpha);
    }

    private static Vector4 ToVector4(
        PrismPremultipliedColor color) =>
        new(
            (float)color.Red,
            (float)color.Green,
            (float)color.Blue,
            (float)color.Alpha);

    private static PrismPremultipliedColor ToPremultiplied(
        Vector4 color) =>
        new(
            color.X,
            color.Y,
            color.Z,
            color.W);
}
