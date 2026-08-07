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

public sealed class PrismCatalogFilterTests
{
    [Fact]
    public void CatalogDrivesEveryRemainingPlannerKernelTestAndDocumentation()
    {
        PrismCatalogEntryDescriptor[] entries = CatalogEntries();
        PrismGraph graph = BuildAllGraph(entries);
        PrismGraphNode[] filterNodes = graph.Nodes
            .Where(node =>
                node.Kind == PrismGraphNodeKind.Filter)
            .ToArray();

        Assert.Equal(73, entries.Length);
        Assert.Equal(
            entries.Select(entry => entry.StableId),
            entries
                .Select(entry => entry.StableId)
                .Distinct());
        foreach (PrismCatalogEntryDescriptor entry in entries)
        {
            PrismFilterId filter =
                (PrismFilterId)entry.StableId;
            PrismGraphNode[] nodes = filterNodes
                .Where(node => node.Filter == filter)
                .OrderBy(node =>
                    node.CatalogFilterPassIndex)
                .ToArray();

            Assert.NotEmpty(nodes);
            Assert.True(
                PrismCatalogFilterPlanner.IsSupported(filter));
            Assert.StartsWith(
                "PrismGraphBuilder/CatalogEntry/",
                entry.Coverage.Planner,
                StringComparison.Ordinal);
            Assert.Equal(
                $"PrismKernelRegistry/{entry.Symbol}",
                entry.Coverage.Kernel);
            Assert.Equal(
                $"PrismCatalogFilterTests/{entry.Symbol}",
                entry.Coverage.Test);
            Assert.StartsWith(
                "generated:",
                entry.Coverage.Documentation,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "planned:",
                string.Join(
                    '|',
                    entry.Coverage.Runtime,
                    entry.Coverage.Planner,
                    entry.Coverage.Kernel,
                    entry.Coverage.Test,
                    entry.Coverage.Documentation),
                StringComparison.Ordinal);
            Assert.True(entry.Deterministic);
            Assert.True(entry.Cacheable);
            Assert.NotNull(entry.Execution);
            Assert.NotEmpty(
                Assert.IsType<PrismCatalogExecutionDescriptor>(
                    entry.Execution)
                    .Primitive);
            Assert.InRange(entry.Properties.Length, 0, 9);
            Assert.Equal(
                Enumerable.Range(0, entry.Properties.Length),
                entry.Properties.Select(property =>
                    property.Slot));

            PrismGraphNode first = nodes[0];
            PrismCatalogFilterPlan prepared =
                Assert.IsType<PrismCatalogFilterPlan>(
                    first.CatalogFilterPlan);
            Assert.Equal(filter, prepared.Filter);
            Assert.Equal(
                entry.Properties.Length,
                first.Parameters.Length);
            Assert.All(
                nodes,
                node =>
                {
                    PrismCatalogFilterPlan nodePlan =
                        Assert.IsType<PrismCatalogFilterPlan>(
                            node.CatalogFilterPlan);
                    Assert.Equal(prepared, nodePlan);
                    Assert.InRange(
                        node.CatalogFilterPassIndex,
                        0,
                        prepared.Passes.Length - 1);
                    Assert.Null(node.NeighborhoodPlan);
                    Assert.Null(node.ResamplingPlan);
                });

            AssertParameterPacking(
                entry,
                first.Parameters,
                prepared);
        }
    }

    [Fact]
    public void EveryCatalogFilterIsDeterministicAndKeepsAssociatedAlpha()
    {
        PrismCatalogEntryDescriptor[] entries = CatalogEntries();
        PrismGraph graph = BuildAllGraph(entries);
        PrismPremultipliedColor[] source = SampleImage();
        Func<Vector2, Vector4> primary = uv =>
            new Vector4(
                uv.X,
                1 - uv.Y,
                0.5f,
                1);
        Func<Vector2, Vector4> auxiliary = uv =>
            new Vector4(
                1 - uv.X,
                uv.Y,
                0.25f,
                1);
        PrismLensProfileResource lensProfile = TestLensProfile();
        PrismLightingResource lighting = TestLighting();

        foreach (PrismCatalogEntryDescriptor entry in entries)
        {
            PrismFilterId filter =
                (PrismFilterId)entry.StableId;
            PrismCatalogFilterPlan plan =
                Assert.IsType<PrismCatalogFilterPlan>(
                    graph.Nodes.First(node =>
                        node.Kind ==
                            PrismGraphNodeKind.Filter &&
                        node.Filter == filter)
                        .CatalogFilterPlan);

            PrismPremultipliedColor[] first =
                PrismCatalogFilterMath.Apply(
                    plan,
                    source,
                    4,
                    4,
                    PrismColorProfile.LinearSrgb,
                    primaryResource: primary,
                    auxiliaryResource: auxiliary,
                    lensProfile: lensProfile,
                    lightingResource: lighting);
            PrismPremultipliedColor[] second =
                PrismCatalogFilterMath.Apply(
                    plan,
                    source,
                    4,
                    4,
                    PrismColorProfile.LinearSrgb,
                    primaryResource: primary,
                    auxiliaryResource: auxiliary,
                    lensProfile: lensProfile,
                    lightingResource: lighting);

            Assert.Equal(first, second);
            Assert.All(first, AssertFiniteAssociated);
        }
    }

    [Fact]
    public void DiffusePublishesAllCoherenceEnhancingShockModes()
    {
        PrismCatalogParameterInfo mode = PrismCatalog
            .GetFilter(PrismFilterId.Diffuse)
            .Parameters
            .Single(parameter => parameter.Name == "Mode");

        Assert.Equal(
            ["Normal", "DarkenOnly", "LightenOnly", "Anisotropic"],
            mode.SymbolOptions);
    }

