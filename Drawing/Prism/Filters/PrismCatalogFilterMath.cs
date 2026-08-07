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
                Procedural(
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
                Video(plan, source, width, height, x, y),
            PrismCatalogFilterPrimitive.Artistic =>
                Artistic(plan, pass, source, width, height, x, y),
            PrismCatalogFilterPrimitive.EdgeDetection =>
                EdgeDetection(plan, pass, source, width, height, x, y),
            PrismCatalogFilterPrimitive.Extrude =>
                PrismExtrudeFilter.ApplyPixel(
                    plan, source, width, height, x, y),
            PrismCatalogFilterPrimitive.Tiling =>
                Tiling(plan, source, width, height, x, y),
            PrismCatalogFilterPrimitive.Texture =>
                Texture(
                    plan,
                    pass,
                    source,
                    width,
                    height,
                    x,
                    y,
                    primaryResource),
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
                Color(
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

    internal static Vector4 BilateralMosaic(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        float cellX,
        float cellY)
    {
        const float inverseTwoRangeSigmaSquared = 8;
        float centerX =
            (MathF.Floor((x + 0.5f) / cellX) + 0.5f) * cellX;
        float centerY =
            (MathF.Floor((y + 0.5f) / cellY) + 0.5f) * cellY;
        Vector4 reference = SamplePixelBilinear(
            source,
            width,
            height,
            centerX - 0.5f,
            centerY - 0.5f);
        Vector3 referenceStraight = Unpremultiply(reference);
        Vector4 weighted = Vector4.Zero;
        float totalWeight = 0;
        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                Vector4 sample =
                    offsetX == 0 && offsetY == 0
                        ? reference
                        : SamplePixelBilinear(
                            source,
                            width,
                            height,
                            centerX +
                                (offsetX * cellX / 3) -
                                0.5f,
                            centerY +
                                (offsetY * cellY / 3) -
                                0.5f);
                Vector3 colorDelta =
                    Unpremultiply(sample) - referenceStraight;
                float alphaDelta = sample.W - reference.W;
                float rangeDistanceSquared =
                    Vector3.Dot(colorDelta, colorDelta) +
                    (alphaDelta * alphaDelta);
                float spatialWeight = MathF.Exp(
                    -0.5f *
                    ((offsetX * offsetX) +
                        (offsetY * offsetY)));
                float rangeWeight = MathF.Exp(
                    -rangeDistanceSquared *
                    inverseTwoRangeSigmaSquared);
                float weight = spatialWeight * rangeWeight;
                weighted += sample * weight;
                totalWeight += weight;
            }
        }

        return weighted / totalWeight;
    }

    internal static Vector4 AnisotropicKuwahara(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        const int radius = 3;
        const float alpha = 1;
        const float zeta = 2f / radius;
        const float gamma = 3 * MathF.PI / 16;
        const float diagonal = 0.7071067811865476f;
        float eta =
            (zeta + MathF.Cos(gamma)) /
            MathF.Pow(MathF.Sin(gamma), 2);
        Vector4 center = SamplePixel(
            source,
            width,
            height,
            x,
            y);
        if (center.W <= 0)
        {
            return Vector4.Zero;
        }

        (float tensorX, float tensorCross, float tensorY) =
            FacetStructureTensor(
                source,
                width,
                height,
                x,
                y);
        float discriminant = MathF.Sqrt(
            MathF.Max(
                0,
                ((tensorX - tensorY) *
                    (tensorX - tensorY)) +
                (4 * tensorCross * tensorCross)));
        float lambda1 =
            0.5f * (tensorX + tensorY + discriminant);
        float lambda2 =
            0.5f * (tensorX + tensorY - discriminant);
        float anisotropy =
            (lambda1 + lambda2) <= 0.000001f
                ? 0
                : Math.Clamp(
                    (lambda1 - lambda2) /
                        (lambda1 + lambda2),
                    0,
                    1);
        float angle =
            (0.5f * MathF.Atan2(
                2 * tensorCross,
                tensorX - tensorY)) +
            (MathF.PI * 0.5f);
        float cosine = MathF.Cos(angle);
        float sine = MathF.Sin(angle);
        float majorRadius =
            radius * ((alpha + anisotropy) / alpha);
        float minorRadius =
            radius * (alpha / (alpha + anisotropy));
        int sampleRadius =
            (int)MathF.Ceiling(majorRadius);

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
                if ((localX * localX) +
                        (localY * localY) >
                    1)
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

                float gaussian =
                    MathF.Exp(
                        -3.125f *
                        ((localX * localX) +
                            (localY * localY))) /
                    sectorTotal;
                Vector4 sample = SamplePixel(
                    source,
                    width,
                    height,
                    x + offsetX,
                    y + offsetY);
                if (sample.W <= 0)
                {
                    continue;
                }
                Vector3 straight = Vector3.Clamp(
                    Unpremultiply(sample),
                    Vector3.Zero,
                    Vector3.One);
                for (int sector = 0; sector < 8; sector++)
                {
                    float weight =
                        sectorWeights[sector] *
                        gaussian *
                        sample.W;
                    colorSums[sector] += straight * weight;
                    squareSums[sector] +=
                        straight * straight * weight;
                    weightSums[sector] += weight;
                }
            }
        }

        Vector3 result = Vector3.Zero;
        float resultWeight = 0;
        Vector3 centerStraight = Unpremultiply(center);
        for (int sector = 0; sector < 8; sector++)
        {
            if (weightSums[sector] <= 0.000001f)
            {
                continue;
            }
            Vector3 mean =
                colorSums[sector] / weightSums[sector];
            Vector3 variance = Vector3.Max(
                Vector3.Zero,
                (squareSums[sector] /
                    weightSums[sector]) -
                (mean * mean));
            float varianceSum =
                variance.X + variance.Y + variance.Z;
            float confidence =
                1 /
                (1 +
                    MathF.Pow(
                        1000 * varianceSum,
                        4));
            result += mean * confidence;
            resultWeight += confidence;
        }
        if (resultWeight <= 0.000001f)
        {
            result = centerStraight;
        }
        else
        {
            result /= resultWeight;
        }
        return Associated(
            Vector3.Clamp(result, Vector3.Zero, Vector3.One),
            center.W);
    }

    private static (
        float Horizontal,
        float Cross,
        float Vertical) FacetStructureTensor(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector3 topLeft = FacetStraightSample(
            source,
            width,
            height,
            x - 1,
            y - 1);
        Vector3 top = FacetStraightSample(
            source,
            width,
            height,
            x,
            y - 1);
        Vector3 topRight = FacetStraightSample(
            source,
            width,
            height,
            x + 1,
            y - 1);
        Vector3 left = FacetStraightSample(
            source,
            width,
            height,
            x - 1,
            y);
        Vector3 right = FacetStraightSample(
            source,
            width,
            height,
            x + 1,
            y);
        Vector3 bottomLeft = FacetStraightSample(
            source,
            width,
            height,
            x - 1,
            y + 1);
        Vector3 bottom = FacetStraightSample(
            source,
            width,
            height,
            x,
            y + 1);
        Vector3 bottomRight = FacetStraightSample(
            source,
            width,
            height,
            x + 1,
            y + 1);
        Vector3 horizontal =
            -topLeft + topRight -
            (2 * left) + (2 * right) -
            bottomLeft + bottomRight;
        Vector3 vertical =
            -topLeft - (2 * top) - topRight +
            bottomLeft + (2 * bottom) + bottomRight;
        return (
            Vector3.Dot(horizontal, horizontal),
            Vector3.Dot(horizontal, vertical),
            Vector3.Dot(vertical, vertical));
    }

    private static Vector3 FacetStraightSample(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 sample = SamplePixel(
            source,
            width,
            height,
            x,
            y);
        return sample.W <= 0
            ? Vector3.Zero
            : Vector3.Clamp(
                Unpremultiply(sample),
                Vector3.Zero,
                Vector3.One);
    }

    private static void FacetSectorWeights(
        float x,
        float y,
        float zeta,
        float eta,
        float diagonal,
        Span<float> weights)
    {
        FacetCardinalSectorWeights(
            x,
            y,
            zeta,
            eta,
            weights,
            0);
        float rotatedX = diagonal * (x - y);
        float rotatedY = diagonal * (x + y);
        FacetCardinalSectorWeights(
            rotatedX,
            rotatedY,
            zeta,
            eta,
            weights,
            1);
    }

    private static void FacetCardinalSectorWeights(
        float x,
        float y,
        float zeta,
        float eta,
        Span<float> weights,
        int start)
    {
        float xPolynomial = zeta - (eta * x * x);
        float yPolynomial = zeta - (eta * y * y);
        weights[start] = FacetSquaredPositive(y + xPolynomial);
        weights[start + 2] =
            FacetSquaredPositive(-x + yPolynomial);
        weights[start + 4] =
            FacetSquaredPositive(-y + xPolynomial);
        weights[start + 6] =
            FacetSquaredPositive(x + yPolynomial);
    }

    private static float FacetSquaredPositive(float value)
    {
        float positive = MathF.Max(0, value);
        return positive * positive;
    }

    internal static Vector4 ColorHalftone(
        PrismCatalogFilterPlan plan,
        Vector4 source,
        Vector2 pixel,
        float maxRadius)
    {
        if (maxRadius <= 0)
        {
            return source;
        }

        Vector3 straight = Vector3.Clamp(
            Unpremultiply(source),
            Vector3.Zero,
            Vector3.One);
        float black = 1 - MathF.Max(
            straight.X,
            MathF.Max(straight.Y, straight.Z));
        float colorRange = 1 - black;
        Vector3 cmy = colorRange <= 0.000001f
            ? Vector3.Zero
            : Vector3.Clamp(
                (Vector3.One - straight - new Vector3(black)) /
                    colorRange,
                Vector3.Zero,
                Vector3.One);
        Vector4 angles = OptionVector(
            plan,
            "Angles",
            new Vector4(108, 162, 90, 45));
        float cyanInk = ColorHalftoneInk(
            pixel,
            maxRadius,
            angles.X,
            cmy.X);
        float magentaInk = ColorHalftoneInk(
            pixel,
            maxRadius,
            angles.Y,
            cmy.Y);
        float yellowInk = ColorHalftoneInk(
            pixel,
            maxRadius,
            angles.Z,
            cmy.Z);
        float blackInk = ColorHalftoneInk(
            pixel,
            maxRadius,
            angles.W,
            black);
        float blackPaper = 1 - blackInk;
        return Associated(
            new Vector3(
                (1 - cyanInk) * blackPaper,
                (1 - magentaInk) * blackPaper,
                (1 - yellowInk) * blackPaper),
            source.W);
    }

    private static float ColorHalftoneInk(
        Vector2 pixel,
        float maxRadius,
        float angleDegrees,
        float coverage)
    {
        if (coverage <= 0)
        {
            return 0;
        }
        if (coverage >= 1)
        {
            return 1;
        }

        float threshold = ColorHalftoneThreshold(
            pixel,
            maxRadius,
            angleDegrees);
        float antialiasWidth = Math.Clamp(
            0.5f / maxRadius,
            0.0001f,
            0.25f);
        return SmoothStep(
            threshold - antialiasWidth,
            threshold + antialiasWidth,
            coverage);
    }

    private static float ColorHalftoneThreshold(
        Vector2 pixel,
        float maxRadius,
        float angleDegrees)
    {
        float radians = angleDegrees * (MathF.PI / 180);
        float cosine = MathF.Cos(radians);
        float sine = MathF.Sin(radians);
        Vector2 rotated = new(
            (cosine * pixel.X) - (sine * pixel.Y),
            (sine * pixel.X) + (cosine * pixel.Y));
        float cellSize = MathF.Sqrt(2) * maxRadius;
        Vector2 local = rotated - new Vector2(
            MathF.Floor((rotated.X / cellSize) + 0.5f) * cellSize,
            MathF.Floor((rotated.Y / cellSize) + 0.5f) * cellSize);
        float halfCell = cellSize * 0.5f;
        float normalizedSquaredDistance = Math.Clamp(
            Vector2.Dot(local, local) / (halfCell * halfCell),
            0,
            2);
        float circleCoverage = MathF.PI * 0.25f;
        if (normalizedSquaredDistance <= 1)
        {
            return circleCoverage * normalizedSquaredDistance;
        }

        return circleCoverage +
            ((1 - circleCoverage) *
                (normalizedSquaredDistance - 1));
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

    private static Vector3 CookTorranceGgxSpecular(
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

    private static Vector4 Artistic(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        if (plan.Filter == PrismFilterId.DryBrush)
        {
            return PrismDryBrushFilter.ApplyPixel(
                plan,
                pass,
                source,
                width,
                height,
                x,
                y);
        }
        if (plan.Filter == PrismFilterId.Cutout)
        {
            return PrismCutoutFilter.ApplyPixel(
                plan,
                pass,
                source,
                width,
                height,
                x,
                y);
        }
        if (plan.Filter == PrismFilterId.FilmGrain)
        {
            return PrismFilmGrainFilter.ApplyPixel(
                plan,
                SamplePixel(source, width, height, x, y),
                x,
                y);
        }
        if (plan.Filter == PrismFilterId.AngledStrokes)
        {
            return PrismAngledStrokesFilter.ApplyPixel(
                plan,
                pass,
                source,
                width,
                height,
                x,
                y);
        }
        if (plan.Filter == PrismFilterId.PaintDaubs)
        {
            return PrismPaintDaubsFilter.ApplyPixel(
                plan,
                pass,
                source,
                width,
                height,
                x,
                y);
        }
        if (plan.Filter == PrismFilterId.PaletteKnife)
        {
            return PrismPaletteKnifeFilter.ApplyPixel(
                plan,
                pass,
                source,
                width,
                height,
                x,
                y);
        }
        if (plan.Filter == PrismFilterId.PlasticWrap)
        {
            return PrismPlasticWrapFilter.ApplyPixel(
                plan,
                pass,
                source,
                width,
                height,
                x,
                y);
        }
        if (plan.Filter == PrismFilterId.RoughPastels)
        {
            return PrismRoughPastelsFilter.ApplyPixel(
                plan,
                pass,
                source,
                width,
                height,
                x,
                y);
        }
        if (plan.Filter == PrismFilterId.SmudgeStick)
        {
            return PrismSmudgeStickFilter.ApplyPixel(
                plan,
                pass,
                source,
                width,
                height,
                x,
                y);
        }
        if (plan.Filter == PrismFilterId.Sponge)
        {
            return PrismSpongeFilter.ApplyPixel(
                plan,
                pass,
                source,
                width,
                height,
                x,
                y);
        }
        if (plan.Filter == PrismFilterId.Underpainting)
        {
            return PrismUnderpaintingFilter.ApplyPixel(
                plan,
                pass,
                source,
                width,
                height,
                x,
                y);
        }
        if (plan.Filter == PrismFilterId.Crosshatch)
        {
            return PrismCrosshatchFilter.ApplyPixel(
                plan,
                SamplePixel(source, width, height, x, y),
                x,
                y);
        }
        if (plan.Filter == PrismFilterId.Spatter)
        {
            return PrismSpatterFilter.ApplyPixel(
                plan, source, width, height, x, y);
        }
        if (plan.Filter == PrismFilterId.SprayedStrokes)
        {
            return PrismSprayedStrokesFilter.ApplyPixel(
                plan,
                source,
                width,
                height,
                x,
                y);
        }

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

    internal static Vector4 Spatter(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        float sprayRadius = MathF.Max(
            0,
            Option(plan, "SprayRadius", 10));
        if (center.W <= 0 || sprayRadius <= 0)
        {
            return center;
        }

        PrismSpatterPointField field =
            PrismRecursiveWangBlueNoise.PointField;
        float averageSpacingInCells =
            field.GridSize / MathF.Sqrt(field.PointCount);
        float cellSize = sprayRadius / averageSpacingInCells;
        Vector2 seedOffset = PrismRecursiveWangBlueNoise.SeedOffset(
            plan.SpatterSeed);
        Vector2 pattern = new(
            ((x + 0.5f) / cellSize) + seedOffset.X,
            ((y + 0.5f) / cellSize) + seedOffset.Y);
        int baseCellX = (int)MathF.Floor(pattern.X);
        int baseCellY = (int)MathF.Floor(pattern.Y);
        float smoothness = Math.Clamp(
            Option(plan, "Smoothness", 5) / 15,
            0,
            1);
        float edgeWidth = 0.04f + (0.24f * smoothness);
        float bestCoverage = 0;
        Vector3 bestColor = Vector3.One;

        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                int cellX = baseCellX + offsetX;
                int cellY = baseCellY + offsetY;
                for (int layer = 0;
                    layer < field.LayerCount;
                    layer++)
                {
                    Vector4 point = field.GetPoint(
                        cellX,
                        cellY,
                        layer);
                    if (point.W <= 0)
                    {
                        continue;
                    }

                    Vector2 pointPosition = new(
                        cellX + point.X,
                        cellY + point.Y);
                    Vector2 pointPixel =
                        (pointPosition - seedOffset) * cellSize;
                    Vector4 pointSource = SamplePixel(
                        source,
                        width,
                        height,
                        (int)MathF.Floor(pointPixel.X),
                        (int)MathF.Floor(pointPixel.Y));
                    float density =
                        (1 - Luminance(pointSource)) * pointSource.W;
                    if (point.Z > density)
                    {
                        continue;
                    }

                    float variationValue =
                        (point.X * 37) +
                        (point.Y * 91) +
                        (point.Z * 65_521);
                    float variation =
                        variationValue - MathF.Floor(variationValue);
                    float pointRadius = 0.68f + (0.2f * variation);
                    float distance = Vector2.Distance(
                        pattern,
                        pointPosition);
                    float coverage = 1 - SmoothStep(
                        pointRadius - edgeWidth,
                        pointRadius + edgeWidth,
                        distance);
                    if (coverage <= bestCoverage)
                    {
                        continue;
                    }

                    bestCoverage = coverage;
                    bestColor = Vector3.Clamp(
                        Unpremultiply(pointSource),
                        Vector3.Zero,
                        Vector3.One);
                }
            }
        }

        return Associated(
            Vector3.Lerp(Vector3.One, bestColor, bestCoverage),
            center.W);
    }

    internal static Vector4 SprayedStrokes(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        float strokeLength = MathF.Max(
            Option(plan, "StrokeLength", 12),
            0);
        float sprayRadius = MathF.Max(
            Option(plan, "SprayRadius", 7),
            0);
        if (center.W <= 0 ||
            (strokeLength <= 0 && sprayRadius <= 0))
        {
            return center;
        }

        Vector2 direction = (int)MathF.Round(
            Option(plan, "Direction", 0)) switch
        {
            1 => Vector2.UnitX,
            2 => Vector2.Normalize(new Vector2(-1, 1)),
            3 => Vector2.UnitY,
            _ => Vector2.Normalize(Vector2.One)
        };
        Vector2 normal = new(-direction.Y, direction.X);
        uint seed = Seed(plan, "Seed");
        Vector2 seedOffset = PrismRecursiveWangBlueNoise.SeedOffset(seed);
        Vector2 pixel = new(x + 0.5f, y + 0.5f);
        float cellSize = MathF.Max(sprayRadius, 2);
        Vector2 pattern = (pixel / cellSize) + seedOffset;
        int baseCellX = (int)MathF.Floor(pattern.X);
        int baseCellY = (int)MathF.Floor(pattern.Y);
        Vector3 centerColor = Vector3.Clamp(
            Unpremultiply(center),
            Vector3.Zero,
            Vector3.One);
        Vector3 accumulated = Vector3.Zero;
        float totalWeight = 0;
        PrismSpatterPointField field =
            PrismRecursiveWangBlueNoise.PointField;

        for (int index = 0; index < 7; index++)
        {
            int cellOffsetX = index switch
            {
                0 or 5 => -1,
                2 or 6 => 1,
                _ => 0
            };
            int cellOffsetY = index switch
            {
                1 or 6 => -1,
                4 or 5 => 1,
                _ => 0
            };
            int cellX = baseCellX + cellOffsetX;
            int cellY = baseCellY + cellOffsetY;
            Vector4 point = field.GetPoint(cellX, cellY, layer: 0);
            uint salt = unchecked((uint)(index + 1) * 0x9e3779b9u);
            Vector2 jitter = point.W > 0
                ? new Vector2(point.X, point.Y)
                : new Vector2(
                    Hash(cellX, cellY, seed ^ salt),
                    Hash(cellX, cellY, seed ^ salt ^ 0x85ebca6bu));
            float position = (index - 3) / 6f;
            float longitudinal =
                (position * strokeLength) +
                ((jitter.X - 0.5f) * strokeLength / 7);
            float lateral =
                (jitter.Y - 0.5f) * 2 * sprayRadius;
            Vector2 samplePosition =
                pixel +
                (direction * longitudinal) +
                (normal * lateral);
            Vector4 sample = SamplePixelBilinear(
                source,
                width,
                height,
                samplePosition.X - 0.5f,
                samplePosition.Y - 0.5f);
            if (sample.W <= 0)
            {
                continue;
            }

            float weight = index switch
            {
                0 or 6 => 0.08f,
                1 or 5 => 0.12f,
                2 or 4 => 0.18f,
                _ => 0.24f
            };
            Vector3 sampleColor = Vector3.Clamp(
                Unpremultiply(sample),
                Vector3.Zero,
                Vector3.One);
            Vector3 colorDelta = sampleColor - centerColor;
            weight /=
                1 + (3 * Vector3.Dot(colorDelta, colorDelta));
            accumulated += sampleColor * weight;
            totalWeight += weight;
        }

        if (totalWeight <= 0)
        {
            return center;
        }
        Vector3 filtered = accumulated / totalWeight;
        float sprayMix = sprayRadius /
            MathF.Max(strokeLength + sprayRadius, 0.0001f);
        float grain = 0.75f + (0.25f * Hash(x, y, seed));
        float strength = (0.58f + (0.22f * sprayMix)) * grain;
        return Associated(
            Vector3.Lerp(centerColor, filtered, strength),
            center.W);
    }

    internal static Vector4 Crosshatch(
        PrismCatalogFilterPlan plan,
        Vector4 source,
        int x,
        int y)
    {
        if (source.W <= 0)
        {
            return Vector4.Zero;
        }

        float strength = Math.Clamp(
            plan.Options2.X,
            0,
            1);
        if (strength <= 0)
        {
            return source;
        }

        Vector3 straight = Vector3.Clamp(
            Unpremultiply(source),
            Vector3.Zero,
            Vector3.One);
        float darkness = 1 - StraightLuminance(straight);
        float period = MathF.Max(
            MathF.Abs(plan.Options0.X),
            4);
        float sharpness = Math.Clamp(
            plan.Options1.X / 10,
            0,
            1);
        float halfWidth = Math.Clamp(period * 0.075f, 0.45f, 1.4f);
        float spatialSoftness = 1.5f - (1.35f * sharpness);
        float toneSoftness = 0.18f - (0.16f * sharpness);
        float pixelX = x + 0.5f;
        float pixelY = y + 0.5f;
        float rising = (pixelX + pixelY) * 0.70710678118f;
        float falling = (pixelX - pixelY) * 0.70710678118f;
        float phaseStep = period / 3;

        float clear = 1;
        clear *= 1 - CrosshatchTransition(
            rising,
            period,
            0,
            halfWidth,
            spatialSoftness,
            darkness,
            0.06f,
            toneSoftness);
        clear *= 1 - CrosshatchTransition(
            falling,
            period,
            0,
            halfWidth,
            spatialSoftness,
            darkness,
            0.22f,
            toneSoftness);
        clear *= 1 - CrosshatchTransition(
            rising,
            period,
            phaseStep,
            halfWidth,
            spatialSoftness,
            darkness,
            0.38f,
            toneSoftness);
        clear *= 1 - CrosshatchTransition(
            falling,
            period,
            phaseStep,
            halfWidth,
            spatialSoftness,
            darkness,
            0.54f,
            toneSoftness);
        clear *= 1 - CrosshatchTransition(
            rising,
            period,
            2 * phaseStep,
            halfWidth,
            spatialSoftness,
            darkness,
            0.70f,
            toneSoftness);
        clear *= 1 - CrosshatchTransition(
            falling,
            period,
            2 * phaseStep,
            halfWidth,
            spatialSoftness,
            darkness,
            0.86f,
            toneSoftness);

        Vector3 hatch = new(Math.Clamp(clear, 0, 1));
        return Associated(
            Vector3.Lerp(straight, hatch, strength),
            source.W);
    }

    private static float CrosshatchTransition(
        float coordinate,
        float period,
        float phase,
        float halfWidth,
        float spatialSoftness,
        float darkness,
        float toneThreshold,
        float toneSoftness)
    {
        float cycle =
            ((coordinate + phase) / period) -
            MathF.Floor((coordinate + phase) / period);
        float distance = MathF.Abs(cycle - 0.5f) * period;
        float line = 1 - SmoothStep(
            MathF.Max(halfWidth - spatialSoftness, 0),
            halfWidth + spatialSoftness,
            distance);
        float tone = SmoothStep(
            toneThreshold - toneSoftness,
            toneThreshold + toneSoftness,
            darkness);
        return line * tone;
    }

    internal static Vector4[] Watercolor(
        PrismCatalogFilterPlan plan,
        Vector4[] original,
        int width,
        int height)
    {
        Vector4[] current = WatercolorMeanShift(
            plan,
            plan.Passes[0],
            original,
            width,
            height);
        current = WatercolorMeanShift(
            plan,
            plan.Passes[1],
            current,
            width,
            height);
        for (int passIndex = 2; passIndex <= 5; passIndex++)
        {
            current = WatercolorMorphology(
                plan.Passes[passIndex],
                current,
                width,
                height,
                erode: passIndex is 2 or 5);
        }

        Vector4[] result = new Vector4[current.Length];
        float edgeRadius = MathF.Max(
            plan.Passes[6].RadiusX,
            plan.Passes[6].RadiusY);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                result[(y * width) + x] = WatercolorComposite(
                    plan,
                    original,
                    current,
                    width,
                    height,
                    x,
                    y,
                    edgeRadius);
            }
        }
        return result;
    }

    private static Vector4[] WatercolorMeanShift(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height)
    {
        Vector4[] result = new Vector4[source.Length];
        float radius = MathF.Max(pass.RadiusX, pass.RadiusY);
        float detail = Math.Clamp(
            Option(plan, "BrushDetail", 9) / 16,
            0,
            1);
        float rangeSigma = 0.3f - (0.24f * detail);
        float rangeDivisor = 2 * rangeSigma * rangeSigma;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector4 center = SamplePixel(
                    source,
                    width,
                    height,
                    x,
                    y);
                if (center.W <= 0.000001f)
                {
                    result[(y * width) + x] = Vector4.Zero;
                    continue;
                }

                Vector3 centerColor = Unpremultiply(center);
                Vector3 colorTotal = Vector3.Zero;
                float weightTotal = 0;
                for (int offsetY = -3; offsetY <= 3; offsetY++)
                {
                    for (int offsetX = -3; offsetX <= 3; offsetX++)
                    {
                        Vector4 sample = SamplePixelBilinear(
                            source,
                            width,
                            height,
                            x + (offsetX * radius / 3),
                            y + (offsetY * radius / 3));
                        if (sample.W <= 0.000001f)
                        {
                            continue;
                        }

                        Vector3 sampleColor = Unpremultiply(sample);
                        Vector3 difference = sampleColor - centerColor;
                        float spatialDistance =
                            (offsetX * offsetX) +
                            (offsetY * offsetY);
                        float weight = MathF.Exp(
                                -spatialDistance / 6f) *
                            MathF.Exp(
                                -Vector3.Dot(difference, difference) /
                                rangeDivisor) *
                            MathF.Exp(-MathF.Abs(sample.W - center.W) * 8);
                        colorTotal += sampleColor * weight;
                        weightTotal += weight;
                    }
                }

                Vector3 shifted = weightTotal <= 0.000001f
                    ? centerColor
                    : colorTotal / weightTotal;
                result[(y * width) + x] = Associated(
                    Vector3.Clamp(
                        shifted,
                        Vector3.Zero,
                        Vector3.One),
                    center.W);
            }
        }
        return result;
    }

    private static Vector4[] WatercolorMorphology(
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        bool erode)
    {
        Vector4[] result = new Vector4[source.Length];
        float radius = MathF.Max(pass.RadiusX, pass.RadiusY);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector4 center = SamplePixel(
                    source,
                    width,
                    height,
                    x,
                    y);
                if (center.W <= 0.000001f)
                {
                    result[(y * width) + x] = Vector4.Zero;
                    continue;
                }

                Vector3 selected = erode
                    ? Vector3.One
                    : Vector3.Zero;
                bool found = false;
                for (int offsetY = -2; offsetY <= 2; offsetY++)
                {
                    for (int offsetX = -2; offsetX <= 2; offsetX++)
                    {
                        if ((offsetX * offsetX) +
                                (offsetY * offsetY) >
                            4)
                        {
                            continue;
                        }
                        Vector4 sample = SamplePixelBilinear(
                            source,
                            width,
                            height,
                            x + (offsetX * radius / 2),
                            y + (offsetY * radius / 2));
                        if (sample.W <= 0.000001f ||
                            MathF.Abs(sample.W - center.W) > 0.25f)
                        {
                            continue;
                        }

                        Vector3 color = Unpremultiply(sample);
                        selected = erode
                            ? Vector3.Min(selected, color)
                            : Vector3.Max(selected, color);
                        found = true;
                    }
                }

                result[(y * width) + x] = Associated(
                    found ? selected : Unpremultiply(center),
                    center.W);
            }
        }
        return result;
    }

    private static Vector4 WatercolorComposite(
        PrismCatalogFilterPlan plan,
        Vector4[] original,
        Vector4[] abstracted,
        int width,
        int height,
        int x,
        int y,
        float edgeRadius)
    {
        Vector4 originalPixel = SamplePixel(
            original,
            width,
            height,
            x,
            y);
        if (originalPixel.W <= 0.000001f)
        {
            return Vector4.Zero;
        }

        float textureStrength = Math.Clamp(
            Option(plan, "Texture", 3) / 10,
            0,
            1);
        float paper = WatercolorPaperHeight(x, y);
        float paperHorizontal =
            WatercolorPaperHeight(x + 1, y) -
            WatercolorPaperHeight(x - 1, y);
        float paperVertical =
            WatercolorPaperHeight(x, y + 1) -
            WatercolorPaperHeight(x, y - 1);
        float wobble = textureStrength * 0.45f;
        float sampleX = x + (paperHorizontal * wobble);
        float sampleY = y + (paperVertical * wobble);
        Vector4 baseSample = SamplePixelBilinear(
            abstracted,
            width,
            height,
            sampleX,
            sampleY);
        Vector3 color = Unpremultiply(baseSample);

        Vector3 horizontal = Vector3.Abs(
            Unpremultiply(SamplePixelBilinear(
                abstracted,
                width,
                height,
                sampleX - edgeRadius,
                sampleY)) -
            Unpremultiply(SamplePixelBilinear(
                abstracted,
                width,
                height,
                sampleX + edgeRadius,
                sampleY)));
        Vector3 vertical = Vector3.Abs(
            Unpremultiply(SamplePixelBilinear(
                abstracted,
                width,
                height,
                sampleX,
                sampleY - edgeRadius)) -
            Unpremultiply(SamplePixelBilinear(
                abstracted,
                width,
                height,
                sampleX,
                sampleY + edgeRadius)));
        float edge = Math.Clamp(
            (horizontal.X + horizontal.Y + horizontal.Z +
                vertical.X + vertical.Y + vertical.Z) / 6,
            0,
            1);
        float shadow = Math.Clamp(
            Option(plan, "ShadowIntensity", 1),
            0,
            4);
        color = WatercolorPigmentDensity(
            color,
            1 + (edge * shadow * 0.85f));

        if (textureStrength > 0)
        {
            float turbulence = WatercolorValueNoise(
                x / 32f,
                y / 32f,
                0x9a4e21d3u);
            float dispersion =
                (0.65f * WatercolorValueNoise(
                    x / 4f,
                    y / 4f,
                    0x68bc21ebu)) +
                (0.35f * WatercolorValueNoise(
                    x / 1.75f,
                    y / 1.75f,
                    0x2e5be93du));
            color = WatercolorPigmentDensity(
                color,
                1 + ((turbulence - 0.5f) *
                    0.28f * textureStrength));
            color = WatercolorPigmentDensity(
                color,
                1 + ((dispersion - 0.5f) *
                    0.2f * textureStrength));
            color = WatercolorPigmentDensity(
                color,
                1 + ((paper - 0.5f) *
                    0.34f * textureStrength));

            float dryThreshold = 0.72f -
                (0.12f * textureStrength);
            float dryGap = SmoothStep(
                    dryThreshold,
                    dryThreshold + 0.14f,
                    paper) *
                textureStrength *
                (0.25f + (0.4f * StraightLuminance(color)));
            color = Vector3.Lerp(color, Vector3.One, dryGap);
        }

        return Associated(
            Vector3.Clamp(color, Vector3.Zero, Vector3.One),
            originalPixel.W);
    }

    private static Vector3 WatercolorPigmentDensity(
        Vector3 color,
        float density)
    {
        Vector3 result = color -
            ((color - (color * color)) * (density - 1));
        return Vector3.Clamp(result, Vector3.Zero, Vector3.One);
    }

    private static float WatercolorPaperHeight(
        float x,
        float y)
    {
        float fine = WatercolorValueNoise(
            x / 2.5f,
            y / 2.5f,
            0x51ed270bu);
        float coarse = WatercolorValueNoise(
            x / 13f,
            y / 13f,
            0x8321ca5du);
        float fiberX = 0.5f +
            (0.5f * MathF.Cos((x + (coarse * 2)) * 2.1f));
        float fiberY = 0.5f +
            (0.5f * MathF.Cos((y - (fine * 2)) * 2.35f));
        return Math.Clamp(
            (0.46f * fine) +
            (0.28f * coarse) +
            (0.13f * fiberX) +
            (0.13f * fiberY),
            0,
            1);
    }

    private static float WatercolorValueNoise(
        float x,
        float y,
        uint seed)
    {
        int cellX = (int)MathF.Floor(x);
        int cellY = (int)MathF.Floor(y);
        float horizontal = x - cellX;
        float vertical = y - cellY;
        horizontal = horizontal * horizontal *
            (3 - (2 * horizontal));
        vertical = vertical * vertical *
            (3 - (2 * vertical));
        float top = float.Lerp(
            Hash(cellX, cellY, seed),
            Hash(cellX + 1, cellY, seed),
            horizontal);
        float bottom = float.Lerp(
            Hash(cellX, cellY + 1, seed),
            Hash(cellX + 1, cellY + 1, seed),
            horizontal);
        return float.Lerp(top, bottom, vertical);
    }

    internal static Vector4[] ChalkCharcoal(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        const float minimumWeight = 0.000001f;
        float charcoalArea = Math.Clamp(
            Option(plan, "CharcoalArea", 2),
            0,
            20);
        float chalkArea = Math.Clamp(
            Option(plan, "ChalkArea", 1),
            0,
            20);
        float pressure = Math.Clamp(
            Option(plan, "StrokePressure", 4) / 10,
            0,
            1);
        Vector4 foregroundOption = plan.GetOption("Foreground");
        Vector4 backgroundOption = plan.GetOption("Background");
        Vector3 foreground = Vector3.Clamp(
            new Vector3(foregroundOption.X, foregroundOption.Y, foregroundOption.Z),
            Vector3.Zero,
            Vector3.One);
        Vector3 background = Vector3.Clamp(
            new Vector3(backgroundOption.X, backgroundOption.Y, backgroundOption.Z),
            Vector3.Zero,
            Vector3.One);
        float sigma = Math.Clamp(plan.Options5.X, 0.5f, 4);
        float extendedSigma = Math.Clamp(plan.Options5.Y, sigma, 6.4f);
        int radius = Math.Clamp(
            (int)MathF.Round(plan.Options5.Z),
            1,
            8);
        int extendedRadius = Math.Clamp(
            (int)MathF.Round(plan.Options5.W),
            radius,
            8);
        (Vector2[] narrow, Vector2[] extended) = XDogLuminance(
            source,
            width,
            height,
            sigma,
            radius,
            extendedSigma,
            extendedRadius);
        float sharpen = float.Lerp(4, 16, pressure);
        float epsilon = float.Lerp(0.035f, -0.015f, pressure);
        float phi = float.Lerp(16, 42, pressure);
        float darkThreshold = float.Lerp(0.24f, 0.72f, charcoalArea / 20);
        float lightThreshold = float.Lerp(0.84f, 0.42f, chalkArea / 20);
        float grainStrength = 0.12f + (0.2f * pressure);
        Vector4[] result = new Vector4[source.Length];
        for (int index = 0; index < source.Length; index++)
        {
            float alpha = Math.Clamp(source[index].W, 0, 1);
            if (alpha <= minimumWeight)
            {
                result[index] = Vector4.Zero;
                continue;
            }

            float narrowLuminance = narrow[index].Y <= minimumWeight
                ? 0
                : narrow[index].X / narrow[index].Y;
            float extendedLuminance = extended[index].Y <= minimumWeight
                ? 0
                : extended[index].X / extended[index].Y;
            float response =
                ((sharpen + 1) * narrowLuminance) -
                (sharpen * extendedLuminance);
            float thresholded = response >= epsilon
                ? 1
                : Math.Clamp(
                    1 + MathF.Tanh(phi * (response - epsilon)),
                    0,
                    1);
            float edgeMask = 1 - thresholded;
            Vector3 straight = Vector3.Clamp(
                Unpremultiply(source[index]),
                Vector3.Zero,
                Vector3.One);
            float luminance = StraightLuminance(straight);
            float darkTone = 1 - SmoothStep(
                darkThreshold - 0.18f,
                darkThreshold + 0.12f,
                luminance);
            float lightTone = SmoothStep(
                lightThreshold - 0.12f,
                lightThreshold + 0.18f,
                luminance);
            int x = index % width;
            int y = index / width;
            float grain = ChalkCharcoalValueNoise(x, y);
            float fiber = 0.5f +
                (0.5f * MathF.Sin(
                    (x * 1.73f) +
                    (y * 0.19f) +
                    (grain * 3.1f)));
            float centeredGrain = (grain - 0.5f) * 2;
            float charcoalGrain = Math.Clamp(
                0.78f +
                    (centeredGrain * grainStrength) -
                    ((fiber - 0.5f) * 0.18f),
                0,
                1);
            float chalkGrain = Math.Clamp(
                0.82f -
                    (centeredGrain * grainStrength * 0.8f) +
                    ((fiber - 0.5f) * 0.14f),
                0,
                1);
            float darkMask = Math.Clamp(
                MathF.Max(edgeMask, darkTone * 0.82f) *
                    charcoalGrain *
                    (0.72f + (0.28f * pressure)),
                0,
                1);
            float lightMask = Math.Clamp(
                lightTone * chalkGrain *
                    (0.68f + (0.32f * pressure)) *
                    (1 - (darkMask * 0.8f)),
                0,
                1);
            Vector3 toned = Vector3.Lerp(
                foreground,
                background,
                SmoothStep(0.2f, 0.8f, luminance));
            toned = Vector3.Lerp(toned, foreground, darkMask);
            toned = Vector3.Lerp(toned, background, lightMask);
            result[index] = Associated(
                Vector3.Clamp(toned, Vector3.Zero, Vector3.One),
                alpha);
        }

        return result;
    }

    private static float ChalkCharcoalValueNoise(int x, int y)
    {
        float fine = ChalkCharcoalHash(x + 17, y + 43);
        float coarse = ChalkCharcoalHash(
            MathF.Floor(x * 0.25f) + 71,
            MathF.Floor(y * 0.25f) + 29);
        return Math.Clamp((0.72f * fine) + (0.28f * coarse), 0, 1);
    }

    private static float ChalkCharcoalHash(float x, float y)
    {
        float valueX = Fraction(x * 0.1031f);
        float valueY = Fraction(y * 0.1031f);
        float valueZ = valueX;
        float shift =
            (valueX * (valueY + 33.33f)) +
            (valueY * (valueZ + 33.33f)) +
            (valueZ * (valueX + 33.33f));
        valueX += shift;
        valueY += shift;
        valueZ += shift;
        return Fraction((valueX + valueY) * valueZ);
    }

    internal static Vector4[] AccentedEdges(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        const float epsilon = 0.02f;
        const float sharpen = 10;
        const float minimumWeight = 0.000001f;
        float sigma = Math.Clamp(plan.Options3.X, 0.5f, 4);
        float extendedSigma = Math.Clamp(plan.Options3.Y, sigma, 6.4f);
        int radius = Math.Clamp(
            (int)MathF.Round(plan.Options3.Z),
            1,
            8);
        int extendedRadius = Math.Clamp(
            (int)MathF.Round(plan.Options3.W),
            radius,
            8);
        float edgeTone = Math.Clamp(
            Option(plan, "EdgeBrightness", 1) / 50,
            0,
            1);
        float smoothness = Math.Clamp(
            Option(plan, "Smoothness", 2) / 15,
            0,
            1);
        float phi = float.Lerp(48, 6, smoothness);
        (Vector2[] narrow, Vector2[] extended) = XDogLuminance(
            source,
            width,
            height,
            sigma,
            radius,
            extendedSigma,
            extendedRadius);
        Vector4[] result = new Vector4[source.Length];
        for (int index = 0; index < source.Length; index++)
        {
            float alpha = Math.Clamp(source[index].W, 0, 1);
            if (alpha <= minimumWeight)
            {
                result[index] = Vector4.Zero;
                continue;
            }

            float narrowLuminance = narrow[index].Y <= minimumWeight
                ? 0
                : narrow[index].X / narrow[index].Y;
            float extendedLuminance = extended[index].Y <= minimumWeight
                ? 0
                : extended[index].X / extended[index].Y;
            float response =
                ((sharpen + 1) * narrowLuminance) -
                (sharpen * extendedLuminance);
            float thresholded = response >= epsilon
                ? 1
                : Math.Clamp(
                    1 + MathF.Tanh(phi * (response - epsilon)),
                    0,
                    1);
            float accent = 1 - thresholded;
            Vector3 straight = Vector3.Clamp(
                Unpremultiply(source[index]),
                Vector3.Zero,
                Vector3.One);
            result[index] = Associated(
                Vector3.Lerp(straight, new Vector3(edgeTone), accent),
                alpha);
        }

        return result;
    }

    internal static Vector4[] DarkStrokes(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        const float sharpen = 10;
        const float minimumWeight = 0.000001f;
        float balance = Math.Clamp(Option(plan, "Balance", 0) / 10, 0, 1);
        float blackIntensity = Math.Clamp(
            Option(plan, "BlackIntensity", 1) / 10,
            0,
            1);
        float whiteIntensity = Math.Clamp(
            Option(plan, "WhiteIntensity", 2) / 10,
            0,
            1);
        if (blackIntensity <= 0 && whiteIntensity <= 0)
        {
            return (Vector4[])source.Clone();
        }

        float epsilon = float.Lerp(-0.04f, 0.06f, balance);
        float sigma = Math.Clamp(plan.Options3.X, 0.5f, 4);
        float extendedSigma = Math.Clamp(plan.Options3.Y, sigma, 6.4f);
        int radius = Math.Clamp(
            (int)MathF.Round(plan.Options3.Z),
            1,
            8);
        int extendedRadius = Math.Clamp(
            (int)MathF.Round(plan.Options3.W),
            radius,
            8);
        (Vector2[] narrow, Vector2[] extended) = XDogLuminance(
            source,
            width,
            height,
            sigma,
            radius,
            extendedSigma,
            extendedRadius);
        Vector4[] result = new Vector4[source.Length];
        for (int index = 0; index < source.Length; index++)
        {
            float alpha = Math.Clamp(source[index].W, 0, 1);
            if (alpha <= minimumWeight)
            {
                result[index] = Vector4.Zero;
                continue;
            }

            float narrowLuminance = narrow[index].Y <= minimumWeight
                ? 0
                : narrow[index].X / narrow[index].Y;
            float extendedLuminance = extended[index].Y <= minimumWeight
                ? 0
                : extended[index].X / extended[index].Y;
            float response =
                ((sharpen + 1) * narrowLuminance) -
                (sharpen * extendedLuminance);
            float thresholded = response >= epsilon
                ? 1
                : Math.Clamp(
                    1 + MathF.Tanh(24 * (response - epsilon)),
                    0,
                    1);
            Vector3 straight = Vector3.Clamp(
                Unpremultiply(source[index]),
                Vector3.Zero,
                Vector3.One);
            float luminance = StraightLuminance(straight);
            float darkMask = Math.Clamp(
                (1 - thresholded) + ((1 - luminance) * 0.35f),
                0,
                1);
            float lightMask = thresholded * luminance;
            Vector3 toned = Vector3.Lerp(
                straight,
                Vector3.Zero,
                blackIntensity * darkMask);
            toned = Vector3.Lerp(
                toned,
                Vector3.One,
                whiteIntensity * lightMask);
            result[index] = Associated(toned, alpha);
        }

        return result;
    }

    internal static Vector4[] InkOutlines(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        const float epsilon = 0.02f;
        const float minimumWeight = 0.000001f;
        float darkIntensity = Math.Clamp(
            Option(plan, "DarkIntensity", 20),
            0,
            50);
        float lightIntensity = Math.Clamp(
            Option(plan, "LightIntensity", 10),
            0,
            50);
        if (darkIntensity <= 0 && lightIntensity <= 0)
        {
            return (Vector4[])source.Clone();
        }

        float sharpen = Math.Clamp(darkIntensity, 0, 64);
        float darkStrength = darkIntensity / 50;
        float lightStrength = lightIntensity / 50;
        float phi = Math.Clamp(8 + (80 * lightStrength), 8, 48);
        float sigma = Math.Clamp(plan.Options3.X, 0.5f, 4);
        float extendedSigma = Math.Clamp(plan.Options3.Y, sigma, 6.4f);
        int radius = Math.Clamp(
            (int)MathF.Round(plan.Options3.Z),
            1,
            8);
        int extendedRadius = Math.Clamp(
            (int)MathF.Round(plan.Options3.W),
            radius,
            8);
        (Vector2[] narrow, Vector2[] extended) = XDogLuminance(
            source,
            width,
            height,
            sigma,
            radius,
            extendedSigma,
            extendedRadius);
        Vector4[] result = new Vector4[source.Length];
        for (int index = 0; index < source.Length; index++)
        {
            float alpha = Math.Clamp(source[index].W, 0, 1);
            if (alpha <= minimumWeight)
            {
                result[index] = Vector4.Zero;
                continue;
            }

            float narrowLuminance = narrow[index].Y <= minimumWeight
                ? 0
                : narrow[index].X / narrow[index].Y;
            float extendedLuminance = extended[index].Y <= minimumWeight
                ? 0
                : extended[index].X / extended[index].Y;
            float response =
                ((sharpen + 1) * narrowLuminance) -
                (sharpen * extendedLuminance);
            float thresholded = response >= epsilon
                ? 1
                : Math.Clamp(
                    1 + MathF.Tanh(phi * (response - epsilon)),
                    0,
                    1);
            Vector3 straight = Vector3.Clamp(
                Unpremultiply(source[index]),
                Vector3.Zero,
                Vector3.One);
            float inkMask = 1 - thresholded;
            float paperMask = thresholded * StraightLuminance(straight);
            Vector3 outlined = Vector3.Lerp(
                straight,
                Vector3.Zero,
                darkStrength * inkMask);
            outlined = Vector3.Lerp(
                outlined,
                Vector3.One,
                lightStrength * paperMask);
            result[index] = Associated(outlined, alpha);
        }

        return result;
    }

    internal static Vector4[] SumiE(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        int washRadius = Math.Clamp(
            (int)MathF.Ceiling(plan.Passes[0].RadiusX),
            1,
            6);
        Vector4[] wash = new Vector4[source.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                wash[(y * width) + x] = SumiEDirectionalWash(
                    plan,
                    source,
                    width,
                    height,
                    x,
                    y,
                    washRadius);
            }
        }

        float sigma = Math.Clamp(plan.Options3.X, 0.5f, 4);
        float extendedSigma = Math.Clamp(
            plan.Options3.Y,
            sigma,
            6.4f);
        int radius = Math.Clamp(
            (int)MathF.Round(plan.Options3.Z),
            1,
            8);
        int extendedRadius = Math.Clamp(
            (int)MathF.Round(plan.Options3.W),
            radius,
            8);
        (Vector2[] narrow, Vector2[] extended) = XDogLuminance(
            wash,
            width,
            height,
            sigma,
            radius,
            extendedSigma,
            extendedRadius);

        float pressure = Math.Clamp(
            Option(plan, "StrokePressure", 2) / 8,
            0,
            1);
        float contrast = Math.Clamp(
            Option(plan, "Contrast", 2),
            -3,
            10);
        float sharpen = 4 + (24 * pressure);
        float epsilon = 0.015f + (0.004f * contrast);
        float phi = Math.Clamp(12 + (8 * contrast), 6, 64);
        float tonalContrast = Math.Clamp(
            1 + (0.22f * contrast),
            0.25f,
            3.2f);
        Vector3 inkColor = new(0.012f, 0.018f, 0.017f);
        Vector3 paperColor = new(0.985f, 0.978f, 0.948f);
        Vector4[] result = new Vector4[source.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                float alpha = Math.Clamp(source[index].W, 0, 1);
                if (alpha <= 0.000001f)
                {
                    result[index] = Vector4.Zero;
                    continue;
                }

                float narrowLuminance = narrow[index].Y <= 0.000001f
                    ? 0
                    : narrow[index].X / narrow[index].Y;
                float extendedLuminance =
                    extended[index].Y <= 0.000001f
                        ? 0
                        : extended[index].X / extended[index].Y;
                float response =
                    ((sharpen + 1) * narrowLuminance) -
                    (sharpen * extendedLuminance);
                float thresholded = response >= epsilon
                    ? 1
                    : Math.Clamp(
                        1 + MathF.Tanh(phi * (response - epsilon)),
                        0,
                        1);
                float inkEdge = 1 - thresholded;
                float tone = Math.Clamp(
                    ((narrowLuminance - 0.5f) * tonalContrast) + 0.5f,
                    0,
                    1);
                float quantizedTone = MathF.Floor((tone * 4) + 0.5f) / 4;
                tone = float.Lerp(tone, quantizedTone, 0.68f);
                tone = Math.Clamp(
                    tone -
                    (inkEdge * float.Lerp(0.42f, 0.9f, pressure)) -
                    ((1 - narrowLuminance) * pressure * 0.08f),
                    0,
                    1);

                float fineGrain = Hash(x, y, 0x51ed270bu);
                float coarseGrain = Hash(x / 9, y / 9, 0x8321ca5du);
                float fiber = 0.5f +
                    (0.25f * MathF.Cos(
                        (x + (coarseGrain * 2)) * 1.7f)) +
                    (0.25f * MathF.Cos(
                        (y - (fineGrain * 2)) * 2.15f));
                float paper = Math.Clamp(
                    (0.45f * fineGrain) +
                    (0.25f * coarseGrain) +
                    (0.30f * fiber),
                    0,
                    1);
                float dryGap = SmoothStep(0.70f, 0.94f, paper) *
                    (1 - (pressure * 0.72f)) *
                    (1 - tone) *
                    0.42f;
                tone = Math.Clamp(
                    float.Lerp(tone, 1, dryGap) +
                    ((paper - 0.5f) * 0.055f),
                    0,
                    1);
                result[index] = Associated(
                    Vector3.Lerp(inkColor, paperColor, tone),
                    alpha);
            }
        }

        return result;
    }

    private static Vector4 SumiEDirectionalWash(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        int radius)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        if (center.W <= 0.000001f)
        {
            return Vector4.Zero;
        }

        float horizontal =
            Luminance(SamplePixel(source, width, height, x + 1, y)) -
            Luminance(SamplePixel(source, width, height, x - 1, y));
        float vertical =
            Luminance(SamplePixel(source, width, height, x, y + 1)) -
            Luminance(SamplePixel(source, width, height, x, y - 1));
        Vector2 gradient = new(horizontal, vertical);
        Vector2 tangent = gradient.LengthSquared() <= 0.000001f
            ? Vector2.UnitX
            : Vector2.Normalize(new Vector2(-gradient.Y, gradient.X));
        Vector2 normal = new(-tangent.Y, tangent.X);
        Span<float> sums = stackalloc float[4];
        Span<float> squaredSums = stackalloc float[4];
        Span<float> weights = stackalloc float[4];
        float radiusSquared = radius * radius;
        for (int offsetY = -radius; offsetY <= radius; offsetY++)
        {
            for (int offsetX = -radius; offsetX <= radius; offsetX++)
            {
                float distanceSquared =
                    (offsetX * offsetX) + (offsetY * offsetY);
                if (distanceSquared > radiusSquared)
                {
                    continue;
                }

                Vector4 sample = SamplePixel(
                    source,
                    width,
                    height,
                    x + offsetX,
                    y + offsetY);
                float alphaConfidence = Math.Clamp(
                    1 - (MathF.Abs(sample.W - center.W) * 4),
                    0,
                    1);
                float weight = sample.W * alphaConfidence * MathF.Exp(
                    -2 * distanceSquared / MathF.Max(radiusSquared, 1));
                if (weight <= 0)
                {
                    continue;
                }

                float luminance = Luminance(sample);
                Vector2 offset = new(offsetX, offsetY);
                float tangentPosition = Vector2.Dot(offset, tangent);
                float normalPosition = Vector2.Dot(offset, normal);
                if (tangentPosition <= 0 && normalPosition <= 0)
                {
                    AccumulateSumiESector(
                        sums,
                        squaredSums,
                        weights,
                        0,
                        luminance,
                        weight);
                }
                if (tangentPosition >= 0 && normalPosition <= 0)
                {
                    AccumulateSumiESector(
                        sums,
                        squaredSums,
                        weights,
                        1,
                        luminance,
                        weight);
                }
                if (tangentPosition >= 0 && normalPosition >= 0)
                {
                    AccumulateSumiESector(
                        sums,
                        squaredSums,
                        weights,
                        2,
                        luminance,
                        weight);
                }
                if (tangentPosition <= 0 && normalPosition >= 0)
                {
                    AccumulateSumiESector(
                        sums,
                        squaredSums,
                        weights,
                        3,
                        luminance,
                        weight);
                }
            }
        }

        float pressure = Math.Clamp(
            Option(plan, "StrokePressure", 2) / 8,
            0,
            1);
        float sharpness = float.Lerp(2, 7, pressure);
        float weightedMean = 0;
        float confidenceTotal = 0;
        for (int sector = 0; sector < 4; sector++)
        {
            float sectorWeight = MathF.Max(weights[sector], 0.000001f);
            float mean = sums[sector] / sectorWeight;
            float variance = MathF.Max(
                MathF.Abs(
                    (squaredSums[sector] / sectorWeight) -
                    (mean * mean)),
                0.000001f);
            float confidence = 1 /
                (1 + MathF.Pow(400 * variance, sharpness));
            weightedMean += mean * confidence;
            confidenceTotal += confidence;
        }

        float centerLuminance = Luminance(center);
        float wash = confidenceTotal <= 0.000001f
            ? centerLuminance
            : weightedMean / confidenceTotal;
        wash = float.Lerp(centerLuminance, wash, 0.88f);
        return Associated(new Vector3(Math.Clamp(wash, 0, 1)), center.W);
    }

    private static void AccumulateSumiESector(
        Span<float> sums,
        Span<float> squaredSums,
        Span<float> weights,
        int sector,
        float luminance,
        float weight)
    {
        sums[sector] += luminance * weight;
        squaredSums[sector] += luminance * luminance * weight;
        weights[sector] += weight;
    }

    private static (Vector2[] Narrow, Vector2[] Extended) XDogLuminance(
        Vector4[] source,
        int width,
        int height,
        float sigma,
        int radius,
        float extendedSigma,
        int extendedRadius)
    {
        const float minimumWeight = 0.000001f;
        Vector2[] weightedLuminance = new Vector2[source.Length];
        for (int index = 0; index < source.Length; index++)
        {
            float alpha = Math.Clamp(source[index].W, 0, 1);
            float luminance = alpha <= minimumWeight
                ? 0
                : StraightLuminance(Unpremultiply(source[index]));
            weightedLuminance[index] = new Vector2(
                luminance * alpha,
                alpha);
        }

        return (
            XDogGaussianBlur(
                weightedLuminance,
                width,
                height,
                sigma,
                radius),
            XDogGaussianBlur(
                weightedLuminance,
                width,
                height,
                extendedSigma,
                extendedRadius));
    }

    private static Vector2[] XDogGaussianBlur(
        Vector2[] source,
        int width,
        int height,
        float sigma,
        int radius)
    {
        Vector2[] horizontal = new Vector2[source.Length];
        Vector2[] output = new Vector2[source.Length];
        float inverseSigma = 1 / (2 * sigma * sigma);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 total = Vector2.Zero;
                float totalWeight = 0;
                for (int offset = -radius; offset <= radius; offset++)
                {
                    float weight = MathF.Exp(
                        -(offset * offset) * inverseSigma);
                    int sampleX = Math.Clamp(x + offset, 0, width - 1);
                    total += source[(y * width) + sampleX] * weight;
                    totalWeight += weight;
                }

                horizontal[(y * width) + x] = total / totalWeight;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 total = Vector2.Zero;
                float totalWeight = 0;
                for (int offset = -radius; offset <= radius; offset++)
                {
                    float weight = MathF.Exp(
                        -(offset * offset) * inverseSigma);
                    int sampleY = Math.Clamp(y + offset, 0, height - 1);
                    total += horizontal[(sampleY * width) + x] * weight;
                    totalWeight += weight;
                }

                output[(y * width) + x] = total / totalWeight;
            }
        }

        return output;
    }

    internal static Vector4[] PosterEdges(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        const float minimumWeight = 0.000001f;
        int radius = Math.Clamp(
            (int)MathF.Round(plan.Passes[0].RadiusX),
            1,
            8);
        float edgeIntensity = Math.Clamp(
            Option(plan, "EdgeIntensity", 1),
            0,
            4);
        int levels = Math.Clamp(
            (int)MathF.Round(Option(plan, "Posterization", 2)),
            2,
            32);
        (float[] alpha, float[] guidedLuminance,
            Vector3[] guidedColor) = GuidedFilter(
                source,
                width,
                height,
                radius,
                epsilon: 0.01f);
        int pixelCount = checked(width * height);

        Vector4[] result = new Vector4[pixelCount];
        float levelScale = levels - 1;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                float pixelAlpha = alpha[index];
                if (pixelAlpha <= minimumWeight)
                {
                    result[index] = Vector4.Zero;
                    continue;
                }

                float edge = Math.Clamp(
                    GuidedScharrGradient(
                        guidedLuminance,
                        width,
                        height,
                        x,
                        y,
                        radius).Length(),
                    0,
                    1);
                float ink = Math.Clamp(
                    edge * edgeIntensity * 2,
                    0,
                    1);
                Vector3 quantized = new(
                    MathF.Round(guidedColor[index].X * levelScale) /
                        levelScale,
                    MathF.Round(guidedColor[index].Y * levelScale) /
                        levelScale,
                    MathF.Round(guidedColor[index].Z * levelScale) /
                        levelScale);
                result[index] = Associated(
                    Vector3.Clamp(
                        quantized * (1 - ink),
                        Vector3.Zero,
                        Vector3.One),
                    pixelAlpha);
            }
        }

        return result;
    }

    internal static Vector4[] BasRelief(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        const float minimumWeight = 0.000001f;
        float smoothness = Math.Clamp(
            Option(plan, "Smoothness", 3),
            0,
            15);
        int radius = Math.Clamp(
            (int)MathF.Round(plan.Passes[0].RadiusX),
            1,
            8);
        float epsilon = 0.0025f * (1 + smoothness);
        float detail = Math.Clamp(
            Option(plan, "Detail", 13),
            0,
            64) * 0.25f;
        Vector2 lightDirection = BasReliefLightDirection(
            (int)MathF.Round(Option(plan, "LightDirection", 5)));
        Vector4 foreground = OptionVector(
            plan,
            "Foreground",
            new Vector4(0, 0, 0, 1));
        Vector4 background = OptionVector(
            plan,
            "Background",
            new Vector4(1, 1, 1, 1));
        (float[] alpha, float[] guidedLuminance, _) = GuidedFilter(
            source,
            width,
            height,
            radius,
            epsilon);
        Vector4[] result = new Vector4[source.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                float pixelAlpha = alpha[index];
                if (pixelAlpha <= minimumWeight)
                {
                    continue;
                }

                Vector2 gradient = GuidedScharrGradient(
                    guidedLuminance,
                    width,
                    height,
                    x,
                    y,
                    radius: 1);
                Vector3 normal = Vector3.Normalize(
                    new Vector3(
                        -gradient.X * detail,
                        -gradient.Y * detail,
                        1));
                float shade = Math.Clamp(
                    0.5f +
                        (0.5f * Vector2.Dot(
                            new Vector2(normal.X, normal.Y),
                            lightDirection)),
                    0,
                    1);
                result[index] = Associated(
                    Vector3.Lerp(
                        new Vector3(
                            foreground.X,
                            foreground.Y,
                            foreground.Z),
                        new Vector3(
                            background.X,
                            background.Y,
                            background.Z),
                        shade),
                    pixelAlpha);
            }
        }

        return result;
    }

    private static (
        float[] Alpha,
        float[] GuidedLuminance,
        Vector3[] GuidedColor) GuidedFilter(
        Vector4[] source,
        int width,
        int height,
        int radius,
        float epsilon)
    {
        const float minimumWeight = 0.000001f;
        int pixelCount = checked(width * height);
        float[] alpha = new float[pixelCount];
        float[] luminance = new float[pixelCount];
        float[] weightedLuminance = new float[pixelCount];
        float[] weightedSquare = new float[pixelCount];
        for (int index = 0; index < pixelCount; index++)
        {
            Vector4 pixel = source[index];
            float pixelAlpha = Math.Clamp(pixel.W, 0, 1);
            float value = pixelAlpha <= minimumWeight
                ? 0
                : StraightLuminance(Unpremultiply(pixel));
            alpha[index] = pixelAlpha;
            luminance[index] = value;
            weightedLuminance[index] = value * pixelAlpha;
            weightedSquare[index] = value * value * pixelAlpha;
        }

        float[] meanAlpha = GuidedBoxBlur(
            alpha,
            width,
            height,
            radius);
        float[] meanWeightedLuminance = GuidedBoxBlur(
            weightedLuminance,
            width,
            height,
            radius);
        float[] meanWeightedSquare = GuidedBoxBlur(
            weightedSquare,
            width,
            height,
            radius);
        float[] weightedA = new float[pixelCount];
        float[] weightedB = new float[pixelCount];
        for (int index = 0; index < pixelCount; index++)
        {
            float weight = meanAlpha[index];
            if (weight <= minimumWeight || alpha[index] <= minimumWeight)
            {
                continue;
            }

            float mean = meanWeightedLuminance[index] / weight;
            float variance = MathF.Max(
                0,
                (meanWeightedSquare[index] / weight) -
                    (mean * mean));
            float a = variance / (variance + epsilon);
            float b = mean - (a * mean);
            weightedA[index] = a * alpha[index];
            weightedB[index] = b * alpha[index];
        }

        float[] meanWeightedA = GuidedBoxBlur(
            weightedA,
            width,
            height,
            radius);
        float[] meanWeightedB = GuidedBoxBlur(
            weightedB,
            width,
            height,
            radius);
        float[] guidedLuminance = new float[pixelCount];
        Vector3[] guidedColor = new Vector3[pixelCount];
        for (int index = 0; index < pixelCount; index++)
        {
            float pixelAlpha = alpha[index];
            if (pixelAlpha <= minimumWeight)
            {
                continue;
            }

            float weight = meanAlpha[index];
            float meanA = weight <= minimumWeight
                ? 0
                : meanWeightedA[index] / weight;
            float meanB = weight <= minimumWeight
                ? luminance[index]
                : meanWeightedB[index] / weight;
            float guided = Math.Clamp(
                (meanA * luminance[index]) + meanB,
                0,
                1);
            Vector3 straight = Vector3.Clamp(
                Unpremultiply(source[index]),
                Vector3.Zero,
                Vector3.One);
            guidedLuminance[index] = guided;
            guidedColor[index] = luminance[index] <= minimumWeight
                ? new Vector3(guided)
                : Vector3.Clamp(
                    straight * (guided / luminance[index]),
                    Vector3.Zero,
                    Vector3.One);
        }

        return (alpha, guidedLuminance, guidedColor);
    }

    private static float[] GuidedBoxBlur(
        float[] source,
        int width,
        int height,
        int radius)
    {
        float[] horizontal = new float[source.Length];
        float[] output = new float[source.Length];
        float inverseDiameter = 1f / ((2 * radius) + 1);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float sum = 0;
                for (int offset = -radius; offset <= radius; offset++)
                {
                    int sampleX = Math.Clamp(x + offset, 0, width - 1);
                    sum += source[(y * width) + sampleX];
                }
                horizontal[(y * width) + x] =
                    sum * inverseDiameter;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float sum = 0;
                for (int offset = -radius; offset <= radius; offset++)
                {
                    int sampleY = Math.Clamp(y + offset, 0, height - 1);
                    sum += horizontal[(sampleY * width) + x];
                }
                output[(y * width) + x] =
                    sum * inverseDiameter;
            }
        }

        return output;
    }

    private static Vector2 GuidedScharrGradient(
        float[] luminance,
        int width,
        int height,
        int x,
        int y,
        int radius)
    {
        float topLeft = PosterEdgesSample(
            luminance, width, height, x - radius, y - radius);
        float top = PosterEdgesSample(
            luminance, width, height, x, y - radius);
        float topRight = PosterEdgesSample(
            luminance, width, height, x + radius, y - radius);
        float left = PosterEdgesSample(
            luminance, width, height, x - radius, y);
        float right = PosterEdgesSample(
            luminance, width, height, x + radius, y);
        float bottomLeft = PosterEdgesSample(
            luminance, width, height, x - radius, y + radius);
        float bottom = PosterEdgesSample(
            luminance, width, height, x, y + radius);
        float bottomRight = PosterEdgesSample(
            luminance, width, height, x + radius, y + radius);
        float horizontal =
            (3 * (topRight - topLeft)) +
            (10 * (right - left)) +
            (3 * (bottomRight - bottomLeft));
        float vertical =
            (3 * (bottomLeft - topLeft)) +
            (10 * (bottom - top)) +
            (3 * (bottomRight - topRight));
        return new Vector2(horizontal, vertical) / 16;
    }

    private static Vector2 BasReliefLightDirection(int code) =>
        code switch
        {
            0 => new Vector2(0, -1),
            1 => Vector2.Normalize(new Vector2(1, -1)),
            2 => new Vector2(1, 0),
            3 => Vector2.Normalize(new Vector2(1, 1)),
            4 => new Vector2(0, 1),
            5 => Vector2.Normalize(new Vector2(-1, 1)),
            6 => new Vector2(-1, 0),
            7 => Vector2.Normalize(new Vector2(-1, -1)),
            _ => Vector2.Normalize(new Vector2(-1, 1))
        };

    private static float PosterEdgesSample(
        float[] source,
        int width,
        int height,
        int x,
        int y) =>
        source[
            (Math.Clamp(y, 0, height - 1) * width) +
            Math.Clamp(x, 0, width - 1)];

    internal static Vector4 PlasticWrap(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        float highlightStrength = MathF.Max(
            0,
            Option(plan, "HighlightStrength", 15));
        if (center.W <= 0 || highlightStrength <= 0)
        {
            return center;
        }

        float detail = Math.Clamp(
            Option(plan, "Detail", 9) / 15,
            0,
            1);
        float smoothness = Math.Clamp(
            Option(plan, "Smoothness", 7) / 15,
            0,
            1);
        float radius = MathF.Max(
            1,
            MathF.Max(pass.RadiusX, pass.RadiusY));
        float left = Luminance(SamplePixelBilinear(
            source,
            width,
            height,
            x - radius,
            y));
        float right = Luminance(SamplePixelBilinear(
            source,
            width,
            height,
            x + radius,
            y));
        float top = Luminance(SamplePixelBilinear(
            source,
            width,
            height,
            x,
            y - radius));
        float bottom = Luminance(SamplePixelBilinear(
            source,
            width,
            height,
            x,
            y + radius));
        float heightScale = 6 * detail;
        Vector3 normal = Vector3.Normalize(
            new Vector3(
                -(right - left) * heightScale,
                -(bottom - top) * heightScale,
                1));
        Vector3 view = Vector3.UnitZ;
        Vector3 surfaceToLight = Vector3.Normalize(
            new Vector3(-0.45f, -0.55f, 1));
        float roughness = MathF.Max(
            0.045f,
            0.4f - (0.3f * smoothness));
        float normalDotLight = MathF.Max(
            Vector3.Dot(normal, surfaceToLight),
            0);
        Vector3 specular = CookTorranceGgxSpecular(
            new Vector3(0.04f),
            normal,
            view,
            surfaceToLight,
            roughness);
        float effectAmount = Math.Clamp(
            highlightStrength / 20,
            0,
            1);
        float diffuseShade = 1 +
            (((0.55f + (0.45f * normalDotLight)) - 1) *
                effectAmount);
        float specularGain =
            highlightStrength *
            (0.65f + (0.35f * smoothness));
        Vector3 result =
            (Unpremultiply(center) * diffuseShade) +
            (specular * normalDotLight * specularGain);
        return Associated(
            Vector3.Clamp(result, Vector3.Zero, Vector3.One),
            center.W);
    }

    internal static Vector4 AngledStrokes(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        float radius = MathF.Max(
            MathF.Max(pass.RadiusX, pass.RadiusY),
            1);
        return PolynomialAnisotropicKuwahara(
            source,
            width,
            height,
            x,
            y,
            radius,
            sharpness: Math.Clamp(
                Option(plan, "Sharpness", 3),
                0.5f,
                12),
            widthScale: 1.65f,
            minorScale: 0.42f,
            roughness: 0,
            luminancePreference: 0,
            diagonalBias: 1,
            diagonalBalance: Math.Clamp(
                Option(plan, "DirectionBalance", 0.5f),
                0,
                1),
            balanceDiagonalsByLuminance: true,
            jitterSeed: unchecked(93u * 0x9e3779b9u));
    }

    internal static Vector4 PaintDaubs(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        int brushType = Math.Clamp(
            (int)MathF.Round(plan.Options1.X),
            0,
            5);
        float radius = MathF.Max(
            MathF.Max(pass.RadiusX, pass.RadiusY),
            1);
        float sharpness = Math.Clamp(
            Option(plan, "Sharpness", 5),
            0.5f,
            10);
        float widthScale = 1;
        float minorScale = 1;
        float roughness = 0;
        float luminancePreference = 0;
        switch (brushType)
        {
            case 1:
                roughness = 0.55f;
                luminancePreference = 0.65f;
                break;
            case 2:
                roughness = 0.55f;
                luminancePreference = -0.65f;
                break;
            case 3:
                widthScale = 1.45f;
                minorScale = 0.7f;
                sharpness *= 1.35f;
                break;
            case 4:
                widthScale = 1.55f;
                minorScale = 1.05f;
                sharpness *= 0.55f;
                break;
            case 5:
                widthScale = 1.1f;
                minorScale = 0.75f;
                roughness = 0.85f;
                luminancePreference = 1.1f;
                sharpness *= 1.6f;
                break;
        }
        sharpness = Math.Clamp(sharpness, 0.5f, 12);

        return PolynomialAnisotropicKuwahara(
            source,
            width,
            height,
            x,
            y,
            radius,
            sharpness,
            widthScale,
            minorScale,
            roughness,
            luminancePreference,
            diagonalBias: 0,
            diagonalBalance: 0,
            balanceDiagonalsByLuminance: false,
            jitterSeed: unchecked(83u * 0x9e3779b9u));
    }

    internal static Vector4 PaletteKnife(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        float detail = MathF.Max(
            Option(plan, "StrokeDetail", 1),
            0);
        float softness = MathF.Max(
            Option(plan, "Softness", 0),
            0);
        float sharpness = Math.Clamp(
            (2 + (2 * detail)) /
                (1 + (0.5f * softness)),
            0.5f,
            12);
        float radius = MathF.Max(
            MathF.Max(pass.RadiusX, pass.RadiusY),
            1);
        return PolynomialAnisotropicKuwahara(
            source,
            width,
            height,
            x,
            y,
            radius,
            sharpness,
            widthScale: 1,
            minorScale: 1,
            roughness: 0,
            luminancePreference: 0,
            diagonalBias: 0,
            diagonalBalance: 0,
            balanceDiagonalsByLuminance: false,
            jitterSeed: unchecked(84u * 0x9e3779b9u));
    }

    internal static Vector4 SmudgeStick(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        float amount = Math.Clamp(
            Option(plan, "Intensity", 10) / 10,
            0,
            1);
        if (center.W <= 0 || amount <= 0)
        {
            return center.W <= 0 ? Vector4.Zero : center;
        }

        float radius = MathF.Max(
            MathF.Max(pass.RadiusX, pass.RadiusY),
            1);
        Vector4 filteredSample = PolynomialAnisotropicKuwahara(
            source,
            width,
            height,
            x,
            y,
            radius,
            sharpness: 3 + (4 * amount),
            widthScale: 1.65f,
            minorScale: 0.42f,
            roughness: 0,
            luminancePreference: -0.65f * amount,
            diagonalBias: 0.8f,
            diagonalBalance: 0,
            balanceDiagonalsByLuminance: false,
            jitterSeed: unchecked(88u * 0x9e3779b9u));
        Vector3 straight = Unpremultiply(center);
        Vector3 filtered = Unpremultiply(filteredSample);
        float darkness = 1 - StraightLuminance(straight);
        float smudgeMix = amount * (0.55f + (0.45f * darkness));
        Vector3 result = Vector3.Lerp(
            straight,
            filtered,
            smudgeMix);

        float highlightArea = Math.Clamp(
            Option(plan, "HighlightArea", 0),
            0,
            20) / 20;
        if (highlightArea > 0)
        {
            float threshold = 1 - (0.75f * highlightArea);
            float highlightMask = Math.Clamp(
                (StraightLuminance(result) - threshold) / 0.2f,
                0,
                1);
            highlightMask =
                highlightMask *
                highlightMask *
                (3 - (2 * highlightMask));
            float highlightGain =
                highlightMask *
                amount *
                (0.15f + (0.2f * highlightArea));
            result = Vector3.Lerp(
                result,
                Vector3.One,
                highlightGain);
        }

        return Associated(
            Vector3.Clamp(result, Vector3.Zero, Vector3.One),
            center.W);
    }

    internal static Vector4 Sponge(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        float definition = Math.Clamp(
            Option(plan, "Definition", 12),
            0,
            24);
        float smoothness = Math.Clamp(
            Option(plan, "Smoothness", 5) / 15,
            0,
            1);
        float sharpness = Math.Clamp(
            (1 + (0.45f * definition)) *
                (1.25f - (0.5f * smoothness)),
            0.5f,
            12);
        float minorScale = 0.38f + (0.52f * smoothness);
        return PolynomialAnisotropicKuwahara(
            source,
            width,
            height,
            x,
            y,
            MathF.Max(
                MathF.Max(pass.RadiusX, pass.RadiusY),
                1),
            sharpness,
            widthScale: 1.2f,
            minorScale,
            roughness: 0,
            luminancePreference: 0,
            diagonalBias: 0,
            diagonalBalance: 0,
            balanceDiagonalsByLuminance: false,
            jitterSeed: unchecked(89u * 0x9e3779b9u));
    }

    internal static Vector4 RoughPastels(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        if (center.W <= 0)
        {
            return Vector4.Zero;
        }

        float radius = MathF.Max(
            MathF.Max(pass.RadiusX, pass.RadiusY),
            1);
        float detail = Math.Clamp(
            Option(plan, "StrokeDetail", 4),
            0,
            16);
        bool coarsePass = pass.Iteration == 0;
        Vector4 filteredSample = PolynomialAnisotropicKuwahara(
            source,
            width,
            height,
            x,
            y,
            radius,
            coarsePass
                ? 2 + (detail * 0.45f)
                : 4 + (detail * 0.5f),
            widthScale: coarsePass ? 1.35f : 1.1f,
            minorScale: coarsePass ? 0.55f : 0.72f,
            roughness: coarsePass ? 0.22f : 0.08f,
            luminancePreference: 0,
            diagonalBias: 0,
            diagonalBalance: 0,
            balanceDiagonalsByLuminance: false,
            jitterSeed: coarsePass
                ? unchecked(87u * 0x9e3779b9u)
                : unchecked(87u * 0x85ebca6bu));
        if (coarsePass)
        {
            return filteredSample;
        }
        Vector3 filtered = Unpremultiply(filteredSample);

        float scaling = MathF.Max(
            Option(plan, "Scaling", 1),
            0.125f);
        int texture = Math.Clamp(
            (int)MathF.Round(Option(plan, "Texture", 0)),
            0,
            3);
        bool invert = Option(plan, "Invert", 0) >= 0.5f;
        float paper = ProceduralTextureHeight(
            x,
            y,
            texture,
            scaling,
            unchecked(87u * 0xc2b2ae35u),
            unchecked(87u * 0x27d4eb2du));
        float heightValue = invert ? 1 - paper : paper;

        (float tensorX, float tensorCross, float tensorY) =
            FacetStructureTensor(source, width, height, x, y);
        float angle = 0.5f * MathF.Atan2(
            2 * tensorCross,
            tensorX - tensorY) +
            (MathF.PI * 0.5f);
        float fiber = 0.5f +
            (0.5f * MathF.Cos(
                (((x * MathF.Cos(angle)) +
                    (y * MathF.Sin(angle))) /
                    MathF.Max(scaling * 0.75f, 0.125f)) *
                MathF.PI));

        float relief = Math.Clamp(
            Option(plan, "Relief", 0.2f),
            0,
            2);
        int lightDirection = Math.Clamp(
            (int)MathF.Round(Option(plan, "LightDirection", 0)),
            0,
            7);
        float lightAngle =
            (-MathF.PI * 0.5f) +
            (lightDirection * MathF.PI * 0.25f);
        float lightX = MathF.Cos(lightAngle);
        float lightY = MathF.Sin(lightAngle);
        float ahead = ProceduralTextureHeight(
            x + lightX,
            y + lightY,
            texture,
            scaling,
            unchecked(87u * 0xc2b2ae35u),
            unchecked(87u * 0x27d4eb2du));
        float behind = ProceduralTextureHeight(
            x - lightX,
            y - lightY,
            texture,
            scaling,
            unchecked(87u * 0xc2b2ae35u),
            unchecked(87u * 0x27d4eb2du));
        if (invert)
        {
            ahead = 1 - ahead;
            behind = 1 - behind;
        }

        float coverageGap =
            (0.55f * heightValue) +
            (0.45f * (1 - fiber));
        float coverage = Math.Clamp(
            1 - (coverageGap * (0.12f + (0.18f * relief))),
            0.55f,
            1);
        float shade = Math.Clamp(
            1 + ((ahead - behind) * relief * 1.25f),
            0.55f,
            1.45f);
        Vector3 result =
            ((filtered * coverage) +
                (Vector3.One * (1 - coverage) * 0.65f)) *
            shade;
        return Associated(
            Vector3.Clamp(result, Vector3.Zero, Vector3.One),
            center.W);
    }

    internal static Vector4 Underpainting(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        if (center.W <= 0)
        {
            return Vector4.Zero;
        }

        float radius = MathF.Max(pass.RadiusX, pass.RadiusY);
        Vector4 filteredSample = radius <= 0.000001f
            ? center
            : PolynomialAnisotropicKuwahara(
                source,
                width,
                height,
                x,
                y,
                radius,
                sharpness: 3 + (0.35f * MathF.Min(radius, 12)),
                widthScale: 1.35f,
                minorScale: 0.55f,
                roughness: 0.08f,
                luminancePreference: 0,
                diagonalBias: 0,
                diagonalBalance: 0,
                balanceDiagonalsByLuminance: false,
                jitterSeed: unchecked(90u * 0x9e3779b9u));
        Vector3 filtered = Unpremultiply(filteredSample);
        float scaling = MathF.Max(
            Option(plan, "Scaling", 1),
            0.125f);
        int texture = Math.Clamp(
            (int)MathF.Round(Option(plan, "Texture", 0)),
            0,
            3);
        bool invert = Option(plan, "Invert", 0) >= 0.5f;
        const uint fineSeed = 0x7584a42du;
        const uint coarseSeed = 0x1f123bb5u;
        float heightValue = ProceduralTextureHeight(
            x,
            y,
            texture,
            scaling,
            fineSeed,
            coarseSeed);
        if (invert)
        {
            heightValue = 1 - heightValue;
        }

        float relief = Math.Clamp(
            Option(plan, "Relief", 0.04f),
            0,
            2);
        int lightDirection = Math.Clamp(
            (int)MathF.Round(Option(plan, "LightDirection", 0)),
            0,
            7);
        float lightAngle =
            (-MathF.PI * 0.5f) +
            (lightDirection * MathF.PI * 0.25f);
        float lightX = MathF.Cos(lightAngle);
        float lightY = MathF.Sin(lightAngle);
        float ahead = ProceduralTextureHeight(
            x + lightX,
            y + lightY,
            texture,
            scaling,
            fineSeed,
            coarseSeed);
        float behind = ProceduralTextureHeight(
            x - lightX,
            y - lightY,
            texture,
            scaling,
            fineSeed,
            coarseSeed);
        if (invert)
        {
            ahead = 1 - ahead;
            behind = 1 - behind;
        }

        float coverage = Math.Clamp(
            Option(plan, "TextureCoverage", 0.2f),
            0,
            1);
        float textureTone = 1 +
            (coverage * ((0.82f + (0.3f * heightValue)) - 1));
        float shade = Math.Clamp(
            1 + ((ahead - behind) * relief * 1.5f),
            0.55f,
            1.45f);
        return Associated(
            Vector3.Clamp(
                filtered * textureTone * shade,
                Vector3.Zero,
                Vector3.One),
            center.W);
    }

    private static float ProceduralTextureHeight(
        float x,
        float y,
        int texture,
        float scaling,
        uint fineSeed,
        uint coarseSeed)
    {
        float qx = x / scaling;
        float qy = y / scaling;
        float fineNoise = Hash(
            (int)MathF.Floor(qx),
            (int)MathF.Floor(qy),
            fineSeed);
        if (texture == 1)
        {
            float row = MathF.Floor(qy / 4);
            float localX = Fraction(
                (qx / 8) + ((row % 2 + 2) % 2 * 0.5f));
            float localY = Fraction(qy / 4);
            float edge = MathF.Min(
                MathF.Min(localX, 1 - localX),
                MathF.Min(localY, 1 - localY));
            float mortar = edge < 0.08f ? 1 : 0;
            return Math.Clamp(
                0.25f + (0.5f * fineNoise) + (0.25f * mortar),
                0,
                1);
        }
        if (texture == 2)
        {
            float warp = 0.5f +
                (0.5f * MathF.Cos(qx * MathF.PI * 0.5f));
            float weft = 0.5f +
                (0.5f * MathF.Cos(qy * MathF.PI * 0.5f));
            return Math.Clamp(
                0.25f +
                    (0.3f * warp) +
                    (0.3f * weft) +
                    (0.15f * fineNoise),
                0,
                1);
        }
        if (texture == 3)
        {
            float coarseNoise = Hash(
                (int)MathF.Floor(qx / 4),
                (int)MathF.Floor(qy / 4),
                coarseSeed);
            return (0.6f * coarseNoise) +
                (0.4f * fineNoise);
        }

        float canvasX = 0.5f +
            (0.5f * MathF.Cos(qx * MathF.PI));
        float canvasY = 0.5f +
            (0.5f * MathF.Cos(qy * MathF.PI));
        return Math.Clamp(
            (0.35f * canvasX) +
                (0.35f * canvasY) +
                (0.3f * fineNoise),
            0,
            1);
    }

    private static float Fraction(float value) =>
        value - MathF.Floor(value);

    private static Vector4 PolynomialAnisotropicKuwahara(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        float radius,
        float sharpness,
        float widthScale,
        float minorScale,
        float roughness,
        float luminancePreference,
        float diagonalBias,
        float diagonalBalance,
        bool balanceDiagonalsByLuminance,
        uint jitterSeed)
    {
        const int latticeRadius = 4;
        const float zeta = 2f / latticeRadius;
        const float gamma = 3 * MathF.PI / 16;
        const float diagonal = 0.7071067811865476f;
        Vector4 center = SamplePixel(source, width, height, x, y);
        if (center.W <= 0)
        {
            return Vector4.Zero;
        }

        (float tensorX, float tensorCross, float tensorY) =
            FacetStructureTensor(
                source,
                width,
                height,
                x,
                y);
        float discriminant = MathF.Sqrt(
            MathF.Max(
                0,
                ((tensorX - tensorY) *
                    (tensorX - tensorY)) +
                (4 * tensorCross * tensorCross)));
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
        float angle = tensorEnergy <= 0.000001f
            ? 0
            : (0.5f * MathF.Atan2(
                2 * tensorCross,
                tensorX - tensorY)) +
                (MathF.PI * 0.5f);
        if (roughness > 0)
        {
            float blockSize = MathF.Max(radius * 2, 1);
            float jitter = Hash(
                (int)MathF.Floor(x / blockSize),
                (int)MathF.Floor(y / blockSize),
                jitterSeed) -
                0.5f;
            angle += jitter * roughness * MathF.PI;
        }
        if (diagonalBias > 0)
        {
            float diagonalAngle;
            if (balanceDiagonalsByLuminance)
            {
                float threshold =
                    1 - Math.Clamp(diagonalBalance, 0, 1);
                diagonalAngle = StraightLuminance(
                    Unpremultiply(center)) >= threshold
                    ? MathF.PI * 0.25f
                    : -MathF.PI * 0.25f;
            }
            else
            {
                diagonalAngle = MathF.Sin(2 * angle) >= 0
                    ? MathF.PI * 0.25f
                    : -MathF.PI * 0.25f;
            }
            Vector2 tangent = new(
                MathF.Cos(angle),
                MathF.Sin(angle));
            Vector2 diagonalTangent = new(
                MathF.Cos(diagonalAngle),
                MathF.Sin(diagonalAngle));
            if (Vector2.Dot(tangent, diagonalTangent) < 0)
            {
                diagonalTangent = -diagonalTangent;
            }
            float effectiveBias = Math.Clamp(
                diagonalBias * (1 - (0.35f * anisotropy)),
                0,
                1);
            tangent = Vector2.Normalize(Vector2.Lerp(
                tangent,
                diagonalTangent,
                effectiveBias));
            angle = MathF.Atan2(tangent.Y, tangent.X);
        }

        float cosine = MathF.Cos(angle);
        float sine = MathF.Sin(angle);
        float majorRadius =
            radius * widthScale * (1 + anisotropy);
        float minorRadius =
            radius * minorScale / (1 + anisotropy);
        float eta =
            (zeta + MathF.Cos(gamma)) /
            MathF.Pow(MathF.Sin(gamma), 2);
        Span<Vector3> colorSums = stackalloc Vector3[8];
        Span<Vector3> squareSums = stackalloc Vector3[8];
        Span<float> weightSums = stackalloc float[8];
        Span<float> sectorWeights = stackalloc float[8];
        for (int offsetY = -latticeRadius;
            offsetY <= latticeRadius;
            offsetY++)
        {
            for (int offsetX = -latticeRadius;
                offsetX <= latticeRadius;
                offsetX++)
            {
                float localX = offsetX / (float)latticeRadius;
                float localY = offsetY / (float)latticeRadius;
                float radiusSquared =
                    (localX * localX) +
                    (localY * localY);
                if (radiusSquared > 1)
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

                float sampleX = x +
                    (cosine * localX * majorRadius) -
                    (sine * localY * minorRadius);
                float sampleY = y +
                    (sine * localX * majorRadius) +
                    (cosine * localY * minorRadius);
                Vector4 sample = SamplePixelBilinear(
                    source,
                    width,
                    height,
                    sampleX,
                    sampleY);
                if (sample.W <= 0)
                {
                    continue;
                }
                Vector3 straight = Vector3.Clamp(
                    Unpremultiply(sample),
                    Vector3.Zero,
                    Vector3.One);
                float gaussian =
                    MathF.Exp(-3.125f * radiusSquared) /
                    sectorTotal;
                for (int sector = 0; sector < 8; sector++)
                {
                    float weight =
                        sectorWeights[sector] *
                        gaussian *
                        sample.W;
                    colorSums[sector] += straight * weight;
                    squareSums[sector] +=
                        straight * straight * weight;
                    weightSums[sector] += weight;
                }
            }
        }

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
                Vector3.Zero,
                (squareSums[sector] / weightSums[sector]) -
                    (mean * mean));
            float varianceSum = variance.X + variance.Y + variance.Z;
            float confidence = 1 /
                (1 + MathF.Pow(
                    MathF.Max(varianceSum * 100, 0),
                    sharpness));
            float meanLuminance = Vector3.Dot(
                mean,
                new Vector3(0.2126f, 0.7152f, 0.0722f));
            confidence *= MathF.Max(
                0.05f,
                1 +
                    (luminancePreference *
                        (meanLuminance - 0.5f)));
            result += mean * confidence;
            resultWeight += confidence;
        }

        if (resultWeight <= 0.000001f)
        {
            result = Unpremultiply(center);
        }
        else
        {
            result /= resultWeight;
        }
        return Associated(
            Vector3.Clamp(result, Vector3.Zero, Vector3.One),
            center.W);
    }

    internal static Vector4 FilmGrain(
        PrismCatalogFilterPlan plan,
        Vector4 center,
        int x,
        int y)
    {
        if (center.W <= 0)
        {
            return Vector4.Zero;
        }

        float intensity = Math.Clamp(
            Option(plan, "Intensity", 10),
            0,
            10) * 0.01f;
        if (intensity <= 0)
        {
            return center;
        }

        float grain = Math.Clamp(
            Option(plan, "Grain", 4),
            0,
            20);
        float grainScale = 1 + (grain * 0.25f);
        float sigma = grainScale * 0.55f;
        float inverseTwoSigmaSquared =
            0.5f / (sigma * sigma);
        float pixelX = x + 0.5f;
        float pixelY = y + 0.5f;
        int cellX = (int)MathF.Floor(pixelX / grainScale);
        int cellY = (int)MathF.Floor(pixelY / grainScale);
        uint seed = Seed(plan, "Seed");
        float weightedNoise = 0;
        float squaredWeightTotal = 0;
        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                int nodeX = cellX + offsetX;
                int nodeY = cellY + offsetY;
                float nodePositionX =
                    (nodeX + 0.5f) * grainScale;
                float nodePositionY =
                    (nodeY + 0.5f) * grainScale;
                float deltaX = pixelX - nodePositionX;
                float deltaY = pixelY - nodePositionY;
                float weight = MathF.Exp(
                    -((deltaX * deltaX) +
                        (deltaY * deltaY)) *
                    inverseTwoSigmaSquared);
                weightedNoise +=
                    FilmGrainGaussian(nodeX, nodeY, seed) *
                    weight;
                squaredWeightTotal += weight * weight;
            }
        }

        float correlatedNoise = weightedNoise /
            MathF.Sqrt(MathF.Max(
                squaredWeightTotal,
                0.000001f));
        Vector3 straight = Unpremultiply(center);
        float luminance = Math.Clamp(
            StraightLuminance(straight),
            0,
            1);
        float highlightArea = Math.Clamp(
            Option(plan, "HighlightArea", 0),
            0,
            20) / 20;
        float variancePeak = 0.5f +
            (highlightArea * 0.4f);
        float booleanLevel = luminance <= variancePeak
            ? 0.5f * luminance / variancePeak
            : 0.5f +
                (0.5f *
                    (luminance - variancePeak) /
                    (1 - variancePeak));
        float signalDeviation = 2 * MathF.Sqrt(
            MathF.Max(
                booleanLevel * (1 - booleanLevel),
                0));
        Vector3 result = straight +
            new Vector3(
                correlatedNoise *
                intensity *
                signalDeviation);
        return Associated(
            Vector3.Clamp(result, Vector3.Zero, Vector3.One),
            center.W);
    }

    private static float FilmGrainGaussian(
        int x,
        int y,
        uint seed) =>
        ((Hash(x, y, seed ^ 0xa511e9b3u) +
            Hash(x, y, seed ^ 0x63d83595u) +
            Hash(x, y, seed ^ 0xb8d26d4du) +
            Hash(x, y, seed ^ 0x9e3779b9u)) -
            2) *
        1.7320508075688772f;

    internal static Vector4 DryBrush(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = SamplePixel(
            source,
            width,
            height,
            x,
            y);
        if (center.W <= 0)
        {
            return Vector4.Zero;
        }

        float radius = MathF.Max(
            1,
            MathF.Max(pass.RadiusX, pass.RadiusY));
        float detail = Math.Clamp(
            Option(plan, "BrushDetail", 8),
            0,
            32);
        float textureStrength = Math.Clamp(
            Option(plan, "Texture", 1) / 4,
            0,
            1);
        uint seed = unchecked(
            (uint)(int)PrismFilterId.DryBrush *
            0x9e3779b9u);
        (float tensorX, float tensorCross, float tensorY) =
            FacetStructureTensor(
                source,
                width,
                height,
                x,
                y);
        float discriminant = MathF.Sqrt(
            MathF.Max(
                0,
                ((tensorX - tensorY) *
                    (tensorX - tensorY)) +
                (4 * tensorCross * tensorCross)));
        float lambda1 =
            0.5f * (tensorX + tensorY + discriminant);
        float lambda2 =
            0.5f * (tensorX + tensorY - discriminant);
        float tensorEnergy = lambda1 + lambda2;
        Vector2 tangent;
        if (tensorEnergy > 0.000001f)
        {
            float angle =
                (0.5f * MathF.Atan2(
                    2 * tensorCross,
                    tensorX - tensorY)) +
                (MathF.PI * 0.5f);
            tangent = new Vector2(
                MathF.Cos(angle),
                MathF.Sin(angle));
        }
        else
        {
            int blockSize = Math.Max(
                1,
                (int)MathF.Round(radius * 2));
            float angle = Hash(
                (int)MathF.Floor(x / (float)blockSize),
                (int)MathF.Floor(y / (float)blockSize),
                seed) * MathF.Tau;
            tangent = new Vector2(
                MathF.Cos(angle),
                MathF.Sin(angle));
        }

        Vector2 normal = new(-tangent.Y, tangent.X);
        float coherence =
            tensorEnergy <= 0.000001f
                ? 0
                : Math.Clamp(
                    (lambda1 - lambda2) /
                        tensorEnergy,
                    0,
                    1);
        float majorScale = 1 + (1.25f * coherence);
        float minorScale = 1 - (0.5f * coherence);
        float sharpness =
            1 + (5 * Math.Clamp(detail / 16, 0, 1));
        Vector3 accumulated = Vector3.Zero;
        float totalConfidence = 0;
        for (int sector = 0; sector < 8; sector++)
        {
            float angle = sector * (MathF.PI / 4);
            Vector2 direction =
                (tangent * (MathF.Cos(angle) * majorScale)) +
                (normal * (MathF.Sin(angle) * minorScale));
            direction = Vector2.Normalize(direction);
            DryBrushSector(
                source,
                width,
                height,
                x,
                y,
                direction,
                radius,
                center.W,
                out Vector3 mean,
                out float variance);
            float confidence = 1 /
                (1 + MathF.Pow(
                    MathF.Max(variance * 24, 0),
                    sharpness));
            accumulated += mean * confidence;
            totalConfidence += confidence;
        }

        Vector3 filtered = accumulated /
            MathF.Max(totalConfidence, 0.000001f);
        Vector2 pixel = new(x, y);
        float tangentCoordinate =
            Vector2.Dot(pixel, tangent);
        float normalCoordinate =
            Vector2.Dot(pixel, normal);
        float phaseScale = MathF.Max(radius * 4, 1);
        float normalPhaseScale = MathF.Max(radius * 2, 1);
        float phase = Hash(
            (int)MathF.Floor(
                tangentCoordinate / phaseScale),
            (int)MathF.Floor(
                normalCoordinate / normalPhaseScale),
            seed ^ 0x68bc21ebu);
        float fiberCoordinate =
            normalCoordinate /
            MathF.Max(radius * 0.32f, 0.75f);
        float fiber =
            0.5f +
            (0.5f * MathF.Cos(
                (fiberCoordinate * MathF.Tau) +
                (phase * MathF.Tau)));
        float grain = Hash(
            x,
            y,
            seed ^ 0x02e5be93u);
        float dryPattern = MathF.Pow(
            Math.Clamp(
                (fiber * 0.82f) +
                (grain * 0.18f),
                0,
                1),
            1.4f);
        Vector3 paperGap = Vector3.Lerp(
            filtered,
            Vector3.One,
            0.3f);
        Vector3 result = Vector3.Lerp(
            filtered,
            paperGap,
            textureStrength * dryPattern);
        return Associated(
            Vector3.Clamp(
                result,
                Vector3.Zero,
                Vector3.One),
            center.W);
    }

    private static void DryBrushSector(
        Vector4[] source,
        int width,
        int height,
        float x,
        float y,
        Vector2 direction,
        float radius,
        float centerAlpha,
        out Vector3 mean,
        out float variance)
    {
        Vector3 sum = Vector3.Zero;
        Vector3 squareSum = Vector3.Zero;
        float totalWeight = 0;
        DryBrushAccumulateSample(
            source,
            width,
            height,
            x,
            y,
            Vector2.Zero,
            1,
            centerAlpha,
            ref sum,
            ref squareSum,
            ref totalWeight);
        for (int step = 1; step <= 3; step++)
        {
            float fraction = step / 3f;
            float spatialWeight = MathF.Exp(
                -2 * fraction * fraction);
            Vector2 offset =
                direction * (radius * fraction);
            DryBrushAccumulateSample(
                source,
                width,
                height,
                x,
                y,
                offset,
                spatialWeight,
                centerAlpha,
                ref sum,
                ref squareSum,
                ref totalWeight);
        }

        mean = sum / MathF.Max(totalWeight, 0.000001f);
        Vector3 colorVariance = Vector3.Max(
            (squareSum /
                MathF.Max(totalWeight, 0.000001f)) -
                (mean * mean),
            Vector3.Zero);
        variance =
            (colorVariance.X +
                colorVariance.Y +
                colorVariance.Z) /
            3;
    }

    private static void DryBrushAccumulateSample(
        Vector4[] source,
        int width,
        int height,
        float x,
        float y,
        Vector2 offset,
        float spatialWeight,
        float centerAlpha,
        ref Vector3 sum,
        ref Vector3 squareSum,
        ref float totalWeight)
    {
        Vector4 sample = SamplePixelBilinear(
            source,
            width,
            height,
            x + offset.X,
            y + offset.Y);
        if (sample.W <= 0)
        {
            return;
        }

        float weight =
            spatialWeight *
            MathF.Exp(
                -MathF.Abs(sample.W - centerAlpha) * 8);
        Vector3 color = Unpremultiply(sample);
        sum += color * weight;
        squareSum += color * color * weight;
        totalWeight += weight;
    }

    internal static Vector4 Cutout(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        if (center.W <= 0)
        {
            return Vector4.Zero;
        }
        if (pass.Kind == PrismCatalogFilterPassKind.Direct)
        {
            int levels = Math.Clamp(
                (int)MathF.Round(Option(plan, "Levels", 8)),
                2,
                32);
            return Associated(
                Quantize(
                    Vector3.Clamp(
                        Unpremultiply(center),
                        Vector3.Zero,
                        Vector3.One),
                    levels),
                center.W);
        }

        float radius = MathF.Max(
            1,
            MathF.Max(pass.RadiusX, pass.RadiusY));
        float fidelity = Math.Clamp(
            Option(plan, "EdgeFidelity", 3) / 10,
            0,
            1);
        float rangeSigma = 0.42f - (0.36f * fidelity);
        float rangeDivisor =
            2 * rangeSigma * rangeSigma;
        const float SpatialDivisor = 3.125f;
        Vector3 centerColor = Unpremultiply(center);
        Vector3 accumulated = Vector3.Zero;
        float totalWeight = 0;
        for (int offsetY = -2; offsetY <= 2; offsetY++)
        {
            for (int offsetX = -2; offsetX <= 2; offsetX++)
            {
                Vector4 sample = SamplePixelBilinear(
                    source,
                    width,
                    height,
                    x + (offsetX * radius * 0.5f),
                    y + (offsetY * radius * 0.5f));
                if (sample.W <= 0)
                {
                    continue;
                }

                Vector3 sampleColor = Unpremultiply(sample);
                float spatialDistance =
                    (offsetX * offsetX) +
                    (offsetY * offsetY);
                float rangeDistance =
                    Vector3.DistanceSquared(
                        sampleColor,
                        centerColor);
                float weight =
                    MathF.Exp(
                        -spatialDistance /
                        SpatialDivisor) *
                    MathF.Exp(
                        -rangeDistance /
                        rangeDivisor) *
                    MathF.Exp(
                        -MathF.Abs(sample.W - center.W) * 8);
                accumulated += sampleColor * weight;
                totalWeight += weight;
            }
        }

        Vector3 shifted = totalWeight > 0
            ? accumulated / totalWeight
            : centerColor;
        return Associated(
            Vector3.Clamp(
                shifted,
                Vector3.Zero,
                Vector3.One),
            center.W);
    }

    private static Vector4 EdgeDetection(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        Vector3 straight = Unpremultiply(center);
        if (plan.Filter == PrismFilterId.Emboss)
        {
            return PrismEmbossFilter.ApplyPixel(
                plan, pass, source, width, height, x, y, center);
        }

        if (plan.Filter == PrismFilterId.FindEdges)
        {
            return PrismFindEdgesFilter.ApplyPixel(
                plan, source, width, height, x, y, center);
        }

        float edge = Sobel(source, width, height, x, y);

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







    internal static Vector4 Extrude(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        float size = MathF.Max(
            1,
            Option(plan, "Size", 30));
        float depth = Math.Clamp(
            Option(plan, "Depth", 30),
            0,
            size);
        int cellX = (int)MathF.Floor(x / size);
        int cellY = (int)MathF.Floor(y / size);
        int type = Symbol(plan, "Type");
        bool maskIncompleteBlocks =
            Option(plan, "MaskIncompleteBlocks", 0) >= 0.5f;
        bool solidFrontFaces =
            Option(plan, "SolidFrontFaces", 1) >= 0.5f;
        bool complete = IsCompleteExtrudeCell(
            cellX,
            cellY,
            size,
            width,
            height);
        Vector4 front = maskIncompleteBlocks && !complete
            ? Vector4.Zero
            : type == 0 && solidFrontFaces
                ? ExtrudeCellSample(
                    source,
                    width,
                    height,
                    cellX,
                    cellY,
                    size)
                : source[(y * width) + x];

        return type == 1
            ? ExtrudePyramid(
                plan,
                source,
                width,
                height,
                x + 0.5f,
                y + 0.5f,
                cellX,
                cellY,
                size,
                depth,
                maskIncompleteBlocks,
                solidFrontFaces,
                front)
            : ExtrudeBlock(
                plan,
                source,
                width,
                height,
                x + 0.5f,
                y + 0.5f,
                cellX,
                cellY,
                size,
                depth,
                maskIncompleteBlocks,
                front);
    }

    private static Vector4 ExtrudeBlock(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        float pixelX,
        float pixelY,
        int cellX,
        int cellY,
        float size,
        float depth,
        bool maskIncompleteBlocks,
        Vector4 front)
    {
        float bestScore = -1;
        Vector4 result = front;
        int firstCellX = Math.Max(0, cellX - 2);
        int firstCellY = Math.Max(0, cellY - 2);
        for (int candidateY = firstCellY;
            candidateY <= cellY;
            candidateY++)
        {
            for (int candidateX = firstCellX;
                candidateX <= cellX;
                candidateX++)
            {
                if (!IsCompleteExtrudeCell(
                        candidateX,
                        candidateY,
                        size,
                        width,
                        height) &&
                    maskIncompleteBlocks)
                {
                    continue;
                }

                float candidateDepth = ExtrudeCellDepth(
                    plan,
                    candidateX,
                    candidateY,
                    size,
                    depth);
                if (!TryExtrudeBlockSide(
                        pixelX,
                        pixelY,
                        candidateX,
                        candidateY,
                        size,
                        candidateDepth,
                        out float sideScore,
                        out float shade))
                {
                    continue;
                }

                float score = candidateDepth + sideScore * 0.001f;
                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                result = ShadeExtrudeFace(
                    ExtrudeCellSample(
                        source,
                        width,
                        height,
                        candidateX,
                        candidateY,
                        size),
                    shade);
            }
        }

        return result;
    }

    private static Vector4 ExtrudePyramid(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        float pixelX,
        float pixelY,
        int cellX,
        int cellY,
        float size,
        float depth,
        bool maskIncompleteBlocks,
        bool solidFrontFaces,
        Vector4 front)
    {
        float bestScore = -1;
        Vector4 result = front;
        int firstCellX = Math.Max(0, cellX - 2);
        int firstCellY = Math.Max(0, cellY - 2);
        for (int candidateY = firstCellY;
            candidateY <= cellY;
            candidateY++)
        {
            for (int candidateX = firstCellX;
                candidateX <= cellX;
                candidateX++)
            {
                if (!IsCompleteExtrudeCell(
                        candidateX,
                        candidateY,
                        size,
                        width,
                        height) &&
                    maskIncompleteBlocks)
                {
                    continue;
                }

                float candidateDepth = ExtrudeCellDepth(
                    plan,
                    candidateX,
                    candidateY,
                    size,
                    depth);
                float left = candidateX * size;
                float top = candidateY * size;
                float right = MathF.Min(
                    (candidateX + 1) * size,
                    width);
                float bottom = MathF.Min(
                    (candidateY + 1) * size,
                    height);
                Vector2 apex = new(
                    (left + right) * 0.5f + candidateDepth * 0.75f,
                    (top + bottom) * 0.5f + candidateDepth * 0.75f);
                int face = ExtrudePyramidFace(
                    new Vector2(pixelX, pixelY),
                    new(left, top),
                    new(right, top),
                    new(right, bottom),
                    new(left, bottom),
                    apex);
                if (face < 0)
                {
                    continue;
                }

                bool currentCell =
                    candidateX == cellX && candidateY == cellY;
                float score = currentCell
                    ? 0
                    : 1 + candidateDepth;
                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                Vector4 candidate = ExtrudeCellSample(
                    source,
                    width,
                    height,
                    candidateX,
                    candidateY,
                    size);
                if (currentCell && !solidFrontFaces)
                {
                    candidate = source[
                        ((int)pixelY * width) + (int)pixelX];
                }
                result = ShadeExtrudeFace(
                    candidate,
                    face switch
                    {
                        0 => 0.84f,
                        1 => 0.68f,
                        2 => 0.54f,
                        _ => 0.74f
                    });
            }
        }

        return result;
    }

    private static float ExtrudeCellDepth(
        PrismCatalogFilterPlan plan,
        int cellX,
        int cellY,
        float size,
        float depth)
    {
        if (depth <= 0)
        {
            return 0;
        }

        int depthMode = Symbol(plan, "DepthMode");
        float level = depthMode == 1
            ? 1
            : 0.45f +
                ExtrudeHash(
                    cellX,
                    cellY,
                    Seed(plan, "Seed")) *
                0.55f;
        return depth * level;
    }

    private static float ExtrudeHash(
        int cellX,
        int cellY,
        uint seed)
    {
        float value =
            (cellX * 127.1f) +
            (cellY * 311.7f);
        float hashed = MathF.Sin(
                value +
                (seed * 0.00006103515625f)) *
            43758.5453123f;
        return hashed - MathF.Floor(hashed);
    }

    private static bool IsCompleteExtrudeCell(
        int cellX,
        int cellY,
        float size,
        int width,
        int height)
    {
        if (cellX < 0 || cellY < 0)
        {
            return false;
        }

        float left = cellX * size;
        float top = cellY * size;
        return left + size <= width + 0.0001f &&
            top + size <= height + 0.0001f;
    }

    private static Vector4 ExtrudeCellSample(
        Vector4[] source,
        int width,
        int height,
        int cellX,
        int cellY,
        float size)
    {
        float left = cellX * size;
        float top = cellY * size;
        return SamplePixel(
            source,
            width,
            height,
            left + (size * 0.5f),
            top + (size * 0.5f));
    }

    private static bool TryExtrudeBlockSide(
        float pixelX,
        float pixelY,
        int cellX,
        int cellY,
        float size,
        float depth,
        out float score,
        out float shade)
    {
        score = 0;
        shade = 0;
        if (depth <= 0)
        {
            return false;
        }

        float left = cellX * size;
        float top = cellY * size;
        float right = left + size;
        float bottom = top + size;
        float offset = depth * 0.75f;
        bool hit = false;
        float bestShade = float.PositiveInfinity;
        float bestScore = 0;

        float rightT = (pixelX - right) / offset;
        if (rightT is >= 0 and <= 1)
        {
            float minimumY = top + (rightT * offset);
            float maximumY = bottom + (rightT * offset);
            if (pixelY >= minimumY && pixelY <= maximumY)
            {
                hit = true;
                bestScore = rightT;
                bestShade = 0.76f - (rightT * 0.16f);
            }
        }

        float bottomT = (pixelY - bottom) / offset;
        if (bottomT is >= 0 and <= 1)
        {
            float minimumX = left + (bottomT * offset);
            float maximumX = right + (bottomT * offset);
            if (pixelX >= minimumX && pixelX <= maximumX)
            {
                float bottomShade = 0.58f - (bottomT * 0.14f);
                if (!hit || bottomShade < bestShade)
                {
                    hit = true;
                    bestScore = bottomT;
                    bestShade = bottomShade;
                }
            }
        }

        score = bestScore;
        shade = bestShade;
        return hit;
    }

    private static int ExtrudePyramidFace(
        Vector2 point,
        Vector2 topLeft,
        Vector2 topRight,
        Vector2 bottomRight,
        Vector2 bottomLeft,
        Vector2 apex)
    {
        if (PointInTriangle(point, topLeft, topRight, apex))
        {
            return 0;
        }
        if (PointInTriangle(point, topRight, bottomRight, apex))
        {
            return 1;
        }
        if (PointInTriangle(point, bottomRight, bottomLeft, apex))
        {
            return 2;
        }
        return PointInTriangle(point, bottomLeft, topLeft, apex)
            ? 3
            : -1;
    }

    private static bool PointInTriangle(
        Vector2 point,
        Vector2 first,
        Vector2 second,
        Vector2 third)
    {
        float firstCross = Cross(second - first, point - first);
        float secondCross = Cross(third - second, point - second);
        float thirdCross = Cross(first - third, point - third);
        bool hasNegative =
            firstCross < -0.0001f ||
            secondCross < -0.0001f ||
            thirdCross < -0.0001f;
        bool hasPositive =
            firstCross > 0.0001f ||
            secondCross > 0.0001f ||
            thirdCross > 0.0001f;
        return !(hasNegative && hasPositive);
    }

    private static float Cross(Vector2 left, Vector2 right) =>
        (left.X * right.Y) - (left.Y * right.X);

    private static Vector4 ShadeExtrudeFace(
        Vector4 color,
        float shade)
    {
        return Associated(
            Vector3.Clamp(
                Unpremultiply(color) * shade,
                Vector3.Zero,
                Vector3.One),
            color.W);
    }

    private static int Symbol(
        PrismCatalogFilterPlan plan,
        string name) =>
        IntegerBits(plan.GetOption(name));

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
            return PrismChromaticAberrationFilter.ApplyPixel(
                plan, source, width, height, x, y);
        }
        return PrismTilesFilter.ApplyPixel(
            plan,
            source,
            width,
            height,
            x,
            y);
    }

    private static Vector4 Texture(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Func<Vector2, Vector4>? primaryResource)
    {
        if (plan.Filter == PrismFilterId.OilPaint)
        {
            return PrismOilPaintFilter.ApplyPixel(
                plan,
                pass,
                source,
                width,
                height,
                x,
                y);
        }

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

    internal static Vector4 OilPaint(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        if (center.W <= 0)
        {
            return Vector4.Zero;
        }

        float stylization = Math.Clamp(
            Option(plan, "Stylization", 0.5f),
            0,
            1);
        float cleanliness = Math.Clamp(
            Option(plan, "Cleanliness", 0.5f),
            0,
            1);
        float bristleDetail = Math.Clamp(
            Option(plan, "BristleDetail", 0.5f),
            0,
            1);
        bool lighting = Option(plan, "Lighting", 1) >= 0.5f;
        float angle = Option(plan, "Angle", 0) *
            (MathF.PI / 180);
        float shine = Math.Clamp(
            Option(plan, "Shine", 0.5f),
            0,
            1);
        float radius = Math.Clamp(
            MathF.Max(pass.RadiusX, pass.RadiusY),
            1,
            12);
        float sharpness =
            1.5f +
            (8 * stylization) +
            (2 * cleanliness);
        float roughness =
            (1 - cleanliness) *
            bristleDetail *
            0.65f;
        Vector4 painted = PolynomialAnisotropicKuwahara(
            source,
            width,
            height,
            x,
            y,
            radius,
            sharpness,
            1.1f + (0.5f * stylization),
            1 - (0.35f * stylization),
            roughness,
            0,
            0,
            0,
            false,
            0x6f696c50u);
        Vector3 straight = Unpremultiply(center);
        Vector3 result = Vector3.Lerp(
            straight,
            Unpremultiply(painted),
            0.35f + (0.65f * stylization));

        float cosine = MathF.Cos(angle);
        float sine = MathF.Sin(angle);
        float along = (x * cosine) + (y * sine);
        float across = (-x * sine) + (y * cosine);
        float ridge = 0.5f +
            (0.5f * MathF.Cos(
                along * (0.75f + (1.5f * bristleDetail)) +
                (MathF.Sin(across * 0.35f) * 1.2f)));
        float grain = Hash(
            (int)MathF.Floor(x / MathF.Max(radius * 0.75f, 1)),
            (int)MathF.Floor(y / MathF.Max(radius * 0.75f, 1)),
            0x62726973u);
        float bristle =
            (((0.65f * ridge) + (0.35f * grain)) - 0.5f) *
            bristleDetail *
            (1 - (0.65f * cleanliness)) *
            0.12f;
        result *= 1 + bristle;

        if (lighting)
        {
            float sampleOffset = MathF.Max(1, radius * 0.35f);
            float left = Luminance(SamplePixel(
                source,
                width,
                height,
                x - sampleOffset,
                y));
            float right = Luminance(SamplePixel(
                source,
                width,
                height,
                x + sampleOffset,
                y));
            float up = Luminance(SamplePixel(
                source,
                width,
                height,
                x,
                y - sampleOffset));
            float down = Luminance(SamplePixel(
                source,
                width,
                height,
                x,
                y + sampleOffset));
            float heightScale = 0.8f + (1.6f * stylization);
            Vector3 normal = Vector3.Normalize(new Vector3(
                (left - right) * heightScale,
                (up - down) * heightScale,
                1));
            Vector3 light = Vector3.Normalize(new Vector3(
                -cosine * 0.55f,
                -sine * 0.55f,
                0.85f));
            float diffuse = MathF.Max(Vector3.Dot(normal, light), 0);
            Vector3 halfVector = Vector3.Normalize(
                light + Vector3.UnitZ);
            float specular = MathF.Pow(
                MathF.Max(Vector3.Dot(normal, halfVector), 0),
                8 + (24 * (1 - shine))) *
                shine *
                0.16f;
            result =
                (result * (0.86f + (0.22f * diffuse))) +
                new Vector3(specular);
        }

        return Associated(
            Vector3.Clamp(result, Vector3.Zero, Vector3.One),
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
        return PrismCustomConvolutionFilter.ApplyPixel(
            plan, source, width, height, x, y, kernel);
    }

    private static Vector4 Color(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        PrismColorMatrixResource? colorMatrixResource)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        Vector3 straight = Unpremultiply(center);
        if (plan.Filter == PrismFilterId.Solarize)
        {
            straight = PrismSolarizeFilter.Apply(
                straight,
                Option(plan, "Threshold", 0.5f));
        }
        else if (plan.Filter == PrismFilterId.Color)
        {
            straight = PrismColorFilter.Apply(
                straight,
                Option(plan, "Brightness", 0),
                Option(plan, "Contrast", 1),
                Option(plan, "Exposure", 0),
                Option(plan, "Saturation", 1),
                Option(plan, "Hue", 0),
                Option(plan, "Temperature", 0),
                OptionVector(plan, "Tint", Vector4.Zero),
                Option(plan, "Clamp", 1) >= 0.5f);
        }
        else if (plan.Filter == PrismFilterId.ColorMatrix)
        {
            Vector4 transformed = PrismColorMatrixFilter.Apply(
                new Vector4(straight, center.W),
                colorMatrixResource,
                Option(plan, "Clamp", 1) >= 0.5f);
            return Associated(
                new Vector3(
                    transformed.X,
                    transformed.Y,
                    transformed.Z),
                transformed.W);
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

    internal static Vector4 Pointillize(
        Vector4[] source,
        int width,
        int height,
        float x,
        float y,
        float cellSize,
        uint seed,
        Vector4 background)
    {
        int cellX = (int)MathF.Floor(x / cellSize);
        int cellY = (int)MathF.Floor(y / cellSize);
        float bestScore = float.PositiveInfinity;
        Vector2 bestCenter = default;
        float bestRadius = 0;

        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                int candidateCellX = cellX + offsetX;
                int candidateCellY = cellY + offsetY;
                Vector2 candidateCenter =
                    PrismIncrementalVoronoiSet.Center(
                        candidateCellX,
                        candidateCellY,
                        seed,
                        cellSize);
                Vector4 candidateSample = SamplePixelBilinear(
                    source,
                    width,
                    height,
                    candidateCenter.X,
                    candidateCenter.Y);
                float darkness = Math.Clamp(
                    (1 - Luminance(candidateSample)) *
                        candidateSample.W,
                    0,
                    1);
                if (PrismIncrementalVoronoiSet.Threshold(
                        candidateCellX,
                        candidateCellY,
                        seed) >
                    darkness)
                {
                    continue;
                }

                float radius = cellSize *
                    (0.28f + (0.2f * MathF.Sqrt(darkness)));
                float antialiasWidth = MathF.Min(0.75f, radius);
                Vector2 delta =
                    new(x - candidateCenter.X, y - candidateCenter.Y);
                float distanceSquared = delta.LengthSquared();
                float maximumDistance = radius + antialiasWidth;
                if (distanceSquared >
                    maximumDistance * maximumDistance)
                {
                    continue;
                }

                float score =
                    distanceSquared / (radius * radius);
                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestCenter = candidateCenter;
                bestRadius = radius;
            }
        }

        if (!float.IsFinite(bestScore))
        {
            return background;
        }

        float distance = Vector2.Distance(
            new Vector2(x, y),
            bestCenter);
        float antialias = MathF.Min(0.75f, bestRadius);
        float coverage = 1 - SmoothStep(
            bestRadius - antialias,
            bestRadius + antialias,
            distance);
        float sampleOffset = MathF.Min(
            bestRadius * 0.35f,
            1.5f);
        Vector4 dotColor = (
            SamplePixelBilinear(
                source,
                width,
                height,
                bestCenter.X,
                bestCenter.Y) +
            SamplePixelBilinear(
                source,
                width,
                height,
                bestCenter.X - sampleOffset,
                bestCenter.Y) +
            SamplePixelBilinear(
                source,
                width,
                height,
                bestCenter.X + sampleOffset,
                bestCenter.Y) +
            SamplePixelBilinear(
                source,
                width,
                height,
                bestCenter.X,
                bestCenter.Y - sampleOffset) +
            SamplePixelBilinear(
                source,
                width,
                height,
                bestCenter.X,
                bestCenter.Y + sampleOffset)) / 5;
        return ClampAssociated(
            Vector4.Lerp(
                background,
                dotColor,
                coverage));
    }

    private static float SmoothStep(
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

    private static int IntegerBits(Vector4 value) =>
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

    internal static float Luminance(Vector4 color) =>
        Vector3.Dot(
            Unpremultiply(color),
            new Vector3(0.2126f, 0.7152f, 0.0722f));

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

    private static Vector4 ClampAssociated(Vector4 color)
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
