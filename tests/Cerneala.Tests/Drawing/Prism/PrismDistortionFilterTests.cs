using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Markup;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismDistortionFilterTests
{
    [Fact]
    public void CatalogDrivesEveryResamplingPlannerKernelTestAndDocumentation()
    {
        PrismCatalogEntryDescriptor[] entries =
            DistortionEntries();
        PrismFilterId[] filters = entries
            .Select(entry =>
                (PrismFilterId)entry.StableId)
            .ToArray();
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "All distortion filters",
            filters: filters.Select(
                filter =>
                    new PrismFilterDefinition(filter)));
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "Distortion defaults",
                layer),
            bounds: new DrawRect(0, 0, 64, 48));
        PrismLayerState layerState =
            scope.Instance.GetLayerState(layer.Id);
        for (int index = 0; index < entries.Length; index++)
        {
            ConfigureRequiredResources(
                layerState.Filters[index],
                entries[index]);
        }

        PrismGraph graph = BuildGraph(scope);
        PrismGraphNode[] nodes = graph.Nodes
            .Where(node =>
                node.Kind == PrismGraphNodeKind.Filter)
            .ToArray();

        Assert.Equal(18, entries.Length);
        Assert.Equal(
            entries.Length,
            nodes
                .Select(node =>
                    Assert.IsType<PrismResamplingPlan>(
                        node.ResamplingPlan)
                        .Operation)
                .Distinct()
                .Count());
        Assert.Equal(
            3,
            nodes.Count(node =>
                node.Filter ==
                    PrismFilterId.DiffuseGlow));
        Assert.Equal(
            4,
            nodes.Count(node =>
                node.Filter ==
                    PrismFilterId.NeonGlow));
        foreach (PrismCatalogEntryDescriptor entry in entries)
        {
            PrismFilterId filter =
                (PrismFilterId)entry.StableId;
            PrismGraphNode[] filterNodes = nodes
                .Where(node => node.Filter == filter)
                .ToArray();

            Assert.NotEmpty(filterNodes);
            Assert.True(
                PrismResamplingPlanner.IsSupported(filter));
            Assert.Equal(
                $"PrismKernelRegistry/{entry.Symbol}",
                entry.Coverage.Kernel);
            Assert.Equal(
                $"PrismDistortionFilterTests/{entry.Symbol}",
                entry.Coverage.Test);
            Assert.StartsWith(
                "generated:",
                entry.Coverage.Documentation,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                entry.Properties,
                property => property.Name is
                    "Source" or
                    "Shader" or
                    "ShaderFilename");
            Assert.All(
                filterNodes,
                node =>
                {
                    Assert.Equal(
                        entry.Properties.Length,
                        node.Parameters.Length);
                    PrismResamplingPlan prepared =
                        Assert.IsType<PrismResamplingPlan>(
                            node.ResamplingPlan);
                    Assert.Equal(filter, prepared.Filter);
                    Assert.InRange(
                        node.ResamplingPassIndex,
                        0,
                        prepared.Passes.Length - 1);
                });
            PrismCatalogExecutionDescriptor execution =
                Assert.IsType<PrismCatalogExecutionDescriptor>(
                    entry.Execution);
            Assert.Equal(
                filter == PrismFilterId.NeonGlow
                    ? "edge-morphology-quantization-texture"
                    : "coordinate-map-morphology",
                execution.Primitive);
            Assert.Equal(
                "linear-premultiplied-rgba",
                execution.SurfaceFormat);
            Assert.Equal(
                "working-profile",
                execution.ColorSpace);
        }
    }

    [Fact]
    public void CatalogAcceptedAdaptiveWideAngleFocalLengthDoesNotFailDuringGraphBuild()
    {
        Exception? failure = Record.Exception(
            () => CreateGraph(
                PrismFilterId.AdaptiveWideAngle,
                new DrawRect(0, 0, 37, 19),
                (state, entry) => SetVector(
                    state,
                    entry,
                    "FocalLength",
                    Vector4.Zero)));

        Assert.True(
            failure is null or ArgumentOutOfRangeException,
            $"FocalLength must be rejected during assignment or remain " +
            $"valid through graph construction. Actual: {failure}");
    }

    [Fact]
    public void ShearUsesSignedAmountCenteredShapePreservingCurveAndBilinearSampling()
    {
        const int width = 9;
        const int height = 5;
        PrismPremultipliedColor[] source =
            CreateCoordinateGradient(width, height);
        PrismResamplingPlan identity = CreatePlan(
            PrismFilterId.Shear,
            new DrawRect(0, 0, width, height));
        PrismResamplingPlan positive = CreatePlan(
            PrismFilterId.Shear,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "Amount", 0.5f);
                SetSymbol(state, entry, "Curve", "SCurve");
                SetSymbol(
                    state,
                    entry,
                    "UndefinedAreas",
                    "Transparent");
            });
        PrismResamplingPlan negative = CreatePlan(
            PrismFilterId.Shear,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "Amount", -0.5f);
                SetSymbol(state, entry, "Curve", "SCurve");
                SetSymbol(
                    state,
                    entry,
                    "UndefinedAreas",
                    "Transparent");
            });

        PrismPremultipliedColor[] unchanged =
            PrismResamplingMath.Apply(
                identity,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] shiftedRight =
            PrismResamplingMath.Apply(
                positive,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] shiftedLeft =
            PrismResamplingMath.Apply(
                negative,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        Assert.Equal(source, unchanged);
        PrismPremultipliedColor positiveTop =
            shiftedRight[(0 * width) + 4];
        PrismPremultipliedColor positiveCenter =
            shiftedRight[(2 * width) + 4];
        PrismPremultipliedColor positiveBottom =
            shiftedRight[(4 * width) + 4];
        Assert.True(positiveTop.Red > positiveCenter.Red);
        Assert.True(positiveCenter.Red > positiveBottom.Red);
        Assert.Equal(0.5, positiveCenter.Red, 6);
        Assert.Equal(
            positiveTop.Red,
            shiftedLeft[(4 * width) + 4].Red,
            6);
        Assert.Equal(
            positiveBottom.Red,
            shiftedLeft[(0 * width) + 4].Red,
            6);
        Assert.InRange(
            shiftedRight[((height - 1) * width)].Alpha,
            0,
            0.999999);
        Assert.All(shiftedRight, AssertFiniteAssociated);
        Assert.All(shiftedLeft, AssertFiniteAssociated);
    }

    [Fact]
    public void SpherizeUsesOrthographicCapAndInverseProjection()
    {
        Vector4 positive = new(1, 0, 0.5f, 0.5f);
        Vector4 negative = new(-1, 0, 0.5f, 0.5f);
        Vector2 coordinate = new(0.75f, 0.5f);

        Vector2 projected =
            PrismResamplingMath.MapSpherizeCoordinate(
                positive,
                coordinate);
        Vector2 restored =
            PrismResamplingMath.MapSpherizeCoordinate(
                negative,
                projected);

        Assert.Equal(2f / 3f, projected.X, 6);
        Assert.Equal(0.5f, projected.Y, 6);
        Assert.Equal(coordinate.X, restored.X, 6);
        Assert.Equal(coordinate.Y, restored.Y, 6);
    }

    [Fact]
    public void SpherizeSaturatesAmountAndKeepsFixedContinuousSupport()
    {
        Vector2 center = new(0.25f, 0.75f);
        Vector2 inside = center + new Vector2(0.25f, 0);
        Vector2 boundary = center + new Vector2(0.5f, 0);
        Vector2 outside = center + new Vector2(0.51f, 0);
        Vector4 positive = new(1, 0, center.X, center.Y);
        Vector4 positiveExtreme =
            new(float.MaxValue, 0, center.X, center.Y);
        Vector4 negative = new(-1, 0, center.X, center.Y);
        Vector4 negativeExtreme =
            new(float.MinValue, 0, center.X, center.Y);

        Assert.Equal(
            PrismResamplingMath.MapSpherizeCoordinate(
                positive,
                inside),
            PrismResamplingMath.MapSpherizeCoordinate(
                positiveExtreme,
                inside));
        Assert.Equal(
            PrismResamplingMath.MapSpherizeCoordinate(
                negative,
                inside),
            PrismResamplingMath.MapSpherizeCoordinate(
                negativeExtreme,
                inside));
        Assert.Equal(
            boundary,
            PrismResamplingMath.MapSpherizeCoordinate(
                positive,
                boundary));
        Assert.Equal(
            outside,
            PrismResamplingMath.MapSpherizeCoordinate(
                positive,
                outside));
    }

    [Fact]
    public void SpherizeUniaxialModesAreIndependent()
    {
        Vector4 horizontal =
            new(1, 1, 0.5f, 0.5f);
        Vector4 vertical =
            new(1, 2, 0.5f, 0.5f);
        Vector2 first = new(0.7f, 0.6f);
        Vector2 second = new(0.7f, 0.9f);

        Vector2 firstHorizontal =
            PrismResamplingMath.MapSpherizeCoordinate(
                horizontal,
                first);
        Vector2 secondHorizontal =
            PrismResamplingMath.MapSpherizeCoordinate(
                horizontal,
                second);
        Vector2 firstVertical =
            PrismResamplingMath.MapSpherizeCoordinate(
                vertical,
                first);

        Assert.Equal(firstHorizontal.X, secondHorizontal.X, 6);
        Assert.Equal(first.Y, firstHorizontal.Y);
        Assert.Equal(second.Y, secondHorizontal.Y);
        Assert.Equal(first.X, firstVertical.X);
        Assert.NotEqual(first.Y, firstVertical.Y);
    }

    [Fact]
    public void ZigZagStylesUseTheirDocumentedDisplacementDirections()
    {
        const int size = 201;
        const int center = size / 2;
        const int sampleX = center + 50;
        const int sampleY = center;
        int sampleIndex = (sampleY * size) + sampleX;

        PrismPremultipliedColor[] pond =
            ApplyZigZag(
                size,
                size,
                amount: 1,
                ridges: 1,
                style: "PondRipples");
        PrismPremultipliedColor[] outward =
            ApplyZigZag(
                size,
                size,
                amount: 1,
                ridges: 1,
                style: "OutFromCenter");
        PrismPremultipliedColor[] around =
            ApplyZigZag(
                size,
                size,
                amount: 1,
                ridges: 1,
                style: "AroundCenter");
        Vector2 pondDisplacement = CoordinateDisplacement(
            pond[sampleIndex],
            sampleX,
            sampleY,
            size,
            size);
        Vector2 outwardDisplacement = CoordinateDisplacement(
            outward[sampleIndex],
            sampleX,
            sampleY,
            size,
            size);
        Vector2 aroundDisplacement = CoordinateDisplacement(
            around[sampleIndex],
            sampleX,
            sampleY,
            size,
            size);
        Vector2 aroundMapped =
            new Vector2(sampleX, sampleY) +
            aroundDisplacement;

        Assert.True(pondDisplacement.X > 0.1f);
        Assert.Equal(
            pondDisplacement.X,
            pondDisplacement.Y,
            3);
        Assert.True(outwardDisplacement.X > 0.1f);
        Assert.InRange(
            MathF.Abs(outwardDisplacement.Y),
            0,
            0.001f);
        Assert.True(aroundDisplacement.Y > 0.1f);
        Assert.Equal(
            50,
            Vector2.Distance(
                new Vector2(center, center),
                aroundMapped),
            3);
    }

    [Fact]
    public void ZigZagUsesPixelSpaceRadiusOnWideSurfaces()
    {
        const int width = 201;
        const int height = 101;
        const int centerX = width / 2;
        const int centerY = height / 2;
        PrismPremultipliedColor[] result =
            ApplyZigZag(
                width,
                height,
                amount: 1,
                ridges: 2,
                style: "OutFromCenter");
        Vector2 horizontal = CoordinateDisplacement(
            result[(centerY * width) + centerX + 20],
            centerX + 20,
            centerY,
            width,
            height);
        Vector2 vertical = CoordinateDisplacement(
            result[((centerY + 20) * width) + centerX],
            centerX,
            centerY + 20,
            width,
            height);

        Assert.Equal(
            horizontal.Length(),
            vertical.Length(),
            3);
        Assert.True(horizontal.X > 0.1f);
        Assert.True(vertical.Y > 0.1f);
    }

    [Fact]
    public void ZigZagRidgesCountRadialDirectionReversals()
    {
        const int size = 401;
        const int center = size / 2;
        const int ridges = 5;
        PrismPremultipliedColor[] result =
            ApplyZigZag(
                size,
                size,
                amount: 1,
                ridges,
                style: "PondRipples");
        Vector2 direction = Vector2.Normalize(Vector2.One);
        int reversals = 0;
        int previousSign = 0;
        for (int offset = 2; offset < center; offset++)
        {
            int x = center + offset;
            int y = center + offset;
            Vector2 displacement = CoordinateDisplacement(
                result[(y * size) + x],
                x,
                y,
                size,
                size);
            float signedDisplacement =
                Vector2.Dot(displacement, direction);
            if (MathF.Abs(signedDisplacement) < 0.02f)
            {
                continue;
            }

            int sign = Math.Sign(signedDisplacement);
            if (previousSign != 0 && sign != previousSign)
            {
                reversals++;
            }
            previousSign = sign;
        }

        Assert.Equal(ridges, reversals);
    }

    [Fact]
    public void ZigZagExtremeAmountRemainsFiniteAndFoldoverFree()
    {
        const int size = 201;
        const int center = size / 2;
        PrismPremultipliedColor[] result =
            ApplyZigZag(
                size,
                size,
                amount: float.MaxValue,
                ridges: 5,
                style: "OutFromCenter");
        double previousMappedX = center;
        for (int x = center + 1; x <= center + 75; x++)
        {
            PrismPremultipliedColor sample =
                result[(center * size) + x];
            AssertFiniteAssociated(sample);
            double mappedX = sample.Red * (size - 1);
            Assert.True(
                mappedX > previousMappedX,
                $"Expected a monotonic radial map at x={x}.");
            previousMappedX = mappedX;
        }
    }

    [Fact]
    public void TransformMapsNegativeCoordinatesAndEveryEdgeMode()
    {
        PrismPremultipliedColor red =
            PrismPremultipliedColor.FromStraight(
                1,
                0,
                0,
                1);
        PrismPremultipliedColor green =
            PrismPremultipliedColor.FromStraight(
                0,
                1,
                0,
                0.5);
        PrismPremultipliedColor blue =
            PrismPremultipliedColor.FromStraight(
                0,
                0,
                1,
                1);
        PrismPremultipliedColor[] source =
            [red, green, blue];

        PrismPremultipliedColor[] transparent =
            ApplyTransform(
                source,
                translateX: 1,
                edgeMode: "Transparent");
        PrismPremultipliedColor[] clamp =
            ApplyTransform(
                source,
                translateX: 1,
                edgeMode: "Clamp");
        PrismPremultipliedColor[] wrap =
            ApplyTransform(
                source,
                translateX: 1,
                edgeMode: "Wrap");
        PrismPremultipliedColor[] mirror =
            ApplyTransform(
                source,
                translateX: 1,
                edgeMode: "Mirror");
        PrismPremultipliedColor[] negative =
            ApplyTransform(
                source,
                translateX: -1,
                edgeMode: "Transparent");

        Assert.Equal(0, transparent[0].Alpha);
        AssertColor(transparent[1], red);
        AssertColor(transparent[2], green);
        AssertColor(clamp[0], red);
        AssertColor(wrap[0], blue);
        Assert.True(mirror[0].Alpha > 0);
        Assert.Equal(0, negative[^1].Alpha);
        Assert.All(
            transparent
                .Concat(clamp)
                .Concat(wrap)
                .Concat(mirror)
                .Concat(negative),
            AssertFiniteAssociated);
    }

    [Fact]
    public void ExtremeScaleRotationAndTransparencyStayFiniteAssociated()
    {
        PrismPremultipliedColor[] source =
        [
            default,
            PrismPremultipliedColor.FromStraight(
                0.9,
                0.2,
                0.1,
                0.25),
            PrismPremultipliedColor.FromStraight(
                0.1,
                0.8,
                0.3,
                0.75),
            PrismPremultipliedColor.FromStraight(
                0.2,
                0.4,
                0.9,
                1)
        ];

        foreach (float scale in new[] { 0.001f, 1_000f })
        {
            PrismResamplingPlan plan = CreatePlan(
                PrismFilterId.Transform,
                new DrawRect(0, 0, 4, 1),
                (state, entry) =>
                {
                    SetVector(
                        state,
                        entry,
                        "Scale",
                        new Vector4(
                            scale,
                            scale,
                            0,
                            0));
                    SetNumber(
                        state,
                        entry,
                        "Rotation",
                        179.99f);
                    SetVector(
                        state,
                        entry,
                        "Skew",
                        new Vector4(
                            45,
                            -45,
                            0,
                            0));
                    SetSymbol(
                        state,
                        entry,
                        "EdgeMode",
                        "Transparent");
                });

            PrismPremultipliedColor[] result =
                PrismResamplingMath.Apply(
                    plan,
                    source,
                    4,
                    1,
                    PrismColorProfile.LinearSrgb);

            Assert.All(
                result,
                AssertFiniteAssociated);
        }
    }

    [Fact]
    public void TransformMinificationUsesMipmappedFootprint()
    {
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[16 * 16];
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                double channel = (x + y) % 2;
                source[(y * 16) + x] =
                    PrismPremultipliedColor.FromStraight(
                        channel,
                        channel,
                        channel,
                        1);
            }
        }

        PrismResamplingPlan plan = CreatePlan(
            PrismFilterId.Transform,
            new DrawRect(0, 0, 16, 16),
            (state, entry) => SetVector(
                state,
                entry,
                "Scale",
                new Vector4(0.25f, 0.25f, 0, 0)));

        PrismPremultipliedColor[] result =
            PrismResamplingMath.Apply(
                plan,
                source,
                16,
                16,
                PrismColorProfile.LinearSrgb);

        PrismPremultipliedColor center = result[(8 * 16) + 8];
        Assert.InRange(center.Red, 0.49, 0.51);
        Assert.InRange(center.Green, 0.49, 0.51);
        Assert.InRange(center.Blue, 0.49, 0.51);
        Assert.Equal(1, center.Alpha);
    }

    [Fact]
    public void PolarCoordinatesUsesPixelSpaceRadiusOnWideSurfaces()
    {
        const int width = 9;
        const int height = 5;
        PrismResamplingPlan plan = CreatePlan(
            PrismFilterId.PolarCoordinates,
            new DrawRect(0, 0, width, height),
            (state, entry) => SetSymbol(
                state,
                entry,
                "Mode",
                "PolarToRectangular"));

        PrismPremultipliedColor[] result =
            PrismResamplingMath.Apply(
                plan,
                CreateCoordinateGradient(width, height),
                width,
                height,
                PrismColorProfile.LinearSrgb);

        PrismPremultipliedColor horizontal =
            result[(2 * width) + 6];
        PrismPremultipliedColor vertical =
            result[(4 * width) + 4];
        Assert.InRange(
            Math.Abs(horizontal.Green - vertical.Green),
            0,
            0.00001);
        Assert.All(result, AssertFiniteAssociated);
    }

    [Fact]
    public void RectangularToPolarTreatsRadialOverflowAsTransparent()
    {
        const int width = 9;
        const int height = 5;
        PrismResamplingPlan plan = CreatePlan(
            PrismFilterId.PolarCoordinates,
            new DrawRect(0, 0, width, height));
        PrismPremultipliedColor opaque =
            PrismPremultipliedColor.FromStraight(
                1,
                0.25,
                0.5,
                1);

        PrismPremultipliedColor[] result =
            PrismResamplingMath.Apply(
                plan,
                Enumerable.Repeat(
                    opaque,
                    width * height)
                    .ToArray(),
                width,
                height,
                PrismColorProfile.LinearSrgb);

        Assert.InRange(
            result[(4 * width) + 4].Alpha,
            0,
            0.5);
        Assert.All(result, AssertFiniteAssociated);
    }

    [Fact]
    public void PolarCoordinatesMinificationUsesAnEllipticalFootprint()
    {
        const int size = 64;
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                double channel = (x + y) % 2;
                source[(y * size) + x] =
                    PrismPremultipliedColor.FromStraight(
                        channel,
                        channel,
                        channel,
                        1);
            }
        }
        PrismResamplingPlan plan = CreatePlan(
            PrismFilterId.PolarCoordinates,
            new DrawRect(0, 0, size, size));

        PrismPremultipliedColor[] result =
            PrismResamplingMath.Apply(
                plan,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);

        PrismPremultipliedColor minified =
            result[(33 * size) + 1];
        Assert.InRange(minified.Red, 0.3, 0.7);
        Assert.InRange(minified.Green, 0.3, 0.7);
        Assert.InRange(minified.Blue, 0.3, 0.7);
        Assert.Equal(1, minified.Alpha, 6);
        Assert.All(result, AssertFiniteAssociated);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(180, 4)]
    [InlineData(1440, 8)]
    public void TwirlUsesFixedOneFourOrEightTapFootprints(
        float angleDegrees,
        int expectedTapCount)
    {
        Vector4 options = new(
            angleDegrees * (MathF.PI / 180f),
            0.5f,
            0.5f,
            0);

        int tapCount =
            PrismResamplingMath.TwirlTapCount(
                options,
                new Vector2(0.75f, 0.5f),
                64,
                64);

        Assert.Equal(expectedTapCount, tapCount);
        Assert.Contains(tapCount, new[] { 1, 4, 8 });
    }

    [Fact]
    public void TwirlAnisotropicFootprintSuppressesCheckerboardAliasing()
    {
        const int size = 64;
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                double channel = (x + y) % 2;
                source[(y * size) + x] =
                    PrismPremultipliedColor.FromStraight(
                        channel,
                        channel,
                        channel,
                        1);
            }
        }
        PrismResamplingPlan plan = CreatePlan(
            PrismFilterId.Twirl,
            new DrawRect(0, 0, size, size),
            (state, entry) =>
                SetNumber(state, entry, "Angle", 1440));

        PrismPremultipliedColor[] result =
            PrismResamplingMath.Apply(
                plan,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);

        double meanDeviation = 0;
        int sampleCount = 0;
        for (int y = 8; y < size - 8; y++)
        {
            for (int x = 8; x < size - 8; x++)
            {
                float radius = Vector2.Distance(
                    new Vector2(
                        (x + 0.5f) / size,
                        (y + 0.5f) / size),
                    new Vector2(0.5f));
                if (radius is < 0.18f or > 0.32f)
                {
                    continue;
                }

                meanDeviation += Math.Abs(
                    result[(y * size) + x].Red - 0.5);
                sampleCount++;
            }
        }

        Assert.True(sampleCount > 0);
        Assert.InRange(
            meanDeviation / sampleCount,
            0,
            0.2);
        Assert.All(result, AssertFiniteAssociated);
    }

    [Fact]
    public void CalibratedAdaptiveWideAngleUsesNoAuxiliaryResource()
    {
        PrismResamplingPlan adaptive =
            CreatePlan(
                PrismFilterId.AdaptiveWideAngle,
                configure: (state, entry) =>
                {
                    SetVector(
                        state,
                        entry,
                        "FocalLength",
                        new Vector4(0.6f, 0.55f, 0, 0));
                    SetVector(
                        state,
                        entry,
                        "PrincipalPoint",
                        new Vector4(0.48f, 0.52f, 0, 0));
                    SetVector(
                        state,
                        entry,
                        "DistortionCoefficients",
                        new Vector4(0.12f, -0.04f, 0.01f, 0));
                });
        PrismResamplingPlan displace =
            CreatePlan(PrismFilterId.Displace);
        PrismResamplingPlan liquify =
            CreatePlan(PrismFilterId.Liquify);
        PrismPremultipliedColor[] source =
        [
            PrismPremultipliedColor.FromStraight(
                0.4,
                0.5,
                0.6,
                1)
        ];

        Assert.False(adaptive.PrimaryResourceRequired);
        Assert.True(displace.PrimaryResourceRequired);
        Assert.True(liquify.PrimaryResourceRequired);
        Assert.False(liquify.AuxiliaryResourceRequired);
        Assert.True(displace.PrimaryResource.Value > 0);
        Assert.True(liquify.PrimaryResource.Value > 0);
        Assert.Equal(
            new Vector4(0.6f, 0.55f, 0.48f, 0.52f),
            adaptive.Options0);
        Assert.Equal(
            new Vector4(0.12f, -0.04f, 0.01f, 0),
            adaptive.Options1);
        Assert.Throws<InvalidOperationException>(
            () => PrismResamplingMath.Apply(
                displace,
                source,
                1,
                1,
                PrismColorProfile.LinearSrgb));

        PrismPremultipliedColor[] result =
            PrismResamplingMath.Apply(
                liquify,
                source,
                1,
                1,
                PrismColorProfile.LinearSrgb,
                primaryResource:
                    _ => new Vector4(
                        0.5f,
                        0.5f,
                        0,
                        1));
        AssertColor(result[0], source[0]);
    }

    [Fact]
    public void LiquifyUsesBicubicSamplingForSafeDisplacement()
    {
        const int width = 7;
        PrismPremultipliedColor[] source =
        [
            Gray(0),
            Gray(0.1),
            Gray(0.4),
            Gray(0.9),
            Gray(0.2),
            Gray(0.05),
            Gray(0)
        ];
        PrismResamplingPlan plan = CreatePlan(
            PrismFilterId.Liquify,
            new DrawRect(0, 0, width, 1));

        PrismPremultipliedColor[] result =
            PrismResamplingMath.Apply(
                plan,
                source,
                width,
                1,
                PrismColorProfile.LinearSrgb,
                primaryResource:
                    _ => new Vector4(
                        0.5f - (0.25f / width),
                        0.5f,
                        0,
                        1));

        Assert.Equal(0.74375, result[2].Red, 5);
        Assert.All(result, AssertFiniteAssociated);
    }

    [Fact]
    public void LiquifyFallsBackToBilinearForFoldedMappings()
    {
        const int width = 7;
        PrismPremultipliedColor[] source =
        [
            Gray(0),
            Gray(0.1),
            Gray(0.4),
            Gray(0.9),
            Gray(0.2),
            Gray(0.05),
            Gray(0)
        ];
        PrismResamplingPlan plan = CreatePlan(
            PrismFilterId.Liquify,
            new DrawRect(0, 0, width, 1));

        PrismPremultipliedColor[] result =
            PrismResamplingMath.Apply(
                plan,
                source,
                width,
                1,
                PrismColorProfile.LinearSrgb,
                primaryResource:
                    uv => new Vector4(
                        uv.X - (0.25f / width),
                        0.5f,
                        0,
                        1));

        Assert.Equal(0.125, result[2].Red, 5);
        Assert.All(result, AssertFiniteAssociated);
    }

    [Fact]
    public void DisplaceUsesEncodedChannelsCenteredScaleAndNativeTiling()
    {
        PrismResamplingPlan alphaPlan = CreatePlan(
            PrismFilterId.Displace,
            new DrawRect(0, 0, 4, 1),
            (state, entry) =>
            {
                SetNumber(state, entry, "HorizontalScale", 4);
                SetSymbol(state, entry, "ChannelX", "Alpha");
            });
        PrismPremultipliedColor[] source =
        [
            PrismPremultipliedColor.FromStraight(0.1, 0, 0, 1),
            PrismPremultipliedColor.FromStraight(0.3, 0, 0, 1),
            PrismPremultipliedColor.FromStraight(0.6, 0, 0, 1),
            PrismPremultipliedColor.FromStraight(0.9, 0, 0, 1)
        ];

        PrismPremultipliedColor[] centered =
            PrismResamplingMath.Apply(
                alphaPlan,
                source,
                4,
                1,
                PrismColorProfile.LinearSrgb,
                primaryResource: _ => new Vector4(0, 0, 0, 0.5f));
        for (int index = 0; index < source.Length; index++)
        {
            AssertColor(centered[index], source[index]);
        }

        PrismPremultipliedColor[] shifted =
            PrismResamplingMath.Apply(
                alphaPlan,
                source,
                4,
                1,
                PrismColorProfile.LinearSrgb,
                primaryResource: _ => new Vector4(0, 0, 0, 1));
        AssertColor(shifted[3], source[1]);

        PrismResamplingPlan tilePlan = CreatePlan(
            PrismFilterId.Displace,
            new DrawRect(0, 0, 4, 1),
            (state, entry) =>
            {
                SetNumber(state, entry, "HorizontalScale", 1);
                SetSymbol(state, entry, "MapFit", "Tile");
            });
        List<Vector2> mapCoordinates = [];
        PrismResamplingMath.Apply(
            tilePlan,
            source,
            4,
            1,
            PrismColorProfile.LinearSrgb,
            primaryResource: coordinate =>
            {
                mapCoordinates.Add(coordinate);
                return new Vector4(0.5f, 0.5f, 0, 1);
            },
            primaryResourceSize: new Vector2(2, 1));

        Assert.Equal(
            [0.25f, 0.75f, 0.25f, 0.75f],
            mapCoordinates.Select(coordinate => coordinate.X));
    }

    [Fact]
    public void DisplaceConservativelyExpandsVisualBoundsByHalfScale()
    {
        DrawRect sourceBounds = new(-8, -4, 20, 10);
        PrismGraph graph = CreateGraph(
            PrismFilterId.Displace,
            sourceBounds,
            (state, entry) =>
            {
                SetNumber(state, entry, "HorizontalScale", -8);
                SetNumber(state, entry, "VerticalScale", 6);
            });
        PrismGraphNode filter = graph.Nodes.Single(
            node => node.Kind == PrismGraphNodeKind.Filter);
        PrismResamplingPlan prepared = Assert.IsType<PrismResamplingPlan>(
            filter.ResamplingPlan);
        PrismGraphExecutionPlan execution =
            new PrismGraphOptimizer().Optimize(graph);

        Assert.Equal(new Vector2(4, 3), prepared.BoundsOutset);
        Assert.False(prepared.TransformsBounds);
        Assert.Equal(
            new DrawRect(-12, -7, 28, 16),
            execution.GetNodePlan(filter.Id).Bounds);
        Assert.Equal(
            PrismGraphBoundsStatus.Conservative,
            execution.GetNodePlan(filter.Id).BoundsStatus);
    }

    [Fact]
    public void GlassUsesDistinctScalarSurfacesSmoothnessAndConservativeBounds()
    {
        const int width = 32;
        const int height = 24;
        PrismPremultipliedColor[] source =
            CreateCoordinateGradient(width, height);
        PrismPremultipliedColor[][] proceduralResults =
            new[] { "Frosted", "TinyLens", "Blocks", "Canvas" }
                .Select(texture =>
                {
                    PrismResamplingPlan plan = CreatePlan(
                        PrismFilterId.Glass,
                        new DrawRect(0, 0, width, height),
                        (state, entry) =>
                        {
                            SetNumber(state, entry, "Distortion", 1);
                            SetNumber(state, entry, "Smoothness", 0.25f);
                            SetNumber(state, entry, "Scaling", 1);
                            SetSymbol(state, entry, "Texture", texture);
                        });
                    return PrismResamplingMath.Apply(
                        plan,
                        source,
                        width,
                        height,
                        PrismColorProfile.LinearSrgb,
                        primaryResource:
                            _ => new Vector4(1, 0, 0, 1));
                })
                .ToArray();

        for (int first = 0;
            first < proceduralResults.Length;
            first++)
        {
            for (int second = first + 1;
                second < proceduralResults.Length;
                second++)
            {
                Assert.False(
                    proceduralResults[first].SequenceEqual(
                        proceduralResults[second]));
            }
        }

        PrismResamplingPlan texturePlan = CreatePlan(
            PrismFilterId.Glass,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "Distortion", 1);
                SetNumber(state, entry, "Smoothness", 0);
                SetSymbol(state, entry, "Texture", "TextureImage");
            });
        PrismPremultipliedColor[] flatTexture =
            PrismResamplingMath.Apply(
                texturePlan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb,
                primaryResource:
                    _ => new Vector4(0.8f, 0.8f, 0.8f, 1));
        for (int index = 0; index < source.Length; index++)
        {
            AssertColor(
                flatTexture[index],
                source[index],
                tolerance: 0.000001);
        }

        PrismResamplingPlan smoothPlan = texturePlan with
        {
            Options0 = texturePlan.Options0 with
            {
                Y = 1
            }
        };
        Func<Vector2, Vector4> texturedSurface = coordinate =>
        {
            float value = Math.Clamp(
                0.5f +
                    (0.25f * MathF.Sin(
                        coordinate.X * width * 1.7f)) +
                    (0.25f * MathF.Cos(
                        coordinate.Y * height * 0.9f)),
                0,
                1);
            return new Vector4(value, value, value, 1);
        };
        PrismPremultipliedColor[] rough =
            PrismResamplingMath.Apply(
                texturePlan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb,
                primaryResource: texturedSurface);
        PrismPremultipliedColor[] smooth =
            PrismResamplingMath.Apply(
                smoothPlan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb,
                primaryResource: texturedSurface);
        Assert.False(rough.SequenceEqual(smooth));

        Assert.Equal(
            new Vector2(10, 10),
            texturePlan.BoundsOutset);
        Assert.All(
            proceduralResults
                .SelectMany(result => result)
                .Concat(flatTexture)
                .Concat(rough)
                .Concat(smooth),
            AssertFiniteAssociated);
    }

    [Fact]
    public void PinchUsesFiniteEllipticalSupportAndHandlesExtremeAmounts()
    {
        const int width = 9;
        const int height = 5;
        PrismPremultipliedColor[] source =
            CreateCoordinateGradient(width, height);
        PrismResamplingPlan plan = CreatePlan(
            PrismFilterId.Pinch,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
                SetNumber(state, entry, "Amount", 0.75f));

        PrismPremultipliedColor[] result =
            PrismResamplingMath.Apply(
                plan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        AssertColor(result[0], source[0]);
        AssertColor(result[width - 1], source[width - 1]);
        AssertColor(
            result[(height - 1) * width],
            source[(height - 1) * width]);
        AssertColor(result[^1], source[^1]);
        AssertColor(
            result[(height / 2 * width) + (width / 2)],
            source[(height / 2 * width) + (width / 2)]);
        Assert.NotEqual(source[width / 2], result[width / 2]);

        foreach (float amount in new[] { float.MaxValue, -float.MaxValue })
        {
            PrismResamplingPlan extremePlan = CreatePlan(
                PrismFilterId.Pinch,
                new DrawRect(0, 0, width, height),
                (state, entry) =>
                    SetNumber(state, entry, "Amount", amount));
            PrismPremultipliedColor[] extreme =
                PrismResamplingMath.Apply(
                    extremePlan,
                    source,
                    width,
                    height,
                    PrismColorProfile.LinearSrgb);

            Assert.All(extreme, AssertFiniteAssociated);
        }
    }

    [Theory]
    [InlineData("Small", 8)]
    [InlineData("Medium", 16)]
    [InlineData("Large", 32)]
    public void RippleSizeUsesStableDevicePixelWavelength(
        string size,
        float expectedWavelength)
    {
        PrismResamplingPlan plan = CreatePlan(
            PrismFilterId.Ripple,
            new DrawRect(0, 0, 64, 64),
            (state, entry) =>
            {
                SetNumber(state, entry, "Amount", 4);
                SetSymbol(state, entry, "Size", size);
                SetInteger(state, entry, "Seed", 1729);
            });

        Assert.Equal(expectedWavelength, plan.Options0.Y);
    }

    [Fact]
    public void RippleUsesContinuousSeededPhaseIndependentOfImageHeight()
    {
        const int width = 64;
        const int shortHeight = 48;
        const int tallHeight = 96;
        const int sampleX = width / 2;
        PrismPremultipliedColor[] shortSource =
            CreateCoordinateGradient(width, shortHeight);
        PrismPremultipliedColor[] tallSource =
            CreateCoordinateGradient(width, tallHeight);
        PrismResamplingPlan shortPlan = CreatePlan(
            PrismFilterId.Ripple,
            new DrawRect(0, 0, width, shortHeight),
            Configure);
        PrismResamplingPlan tallPlan = CreatePlan(
            PrismFilterId.Ripple,
            new DrawRect(0, 0, width, tallHeight),
            Configure);
        PrismResamplingPlan alternateSeedPlan = CreatePlan(
            PrismFilterId.Ripple,
            new DrawRect(0, 0, width, shortHeight),
            (state, entry) =>
            {
                Configure(state, entry);
                SetInteger(state, entry, "Seed", 1730);
            });

        PrismPremultipliedColor[] shortResult =
            PrismResamplingMath.Apply(
                shortPlan,
                shortSource,
                width,
                shortHeight,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] repeated =
            PrismResamplingMath.Apply(
                shortPlan,
                shortSource,
                width,
                shortHeight,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] tallResult =
            PrismResamplingMath.Apply(
                tallPlan,
                tallSource,
                width,
                tallHeight,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] alternateSeedResult =
            PrismResamplingMath.Apply(
                alternateSeedPlan,
                shortSource,
                width,
                shortHeight,
                PrismColorProfile.LinearSrgb);

        Assert.Equal(shortResult, repeated);
        Assert.False(shortResult.SequenceEqual(alternateSeedResult));

        float maximumNeighborChange = 0;
        for (int y = 8; y < shortHeight - 8; y++)
        {
            float shortDisplacement =
                HorizontalDisplacement(shortResult, y);
            float tallDisplacement =
                HorizontalDisplacement(tallResult, y);
            float nextDisplacement =
                HorizontalDisplacement(shortResult, y + 1);

            Assert.InRange(
                MathF.Abs(shortDisplacement - tallDisplacement),
                0,
                0.001f);
            maximumNeighborChange = MathF.Max(
                maximumNeighborChange,
                MathF.Abs(shortDisplacement - nextDisplacement));
        }

        Assert.InRange(maximumNeighborChange, 0, 3.5f);
        Assert.All(shortResult, AssertFiniteAssociated);

        void Configure(
            PrismFilterState state,
            PrismCatalogEntryDescriptor entry)
        {
            SetNumber(state, entry, "Amount", 4);
            SetSymbol(state, entry, "Size", "Medium");
            SetInteger(state, entry, "Seed", 1729);
            SetSymbol(state, entry, "EdgeMode", "Clamp");
        }

        float HorizontalDisplacement(
            PrismPremultipliedColor[] pixels,
            int y)
        {
            PrismPremultipliedColor pixel =
                pixels[(y * width) + sampleX];
            return ((float)pixel.Red * (width - 1)) - sampleX;
        }
    }

    [Fact]
    public void OceanRippleUsesContinuousSeededTwoDimensionalDomainWarp()
    {
        const int width = 64;
        const int height = 64;
        PrismPremultipliedColor[] source =
            CreateCoordinateGradient(width, height);
        PrismResamplingPlan firstPlan = CreatePlan(
            PrismFilterId.OceanRipple,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "RippleSize", 12);
                SetNumber(state, entry, "RippleMagnitude", 6);
                SetInteger(state, entry, "Seed", 1729);
            });
        PrismResamplingPlan secondPlan = CreatePlan(
            PrismFilterId.OceanRipple,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "RippleSize", 12);
                SetNumber(state, entry, "RippleMagnitude", 6);
                SetInteger(state, entry, "Seed", 1730);
            });

        PrismPremultipliedColor[] first =
            PrismResamplingMath.Apply(
                firstPlan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] repeated =
            PrismResamplingMath.Apply(
                firstPlan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] second =
            PrismResamplingMath.Apply(
                secondPlan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        Assert.Equal(first, repeated);
        Assert.False(first.SequenceEqual(second));

        float maximumNeighborChange = 0;
        float verticalInfluenceOnX = 0;
        float horizontalInfluenceOnY = 0;
        int comparisons = 0;
        for (int y = 12; y < height - 12; y++)
        {
            for (int x = 12; x < width - 12; x++)
            {
                Vector2 displacement = Displacement(first, x, y);
                Vector2 right = Displacement(first, x + 1, y);
                Vector2 below = Displacement(first, x, y + 1);
                maximumNeighborChange = MathF.Max(
                    maximumNeighborChange,
                    MathF.Max(
                        Vector2.Distance(displacement, right),
                        Vector2.Distance(displacement, below)));
                verticalInfluenceOnX +=
                    MathF.Abs(displacement.X - below.X);
                horizontalInfluenceOnY +=
                    MathF.Abs(displacement.Y - right.Y);
                comparisons++;
            }
        }

        Assert.InRange(maximumNeighborChange, 0, 5);
        Assert.True(verticalInfluenceOnX / comparisons > 0.01f);
        Assert.True(horizontalInfluenceOnY / comparisons > 0.01f);
        Assert.All(first, AssertFiniteAssociated);

        Vector2 Displacement(
            PrismPremultipliedColor[] pixels,
            int x,
            int y)
        {
            PrismPremultipliedColor pixel =
                pixels[(y * width) + x];
            return new Vector2(
                ((float)pixel.Red * (width - 1)) - x,
                ((float)pixel.Green * (height - 1)) - y);
        }
    }

    [Fact]
    public void OceanRippleZeroMagnitudeIsAnExactNoOp()
    {
        const int width = 17;
        const int height = 13;
        PrismPremultipliedColor[] source =
            CreateCoordinateGradient(width, height);
        PrismResamplingPlan plan = CreatePlan(
            PrismFilterId.OceanRipple,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "RippleSize", 0);
                SetNumber(state, entry, "RippleMagnitude", 0);
                SetInteger(state, entry, "Seed", int.MaxValue);
            });

        PrismPremultipliedColor[] result =
            PrismResamplingMath.Apply(
                plan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        Assert.True(plan.Passes[0].IsNoOp);
        Assert.Equal(source, result);
    }

    [Fact]
    public void AdaptiveWideAngleUsesCalibratedFisheyeInverseMapping()
    {
        PrismResamplingPlan plan = CreatePlan(
            PrismFilterId.AdaptiveWideAngle,
            new DrawRect(0, 0, 5, 1),
            (state, entry) =>
            {
                SetVector(
                    state,
                    entry,
                    "FocalLength",
                    new Vector4(0.5f, 0.5f, 0, 0));
                SetVector(
                    state,
                    entry,
                    "PrincipalPoint",
                    new Vector4(0.5f, 0.5f, 0, 0));
                SetVector(
                    state,
                    entry,
                    "DistortionCoefficients",
                    new Vector4(5, 0, 0, 0));
            });
        PrismPremultipliedColor[] source =
        [
            PrismPremultipliedColor.FromStraight(1, 0, 0, 1),
            PrismPremultipliedColor.FromStraight(0, 1, 0, 0.5),
            PrismPremultipliedColor.FromStraight(0, 0, 1, 1),
            PrismPremultipliedColor.FromStraight(1, 1, 0, 0.75),
            PrismPremultipliedColor.FromStraight(1, 0, 1, 1)
        ];

        PrismPremultipliedColor[] result =
            PrismResamplingMath.Apply(
                plan,
                source,
                5,
                1,
                PrismColorProfile.LinearSrgb);

        Assert.Equal(0, result[0].Alpha);
        AssertColor(result[2], source[2]);
        Assert.Equal(0, result[4].Alpha);
        Assert.All(result, AssertFiniteAssociated);
    }

    [Fact]
    public void AdaptiveWideAngleIdentityCalibrationPreservesAssociatedAlpha()
    {
        PrismResamplingPlan plan = CreatePlan(
            PrismFilterId.AdaptiveWideAngle,
            new DrawRect(0, 0, 3, 1),
            (state, entry) =>
            {
                SetVector(
                    state,
                    entry,
                    "FocalLength",
                    new Vector4(0.7f, 0.6f, 0, 0));
                SetVector(
                    state,
                    entry,
                    "PrincipalPoint",
                    new Vector4(0.45f, 0.55f, 0, 0));
                SetVector(
                    state,
                    entry,
                    "DistortionCoefficients",
                    Vector4.Zero);
            });
        PrismPremultipliedColor[] source =
        [
            default,
            PrismPremultipliedColor.FromStraight(0.6, 0.2, 0.9, 0.4),
            PrismPremultipliedColor.FromStraight(0.1, 0.7, 0.3, 1)
        ];

        PrismPremultipliedColor[] result =
            PrismResamplingMath.Apply(
                plan,
                source,
                3,
                1,
                PrismColorProfile.LinearSrgb);

        for (int index = 0; index < source.Length; index++)
        {
            AssertColor(result[index], source[index]);
        }
        Assert.All(result, AssertFiniteAssociated);
    }

    [Fact]
    public void LensCorrectionAppliesChannelSpecificChromaticCorrection()
    {
        PrismPremultipliedColor[] source = new PrismPremultipliedColor[81];
        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                source[(y * 9) + x] =
                    PrismPremultipliedColor.FromStraight(
                        x / 8d,
                        y / 8d,
                        (x + y) / 16d,
                        1);
            }
        }

        PrismResamplingPlan baseline = CreatePlan(
            PrismFilterId.LensCorrection,
            new DrawRect(0, 0, 9, 9),
            (state, entry) =>
            {
                SetNumber(state, entry, "Distortion", 0.15f);
                SetSymbol(state, entry, "EdgeMode", "Clamp");
            });
        PrismResamplingPlan chromatic = CreatePlan(
            PrismFilterId.LensCorrection,
            new DrawRect(0, 0, 9, 9),
            (state, entry) =>
            {
                SetNumber(state, entry, "Distortion", 0.15f);
                SetNumber(
                    state,
                    entry,
                    "ChromaticRedCyan",
                    6);
                SetNumber(
                    state,
                    entry,
                    "ChromaticBlueYellow",
                    -4);
                SetSymbol(state, entry, "EdgeMode", "Clamp");
            });

        PrismPremultipliedColor[] baselineResult =
            PrismResamplingMath.Apply(
                baseline,
                source,
                9,
                9,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] chromaticResult =
            PrismResamplingMath.Apply(
                chromatic,
                source,
                9,
                9,
                PrismColorProfile.LinearSrgb);

        Assert.NotEqual(
            baselineResult[(2 * 9) + 2],
            chromaticResult[(2 * 9) + 2]);
        Assert.All(chromaticResult, AssertFiniteAssociated);
    }

    [Fact]
    public void LensCorrectionAppliesVignettingWithoutChangingAlpha()
    {
        PrismPremultipliedColor input =
            PrismPremultipliedColor.FromStraight(
                0.8,
                0.6,
                0.4,
                0.7);
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(input, 81).ToArray();
        PrismResamplingPlan plan = CreatePlan(
            PrismFilterId.LensCorrection,
            new DrawRect(0, 0, 9, 9),
            (state, entry) =>
            {
                SetNumber(state, entry, "VignetteAmount", 0.6f);
                SetNumber(state, entry, "VignetteMidpoint", 0.2f);
                SetSymbol(state, entry, "EdgeMode", "Clamp");
            });

        PrismPremultipliedColor[] result =
            PrismResamplingMath.Apply(
                plan,
                source,
                9,
                9,
                PrismColorProfile.LinearSrgb);

        PrismPremultipliedColor center = result[(4 * 9) + 4];
        PrismPremultipliedColor corner = result[0];
        Assert.True(corner.Red < center.Red);
        Assert.True(corner.Green < center.Green);
        Assert.True(corner.Blue < center.Blue);
        Assert.Equal(input.Alpha, corner.Alpha, 6);
        Assert.Equal(input.Alpha, center.Alpha, 6);
        Assert.All(result, AssertFiniteAssociated);
    }

    [Fact]
    public void WavePlannerTreatsWavelengthAndAmplitudeAsIntervals()
    {
        PrismResamplingPlan plan = CreatePlan(
            PrismFilterId.Wave,
            new DrawRect(0, 0, 64, 32),
            (state, entry) =>
            {
                SetVector(
                    state,
                    entry,
                    "Wavelength",
                    new Vector4(40, 10, 0, 0));
                SetVector(
                    state,
                    entry,
                    "Amplitude",
                    new Vector4(12, 4, 0, 0));
            });

        Assert.Equal(10, plan.Options0.Y);
        Assert.Equal(40, plan.Options0.Z);
        Assert.Equal(4, plan.Options1.X);
        Assert.Equal(12, plan.Options1.Y);
    }

    [Fact]
    public void WaveGeneratorFieldIsSpatiallyCoherent()
    {
        const int width = 65;
        const int height = 33;
        PrismPremultipliedColor[] source =
            CreateCoordinateGradient(width, height);
        PrismResamplingPlan plan = CreatePlan(
            PrismFilterId.Wave,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "Generators", 1);
                SetVector(
                    state,
                    entry,
                    "Wavelength",
                    new Vector4(24, 24, 0, 0));
                SetVector(
                    state,
                    entry,
                    "Amplitude",
                    new Vector4(4, 4, 0, 0));
                SetVector(
                    state,
                    entry,
                    "Scale",
                    new Vector4(1, 0, 0, 0));
                SetInteger(state, entry, "Seed", 42);
            });

        PrismPremultipliedColor[] result =
            PrismResamplingMath.Apply(
                plan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        const int y = height / 2;
        double[] shifts = Enumerable
            .Range(8, width - 16)
            .Select(x =>
                result[(y * width) + x].Red -
                source[(y * width) + x].Red)
            .ToArray();
        double maximumStep = shifts
            .Zip(
                shifts.Skip(1),
                (left, right) =>
                    Math.Abs(right - left))
            .Max();

        Assert.InRange(maximumStep, 0, 0.02);
        Assert.All(result, AssertFiniteAssociated);
    }

    [Fact]
    public void WaveBandLimitsSquareGeneratorsAboveNyquist()
    {
        const int width = 33;
        const int height = 17;
        PrismPremultipliedColor[] source =
            CreateCoordinateGradient(width, height);
        PrismResamplingPlan plan = CreatePlan(
            PrismFilterId.Wave,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "Generators", 1);
                SetVector(
                    state,
                    entry,
                    "Wavelength",
                    new Vector4(1, 1, 0, 0));
                SetVector(
                    state,
                    entry,
                    "Amplitude",
                    new Vector4(8, 8, 0, 0));
                SetVector(
                    state,
                    entry,
                    "Scale",
                    new Vector4(1, 0, 0, 0));
                SetSymbol(state, entry, "Type", "Square");
                SetInteger(state, entry, "Seed", 17);
            });

        PrismPremultipliedColor[] result =
            PrismResamplingMath.Apply(
                plan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        for (int index = 0; index < source.Length; index++)
        {
            AssertColor(result[index], source[index]);
        }
    }

    [Fact]
    public void WaveSeedSelectsAStableGeneratorBankAndCountIsBounded()
    {
        const int width = 33;
        const int height = 17;
        PrismPremultipliedColor[] source =
            CreateCoordinateGradient(width, height);
        PrismResamplingPlan first = CreateWavePlan(
            seed: 41,
            generators: 5);
        PrismResamplingPlan repeated = CreateWavePlan(
            seed: 41,
            generators: 5);
        PrismResamplingPlan changed = CreateWavePlan(
            seed: 42,
            generators: 5);

        PrismPremultipliedColor[] firstResult =
            PrismResamplingMath.Apply(
                first,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] repeatedResult =
            PrismResamplingMath.Apply(
                repeated,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] changedResult =
            PrismResamplingMath.Apply(
                changed,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        Assert.Equal(firstResult, repeatedResult);
        Assert.NotEqual(firstResult, changedResult);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateWavePlan(
                seed: 41,
                generators:
                    PrismResamplingPlanner
                        .MaximumWaveGenerators +
                    1));

        PrismResamplingPlan CreateWavePlan(
            int seed,
            float generators) =>
            CreatePlan(
                PrismFilterId.Wave,
                new DrawRect(0, 0, width, height),
                (state, entry) =>
                {
                    SetNumber(
                        state,
                        entry,
                        "Generators",
                        generators);
                    SetVector(
                        state,
                        entry,
                        "Wavelength",
                        new Vector4(7, 23, 0, 0));
                    SetVector(
                        state,
                        entry,
                        "Amplitude",
                        new Vector4(2, 6, 0, 0));
                    SetInteger(state, entry, "Seed", seed);
                });
    }

    [Fact]
    public void WaveTypesProduceDistinctFiniteBanks()
    {
        const int width = 33;
        const int height = 17;
        PrismPremultipliedColor[] source =
            CreateCoordinateGradient(width, height);
        PrismPremultipliedColor[][] results =
            new[] { "Sine", "Triangle", "Square" }
                .Select(type =>
                {
                    PrismResamplingPlan plan = CreatePlan(
                        PrismFilterId.Wave,
                        new DrawRect(0, 0, width, height),
                        (state, entry) =>
                        {
                            SetNumber(
                                state,
                                entry,
                                "Generators",
                                3);
                            SetVector(
                                state,
                                entry,
                                "Wavelength",
                                new Vector4(
                                    8,
                                    20,
                                    0,
                                    0));
                            SetVector(
                                state,
                                entry,
                                "Amplitude",
                                new Vector4(
                                    2,
                                    5,
                                    0,
                                    0));
                            SetSymbol(
                                state,
                                entry,
                                "Type",
                                type);
                            SetInteger(
                                state,
                                entry,
                                "Seed",
                                91);
                        });
                    return PrismResamplingMath.Apply(
                        plan,
                        source,
                        width,
                        height,
                        PrismColorProfile.LinearSrgb);
                })
                .ToArray();

        Assert.NotEqual(results[0], results[1]);
        Assert.NotEqual(results[0], results[2]);
        Assert.NotEqual(results[1], results[2]);
        Assert.All(
            results.SelectMany(result => result),
            AssertFiniteAssociated);
    }

    [Fact]
    public void WaveUndefinedAreasDistinguishTransparentAndWrap()
    {
        const int width = 33;
        const int height = 17;
        PrismPremultipliedColor opaque =
            PrismPremultipliedColor.FromStraight(
                0.8,
                0.4,
                0.2,
                1);
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(
                opaque,
                width * height)
                .ToArray();
        PrismPremultipliedColor[] transparent =
            Apply("Transparent");
        PrismPremultipliedColor[] wrapped =
            Apply("WrapAround");

        Assert.Contains(
            transparent,
            pixel => pixel.Alpha < 0.999999);
        Assert.All(
            wrapped,
            pixel => Assert.Equal(1, pixel.Alpha, 6));
        Assert.All(transparent, AssertFiniteAssociated);
        Assert.All(wrapped, AssertFiniteAssociated);

        PrismPremultipliedColor[] Apply(string undefinedAreas)
        {
            PrismResamplingPlan plan = CreatePlan(
                PrismFilterId.Wave,
                new DrawRect(0, 0, width, height),
                (state, entry) =>
                {
                    SetNumber(
                        state,
                        entry,
                        "Generators",
                        1);
                    SetVector(
                        state,
                        entry,
                        "Wavelength",
                        new Vector4(12, 12, 0, 0));
                    SetVector(
                        state,
                        entry,
                        "Amplitude",
                        new Vector4(40, 40, 0, 0));
                    SetSymbol(
                        state,
                        entry,
                        "UndefinedAreas",
                        undefinedAreas);
                    SetInteger(
                        state,
                        entry,
                        "Seed",
                        7);
                });
            return PrismResamplingMath.Apply(
                plan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);
        }
    }

    [Fact]
    public void EveryDistortionIsDeterministicAndFiniteOnSmallImages()
    {
        PrismPremultipliedColor[] source =
        [
            default,
            PrismPremultipliedColor.FromStraight(
                0.9,
                0.1,
                0.2,
                0.4),
            PrismPremultipliedColor.FromStraight(
                0.2,
                0.8,
                0.3,
                1),
            PrismPremultipliedColor.FromStraight(
                0.7,
                0.6,
                0.1,
                0.8),
            PrismPremultipliedColor.FromStraight(
                0.3,
                0.5,
                0.9,
                0.6),
            PrismPremultipliedColor.FromStraight(
                0.1,
                0.2,
                0.4,
                1),
            default,
            PrismPremultipliedColor.FromStraight(
                0.4,
                0.2,
                0.7,
                0.5),
            PrismPremultipliedColor.FromStraight(
                0.8,
                0.8,
                0.8,
                1)
        ];
        Func<Vector2, Vector4> primary =
            uv => new Vector4(
                uv.X,
                uv.Y,
                (uv.X + uv.Y) * 0.5f,
                1);
        Func<Vector2, Vector4> auxiliary =
            uv => new Vector4(
                0,
                0,
                0,
                uv.X);

        foreach (PrismCatalogEntryDescriptor entry in
            DistortionEntries())
        {
            PrismResamplingPlan plan = CreatePlan(
                (PrismFilterId)entry.StableId,
                new DrawRect(0, 0, 3, 3));
            PrismPremultipliedColor[] first =
                PrismResamplingMath.Apply(
                    plan,
                    source,
                    3,
                    3,
                    PrismColorProfile.LinearSrgb,
                    primaryResource: primary,
                    auxiliaryResource: auxiliary);
            PrismPremultipliedColor[] repeated =
                PrismResamplingMath.Apply(
                    plan,
                    source,
                    3,
                    3,
                    PrismColorProfile.LinearSrgb,
                    primaryResource: primary,
                    auxiliaryResource: auxiliary);

            Assert.Equal(first, repeated);
            Assert.All(
                first,
                AssertFiniteAssociated);
        }
    }

    [Fact]
    public void DiffuseGlowExtractsHighlightsAndPreservesItsOriginalInput()
    {
        PrismGraph graph = CreateGraph(
            PrismFilterId.DiffuseGlow,
            new DrawRect(0, 0, 5, 3),
            (state, entry) =>
            {
                SetNumber(state, entry, "GlowAmount", 0.8f);
                SetNumber(state, entry, "ClearAmount", 0.75f);
                SetNumber(state, entry, "Grain", 0);
                SetColor(
                    state,
                    entry,
                    "Color",
                    new Color(0, 0, 255));
            });
        PrismGraphNode[] nodes = graph.Nodes
            .Where(node =>
                node.Filter == PrismFilterId.DiffuseGlow)
            .OrderBy(node => node.ResamplingPassIndex)
            .ToArray();
        PrismResamplingPlan plan = Assert.IsType<PrismResamplingPlan>(
            nodes[0].ResamplingPlan);
        PrismPremultipliedColor dark =
            PrismPremultipliedColor.FromStraight(
                0.1,
                0.1,
                0.1,
                1);
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(dark, 15).ToArray();
        source[(1 * 5) + 2] =
            PrismPremultipliedColor.FromStraight(
                1,
                1,
                1,
                1);

        PrismPremultipliedColor[] result =
            PrismResamplingMath.Apply(
                plan,
                source,
                5,
                3,
                PrismColorProfile.LinearSrgb);

        Assert.Equal(3, nodes.Length);
        Assert.Contains(
            graph.Edges,
            edge =>
                edge.Target == nodes[1].Id &&
                edge.Kind == PrismGraphEdgeKind.FilterOriginal);
        Assert.True(result[(1 * 5) + 1].Blue > dark.Blue);
        Assert.All(
            result,
            pixel => Assert.Equal(1, pixel.Alpha, 6));
        Assert.All(result, AssertFiniteAssociated);
    }

    [Fact]
    public void NeonGlowUsesFourPassPyramidAndPreservesItsOriginalInput()
    {
        PrismGraph graph = CreateGraph(
            PrismFilterId.NeonGlow,
            new DrawRect(0, 0, 32, 24));
        PrismGraphNode[] nodes = graph.Nodes
            .Where(node =>
                node.Filter == PrismFilterId.NeonGlow)
            .OrderBy(node => node.ResamplingPassIndex)
            .ToArray();
        PrismResamplingPlan plan = Assert.IsType<PrismResamplingPlan>(
            nodes[0].ResamplingPlan);
        PrismGraphExecutionPlan execution =
            new PrismGraphOptimizer().Optimize(graph);
        PrismPremultipliedColor black =
            PrismPremultipliedColor.FromStraight(0, 0, 0, 1);
        PrismPremultipliedColor white =
            PrismPremultipliedColor.FromStraight(1, 1, 1, 1);
        PrismPremultipliedColor[] source = new PrismPremultipliedColor[32 * 24];
        for (int y = 0; y < 24; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                source[(y * 32) + x] = x < 16
                    ? black
                    : white;
            }
        }

        PrismPremultipliedColor[] result =
            PrismResamplingMath.Apply(
                plan,
                source,
                32,
                24,
                PrismColorProfile.LinearSrgb);

        Assert.Equal(4, nodes.Length);
        Assert.Equal(
            [
                PrismResamplingPassKind.NeonEdgeExtract,
                PrismResamplingPassKind.NeonBlurHorizontal,
                PrismResamplingPassKind.NeonBlurVertical,
                PrismResamplingPassKind.NeonPyramidComposite
            ],
            plan.Passes.Select(pass => pass.Kind));
        Assert.Equal(PrismResamplingOperation.NeonGlow, plan.Operation);
        Assert.Equal(new Vector2(5), plan.BoundsOutset);
        Assert.All(
            nodes,
            node => Assert.Equal(
                new DrawRect(-5, -5, 42, 34),
                execution.GetNodePlan(node.Id).Bounds));
        Assert.True(result[(12 * 32) + 14].Blue > black.Blue);
        Assert.True(
            result[(12 * 32) + 14].Blue >
            result[(12 * 32) + 14].Red);
        Assert.True(
            result[(12 * 32) + 14].Blue >
            result[(12 * 32) + 2].Blue);
        Assert.All(
            result,
            pixel => Assert.Equal(1, pixel.Alpha, 6));
        Assert.All(result, AssertFiniteAssociated);
        Assert.All(
            nodes,
            node =>
            {
                Assert.IsType<PrismResamplingPlan>(
                    node.ResamplingPlan);
                Assert.Null(node.CatalogFilterPlan);
            });
        Assert.Contains(
            graph.Edges,
            edge =>
                edge.Target == nodes[^1].Id &&
                edge.Kind == PrismGraphEdgeKind.FilterOriginal);
    }

    [Fact]
    public void NeonGlowZeroSizeIsAnExactNoOp()
    {
        PrismResamplingPlan plan = CreatePlan(
            PrismFilterId.NeonGlow,
            new DrawRect(0, 0, 3, 3),
            (state, entry) =>
                SetNumber(state, entry, "GlowSize", 0));
        PrismPremultipliedColor[] source =
        [
            PrismPremultipliedColor.FromStraight(0, 0, 0, 1),
            PrismPremultipliedColor.FromStraight(1, 1, 1, 1),
            PrismPremultipliedColor.FromStraight(0, 0, 0, 1),
            PrismPremultipliedColor.FromStraight(1, 0, 0, 0.5),
            PrismPremultipliedColor.FromStraight(0, 1, 0, 0.5),
            PrismPremultipliedColor.FromStraight(0, 0, 1, 0.5),
            PrismPremultipliedColor.FromStraight(0.2, 0.3, 0.4, 1),
            PrismPremultipliedColor.FromStraight(0.8, 0.7, 0.6, 1),
            PrismPremultipliedColor.FromStraight(0, 0, 0, 0)
        ];

        PrismPremultipliedColor[] result =
            PrismResamplingMath.Apply(
                plan,
                source,
                3,
                3,
                PrismColorProfile.LinearSrgb);

        Assert.Equal(source, result);
        Assert.All(plan.Passes, pass => Assert.True(pass.IsNoOp));
    }

    [Fact]
    public void NeonGlowPropertiesControlMagnitudeSpreadAndHue()
    {
        const int width = 64;
        const int height = 16;
        PrismPremultipliedColor black =
            PrismPremultipliedColor.FromStraight(0, 0, 0, 1);
        PrismPremultipliedColor white =
            PrismPremultipliedColor.FromStraight(1, 1, 1, 1);
        PrismPremultipliedColor[] source = new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                source[(y * width) + x] = x < 32
                    ? black
                    : white;
            }
        }

        PrismResamplingPlan dim = CreatePlan(
            PrismFilterId.NeonGlow,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "GlowSize", 2);
                SetNumber(state, entry, "GlowBrightness", 1);
            });
        PrismResamplingPlan bright = CreatePlan(
            PrismFilterId.NeonGlow,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "GlowSize", 2);
                SetNumber(state, entry, "GlowBrightness", 8);
            });
        PrismResamplingPlan wideRed = CreatePlan(
            PrismFilterId.NeonGlow,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "GlowSize", 12);
                SetNumber(state, entry, "GlowBrightness", 8);
                SetColor(
                    state,
                    entry,
                    "GlowColor",
                    new Color(255, 0, 0));
            });

        PrismPremultipliedColor[] dimResult = Apply(dim);
        PrismPremultipliedColor[] brightResult = Apply(bright);
        PrismPremultipliedColor[] wideRedResult = Apply(wideRed);
        int nearEdge = (8 * width) + 30;
        int farFromEdge = (8 * width) + 22;

        Assert.True(brightResult[nearEdge].Blue > dimResult[nearEdge].Blue);
        Assert.True(wideRedResult[farFromEdge].Red > brightResult[farFromEdge].Red);
        Assert.True(wideRedResult[nearEdge].Red > wideRedResult[nearEdge].Blue);
        Assert.All(wideRedResult, AssertFiniteAssociated);

        PrismPremultipliedColor[] Apply(PrismResamplingPlan plan) =>
            PrismResamplingMath.Apply(
                plan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);
    }

    [Fact]
    public void NestedTransformsPreserveConstantPixelsAcrossWorkingProfiles()
    {
        PrismResamplingPlan firstPlan = CreatePlan(
            PrismFilterId.Transform,
            new DrawRect(0, 0, 3, 3),
            (state, entry) =>
            {
                SetVector(
                    state,
                    entry,
                    "Translate",
                    new Vector4(
                        -2,
                        1,
                        0,
                        0));
                SetNumber(
                    state,
                    entry,
                    "Rotation",
                    37);
                SetSymbol(
                    state,
                    entry,
                    "EdgeMode",
                    "Clamp");
            });
        PrismResamplingPlan secondPlan = CreatePlan(
            PrismFilterId.Transform,
            new DrawRect(0, 0, 3, 3),
            (state, entry) =>
            {
                SetVector(
                    state,
                    entry,
                    "Scale",
                    new Vector4(
                        1.5f,
                        0.75f,
                        0,
                        0));
                SetSymbol(
                    state,
                    entry,
                    "EdgeMode",
                    "Clamp");
            });
        PrismPremultipliedColor input =
            PrismPremultipliedColor.FromStraight(
                0.31,
                0.57,
                0.83,
                0.73);

        foreach (PrismColorProfile profile in
            Enum.GetValues<PrismColorProfile>())
        {
            PrismPremultipliedColor working =
                PrismColorPipeline.ConvertInputToWorking(
                    input,
                    profile);
            PrismPremultipliedColor[] source =
                Enumerable.Repeat(working, 9).ToArray();
            PrismPremultipliedColor[] first =
                PrismResamplingMath.Apply(
                    firstPlan,
                    source,
                    3,
                    3,
                    profile);
            PrismPremultipliedColor[] second =
                PrismResamplingMath.Apply(
                    secondPlan,
                    first,
                    3,
                    3,
                    profile);

            Assert.All(
                second,
                pixel => AssertColor(
                    pixel,
                    working,
                    tolerance: 0.00001));
        }
    }

    [Fact]
    public void TransformChangesOnlyVisualBoundsAndSurvivesMaskAndClipComposition()
    {
        DrawRect sourceBounds =
            new(-8, -4, 20, 10);
        PrismLayerDefinition baseLayer = new(
            new PrismNodeId(1),
            "Masked transform",
            filters:
            [
                new PrismFilterDefinition(
                    PrismFilterId.Transform)
            ],
            mask: new PrismMaskDefinition(
                new PrismResourceId(71)));
        PrismLayerDefinition clippedLayer = new(
            new PrismNodeId(2),
            "Clipped transform",
            filters:
            [
                new PrismFilterDefinition(
                    PrismFilterId.Transform)
            ],
            clipToBelow: true);
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "Transform composition",
                clippedLayer,
                baseLayer),
            bounds: sourceBounds);
        ConfigureTransform(
            scope.Instance
                .GetLayerState(baseLayer.Id)
                .Filters[0]);
        ConfigureTransform(
            scope.Instance
                .GetLayerState(clippedLayer.Id)
                .Filters[0]);
        PrismGraph graph = BuildGraph(scope);
        PrismGraphExecutionPlan plan =
            new PrismGraphOptimizer().Optimize(graph);

        Assert.All(
            graph.Scopes,
            graphScope =>
            {
                Assert.Equal(
                    sourceBounds,
                    graphScope.ControlBounds);
                Assert.Equal(
                    sourceBounds,
                    graphScope.Bounds);
            });
        Assert.Contains(
            graph.Nodes,
            node => node.Kind ==
                PrismGraphNodeKind.Mask);
        Assert.Contains(
            graph.Nodes,
            node => node.Kind ==
                PrismGraphNodeKind.ClipToBelow);
        PrismGraphNode[] filters = graph.Nodes
            .Where(node =>
                node.Kind ==
                    PrismGraphNodeKind.Filter)
            .ToArray();
        Assert.Equal(2, filters.Length);
        Assert.All(
            filters,
            node =>
            {
                Assert.IsType<PrismResamplingPlan>(
                    node.ResamplingPlan);
                int pixelInputs = graph.Edges.Count(edge =>
                    edge.Target == node.Id &&
                    edge.Kind is
                        PrismGraphEdgeKind.Content or
                        PrismGraphEdgeKind.Backdrop);
                Assert.Equal(1, pixelInputs);
                Assert.NotEqual(
                    sourceBounds,
                    plan.GetNodePlan(node.Id).Bounds);
            });

        void ConfigureTransform(
            PrismFilterState state)
        {
            PrismCatalogEntryDescriptor entry =
                PrismCatalogRuntime.GetEntry(
                    (int)PrismFilterId.Transform);
            SetVector(
                state,
                entry,
                "Translate",
                new Vector4(
                    3,
                    -2,
                    0,
                    0));
            SetVector(
                state,
                entry,
                "Scale",
                new Vector4(
                    1.25f,
                    0.75f,
                    0,
                    0));
            SetNumber(
                state,
                entry,
                "Rotation",
                15);
        }
    }

    private static PrismPremultipliedColor[] ApplyTransform(
        PrismPremultipliedColor[] source,
        float translateX,
        string edgeMode)
    {
        PrismResamplingPlan plan = CreatePlan(
            PrismFilterId.Transform,
            new DrawRect(
                0,
                0,
                source.Length,
                1),
            (state, entry) =>
            {
                SetVector(
                    state,
                    entry,
                    "Translate",
                    new Vector4(
                        translateX,
                        0,
                        0,
                        0));
                SetSymbol(
                    state,
                    entry,
                    "EdgeMode",
                    edgeMode);
            });
        return PrismResamplingMath.Apply(
            plan,
            source,
            source.Length,
            1,
            PrismColorProfile.LinearSrgb);
    }

    private static PrismPremultipliedColor[] ApplyZigZag(
        int width,
        int height,
        float amount,
        float ridges,
        string style)
    {
        PrismResamplingPlan plan = CreatePlan(
            PrismFilterId.ZigZag,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "Amount", amount);
                SetNumber(state, entry, "Ridges", ridges);
                SetSymbol(state, entry, "Style", style);
            });
        return PrismResamplingMath.Apply(
            plan,
            CreateCoordinateGradient(width, height),
            width,
            height,
            PrismColorProfile.LinearSrgb);
    }

    private static Vector2 CoordinateDisplacement(
        PrismPremultipliedColor sample,
        int x,
        int y,
        int width,
        int height) =>
        new(
            ((float)sample.Red * (width - 1)) - x,
            ((float)sample.Green * (height - 1)) - y);

    private static PrismResamplingPlan CreatePlan(
        PrismFilterId filter,
        DrawRect? bounds = null,
        Action<
            PrismFilterState,
            PrismCatalogEntryDescriptor>? configure = null)
    {
        PrismGraph graph = CreateGraph(
            filter,
            bounds ?? new DrawRect(
                0,
                0,
                20,
                10),
            configure);
        PrismGraphNode node = graph.Nodes.First(
            candidate =>
                candidate.Kind ==
                    PrismGraphNodeKind.Filter);
        return Assert.IsType<PrismResamplingPlan>(
            node.ResamplingPlan);
    }

    private static PrismGraph CreateGraph(
        PrismFilterId filter,
        DrawRect bounds,
        Action<
            PrismFilterState,
            PrismCatalogEntryDescriptor>? configure = null)
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            filter.ToString(),
            filters:
            [
                new PrismFilterDefinition(filter)
            ]);
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                $"Plan {filter}",
                layer),
            bounds: bounds);
        PrismFilterState state = Assert.Single(
            scope.Instance
                .GetLayerState(layer.Id)
                .Filters);
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)filter);
        ConfigureRequiredResources(state, entry);
        configure?.Invoke(state, entry);
        return BuildGraph(scope);
    }

    private static void ConfigureRequiredResources(
        PrismFilterState state,
        PrismCatalogEntryDescriptor entry)
    {
        foreach (PrismCatalogPropertyDescriptor property in
            entry.Properties.Where(property =>
                property.Required &&
                property.ValueType ==
                    PrismCatalogValueType.Resource))
        {
            GeneratedMarkup.SetPrismFilterResource(
                state,
                entry.StableId,
                property.TypeSlot,
                new PrismResourceId(
                    $"distortion-{entry.Symbol}-{property.Name}"));
        }
    }

    private static void SetNumber(
        PrismFilterState state,
        PrismCatalogEntryDescriptor entry,
        string name,
        float value) =>
        GeneratedMarkup.SetPrismFilterNumber(
            state,
            entry.StableId,
            Property(entry, name).TypeSlot,
            value);

    private static void SetInteger(
        PrismFilterState state,
        PrismCatalogEntryDescriptor entry,
        string name,
        int value) =>
        GeneratedMarkup.SetPrismFilterInteger(
            state,
            entry.StableId,
            Property(entry, name).TypeSlot,
            value);

    private static void SetVector(
        PrismFilterState state,
        PrismCatalogEntryDescriptor entry,
        string name,
        Vector4 value) =>
        GeneratedMarkup.SetPrismFilterVector(
            state,
            entry.StableId,
            Property(entry, name).TypeSlot,
            value);

    private static void SetColor(
        PrismFilterState state,
        PrismCatalogEntryDescriptor entry,
        string name,
        Color value) =>
        GeneratedMarkup.SetPrismFilterColor(
            state,
            entry.StableId,
            Property(entry, name).TypeSlot,
            value);

    private static void SetSymbol(
        PrismFilterState state,
        PrismCatalogEntryDescriptor entry,
        string name,
        string value) =>
        GeneratedMarkup.SetPrismFilterInteger(
            state,
            entry.StableId,
            Property(entry, name).TypeSlot,
            PrismCatalogRuntime.ResolveSymbol(
                name,
                value));

    private static PrismCatalogPropertyDescriptor Property(
        PrismCatalogEntryDescriptor entry,
        string name) =>
        entry.Properties.Single(property =>
            property.Name == name);

    private static PrismCatalogEntryDescriptor[]
        DistortionEntries() =>
        PrismCatalogGenerated.Entries
            .Where(entry =>
                entry.Kind == "filter" &&
                entry.Coverage.Test.StartsWith(
                    "PrismDistortionFilterTests/",
                    StringComparison.Ordinal))
            .ToArray();

    private static PrismGraph BuildGraph(
        PrismDrawScope scope)
    {
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(
                new DrawRect(0, 0, 20, 10),
                new Color(
                    255,
                    255,
                    255)),
            DrawCommand.EndPrism());
        return new PrismGraphBuilder().Build(
            new PrismFrameAnalyzer()
                .Analyze(commands));
    }

    private static void AssertColor(
        PrismPremultipliedColor actual,
        PrismPremultipliedColor expected,
        double tolerance = 0.000001)
    {
        Assert.InRange(
            Math.Abs(actual.Red - expected.Red),
            0,
            tolerance);
        Assert.InRange(
            Math.Abs(actual.Green - expected.Green),
            0,
            tolerance);
        Assert.InRange(
            Math.Abs(actual.Blue - expected.Blue),
            0,
            tolerance);
        Assert.InRange(
            Math.Abs(actual.Alpha - expected.Alpha),
            0,
            tolerance);
    }

    private static void AssertFiniteAssociated(
        PrismPremultipliedColor color)
    {
        Assert.True(double.IsFinite(color.Red));
        Assert.True(double.IsFinite(color.Green));
        Assert.True(double.IsFinite(color.Blue));
        Assert.True(double.IsFinite(color.Alpha));
        Assert.InRange(color.Alpha, 0, 1);
        Assert.InRange(color.Red, 0, color.Alpha);
        Assert.InRange(color.Green, 0, color.Alpha);
        Assert.InRange(color.Blue, 0, color.Alpha);
    }

    private static PrismPremultipliedColor Gray(double value) =>
        PrismPremultipliedColor.FromStraight(
            value,
            value,
            value,
            1);

    private static PrismPremultipliedColor[] CreateCoordinateGradient(
        int width,
        int height)
    {
        PrismPremultipliedColor[] pixels =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                pixels[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        x / (double)(width - 1),
                        y / (double)(height - 1),
                        (x + y) /
                            (double)(width + height - 2),
                        1);
            }
        }
        return pixels;
    }
}