    [Fact]
    public void DiffusePlansExplicitIterationsAndTensorFootprint()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            PrismFilterId.Diffuse,
            new DrawRect(0, 0, 20, 10),
            (state, entry) =>
            {
                SetSymbol(state, entry, "Mode", "Normal");
                SetNumber(state, entry, "Iterations", 2);
                SetInteger(state, entry, "Seed", 17);
            });

        Assert.Equal(2, plan.Passes.Length);
        Assert.Equal([0, 1], plan.Passes.Select(pass => pass.Iteration));
        Assert.All(
            plan.Passes,
            pass =>
            {
                Assert.Equal(PrismCatalogFilterPassKind.Iteration, pass.Kind);
                Assert.Equal(1, pass.RadiusX);
                Assert.Equal(1, pass.RadiusY);
                Assert.Equal(2, pass.BoundsRadiusX);
                Assert.Equal(2, pass.BoundsRadiusY);
                Assert.False(pass.IsNoOp);
            });
    }

    [Fact]
    public void DiffuseRunsCoherentShockBranchesAndPreservesAlpha()
    {
        const int width = 19;
        const int height = 13;
        const double alpha = 0.68;
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double edge = x switch
                {
                    < 7 => 0.12,
                    7 => 0.28,
                    8 => 0.46,
                    9 => 0.62,
                    10 => 0.78,
                    _ => 0.9
                };
                double texture =
                    (((x * 7) + (y * 11)) % 13 - 6) * 0.008;
                double level = Math.Clamp(edge + texture, 0, 1);
                source[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        level,
                        level,
                        level,
                        alpha);
            }
        }

        PrismCatalogFilterPlan Plan(string mode, int seed) =>
            CreatePlan(
                PrismFilterId.Diffuse,
                new DrawRect(0, 0, width, height),
                (state, entry) =>
                {
                    SetSymbol(state, entry, "Mode", mode);
                    SetNumber(state, entry, "Iterations", 2);
                    SetInteger(state, entry, "Seed", seed);
                });

        PrismPremultipliedColor[] Apply(string mode, int seed) =>
            PrismCatalogFilterMath.Apply(
                Plan(mode, seed),
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        PrismPremultipliedColor[] normal = Apply("Normal", 31);
        PrismPremultipliedColor[] repeated = Apply("Normal", 31);
        PrismPremultipliedColor[] reseeded = Apply("Normal", 97);
        PrismPremultipliedColor[] darkened = Apply("DarkenOnly", 31);
        PrismPremultipliedColor[] lightened = Apply("LightenOnly", 31);
        PrismPremultipliedColor[] anisotropic =
            Apply("Anisotropic", 31);

        Assert.Equal(normal, repeated);
        Assert.False(source.SequenceEqual(normal));
        Assert.False(normal.SequenceEqual(reseeded));
        Assert.False(normal.SequenceEqual(anisotropic));
        Assert.False(darkened.SequenceEqual(lightened));
        for (int index = 0; index < source.Length; index++)
        {
            Assert.Equal(source[index].Alpha, normal[index].Alpha, 6);
            Assert.Equal(source[index].Alpha, darkened[index].Alpha, 6);
            Assert.Equal(source[index].Alpha, lightened[index].Alpha, 6);
            Assert.True(
                darkened[index].Red <= source[index].Red + 0.000001);
            Assert.True(
                lightened[index].Red >= source[index].Red - 0.000001);
            AssertFiniteAssociated(normal[index]);
            AssertFiniteAssociated(darkened[index]);
            AssertFiniteAssociated(lightened[index]);
            AssertFiniteAssociated(anisotropic[index]);
        }

        PrismPremultipliedColor constant =
            PrismPremultipliedColor.FromStraight(
                0.37,
                0.51,
                0.64,
                alpha);
        PrismPremultipliedColor[] constantSource =
            Enumerable.Repeat(constant, width * height).ToArray();
        PrismPremultipliedColor[] constantResult =
            PrismCatalogFilterMath.Apply(
                Plan("Anisotropic", 31),
                constantSource,
                width,
                height,
                PrismColorProfile.LinearSrgb);
        Assert.All(
            constantResult,
            pixel =>
            {
                Assert.Equal(constant.Red, pixel.Red, 6);
                Assert.Equal(constant.Green, pixel.Green, 6);
                Assert.Equal(constant.Blue, pixel.Blue, 6);
                Assert.Equal(constant.Alpha, pixel.Alpha, 6);
            });
    }

    [Fact]
    public void FilmGrainSynthesizesSignalDependentCorrelatedNoiseAndHonorsEveryControl()
    {
        const int width = 96;
        const int height = 48;
        const double alpha = 0.7;
        int bandWidth = width / 3;
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double level = x < bandWidth
                    ? 0.05
                    : x < bandWidth * 2
                        ? 0.5
                        : 0.8;
                source[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        level,
                        level,
                        level,
                        alpha);
            }
        }

        PrismCatalogFilterPlan baseline = Plan(
            grain: 4,
            highlightArea: 0,
            intensity: 10,
            seed: 123456789);
        PrismCatalogFilterPlan differentSeed = Plan(
            grain: 4,
            highlightArea: 0,
            intensity: 10,
            seed: 987654321);
        PrismCatalogFilterPlan fine = Plan(
            grain: 0,
            highlightArea: 0,
            intensity: 10,
            seed: 123456789);
        PrismCatalogFilterPlan coarse = Plan(
            grain: 20,
            highlightArea: 0,
            intensity: 10,
            seed: 123456789);
        PrismCatalogFilterPlan highlightWeighted = Plan(
            grain: 4,
            highlightArea: 20,
            intensity: 10,
            seed: 123456789);
        PrismCatalogFilterPlan disabled = Plan(
            grain: 4,
            highlightArea: 0,
            intensity: 0,
            seed: 123456789);

        PrismPremultipliedColor[] result = Apply(baseline);
        PrismPremultipliedColor[] repeated = Apply(baseline);
        PrismPremultipliedColor[] reseeded = Apply(differentSeed);
        PrismPremultipliedColor[] fineResult = Apply(fine);
        PrismPremultipliedColor[] coarseResult = Apply(coarse);
        PrismPremultipliedColor[] highlightResult =
            Apply(highlightWeighted);
        PrismPremultipliedColor[] disabledResult = Apply(disabled);

        Assert.NotEqual(
            baseline.GetOption("Seed"),
            differentSeed.GetOption("Seed"));
        Assert.Equal(result, repeated);
        Assert.False(source.SequenceEqual(result));
        Assert.False(result.SequenceEqual(reseeded));
        Assert.All(result, AssertFiniteAssociated);
        Assert.All(
            result,
            color => Assert.Equal(alpha, color.Alpha, 6));
        Assert.All(
            source.Zip(disabledResult),
            pair =>
            {
                Assert.Equal(pair.First.Red, pair.Second.Red, 6);
                Assert.Equal(pair.First.Green, pair.Second.Green, 6);
                Assert.Equal(pair.First.Blue, pair.Second.Blue, 6);
                Assert.Equal(pair.First.Alpha, pair.Second.Alpha, 6);
            });

        double darkDeviation = BandDeviation(result, 0);
        double middleDeviation = BandDeviation(result, 1);
        double brightDeviation = BandDeviation(result, 2);
        Assert.True(
            middleDeviation > darkDeviation * 1.5,
            $"Middle deviation {middleDeviation:F6} must exceed dark deviation {darkDeviation:F6}.");
        Assert.True(
            middleDeviation > brightDeviation * 1.2,
            $"Middle deviation {middleDeviation:F6} must exceed bright deviation {brightDeviation:F6}.");
        Assert.InRange(
            BandDeviation(reseeded, 1) / middleDeviation,
            0.75,
            1.25);

        Assert.InRange(
            Math.Abs(BandMean(result, 0) - (0.05 * alpha)),
            0,
            0.015);
        Assert.InRange(
            Math.Abs(BandMean(result, 1) - (0.5 * alpha)),
            0,
            0.015);
        Assert.InRange(
            Math.Abs(BandMean(result, 2) - (0.8 * alpha)),
            0,
            0.015);

        Assert.True(
            BandDeviation(highlightResult, 2) >
                brightDeviation * 1.2,
            "HighlightArea must shift grain energy toward highlights.");
        Assert.True(
            BandDeviation(highlightResult, 1) <
                middleDeviation * 0.9,
            "HighlightArea must not behave like a global intensity control.");
        Assert.True(
            MiddleAdjacentDifference(fineResult) >
                MiddleAdjacentDifference(coarseResult) * 1.5,
            "Grain must control spatial correlation rather than amplitude.");

        PrismCatalogFilterPlan Plan(
            float grain,
            float highlightArea,
            float intensity,
            int seed) =>
            CreatePlan(
                PrismFilterId.FilmGrain,
                new DrawRect(0, 0, width, height),
                (state, entry) =>
                {
                    SetNumber(state, entry, "Grain", grain);
                    SetNumber(
                        state,
                        entry,
                        "HighlightArea",
                        highlightArea);
                    SetNumber(state, entry, "Intensity", intensity);
                    SetInteger(state, entry, "Seed", seed);
                });

        PrismPremultipliedColor[] Apply(
            PrismCatalogFilterPlan plan) =>
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        double BandMean(
            PrismPremultipliedColor[] pixels,
            int band)
        {
            double total = 0;
            int startX = band * bandWidth;
            int endX = startX + bandWidth;
            for (int y = 0; y < height; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    total += pixels[(y * width) + x].Red;
                }
            }
            return total / (bandWidth * height);
        }

        double BandDeviation(
            PrismPremultipliedColor[] pixels,
            int band)
        {
            double mean = BandMean(pixels, band);
            double squaredTotal = 0;
            int startX = band * bandWidth;
            int endX = startX + bandWidth;
            for (int y = 0; y < height; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    double delta =
                        pixels[(y * width) + x].Red - mean;
                    squaredTotal += delta * delta;
                }
            }
            return Math.Sqrt(
                squaredTotal / (bandWidth * height));
        }

        double MiddleAdjacentDifference(
            PrismPremultipliedColor[] pixels)
        {
            double total = 0;
            int count = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = bandWidth; x < (bandWidth * 2) - 1; x++)
                {
                    total += Math.Abs(
                        pixels[(y * width) + x].Red -
                        pixels[(y * width) + x + 1].Red);
                    count++;
                }
            }
            return total / count;
        }
    }

    [Fact]
    public void AngledStrokesPlansStrokeLengthInDevicePixels()
    {
        PrismCatalogFilterPlan plan = PrismCatalogFilterPlanner.Create(
            PrismFilterId.AngledStrokes,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Number,
                    numberValue: 0.25f),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Number,
                    numberValue: 4),
                new PrismGraphParameter(
                    2,
                    PrismGraphParameterValueKind.Number,
                    numberValue: 6)
            ],
            PrismBlendMode.Normal,
            pixelScale: 2,
            effectiveTransform: Matrix3x2.CreateScale(1.5f),
            new DrawRect(0, 0, 20, 10));

        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);

        Assert.Equal(0.25f, plan.GetOption("DirectionBalance").X);
        Assert.Equal(6, plan.GetOption("StrokeLength").X);
        Assert.Equal(4, plan.GetOption("Sharpness").X);
        Assert.Equal(PrismCatalogFilterPassKind.Direct, pass.Kind);
        Assert.Equal(18, pass.RadiusX);
        Assert.Equal(18, pass.RadiusY);
        Assert.False(pass.IsNoOp);
    }

    [Fact]
    public void AngledStrokesPreservesConstantAssociatedColor()
    {
        const int width = 13;
        const int height = 9;
        PrismPremultipliedColor constant =
            PrismPremultipliedColor.FromStraight(
                0.31,
                0.47,
                0.68,
                0.72);
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(constant, width * height).ToArray();
        PrismCatalogFilterPlan plan = CreatePlan(
            PrismFilterId.AngledStrokes,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "DirectionBalance", 0.5f);
                SetNumber(state, entry, "StrokeLength", 7);
                SetNumber(state, entry, "Sharpness", 5);
            });

        PrismPremultipliedColor[] result =
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        Assert.All(
            result,
            pixel =>
            {
                Assert.Equal(constant.Red, pixel.Red, 5);
                Assert.Equal(constant.Green, pixel.Green, 5);
                Assert.Equal(constant.Blue, pixel.Blue, 5);
                Assert.Equal(constant.Alpha, pixel.Alpha, 6);
            });
    }

    [Fact]
    public void AngledStrokesIsDeterministicAndHonorsEveryControl()
    {
        const int width = 21;
        const int height = 15;
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool light = ((x / 4) + (y / 3)) % 2 == 0;
                double ripple = ((x * 5 + y * 3) % 11) / 55.0;
                source[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        (light ? 0.68 : 0.12) + ripple,
                        (light ? 0.42 : 0.2) + (ripple * 0.5),
                        (light ? 0.18 : 0.64) - (ripple * 0.35),
                        0.74);
            }
        }
        source[0] = default;

        PrismCatalogFilterPlan Plan(
            float directionBalance,
            float strokeLength,
            float sharpness) =>
            CreatePlan(
                PrismFilterId.AngledStrokes,
                new DrawRect(0, 0, width, height),
                (state, entry) =>
                {
                    SetNumber(
                        state,
                        entry,
                        "DirectionBalance",
                        directionBalance);
                    SetNumber(
                        state,
                        entry,
                        "StrokeLength",
                        strokeLength);
                    SetNumber(state, entry, "Sharpness", sharpness);
                });

        PrismPremultipliedColor[] Apply(
            PrismCatalogFilterPlan filterPlan) =>
            PrismCatalogFilterMath.Apply(
                filterPlan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        PrismPremultipliedColor[] baseline = Apply(Plan(0.5f, 5, 4));
        PrismPremultipliedColor[] repeated = Apply(Plan(0.5f, 5, 4));
        PrismPremultipliedColor[] oppositeBias = Apply(Plan(1, 5, 4));
        PrismPremultipliedColor[] longStrokes = Apply(Plan(0.5f, 10, 4));
        PrismPremultipliedColor[] sharp = Apply(Plan(0.5f, 5, 10));

        Assert.Equal(baseline, repeated);
        Assert.False(baseline.SequenceEqual(source));
        Assert.False(baseline.SequenceEqual(oppositeBias));
        Assert.False(baseline.SequenceEqual(longStrokes));
        Assert.False(baseline.SequenceEqual(sharp));
        for (int index = 0; index < source.Length; index++)
        {
            Assert.Equal(source[index].Alpha, baseline[index].Alpha, 6);
            AssertFiniteAssociated(baseline[index]);
        }
        Assert.Equal(default(PrismPremultipliedColor), baseline[0]);
    }

    [Fact]
    public void DryBrushPlansBoundedDeviceRadiusFromBrushSize()
    {
        PrismCatalogFilterPlan plan = PrismCatalogFilterPlanner.Create(
            PrismFilterId.DryBrush,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Number,
                    numberValue: 8),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Number,
                    numberValue: 2.5f),
                new PrismGraphParameter(
                    2,
                    PrismGraphParameterValueKind.Number,
                    numberValue: 1)
            ],
            PrismBlendMode.Normal,
            pixelScale: 2,
            effectiveTransform: Matrix3x2.CreateScale(1.5f),
            new DrawRect(0, 0, 20, 10));
        PrismCatalogFilterPlan clamped =
            PrismCatalogFilterPlanner.Create(
                PrismFilterId.DryBrush,
                [
                    new PrismGraphParameter(
                        0,
                        PrismGraphParameterValueKind.Number,
                        numberValue: 8),
                    new PrismGraphParameter(
                        1,
                        PrismGraphParameterValueKind.Number,
                        numberValue: 100),
                    new PrismGraphParameter(
                        2,
                        PrismGraphParameterValueKind.Number,
                        numberValue: 1)
                ],
                PrismBlendMode.Normal,
                pixelScale: 2,
                effectiveTransform: Matrix3x2.CreateScale(1.5f),
                new DrawRect(0, 0, 20, 10));

        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);
        PrismCatalogFilterPass clampedPass =
            Assert.Single(clamped.Passes);

        Assert.Equal(2.5f, plan.Options1.X);
        Assert.Equal(PrismCatalogFilterPassKind.Direct, pass.Kind);
        Assert.Equal(7.5f, pass.RadiusX);
        Assert.Equal(7.5f, pass.RadiusY);
        Assert.Equal(18, clampedPass.RadiusX);
        Assert.Equal(18, clampedPass.RadiusY);
    }

    [Fact]
    public void PaintDaubsPlansDeviceRadiusAndCanonicalBrushType()
    {
        PrismCatalogFilterPlan plan = PrismCatalogFilterPlanner.Create(
            PrismFilterId.PaintDaubs,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Number,
                    numberValue: 7),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Symbol,
                    integerValue: PrismCatalogRuntime.ResolveSymbol(
                        "BrushType",
                        "WideSharp")),
                new PrismGraphParameter(
                    2,
                    PrismGraphParameterValueKind.Number,
                    numberValue: 5)
            ],
            PrismBlendMode.Normal,
            pixelScale: 2,
            effectiveTransform: Matrix3x2.CreateScale(1.5f),
            new DrawRect(0, 0, 20, 10));
        PrismCatalogFilterPlan clamped = PrismCatalogFilterPlanner.Create(
            PrismFilterId.PaintDaubs,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Number,
                    numberValue: 100),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Symbol,
                    integerValue: PrismCatalogRuntime.ResolveSymbol(
                        "BrushType",
                        "Simple")),
                new PrismGraphParameter(
                    2,
                    PrismGraphParameterValueKind.Number,
                    numberValue: 5)
            ],
            PrismBlendMode.Normal,
            pixelScale: 2,
            effectiveTransform: Matrix3x2.CreateScale(1.5f),
            new DrawRect(0, 0, 20, 10));

        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);
        PrismCatalogFilterPass clampedPass = Assert.Single(clamped.Passes);

        Assert.Equal(PrismCatalogFilterPassKind.Direct, pass.Kind);
        Assert.Equal(21, pass.RadiusX);
        Assert.Equal(21, pass.RadiusY);
        Assert.Equal(3, plan.Options1.X);
        Assert.Equal(150, clampedPass.RadiusX);
        Assert.Equal(150, clampedPass.RadiusY);

        string[] brushTypes =
        [
            "Simple",
            "LightRough",
            "DarkRough",
            "WideSharp",
            "WideBlurry",
            "Sparkle"
        ];
        for (int code = 0; code < brushTypes.Length; code++)
        {
            PrismCatalogFilterPlan brushPlan = CreatePlan(
                PrismFilterId.PaintDaubs,
                configure: (state, entry) =>
                    SetSymbol(
                        state,
                        entry,
                        "BrushType",
                        brushTypes[code]));
            Assert.Equal(code, brushPlan.Options1.X);
        }
    }

    [Fact]
    public void PaintDaubsPreservesConstantAssociatedColor()
    {
        const int width = 13;
        const int height = 9;
        PrismPremultipliedColor constant =
            PrismPremultipliedColor.FromStraight(
                0.31,
                0.47,
                0.68,
                0.72);
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(constant, width * height).ToArray();
        PrismCatalogFilterPlan plan = CreatePlan(
            PrismFilterId.PaintDaubs,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "BrushSize", 7);
                SetNumber(state, entry, "Sharpness", 5);
                SetSymbol(state, entry, "BrushType", "Simple");
            });

        PrismPremultipliedColor[] result =
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        Assert.All(
            result,
            pixel =>
            {
                Assert.Equal(constant.Red, pixel.Red, 5);
                Assert.Equal(constant.Green, pixel.Green, 5);
                Assert.Equal(constant.Blue, pixel.Blue, 5);
                Assert.Equal(constant.Alpha, pixel.Alpha, 6);
            });
    }

    [Fact]
    public void PaintDaubsIsDeterministicAndHonorsItsControls()
    {
        const int width = 21;
        const int height = 15;
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool right = x >= width / 2;
                double ripple = ((x * 5 + y * 3) % 11) / 50.0;
                source[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        (right ? 0.62 : 0.12) + ripple,
                        (right ? 0.24 : 0.48) + (ripple * 0.5),
                        (right ? 0.16 : 0.66) - (ripple * 0.4),
                        0.74);
            }
        }
        source[0] = default;

        PrismCatalogFilterPlan Plan(
            float brushSize,
            float sharpness,
            string brushType) =>
            CreatePlan(
                PrismFilterId.PaintDaubs,
                new DrawRect(0, 0, width, height),
                (state, entry) =>
                {
                    SetNumber(state, entry, "BrushSize", brushSize);
                    SetNumber(state, entry, "Sharpness", sharpness);
                    SetSymbol(state, entry, "BrushType", brushType);
                });

        PrismPremultipliedColor[] Apply(
            PrismCatalogFilterPlan filterPlan) =>
            PrismCatalogFilterMath.Apply(
                filterPlan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        PrismPremultipliedColor[] baseline = Apply(Plan(3, 5, "Simple"));
        PrismPremultipliedColor[] repeated = Apply(Plan(3, 5, "Simple"));
        PrismPremultipliedColor[] largeBrush = Apply(Plan(9, 5, "Simple"));
        PrismPremultipliedColor[] sharp = Apply(Plan(3, 10, "Simple"));

        Assert.Equal(baseline, repeated);
        Assert.False(baseline.SequenceEqual(source));
        Assert.False(baseline.SequenceEqual(largeBrush));
        Assert.False(baseline.SequenceEqual(sharp));
        foreach (string brushType in new[]
        {
            "LightRough",
            "DarkRough",
            "WideSharp",
            "WideBlurry",
            "Sparkle"
        })
        {
            Assert.False(
                baseline.SequenceEqual(
                    Apply(Plan(3, 5, brushType))),
                $"Brush type '{brushType}' must alter the daub profile.");
        }
        for (int index = 0; index < source.Length; index++)
        {
            Assert.Equal(source[index].Alpha, baseline[index].Alpha, 6);
            AssertFiniteAssociated(baseline[index]);
        }
        Assert.Equal(default(PrismPremultipliedColor), baseline[0]);
    }

    [Fact]
    public void PaletteKnifePlansStrokeSizeInDevicePixels()
    {
        PrismCatalogFilterPlan plan = PrismCatalogFilterPlanner.Create(
            PrismFilterId.PaletteKnife,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Number,
                    numberValue: 0),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Number,
                    numberValue: 1),
                new PrismGraphParameter(
                    2,
                    PrismGraphParameterValueKind.Number,
                    numberValue: 3)
            ],
            PrismBlendMode.Normal,
            pixelScale: 2,
            effectiveTransform: Matrix3x2.CreateScale(1.5f),
            new DrawRect(0, 0, 20, 10));

        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);

        Assert.Equal(3, plan.GetOption("StrokeSize").X);
        Assert.Equal(1, plan.GetOption("StrokeDetail").X);
        Assert.Equal(0, plan.GetOption("Softness").X);
        Assert.Equal(PrismCatalogFilterPassKind.Direct, pass.Kind);
        Assert.Equal(9, pass.RadiusX);
        Assert.Equal(9, pass.RadiusY);
    }

    [Fact]
    public void PaletteKnifePreservesConstantAssociatedColor()
    {
        const int width = 13;
        const int height = 9;
        PrismPremultipliedColor constant =
            PrismPremultipliedColor.FromStraight(
                0.31,
                0.47,
                0.68,
                0.72);
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(constant, width * height).ToArray();
        PrismCatalogFilterPlan plan = CreatePlan(
            PrismFilterId.PaletteKnife,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "StrokeSize", 5);
                SetNumber(state, entry, "StrokeDetail", 2);
                SetNumber(state, entry, "Softness", 1);
            });

        PrismPremultipliedColor[] result =
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        Assert.All(
            result,
            pixel =>
            {
                Assert.Equal(constant.Red, pixel.Red, 5);
                Assert.Equal(constant.Green, pixel.Green, 5);
                Assert.Equal(constant.Blue, pixel.Blue, 5);
                Assert.Equal(constant.Alpha, pixel.Alpha, 6);
            });
    }

    [Fact]
    public void PaletteKnifeIsDeterministicAndHonorsEveryControl()
    {
        const int width = 21;
        const int height = 15;
        const double alpha = 0.74;
        int edgeX = width / 2;
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool right = x >= edgeX;
                double ripple = ((x * 5 + y * 3) % 11) / 45.0;
                source[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        (right ? 0.66 : 0.1) + ripple,
                        (right ? 0.22 : 0.45) + (ripple * 0.4),
                        (right ? 0.14 : 0.62) - (ripple * 0.35),
                        alpha);
            }
        }

        PrismCatalogFilterPlan Plan(
            float strokeSize,
            float detail,
            float softness) =>
            CreatePlan(
                PrismFilterId.PaletteKnife,
                new DrawRect(0, 0, width, height),
                (state, entry) =>
                {
                    SetNumber(state, entry, "StrokeSize", strokeSize);
                    SetNumber(state, entry, "StrokeDetail", detail);
                    SetNumber(state, entry, "Softness", softness);
                });

        PrismPremultipliedColor[] Apply(
            PrismCatalogFilterPlan filterPlan) =>
            PrismCatalogFilterMath.Apply(
                filterPlan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        PrismPremultipliedColor[] baseline = Apply(Plan(3, 1, 0));
        PrismPremultipliedColor[] repeated = Apply(Plan(3, 1, 0));
        PrismPremultipliedColor[] largeStroke = Apply(Plan(7, 1, 0));
        PrismPremultipliedColor[] detailed = Apply(Plan(3, 5, 0));
        PrismPremultipliedColor[] soft = Apply(Plan(3, 1, 4));

        Assert.Equal(baseline, repeated);
        Assert.False(source.SequenceEqual(baseline));
        Assert.False(baseline.SequenceEqual(largeStroke));
        Assert.False(baseline.SequenceEqual(detailed));
        Assert.False(baseline.SequenceEqual(soft));
        Assert.True(
            RegionDifference(baseline) <
                RegionDifference(source) * 0.8,
            "Polynomial sectors must smooth variation inside each side of the edge.");
        Assert.True(
            EdgeContrast(baseline) >
                EdgeContrast(source) * 0.75,
            "The anisotropic kernel must not smear across the dominant edge.");
        for (int index = 0; index < source.Length; index++)
        {
            Assert.Equal(source[index].Alpha, baseline[index].Alpha, 6);
            AssertFiniteAssociated(baseline[index]);
        }

        double RegionDifference(PrismPremultipliedColor[] pixels)
        {
            double total = 0;
            int count = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    if (x == edgeX - 1)
                    {
                        continue;
                    }
                    total += Math.Abs(
                        pixels[(y * width) + x].Red -
                        pixels[(y * width) + x + 1].Red);
                    count++;
                }
            }
            return total / count;
        }

        double EdgeContrast(PrismPremultipliedColor[] pixels)
        {
            double total = 0;
            for (int y = 0; y < height; y++)
            {
                total += Math.Abs(
                    pixels[(y * width) + edgeX].Red -
                    pixels[(y * width) + edgeX - 1].Red);
            }
            return total / height;
        }
    }

    [Fact]
    public void DryBrushPreservesEdgesAndBuildsDirectionalFibers()
    {
        const int width = 25;
        const int height = 17;
        const double alpha = 0.72;
        int edgeX = width / 2;
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool right = x >= edgeX;
                double regionPosition = right
                    ? (x - edgeX) / (double)(width - edgeX - 1)
                    : x / (double)(edgeX - 1);
                source[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        right
                            ? 0.68 + (regionPosition * 0.18)
                            : 0.12 + (regionPosition * 0.16),
                        right
                            ? 0.22 + (regionPosition * 0.08)
                            : 0.36 + (regionPosition * 0.12),
                        right
                            ? 0.14 + (regionPosition * 0.05)
                            : 0.62 - (regionPosition * 0.12),
                        alpha);
            }
        }
        source[0] = default;

        PrismCatalogFilterPlan smooth = CreatePlan(
            PrismFilterId.DryBrush,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "BrushSize", 4);
                SetNumber(state, entry, "BrushDetail", 8);
                SetNumber(state, entry, "Texture", 0);
            });
        PrismCatalogFilterPlan textured = CreatePlan(
            PrismFilterId.DryBrush,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "BrushSize", 4);
                SetNumber(state, entry, "BrushDetail", 8);
                SetNumber(state, entry, "Texture", 4);
            });
        PrismCatalogFilterPlan smallBrush = CreatePlan(
            PrismFilterId.DryBrush,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "BrushSize", 1);
                SetNumber(state, entry, "BrushDetail", 8);
                SetNumber(state, entry, "Texture", 0);
            });
        PrismCatalogFilterPlan highDetail = CreatePlan(
            PrismFilterId.DryBrush,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "BrushSize", 4);
                SetNumber(state, entry, "BrushDetail", 16);
                SetNumber(state, entry, "Texture", 0);
            });

        PrismPremultipliedColor[] smoothResult =
            PrismCatalogFilterMath.Apply(
                smooth,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] texturedResult =
            PrismCatalogFilterMath.Apply(
                textured,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] repeatedTexture =
            PrismCatalogFilterMath.Apply(
                textured,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] smallBrushResult =
            PrismCatalogFilterMath.Apply(
                smallBrush,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] highDetailResult =
            PrismCatalogFilterMath.Apply(
                highDetail,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        Assert.Equal(texturedResult, repeatedTexture);
        Assert.False(smoothResult.SequenceEqual(texturedResult));
        Assert.False(smoothResult.SequenceEqual(smallBrushResult));
        Assert.False(smoothResult.SequenceEqual(highDetailResult));
        for (int index = 0; index < source.Length; index++)
        {
            Assert.Equal(
                source[index].Alpha,
                texturedResult[index].Alpha,
                6);
            AssertFiniteAssociated(texturedResult[index]);
        }
        Assert.Equal(
            default(PrismPremultipliedColor),
            texturedResult[0]);

        Vector3 leftEdgeColor = Vector3.Zero;
        Vector3 rightEdgeColor = Vector3.Zero;
        for (int y = 2; y < height - 2; y++)
        {
            leftEdgeColor += StraightColor(
                smoothResult[(y * width) + edgeX - 2]);
            rightEdgeColor += StraightColor(
                smoothResult[(y * width) + edgeX + 2]);
        }
        leftEdgeColor /= height - 4;
        rightEdgeColor /= height - 4;
        float edgeDistance =
            Vector3.Distance(leftEdgeColor, rightEdgeColor);
        Assert.True(
            edgeDistance > 0.25f,
            $"DryBrush edge distance was {edgeDistance:F4}.");

        double horizontalFiberDifference = 0;
        double verticalFiberDifference = 0;
        int horizontalCount = 0;
        int verticalCount = 0;
        for (int y = 2; y < height - 2; y++)
        {
            for (int x = 2; x < width - 2; x++)
            {
                if (Math.Abs(x - edgeX) <= 2)
                {
                    continue;
                }

                double current =
                    texturedResult[(y * width) + x].Red -
                    smoothResult[(y * width) + x].Red;
                horizontalFiberDifference += Math.Abs(
                    current -
                    (texturedResult[(y * width) + x + 1].Red -
                        smoothResult[(y * width) + x + 1].Red));
                verticalFiberDifference += Math.Abs(
                    current -
                    (texturedResult[((y + 1) * width) + x].Red -
                        smoothResult[((y + 1) * width) + x].Red));
                horizontalCount++;
                verticalCount++;
            }
        }

        Assert.True(
            (horizontalFiberDifference / horizontalCount) >
                (verticalFiberDifference / verticalCount) * 1.25,
            "Texture must form fibers along the local tangent.");

        static Vector3 StraightColor(
            PrismPremultipliedColor color)
        {
            if (color.Alpha <= 0)
            {
                return Vector3.Zero;
            }

            return new Vector3(
                (float)(color.Red / color.Alpha),
                (float)(color.Green / color.Alpha),
                (float)(color.Blue / color.Alpha));
        }
    }

    [Fact]
    public void CutoutRunsBoundedMeanShiftBeforeQuantizingAndHonorsEveryControl()
    {
        const int width = 17;
        const int height = 9;
        const double alpha = 0.6;
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool leftRegion = x < width / 2;
                double noise =
                    ((((x * 3) + (y * 5)) % 7) - 3) * 0.018;
                source[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        leftRegion ? 0.18 + noise : 0.78 + noise,
                        leftRegion ? 0.45 - (noise * 0.5) : 0.28 - (noise * 0.3),
                        leftRegion ? 0.72 + (noise * 0.3) : 0.14 - (noise * 0.5),
                        alpha);
            }
        }

        PrismCatalogFilterPlan baseline = CreatePlan(
            PrismFilterId.Cutout,
            new DrawRect(0, 0, width, height));
        PrismCatalogFilterPlan detailed = CreatePlan(
            PrismFilterId.Cutout,
            new DrawRect(0, 0, width, height),
            (state, entry) => SetNumber(state, entry, "Levels", 16));
        PrismCatalogFilterPlan simple = CreatePlan(
            PrismFilterId.Cutout,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
                SetNumber(state, entry, "EdgeSimplicity", 8));
        PrismCatalogFilterPlan looseEdges = CreatePlan(
            PrismFilterId.Cutout,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
                SetNumber(state, entry, "EdgeFidelity", 0));
        PrismCatalogFilterPlan faithfulEdges = CreatePlan(
            PrismFilterId.Cutout,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
                SetNumber(state, entry, "EdgeFidelity", 10));

        Assert.Equal(
            [
                PrismCatalogFilterPassKind.Iteration,
                PrismCatalogFilterPassKind.Iteration,
                PrismCatalogFilterPassKind.Direct
            ],
            baseline.Passes.Select(pass => pass.Kind));
        Assert.Equal(
            [0, 1, 2],
            baseline.Passes.Select(pass => pass.Iteration));
        PrismGraph graph = CreateGraph(
            PrismFilterId.Cutout,
            new DrawRect(0, 0, width, height));
        PrismGraphNode quantizationPass = graph.Nodes.Single(node =>
            node.Filter == PrismFilterId.Cutout &&
            node.CatalogFilterPassIndex == 2);
        Assert.Contains(
            graph.Edges,
            edge =>
                edge.Target == quantizationPass.Id &&
                edge.Kind == PrismGraphEdgeKind.FilterOriginal);

        PrismPremultipliedColor[] baselineResult = Apply(baseline);
        PrismPremultipliedColor[] detailedResult = Apply(detailed);
        PrismPremultipliedColor[] simpleResult = Apply(simple);
        PrismPremultipliedColor[] looseResult = Apply(looseEdges);
        PrismPremultipliedColor[] faithfulResult = Apply(faithfulEdges);
        PrismPremultipliedColor[] transparentEffect =
            PrismCatalogFilterMath.Apply(
                baseline,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb,
                opacity: 0);

        Assert.NotEqual(baselineResult, detailedResult);
        Assert.NotEqual(baselineResult, simpleResult);
        Assert.NotEqual(looseResult, faithfulResult);
        Assert.All(
            baselineResult,
            color => Assert.Equal(alpha, color.Alpha, 6));
        Assert.All(baselineResult, AssertFiniteAssociated);
        Assert.All(detailedResult, AssertFiniteAssociated);
        Assert.All(simpleResult, AssertFiniteAssociated);
        Assert.All(looseResult, AssertFiniteAssociated);
        Assert.All(faithfulResult, AssertFiniteAssociated);
        Assert.All(
            source.Zip(transparentEffect),
            pair =>
            {
                Assert.Equal(pair.First.Red, pair.Second.Red, 6);
                Assert.Equal(pair.First.Green, pair.Second.Green, 6);
                Assert.Equal(pair.First.Blue, pair.Second.Blue, 6);
                Assert.Equal(pair.First.Alpha, pair.Second.Alpha, 6);
            });
        AssertQuantizationGrid(baselineResult, 8);
        AssertQuantizationGrid(detailedResult, 16);
        Assert.True(
            AverageHorizontalVariation(baselineResult) <
            AverageHorizontalVariation(source));
        Assert.True(
            BoundaryContrast(faithfulResult) >
            BoundaryContrast(looseResult));

        PrismPremultipliedColor[] Apply(
            PrismCatalogFilterPlan plan) =>
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        static void AssertQuantizationGrid(
            IEnumerable<PrismPremultipliedColor> colors,
            int levels)
        {
            foreach (PrismPremultipliedColor color in colors)
            {
                double scale = levels - 1;
                Assert.Equal(
                    Math.Round((color.Red / color.Alpha) * scale),
                    (color.Red / color.Alpha) * scale,
                    5);
                Assert.Equal(
                    Math.Round((color.Green / color.Alpha) * scale),
                    (color.Green / color.Alpha) * scale,
                    5);
                Assert.Equal(
                    Math.Round((color.Blue / color.Alpha) * scale),
                    (color.Blue / color.Alpha) * scale,
                    5);
            }
        }

        static double AverageHorizontalVariation(
            IReadOnlyList<PrismPremultipliedColor> colors)
        {
            double total = 0;
            int count = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 1; x < width; x++)
                {
                    if (x == width / 2)
                    {
                        continue;
                    }

                    PrismPremultipliedColor left =
                        colors[(y * width) + x - 1];
                    PrismPremultipliedColor right =
                        colors[(y * width) + x];
                    total +=
                        Math.Abs(
                            (left.Red / left.Alpha) -
                            (right.Red / right.Alpha)) +
                        Math.Abs(
                            (left.Green / left.Alpha) -
                            (right.Green / right.Alpha)) +
                        Math.Abs(
                            (left.Blue / left.Alpha) -
                            (right.Blue / right.Alpha));
                    count++;
                }
            }

            return total / count;
        }

        static double BoundaryContrast(
            IReadOnlyList<PrismPremultipliedColor> colors)
        {
            double total = 0;
            for (int y = 0; y < height; y++)
            {
                PrismPremultipliedColor left =
                    colors[(y * width) + (width / 2) - 1];
                PrismPremultipliedColor right =
                    colors[(y * width) + (width / 2)];
                total +=
                    Math.Abs(
                        (left.Red / left.Alpha) -
                        (right.Red / right.Alpha)) +
                    Math.Abs(
                        (left.Green / left.Alpha) -
                        (right.Green / right.Alpha)) +
                    Math.Abs(
                        (left.Blue / left.Alpha) -
                        (right.Blue / right.Alpha));
            }

            return total / height;
        }
    }

    [Fact]
    public void ColoredPencilRunsSwingBilateralLicAndHonorsEveryControl()
    {
        const int width = 17;
        const int height = 17;
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool firstRegion = x + y < width;
                double stripe = ((x - y) & 3) == 0 ? 0.08 : 0;
                source[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        firstRegion ? 0.15 + stripe : 0.85 - stripe,
                        firstRegion ? 0.45 + stripe : 0.35,
                        firstRegion ? 0.75 : 0.2 + stripe,
                        0.65);
            }
        }

        PrismCatalogFilterPlan baseline = CreatePlan(
            PrismFilterId.ColoredPencil,
            new DrawRect(0, 0, width, height));
        PrismCatalogFilterPlan wide = CreatePlan(
            PrismFilterId.ColoredPencil,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
                SetNumber(state, entry, "PencilWidth", 7));
        PrismCatalogFilterPlan soft = CreatePlan(
            PrismFilterId.ColoredPencil,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
                SetNumber(state, entry, "StrokePressure", 2));
        PrismCatalogFilterPlan brightRedPaper = CreatePlan(
            PrismFilterId.ColoredPencil,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "PaperBrightness", 0.9f);
                SetColor(
                    state,
                    entry,
                    "PaperColor",
                    new Color(255, 64, 32, 255));
            });

        Assert.Equal(
            [
                PrismCatalogFilterPassKind.Direct,
                PrismCatalogFilterPassKind.Horizontal,
                PrismCatalogFilterPassKind.Vertical,
                PrismCatalogFilterPassKind.Iteration
            ],
            baseline.Passes.Select(pass => pass.Kind));
        Assert.Equal(
            [0, 1, 2, 3],
            baseline.Passes.Select(pass => pass.Iteration));
        PrismGraph graph = CreateGraph(
            PrismFilterId.ColoredPencil,
            new DrawRect(0, 0, width, height));
        PrismGraphNode compositePass = graph.Nodes.Single(node =>
            node.Filter == PrismFilterId.ColoredPencil &&
            node.CatalogFilterPassIndex == 3);
        Assert.Contains(
            graph.Edges,
            edge =>
                edge.Target == compositePass.Id &&
                edge.Kind == PrismGraphEdgeKind.FilterOriginal);

        PrismPremultipliedColor[] baselineResult = Apply(baseline);
        PrismPremultipliedColor[] wideResult = Apply(wide);
        PrismPremultipliedColor[] softResult = Apply(soft);
        PrismPremultipliedColor[] paperResult = Apply(brightRedPaper);

        Assert.NotEqual(baselineResult, wideResult);
        Assert.NotEqual(baselineResult, softResult);
        Assert.NotEqual(baselineResult, paperResult);
        Assert.All(
            baselineResult,
            color => Assert.Equal(0.65, color.Alpha, 6));
        Assert.All(baselineResult, AssertFiniteAssociated);
        Assert.All(wideResult, AssertFiniteAssociated);
        Assert.All(softResult, AssertFiniteAssociated);
        Assert.All(paperResult, AssertFiniteAssociated);
        Assert.True(
            paperResult.Average(color => color.Red) >
            paperResult.Average(color => color.Green));

        PrismPremultipliedColor[] Apply(
            PrismCatalogFilterPlan plan) =>
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);
    }

    [Fact]
    public void DeinterlaceTracesDiagonalEdgesAndPreservesTheSourceField()
    {
        const int size = 9;
        const double alpha = 0.4;
        PrismPremultipliedColor black =
            PrismPremultipliedColor.FromStraight(0, 0, 0, alpha);
        PrismPremultipliedColor white =
            PrismPremultipliedColor.FromStraight(1, 1, 1, alpha);
        PrismPremultipliedColor discarded =
            PrismPremultipliedColor.FromStraight(1, 0, 0, alpha);
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                source[(y * size) + x] =
                    (y & 1) == 1
                        ? discarded
                        : x == y
                            ? white
                            : black;
            }
        }

        PrismCatalogFilterPlan plan = CreatePlan(
            PrismFilterId.Deinterlace);
        PrismPremultipliedColor[] result =
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);

        Assert.Equal(
            source[(4 * size) + 4].Red,
            result[(4 * size) + 4].Red,
            6);
        Assert.Equal(
            source[(4 * size) + 4].Alpha,
            result[(4 * size) + 4].Alpha,
            6);
        Assert.InRange(
            result[(3 * size) + 3].Red,
            0.35,
            alpha + 0.000001);
        Assert.Equal(alpha, result[(3 * size) + 3].Alpha, 6);
        Assert.True(result[(3 * size) + 3].Red >
            result[(3 * size) + 2].Red);
        Assert.All(result, AssertFiniteAssociated);
    }

    [Fact]
    public void DeinterlaceHonorsEvenFieldAndDuplicationControls()
    {
        const int width = 3;
        const int height = 5;
        PrismPremultipliedColor red =
            PrismPremultipliedColor.FromStraight(1, 0, 0, 1);
        PrismPremultipliedColor green =
            PrismPremultipliedColor.FromStraight(0, 1, 0, 1);
        PrismPremultipliedColor blue =
            PrismPremultipliedColor.FromStraight(0, 0, 1, 1);
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(red, width * height).ToArray();
        for (int x = 0; x < width; x++)
        {
            source[(1 * width) + x] = green;
            source[(3 * width) + x] = blue;
        }
        PrismCatalogFilterPlan plan = CreatePlan(
            PrismFilterId.Deinterlace,
            configure: (state, entry) =>
            {
                SetSymbol(state, entry, "Field", "Even");
                SetSymbol(
                    state,
                    entry,
                    "Replacement",
                    "Duplication");
            });

        PrismPremultipliedColor[] result =
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        Assert.Equal(green, result[0]);
        Assert.Equal(green, result[(1 * width) + 1]);
        Assert.Equal(green, result[(2 * width) + 1]);
        Assert.Equal(blue, result[(3 * width) + 1]);
        Assert.Equal(blue, result[(4 * width) + 1]);
    }

    [Fact]
    public void NtscColorsReducesLuminanceUntilCompositeSignalIsLegal()
    {
        const double alpha = 0.4;
        PrismPremultipliedColor[] source =
        [
            PrismPremultipliedColor.FromStraight(1, 0, 0, alpha),
            PrismPremultipliedColor.FromStraight(1, 1, 0, 1),
            PrismPremultipliedColor.FromStraight(0.5, 0.5, 0.5, 1)
        ];
        PrismCatalogFilterPlan plan = CreatePlan(
            PrismFilterId.NtscColors);

        PrismPremultipliedColor[] result =
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                source.Length,
                1,
                PrismColorProfile.LinearSrgb);

        for (int index = 0; index < result.Length; index++)
        {
            (double chrominance, double compositePeak) =
                NtscSignal(result[index]);
            Assert.InRange(chrominance, 0, 50.0001);
            Assert.InRange(
                compositePeak,
                0,
                110.0001);
            Assert.Equal(source[index].Alpha, result[index].Alpha, 6);
            AssertFiniteAssociated(result[index]);
        }

        Assert.True(result[0].Red < source[0].Red);
        Assert.Equal(
            0.708372,
            result[0].Red / result[0].Alpha,
            5);
        Assert.True(result[1].Red < source[1].Red);
        Assert.Equal(source[2], result[2]);
    }

    [Fact]
    public void LightingEffectsUsesGgxMaterialHeightAndExposureControls()
    {
        const int size = 5;
        const double alpha = 0.4;
        PrismPremultipliedColor[] source = Enumerable.Repeat(
                PrismPremultipliedColor.FromStraight(
                    0.6,
                    0.35,
                    0.15,
                    alpha),
                size * size)
            .ToArray();
        PrismLightingResource lighting = new(
        [
            PrismLight.Directional(
                Vector3.Normalize(new Vector3(0.55f, -0.25f, 1)),
                new Vector3(1, 0.8f, 0.6f),
                1.2f),
            PrismLight.Point(
                new Vector3(0.75f, 0.2f, 0.6f),
                new Vector3(0.25f, 0.45f, 1),
                0.08f)
        ]);
        Func<Vector2, Vector4> height = uv =>
            new Vector4(
                (0.7f * uv.X) + (0.3f * uv.Y),
                (0.7f * uv.X) + (0.3f * uv.Y),
                (0.7f * uv.X) + (0.3f * uv.Y),
                1);

        PrismPremultipliedColor[] baseline = ApplyLighting(
            metallic: 0,
            gloss: 0.35f,
            exposure: 0,
            textureHeight: 0);
        PrismPremultipliedColor[] metallic = ApplyLighting(
            metallic: 1,
            gloss: 0.35f,
            exposure: 0,
            textureHeight: 0);
        PrismPremultipliedColor[] glossy = ApplyLighting(
            metallic: 0,
            gloss: 0.9f,
            exposure: 0,
            textureHeight: 0);
        PrismPremultipliedColor[] exposed = ApplyLighting(
            metallic: 0,
            gloss: 0.35f,
            exposure: 1,
            textureHeight: 0);
        PrismPremultipliedColor[] textured = ApplyLighting(
            metallic: 0,
            gloss: 0.35f,
            exposure: 0,
            textureHeight: 2);

        Assert.False(baseline.SequenceEqual(metallic));
        Assert.False(baseline.SequenceEqual(glossy));
        Assert.False(baseline.SequenceEqual(exposed));
        Assert.False(baseline.SequenceEqual(textured));
        Assert.True(
            exposed[(2 * size) + 2].Red >
            baseline[(2 * size) + 2].Red);
        Assert.All(
            baseline
                .Concat(metallic)
                .Concat(glossy)
                .Concat(exposed)
                .Concat(textured),
            color =>
            {
                Assert.Equal(alpha, color.Alpha, 6);
                AssertFiniteAssociated(color);
            });

        PrismPremultipliedColor[] ApplyLighting(
            float metallic,
            float gloss,
            float exposure,
            float textureHeight)
        {
            PrismCatalogFilterPlan plan = CreatePlan(
                PrismFilterId.LightingEffects,
                new DrawRect(0, 0, size, size),
                (state, entry) =>
                {
                    SetNumber(state, entry, "Ambient", 0.05f);
                    SetNumber(state, entry, "Metallic", metallic);
                    SetNumber(state, entry, "Gloss", gloss);
                    SetNumber(state, entry, "Exposure", exposure);
                    SetNumber(
                        state,
                        entry,
                        "TextureHeight",
                        textureHeight);
                });
            return PrismCatalogFilterMath.Apply(
                plan,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb,
                auxiliaryResource: height,
                lightingResource: lighting);
        }
    }

    [Fact]
    public void PlasticWrapUsesSourceHeightGgxAndExistingControls()
    {
        const int size = 9;
        const double alpha = 0.4;
        PrismPremultipliedColor[] source = Enumerable.Range(
                0,
                size * size)
            .Select(index =>
            {
                int x = index % size;
                int y = index / size;
                double distance = Math.Sqrt(
                    Math.Pow(x - 4, 2) +
                    Math.Pow(y - 4, 2));
                double height = Math.Clamp(
                    0.15 + ((1 - (distance / 6)) * 0.7),
                    0.15,
                    0.85);
                return PrismPremultipliedColor.FromStraight(
                    0.15 + (height * 0.55),
                    0.12 + (height * 0.35),
                    0.08 + (height * 0.2),
                    alpha);
            })
            .ToArray();

        PrismPremultipliedColor[] baseline = ApplyPlasticWrap(
            highlightStrength: 15,
            detail: 9,
            smoothness: 7,
            source);
        PrismPremultipliedColor[] repeated = ApplyPlasticWrap(
            highlightStrength: 15,
            detail: 9,
            smoothness: 7,
            source);
        PrismPremultipliedColor[] noHighlight = ApplyPlasticWrap(
            highlightStrength: 0,
            detail: 9,
            smoothness: 7,
            source);
        PrismPremultipliedColor[] lowDetail = ApplyPlasticWrap(
            highlightStrength: 15,
            detail: 0,
            smoothness: 7,
            source);
        PrismPremultipliedColor[] smooth = ApplyPlasticWrap(
            highlightStrength: 15,
            detail: 9,
            smoothness: 15,
            source);

        for (int index = 0; index < source.Length; index++)
        {
            Assert.Equal(source[index].Red, noHighlight[index].Red, 6);
            Assert.Equal(source[index].Green, noHighlight[index].Green, 6);
            Assert.Equal(source[index].Blue, noHighlight[index].Blue, 6);
            Assert.Equal(source[index].Alpha, noHighlight[index].Alpha, 6);
        }
        Assert.Equal(baseline, repeated);
        Assert.False(source.SequenceEqual(baseline));
        Assert.False(baseline.SequenceEqual(lowDetail));
        Assert.False(baseline.SequenceEqual(smooth));

        PrismPremultipliedColor flat =
            PrismPremultipliedColor.FromStraight(
                0.35,
                0.25,
                0.15,
                alpha);
        PrismPremultipliedColor[] flatResult = ApplyPlasticWrap(
            highlightStrength: 15,
            detail: 9,
            smoothness: 7,
            Enumerable.Repeat(flat, size * size).ToArray());
        Assert.All(flatResult, color => Assert.Equal(flatResult[0], color));

        Assert.All(
            baseline
                .Concat(lowDetail)
                .Concat(smooth)
                .Concat(flatResult),
            color =>
            {
                Assert.Equal(alpha, color.Alpha, 6);
                AssertFiniteAssociated(color);
            });

        PrismPremultipliedColor[] ApplyPlasticWrap(
            float highlightStrength,
            float detail,
            float smoothness,
            PrismPremultipliedColor[] pixels)
        {
            PrismCatalogFilterPlan plan = CreatePlan(
                PrismFilterId.PlasticWrap,
                new DrawRect(0, 0, size, size),
                (state, entry) =>
                {
                    SetNumber(
                        state,
                        entry,
                        "HighlightStrength",
                        highlightStrength);
                    SetNumber(state, entry, "Detail", detail);
                    SetNumber(
                        state,
                        entry,
                        "Smoothness",
                        smoothness);
                });
            return PrismCatalogFilterMath.Apply(
                plan,
                pixels,
                size,
                size,
                PrismColorProfile.LinearSrgb);
        }
    }

    [Fact]
    public void ProceduralSeedRepeatsAndChangesThePattern()
    {
        PrismPremultipliedColor[] source = SampleImage();
        PrismCatalogFilterPlan seedSeven = CreatePlan(
            PrismFilterId.Clouds,
            configure: (state, entry) =>
                SetInteger(
                    state,
                    entry,
                    "Seed",
                    2_000_000_007));
        PrismCatalogFilterPlan seedEight = CreatePlan(
            PrismFilterId.Clouds,
            configure: (state, entry) =>
                SetInteger(
                    state,
                    entry,
                    "Seed",
                    2_000_000_008));

        PrismPremultipliedColor[] first =
            PrismCatalogFilterMath.Apply(
                seedSeven,
                source,
                4,
                4,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] repeated =
            PrismCatalogFilterMath.Apply(
                seedSeven,
                source,
                4,
                4,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] changed =
            PrismCatalogFilterMath.Apply(
                seedEight,
                source,
                4,
                4,
                PrismColorProfile.LinearSrgb);

        Assert.Equal(2_000_000_007u, seedSeven.WaveNoiseSeed);
        Assert.Equal(2_000_000_008u, seedEight.WaveNoiseSeed);
        Assert.Equal(first, repeated);
        Assert.False(first.SequenceEqual(changed));
    }

    [Fact]
    public void FibersProducesLongitudinallyCoherentSeededPattern()
    {
        const int size = 64;
        const double alpha = 0.4;
        PrismPremultipliedColor sourcePixel =
            PrismPremultipliedColor.FromStraight(1, 1, 1, alpha);
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(sourcePixel, size * size).ToArray();
        PrismCatalogFilterPlan baseline = CreatePlan(
            PrismFilterId.Fibers,
            configure: (state, entry) =>
            {
                SetNumber(state, entry, "Variance", 12);
                SetNumber(state, entry, "Strength", 1);
                SetInteger(state, entry, "Seed", 17);
            });
        PrismCatalogFilterPlan changedSeed = CreatePlan(
            PrismFilterId.Fibers,
            configure: (state, entry) =>
            {
                SetNumber(state, entry, "Variance", 12);
                SetNumber(state, entry, "Strength", 1);
                SetInteger(state, entry, "Seed", 18);
            });
        PrismCatalogFilterPlan changedVariance = CreatePlan(
            PrismFilterId.Fibers,
            configure: (state, entry) =>
            {
                SetNumber(state, entry, "Variance", 4);
                SetNumber(state, entry, "Strength", 1);
                SetInteger(state, entry, "Seed", 17);
            });
        PrismCatalogFilterPlan changedStrength = CreatePlan(
            PrismFilterId.Fibers,
            configure: (state, entry) =>
            {
                SetNumber(state, entry, "Variance", 12);
                SetNumber(state, entry, "Strength", 4);
                SetInteger(state, entry, "Seed", 17);
            });

        PrismPremultipliedColor[] first =
            PrismCatalogFilterMath.Apply(
                baseline,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] repeated =
            PrismCatalogFilterMath.Apply(
                baseline,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] differentSeed =
            PrismCatalogFilterMath.Apply(
                changedSeed,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] differentVariance =
            PrismCatalogFilterMath.Apply(
                changedVariance,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] differentStrength =
            PrismCatalogFilterMath.Apply(
                changedStrength,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);

        Assert.Equal(first, repeated);
        Assert.False(first.SequenceEqual(differentSeed));
        Assert.False(first.SequenceEqual(differentVariance));
        Assert.False(first.SequenceEqual(differentStrength));
        Assert.True(
            AverageAdjacentDifference(
                first,
                size,
                size,
                horizontal: false) <
            AverageAdjacentDifference(
                first,
                size,
                size,
                horizontal: true) *
            0.65);
        Assert.All(
            first,
            color =>
            {
                Assert.Equal(alpha, color.Alpha, 6);
                AssertFiniteAssociated(color);
            });
    }

    [Fact]
    public void WaveNoisePrecomputationIsDeterministicSpectralAndContinuous()
    {
        Vector4 frequencyRange =
            new(0.2f, 0.21f, 0, 0);
        PrismWaveNoiseTable brown =
            PrismWaveNoise.Precompute(
                37,
                frequencyRange,
                PrismWaveSpectrum.Brown);
        PrismWaveNoiseTable repeated =
            PrismWaveNoise.Precompute(
                37,
                frequencyRange,
                PrismWaveSpectrum.Brown);
        PrismWaveNoiseTable white =
            PrismWaveNoise.Precompute(
                37,
                new Vector4(0.03125f, 1, 0, 0),
                PrismWaveSpectrum.White);

        Assert.Equal(
            PrismWaveNoise.PackedTableSampleCount,
            brown.PackedSamples.Length);
        Assert.True(float.IsFinite(brown.Normalization));
        Assert.True(brown.Normalization > 0);
        Assert.True(
            brown.PackedSamples.SequenceEqual(
                repeated.PackedSamples));
        Assert.False(
            brown.PackedSamples.SequenceEqual(
                white.PackedSamples));

        float minimum = 1;
        float maximum = 0;
        float largestStep = 0;
        float previous = PrismWaveNoise.Sample(
            white,
            new Vector2(0, 2.75f),
            37,
            20,
            4,
            new Vector4(0, 1, 0, 0));
        for (int index = 1; index <= 256; index++)
        {
            float current = PrismWaveNoise.Sample(
                white,
                new Vector2(index / 32f, 2.75f),
                37,
                20,
                4,
                new Vector4(0, 1, 0, 0));
            minimum = MathF.Min(minimum, current);
            maximum = MathF.Max(maximum, current);
            largestStep = MathF.Max(
                largestStep,
                MathF.Abs(current - previous));
            previous = current;
        }

        Assert.True(maximum - minimum > 0.05f);
        Assert.InRange(largestStep, 0, 0.2f);
    }

    [Fact]
    public void WaveNoiseAnisotropyChangesTheDirectionalField()
    {
        PrismWaveNoiseTable table =
            PrismWaveNoise.Precompute(
                91,
                new Vector4(0.03125f, 1, 0, 0),
                PrismWaveSpectrum.Pink);
        float horizontal = PrismWaveNoise.Sample(
            table,
            new Vector2(3.25f, 7.5f),
            91,
            20,
            4,
            new Vector4(0, 0, 0, 0));
        float vertical = PrismWaveNoise.Sample(
            table,
            new Vector2(3.25f, 7.5f),
            91,
            20,
            4,
            new Vector4(90, 0, 0, 0));

        Assert.True(MathF.Abs(horizontal - vertical) > 0.001f);
    }

    [Fact]
    public void CrystallizeUsesTheNearestGeneratorAcrossCellBoundaries()
    {
        const int width = 12;
        const int height = 12;
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double alpha =
                    (((x * 3) + (y * 2)) % 7 + 1) / 8d;
                source[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        x / (double)(width - 1),
                        y / (double)(height - 1),
                        (x + y) /
                            (double)(width + height - 2),
                        alpha);
            }
        }
        PrismCatalogFilterPlan plan = CreatePlan(
            PrismFilterId.Crystallize,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "CellSize", 4);
                SetInteger(state, entry, "Seed", 0);
            });

        PrismPremultipliedColor[] result =
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        PrismPremultipliedColor expected =
            source[(4 * width) + 8];
        PrismPremultipliedColor actual =
            result[(4 * width) + 6];

        Assert.Equal(expected.Red, actual.Red, 6);
        Assert.Equal(expected.Green, actual.Green, 6);
        Assert.Equal(expected.Blue, actual.Blue, 6);
        Assert.Equal(expected.Alpha, actual.Alpha, 6);
        Assert.NotEqual(
            source[(5 * width) + 4],
            actual);
    }

    [Fact]
    public void CrystallizeSeedChangesTheVoronoiCells()
    {
        const int width = 12;
        const int height = 12;
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        for (int index = 0; index < source.Length; index++)
        {
            source[index] =
                PrismPremultipliedColor.FromStraight(
                    index / (double)(source.Length - 1),
                    0,
                    0,
                    1);
        }
        PrismCatalogFilterPlan firstPlan = CreatePlan(
            PrismFilterId.Crystallize,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "CellSize", 4);
                SetInteger(state, entry, "Seed", 0);
            });
        PrismCatalogFilterPlan changedPlan = CreatePlan(
            PrismFilterId.Crystallize,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "CellSize", 4);
                SetInteger(state, entry, "Seed", 1);
            });

        PrismPremultipliedColor[] first =
            PrismCatalogFilterMath.Apply(
                firstPlan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] changed =
            PrismCatalogFilterMath.Apply(
                changedPlan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        Assert.False(first.SequenceEqual(changed));
    }

    [Fact]
    public void ExtrudeBuildsDeterministicBlockCapsAndSideFaces()
    {
        const int width = 12;
        const int height = 8;
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int cellX = x / 4;
                int cellY = y / 4;
                source[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        (cellX + 1) / 4d,
                        (cellY + 1) / 3d,
                        (cellX + cellY + 1) / 5d,
                        0.75);
            }
        }

        PrismCatalogFilterPlan plan = CreatePlan(
            PrismFilterId.Extrude,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetSymbol(state, entry, "Type", "Blocks");
                SetNumber(state, entry, "Size", 4);
                SetNumber(state, entry, "Depth", 4);
                SetSymbol(state, entry, "DepthMode", "Level");
                SetBoolean(state, entry, "SolidFrontFaces", true);
                SetBoolean(state, entry, "MaskIncompleteBlocks", false);
                SetInteger(state, entry, "Seed", 17);
            });
        PrismCatalogFilterPlan flatPlan = CreatePlan(
            PrismFilterId.Extrude,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetSymbol(state, entry, "Type", "Blocks");
                SetNumber(state, entry, "Size", 4);
                SetNumber(state, entry, "Depth", 0);
                SetSymbol(state, entry, "DepthMode", "Level");
                SetBoolean(state, entry, "SolidFrontFaces", true);
                SetBoolean(state, entry, "MaskIncompleteBlocks", false);
            });

        PrismPremultipliedColor[] result =
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] repeated =
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] flat =
            PrismCatalogFilterMath.Apply(
                flatPlan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        Assert.Equal(PrismCatalogFilterPrimitive.Extrude, plan.Primitive);
        Assert.Equal(0f, plan.GetOption("Type").X);
        Assert.Equal(1f, plan.GetOption("DepthMode").X);
        Assert.True(result.SequenceEqual(repeated));
        Assert.False(result.SequenceEqual(flat));
        Assert.NotEqual(source[(5 * width) + 4], result[(5 * width) + 4]);
        Assert.Equal(0.75, result[(5 * width) + 4].Alpha, 6);
        Assert.All(result, AssertFiniteAssociated);
    }

    [Fact]
    public void ExtrudeSupportsPyramidsAndMasksIncompleteCells()
    {
        const int width = 10;
        const int height = 9;
        PrismPremultipliedColor[] source =
            Enumerable.Range(0, width * height)
                .Select(index =>
                    PrismPremultipliedColor.FromStraight(
                        (index % width) / (double)(width - 1),
                        (index / width) / (double)(height - 1),
                        0.4,
                        0.6))
                .ToArray();
        PrismCatalogFilterPlan pyramidPlan = CreatePlan(
            PrismFilterId.Extrude,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetSymbol(state, entry, "Type", "Pyramids");
                SetNumber(state, entry, "Size", 4);
                SetNumber(state, entry, "Depth", 4);
                SetSymbol(state, entry, "DepthMode", "Level");
                SetBoolean(state, entry, "SolidFrontFaces", true);
                SetBoolean(state, entry, "MaskIncompleteBlocks", false);
            });
        PrismCatalogFilterPlan blockPlan = CreatePlan(
            PrismFilterId.Extrude,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetSymbol(state, entry, "Type", "Blocks");
                SetNumber(state, entry, "Size", 4);
                SetNumber(state, entry, "Depth", 4);
                SetSymbol(state, entry, "DepthMode", "Level");
                SetBoolean(state, entry, "SolidFrontFaces", true);
                SetBoolean(state, entry, "MaskIncompleteBlocks", false);
            });
        PrismCatalogFilterPlan maskedPlan = CreatePlan(
            PrismFilterId.Extrude,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetSymbol(state, entry, "Type", "Blocks");
                SetNumber(state, entry, "Size", 4);
                SetNumber(state, entry, "Depth", 0);
                SetSymbol(state, entry, "DepthMode", "Level");
                SetBoolean(state, entry, "SolidFrontFaces", false);
                SetBoolean(state, entry, "MaskIncompleteBlocks", true);
            });

        PrismPremultipliedColor[] pyramids =
            PrismCatalogFilterMath.Apply(
                pyramidPlan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] blocks =
            PrismCatalogFilterMath.Apply(
                blockPlan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] masked =
            PrismCatalogFilterMath.Apply(
                maskedPlan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        Assert.Equal(1f, pyramidPlan.GetOption("Type").X);
        Assert.False(pyramids.SequenceEqual(blocks));
        Assert.Equal(0d, masked[(8 * width) + 9].Alpha);
        Assert.Equal(source[0].Alpha, masked[0].Alpha, 6);
        Assert.All(pyramids, AssertFiniteAssociated);
    }

    [Fact]
    public void PointillizeRankTileIsProgressiveBlueNoise()
    {
        int[] ranks = Enumerable.Range(0, 256)
            .Select(index =>
                PrismIncrementalVoronoiSet.Rank(
                    index % 16,
                    index / 16,
                    0))
            .ToArray();

        Assert.Equal(
            Enumerable.Range(0, 256),
            ranks.Order());
        Assert.True(
            MinimumPointillizeDistanceSquared(ranks, 64) >= 4);
        Assert.True(
            MinimumPointillizeDistanceSquared(ranks, 128) >= 2);
    }

    [Fact]
    public void PointillizeIsDeterministicAndSeedChangesTheSet()
    {
        const int width = 48;
        const int height = 48;
        PrismPremultipliedColor[] source = Enumerable.Repeat(
                PrismPremultipliedColor.FromStraight(
                    0.2,
                    0.35,
                    0.5,
                    1),
                width * height)
            .ToArray();
        PrismCatalogFilterPlan seedSeven = CreatePlan(
            PrismFilterId.Pointillize,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "CellSize", 4);
                SetInteger(state, entry, "Seed", 7);
            });
        PrismCatalogFilterPlan seedEight = CreatePlan(
            PrismFilterId.Pointillize,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "CellSize", 4);
                SetInteger(state, entry, "Seed", 8);
            });

        PrismPremultipliedColor[] first =
            PrismCatalogFilterMath.Apply(
                seedSeven,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] repeated =
            PrismCatalogFilterMath.Apply(
                seedSeven,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] changed =
            PrismCatalogFilterMath.Apply(
                seedEight,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        Assert.Equal(first, repeated);
        Assert.False(first.SequenceEqual(changed));
    }

    [Fact]
    public void PointillizeUsesToneDependentNestedDensity()
    {
        const int width = 64;
        const int height = 64;
        PrismCatalogFilterPlan plan = CreatePlan(
            PrismFilterId.Pointillize,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "CellSize", 4);
                SetInteger(state, entry, "Seed", 23);
            });
        PrismPremultipliedColor[] dark = Enumerable.Repeat(
                PrismPremultipliedColor.FromStraight(
                    0.15,
                    0.15,
                    0.15,
                    1),
                width * height)
            .ToArray();
        PrismPremultipliedColor[] light = Enumerable.Repeat(
                PrismPremultipliedColor.FromStraight(
                    0.8,
                    0.8,
                    0.8,
                    1),
                width * height)
            .ToArray();

        PrismPremultipliedColor[] darkResult =
            PrismCatalogFilterMath.Apply(
                plan,
                dark,
                width,
                height,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] lightResult =
            PrismCatalogFilterMath.Apply(
                plan,
                light,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        int darkCoverage = darkResult.Count(color =>
            color.Alpha > 0.01);
        int lightCoverage = lightResult.Count(color =>
            color.Alpha > 0.01);
        Assert.True(darkCoverage > lightCoverage * 2);
        Assert.All(darkResult, AssertFiniteAssociated);
        Assert.All(lightResult, AssertFiniteAssociated);
    }

    [Fact]
    public void PointillizeAntialiasesOverIndependentBackgroundAlpha()
    {
        const int width = 64;
        const int height = 64;
        const double backgroundAlpha = 64d / 255;
        PrismCatalogFilterPlan plan = CreatePlan(
            PrismFilterId.Pointillize,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "CellSize", 5);
                SetColor(
                    state,
                    entry,
                    "Background",
                    new Color(32, 96, 192, 64));
                SetInteger(state, entry, "Seed", 91);
            });
        PrismPremultipliedColor[] source = Enumerable.Repeat(
                PrismPremultipliedColor.FromStraight(
                    0.65,
                    0.2,
                    0.1,
                    1),
                width * height)
            .ToArray();

        PrismPremultipliedColor[] result =
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        Assert.Contains(result, color =>
            Math.Abs(color.Alpha - backgroundAlpha) < 0.000001);
        Assert.Contains(result, color =>
            color.Alpha > backgroundAlpha + 0.05 &&
            color.Alpha < 0.95);
        Assert.Contains(result, color =>
            color.Alpha > 0.99);
        Assert.All(result, AssertFiniteAssociated);
    }

    [Fact]
    public void MosaicPreserveEdgesUsesBilateralCellRepresentative()
    {
        const int width = 6;
        const int height = 3;
        const double alpha = 0.4;
        double[] red =
        [
            0.2,
            0.4,
            0.6,
            0.8,
            0,
            0
        ];
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                source[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        red[x],
                        0,
                        x >= 4 ? 1 : 0,
                        alpha);
            }
        }
        PrismCatalogFilterPlan baseline = CreatePlan(
            PrismFilterId.Mosaic,
            configure: (state, entry) =>
                SetVector(
                    state,
                    entry,
                    "CellSize",
                    new Vector4(width, height, 0, 0)));
        PrismCatalogFilterPlan preserveEdges = CreatePlan(
            PrismFilterId.Mosaic,
            configure: (state, entry) =>
            {
                SetVector(
                    state,
                    entry,
                    "CellSize",
                    new Vector4(width, height, 0, 0));
                SetBoolean(
                    state,
                    entry,
                    "PreserveEdges",
                    true);
            });

        Assert.Equal(
            1,
            preserveEdges.GetOption("PreserveEdges").X);
        PrismPremultipliedColor[] baselineResult =
            PrismCatalogFilterMath.Apply(
                baseline,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] preservedResult =
            PrismCatalogFilterMath.Apply(
                preserveEdges,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        Assert.False(baselineResult.SequenceEqual(preservedResult));
        Assert.True(
            EveryBlockIsUniform(
                preservedResult,
                width,
                height,
                width,
                height));
        Assert.All(
            preservedResult,
            color =>
            {
                Assert.Equal(alpha, color.Alpha, 6);
                Assert.InRange(
                    color.Red / color.Alpha,
                    0.35,
                    0.75);
                Assert.InRange(
                    color.Blue / color.Alpha,
                    0,
                    0.05);
                AssertFiniteAssociated(color);
            });
    }

    [Fact]
    public void FragmentPlansItsOffsetInDeviceSpaceAndExpandsBounds()
    {
        PrismCatalogFilterPlan plan = PrismCatalogFilterPlanner.Create(
            PrismFilterId.Fragment,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Number,
                    numberValue: 1.5f)
            ],
            PrismBlendMode.Normal,
            pixelScale: 2,
            effectiveTransform: Matrix3x2.CreateScale(3),
            new DrawRect(0, 0, 10, 8));

        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);

        Assert.False(pass.IsNoOp);
        Assert.Equal(9, pass.RadiusX);
        Assert.Equal(9, pass.RadiusY);
        Assert.Equal(4.5f, pass.BoundsRadiusX);
        Assert.Equal(4.5f, pass.BoundsRadiusY);
    }

    [Fact]
    public void FragmentUsesBilinearSamplingForFractionalOffsets()
    {
        const int size = 3;
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[size * size];
        source[(1 * size) + 1] =
            PrismPremultipliedColor.FromStraight(1, 1, 1, 1);
        PrismCatalogFilterPlan plan = CreatePlan(
            PrismFilterId.Fragment,
            new DrawRect(0, 0, size, size),
            (state, entry) =>
                SetNumber(
                    state,
                    entry,
                    "Offset",
                    0.5f));

        PrismPremultipliedColor[] result =
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor center =
            result[(1 * size) + 1];

        Assert.Equal(0.25, center.Red, 6);
        Assert.Equal(0.25, center.Green, 6);
        Assert.Equal(0.25, center.Blue, 6);
        Assert.Equal(0.25, center.Alpha, 6);
        AssertFiniteAssociated(center);
    }

    [Fact]
    public void FragmentWithZeroOffsetPlansANoOp()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            PrismFilterId.Fragment,
            configure: (state, entry) =>
                SetNumber(
                    state,
                    entry,
                    "Offset",
                    0));

        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);

        Assert.True(pass.IsNoOp);
        Assert.Equal(0, pass.RadiusX);
        Assert.Equal(0, pass.RadiusY);
        Assert.Equal(0, pass.BoundsRadiusX);
        Assert.Equal(0, pass.BoundsRadiusY);
    }

    [Fact]
    public void MorphologyAndIterationFiltersPrepareRequiredPassesAndBounds()
    {
        PrismGraph morphology = CreateGraph(
            PrismFilterId.Maximum,
            new DrawRect(0, 0, 10, 8),
            (state, entry) =>
                SetNumber(
                    state,
                    entry,
                    "Radius",
                    3));
        PrismGraphNode[] morphologyNodes = morphology.Nodes
            .Where(node =>
                node.Kind == PrismGraphNodeKind.Filter &&
                node.Filter == PrismFilterId.Maximum)
            .OrderBy(node =>
                node.CatalogFilterPassIndex)
            .ToArray();
        PrismCatalogFilterPlan morphologyPlan =
            Assert.IsType<PrismCatalogFilterPlan>(
                morphologyNodes[0].CatalogFilterPlan);

        Assert.Single(morphologyNodes);
        PrismCatalogFilterPass morphologyPass = Assert.Single(
            morphologyPlan.Passes);
        Assert.Equal(
            PrismCatalogFilterPassKind.Direct,
            morphologyPass.Kind);
        Assert.Equal(3, morphologyPass.RadiusX);
        Assert.Equal(3, morphologyPass.RadiusY);
        Assert.Equal(3, morphologyPass.BoundsRadiusX);
        Assert.Equal(3, morphologyPass.BoundsRadiusY);

        PrismGraph facet = CreateGraph(
            PrismFilterId.Facet,
            new DrawRect(0, 0, 10, 8),
            (state, entry) =>
                SetNumber(
                    state,
                    entry,
                    "Iterations",
                    3));
        PrismGraphNode[] facetNodes = facet.Nodes
            .Where(node =>
                node.Kind == PrismGraphNodeKind.Filter &&
                node.Filter == PrismFilterId.Facet)
            .OrderBy(node =>
                node.CatalogFilterPassIndex)
            .ToArray();
        PrismCatalogFilterPlan facetPlan =
            Assert.IsType<PrismCatalogFilterPlan>(
                facetNodes[0].CatalogFilterPlan);

        Assert.Equal(3, facetNodes.Length);
        Assert.Equal([0, 1, 2], facetPlan.Passes
            .Select(pass => pass.Iteration));
        Assert.All(
            facetPlan.Passes,
            pass =>
            {
                Assert.Equal(
                    PrismCatalogFilterPassKind.Iteration,
                    pass.Kind);
                Assert.Equal(6, pass.RadiusX);
                Assert.Equal(6, pass.RadiusY);
                Assert.Equal(6, pass.BoundsRadiusX);
                Assert.Equal(6, pass.BoundsRadiusY);
            });
    }

    [Fact]
    public void FacetFlattensRegionsWithoutBleedingAcrossHardEdges()
    {
        const int width = 11;
        const int height = 7;
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float variation = ((x + y) & 1) == 0
                    ? 0.08f
                    : -0.08f;
                source[(y * width) + x] = x < 5
                    ? PrismPremultipliedColor.FromStraight(
                        0.8f + variation,
                        0.1f,
                        0.1f,
                        1)
                    : PrismPremultipliedColor.FromStraight(
                        0.1f,
                        0.1f,
                        0.8f + variation,
                        1);
            }
        }
        PrismCatalogFilterPlan plan =
            CreatePlan(PrismFilterId.Facet);

        PrismPremultipliedColor[] result =
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        PrismPremultipliedColor leftEdge =
            result[(3 * width) + 4];
        PrismPremultipliedColor rightEdge =
            result[(3 * width) + 5];
        Assert.True(leftEdge.Red > leftEdge.Blue * 4);
        Assert.True(rightEdge.Blue > rightEdge.Red * 4);
        Assert.True(
            Math.Abs(result[(3 * width) + 1].Red -
                result[(3 * width) + 2].Red) <
            Math.Abs(source[(3 * width) + 1].Red -
                source[(3 * width) + 2].Red));
    }

    [Fact]
    public void FacetPreservesCenterAlphaAndIgnoresTransparentNeighbors()
    {
        const int size = 7;
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[size * size];
        source[(3 * size) + 3] =
            PrismPremultipliedColor.FromStraight(
                0.1,
                0.25,
                0.9,
                0.4);
        PrismCatalogFilterPlan plan =
            CreatePlan(PrismFilterId.Facet);

        PrismPremultipliedColor[] result =
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor center =
            result[(3 * size) + 3];

        Assert.Equal(0.4, center.Alpha, 6);
        Assert.Equal(0.04, center.Red, 6);
        Assert.Equal(0.1, center.Green, 6);
        Assert.Equal(0.36, center.Blue, 6);
        AssertFiniteAssociated(center);
    }

    [Fact]
    public void MaximumCapsRedundantRadiusAtTheSourceDiagonal()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            PrismFilterId.Maximum,
            new DrawRect(0, 0, 3, 4),
            (state, entry) =>
                SetNumber(
                    state,
                    entry,
                    "Radius",
                    float.MaxValue));

        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);
        Assert.Equal(5, pass.RadiusX);
        Assert.Equal(5, pass.RadiusY);
        Assert.Equal(5, pass.BoundsRadiusX);
        Assert.Equal(5, pass.BoundsRadiusY);
    }

    [Fact]
    public void MaximumDilatesEveryPixelInsideItsRoundFootprint()
    {
        const int size = 7;
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[size * size];
        source[(3 * size) + 3] =
            PrismPremultipliedColor.FromStraight(1, 1, 1, 1);
        PrismCatalogFilterPlan plan = CreatePlan(
            PrismFilterId.Maximum,
            new DrawRect(0, 0, size, size),
            (state, entry) =>
                SetNumber(
                    state,
                    entry,
                    "Radius",
                    2));

        PrismPremultipliedColor[] result =
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = x - 3;
                int dy = y - 3;
                double expected =
                    (dx * dx) + (dy * dy) <= 4 ? 1 : 0;
                PrismPremultipliedColor actual =
                    result[(y * size) + x];
                Assert.Equal(expected, actual.Red, 6);
                Assert.Equal(expected, actual.Green, 6);
                Assert.Equal(expected, actual.Blue, 6);
                Assert.Equal(expected, actual.Alpha, 6);
            }
        }
    }

    [Fact]
    public void MaximumMatchesDirectRoundDilationAtEdges()
    {
        const int width = 6;
        const int height = 5;
        const float radius = 2.5f;
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double alpha =
                    (((x * 3) + (y * 2)) % 7 + 1) / 8d;
                source[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        x / (double)(width - 1),
                        y / (double)(height - 1),
                        (x + y) / (double)(width + height - 2),
                        alpha);
            }
        }
        PrismCatalogFilterPlan plan = CreatePlan(
            PrismFilterId.Maximum,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
                SetNumber(
                    state,
                    entry,
                    "Radius",
                    radius));

        PrismPremultipliedColor[] result =
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        PrismPremultipliedColor[] expected =
            DirectRoundMaximum(
                source,
                width,
                height,
                radius);
        for (int index = 0; index < result.Length; index++)
        {
            Assert.Equal(expected[index].Red, result[index].Red, 6);
            Assert.Equal(expected[index].Green, result[index].Green, 6);
            Assert.Equal(expected[index].Blue, result[index].Blue, 6);
            Assert.Equal(expected[index].Alpha, result[index].Alpha, 6);
        }
    }

    [Fact]
    public void MinimumErodesEveryPixelInsideItsRoundFootprint()
    {
        const int size = 7;
        PrismPremultipliedColor white =
            PrismPremultipliedColor.FromStraight(1, 1, 1, 1);
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(white, size * size).ToArray();
        source[(3 * size) + 3] = default;
        PrismCatalogFilterPlan plan = CreatePlan(
            PrismFilterId.Minimum,
            new DrawRect(0, 0, size, size),
            (state, entry) =>
                SetNumber(
                    state,
                    entry,
                    "Radius",
                    2));

        PrismPremultipliedColor[] result =
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);

        PrismPremultipliedColor[] expected =
            DirectRoundMinimum(
                source,
                size,
                size,
                2);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MinimumMatchesDirectRoundErosionAtEdges()
    {
        const int width = 6;
        const int height = 5;
        const float radius = 2.5f;
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double alpha =
                    (((x * 3) + (y * 2)) % 7 + 1) / 8d;
                source[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        x / (double)(width - 1),
                        y / (double)(height - 1),
                        (x + y) / (double)(width + height - 2),
                        alpha);
            }
        }
        PrismCatalogFilterPlan plan = CreatePlan(
            PrismFilterId.Minimum,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
                SetNumber(
                    state,
                    entry,
                    "Radius",
                    radius));

        PrismPremultipliedColor[] result =
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        PrismPremultipliedColor[] expected =
            DirectRoundMinimum(
                source,
                width,
                height,
                radius);
        for (int index = 0; index < result.Length; index++)
        {
            Assert.Equal(expected[index].Red, result[index].Red, 6);
            Assert.Equal(expected[index].Green, result[index].Green, 6);
            Assert.Equal(expected[index].Blue, result[index].Blue, 6);
            Assert.Equal(expected[index].Alpha, result[index].Alpha, 6);
        }
    }

    [Fact]
    public void MinimumPreserveSquarenessUsesTheFullSquareFootprint()
    {
        const int size = 7;
        PrismPremultipliedColor white =
            PrismPremultipliedColor.FromStraight(1, 1, 1, 1);
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(white, size * size).ToArray();
        source[(3 * size) + 3] = default;
        PrismCatalogFilterPlan plan = CreatePlan(
            PrismFilterId.Minimum,
            new DrawRect(0, 0, size, size),
            (state, entry) =>
            {
                SetNumber(state, entry, "Radius", 2);
                SetSymbol(
                    state,
                    entry,
                    "Preserve",
                    "Squareness");
            });

        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);
        Assert.Equal(1, pass.Iteration);
        PrismPremultipliedColor[] result =
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool insideSquare =
                    Math.Abs(x - 3) <= 2 &&
                    Math.Abs(y - 3) <= 2;
                PrismPremultipliedColor expected =
                    insideSquare ? default : white;
                Assert.Equal(
                    expected,
                    result[(y * size) + x]);
            }
        }
    }

    [Fact]
    public void HalftonePatternPlansADeviceScaledProceduralScreen()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            PrismFilterId.HalftonePattern,
            configure: (state, entry) =>
            {
                SetNumber(state, entry, "Size", 4);
                SetNumber(state, entry, "Contrast", 7);
                SetSymbol(state, entry, "PatternType", "Dot");
            });

        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);

        Assert.Equal(
            PrismCatalogFilterPrimitive.Procedural,
            plan.Primitive);
        Assert.Equal(8, plan.GetOption("Size").X);
        Assert.Equal(8, plan.Options4.X);
        Assert.Equal(7, plan.GetOption("Contrast").X);
        Assert.Equal(0, plan.Options3.X);
        Assert.Equal(0, plan.GetOption("PatternType").X);
        Assert.Equal(PrismCatalogFilterPassKind.Direct, pass.Kind);
        Assert.Equal(0, pass.RadiusX);
        Assert.Equal(0, pass.RadiusY);
    }

    [Theory]
    [InlineData("Dot", 0)]
    [InlineData("Line", 1)]
    [InlineData("Circle", 2)]
    public void HalftonePatternPreservesToneAreaAndSourceAlpha(
        string patternType,
        int expectedPatternType)
    {
        const int width = 128;
        const int height = 128;
        const double alpha = 0.4;
        PrismPremultipliedColor sourcePixel =
            PrismPremultipliedColor.FromStraight(
                0.7,
                0.7,
                0.7,
                alpha);
        PrismCatalogFilterPlan plan = CreatePlan(
            PrismFilterId.HalftonePattern,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "Size", 4);
                SetNumber(state, entry, "Contrast", 0);
                SetSymbol(
                    state,
                    entry,
                    "PatternType",
                    patternType);
                SetColor(
                    state,
                    entry,
                    "Foreground",
                    new Color(255, 0, 0, 255));
                SetColor(
                    state,
                    entry,
                    "Background",
                    new Color(0, 0, 255, 255));
            });

        Assert.Equal(
            expectedPatternType,
            plan.Options3.X);
        Assert.Equal(
            expectedPatternType,
            plan.GetOption("PatternType").X);
        Assert.Equal(1, plan.GetOption("Foreground").X, 6);
        Assert.Equal(0, plan.GetOption("Foreground").Z, 6);
        Assert.Equal(0, plan.GetOption("Background").X, 6);
        Assert.Equal(1, plan.GetOption("Background").Z, 6);

        PrismPremultipliedColor[] result =
            PrismCatalogFilterMath.Apply(
                plan,
                Enumerable.Repeat(
                    sourcePixel,
                    width * height).ToArray(),
                width,
                height,
                PrismColorProfile.LinearSrgb);
        double[] ink = result
            .Select(color => color.Red / color.Alpha)
            .ToArray();

        Assert.InRange(ink.Average(), 0.25, 0.35);
        Assert.True(ink.Max() - ink.Min() > 0.5);
        Assert.All(
            result,
            color =>
            {
                Assert.Equal(alpha, color.Alpha, 6);
                AssertFiniteAssociated(color);
            });
    }

    [Fact]
    public void HalftonePatternContrastExpandsCoverageAwayFromMidtone()
    {
        const int size = 128;
        PrismPremultipliedColor sourcePixel =
            PrismPremultipliedColor.FromStraight(
                0.3,
                0.3,
                0.3,
                1);
        PrismPremultipliedColor[] source = Enumerable.Repeat(
                sourcePixel,
                size * size)
            .ToArray();

        double InkCoverage(float contrast)
        {
            PrismCatalogFilterPlan plan = CreatePlan(
                PrismFilterId.HalftonePattern,
                new DrawRect(0, 0, size, size),
                (state, entry) =>
                {
                    SetNumber(state, entry, "Size", 4);
                    SetNumber(state, entry, "Contrast", contrast);
                    SetSymbol(state, entry, "PatternType", "Dot");
                });
            return PrismCatalogFilterMath.Apply(
                    plan,
                    source,
                    size,
                    size,
                    PrismColorProfile.LinearSrgb)
                .Average(color => 1 - color.Red);
        }

        double neutral = InkCoverage(0);
        double contrasted = InkCoverage(10);

        Assert.True(contrasted > neutral + 0.1);
    }

    [Fact]
    public void HalftonePatternTypesHaveDistinctPeriodicTopologies()
    {
        const int size = 65;
        PrismPremultipliedColor sourcePixel =
            PrismPremultipliedColor.FromStraight(
                0.5,
                0.5,
                0.5,
                1);
        PrismPremultipliedColor[] source = Enumerable.Repeat(
                sourcePixel,
                size * size)
            .ToArray();

        PrismPremultipliedColor[] Apply(string patternType)
        {
            PrismCatalogFilterPlan plan = CreatePlan(
                PrismFilterId.HalftonePattern,
                new DrawRect(0, 0, size, size),
                (state, entry) =>
                {
                    SetNumber(state, entry, "Size", 4);
                    SetNumber(state, entry, "Contrast", 0);
                    SetSymbol(
                        state,
                        entry,
                        "PatternType",
                        patternType);
                });
            return PrismCatalogFilterMath.Apply(
                plan,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);
        }

        PrismPremultipliedColor[] dots = Apply("Dot");
        PrismPremultipliedColor[] lines = Apply("Line");
        PrismPremultipliedColor[] circles = Apply("Circle");

        Assert.False(dots.SequenceEqual(lines));
        Assert.False(dots.SequenceEqual(circles));
        Assert.False(lines.SequenceEqual(circles));
        for (int y = 0; y < size; y++)
        {
            double rowValue = lines[y * size].Red;
            for (int x = 1; x < size; x++)
            {
                Assert.Equal(
                    rowValue,
                    lines[(y * size) + x].Red,
                    6);
            }
        }

        const int center = size / 2;
        for (int offset = 0; offset <= center; offset++)
        {
            Assert.Equal(
                circles[(center * size) + center + offset].Red,
                circles[((center + offset) * size) + center].Red,
                6);
        }
    }

    [Fact]
    public void ColorHalftoneUsesEveryDeclaredScreenAngle()
    {
        const int size = 32;
        Vector4 defaultAngles = new(108, 162, 90, 45);
        PrismCatalogFilterPlan packedPlan = CreatePlan(
            PrismFilterId.ColorHalftone,
            configure: (state, entry) =>
            {
                SetNumber(state, entry, "MaxRadius", 4);
                SetVector(state, entry, "Angles", defaultAngles);
            });
        PrismCatalogFilterPass packedPass =
            Assert.Single(packedPlan.Passes);

        Assert.Equal(4, packedPass.RadiusX);
        Assert.Equal(4, packedPass.RadiusY);
        for (int index = 0; index < 4; index++)
        {
            float radians =
                defaultAngles[index] * (MathF.PI / 180);
            Assert.Equal(
                MathF.Cos(radians),
                packedPlan.Options2[index],
                6);
            Assert.Equal(
                MathF.Sin(radians),
                packedPlan.Options3[index],
                6);
        }
        (Vector3 Color, int AngleIndex)[] cases =
        [
            (new Vector3(0.5f, 1, 1), 0),
            (new Vector3(1, 0.5f, 1), 1),
            (new Vector3(1, 1, 0.5f), 2),
            (new Vector3(0.5f), 3)
        ];

        foreach ((Vector3 color, int angleIndex) in cases)
        {
            Vector4 changedAngles = defaultAngles;
            changedAngles[angleIndex] += 31;
            PrismCatalogFilterPlan baseline = CreatePlan(
                PrismFilterId.ColorHalftone,
                configure: (state, entry) =>
                {
                    SetNumber(state, entry, "MaxRadius", 4);
                    SetVector(state, entry, "Angles", defaultAngles);
                });
            PrismCatalogFilterPlan changed = CreatePlan(
                PrismFilterId.ColorHalftone,
                configure: (state, entry) =>
                {
                    SetNumber(state, entry, "MaxRadius", 4);
                    SetVector(state, entry, "Angles", changedAngles);
                });
            PrismPremultipliedColor pixel =
                PrismPremultipliedColor.FromStraight(
                    color.X,
                    color.Y,
                    color.Z,
                    1);
            PrismPremultipliedColor[] source =
                Enumerable.Repeat(pixel, size * size).ToArray();

            PrismPremultipliedColor[] baselineResult =
                PrismCatalogFilterMath.Apply(
                    baseline,
                    source,
                    size,
                    size,
                    PrismColorProfile.LinearSrgb);
            PrismPremultipliedColor[] changedResult =
                PrismCatalogFilterMath.Apply(
                    changed,
                    source,
                    size,
                    size,
                    PrismColorProfile.LinearSrgb);

            Assert.False(
                baselineResult.SequenceEqual(changedResult),
                $"ColorHalftone ignored Angles[{angleIndex}].");
        }
    }

    [Fact]
    public void ColorHalftoneUsesCircularAntialiasedDots()
    {
        const int size = 16;
        PrismPremultipliedColor sourcePixel =
            PrismPremultipliedColor.FromStraight(0.5, 1, 1, 1);
        PrismCatalogFilterPlan plan = CreatePlan(
            PrismFilterId.ColorHalftone,
            configure: (state, entry) =>
            {
                SetNumber(state, entry, "MaxRadius", 4);
                SetVector(state, entry, "Angles", Vector4.Zero);
            });
        PrismPremultipliedColor[] result =
            PrismCatalogFilterMath.Apply(
                plan,
                Enumerable.Repeat(sourcePixel, size * size).ToArray(),
                size,
                size,
                PrismColorProfile.LinearSrgb);

        Assert.Contains(
            result,
            color => color.Red > 0.01 && color.Red < 0.99);
        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                Assert.Equal(
                    result[(y * size) + x].Red,
                    result[(x * size) + y].Red,
                    6);
            }
        }
    }

    [Fact]
    public void ColorHalftoneProducesChromaticDotsAndPreservesAlpha()
    {
        const int size = 32;
        const double alpha = 0.4;
        PrismPremultipliedColor sourcePixel =
            PrismPremultipliedColor.FromStraight(
                0.25,
                0.55,
                0.85,
                alpha);
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(sourcePixel, size * size).ToArray();
        PrismCatalogFilterPlan plan =
            CreatePlan(PrismFilterId.ColorHalftone);

        PrismPremultipliedColor[] result =
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);

        Assert.Contains(
            result,
            color =>
                Math.Abs(color.Red - color.Green) > 0.01 ||
                Math.Abs(color.Green - color.Blue) > 0.01);
        Assert.All(
            result,
            color =>
            {
                Assert.Equal(alpha, color.Alpha, 6);
                AssertFiniteAssociated(color);
            });
    }

    [Fact]
    public void MezzotintSupportsEveryPhotoshopPatternType()
    {
        (string Type, Vector4 Pattern)[] cases =
        [
            ("FineDots", new Vector4(1, 1, 0, 0)),
            ("MediumDots", new Vector4(2, 2, 0, 0)),
            ("GrainyDots", new Vector4(1, 1, 1, 0)),
            ("CoarseDots", new Vector4(4, 4, 0, 0)),
            ("ShortLines", new Vector4(3, 1, 2, 0)),
            ("MediumLines", new Vector4(6, 1, 2, 0)),
            ("LongLines", new Vector4(9, 1, 2, 0)),
            ("ShortStrokes", new Vector4(3, 2, 3, 0)),
            ("MediumStrokes", new Vector4(6, 2, 3, 0)),
            ("LongStrokes", new Vector4(9, 2, 3, 0))
        ];

        foreach ((string type, Vector4 expected) in cases)
        {
            PrismCatalogFilterPlan plan = CreatePlan(
                PrismFilterId.Mezzotint,
                configure: (state, entry) =>
                    SetSymbol(state, entry, "Type", type));

            Assert.Equal(expected, plan.Options2);
        }
    }

    [Theory]
    [InlineData(0.25, 64)]
    [InlineData(0.5, 128)]
    [InlineData(0.75, 192)]
    public void MezzotintVoidAndClusterScreenPreservesToneAndAlpha(
        double luminance,
        int expectedWhite)
    {
        const int size = 16;
        const double alpha = 0.4;
        PrismPremultipliedColor pixel =
            PrismPremultipliedColor.FromStraight(
                luminance,
                luminance,
                luminance,
                alpha);
        PrismCatalogFilterPlan plan = CreatePlan(
            PrismFilterId.Mezzotint,
            configure: (state, entry) =>
            {
                SetSymbol(state, entry, "Type", "FineDots");
                SetInteger(state, entry, "Seed", 17);
            });

        PrismPremultipliedColor[] result =
            PrismCatalogFilterMath.Apply(
                plan,
                Enumerable.Repeat(pixel, size * size).ToArray(),
                size,
                size,
                PrismColorProfile.LinearSrgb);

        Assert.Equal(
            expectedWhite,
            result.Count(color =>
                Math.Abs(color.Red - alpha) < 0.000001));
        Assert.All(
            result,
            color =>
            {
                Assert.True(
                    Math.Abs(color.Red) < 0.000001 ||
                    Math.Abs(color.Red - alpha) < 0.000001);
                Assert.Equal(color.Red, color.Green, 6);
                Assert.Equal(color.Red, color.Blue, 6);
                Assert.Equal(alpha, color.Alpha, 6);
                AssertFiniteAssociated(color);
            });
    }

    [Fact]
    public void MezzotintSeedRepeatsAndChangesTheScreenPhase()
    {
        const int size = 24;
        PrismPremultipliedColor pixel =
            PrismPremultipliedColor.FromStraight(
                0.5,
                0.5,
                0.5,
                1);
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(pixel, size * size).ToArray();
        PrismCatalogFilterPlan seedSeven = CreatePlan(
            PrismFilterId.Mezzotint,
            configure: (state, entry) =>
            {
                SetSymbol(state, entry, "Type", "GrainyDots");
                SetInteger(state, entry, "Seed", 7);
            });
        PrismCatalogFilterPlan seedEight = CreatePlan(
            PrismFilterId.Mezzotint,
            configure: (state, entry) =>
            {
                SetSymbol(state, entry, "Type", "GrainyDots");
                SetInteger(state, entry, "Seed", 8);
            });

        PrismPremultipliedColor[] first =
            PrismCatalogFilterMath.Apply(
                seedSeven,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] repeated =
            PrismCatalogFilterMath.Apply(
                seedSeven,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] changed =
            PrismCatalogFilterMath.Apply(
                seedEight,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);

        Assert.Equal(first, repeated);
        Assert.False(first.SequenceEqual(changed));
    }

    [Fact]
    public void MezzotintLinesAndStrokesUseTheirDeclaredFootprints()
    {
        const int size = 32;
        PrismPremultipliedColor pixel =
            PrismPremultipliedColor.FromStraight(
                0.5,
                0.5,
                0.5,
                1);
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(pixel, size * size).ToArray();
        PrismCatalogFilterPlan lines = CreatePlan(
            PrismFilterId.Mezzotint,
            configure: (state, entry) =>
            {
                SetSymbol(state, entry, "Type", "MediumLines");
                SetInteger(state, entry, "Seed", 3);
            });
        PrismCatalogFilterPlan strokes = CreatePlan(
            PrismFilterId.Mezzotint,
            configure: (state, entry) =>
            {
                SetSymbol(state, entry, "Type", "MediumStrokes");
                SetInteger(state, entry, "Seed", 3);
            });

        PrismPremultipliedColor[] lineResult =
            PrismCatalogFilterMath.Apply(
                lines,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] strokeResult =
            PrismCatalogFilterMath.Apply(
                strokes,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);

        Assert.True(
            EveryBlockIsUniform(lineResult, size, size, 6, 1) ||
            EveryBlockIsUniform(lineResult, size, size, 1, 6));
        Assert.True(
            EveryBlockIsUniform(strokeResult, size, size, 6, 2) ||
            EveryBlockIsUniform(strokeResult, size, size, 2, 6));
        Assert.False(lineResult.SequenceEqual(strokeResult));
    }

    [Fact]
    public void ChainedCatalogFiltersPreserveDeclaredOrder()
    {
        PrismPremultipliedColor[] source = SampleImage();
        PrismCatalogFilterPlan halftone =
            CreatePlan(PrismFilterId.ColorHalftone);
        PrismCatalogFilterPlan solarize =
            CreatePlan(PrismFilterId.Solarize);

        PrismPremultipliedColor[] halftoneThenSolarize =
            PrismCatalogFilterMath.Apply(
                solarize,
                PrismCatalogFilterMath.Apply(
                    halftone,
                    source,
                    4,
                    4,
                    PrismColorProfile.LinearSrgb),
                4,
                4,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] solarizeThenHalftone =
            PrismCatalogFilterMath.Apply(
                halftone,
                PrismCatalogFilterMath.Apply(
                    solarize,
                    source,
                    4,
                    4,
                    PrismColorProfile.LinearSrgb),
                4,
                4,
                PrismColorProfile.LinearSrgb);

        Assert.False(
            halftoneThenSolarize.SequenceEqual(
                solarizeThenHalftone));
    }

    [Fact]
    public void FindEdgesUsesNormalizedScharrGradientAndPreservesAlpha()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            PrismFilterId.FindEdges,
            configure: (state, entry) =>
                SetNumber(state, entry, "Threshold", 0));
        PrismPremultipliedColor[] source = Enumerable
            .Repeat(
                PrismPremultipliedColor.FromStraight(0, 0, 0, 0.5),
                9)
            .ToArray();
        source[5] = PrismPremultipliedColor.FromStraight(
            0.04,
            0.04,
            0.04,
            0.5);

        PrismPremultipliedColor center =
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                3,
                3,
                PrismColorProfile.LinearSrgb)[4];

        Assert.InRange(center.Red, 0.44999, 0.45001);
        Assert.InRange(center.Green, 0.44999, 0.45001);
        Assert.InRange(center.Blue, 0.44999, 0.45001);
        Assert.Equal(0.5, center.Alpha, 12);
    }

    [Fact]
    public void CatalogFilterStaysInsideGroupMaskClippingAndBlendBoundaries()
    {
        PrismLayerDefinition clipped = new(
            new PrismNodeId(11),
            "Filtered clipped layer",
            filters:
            [
                new PrismFilterDefinition(
                    PrismFilterId.FindEdges)
            ],
            clipToBelow: true,
            blendMode: PrismBlendMode.Multiply);
        PrismLayerDefinition clipBase =
            PrismTestData.Layer(12, "Clip base");
        PrismGroupDefinition group = new(
            new PrismNodeId(10),
            "Isolated filtered group",
            [clipped, clipBase],
            mask: new PrismMaskDefinition(
                new PrismResourceId("catalog-mask")),
            blendMode: PrismBlendMode.Normal);
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "Catalog filter boundaries",
                group),
            bounds: new DrawRect(0, 0, 20, 10));

        PrismGraph graph = BuildGraph(scope);

        PrismGraphNode filter = Assert.Single(
            graph.Nodes.Where(node =>
                node.Kind == PrismGraphNodeKind.Filter &&
                node.DefinitionNodeId == clipped.Id &&
                node.Filter == PrismFilterId.FindEdges));
        PrismGraphNode groupNode = Assert.Single(
            graph.Nodes.Where(node =>
                node.Kind == PrismGraphNodeKind.Group &&
                node.DefinitionNodeId == group.Id));
        PrismGraphNode mask = Assert.Single(
            graph.Nodes.Where(node =>
                node.Kind == PrismGraphNodeKind.Mask &&
                node.DefinitionNodeId == group.Id));
        PrismGraphNode maskComposite = Assert.Single(
            graph.Nodes.Where(node =>
                node.Kind == PrismGraphNodeKind.Composite &&
                node.DefinitionNodeId == group.Id &&
                graph.Edges.Any(edge =>
                    edge.Source == mask.Id &&
                    edge.Target == node.Id &&
                    edge.Kind ==
                        PrismGraphEdgeKind.MaskAlpha)));
        PrismGraphNode clipping = Assert.Single(
            graph.Nodes.Where(node =>
                node.Kind ==
                    PrismGraphNodeKind.ClipToBelow &&
                node.DefinitionNodeId == clipped.Id));
        PrismGraphNode composite = Assert.Single(
            graph.Nodes.Where(node =>
                node.Kind == PrismGraphNodeKind.Composite &&
                node.DefinitionNodeId == clipped.Id));

        Assert.NotNull(filter.CatalogFilterPlan);
        Assert.True(groupNode.IsIsolationBoundary);
        Assert.Equal(
            PrismBlendMode.Multiply,
            composite.BlendMode);
        Assert.True(
            HasDirectedPath(
                graph,
                filter.Id,
                clipping.Id));
        Assert.Contains(
            graph.Edges,
            edge =>
                edge.Source == groupNode.Id &&
                edge.Target == maskComposite.Id &&
                edge.Kind == PrismGraphEdgeKind.Content);
        Assert.Contains(
            graph.Edges,
            edge =>
                edge.Source == mask.Id &&
                edge.Target == maskComposite.Id &&
                edge.Kind == PrismGraphEdgeKind.MaskAlpha);
    }

    [Fact]
    public void ConformanceGalleryComesFromTheCatalogList()
    {
        PrismCatalogEntryDescriptor[] entries = CatalogEntries();
        PrismFilterConformanceGalleryEntry[] gallery =
            PrismFilterConformanceGallery.Entries.ToArray();

        Assert.Equal(entries.Length, gallery.Length);
        Assert.Equal(
            entries.Select(entry => entry.Symbol),
            gallery.Select(entry => entry.Symbol));
        foreach (PrismFilterConformanceGalleryEntry item in
            gallery)
        {
            PrismLayerDefinition layer =
                Assert.IsType<PrismLayerDefinition>(
                    Assert.Single(
                        item.Composition.Nodes));
            PrismFilterDefinition filter =
                Assert.Single(layer.Filters);

            Assert.Equal(item.Filter, filter.Filter);
            Assert.Equal(
                $"PrismFilterConformance.{item.Symbol}",
                item.Composition.Name);
        }
    }

    private static void AssertParameterPacking(
        PrismCatalogEntryDescriptor entry,
        System.Collections.Immutable.ImmutableArray<
            PrismGraphParameter> parameters,
        PrismCatalogFilterPlan plan)
    {
        PrismFilterId filter =
            (PrismFilterId)entry.StableId;
        PrismFilterParameterReader reader =
            new(filter, parameters);
        PrismCatalogPropertyDescriptor[] resources =
            entry.Properties
                .Where(property =>
                    property.ValueType ==
                        PrismCatalogValueType.Resource)
                .ToArray();

        foreach (PrismCatalogPropertyDescriptor property in
            entry.Properties)
        {
            PrismGraphParameter parameter =
                parameters[property.Slot];
            Assert.Equal(
                ExpectedKind(property.ValueType),
                parameter.Kind);
            Assert.Equal(property.Slot, parameter.Index);
            if (property.ValueType ==
                PrismCatalogValueType.Resource)
            {
                continue;
            }

            Vector4 expected = (filter, property.Name) switch
            {
                (PrismFilterId.MosaicTiles, "TileSize") =>
                    new Vector4(
                        Math.Clamp(parameter.NumberValue, 1, 16384),
                        0,
                        0,
                        0),
                (PrismFilterId.MosaicTiles, "GroutWidth") =>
                    new Vector4(
                        Math.Clamp(parameter.NumberValue, 0, 16384),
                        0,
                        0,
                        0),
                (PrismFilterId.MosaicTiles, "LightenGrout") =>
                    new Vector4(
                        Math.Clamp(parameter.NumberValue / 10, 0, 1),
                        0,
                        0,
                        0),
                (PrismFilterId.Patchwork, "SquareSize") =>
                    new Vector4(
                        Math.Clamp(parameter.NumberValue, 1, 16384),
                        0,
                        0,
                        0),
                (PrismFilterId.Patchwork, "Relief") =>
                    new Vector4(
                        Math.Clamp(parameter.NumberValue / 50, 0, 1),
                        0,
                        0,
                        0),
                (PrismFilterId.HalftonePattern, "Size") =>
                    new Vector4(
                        Math.Clamp(
                            MathF.Max(0, parameter.NumberValue) * 2,
                            2,
                            16384),
                        0,
                        0,
                        0),
                (PrismFilterId.HalftonePattern, "PatternType") =>
                    new Vector4(
                        reader.SymbolCode(
                            "PatternType",
                            ("Dot", 0),
                            ("Line", 1),
                            ("Circle", 2)),
                        0,
                        0,
                        0),
                (PrismFilterId.GraphicPen, "StrokeLength") =>
                    new Vector4(
                        Math.Clamp(
                            MathF.Abs(parameter.NumberValue),
                            1,
                            96),
                        0,
                        0,
                        0),
                (PrismFilterId.GraphicPen, "StrokeDirection") =>
                    new Vector4(
                        reader.SymbolCode(
                            "StrokeDirection",
                            ("RightDiagonal", 0),
                            ("Horizontal", 1),
                            ("LeftDiagonal", 2),
                            ("Vertical", 3)),
                        0,
                        0,
                        0),
                (PrismFilterId.SprayedStrokes, "Direction") =>
                    new Vector4(
                        reader.SymbolCode(
                            "Direction",
                            ("RightDiagonal", 0),
                            ("Horizontal", 1),
                            ("LeftDiagonal", 2),
                            ("Vertical", 3)),
                        0,
                        0,
                        0),
                (PrismFilterId.Wind, "Direction") =>
                    new Vector4(
                        reader.SymbolCode(
                            "Direction",
                            ("FromRight", 0),
                            ("FromLeft", 1)),
                        0,
                        0,
                        0),
                (PrismFilterId.Wind, "Method") =>
                    new Vector4(
                        reader.SymbolCode(
                            "Method",
                            ("Wind", 0),
                            ("Blast", 1),
                            ("Stagger", 2)),
                        0,
                        0,
                        0),
                (PrismFilterId.Deinterlace, "Field") =>
                    new Vector4(
                        reader.SymbolCode(
                            "Field",
                            ("Even", 0),
                            ("Odd", 1)),
                        0,
                        0,
                        0),
                (PrismFilterId.Deinterlace, "Replacement") =>
                    new Vector4(
                        reader.SymbolCode(
                            "Replacement",
                            ("Interpolation", 0),
                            ("Duplication", 1),
                            ("Duplicate", 1)),
                        0,
                        0,
                        0),
                (PrismFilterId.NtscColors, "Standard") =>
                    new Vector4(
                        reader.SymbolCode(
                            "Standard",
                            ("NTSC", 0)),
                        0,
                        0,
                        0),
                (PrismFilterId.NtscColors, "Method") =>
                    new Vector4(
                        reader.SymbolCode(
                            "Method",
                            ("ReduceLuminance", 0)),
                        0,
                        0,
                        0),
                (PrismFilterId.PaintDaubs, "BrushType") =>
                    new Vector4(
                        reader.SymbolCode(
                            "BrushType",
                            ("Simple", 0),
                            ("LightRough", 1),
                            ("DarkRough", 2),
                            ("WideSharp", 3),
                            ("WideBlurry", 4),
                            ("Sparkle", 5)),
                        0,
                        0,
                        0),
                (PrismFilterId.Extrude, "Type") =>
                    new Vector4(
                        reader.SymbolCode(
                            "Type",
                            ("Blocks", 0),
                            ("Pyramids", 1)),
                        0,
                        0,
                        0),
                (PrismFilterId.Extrude, "DepthMode") =>
                    new Vector4(
                        reader.SymbolCode(
                            "DepthMode",
                            ("Random", 0),
                            ("Level", 1)),
                        0,
                        0,
                        0),
                (PrismFilterId.CustomConvolution, "EdgeMode") =>
                    new Vector4(
                        reader.SymbolCode(
                            "EdgeMode",
                            ("Clamp", 0),
                            ("Transparent", 1),
                            ("Wrap", 2),
                            ("Mirror", 3),
                            ("Reflect", 3)),
                        0,
                        0,
                        0),
                (PrismFilterId.ConteCrayon or
                    PrismFilterId.RoughPastels or
                    PrismFilterId.Texturizer or
                    PrismFilterId.Underpainting, "Texture") =>
                    new Vector4(
                        reader.SymbolCode(
                            "Texture",
                            ("Canvas", 0),
                            ("Brick", 1),
                            ("Burlap", 2),
                            ("Sandstone", 3)),
                        0,
                        0,
                        0),
                (PrismFilterId.BasRelief or
                    PrismFilterId.ConteCrayon or
                    PrismFilterId.RoughPastels or
                    PrismFilterId.Texturizer or
                    PrismFilterId.Underpainting, "LightDirection") =>
                    new Vector4(
                        reader.SymbolCode(
                            "LightDirection",
                            ("Top", 0),
                            ("TopRight", 1),
                            ("Right", 2),
                            ("BottomRight", 3),
                            ("Bottom", 4),
                            ("BottomLeft", 5),
                            ("Left", 6),
                            ("TopLeft", 7)),
                        0,
                        0,
                        0),
                _ => property.ValueType switch
                {
                    PrismCatalogValueType.Boolean =>
                        new Vector4(
                            parameter.BooleanValue ? 1 : 0,
                            0,
                            0,
                            0),
                    PrismCatalogValueType.Integer or
                        PrismCatalogValueType.Symbol =>
                        PackInteger(parameter.IntegerValue),
                    PrismCatalogValueType.Number =>
                        new Vector4(
                            parameter.NumberValue,
                            0,
                            0,
                            0),
                    PrismCatalogValueType.Color =>
                        reader.Color(property.Name),
                    PrismCatalogValueType.Vector =>
                        parameter.VectorValue,
                    _ => throw new InvalidOperationException(
                        $"Unexpected catalog type {property.ValueType}.")
                }
            };
            Vector4 actual = plan.GetOption(property.Slot);
            Assert.True(
                expected == actual,
                $"{entry.Symbol}.{property.Name}: expected {expected}, " +
                $"actual {actual}.");
        }

        if (resources.Length > 0)
        {
            Assert.Equal(
                parameters[resources[0].Slot]
                    .ResourceValue,
                plan.PrimaryResource);
            Assert.Equal(
                resources[0].Required,
                plan.PrimaryResourceRequired);
        }
        if (resources.Length > 1)
        {
            Assert.Equal(
                parameters[resources[1].Slot]
                    .ResourceValue,
                plan.AuxiliaryResource);
            Assert.Equal(
                resources[1].Required,
                plan.AuxiliaryResourceRequired);
        }
    }

    private static bool HasDirectedPath(
        PrismGraph graph,
        PrismGraphNodeId source,
        PrismGraphNodeId target)
    {
        Queue<PrismGraphNodeId> pending = new([source]);
        HashSet<PrismGraphNodeId> visited = [source];
        while (pending.TryDequeue(out PrismGraphNodeId current))
        {
            foreach (PrismGraphEdge edge in graph.Edges)
            {
                if (edge.Source != current ||
                    !visited.Add(edge.Target))
                {
                    continue;
                }
                if (edge.Target == target)
                {
                    return true;
                }
                pending.Enqueue(edge.Target);
            }
        }
        return false;
    }

    private static PrismGraphParameterValueKind ExpectedKind(
        PrismCatalogValueType valueType) =>
        valueType switch
        {
            PrismCatalogValueType.Boolean =>
                PrismGraphParameterValueKind.Boolean,
            PrismCatalogValueType.Integer =>
                PrismGraphParameterValueKind.Integer,
            PrismCatalogValueType.Number =>
                PrismGraphParameterValueKind.Number,
            PrismCatalogValueType.Color =>
                PrismGraphParameterValueKind.Color,
            PrismCatalogValueType.Vector =>
                PrismGraphParameterValueKind.Vector,
            PrismCatalogValueType.Symbol =>
                PrismGraphParameterValueKind.Symbol,
            PrismCatalogValueType.Resource =>
                PrismGraphParameterValueKind.Resource,
            _ => throw new InvalidOperationException(
                $"Unexpected catalog type {valueType}.")
        };

    private static Vector4 PackInteger(int value)
    {
        uint bits = unchecked((uint)value);
        return new Vector4(
            bits & 0xffffu,
            bits >> 16,
            0,
            0);
    }

    private static PrismPremultipliedColor[] SampleImage()
    {
        PrismPremultipliedColor[] result =
            new PrismPremultipliedColor[16];
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                double alpha =
                    ((x + y) % 4) / 3d;
                result[(y * 4) + x] =
                    PrismPremultipliedColor.FromStraight(
                        x / 3d,
                        y / 3d,
                        (x + y) / 6d,
                        alpha);
            }
        }
        return result;
    }

    private static PrismGraph BuildAllGraph(
        PrismCatalogEntryDescriptor[] entries)
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "All remaining catalog filters",
            filters: entries.Select(entry =>
                new PrismFilterDefinition(
                    (PrismFilterId)entry.StableId)));
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "All remaining catalog filters",
                layer),
            bounds: new DrawRect(0, 0, 64, 48));
        PrismLayerState state =
            scope.Instance.GetLayerState(layer.Id);
        for (int index = 0; index < entries.Length; index++)
        {
            ConfigureRequiredResources(
                state.Filters[index],
                entries[index]);
        }
        return BuildGraph(scope);
    }

    private static PrismCatalogFilterPlan CreatePlan(
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
        return Assert.IsType<PrismCatalogFilterPlan>(
            graph.Nodes.First(node =>
                node.Kind == PrismGraphNodeKind.Filter &&
                node.Filter == filter)
                .CatalogFilterPlan);
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
                    $"catalog-{entry.Symbol}-{property.Name}"));
        }
    }

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

    private static void SetBoolean(
        PrismFilterState state,
        PrismCatalogEntryDescriptor entry,
        string name,
        bool value) =>
        GeneratedMarkup.SetPrismFilterBoolean(
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
            PrismCatalogRuntime.ResolveSymbol(name, value));

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

    private static PrismCatalogPropertyDescriptor Property(
        PrismCatalogEntryDescriptor entry,
        string name) =>
        entry.Properties.Single(property =>
            property.Name == name);

    private static PrismCatalogEntryDescriptor[]
        CatalogEntries() =>
        PrismCatalogGenerated.Entries
            .Where(entry =>
            {
                if (entry.Kind != "filter")
                {
                    return false;
                }

                PrismFilterId filter =
                    (PrismFilterId)entry.StableId;
                return
                    !PrismAdjustmentPlanner.IsSupported(filter) &&
                    !PrismNeighborhoodPlanner.IsSupported(filter) &&
                    !PrismResamplingPlanner.IsSupported(filter);
            })
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

    private static PrismPremultipliedColor[] DirectRoundMinimum(
        PrismPremultipliedColor[] source,
        int width,
        int height,
        float radius)
    {
        PrismPremultipliedColor[] result =
            new PrismPremultipliedColor[source.Length];
        int extent = (int)MathF.Ceiling(radius);
        float squaredRadius = radius * radius;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                PrismPremultipliedColor minimum =
                    PrismPremultipliedColor.FromStraight(1, 1, 1, 1);
                for (int dy = -extent; dy <= extent; dy++)
                {
                    for (int dx = -extent; dx <= extent; dx++)
                    {
                        if ((dx * dx) + (dy * dy) >
                            squaredRadius)
                        {
                            continue;
                        }

                        PrismPremultipliedColor candidate =
                            source[
                                (Math.Clamp(y + dy, 0, height - 1) *
                                    width) +
                                Math.Clamp(x + dx, 0, width - 1)];
                        minimum = new PrismPremultipliedColor(
                            Math.Min(minimum.Red, candidate.Red),
                            Math.Min(minimum.Green, candidate.Green),
                            Math.Min(minimum.Blue, candidate.Blue),
                            Math.Min(minimum.Alpha, candidate.Alpha));
                    }
                }
                result[(y * width) + x] = minimum;
            }
        }
        return result;
    }

    private static PrismPremultipliedColor[] DirectRoundMaximum(
        PrismPremultipliedColor[] source,
        int width,
        int height,
        float radius)
    {
        PrismPremultipliedColor[] result =
            new PrismPremultipliedColor[source.Length];
        int extent = (int)MathF.Ceiling(radius);
        float squaredRadius = radius * radius;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                PrismPremultipliedColor maximum = default;
                for (int dy = -extent; dy <= extent; dy++)
                {
                    for (int dx = -extent; dx <= extent; dx++)
                    {
                        if ((dx * dx) + (dy * dy) >
                            squaredRadius)
                        {
                            continue;
                        }

                        PrismPremultipliedColor candidate =
                            source[
                                (Math.Clamp(y + dy, 0, height - 1) *
                                    width) +
                                Math.Clamp(x + dx, 0, width - 1)];
                        maximum = new PrismPremultipliedColor(
                            Math.Max(maximum.Red, candidate.Red),
                            Math.Max(maximum.Green, candidate.Green),
                            Math.Max(maximum.Blue, candidate.Blue),
                            Math.Max(maximum.Alpha, candidate.Alpha));
                    }
                }
                result[(y * width) + x] = maximum;
            }
        }
        return result;
    }

    private static bool EveryBlockIsUniform(
        PrismPremultipliedColor[] pixels,
        int width,
        int height,
        int blockWidth,
        int blockHeight)
    {
        for (int blockY = 0; blockY < height; blockY += blockHeight)
        {
            for (int blockX = 0; blockX < width; blockX += blockWidth)
            {
                PrismPremultipliedColor expected =
                    pixels[(blockY * width) + blockX];
                int endY = Math.Min(blockY + blockHeight, height);
                int endX = Math.Min(blockX + blockWidth, width);
                for (int y = blockY; y < endY; y++)
                {
                    for (int x = blockX; x < endX; x++)
                    {
                        if (pixels[(y * width) + x] != expected)
                        {
                            return false;
                        }
                    }
                }
            }
        }
        return true;
    }

    private static int MinimumPointillizeDistanceSquared(
        int[] ranks,
        int prefixLength)
    {
        int minimum = int.MaxValue;
        int[] selected = Enumerable.Range(0, ranks.Length)
            .Where(index => ranks[index] < prefixLength)
            .ToArray();
        for (int first = 0; first < selected.Length; first++)
        {
            for (int second = first + 1;
                second < selected.Length;
                second++)
            {
                int horizontal = Math.Abs(
                    (selected[first] % 16) -
                    (selected[second] % 16));
                int vertical = Math.Abs(
                    (selected[first] / 16) -
                    (selected[second] / 16));
                horizontal = Math.Min(
                    horizontal,
                    16 - horizontal);
                vertical = Math.Min(
                    vertical,
                    16 - vertical);
                minimum = Math.Min(
                    minimum,
                    (horizontal * horizontal) +
                    (vertical * vertical));
            }
        }
        return minimum;
    }

    private static double AverageAdjacentDifference(
        PrismPremultipliedColor[] pixels,
        int width,
        int height,
        bool horizontal)
    {
        double total = 0;
        int count = 0;
        int maximumX = horizontal ? width - 1 : width;
        int maximumY = horizontal ? height : height - 1;
        for (int y = 0; y < maximumY; y++)
        {
            for (int x = 0; x < maximumX; x++)
            {
                int neighborX = horizontal ? x + 1 : x;
                int neighborY = horizontal ? y : y + 1;
                total += Math.Abs(
                    pixels[(y * width) + x].Red -
                    pixels[(neighborY * width) + neighborX].Red);
                count++;
            }
        }
        return total / count;
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

    private static (double Chrominance, double CompositePeak) NtscSignal(
        PrismPremultipliedColor color)
    {
        double inverseAlpha = color.Alpha > 0
            ? 1 / color.Alpha
            : 0;
        double red = EncodeNtscGamma(color.Red * inverseAlpha);
        double green = EncodeNtscGamma(color.Green * inverseAlpha);
        double blue = EncodeNtscGamma(color.Blue * inverseAlpha);
        double luminance =
            (0.2989 * red) +
            (0.5866 * green) +
            (0.1144 * blue);
        double inPhase =
            (0.5959 * red) -
            (0.2741 * green) -
            (0.3218 * blue);
        double quadrature =
            (0.2113 * red) -
            (0.5227 * green) +
            (0.3113 * blue);
        double chrominance = Math.Sqrt(
            (inPhase * inPhase) +
            (quadrature * quadrature));
        const double pedestalIre = 7.5;
        const double activeVideoIre = 100 - pedestalIre;
        return (
            activeVideoIre * chrominance,
            pedestalIre +
                (activeVideoIre * (luminance + chrominance)));
    }

    private static double EncodeNtscGamma(double value) =>
        Math.Pow(value, 1 / 2.2);

    private static PrismLensProfileResource TestLensProfile()
    {
        PrismSparsePolynomial Constant(float value) =>
            new(
            [
                new PrismSparsePolynomialTerm(
                    value,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0)
            ]);
        PrismSparsePolynomial PupilX(float scale) =>
            new(
            [
                new PrismSparsePolynomialTerm(
                    scale,
                    1,
                    0,
                    0,
                    0,
                    0,
                    0)
            ]);
        PrismSparsePolynomial PupilY(float scale) =>
            new(
            [
                new PrismSparsePolynomialTerm(
                    scale,
                    0,
                    1,
                    0,
                    0,
                    0,
                    0)
            ]);

        PrismLensFlarePolynomialRegion region = new(
            0,
            61,
            PupilX(0.5f),
            PupilY(0.5f),
            PupilX(0.4f),
            PupilY(0.4f),
            Constant(0.2f),
            Constant(0.5f));
        return new PrismLensProfileResource(
        [
            new PrismLensFlareGhost([region])
        ],
        pupilGridSize: 4);
    }

    private static PrismLightingResource TestLighting() =>
        new(
        [
            PrismLight.Directional(
                Vector3.UnitZ,
                Vector3.One)
        ]);
}
