using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.UI.Prism.Definitions;

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
        Func<Vector2, Vector4>? auxiliaryResource = null,
        PrismLensProfileResource? lensProfile = null,
        PrismLightingResource? lightingResource = null,
        PrismColorMatrixResource? colorMatrixResource = null)
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
        if (plan.Filter == PrismFilterId.LensFlare &&
            plan.PrimaryResourceRequired &&
            lensProfile is null)
        {
            throw new InvalidOperationException(
                "Filter 'LensFlare' requires its prepared lens profile.");
        }
        if (plan.Filter == PrismFilterId.LightingEffects &&
            plan.PrimaryResourceRequired &&
            lightingResource is null)
        {
            throw new InvalidOperationException(
                "Filter 'LightingEffects' requires its prepared lighting resource.");
        }
        if (plan.Filter == PrismFilterId.ColorMatrix &&
            plan.PrimaryResource.Value > 0 &&
            colorMatrixResource is null)
        {
            throw new InvalidOperationException(
                "Filter 'ColorMatrix' requires its referenced matrix resource.");
        }
        if (plan.Filter is not
                PrismFilterId.LensFlare and not
                PrismFilterId.LightingEffects &&
            plan.PrimaryResourceRequired &&
            primaryResource is null)
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
        IEnumerable<PrismCatalogFilterPass> passes = plan.Passes;
        if (plan.Filter == PrismFilterId.ColoredPencil)
        {
            current = PrismColoredPencilFilter.Apply(
                plan, original, width, height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.Fresco)
        {
            current = PrismFrescoFilter.Apply(
                plan, original, width, height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.Watercolor)
        {
            current = PrismWatercolorFilter.Apply(
                plan, original, width, height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.WaterPaper)
        {
            current = PrismWaterPaperFilter.Apply(
                plan,
                original,
                width,
                height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.SumiE)
        {
            current = PrismSumiEFilter.Apply(
                plan, original, width, height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.Charcoal)
        {
            current = PrismCharcoalFilter.Apply(plan, original, width, height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.ConteCrayon)
        {
            current = PrismConteCrayonFilter.Apply(
                plan,
                original,
                width,
                height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.GraphicPen)
        {
            current = PrismGraphicPenFilter.Apply(
                plan,
                original,
                width,
                height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.ChalkCharcoal)
        {
            current = PrismChalkCharcoalFilter.Apply(
                plan, original, width, height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.AccentedEdges)
        {
            current = PrismAccentedEdgesFilter.Apply(
                plan, original, width, height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.GlowingEdges)
        {
            current = PrismGlowingEdgesFilter.Apply(
                plan,
                original,
                width,
                height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.TraceContour)
        {
            current = PrismTraceContourFilter.Apply(
                plan,
                original,
                width,
                height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.DarkStrokes)
        {
            current = PrismDarkStrokesFilter.Apply(
                plan, original, width, height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.InkOutlines)
        {
            current = PrismInkOutlinesFilter.Apply(
                plan, original, width, height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.BasRelief)
        {
            current = PrismBasReliefFilter.Apply(
                plan, original, width, height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.PosterEdges)
        {
            current = PrismPosterEdgesFilter.Apply(
                plan, original, width, height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.Chrome)
        {
            current = PrismChromeFilter.Apply(
                plan,
                original,
                width,
                height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.NotePaper)
        {
            current = PrismNotePaperFilter.Apply(
                plan,
                original,
                width,
                height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.Plaster)
        {
            current = PrismPlasterFilter.Apply(
                plan,
                original,
                width,
                height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.Photocopy)
        {
            current = PrismPhotocopyFilter.Apply(
                plan,
                original,
                width,
                height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.Stamp)
        {
            current = PrismStampFilter.Apply(
                plan,
                original,
                width,
                height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.TornEdges)
        {
            current = PrismTornEdgesFilter.Apply(
                plan,
                original,
                width,
                height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.Craquelure)
        {
            current = PrismCraquelureFilter.Apply(
                plan,
                original,
                width,
                height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.Texturizer)
        {
            current = PrismTexturizerFilter.Apply(
                plan,
                original,
                width,
                height,
                primaryResource);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.Grain)
        {
            current = PrismGrainFilter.Apply(
                plan,
                original,
                width,
                height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.MosaicTiles)
        {
            current = PrismMosaicTilesFilter.Apply(
                plan,
                original,
                width,
                height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.Patchwork)
        {
            current = PrismPatchworkFilter.Apply(
                plan,
                original,
                width,
                height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.Reticulation)
        {
            current = PrismReticulationFilter.Apply(
                plan,
                original,
                width,
                height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.StainedGlass)
        {
            current = PrismStainedGlassFilter.Apply(
                plan,
                original,
                width,
                height);
            passes = [];
        }
        else if (plan.Filter == PrismFilterId.Wind)
        {
            current = PrismWindFilter.Apply(
                plan,
                original,
                width,
                height);
            passes = [];
        }
        Vector4[]? lensFlare = plan.Filter == PrismFilterId.LensFlare
            ? PrismLensFlareFilter.Render(
                plan,
                lensProfile!,
                width,
                height)
            : null;
        foreach (PrismCatalogFilterPass pass in passes)
        {
            if (pass.IsNoOp)
            {
                continue;
            }
            if (lensFlare is not null)
            {
                current = PrismLensFlareFilter.Composite(
                    current,
                    lensFlare);
                continue;
            }
            if (plan.Primitive ==
                    PrismCatalogFilterPrimitive.Morphology &&
                plan.Filter is
                    PrismFilterId.Maximum or
                    PrismFilterId.Minimum)
            {
                current = plan.Filter == PrismFilterId.Maximum
                    ? PrismMaximumFilter.ApplyPass(
                        current, width, height, pass)
                    : PrismMinimumFilter.ApplyPass(
                        current, width, height, pass);
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
                        auxiliaryResource,
                        lightingResource,
                        colorMatrixResource);
                }
            }
            current = output;
        }

        PrismPremultipliedColor[] result =
            new PrismPremultipliedColor[current.Length];
        for (int index = 0; index < current.Length; index++)
        {
            bool preserveExtendedRange =
                plan.Filter is PrismFilterId.ColorMatrix or PrismFilterId.Color &&
                Option(plan, "Clamp", 1) < 0.5f;
            Vector4 filtered = preserveExtendedRange
                ? ClampExtended(current[index])
                : ClampAssociated(current[index]);
            Vector4 blended = preserveExtendedRange
                ? ClampExtended(Vector4.Lerp(
                    original[index],
                    filtered,
                    opacity))
                : ClampAssociated(Vector4.Lerp(
                    original[index],
                    filtered,
                    opacity));
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
        Func<Vector2, Vector4>? auxiliaryResource,
        PrismLightingResource? lightingResource,
        PrismColorMatrixResource? colorMatrixResource)
    {
        Vector4 center = source[(y * width) + x];
        return plan.Primitive switch
        {
            PrismCatalogFilterPrimitive.Morphology =>
                Morphology(plan, pass, source, width, height, x, y),
            PrismCatalogFilterPrimitive.Quantization =>
                Quantization(plan, pass, source, width, height, x, y),
            PrismCatalogFilterPrimitive.Procedural =>
                PrismCatalogProceduralMath.Procedural(
                    plan,
                    pass,
                    source,
                    width,
                    height,
                    x,
                    y,
                    primaryResource,
                    auxiliaryResource,
                    lightingResource),
            PrismCatalogFilterPrimitive.Video =>
                PrismCatalogProceduralMath.Video(plan, source, width, height, x, y),
            PrismCatalogFilterPrimitive.Artistic =>
                PrismCatalogArtisticMath.Artistic(plan, pass, source, width, height, x, y),
            PrismCatalogFilterPrimitive.EdgeDetection =>
                PrismCatalogGeometryMath.EdgeDetection(plan, pass, source, width, height, x, y),
            PrismCatalogFilterPrimitive.Extrude =>
                PrismExtrudeFilter.ApplyPixel(
                    plan, source, width, height, x, y),
            PrismCatalogFilterPrimitive.Tiling =>
                PrismCatalogGeometryMath.Tiling(plan, source, width, height, x, y),
            PrismCatalogFilterPrimitive.Texture =>
                PrismCatalogGeometryMath.Texture(
                    plan,
                    pass,
                    source,
                    width,
                    height,
                    x,
                    y,
                    primaryResource),
            PrismCatalogFilterPrimitive.Convolution =>
                PrismCatalogColorMath.Convolution(
                    plan,
                    source,
                    width,
                    height,
                    x,
                    y,
                    primaryResource),
            PrismCatalogFilterPrimitive.Color =>
                PrismCatalogColorMath.Color(
                    plan,
                    source,
                    width,
                    height,
                    x,
                    y,
                    colorMatrixResource),
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
        PrismCatalogFilterPass pass,
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
                return PrismColorHalftoneFilter.ApplyPixel(
                    plan, pass, center, x, y);
            case PrismFilterId.Crystallize:
                return PrismCrystallizeFilter.ApplyPixel(
                    plan, source, width, height, x, y);
            case PrismFilterId.Pointillize:
                return PrismPointillizeFilter.ApplyPixel(
                    plan, source, width, height, x, y);
            case PrismFilterId.Facet:
                return PrismFacetFilter.ApplyPixel(
                    source, width, height, x, y);
            case PrismFilterId.Fragment:
                return PrismFragmentFilter.ApplyPixel(
                    pass, source, width, height, x, y);
            case PrismFilterId.Mezzotint:
                return PrismMezzotintFilter.ApplyPixel(
                    plan, center, x, y);
            case PrismFilterId.Mosaic:
                return PrismMosaicFilter.ApplyPixel(
                    plan, source, width, height, x, y);
            default:
                return center;
        }
    }

    internal static float SmoothStep(
        float edge0,
        float edge1,
        float value)
    {
        float t = Math.Clamp(
            (value - edge0) / (edge1 - edge0),
            0,
            1);
        return t * t * (3 - (2 * t));
    }

    internal static float Sobel(
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

    internal static float Scharr(
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
        float horizontal = (
            (-3 * topLeft) + (3 * topRight) -
            (10 * left) + (10 * right) -
            (3 * bottomLeft) + (3 * bottomRight)) / 4;
        float vertical = (
            (-3 * topLeft) - (10 * top) - (3 * topRight) +
            (3 * bottomLeft) + (10 * bottom) +
            (3 * bottomRight)) / 4;
        return Math.Clamp(
            MathF.Sqrt(
                (horizontal * horizontal) +
                (vertical * vertical)),
            0,
            1);
    }

    internal static Vector2 DirectionalReliefGradient(
        Vector4[] source,
        int width,
        int height,
        float x,
        float y,
        float radiusX,
        float radiusY)
    {
        float topLeft = Luminance(SamplePixelBilinear(
            source,
            width,
            height,
            x - radiusX,
            y - radiusY));
        float top = Luminance(SamplePixelBilinear(
            source,
            width,
            height,
            x,
            y - radiusY));
        float topRight = Luminance(SamplePixelBilinear(
            source,
            width,
            height,
            x + radiusX,
            y - radiusY));
        float left = Luminance(SamplePixelBilinear(
            source,
            width,
            height,
            x - radiusX,
            y));
        float right = Luminance(SamplePixelBilinear(
            source,
            width,
            height,
            x + radiusX,
            y));
        float bottomLeft = Luminance(SamplePixelBilinear(
            source,
            width,
            height,
            x - radiusX,
            y + radiusY));
        float bottom = Luminance(SamplePixelBilinear(
            source,
            width,
            height,
            x,
            y + radiusY));
        float bottomRight = Luminance(SamplePixelBilinear(
            source,
            width,
            height,
            x + radiusX,
            y + radiusY));
        float horizontal = (
            (-3 * topLeft) + (3 * topRight) -
            (10 * left) + (10 * right) -
            (3 * bottomLeft) + (3 * bottomRight)) / 16;
        float vertical = (
            (-3 * topLeft) - (10 * top) - (3 * topRight) +
            (3 * bottomLeft) + (10 * bottom) +
            (3 * bottomRight)) / 16;
        return new Vector2(horizontal, vertical);
    }

    internal static Vector4 SamplePixel(
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

    internal static Vector4 SampleConvolutionPixel(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        int edgeMode)
    {
        if (edgeMode == 1 &&
            (x < 0 || x >= width || y < 0 || y >= height))
        {
            return Vector4.Zero;
        }

        int sampleX = edgeMode switch
        {
            2 => WrapConvolutionCoordinate(x, width),
            3 => MirrorConvolutionCoordinate(x, width),
            _ => Math.Clamp(x, 0, width - 1)
        };
        int sampleY = edgeMode switch
        {
            2 => WrapConvolutionCoordinate(y, height),
            3 => MirrorConvolutionCoordinate(y, height),
            _ => Math.Clamp(y, 0, height - 1)
        };
        return source[(sampleY * width) + sampleX];
    }

    private static int WrapConvolutionCoordinate(int value, int length)
    {
        int wrapped = value % length;
        return wrapped < 0 ? wrapped + length : wrapped;
    }

    private static int MirrorConvolutionCoordinate(int value, int length)
    {
        if (length == 1)
        {
            return 0;
        }

        int period = (length * 2) - 2;
        int mirrored = WrapConvolutionCoordinate(value, period);
        return mirrored < length
            ? mirrored
            : period - mirrored;
    }

    internal static Vector4 SamplePixelBilinear(
        Vector4[] source,
        int width,
        int height,
        float x,
        float y)
    {
        float clampedX = Math.Clamp(x, 0, width - 1);
        float clampedY = Math.Clamp(y, 0, height - 1);
        int left = (int)MathF.Floor(clampedX);
        int top = (int)MathF.Floor(clampedY);
        int right = Math.Min(left + 1, width - 1);
        int bottom = Math.Min(top + 1, height - 1);
        float horizontal = clampedX - left;
        float vertical = clampedY - top;
        Vector4 upper = Vector4.Lerp(
            source[(top * width) + left],
            source[(top * width) + right],
            horizontal);
        Vector4 lower = Vector4.Lerp(
            source[(bottom * width) + left],
            source[(bottom * width) + right],
            horizontal);
        return Vector4.Lerp(upper, lower, vertical);
    }

    internal static float Option(
        PrismCatalogFilterPlan plan,
        string name,
        float fallback) =>
        plan.TryGetOption(name, out Vector4 value)
            ? value.X
            : fallback;

    internal static Vector4 OptionVector(
        PrismCatalogFilterPlan plan,
        string name,
        Vector4 fallback) =>
        plan.TryGetOption(name, out Vector4 value)
            ? value
            : fallback;

    internal static uint Seed(
        PrismCatalogFilterPlan plan,
        string name) =>
        plan.TryGetOption(name, out Vector4 value)
            ? unchecked((uint)IntegerBits(value))
            : unchecked((uint)(int)plan.Filter * 0x9e3779b9u);

    internal static int IntegerBits(Vector4 value) =>
        unchecked(
            (int)(
                ((uint)value.Y << 16) |
                ((uint)value.X & 0xffffu)));

    internal static float Hash(
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

    internal static Vector3 Quantize(
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

    internal static float Luminance(Vector4 color) =>
        Vector3.Dot(
            Unpremultiply(color),
            new Vector3(0.2126f, 0.7152f, 0.0722f));

    internal static float StraightLuminance(Vector3 color) =>
        Vector3.Dot(
            color,
            new Vector3(0.2126f, 0.7152f, 0.0722f));

    internal static float Fraction(float value) =>
        value - MathF.Floor(value);

    internal static Vector4 AssociatedColor(
        Vector4 straight,
        float sourceAlpha) =>
        Associated(
            new Vector3(straight.X, straight.Y, straight.Z),
            sourceAlpha * straight.W);

    internal static Vector4 Associated(
        Vector3 straight,
        float alpha) =>
        new(straight * alpha, alpha);

    internal static Vector3 Unpremultiply(
        Vector4 color) =>
        color.W <= 0
            ? Vector3.Zero
            : new Vector3(
                color.X,
                color.Y,
                color.Z) / color.W;

    internal static Vector4 ClampAssociated(Vector4 color)
    {
        float alpha = Math.Clamp(color.W, 0, 1);
        return new Vector4(
            Math.Clamp(color.X, 0, alpha),
            Math.Clamp(color.Y, 0, alpha),
            Math.Clamp(color.Z, 0, alpha),
            alpha);
    }

    private static Vector4 ClampExtended(Vector4 color) =>
        new(
            Math.Clamp(
                color.X,
                -PrismColorMatrixFilter.MaximumHalfValue,
                PrismColorMatrixFilter.MaximumHalfValue),
            Math.Clamp(
                color.Y,
                -PrismColorMatrixFilter.MaximumHalfValue,
                PrismColorMatrixFilter.MaximumHalfValue),
            Math.Clamp(
                color.Z,
                -PrismColorMatrixFilter.MaximumHalfValue,
                PrismColorMatrixFilter.MaximumHalfValue),
            Math.Clamp(color.W, 0, 1));

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
