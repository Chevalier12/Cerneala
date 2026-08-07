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

public sealed class PrismNeighborhoodFilterTests
{
    [Fact]
    public void CatalogDrivesEveryNeighborhoodPlannerKernelTestAndDocumentation()
    {
        PrismCatalogEntryDescriptor[] entries =
            NeighborhoodEntries();
        PrismFilterId[] filters = entries
            .Select(entry => (PrismFilterId)entry.StableId)
            .ToArray();
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "All neighborhood filters",
            filters: filters.Select(
                filter => new PrismFilterDefinition(filter)));
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "Neighborhood defaults",
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

        Assert.Equal(27, entries.Length);
        Assert.Equal(
            entries.Length,
            nodes
                .Select(node =>
                    Assert.IsType<PrismNeighborhoodPlan>(
                        node.NeighborhoodPlan)
                        .Operation)
                .Distinct()
                .Count());
        foreach (PrismCatalogEntryDescriptor entry in entries)
        {
            PrismFilterId filter =
                (PrismFilterId)entry.StableId;
            PrismGraphNode[] filterNodes = nodes
                .Where(node => node.Filter == filter)
                .ToArray();

            Assert.NotEmpty(filterNodes);
            Assert.True(
                PrismNeighborhoodPlanner.IsSupported(filter));
            Assert.Equal(
                $"PrismKernelRegistry/{entry.Symbol}",
                entry.Coverage.Kernel);
            Assert.Equal(
                $"PrismNeighborhoodFilterTests/{entry.Symbol}",
                entry.Coverage.Test);
            Assert.StartsWith(
                "generated:",
                entry.Coverage.Documentation,
                StringComparison.Ordinal);
            Assert.All(
                filterNodes,
                node =>
                {
                    Assert.Equal(
                        entry.Properties.Length,
                        node.Parameters.Length);
                    Assert.Equal(
                        Enumerable.Range(
                            0,
                            entry.Properties.Length),
                        node.Parameters.Select(
                            parameter => parameter.Index));
                    PrismNeighborhoodPlan prepared =
                        Assert.IsType<PrismNeighborhoodPlan>(
                            node.NeighborhoodPlan);
                    Assert.Equal(filter, prepared.Filter);
                    Assert.InRange(
                        node.NeighborhoodPassIndex,
                        0,
                        prepared.Passes.Length - 1);
                });
            PrismCatalogExecutionDescriptor execution =
                Assert.IsType<PrismCatalogExecutionDescriptor>(
                    entry.Execution);
            Assert.Contains(
                execution.Primitive,
                new[]
                {
                    "convolution-neighborhood",
                    "noise-quantization-procedural"
                });
            Assert.Equal(
                "linear-premultiplied-rgba",
                execution.SurfaceFormat);
            Assert.Equal(
                "working-profile",
                execution.ColorSpace);
        }
    }

    [Fact]
    public void PlannerPreparesPassesRadiiBoundsAndQualityOnlyOnce()
    {
        PrismNeighborhoodPlan gaussian = CreatePlan(
            PrismFilterId.GaussianBlur,
            new DrawRect(0, 0, 40, 30),
            (state, entry) =>
            {
                SetNumber(state, entry, "Radius", 3);
                SetSymbol(state, entry, "Quality", "Best");
            });

        Assert.Collection(
            gaussian.Passes,
            horizontal =>
            {
                Assert.Equal(
                    PrismNeighborhoodPassKind.Horizontal,
                    horizontal.Kind);
                Assert.Equal(3, horizontal.RadiusX);
                Assert.Equal(0, horizontal.RadiusY);
                Assert.Equal(3, horizontal.BoundsRadiusX);
                Assert.Equal(0, horizontal.BoundsRadiusY);
                Assert.Equal(17, horizontal.SampleCount);
                Assert.False(horizontal.IsNoOp);
            },
            vertical =>
            {
                Assert.Equal(
                    PrismNeighborhoodPassKind.Vertical,
                    vertical.Kind);
                Assert.Equal(0, vertical.RadiusX);
                Assert.Equal(3, vertical.RadiusY);
                Assert.Equal(0, vertical.BoundsRadiusX);
                Assert.Equal(3, vertical.BoundsRadiusY);
                Assert.Equal(17, vertical.SampleCount);
                Assert.False(vertical.IsNoOp);
            });

        PrismNeighborhoodPlan tiny = CreatePlan(
            PrismFilterId.GaussianBlur,
            new DrawRect(0, 0, 1, 1));
        PrismNeighborhoodPass tinyPass =
            Assert.Single(tiny.Passes);
        Assert.True(tinyPass.IsNoOp);
        Assert.Equal(
            PrismNeighborhoodPassKind.Direct,
            tinyPass.Kind);

        PrismNeighborhoodPlan draftBlur = CreatePlan(
            PrismFilterId.Blur,
            configure: (state, entry) =>
                SetSymbol(
                    state,
                    entry,
                    "Quality",
                    "Draft"));
        Assert.Collection(
            draftBlur.Passes,
            horizontal =>
            {
                Assert.Equal(
                    PrismNeighborhoodPassKind.Horizontal,
                    horizontal.Kind);
                Assert.Equal(5, horizontal.SampleCount);
            },
            vertical =>
            {
                Assert.Equal(
                    PrismNeighborhoodPassKind.Vertical,
                    vertical.Kind);
                Assert.Equal(5, vertical.SampleCount);
            });

        PrismNeighborhoodPlan blur =
            CreatePlan(PrismFilterId.Blur);
        Assert.Equal(2, blur.Passes.Length);
        Assert.All(
            blur.Passes,
            pass => Assert.Equal(9, pass.SampleCount));

        PrismNeighborhoodPlan blurMore =
            CreatePlan(PrismFilterId.BlurMore);
        Assert.Equal(2, blurMore.Passes.Length);
        Assert.All(
            blurMore.Passes,
            pass => Assert.Equal(9, pass.SampleCount));
    }

    [Fact]
    public void AverageUsesNormalizedUniform3x3KernelWithClampEdges()
    {
        PrismNeighborhoodPlan plan = CreatePlan(
            PrismFilterId.Average,
            new DrawRect(0, 0, 3, 1));
        PrismNeighborhoodPass pass = Assert.Single(plan.Passes);

        Assert.Equal(PrismNeighborhoodPassKind.Direct, pass.Kind);
        Assert.Equal(1, pass.RadiusX);
        Assert.Equal(1, pass.RadiusY);
        Assert.Equal(9, pass.SampleCount);
        Assert.Equal(0, pass.BoundsRadiusX);
        Assert.Equal(0, pass.BoundsRadiusY);
        Assert.False(pass.IsNoOp);

        PrismPremultipliedColor black =
            PrismPremultipliedColor.FromStraight(0, 0, 0, 1);
        PrismPremultipliedColor white =
            PrismPremultipliedColor.FromStraight(1, 1, 1, 1);
        PrismPremultipliedColor[] result =
            PrismNeighborhoodMath.Apply(
                plan,
                [black, black, white],
                3,
                1,
                PrismColorProfile.LinearSrgb);

        AssertColor(result[0], black, tolerance: 0.000001);
        AssertColor(
            result[1],
            PrismPremultipliedColor.FromStraight(
                1.0 / 3,
                1.0 / 3,
                1.0 / 3,
                1),
            tolerance: 0.000001);
        AssertColor(
            result[2],
            PrismPremultipliedColor.FromStraight(
                2.0 / 3,
                2.0 / 3,
                2.0 / 3,
                1),
            tolerance: 0.000001);
        Assert.All(result, AssertFiniteAssociated);
    }

    [Fact]
    public void MedianRadiusIsStrictlyZeroOrOne()
    {
        PrismNeighborhoodPlan identity = CreatePlan(
            PrismFilterId.Median,
            new DrawRect(0, 0, 3, 3),
            (state, entry) =>
                SetInteger(state, entry, "Radius", 0));
        PrismNeighborhoodPass identityPass =
            Assert.Single(identity.Passes);

        Assert.True(identityPass.IsNoOp);
        Assert.Equal(0, identityPass.RadiusX);
        Assert.Equal(0, identityPass.RadiusY);

        PrismNeighborhoodPlan median = CreatePlan(
            PrismFilterId.Median);
        PrismNeighborhoodPass medianPass =
            Assert.Single(median.Passes);

        Assert.False(medianPass.IsNoOp);
        Assert.Equal(1, medianPass.RadiusX);
        Assert.Equal(1, medianPass.RadiusY);
        Assert.Equal(9, medianPass.SampleCount);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreatePlan(
                PrismFilterId.Median,
                configure: (state, entry) =>
                    SetInteger(state, entry, "Radius", 2)));
    }

    [Fact]
    public void MedianRanksStraightLinearLuminanceAndReturnsAssociatedCandidate()
    {
        PrismNeighborhoodPlan plan = CreatePlan(
            PrismFilterId.Median,
            new DrawRect(0, 0, 3, 3));
        double[] alpha =
            [1, 1, 1, 1, 0.2, 0.2, 0.2, 0.2, 0.2];
        PrismPremultipliedColor[] source = Enumerable
            .Range(1, 9)
            .Select(index =>
            {
                double value = index / 10.0;
                return PrismPremultipliedColor.FromStraight(
                    value,
                    value,
                    value,
                    alpha[index - 1]);
            })
            .ToArray();

        PrismPremultipliedColor[] result =
            PrismNeighborhoodMath.Apply(
                plan,
                source,
                3,
                3,
                PrismColorProfile.LinearSrgb);

        AssertColor(
            result[4],
            source[4],
            tolerance: 0.000001);
        Assert.All(result, AssertFiniteAssociated);
    }

    [Fact]
    public void MedianNetworkReturnsTheFifthRankForDeterministicPermutations()
    {
        PrismNeighborhoodPlan plan = CreatePlan(
            PrismFilterId.Median,
            new DrawRect(0, 0, 3, 3));
        PrismPremultipliedColor[] ordered = Enumerable
            .Range(1, 9)
            .Select(index =>
            {
                double value = index / 10.0;
                return PrismPremultipliedColor.FromStraight(
                    value,
                    value,
                    value,
                    1);
            })
            .ToArray();
        Random random = new(61);

        for (int iteration = 0; iteration < 512; iteration++)
        {
            PrismPremultipliedColor[] permutation =
                [.. ordered];
            for (int index = permutation.Length - 1;
                index > 0;
                index--)
            {
                int exchange = random.Next(index + 1);
                (permutation[index], permutation[exchange]) =
                    (permutation[exchange], permutation[index]);
            }

            PrismPremultipliedColor[] result =
                PrismNeighborhoodMath.Apply(
                    plan,
                    permutation,
                    3,
                    3,
                    PrismColorProfile.LinearSrgb);

            AssertColor(
                result[4],
                ordered[4],
                tolerance: 0.000001);
        }
    }

    [Fact]
    public void EdgeModesAndAlphaEdgesUseAssociatedLinearSamples()
    {
        PrismPremultipliedColor[] pixels =
        [
            default,
            PrismPremultipliedColor.FromStraight(
                1,
                0,
                0,
                1),
            default
        ];
        PrismNeighborhoodPlan clamp = CreatePlan(
            PrismFilterId.Blur,
            configure: (state, entry) =>
            {
                SetNumber(state, entry, "Radius", 2);
                SetSymbol(state, entry, "EdgeMode", "Clamp");
            });
        PrismNeighborhoodPlan transparent = CreatePlan(
            PrismFilterId.Blur,
            configure: (state, entry) =>
            {
                SetNumber(state, entry, "Radius", 2);
                SetSymbol(
                    state,
                    entry,
                    "EdgeMode",
                    "Transparent");
            });

        PrismPremultipliedColor[] clamped =
            PrismNeighborhoodMath.Apply(
                clamp,
                pixels,
                3,
                1,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] transparentResult =
            PrismNeighborhoodMath.Apply(
                transparent,
                pixels,
                3,
                1,
                PrismColorProfile.LinearSrgb);

        Assert.True(
            clamped[1].Alpha >
            transparentResult[1].Alpha);
        Assert.True(clamped[0].Alpha > 0);
        Assert.True(clamped[2].Alpha > 0);
        Assert.All(clamped, AssertFiniteAssociated);
        Assert.All(transparentResult, AssertFiniteAssociated);
    }

    [Fact]
    public void BoxBlurUsesNormalizedSummedAreaRectanglesAndIterations()
    {
        const int size = 7;
        int center = size / 2;
        PrismPremultipliedColor[] impulse = new PrismPremultipliedColor[size * size];
        impulse[(center * size) + center] =
            PrismPremultipliedColor.FromStraight(1, 0.5, 0.25, 0.8);
        PrismNeighborhoodPlan radiusOne = CreatePlan(
            PrismFilterId.BoxBlur,
            new DrawRect(0, 0, size, size),
            (state, entry) =>
            {
                SetNumber(state, entry, "Radius", 1);
                SetNumber(state, entry, "Iterations", 1);
                SetSymbol(state, entry, "EdgeMode", "Transparent");
            });

        Assert.Collection(
            radiusOne.Passes,
            horizontal =>
            {
                Assert.Equal(PrismNeighborhoodPassKind.Horizontal, horizontal.Kind);
                Assert.Equal(1, horizontal.RadiusX);
                Assert.Equal(0, horizontal.RadiusY);
                Assert.Equal(3, horizontal.SampleCount);
            },
            vertical =>
            {
                Assert.Equal(PrismNeighborhoodPassKind.Vertical, vertical.Kind);
                Assert.Equal(0, vertical.RadiusX);
                Assert.Equal(1, vertical.RadiusY);
                Assert.Equal(3, vertical.SampleCount);
            });
        PrismPremultipliedColor[] result = PrismNeighborhoodMath.Apply(
            radiusOne,
            impulse,
            size,
            size,
            PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor expected =
            PrismPremultipliedColor.FromStraight(1, 0.5, 0.25, 0.8 / 9);
        for (int y = center - 1; y <= center + 1; y++)
        {
            for (int x = center - 1; x <= center + 1; x++)
            {
                AssertColor(result[(y * size) + x], expected, 0.000001);
            }
        }
        Assert.InRange(result.Sum(pixel => pixel.Alpha), 0.799999, 0.800001);

        PrismNeighborhoodPlan twice = CreatePlan(
            PrismFilterId.BoxBlur,
            new DrawRect(0, 0, size, size),
            (state, entry) =>
            {
                SetNumber(state, entry, "Radius", 1);
                SetNumber(state, entry, "Iterations", 2);
                SetSymbol(state, entry, "EdgeMode", "Transparent");
            });
        Assert.Equal(4, twice.Passes.Length);
        PrismPremultipliedColor[] repeated = PrismNeighborhoodMath.Apply(
            radiusOne,
            result,
            size,
            size,
            PrismColorProfile.LinearSrgb);
        Assert.Equal(
            repeated,
            PrismNeighborhoodMath.Apply(
                twice,
                impulse,
                size,
                size,
                PrismColorProfile.LinearSrgb));
        Assert.All(result, AssertFiniteAssociated);
    }

    [Fact]
    public void BoxBlurPreservesConstantAlphaAndMatchesRectangleReference()
    {
        const int width = 5;
        const int height = 4;
        PrismPremultipliedColor constant =
            PrismPremultipliedColor.FromStraight(0.2, 0.4, 0.6, 0.5);
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(constant, width * height).ToArray();
        PrismNeighborhoodPlan plan = CreatePlan(
            PrismFilterId.BoxBlur,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                SetNumber(state, entry, "Radius", 2);
                SetNumber(state, entry, "Iterations", 1);
                SetSymbol(state, entry, "EdgeMode", "Clamp");
            });
        PrismPremultipliedColor[] actual = PrismNeighborhoodMath.Apply(
            plan, source, width, height, PrismColorProfile.LinearSrgb);

        Assert.All(actual, pixel => AssertColor(pixel, constant, 0.000001));
        Assert.Equal(2, plan.Options0.X);
        Assert.Collection(
            plan.Passes,
            horizontal =>
            {
                Assert.Equal(2, horizontal.BoundsRadiusX);
                Assert.Equal(0, horizontal.BoundsRadiusY);
            },
            vertical =>
            {
                Assert.Equal(0, vertical.BoundsRadiusX);
                Assert.Equal(2, vertical.BoundsRadiusY);
            });
    }

    [Fact]
    public void BlurProducesSymmetricGaussianFalloffFromAnImpulse()
    {
        const int size = 9;
        PrismPremultipliedColor[] source = new PrismPremultipliedColor[
            size * size];
        source[((size / 2) * size) + (size / 2)] =
            PrismPremultipliedColor.FromStraight(1, 1, 1, 1);
        PrismNeighborhoodPlan blur = CreatePlan(
            PrismFilterId.Blur,
            new DrawRect(0, 0, size, size),
            (state, entry) =>
            {
                SetNumber(state, entry, "Radius", 4);
                SetSymbol(state, entry, "Quality", "Best");
            });

        PrismPremultipliedColor[] result =
            PrismNeighborhoodMath.Apply(
                blur,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);
        int center = size / 2;
        double centerAlpha = result[(center * size) + center].Alpha;
        double nearAlpha = result[(center * size) + center + 1].Alpha;
        double farAlpha = result[(center * size) + center + 3].Alpha;

        Assert.True(centerAlpha > nearAlpha);
        Assert.True(nearAlpha > farAlpha);
        for (int offset = 1; offset <= center; offset++)
        {
            Assert.Equal(
                result[(center * size) + center - offset].Alpha,
                result[(center * size) + center + offset].Alpha,
                precision: 7);
            Assert.Equal(
                result[((center - offset) * size) + center].Alpha,
                result[((center + offset) * size) + center].Alpha,
                precision: 7);
        }
        Assert.All(result, AssertFiniteAssociated);
    }

    [Fact]
    public void BlurMoreUsesNormalizedSeparableGaussianAndIsStrongerThanBlur()
    {
        const int size = 17;
        int center = size / 2;
        PrismPremultipliedColor[] source = new PrismPremultipliedColor[
            size * size];
        source[(center * size) + center] =
            PrismPremultipliedColor.FromStraight(1, 1, 1, 1);
        PrismNeighborhoodPlan blur = CreatePlan(
            PrismFilterId.Blur,
            new DrawRect(0, 0, size, size));
        PrismNeighborhoodPlan blurMore = CreatePlan(
            PrismFilterId.BlurMore,
            new DrawRect(0, 0, size, size),
            (state, entry) =>
                SetSymbol(state, entry, "EdgeMode", "Transparent"));

        Assert.Equal(2, blurMore.Passes.Length);
        Assert.Equal(4, blurMore.Options0.X);
        Assert.True(blurMore.Options0.X > blur.Options0.X);
        Assert.Collection(
            blurMore.Passes,
            horizontal =>
            {
                Assert.Equal(
                    PrismNeighborhoodPassKind.Horizontal,
                    horizontal.Kind);
                Assert.Equal(4, horizontal.RadiusX);
                Assert.Equal(4, horizontal.BoundsRadiusX);
                Assert.Equal(0, horizontal.BoundsRadiusY);
            },
            vertical =>
            {
                Assert.Equal(
                    PrismNeighborhoodPassKind.Vertical,
                    vertical.Kind);
                Assert.Equal(4, vertical.RadiusY);
                Assert.Equal(0, vertical.BoundsRadiusX);
                Assert.Equal(4, vertical.BoundsRadiusY);
            });

        PrismPremultipliedColor[] blurred =
            PrismNeighborhoodMath.Apply(
                blur,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] stronger =
            PrismNeighborhoodMath.Apply(
                blurMore,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);
        Assert.True(
            stronger[(center * size) + center].Alpha <
            blurred[(center * size) + center].Alpha);
        Assert.InRange(
            stronger.Sum(pixel => pixel.Alpha),
            0.99999,
            1.00001);

        PrismNeighborhoodPlan equivalentBlur = CreatePlan(
            PrismFilterId.Blur,
            new DrawRect(0, 0, size, size),
            (state, entry) =>
            {
                SetNumber(state, entry, "Radius", 4);
                SetSymbol(state, entry, "EdgeMode", "Transparent");
            });
        PrismPremultipliedColor[] equivalent =
            PrismNeighborhoodMath.Apply(
                equivalentBlur,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);
        Assert.Equal(equivalent, stronger);
        Assert.All(stronger, AssertFiniteAssociated);
    }

    [Fact]
    public void SharpenPlansSingleContrastAdaptiveFiveTapPass()
    {
        PrismNeighborhoodPlan plan = CreatePlan(
            PrismFilterId.Sharpen,
            configure: (state, entry) =>
                SetNumber(state, entry, "Amount", 0.75f));
        PrismNeighborhoodPass pass = Assert.Single(plan.Passes);

        Assert.Equal(PrismNeighborhoodPassKind.Direct, pass.Kind);
        Assert.Equal(1, pass.RadiusX);
        Assert.Equal(1, pass.RadiusY);
        Assert.Equal(5, pass.SampleCount);
        Assert.Equal(0, pass.BoundsRadiusX);
        Assert.Equal(0, pass.BoundsRadiusY);
        Assert.False(pass.IsNoOp);
        Assert.Equal(0.75f, plan.Options0.X);

        PrismNeighborhoodPass disabled = Assert.Single(
            CreatePlan(
                PrismFilterId.Sharpen,
                configure: (state, entry) =>
                    SetNumber(state, entry, "Amount", 0))
                .Passes);
        Assert.True(disabled.IsNoOp);
    }

    [Fact]
    public void SharpenUsesPerChannelContrastAdaptationAndIgnoresDiagonals()
    {
        const int size = 3;
        const int center = 4;
        PrismPremultipliedColor edge =
            PrismPremultipliedColor.FromStraight(0.2, 0.2, 0.2, 1);
        PrismPremultipliedColor focus =
            PrismPremultipliedColor.FromStraight(0.5, 0.4, 0.3, 0.5);
        PrismPremultipliedColor[] darkDiagonals =
            Enumerable.Repeat(edge, size * size).ToArray();
        darkDiagonals[center] = focus;
        PrismPremultipliedColor[] brightDiagonals =
            darkDiagonals.ToArray();
        foreach (int index in new[] { 0, 2, 6, 8 })
        {
            brightDiagonals[index] =
                PrismPremultipliedColor.FromStraight(1, 1, 1, 1);
        }

        PrismNeighborhoodPlan plan = CreatePlan(
            PrismFilterId.Sharpen,
            new DrawRect(0, 0, size, size),
            (state, entry) =>
                SetNumber(state, entry, "Amount", 1));
        PrismPremultipliedColor[] first = PrismNeighborhoodMath.Apply(
            plan,
            darkDiagonals,
            size,
            size,
            PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] second = PrismNeighborhoodMath.Apply(
            plan,
            brightDiagonals,
            size,
            size,
            PrismColorProfile.LinearSrgb);

        AssertColor(
            first[center],
            PrismPremultipliedColor.FromStraight(
                0.807243720010863,
                0.660495713220364,
                0.48834836012945,
                0.5),
            0.000001);
        AssertColor(second[center], first[center], 0.000001);
        Assert.Equal(focus.Alpha, first[center].Alpha, precision: 7);
        AssertFiniteAssociated(first[center]);
    }

    [Fact]
    public void SharpenPreservesCenterAlphaAndIgnoresTransparentNeighborColor()
    {
        const int size = 3;
        const int center = 4;
        PrismPremultipliedColor focus =
            PrismPremultipliedColor.FromStraight(0.3, 0.6, 0.9, 0.4);
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[size * size];
        source[center] = focus;
        PrismNeighborhoodPlan plan = CreatePlan(
            PrismFilterId.Sharpen,
            new DrawRect(0, 0, size, size),
            (state, entry) =>
                SetNumber(state, entry, "Amount", 1));

        PrismPremultipliedColor[] result = PrismNeighborhoodMath.Apply(
            plan,
            source,
            size,
            size,
            PrismColorProfile.LinearSrgb);

        AssertColor(result[center], focus, 0.000001);
        Assert.Equal(focus.Alpha, result[center].Alpha, precision: 7);
        Assert.All(result, AssertFiniteAssociated);
    }

    [Fact]
    public void SharpenMorePlansSingleFixedBinomialNineTapPass()
    {
        PrismNeighborhoodPlan plan = CreatePlan(
            PrismFilterId.SharpenMore,
            configure: (state, entry) =>
                SetNumber(state, entry, "Amount", 0.5f));
        PrismNeighborhoodPass pass = Assert.Single(plan.Passes);

        Assert.Equal(PrismNeighborhoodPassKind.Direct, pass.Kind);
        Assert.Equal(1, pass.RadiusX);
        Assert.Equal(1, pass.RadiusY);
        Assert.Equal(9, pass.SampleCount);
        Assert.Equal(0, pass.BoundsRadiusX);
        Assert.Equal(0, pass.BoundsRadiusY);
        Assert.False(pass.IsNoOp);
        Assert.Equal(0.5f, plan.Options0.X);

        PrismNeighborhoodPass disabled = Assert.Single(
            CreatePlan(
                PrismFilterId.SharpenMore,
                configure: (state, entry) =>
                    SetNumber(state, entry, "Amount", 0))
                .Passes);
        Assert.True(disabled.IsNoOp);
    }

    [Fact]
    public void SharpenMoreUsesBinomialHighBoostAtDefaultAmount()
    {
        const int size = 3;
        const int center = 4;
        PrismPremultipliedColor diagonal =
            PrismPremultipliedColor.FromStraight(0.2, 0.2, 0.2, 0.4);
        PrismPremultipliedColor orthogonal =
            PrismPremultipliedColor.FromStraight(0.4, 0.4, 0.4, 0.4);
        PrismPremultipliedColor focus =
            PrismPremultipliedColor.FromStraight(0.5, 0.5, 0.5, 0.4);
        PrismPremultipliedColor[] source =
        [
            diagonal, orthogonal, diagonal,
            orthogonal, focus, orthogonal,
            diagonal, orthogonal, diagonal,
        ];
        PrismNeighborhoodPlan plan = CreatePlan(
            PrismFilterId.SharpenMore,
            new DrawRect(0, 0, size, size),
            (state, entry) =>
                SetNumber(state, entry, "Amount", 0.5f));

        PrismPremultipliedColor[] result = PrismNeighborhoodMath.Apply(
            plan,
            source,
            size,
            size,
            PrismColorProfile.LinearSrgb);

        AssertColor(
            result[center],
            PrismPremultipliedColor.FromStraight(0.625, 0.625, 0.625, 0.4),
            0.000001);
        Assert.Equal(focus.Alpha, result[center].Alpha, precision: 7);
        Assert.All(result, AssertFiniteAssociated);
    }

    [Fact]
    public void SharpenMorePreservesCenterAlphaAcrossOpaqueNeighbors()
    {
        const int size = 3;
        const int center = 4;
        PrismPremultipliedColor opaque =
            PrismPremultipliedColor.FromStraight(0.5, 0.5, 0.5, 1);
        PrismPremultipliedColor focus =
            PrismPremultipliedColor.FromStraight(0.5, 0.5, 0.5, 0.4);
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(opaque, size * size).ToArray();
        source[center] = focus;
        PrismNeighborhoodPlan plan = CreatePlan(
            PrismFilterId.SharpenMore,
            new DrawRect(0, 0, size, size),
            (state, entry) =>
                SetNumber(state, entry, "Amount", 1));

        PrismPremultipliedColor[] result = PrismNeighborhoodMath.Apply(
            plan,
            source,
            size,
            size,
            PrismColorProfile.LinearSrgb);

        AssertColor(result[center], focus, 0.000001);
        Assert.Equal(focus.Alpha, result[center].Alpha, precision: 7);
        Assert.All(result, AssertFiniteAssociated);
    }

    [Fact]
    public void SharpenEdgesPlansSingleSobelGatedNineTapPass()
    {
        PrismNeighborhoodPlan plan = CreatePlan(
            PrismFilterId.SharpenEdges,
            configure: (state, entry) =>
            {
                SetNumber(state, entry, "Amount", 0.75f);
                SetNumber(state, entry, "Threshold", 0.2f);
            });
        PrismNeighborhoodPass pass = Assert.Single(plan.Passes);

        Assert.Equal(PrismNeighborhoodPassKind.Direct, pass.Kind);
        Assert.Equal(1, pass.RadiusX);
        Assert.Equal(1, pass.RadiusY);
        Assert.Equal(9, pass.SampleCount);
        Assert.Equal(0, pass.BoundsRadiusX);
        Assert.Equal(0, pass.BoundsRadiusY);
        Assert.False(pass.IsNoOp);
        Assert.Equal(0.75f, plan.Options0.X);
        Assert.Equal(0.2f, plan.Options0.Y);

        PrismNeighborhoodPass disabled = Assert.Single(
            CreatePlan(
                PrismFilterId.SharpenEdges,
                configure: (state, entry) =>
                    SetNumber(state, entry, "Amount", 0))
                .Passes);
        Assert.True(disabled.IsNoOp);
    }

    [Fact]
    public void SharpenEdgesUsesDirectionalEdgesAndSoftThresholdKnee()
    {
        const int size = 5;
        const int center = 12;
        PrismPremultipliedColor dark =
            PrismPremultipliedColor.FromStraight(0.2, 0.2, 0.2, 1);
        PrismPremultipliedColor light =
            PrismPremultipliedColor.FromStraight(0.6, 0.6, 0.6, 1);
        PrismPremultipliedColor[] vertical = new PrismPremultipliedColor[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
                vertical[(y * size) + x] = x < 2 ? dark : light;
        }

        PrismPremultipliedColor[] lowThreshold = ApplySharpenEdges(vertical, size, 0.1f);
        PrismPremultipliedColor[] kneeThreshold = ApplySharpenEdges(vertical, size, 0.4f);
        PrismPremultipliedColor[] highThreshold = ApplySharpenEdges(vertical, size, 0.8f);

        Assert.True(lowThreshold[center].Red > kneeThreshold[center].Red);
        Assert.True(kneeThreshold[center].Red > highThreshold[center].Red);
        AssertColor(highThreshold[center], light, 0.000001);
        Assert.InRange(lowThreshold[center].Red, light.Red, 0.7f);

        PrismPremultipliedColor[] diagonal = new PrismPremultipliedColor[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
                diagonal[(y * size) + x] = x + y < 4 ? dark : light;
        }

        PrismPremultipliedColor[] diagonalResult =
            ApplySharpenEdges(diagonal, size, 0.1f);

        Assert.True(diagonalResult[center].Red > diagonal[center].Red);
        Assert.All(diagonalResult, AssertFiniteAssociated);
    }

    [Fact]
    public void SharpenEdgesPreservesCenterAlphaAndIgnoresTransparentNeighbors()
    {
        const int size = 3;
        const int center = 4;
        PrismPremultipliedColor focus =
            PrismPremultipliedColor.FromStraight(0.3, 0.5, 0.7, 0.4);
        PrismPremultipliedColor[] source = new PrismPremultipliedColor[size * size];
        source[center] = focus;

        PrismPremultipliedColor[] result =
            ApplySharpenEdges(source, size, threshold: 0);

        AssertColor(result[center], focus, 0.000001);
        Assert.Equal(focus.Alpha, result[center].Alpha, precision: 7);
        Assert.All(result, AssertFiniteAssociated);
    }

    [Fact]
    public void UnsharpMaskPlansGaussianPassesAndPreservesOriginalInput()
    {
        PrismGraph graph = CreateGraph(
            PrismFilterId.UnsharpMask,
            new DrawRect(0, 0, 32, 24),
            (state, entry) =>
            {
                SetNumber(state, entry, "Amount", 0.75f);
                SetNumber(state, entry, "Radius", 3);
                SetNumber(state, entry, "Threshold", 0.2f);
            });
        PrismGraphNode[] nodes = graph.Nodes
            .Where(node => node.Filter == PrismFilterId.UnsharpMask)
            .OrderBy(node => node.NeighborhoodPassIndex)
            .ToArray();
        PrismNeighborhoodPlan plan =
            Assert.IsType<PrismNeighborhoodPlan>(
                nodes[0].NeighborhoodPlan);

        Assert.Equal(3, plan.Passes.Length);
        Assert.Collection(
            plan.Passes,
            horizontal =>
            {
                Assert.Equal(
                    PrismNeighborhoodPassKind.Horizontal,
                    horizontal.Kind);
                Assert.Equal(3, horizontal.RadiusX);
                Assert.Equal(17, horizontal.SampleCount);
                Assert.Equal(0, horizontal.BoundsRadiusX);
            },
            vertical =>
            {
                Assert.Equal(
                    PrismNeighborhoodPassKind.Vertical,
                    vertical.Kind);
                Assert.Equal(3, vertical.RadiusY);
                Assert.Equal(17, vertical.SampleCount);
                Assert.Equal(0, vertical.BoundsRadiusY);
            },
            recombine =>
            {
                Assert.Equal(
                    PrismNeighborhoodPassKind.Recombine,
                    recombine.Kind);
                Assert.Equal(1, recombine.SampleCount);
                Assert.Equal(0, recombine.RadiusX);
                Assert.Equal(0, recombine.RadiusY);
            });
        Assert.Equal(0.75f, plan.Options0.X);
        Assert.Equal(3, plan.Options0.Y);
        Assert.Equal(0.2f, plan.Options0.Z);
        Assert.Equal(1, plan.Options0.W);

        PrismGraphNode final = nodes[^1];
        PrismGraphEdge original = Assert.Single(
            graph.Edges.Where(edge =>
                edge.Target == final.Id &&
                edge.Kind == PrismGraphEdgeKind.FilterOriginal));
        PrismGraphEdge firstContent = Assert.Single(
            graph.Edges.Where(edge =>
                edge.Target == nodes[0].Id &&
                edge.Kind == PrismGraphEdgeKind.Content));
        Assert.Equal(firstContent.Source, original.Source);
        PrismGraphExecutionPlan execution =
            new PrismGraphOptimizer().Optimize(graph);
        PrismGraphEdge optimizedOriginal = Assert.Single(
            execution.OptimizedGraph.Edges.Where(edge =>
                edge.Target == final.Id &&
                edge.Kind == PrismGraphEdgeKind.FilterOriginal));
        PrismGraphSurfaceLifetime originalLifetime =
            execution.SurfaceLifetimes.Single(lifetime =>
                lifetime.NodeId == optimizedOriginal.Source);
        Assert.True(
            originalLifetime.LastStep >=
                execution.ExecutionOrder.IndexOf(final.Id));

        Assert.True(
            Assert.Single(
                CreatePlan(
                    PrismFilterId.UnsharpMask,
                    configure: (state, entry) =>
                        SetNumber(state, entry, "Amount", 0))
                    .Passes)
                .IsNoOp);
        Assert.True(
            Assert.Single(
                CreatePlan(
                    PrismFilterId.UnsharpMask,
                    configure: (state, entry) =>
                        SetNumber(state, entry, "Radius", 0))
                    .Passes)
                .IsNoOp);
    }

    [Fact]
    public void ReduceNoisePlansThreeDomainTransformIterationsAndJpegDeblocking()
    {
        PrismGraph graph = CreateGraph(
            PrismFilterId.ReduceNoise,
            new DrawRect(0, 0, 32, 24),
            (state, entry) =>
            {
                SetNumber(state, entry, "Strength", 1);
                SetNumber(state, entry, "PreserveDetails", 0.25f);
                SetNumber(state, entry, "ReduceColorNoise", 0.75f);
                SetNumber(state, entry, "SharpenDetails", 0.5f);
                SetBoolean(
                    state,
                    entry,
                    "RemoveJpegArtifact",
                    true);
            });
        PrismGraphNode[] nodes = graph.Nodes
            .Where(node => node.Filter == PrismFilterId.ReduceNoise)
            .OrderBy(node => node.NeighborhoodPassIndex)
            .ToArray();
        PrismNeighborhoodPlan plan =
            Assert.IsType<PrismNeighborhoodPlan>(
                nodes[0].NeighborhoodPlan);

        Assert.Equal(
            [
                PrismNeighborhoodPassKind.Horizontal,
                PrismNeighborhoodPassKind.Vertical,
                PrismNeighborhoodPassKind.Horizontal,
                PrismNeighborhoodPassKind.Vertical,
                PrismNeighborhoodPassKind.Horizontal,
                PrismNeighborhoodPassKind.Vertical,
                PrismNeighborhoodPassKind.JpegDeblockHorizontal,
                PrismNeighborhoodPassKind.JpegDeblockVertical,
                PrismNeighborhoodPassKind.Recombine
            ],
            plan.Passes.Select(pass => pass.Kind).ToArray());
        Assert.Equal(
            [0, 0, 1, 1, 2, 2],
            plan.Passes.Take(6)
                .Select(pass => pass.SampleCount)
                .ToArray());
        Assert.True(plan.Options2.X > plan.Options2.Y);
        Assert.True(plan.Options2.Y > plan.Options2.Z);
        Assert.All(plan.Passes, pass =>
            Assert.InRange(
                MathF.Max(pass.RadiusX, pass.RadiusY),
                0,
                8));

        Assert.Equal(plan.Passes.Length, nodes.Length);
        PrismGraphEdge initialInput = Assert.Single(
            graph.Edges.Where(edge =>
                edge.Target == nodes[0].Id &&
                edge.Kind == PrismGraphEdgeKind.Content));
        PrismGraphEdge original = Assert.Single(
            graph.Edges.Where(edge =>
                edge.Target == nodes[^1].Id &&
                edge.Kind == PrismGraphEdgeKind.FilterOriginal));
        Assert.Equal(initialInput.Source, original.Source);
    }

    [Fact]
    public void ReduceNoiseSeparatesLumaAndChromaAndPreservesAlpha()
    {
        const int width = 17;
        const float alpha = 0.6f;
        PrismPremultipliedColor[] source =
            Enumerable.Range(0, width)
                .Select(index => index == 0
                    ? default
                    : PrismPremultipliedColor.FromStraight(
                        index % 2 == 0 ? 0.52 : 0.48,
                        0.48,
                        index % 2 == 0 ? 0.48 : 0.52,
                        alpha))
                .ToArray();
        PrismNeighborhoodPlan plan = CreatePlan(
            PrismFilterId.ReduceNoise,
            new DrawRect(0, 0, width, 1),
            (state, entry) =>
            {
                SetNumber(state, entry, "Strength", 0);
                SetNumber(state, entry, "PreserveDetails", 0);
                SetNumber(state, entry, "ReduceColorNoise", 1);
                SetNumber(state, entry, "SharpenDetails", 0);
                SetBoolean(
                    state,
                    entry,
                    "RemoveJpegArtifact",
                    false);
            });
        PrismPremultipliedColor[] result =
            PrismNeighborhoodMath.Apply(
                plan,
                source,
                width,
                1,
                PrismColorProfile.LinearSrgb);

        const int center = width / 2;
        double originalRed = source[center].Red / alpha;
        double resultRed = result[center].Red / alpha;
        double originalLuma =
            ((source[center].Red / alpha) * 0.25) +
            ((source[center].Green / alpha) * 0.5) +
            ((source[center].Blue / alpha) * 0.25);
        double resultLuma =
            ((result[center].Red / alpha) * 0.25) +
            ((result[center].Green / alpha) * 0.5) +
            ((result[center].Blue / alpha) * 0.25);

        Assert.True(Math.Abs(resultRed - originalRed) > 0.0001);
        Assert.Equal(originalLuma, resultLuma, precision: 6);
        Assert.Equal(default, result[0]);
        Assert.All(
            result.Skip(1),
            color => Assert.Equal(alpha, color.Alpha, precision: 7));
        Assert.All(result, AssertFiniteAssociated);
    }

    [Fact]
    public void ReduceNoisePreserveAndSharpenProduceDistinctDetailResponses()
    {
        const int width = 17;
        const int center = width / 2;
        const float alpha = 0.7f;
        PrismPremultipliedColor[] source =
            Enumerable.Range(0, width)
                .Select(index =>
                    PrismPremultipliedColor.FromStraight(
                        index == center ? 0.55 : 0.45,
                        index == center ? 0.55 : 0.45,
                        index == center ? 0.55 : 0.45,
                        alpha))
                .ToArray();

        PrismPremultipliedColor[] smooth = Apply(
            preserve: 0,
            sharpen: 0);
        PrismPremultipliedColor[] preserved = Apply(
            preserve: 1,
            sharpen: 0);
        PrismPremultipliedColor[] sharpened = Apply(
            preserve: 0,
            sharpen: 1);

        Assert.True(smooth[center].Red < source[center].Red);
        Assert.True(preserved[center].Red > smooth[center].Red);
        Assert.True(sharpened[center].Red > smooth[center].Red);
        Assert.NotEqual(
            preserved[center].Red,
            sharpened[center].Red);
        Assert.All(smooth, AssertFiniteAssociated);
        Assert.All(preserved, AssertFiniteAssociated);
        Assert.All(sharpened, AssertFiniteAssociated);

        PrismPremultipliedColor[] Apply(
            float preserve,
            float sharpen)
        {
            PrismNeighborhoodPlan plan = CreatePlan(
                PrismFilterId.ReduceNoise,
                new DrawRect(0, 0, width, 1),
                (state, entry) =>
                {
                    SetNumber(state, entry, "Strength", 1);
                    SetNumber(
                        state,
                        entry,
                        "PreserveDetails",
                        preserve);
                    SetNumber(state, entry, "ReduceColorNoise", 0);
                    SetNumber(
                        state,
                        entry,
                        "SharpenDetails",
                        sharpen);
                    SetBoolean(
                        state,
                        entry,
                        "RemoveJpegArtifact",
                        false);
                });
            return PrismNeighborhoodMath.Apply(
                plan,
                source,
                width,
                1,
                PrismColorProfile.LinearSrgb);
        }
    }

    [Fact]
    public void ReduceNoiseJpegDeblockingTargetsEightPixelBoundaries()
    {
        const int width = 16;
        const float alpha = 0.8f;
        PrismPremultipliedColor[] source =
            Enumerable.Range(0, width)
                .Select(index =>
                    PrismPremultipliedColor.FromStraight(
                        index < 8 ? 0.4 : 0.48,
                        index < 8 ? 0.4 : 0.48,
                        index < 8 ? 0.4 : 0.48,
                        alpha))
                .ToArray();
        PrismPremultipliedColor[] disabled = Apply(false);
        PrismPremultipliedColor[] enabled = Apply(true);

        for (int index = 0; index < source.Length; index++)
        {
            AssertColor(disabled[index], source[index], 0.0000001);
        }
        Assert.True(enabled[7].Red > source[7].Red);
        Assert.True(enabled[8].Red < source[8].Red);
        AssertColor(enabled[3], source[3], 0.0000001);
        Assert.Equal(alpha, enabled[7].Alpha, precision: 7);
        Assert.Equal(alpha, enabled[8].Alpha, precision: 7);
        Assert.All(enabled, AssertFiniteAssociated);

        PrismPremultipliedColor[] Apply(bool removeJpeg)
        {
            PrismNeighborhoodPlan plan = CreatePlan(
                PrismFilterId.ReduceNoise,
                new DrawRect(0, 0, width, 1),
                (state, entry) =>
                {
                    SetNumber(state, entry, "Strength", 0);
                    SetNumber(state, entry, "PreserveDetails", 0);
                    SetNumber(state, entry, "ReduceColorNoise", 0);
                    SetNumber(state, entry, "SharpenDetails", 0);
                    SetBoolean(
                        state,
                        entry,
                        "RemoveJpegArtifact",
                        removeJpeg);
                });
            return PrismNeighborhoodMath.Apply(
                plan,
                source,
                width,
                1,
                PrismColorProfile.LinearSrgb);
        }
    }

    [Fact]
    public void HighPassPlansGaussianPassesAndPreservesOriginalInput()
    {
        PrismGraph graph = CreateGraph(
            PrismFilterId.HighPass,
            new DrawRect(0, 0, 32, 24),
            (state, entry) =>
            {
                SetNumber(state, entry, "Radius", 3);
                SetSymbol(state, entry, "EdgeMode", "Transparent");
            });
        PrismGraphNode[] nodes = graph.Nodes
            .Where(node => node.Filter == PrismFilterId.HighPass)
            .OrderBy(node => node.NeighborhoodPassIndex)
            .ToArray();
        PrismNeighborhoodPlan plan =
            Assert.IsType<PrismNeighborhoodPlan>(
                nodes[0].NeighborhoodPlan);

        Assert.Collection(
            plan.Passes,
            horizontal =>
            {
                Assert.Equal(
                    PrismNeighborhoodPassKind.Horizontal,
                    horizontal.Kind);
                Assert.Equal(3, horizontal.RadiusX);
                Assert.Equal(17, horizontal.SampleCount);
                Assert.Equal(0, horizontal.BoundsRadiusX);
            },
            vertical =>
            {
                Assert.Equal(
                    PrismNeighborhoodPassKind.Vertical,
                    vertical.Kind);
                Assert.Equal(3, vertical.RadiusY);
                Assert.Equal(17, vertical.SampleCount);
                Assert.Equal(0, vertical.BoundsRadiusY);
            },
            recombine =>
            {
                Assert.Equal(
                    PrismNeighborhoodPassKind.Recombine,
                    recombine.Kind);
                Assert.Equal(1, recombine.SampleCount);
            });
        Assert.Equal(3, plan.Options0.X);
        Assert.Equal(17, plan.Options0.Y);
        Assert.Equal(1, plan.Options0.Z);
        Assert.Equal(1, plan.Options0.W);

        PrismGraphEdge initialInput = Assert.Single(
            graph.Edges.Where(edge =>
                edge.Target == nodes[0].Id &&
                edge.Kind == PrismGraphEdgeKind.Content));
        PrismGraphEdge original = Assert.Single(
            graph.Edges.Where(edge =>
                edge.Target == nodes[^1].Id &&
                edge.Kind == PrismGraphEdgeKind.FilterOriginal));
        Assert.Equal(initialInput.Source, original.Source);

        PrismGraphExecutionPlan execution =
            new PrismGraphOptimizer().Optimize(graph);
        PrismGraphEdge optimizedOriginal = Assert.Single(
            execution.OptimizedGraph.Edges.Where(edge =>
                edge.Target == nodes[^1].Id &&
                edge.Kind == PrismGraphEdgeKind.FilterOriginal));
        PrismGraphSurfaceLifetime originalLifetime =
            execution.SurfaceLifetimes.Single(lifetime =>
                lifetime.NodeId == optimizedOriginal.Source);
        Assert.True(
            originalLifetime.LastStep >=
            execution.ExecutionOrder.IndexOf(nodes[^1].Id));
    }

    [Fact]
    public void HighPassUsesGaussianDetailPreservesAlphaAndHonorsEdgeMode()
    {
        const int width = 7;
        const int center = width / 2;
        const float radius = 3;
        const float alpha = 0.6f;
        const float impulse = 0.2f;
        PrismPremultipliedColor[] source =
            Enumerable.Range(0, width)
                .Select(index =>
                    PrismPremultipliedColor.FromStraight(
                        index == center ? impulse : 0,
                        index == center ? impulse : 0,
                        index == center ? impulse : 0,
                        alpha))
                .ToArray();
        PrismNeighborhoodPlan plan = CreatePlan(
            PrismFilterId.HighPass,
            new DrawRect(0, 0, width, 1),
            (state, entry) =>
            {
                SetNumber(state, entry, "Radius", radius);
                SetSymbol(state, entry, "EdgeMode", "Clamp");
            });
        PrismPremultipliedColor[] result =
            PrismNeighborhoodMath.Apply(
                plan,
                source,
                width,
                1,
                PrismColorProfile.LinearSrgb);

        PrismNeighborhoodPlan gaussianPlan = CreatePlan(
            PrismFilterId.GaussianBlur,
            new DrawRect(0, 0, width, 1),
            (state, entry) =>
            {
                SetNumber(state, entry, "Radius", radius);
                SetSymbol(state, entry, "Quality", "Best");
                SetSymbol(state, entry, "EdgeMode", "Clamp");
            });
        PrismPremultipliedColor[] blurred =
            PrismNeighborhoodMath.Apply(
                gaussianPlan,
                source,
                width,
                1,
                PrismColorProfile.LinearSrgb);
        for (int offset = -3; offset <= 3; offset++)
        {
            int index = center + offset;
            double sourceRed = source[index].Red / alpha;
            double blurredRed = blurred[index].Red / alpha;
            double expected = (0.5 + sourceRed - blurredRed) * alpha;
            Assert.Equal(expected, result[index].Red, precision: 6);
            Assert.Equal(alpha, result[index].Alpha, precision: 7);
        }

        PrismPremultipliedColor[] edgeSource =
        [
            PrismPremultipliedColor.FromStraight(1, 0, 0, alpha),
            default,
            default
        ];
        PrismPremultipliedColor[] clamped =
            ApplyHighPass(edgeSource, "Clamp");
        PrismPremultipliedColor[] transparent =
            ApplyHighPass(edgeSource, "Transparent");

        Assert.True(transparent[0].Red > clamped[0].Red);
        Assert.Equal(alpha, clamped[0].Alpha, precision: 7);
        Assert.Equal(alpha, transparent[0].Alpha, precision: 7);
        Assert.All(result, AssertFiniteAssociated);
        Assert.All(clamped, AssertFiniteAssociated);
        Assert.All(transparent, AssertFiniteAssociated);

        static PrismPremultipliedColor[] ApplyHighPass(
            PrismPremultipliedColor[] pixels,
            string edgeMode)
        {
            PrismNeighborhoodPlan highPass = CreatePlan(
                PrismFilterId.HighPass,
                new DrawRect(0, 0, pixels.Length, 1),
                (state, entry) =>
                {
                    SetNumber(state, entry, "Radius", 2);
                    SetSymbol(state, entry, "EdgeMode", edgeMode);
                });
            return PrismNeighborhoodMath.Apply(
                highPass,
                pixels,
                pixels.Length,
                1,
                PrismColorProfile.LinearSrgb);
        }
    }

    [Fact]
    public void UnsharpMaskUsesGaussianHighBoostSoftThresholdAndCenterAlpha()
    {
        const int width = 17;
        const int edge = width / 2;
        const float alpha = 0.6f;
        PrismPremultipliedColor[] source =
            Enumerable.Range(0, width)
                .Select(index =>
                    PrismPremultipliedColor.FromStraight(
                        index < edge ? 0.4 : 0.6,
                        index < edge ? 0.4 : 0.6,
                        index < edge ? 0.4 : 0.6,
                        alpha))
                .ToArray();
        PrismNeighborhoodPlan gaussian = CreatePlan(
            PrismFilterId.GaussianBlur,
            new DrawRect(0, 0, width, 1),
            (state, entry) =>
            {
                SetNumber(state, entry, "Radius", 3);
                SetSymbol(state, entry, "Quality", "Best");
                SetSymbol(state, entry, "EdgeMode", "Clamp");
            });
        PrismPremultipliedColor[] blurred =
            PrismNeighborhoodMath.Apply(
                gaussian,
                source,
                width,
                1,
                PrismColorProfile.LinearSrgb);
        float originalStraight =
            (float)(source[edge].Red / source[edge].Alpha);
        float blurredStraight =
            (float)(blurred[edge].Red / blurred[edge].Alpha);
        float difference =
            MathF.Abs(originalStraight - blurredStraight);

        PrismPremultipliedColor[] full = ApplyUnsharp(
            threshold: 0);
        PrismPremultipliedColor[] knee = ApplyUnsharp(
            threshold: difference);
        PrismPremultipliedColor[] blocked = ApplyUnsharp(
            threshold: 1);
        double fullExpected = Math.Clamp(
            originalStraight +
                ((originalStraight - blurredStraight) * 0.5f),
            0,
            1) * alpha;
        double kneeExpected = Math.Clamp(
            originalStraight +
                ((originalStraight - blurredStraight) * 0.25f),
            0,
            1) * alpha;

        Assert.Equal(fullExpected, full[edge].Red, precision: 6);
        Assert.Equal(kneeExpected, knee[edge].Red, precision: 6);
        AssertColor(blocked[edge], source[edge], 0.000001);
        for (int index = 0; index < width; index++)
        {
            Assert.Equal(
                source[index].Alpha,
                full[index].Alpha,
                precision: 7);
        }
        Assert.All(full, AssertFiniteAssociated);
        Assert.All(knee, AssertFiniteAssociated);
        Assert.All(blocked, AssertFiniteAssociated);

        PrismPremultipliedColor[] ApplyUnsharp(float threshold)
        {
            PrismNeighborhoodPlan plan = CreatePlan(
                PrismFilterId.UnsharpMask,
                new DrawRect(0, 0, width, 1),
                (state, entry) =>
                {
                    SetNumber(state, entry, "Amount", 0.5f);
                    SetNumber(state, entry, "Radius", 3);
                    SetNumber(state, entry, "Threshold", threshold);
                });
            return PrismNeighborhoodMath.Apply(
                plan,
                source,
                width,
                1,
                PrismColorProfile.LinearSrgb);
        }
    }

    [Fact]
    public void SmartSharpenPlansFourRichardsonLucyIterationsWithPreservedInputs()
    {
        PrismGraph graph = CreateGraph(
            PrismFilterId.SmartSharpen,
            new DrawRect(0, 0, 48, 32),
            (state, entry) =>
            {
                SetNumber(state, entry, "Amount", 0.8f);
                SetNumber(state, entry, "Radius", 2);
                SetNumber(state, entry, "ReduceNoise", 0.25f);
                SetSymbol(state, entry, "Remove", "MotionBlur");
                SetNumber(state, entry, "Angle", 35);
            });
        PrismGraphNode[] nodes = graph.Nodes
            .Where(node => node.Filter == PrismFilterId.SmartSharpen)
            .OrderBy(node => node.NeighborhoodPassIndex)
            .ToArray();
        PrismNeighborhoodPlan plan =
            Assert.IsType<PrismNeighborhoodPlan>(
                nodes[0].NeighborhoodPlan);

        Assert.Equal(17, plan.Passes.Length);
        for (int iteration = 0; iteration < 4; iteration++)
        {
            int offset = iteration * 4;
            Assert.Equal(
                PrismNeighborhoodPassKind.RichardsonLucyPsf,
                plan.Passes[offset].Kind);
            Assert.Equal(
                PrismNeighborhoodPassKind.RichardsonLucyRatio,
                plan.Passes[offset + 1].Kind);
            Assert.Equal(
                PrismNeighborhoodPassKind.RichardsonLucyBackProject,
                plan.Passes[offset + 2].Kind);
            Assert.Equal(
                PrismNeighborhoodPassKind.RichardsonLucyUpdate,
                plan.Passes[offset + 3].Kind);
        }
        Assert.Equal(
            PrismNeighborhoodPassKind.Recombine,
            plan.Passes[^1].Kind);
        Assert.Equal(4, plan.Options2.W);

        PrismGraphEdge initialInput = Assert.Single(
            graph.Edges.Where(edge =>
                edge.Target == nodes[0].Id &&
                edge.Kind == PrismGraphEdgeKind.Content));
        for (int iteration = 0; iteration < 4; iteration++)
        {
            int offset = iteration * 4;
            PrismGraphEdge ratioOriginal = Assert.Single(
                graph.Edges.Where(edge =>
                    edge.Target == nodes[offset + 1].Id &&
                    edge.Kind == PrismGraphEdgeKind.FilterOriginal));
            Assert.Equal(initialInput.Source, ratioOriginal.Source);

            PrismGraphEdge estimate = Assert.Single(
                graph.Edges.Where(edge =>
                    edge.Target == nodes[offset + 3].Id &&
                    edge.Kind == PrismGraphEdgeKind.FilterOriginal));
            Assert.Equal(
                iteration == 0
                    ? initialInput.Source
                    : nodes[offset - 1].Id,
                estimate.Source);
        }
        PrismGraphEdge finalOriginal = Assert.Single(
            graph.Edges.Where(edge =>
                edge.Target == nodes[^1].Id &&
                edge.Kind == PrismGraphEdgeKind.FilterOriginal));
        Assert.Equal(initialInput.Source, finalOriginal.Source);
    }

    [Fact]
    public void SmartSharpenDeconvolvesDistinctPsfsAndPreservesAlpha()
    {
        const int size = 17;
        int center = size / 2;
        PrismPremultipliedColor[] source =
            Enumerable.Range(0, size * size)
                .Select(index =>
                {
                    int x = index % size;
                    int y = index / size;
                    double value = x >= center && y >= center ? 0.75 : 0.2;
                    return PrismPremultipliedColor.FromStraight(
                        value,
                        value * 0.7,
                        value * 0.4,
                        0.55);
                })
                .ToArray();

        PrismPremultipliedColor[] gaussian = Apply("GaussianBlur", 0);
        PrismPremultipliedColor[] lens = Apply("LensBlur", 0);
        PrismPremultipliedColor[] motion = Apply("MotionBlur", 35);
        PrismPremultipliedColor[] otherAngle = Apply("MotionBlur", -35);
        PrismPremultipliedColor[] damped = Apply(
            "GaussianBlur",
            0,
            reduceNoise: 1);
        PrismPremultipliedColor[] unprotected = Apply(
            "GaussianBlur",
            0,
            shadowFade: 0,
            highlightFade: 0);
        PrismPremultipliedColor[] wideProtection = Apply(
            "GaussianBlur",
            0,
            shadowFade: 0.8f,
            highlightFade: 0.8f,
            tonalWidth: 0.8f,
            tonalRadius: 4);

        Assert.NotEqual(gaussian, lens);
        Assert.NotEqual(gaussian, motion);
        Assert.NotEqual(lens, motion);
        Assert.NotEqual(motion, otherAngle);
        Assert.NotEqual(gaussian, damped);
        Assert.NotEqual(unprotected, wideProtection);
        Assert.All(
            gaussian.Concat(lens)
                .Concat(motion)
                .Concat(otherAngle)
                .Concat(damped)
                .Concat(unprotected)
                .Concat(wideProtection),
            pixel =>
            {
                Assert.Equal(0.55, pixel.Alpha, precision: 6);
                AssertFiniteAssociated(pixel);
            });

        PrismPremultipliedColor[] Apply(
            string remove,
            float angle,
            float reduceNoise = 0.1f,
            float shadowFade = 0.4f,
            float highlightFade = 0.3f,
            float tonalWidth = 0.5f,
            float tonalRadius = 1)
        {
            PrismNeighborhoodPlan plan = CreatePlan(
                PrismFilterId.SmartSharpen,
                new DrawRect(0, 0, size, size),
                (state, entry) =>
                {
                    SetNumber(state, entry, "Amount", 1);
                    SetNumber(state, entry, "Radius", 2);
                    SetNumber(state, entry, "ReduceNoise", reduceNoise);
                    SetSymbol(state, entry, "Remove", remove);
                    SetNumber(state, entry, "Angle", angle);
                    SetNumber(state, entry, "ShadowFade", shadowFade);
                    SetNumber(
                        state,
                        entry,
                        "ShadowTonalWidth",
                        tonalWidth);
                    SetNumber(
                        state,
                        entry,
                        "ShadowRadius",
                        tonalRadius);
                    SetNumber(
                        state,
                        entry,
                        "HighlightFade",
                        highlightFade);
                    SetNumber(
                        state,
                        entry,
                        "HighlightTonalWidth",
                        tonalWidth);
                    SetNumber(
                        state,
                        entry,
                        "HighlightRadius",
                        tonalRadius);
                });
            return PrismNeighborhoodMath.Apply(
                plan,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);
        }
    }

    [Fact]
    public void GaussianBlurIncrementalKernelMatchesDirectReferenceAndPreservesInvariants()
    {
        const int size = 17;
        const int radius = 4;
        int center = size / 2;
        PrismNeighborhoodPlan plan = CreatePlan(
            PrismFilterId.GaussianBlur,
            new DrawRect(0, 0, size, size),
            (state, entry) =>
            {
                SetNumber(state, entry, "Radius", radius);
                SetSymbol(state, entry, "Quality", "Good");
                SetSymbol(state, entry, "EdgeMode", "Transparent");
            });

        Assert.Equal(radius / 3f, plan.Options0.W);
        Assert.Equal(2, plan.Passes.Length);
        PrismPremultipliedColor constant =
            PrismPremultipliedColor.FromStraight(0.2, 0.4, 0.6, 0.5);
        PrismNeighborhoodPlan clampPlan = CreatePlan(
            PrismFilterId.GaussianBlur,
            new DrawRect(0, 0, size, size),
            (state, entry) =>
            {
                SetNumber(state, entry, "Radius", radius);
                SetSymbol(state, entry, "Quality", "Good");
                SetSymbol(state, entry, "EdgeMode", "Clamp");
            });
        PrismPremultipliedColor[] constantResult = PrismNeighborhoodMath.Apply(
            clampPlan,
            Enumerable.Repeat(constant, size * size).ToArray(),
            size,
            size,
            PrismColorProfile.LinearSrgb);
        Assert.All(
            constantResult,
            pixel => AssertColor(pixel, constant, 0.000001));

        PrismPremultipliedColor[] impulse =
            new PrismPremultipliedColor[size * size];
        impulse[(center * size) + center] =
            PrismPremultipliedColor.FromStraight(1, 1, 1, 1);
        PrismPremultipliedColor[] actual = PrismNeighborhoodMath.Apply(
            plan, impulse, size, size, PrismColorProfile.LinearSrgb);
        double sigma = radius / 3.0;
        double[] weights = Enumerable.Range(-radius, (radius * 2) + 1)
            .Select(offset => Math.Exp(-(offset * offset) / (2 * sigma * sigma)))
            .ToArray();
        double normalization = weights.Sum();

        for (int offset = -radius; offset <= radius; offset++)
        {
            double expected = weights[offset + radius] /
                normalization * weights[radius] / normalization;
            Assert.InRange(
                Math.Abs(actual[(center * size) + center + offset].Alpha - expected),
                0,
                0.000001);
            Assert.Equal(
                actual[(center * size) + center - offset].Alpha,
                actual[(center * size) + center + offset].Alpha,
                precision: 7);
            Assert.Equal(
                actual[((center - offset) * size) + center].Alpha,
                actual[((center + offset) * size) + center].Alpha,
                precision: 7);
        }
        Assert.InRange(actual.Sum(pixel => pixel.Alpha), 0.999999, 1.000001);
        Assert.All(actual, AssertFiniteAssociated);
    }

    [Fact]
    public void BlurZeroAndLargeRadiiHaveExactNoOpAndConservativeBounds()
    {
        DrawRect sourceBounds = new(0, 0, 3, 2);
        PrismGraph zero = CreateGraph(
            PrismFilterId.Blur,
            sourceBounds,
            (state, entry) =>
                SetNumber(state, entry, "Radius", 0));
        PrismGraphExecutionPlan zeroPlan =
            new PrismGraphOptimizer().Optimize(zero);
        Assert.DoesNotContain(
            zeroPlan.OptimizedGraph.Nodes,
            node => node.Kind == PrismGraphNodeKind.Filter);

        PrismGraph large = CreateGraph(
            PrismFilterId.Blur,
            sourceBounds,
            (state, entry) =>
                SetNumber(state, entry, "Radius", 100_000));
        PrismGraphExecutionPlan largePlan =
            new PrismGraphOptimizer().Optimize(large);
        PrismGraphNode[] passes = largePlan.OptimizedGraph.Nodes
            .Where(node =>
                node.Kind == PrismGraphNodeKind.Filter)
            .ToArray();

        Assert.Equal(2, passes.Length);
        Assert.All(
            passes,
            node => Assert.False(
                Assert.IsType<PrismNeighborhoodPlan>(
                    node.NeighborhoodPlan)
                    .Passes[node.NeighborhoodPassIndex]
                    .IsNoOp));
        PrismGraphNodePlan final =
            largePlan.GetNodePlan(passes[^1].Id);
        Assert.Equal(
            new DrawRect(
                -100_000,
                -100_000,
                200_003,
                200_002),
            final.Bounds);
        Assert.Equal(
            PrismGraphBoundsStatus.Conservative,
            final.BoundsStatus);
    }

    [Fact]
    public void LensBlurUsesNormalizedRotatableApertureAndAssociatedHighlights()
    {
        const int size = 31;
        int center = size / 2;
        PrismPremultipliedColor[] impulse =
            new PrismPremultipliedColor[size * size];
        impulse[(center * size) + center] =
            PrismPremultipliedColor.FromStraight(0.2, 0.1, 0.05, 0.5);

        PrismNeighborhoodPlan hexagon = LensPlan(rotation: 0, boost: 0);
        PrismNeighborhoodPlan rotated = LensPlan(rotation: 30, boost: 0);
        PrismPremultipliedColor[] first = PrismNeighborhoodMath.Apply(
            hexagon, impulse, size, size, PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] turned = PrismNeighborhoodMath.Apply(
            rotated, impulse, size, size, PrismColorProfile.LinearSrgb);

        PrismNeighborhoodPass pass = Assert.Single(hexagon.Passes);
        Assert.Equal(6, hexagon.Options0.Y);
        Assert.Equal(5, pass.RadiusX);
        Assert.Equal(5, pass.BoundsRadiusX);
        Assert.Equal(5, pass.BoundsRadiusY);
        Assert.InRange(first.Sum(pixel => pixel.Alpha), 0.49999, 0.50001);
        Assert.False(first.SequenceEqual(turned));
        Assert.All(first, AssertFiniteAssociated);
        Assert.All(turned, AssertFiniteAssociated);

        PrismPremultipliedColor[] boosted = PrismNeighborhoodMath.Apply(
            LensPlan(rotation: 0, boost: 1),
            impulse,
            size,
            size,
            PrismColorProfile.LinearSrgb);
        Assert.True(boosted.Sum(pixel => pixel.Red) >
            first.Sum(pixel => pixel.Red));
        Assert.InRange(boosted.Sum(pixel => pixel.Alpha), 0.49999, 0.50001);
        Assert.All(boosted, AssertFiniteAssociated);

        PrismNeighborhoodPlan LensPlan(float rotation, float boost) =>
            CreatePlan(
                PrismFilterId.LensBlur,
                new DrawRect(0, 0, size, size),
                (state, entry) =>
                {
                    SetNumber(state, entry, "Radius", 5);
                    SetNumber(state, entry, "BladeCount", 6);
                    SetNumber(state, entry, "BladeCurvature", 0);
                    SetNumber(state, entry, "Rotation", rotation);
                    SetNumber(state, entry, "SpecularBrightness", boost);
                    SetNumber(state, entry, "SpecularThreshold", 0.1f);
                });
    }

    [Fact]
    public void MotionBlurUsesCenteredNormalizedDirectionalLinePsf()
    {
        const int size = 21;
        int center = size / 2;
        PrismPremultipliedColor[] impulse =
            new PrismPremultipliedColor[size * size];
        impulse[(center * size) + center] =
            PrismPremultipliedColor.FromStraight(1, 0.5, 0.25, 0.8);
        PrismNeighborhoodPlan horizontal = MotionPlan(angle: 0);
        PrismNeighborhoodPlan vertical = MotionPlan(angle: 90);

        PrismNeighborhoodPass horizontalPass =
            Assert.Single(horizontal.Passes);
        Assert.Equal(4, horizontalPass.RadiusX, precision: 6);
        Assert.Equal(0, horizontalPass.RadiusY, precision: 6);
        Assert.Equal(4, horizontalPass.BoundsRadiusX, precision: 6);
        Assert.Equal(0, horizontalPass.BoundsRadiusY, precision: 6);
        Assert.Equal(17, horizontalPass.SampleCount);
        Assert.Equal(8, horizontal.Options0.X, precision: 6);
        Assert.Equal(17, horizontal.Options0.Z);

        PrismPremultipliedColor[] across = PrismNeighborhoodMath.Apply(
            horizontal,
            impulse,
            size,
            size,
            PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] down = PrismNeighborhoodMath.Apply(
            vertical,
            impulse,
            size,
            size,
            PrismColorProfile.LinearSrgb);

        Assert.InRange(across.Sum(pixel => pixel.Alpha), 0.799999, 0.800001);
        Assert.InRange(down.Sum(pixel => pixel.Alpha), 0.799999, 0.800001);
        Assert.True(
            across[(center * size) + center].Alpha >
            across[(center * size) + center + 4].Alpha);
        for (int offset = -center; offset <= center; offset++)
        {
            AssertColor(
                down[((center - offset) * size) + center],
                across[(center * size) + center + offset],
                tolerance: 0.000001);
            Assert.Equal(
                across[(center * size) + center - offset].Alpha,
                across[(center * size) + center + offset].Alpha,
                precision: 7);
        }
        Assert.All(across, AssertFiniteAssociated);
        Assert.All(down, AssertFiniteAssociated);

        PrismNeighborhoodPlan MotionPlan(float angle) => CreatePlan(
            PrismFilterId.MotionBlur,
            new DrawRect(0, 0, size, size),
            (state, entry) =>
            {
                SetNumber(state, entry, "Distance", 8);
                SetNumber(state, entry, "Angle", angle);
                SetSymbol(state, entry, "Quality", "Best");
                SetSymbol(state, entry, "EdgeMode", "Transparent");
            });
    }

    [Fact]
    public void RadialBlurUsesCenteredPolarSpinAndZoomWithEffectiveQuality()
    {
        const int width = 9;
        const int height = 5;
        int centerX = width / 2;
        int centerY = height / 2;
        PrismPremultipliedColor[] impulse =
            new PrismPremultipliedColor[width * height];
        PrismPremultipliedColor marker =
            PrismPremultipliedColor.FromStraight(1, 0.5, 0.25, 0.8);
        impulse[(centerY * width) + centerX] = marker;

        PrismNeighborhoodPlan spin = RadialPlan("Spin", 1.2f, "Best");
        PrismNeighborhoodPlan zoom = RadialPlan("Zoom", 1.2f, "Draft");
        PrismNeighborhoodPass spinPass = Assert.Single(spin.Passes);
        PrismNeighborhoodPass zoomPass = Assert.Single(zoom.Passes);

        Assert.Equal(0, spin.Options0.X);
        Assert.Equal(1, zoom.Options0.X);
        Assert.Equal(1.2f, spin.Options0.Y);
        Assert.Equal(0.5f, spin.Options0.Z);
        Assert.Equal(0.5f, spin.Options0.W);
        Assert.Equal(17, spinPass.SampleCount);
        Assert.Equal(5, zoomPass.SampleCount);
        Assert.Equal(0, spinPass.BoundsRadiusX);
        Assert.Equal(0, spinPass.BoundsRadiusY);
        Assert.True(spinPass.RadiusX > 0);
        Assert.True(zoomPass.RadiusX > 0);

        PrismPremultipliedColor[] spun = PrismNeighborhoodMath.Apply(
            spin, impulse, width, height, PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] zoomed = PrismNeighborhoodMath.Apply(
            zoom, impulse, width, height, PrismColorProfile.LinearSrgb);
        AssertColor(spun[(centerY * width) + centerX], marker, 0.000001);
        AssertColor(zoomed[(centerY * width) + centerX], marker, 0.000001);
        Assert.False(spun.SequenceEqual(zoomed));
        Assert.All(spun, AssertFiniteAssociated);
        Assert.All(zoomed, AssertFiniteAssociated);

        PrismNeighborhoodPlan zero = RadialPlan("Spin", 0, "Good");
        Assert.True(Assert.Single(zero.Passes).IsNoOp);
        AssertColors(
            impulse,
            PrismNeighborhoodMath.Apply(
                zero,
                impulse,
                width,
                height,
                PrismColorProfile.LinearSrgb));

        static void AssertColors(
            PrismPremultipliedColor[] expected,
            PrismPremultipliedColor[] actual)
        {
            Assert.Equal(expected.Length, actual.Length);
            for (int index = 0; index < expected.Length; index++)
            {
                AssertColor(actual[index], expected[index], 0.0000001);
            }
        }

        PrismNeighborhoodPlan RadialPlan(
            string mode,
            float amount,
            string quality) => CreatePlan(
                PrismFilterId.RadialBlur,
                new DrawRect(0, 0, width, height),
                (state, entry) =>
                {
                    SetSymbol(state, entry, "Mode", mode);
                    SetNumber(state, entry, "Amount", amount);
                    SetSymbol(state, entry, "Quality", quality);
                });
    }

    [Fact]
    public void RadialBlurPreservesConstantAssociatedColorAcrossProfiles()
    {
        PrismNeighborhoodPlan plan = CreatePlan(
            PrismFilterId.RadialBlur,
            new DrawRect(0, 0, 7, 5),
            (state, entry) =>
            {
                SetSymbol(state, entry, "Mode", "Zoom");
                SetNumber(state, entry, "Amount", 0.8f);
                SetSymbol(state, entry, "Quality", "Best");
            });
        PrismPremultipliedColor input =
            PrismPremultipliedColor.FromStraight(0.2, 0.6, 0.9, 0.4);

        foreach (PrismColorProfile profile in
            Enum.GetValues<PrismColorProfile>())
        {
            PrismPremultipliedColor working =
                PrismColorPipeline.ConvertInputToWorking(input, profile);
            PrismPremultipliedColor[] actual = PrismNeighborhoodMath.Apply(
                plan,
                Enumerable.Repeat(working, 35).ToArray(),
                7,
                5,
                profile);
            Assert.All(actual, pixel =>
            {
                AssertColor(pixel, working, 0.00001);
                AssertFiniteAssociated(pixel);
            });
        }
    }

    [Fact]
    public void MotionBlurPreservesConstantAssociatedColorAcrossProfiles()
    {
        PrismNeighborhoodPlan plan = CreatePlan(
            PrismFilterId.MotionBlur,
            new DrawRect(0, 0, 7, 5),
            (state, entry) =>
            {
                SetNumber(state, entry, "Distance", 7);
                SetNumber(state, entry, "Angle", 31);
                SetSymbol(state, entry, "Quality", "Draft");
                SetSymbol(state, entry, "EdgeMode", "Clamp");
            });
        Assert.Equal(5, Assert.Single(plan.Passes).SampleCount);
        PrismPremultipliedColor input =
            PrismPremultipliedColor.FromStraight(0.2, 0.6, 0.9, 0.4);

        foreach (PrismColorProfile profile in
            Enum.GetValues<PrismColorProfile>())
        {
            PrismPremultipliedColor working =
                PrismColorPipeline.ConvertInputToWorking(input, profile);
            PrismPremultipliedColor[] actual = PrismNeighborhoodMath.Apply(
                plan,
                Enumerable.Repeat(working, 35).ToArray(),
                7,
                5,
                profile);
            Assert.All(
                actual,
                pixel =>
                {
                    AssertColor(pixel, working, 0.00001);
                    AssertFiniteAssociated(pixel);
                });
        }
    }

    [Fact]
    public void AddNoiseIsDeterministicSeededAndMonochromatic()
    {
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(
                PrismPremultipliedColor.FromStraight(
                    0.5,
                    0.5,
                    0.5,
                    1),
                32)
            .ToArray();
        PrismNeighborhoodPlan plan = NoisePlan(seed: 42);

        PrismPremultipliedColor[] first =
            PrismNeighborhoodMath.Apply(
                plan,
                source,
                source.Length,
                1,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] repeated =
            PrismNeighborhoodMath.Apply(
                plan,
                source,
                source.Length,
                1,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] changed =
            PrismNeighborhoodMath.Apply(
                NoisePlan(seed: 43),
                source,
                source.Length,
                1,
                PrismColorProfile.LinearSrgb);

        Assert.Equal(first, repeated);
        Assert.False(first.SequenceEqual(changed));
        Assert.All(
            first,
            pixel =>
            {
                Assert.Equal(
                    pixel.Red,
                    pixel.Green,
                    precision: 7);
                Assert.Equal(
                    pixel.Red,
                    pixel.Blue,
                    precision: 7);
                AssertFiniteAssociated(pixel);
            });

        PrismNeighborhoodPlan NoisePlan(int seed) =>
            CreatePlan(
                PrismFilterId.AddNoise,
                configure: (state, entry) =>
                {
                    SetNumber(state, entry, "Amount", 0.2f);
                    SetBoolean(
                        state,
                        entry,
                        "Monochromatic",
                        true);
                    SetInteger(state, entry, "Seed", seed);
                });
    }

    [Fact]
    public void AddNoiseGaussianHasNormalMomentsAndDiffersFromUniform()
    {
        const int sampleCount = 16384;
        const float amount = 0.1f;
        PrismPremultipliedColor center =
            PrismPremultipliedColor.FromStraight(0.5, 0.5, 0.5, 0.75);
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(center, sampleCount).ToArray();

        PrismPremultipliedColor[] gaussianPixels = Apply("Gaussian");
        PrismPremultipliedColor[] uniformPixels = Apply("Uniform");
        double[] gaussian = gaussianPixels
            .Select(pixel =>
                ((pixel.Red / pixel.Alpha) - 0.5) / amount)
            .ToArray();
        double[] uniform = uniformPixels
            .Select(pixel =>
                ((pixel.Red / pixel.Alpha) - 0.5) / amount)
            .ToArray();

        Assert.All(gaussianPixels, pixel =>
            Assert.Equal(center.Alpha, pixel.Alpha, precision: 7));
        Assert.All(uniformPixels, pixel =>
            Assert.Equal(center.Alpha, pixel.Alpha, precision: 7));
        Assert.InRange(gaussian.Average(), -0.04, 0.04);
        Assert.InRange(Variance(gaussian), 0.92, 1.08);
        Assert.InRange(Variance(uniform), 0.31, 0.36);
        Assert.Contains(gaussian, sample => Math.Abs(sample) > 2);
        Assert.DoesNotContain(uniform, sample => Math.Abs(sample) > 1.000001);

        PrismPremultipliedColor[] Apply(string distribution)
        {
            PrismNeighborhoodPlan plan = CreatePlan(
                PrismFilterId.AddNoise,
                new DrawRect(0, 0, sampleCount, 1),
                (state, entry) =>
                {
                    SetNumber(state, entry, "Amount", amount);
                    SetSymbol(state, entry, "Distribution", distribution);
                    SetBoolean(state, entry, "Monochromatic", true);
                    SetInteger(state, entry, "Seed", 0x12345678);
                });
            return PrismNeighborhoodMath.Apply(
                plan,
                source,
                sampleCount,
                1,
                PrismColorProfile.LinearSrgb);
        }

        static double Variance(double[] samples)
        {
            double mean = samples.Average();
            return samples.Average(sample =>
                (sample - mean) * (sample - mean));
        }
    }

    [Fact]
    public void DustScratchesPlansBoundedAdaptiveWindow()
    {
        PrismNeighborhoodPlan plan = CreatePlan(
            PrismFilterId.DustScratches,
            new DrawRect(0, 0, 32, 24),
            (state, entry) =>
            {
                SetNumber(state, entry, "Radius", 6);
                SetNumber(state, entry, "Threshold", -0.2f);
            });

        PrismNeighborhoodPass pass = Assert.Single(plan.Passes);
        Assert.Equal(3, pass.RadiusX);
        Assert.Equal(3, pass.RadiusY);
        Assert.Equal(49, pass.SampleCount);
        Assert.False(pass.IsNoOp);
        Assert.Equal(3, plan.Options0.X);
        Assert.Equal(0, plan.Options0.Y);
    }

    [Fact]
    public void DustScratchesExpandsWindowAndPreservesCoverage()
    {
        const int size = 7;
        int center = (size / 2 * size) + (size / 2);
        PrismPremultipliedColor background =
            PrismPremultipliedColor.FromStraight(0.2, 0.3, 0.4, 0.8);
        PrismPremultipliedColor impulse =
            PrismPremultipliedColor.FromStraight(1, 1, 1, 0.35);
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(background, size * size).ToArray();
        for (int y = 2; y <= 4; y++)
        {
            for (int x = 2; x <= 4; x++)
            {
                source[(y * size) + x] = impulse;
            }
        }

        PrismPremultipliedColor[] radiusOne = Apply(radius: 1, threshold: 0);
        PrismPremultipliedColor[] radiusTwo = Apply(radius: 2, threshold: 0);
        PrismPremultipliedColor[] protectedPixels =
            Apply(radius: 2, threshold: 1);

        AssertColor(radiusOne[center], impulse, 0.00001);
        Assert.Equal(impulse.Alpha, radiusTwo[center].Alpha, precision: 7);
        Assert.InRange(
            radiusTwo[center].Red / radiusTwo[center].Alpha,
            0.19999,
            0.20001);
        Assert.InRange(
            radiusTwo[center].Green / radiusTwo[center].Alpha,
            0.29999,
            0.30001);
        Assert.InRange(
            radiusTwo[center].Blue / radiusTwo[center].Alpha,
            0.39999,
            0.40001);
        AssertColor(protectedPixels[center], impulse, 0.00001);
        Assert.All(radiusTwo, AssertFiniteAssociated);

        PrismPremultipliedColor[] Apply(float radius, float threshold)
        {
            PrismNeighborhoodPlan plan = CreatePlan(
                PrismFilterId.DustScratches,
                new DrawRect(0, 0, size, size),
                (state, entry) =>
                {
                    SetNumber(state, entry, "Radius", radius);
                    SetNumber(state, entry, "Threshold", threshold);
                });
            return PrismNeighborhoodMath.Apply(
                plan,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);
        }
    }

    [Fact]
    public void DustScratchesKeepsNonImpulseDetail()
    {
        const int size = 5;
        PrismPremultipliedColor[] gradient = new PrismPremultipliedColor[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                double value = (x + y) / 8.0;
                gradient[(y * size) + x] =
                    PrismPremultipliedColor.FromStraight(value, value, value, 0.6);
            }
        }

        PrismNeighborhoodPlan plan = CreatePlan(
            PrismFilterId.DustScratches,
            new DrawRect(0, 0, size, size),
            (state, entry) =>
            {
                SetNumber(state, entry, "Radius", 2);
                SetNumber(state, entry, "Threshold", 0);
            });
        PrismPremultipliedColor[] result = PrismNeighborhoodMath.Apply(
            plan,
            gradient,
            size,
            size,
            PrismColorProfile.LinearSrgb);

        AssertColor(result[12], gradient[12], 0.000001);
    }

    [Fact]
    public void DespecklePlansProgressiveDetectionFilteringAndDecodePasses()
    {
        PrismNeighborhoodPlan plan = CreatePlan(
            PrismFilterId.Despeckle,
            new DrawRect(0, 0, 32, 24),
            (state, entry) =>
            {
                SetNumber(state, entry, "Threshold", 0.2f);
                SetNumber(state, entry, "Radius", 3);
            });

        Assert.Equal(7, plan.Passes.Length);
        Assert.Equal(
            [
                PrismNeighborhoodPassKind.DespeckleDetect,
                PrismNeighborhoodPassKind.DespeckleDetect,
                PrismNeighborhoodPassKind.DespeckleDetect,
                PrismNeighborhoodPassKind.DespeckleFilter,
                PrismNeighborhoodPassKind.DespeckleFilter,
                PrismNeighborhoodPassKind.DespeckleFilter,
                PrismNeighborhoodPassKind.DespeckleDecode
            ],
            plan.Passes.Select(pass => pass.Kind));
        Assert.All(plan.Passes, pass =>
        {
            Assert.Equal(3, pass.RadiusX);
            Assert.Equal(3, pass.RadiusY);
            Assert.False(pass.IsNoOp);
        });
        Assert.Equal(0.2f, plan.Options0.X);
        Assert.Equal(3, plan.Options0.Y);

        PrismGraph graph = CreateGraph(
            PrismFilterId.Despeckle,
            new DrawRect(0, 0, 32, 24),
            (state, entry) =>
            {
                SetNumber(state, entry, "Threshold", 0.2f);
                SetNumber(state, entry, "Radius", 3);
            });
        PrismGraphNode[] nodes = graph.Nodes
            .Where(node => node.Filter == PrismFilterId.Despeckle)
            .OrderBy(node => node.NeighborhoodPassIndex)
            .ToArray();
        Assert.Equal(7, nodes.Length);
        PrismGraphNodeId originalSource = Assert.Single(
            graph.Edges.Where(edge =>
                edge.Target == nodes[0].Id &&
                edge.Kind == PrismGraphEdgeKind.Content)).Source;
        Assert.All(nodes, node =>
            Assert.Equal(
                originalSource,
                Assert.Single(graph.Edges.Where(edge =>
                    edge.Target == node.Id &&
                    edge.Kind == PrismGraphEdgeKind.FilterOriginal)).Source));

        PrismGraphExecutionPlan execution =
            new PrismGraphOptimizer().Optimize(graph);
        PrismGraphSurfaceLifetime originalLifetime =
            execution.SurfaceLifetimes.Single(lifetime =>
                lifetime.NodeId == originalSource);
        Assert.True(
            originalLifetime.LastStep >=
                execution.ExecutionOrder.IndexOf(nodes[^1].Id));
    }

    [Fact]
    public void DespeckleRemovesIsolatedImpulsePreservesCoverageAndHonorsThreshold()
    {
        const int size = 7;
        int center = (size / 2 * size) + (size / 2);
        PrismPremultipliedColor background =
            PrismPremultipliedColor.FromStraight(0.2, 0.3, 0.4, 0.6);
        PrismPremultipliedColor impulse =
            PrismPremultipliedColor.FromStraight(1, 1, 1, 0.6);
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(background, size * size).ToArray();
        source[center] = impulse;

        PrismPremultipliedColor[] filtered = Apply(threshold: 0.1f);
        PrismPremultipliedColor[] protectedPixels = Apply(threshold: 1f);

        AssertColor(filtered[center], background, 0.00001);
        Assert.Equal(impulse.Alpha, filtered[center].Alpha, precision: 7);
        AssertColor(protectedPixels[center], impulse, 0.00001);
        Assert.All(filtered, AssertFiniteAssociated);

        PrismPremultipliedColor[] Apply(float threshold)
        {
            PrismNeighborhoodPlan plan = CreatePlan(
                PrismFilterId.Despeckle,
                new DrawRect(0, 0, size, size),
                (state, entry) =>
                {
                    SetNumber(state, entry, "Threshold", threshold);
                    SetNumber(state, entry, "Radius", 1);
                });
            return PrismNeighborhoodMath.Apply(
                plan,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);
        }
    }

    [Fact]
    public void DespeckleRadiusControlsIsotropicProgressiveRestoration()
    {
        const int size = 9;
        PrismPremultipliedColor background =
            PrismPremultipliedColor.FromStraight(0.15, 0.15, 0.15, 1);
        PrismPremultipliedColor impulse =
            PrismPremultipliedColor.FromStraight(0.95, 0.95, 0.95, 1);
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(background, size * size).ToArray();
        for (int y = 2; y <= 6; y++)
        {
            for (int x = 2; x <= 6; x++)
            {
                source[(y * size) + x] = impulse;
            }
        }

        PrismPremultipliedColor[] radiusOne = Apply(radius: 1);
        PrismPremultipliedColor[] radiusThree = Apply(radius: 3);
        int center = (4 * size) + 4;

        AssertColor(radiusOne[center], impulse, 0.00001);
        AssertColor(radiusThree[center], background, 0.00001);
        Assert.False(radiusOne.SequenceEqual(radiusThree));

        PrismPremultipliedColor[] Apply(float radius)
        {
            PrismNeighborhoodPlan plan = CreatePlan(
                PrismFilterId.Despeckle,
                new DrawRect(0, 0, size, size),
                (state, entry) =>
                {
                    SetNumber(state, entry, "Threshold", 0.2f);
                    SetNumber(state, entry, "Radius", radius);
                });
            return PrismNeighborhoodMath.Apply(
                plan,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);
        }
    }

    [Fact]
    public void ShapeBlurUsesNormalizedArbitraryAsymmetricPsfAndPreservesAlpha()
    {
        const int size = 9;
        int center = size / 2;
        PrismPremultipliedColor[] impulse = new PrismPremultipliedColor[size];
        impulse[center] = PrismPremultipliedColor.FromStraight(1, 0.5, 0.25, 0.6);
        PrismNeighborhoodPlan plan = CreatePlan(
            PrismFilterId.ShapeBlur,
            new DrawRect(0, 0, size, 1),
            (state, entry) =>
            {
                SetNumber(state, entry, "Radius", 2);
                SetSymbol(state, entry, "Quality", "Good");
                SetSymbol(state, entry, "EdgeMode", "Transparent");
            });

        Assert.Equal(9, Assert.Single(plan.Passes).SampleCount);
        Assert.Equal(2, Assert.Single(plan.Passes).BoundsRadiusX);
        Assert.True(plan.ResourceRequired);
        Assert.True(plan.Resource.Value > 0);

        PrismPremultipliedColor[] identity = PrismNeighborhoodMath.Apply(
            plan,
            impulse,
            size,
            1,
            PrismColorProfile.LinearSrgb,
            resource: uv => MathF.Abs(uv.X - 0.5f) < 0.01f &&
                MathF.Abs(uv.Y - 0.5f) < 0.01f
                    ? Vector4.One
                    : Vector4.Zero);
        AssertColors(impulse, identity);

        PrismPremultipliedColor[] shifted = PrismNeighborhoodMath.Apply(
            plan,
            impulse,
            size,
            1,
            PrismColorProfile.LinearSrgb,
            resource: uv => uv.X > 0.9f &&
                MathF.Abs(uv.Y - 0.5f) < 0.01f
                    ? new Vector4(0, 0, 0, 7)
                    : Vector4.Zero);
        AssertColor(shifted[center + 2], impulse[center], 0.000001);
        Assert.Equal(0, shifted[center].Alpha);
        Assert.InRange(shifted.Sum(pixel => pixel.Alpha), 0.599999, 0.600001);
        Assert.All(shifted, AssertFiniteAssociated);

        PrismPremultipliedColor[] emptyPsf = PrismNeighborhoodMath.Apply(
            plan,
            impulse,
            size,
            1,
            PrismColorProfile.LinearSrgb,
            resource: _ => Vector4.Zero);
        AssertColors(impulse, emptyPsf);

        static void AssertColors(
            PrismPremultipliedColor[] expected,
            PrismPremultipliedColor[] actual)
        {
            Assert.Equal(expected.Length, actual.Length);
            for (int index = 0; index < expected.Length; index++)
            {
                AssertColor(actual[index], expected[index], 0.0000001);
            }
        }
    }

    [Fact]
    public void SmartBlurIsNormalizedBilateralWithEffectiveParametersAndModes()
    {
        const int width = 9;
        PrismPremultipliedColor dark =
            PrismPremultipliedColor.FromStraight(0.1, 0.1, 0.1, 0.6);
        PrismPremultipliedColor light =
            PrismPremultipliedColor.FromStraight(0.9, 0.9, 0.9, 0.6);
        PrismPremultipliedColor[] edge = Enumerable.Range(0, width)
            .Select(index => index < width / 2 ? dark : light)
            .ToArray();

        PrismNeighborhoodPlan strict = SmartPlan(3, 0.01f, "Best", "Normal");
        PrismNeighborhoodPlan permissive = SmartPlan(3, 1, "Best", "Normal");
        PrismNeighborhoodPass strictPass = Assert.Single(strict.Passes);
        Assert.Equal(3, strictPass.RadiusX);
        Assert.Equal(3, strictPass.RadiusY);
        Assert.Equal(0, strictPass.BoundsRadiusX);
        Assert.Equal(0, strictPass.BoundsRadiusY);
        Assert.Equal(17, strictPass.SampleCount);
        Assert.Equal(3, strict.Options0.X);
        Assert.Equal(0.01f, strict.Options0.Y);
        Assert.Equal(0, strict.Options0.W);
        Assert.Equal(0, strict.Options1.X);

        PrismPremultipliedColor[] preserved = PrismNeighborhoodMath.Apply(
            strict, edge, width, 1, PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] softened = PrismNeighborhoodMath.Apply(
            permissive, edge, width, 1, PrismColorProfile.LinearSrgb);
        Assert.True(preserved[3].Red < softened[3].Red);
        Assert.True(preserved[4].Red > softened[4].Red);

        PrismPremultipliedColor constant =
            PrismPremultipliedColor.FromStraight(0.25, 0.5, 0.75, 0.4);
        foreach (PrismColorProfile profile in Enum.GetValues<PrismColorProfile>())
        {
            PrismPremultipliedColor working =
                PrismColorPipeline.ConvertInputToWorking(constant, profile);
            PrismPremultipliedColor[] result = PrismNeighborhoodMath.Apply(
                permissive,
                Enumerable.Repeat(working, width).ToArray(),
                width,
                1,
                profile);
            Assert.All(result, pixel =>
            {
                AssertColor(pixel, working, 0.00001);
                AssertFiniteAssociated(pixel);
            });
        }

        PrismNeighborhoodPlan draft = SmartPlan(1, 1, "Draft", "Normal");
        Assert.Equal(5, Assert.Single(draft.Passes).SampleCount);
        Assert.False(PrismNeighborhoodMath.Apply(
            draft, edge, width, 1, PrismColorProfile.LinearSrgb).SequenceEqual(
            softened));

        PrismNeighborhoodPlan zero = SmartPlan(0, 1, "Good", "Normal");
        Assert.True(Assert.Single(zero.Passes).IsNoOp);
        PrismPremultipliedColor[] zeroResult = PrismNeighborhoodMath.Apply(
            zero, edge, width, 1, PrismColorProfile.LinearSrgb);
        Assert.Equal(edge.Length, zeroResult.Length);
        for (int index = 0; index < edge.Length; index++)
        {
            AssertColor(zeroResult[index], edge[index], 0.0000001);
        }

        PrismPremultipliedColor[] edgeOnly = PrismNeighborhoodMath.Apply(
            SmartPlan(3, 1, "Best", "EdgeOnly"),
            edge, width, 1, PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] overlay = PrismNeighborhoodMath.Apply(
            SmartPlan(3, 1, "Best", "OverlayEdge"),
            edge, width, 1, PrismColorProfile.LinearSrgb);
        Assert.False(edgeOnly.SequenceEqual(softened));
        Assert.False(overlay.SequenceEqual(edgeOnly));
        Assert.All(edgeOnly, AssertFiniteAssociated);
        Assert.All(overlay, AssertFiniteAssociated);

        PrismNeighborhoodPlan SmartPlan(
            float radius,
            float threshold,
            string quality,
            string mode) => CreatePlan(
                PrismFilterId.SmartBlur,
                new DrawRect(0, 0, width, 1),
                (state, entry) =>
                {
                    SetNumber(state, entry, "Radius", radius);
                    SetNumber(state, entry, "Threshold", threshold);
                    SetSymbol(state, entry, "Quality", quality);
                    SetSymbol(state, entry, "Mode", mode);
                    SetSymbol(state, entry, "EdgeMode", "Clamp");
                });
    }

    [Fact]
    public void SurfaceBlurIsLuminanceBilateralAndPreservesAlphaAndEdges()
    {
        const int width = 9;
        PrismPremultipliedColor dark =
            PrismPremultipliedColor.FromStraight(0.1, 0.1, 0.1, 0.35);
        PrismPremultipliedColor light =
            PrismPremultipliedColor.FromStraight(0.9, 0.9, 0.9, 0.8);
        PrismPremultipliedColor[] edge = Enumerable.Range(0, width)
            .Select(index => index < 4 ? dark : light)
            .ToArray();

        PrismNeighborhoodPlan strict = SurfacePlan(3, 0.01f, "Best", "Clamp");
        PrismNeighborhoodPlan permissive = SurfacePlan(3, 1, "Best", "Clamp");
        PrismNeighborhoodPass pass = Assert.Single(strict.Passes);
        Assert.InRange(pass.RadiusX, 2.99999f, 3.00001f);
        Assert.InRange(pass.RadiusY, 2.99999f, 3.00001f);
        Assert.InRange(pass.BoundsRadiusX, -0.00001f, 0.00001f);
        Assert.InRange(pass.BoundsRadiusY, -0.00001f, 0.00001f);
        Assert.InRange(pass.SampleCount, 17, 17);
        Assert.InRange(strict.Options0.Y, 0.00999f, 0.01001f);
        Assert.InRange(strict.Options1.X, -0.00001f, 0.00001f);

        PrismPremultipliedColor[] preserved = PrismNeighborhoodMath.Apply(
            strict, edge, width, 1, PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] softened = PrismNeighborhoodMath.Apply(
            permissive, edge, width, 1, PrismColorProfile.LinearSrgb);
        Assert.True(preserved[3].Red < softened[3].Red);
        Assert.True(preserved[4].Red > softened[4].Red);
        Assert.All(preserved, AssertFiniteAssociated);
        Assert.All(softened, AssertFiniteAssociated);

        PrismPremultipliedColor constant =
            PrismPremultipliedColor.FromStraight(0.25, 0.5, 0.75, 0.4);
        foreach (PrismColorProfile profile in Enum.GetValues<PrismColorProfile>())
        {
            PrismPremultipliedColor working =
                PrismColorPipeline.ConvertInputToWorking(constant, profile);
            PrismPremultipliedColor[] result = PrismNeighborhoodMath.Apply(
                permissive,
                Enumerable.Repeat(working, width).ToArray(),
                width,
                1,
                profile);
            Assert.All(result, pixel => AssertColor(pixel, working, 0.00001));
        }

        PrismNeighborhoodPlan zero = SurfacePlan(0, 1, "Good", "Clamp");
        Assert.True(Assert.Single(zero.Passes).IsNoOp);
        PrismPremultipliedColor[] zeroResult = PrismNeighborhoodMath.Apply(
            zero, edge, width, 1, PrismColorProfile.LinearSrgb);
        for (int index = 0; index < edge.Length; index++)
        {
            AssertColor(zeroResult[index], edge[index], 0.0000001);
        }

        PrismNeighborhoodPlan draft = SurfacePlan(1, 1, "Draft", "Clamp");
        Assert.InRange(Assert.Single(draft.Passes).SampleCount, 5, 5);
        PrismPremultipliedColor[] draftResult = PrismNeighborhoodMath.Apply(
            draft, edge, width, 1, PrismColorProfile.LinearSrgb);
        Assert.True(Math.Abs(draftResult[3].Red - softened[3].Red) > 0.000001);

        PrismPremultipliedColor[] transparent = PrismNeighborhoodMath.Apply(
            SurfacePlan(3, 1, "Best", "Transparent"),
            Enumerable.Repeat(constant, width).ToArray(),
            width,
            1,
            PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] clamped = PrismNeighborhoodMath.Apply(
            permissive,
            Enumerable.Repeat(constant, width).ToArray(),
            width,
            1,
            PrismColorProfile.LinearSrgb);
        Assert.True(transparent[0].Alpha < clamped[0].Alpha);

        PrismPremultipliedColor equalLumaRed =
            PrismPremultipliedColor.FromStraight(0.5, 0, 0, 1);
        PrismPremultipliedColor equalLumaGreen =
            PrismPremultipliedColor.FromStraight(0, 0.14863, 0, 1);
        PrismPremultipliedColor[] chromaEdge = Enumerable.Range(0, width)
            .Select(index => index < 4 ? equalLumaRed : equalLumaGreen)
            .ToArray();
        PrismPremultipliedColor[] surfaceChroma = PrismNeighborhoodMath.Apply(
            SurfacePlan(3, 0.01f, "Best", "Clamp"),
            chromaEdge, width, 1, PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] smartChroma = PrismNeighborhoodMath.Apply(
            CreatePlan(
                PrismFilterId.SmartBlur,
                new DrawRect(0, 0, width, 1),
                (state, entry) =>
                {
                    SetNumber(state, entry, "Radius", 3);
                    SetNumber(state, entry, "Threshold", 0.01f);
                    SetSymbol(state, entry, "Quality", "Best");
                    SetSymbol(state, entry, "Mode", "Normal");
                    SetSymbol(state, entry, "EdgeMode", "Clamp");
                }),
            chromaEdge, width, 1, PrismColorProfile.LinearSrgb);
        Assert.True(surfaceChroma[3].Green > smartChroma[3].Green + 0.000001);

        PrismNeighborhoodPlan SurfacePlan(
            float radius,
            float threshold,
            string quality,
            string edgeMode) => CreatePlan(
                PrismFilterId.SurfaceBlur,
                new DrawRect(0, 0, width, 1),
                (state, entry) =>
                {
                    SetNumber(state, entry, "Radius", radius);
                    SetNumber(state, entry, "Threshold", threshold);
                    SetSymbol(state, entry, "Quality", quality);
                    SetSymbol(state, entry, "EdgeMode", edgeMode);
                });
    }

    [Fact]
    public void FieldBlurUsesDepthDerivedCircleOfConfusionAndHighlightWeighting()
    {
        const int width = 9;
        const int center = width / 2;
        PrismPremultipliedColor black =
            PrismPremultipliedColor.FromStraight(0, 0, 0, 1);
        PrismPremultipliedColor white =
            PrismPremultipliedColor.FromStraight(1, 1, 1, 1);
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(black, width).ToArray();
        source[center] = white;

        PrismNeighborhoodPlan plan = FieldPlan(highlight: 0);
        PrismNeighborhoodPass pass = Assert.Single(plan.Passes);
        Assert.Equal(3, pass.RadiusX, precision: 6);
        Assert.Equal(17, pass.SampleCount);
        Assert.True(plan.ResourceRequired);

        PrismPremultipliedColor[] focused = PrismNeighborhoodMath.Apply(
            plan,
            source,
            width,
            1,
            PrismColorProfile.LinearSrgb,
            resource: _ => new Vector4(0.25f));
        for (int index = 0; index < width; index++)
        {
            AssertColor(focused[index], source[index], 0.000001);
        }

        PrismPremultipliedColor[] blurred = PrismNeighborhoodMath.Apply(
            plan,
            source,
            width,
            1,
            PrismColorProfile.LinearSrgb,
            resource: _ => Vector4.Zero);
        Assert.True(blurred[center].Red < focused[center].Red);
        Assert.True(blurred[center - 1].Red > focused[center - 1].Red);

        PrismPremultipliedColor[] inverted = PrismNeighborhoodMath.Apply(
            plan with
            {
                Options0 = new Vector4(
                    plan.Options0.X,
                    1,
                    plan.Options0.Z,
                    plan.Options0.W)
            },
            source,
            width,
            1,
            PrismColorProfile.LinearSrgb,
            resource: _ => new Vector4(0.25f));
        Assert.True(inverted[center].Red < focused[center].Red);

        PrismPremultipliedColor[] highlighted = PrismNeighborhoodMath.Apply(
            FieldPlan(highlight: 3),
            source,
            width,
            1,
            PrismColorProfile.LinearSrgb,
            resource: _ => Vector4.Zero);
        Assert.True(highlighted[center].Red > blurred[center].Red);
        Assert.All(highlighted, AssertFiniteAssociated);

        PrismNeighborhoodPlan FieldPlan(float highlight) => CreatePlan(
            PrismFilterId.FieldBlur,
            new DrawRect(0, 0, width, 1),
            (state, entry) =>
            {
                SetNumber(state, entry, "Blur", 3);
                SetNumber(state, entry, "FocalDistance", 0.25f);
                SetBoolean(state, entry, "Invert", false);
                SetSymbol(state, entry, "Quality", "Best");
                SetNumber(state, entry, "Highlight", highlight);
            });
    }

    [Fact]
    public void IrisBlurUsesRotatedEllipticalFocusMaskAndSmoothTransition()
    {
        const int size = 31;
        const int center = size / 2;
        PrismPremultipliedColor black =
            PrismPremultipliedColor.FromStraight(0, 0, 0, 1);
        PrismPremultipliedColor white =
            PrismPremultipliedColor.FromStraight(1, 1, 1, 1);
        PrismPremultipliedColor[] checker = new PrismPremultipliedColor[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                checker[(y * size) + x] = ((x + y) & 1) == 0
                    ? white
                    : black;
            }
        }

        PrismNeighborhoodPlan horizontal = IrisPlan(rotation: 0, blur: 3);
        PrismNeighborhoodPlan vertical = IrisPlan(rotation: 90, blur: 3);
        PrismPremultipliedColor[] horizontalResult = PrismNeighborhoodMath.Apply(
            horizontal,
            checker,
            size,
            size,
            PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] verticalResult = PrismNeighborhoodMath.Apply(
            vertical,
            checker,
            size,
            size,
            PrismColorProfile.LinearSrgb);

        int focusedPoint = (center * size) + center + 6;
        AssertColor(horizontalResult[focusedPoint], white, 0.000001);
        Assert.True(verticalResult[focusedPoint].Red < 0.99);
        AssertColor(
            horizontalResult[(center * size) + center],
            white,
            0.000001);
        AssertColor(
            verticalResult[(center * size) + center],
            white,
            0.000001);
        Assert.All(verticalResult, AssertFiniteAssociated);

        PrismNeighborhoodPlan noOp = IrisPlan(rotation: 0, blur: 0);
        Assert.True(Assert.Single(noOp.Passes).IsNoOp);

        PrismNeighborhoodPlan IrisPlan(float rotation, float blur) => CreatePlan(
            PrismFilterId.IrisBlur,
            new DrawRect(0, 0, size, size),
            (state, entry) =>
            {
                SetVector(state, entry, "Center", new Vector4(0.5f, 0.5f, 0, 0));
                SetVector(state, entry, "Radius", new Vector4(0.25f, 0.1f, 0, 0));
                SetNumber(state, entry, "Feather", 0.1f);
                SetNumber(state, entry, "Rotation", rotation);
                SetNumber(state, entry, "Blur", blur);
            });
    }

    [Fact]
    public void TiltShiftUsesPlanarCircleOfConfusionWithSmoothFeather()
    {
        const int size = 31;
        const int center = size / 2;
        PrismPremultipliedColor black =
            PrismPremultipliedColor.FromStraight(0, 0, 0, 1);
        PrismPremultipliedColor white =
            PrismPremultipliedColor.FromStraight(1, 1, 1, 1);
        PrismPremultipliedColor[] checker = new PrismPremultipliedColor[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                checker[(y * size) + x] = ((x + y) & 1) == 0
                    ? white
                    : black;
            }
        }

        PrismPremultipliedColor[] horizontal = PrismNeighborhoodMath.Apply(
            TiltPlan(angle: 0, blur: 3),
            checker,
            size,
            size,
            PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] vertical = PrismNeighborhoodMath.Apply(
            TiltPlan(angle: 90, blur: 3),
            checker,
            size,
            size,
            PrismColorProfile.LinearSrgb);
        int horizontalPoint = (center * size) + center + 6;
        int verticalPoint = ((center + 6) * size) + center;
        AssertColor(horizontal[horizontalPoint], white, 0.000001);
        Assert.True(horizontal[verticalPoint].Red < 0.99);
        AssertColor(vertical[verticalPoint], white, 0.000001);
        Assert.True(vertical[horizontalPoint].Red < 0.99);
        Assert.All(horizontal, AssertFiniteAssociated);
        Assert.True(Assert.Single(TiltPlan(0, 0).Passes).IsNoOp);

        PrismNeighborhoodPlan TiltPlan(float angle, float blur) => CreatePlan(
            PrismFilterId.TiltShift,
            new DrawRect(0, 0, size, size),
            (state, entry) =>
            {
                SetVector(state, entry, "Center", new Vector4(0.5f, 0.5f, 0, 0));
                SetNumber(state, entry, "Angle", angle);
                SetNumber(state, entry, "FocusWidth", 0.1f);
                SetNumber(state, entry, "Feather", 0.1f);
                SetNumber(state, entry, "Blur", blur);
            });
    }

    [Fact]
    public void PathBlurFollowsCurvedFieldWithBidirectionalRk4Advection()
    {
        const int size = 21;
        const int center = size / 2;
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[size * size];
        source[(15 * size) + 11] =
            PrismPremultipliedColor.FromStraight(1, 1, 1, 1);
        PrismNeighborhoodPlan plan = PathPlan(speed: 16);

        PrismPremultipliedColor[] curved = PrismNeighborhoodMath.Apply(
            plan,
            source,
            size,
            size,
            PrismColorProfile.LinearSrgb,
            resource: uv =>
            {
                Vector2 radial = new(
                    (uv.X * size) - (center + 0.5f),
                    (uv.Y * size) - (center + 0.5f));
                Vector2 tangent = radial.LengthSquared() > 0.000001f
                    ? Vector2.Normalize(new Vector2(-radial.Y, radial.X))
                    : Vector2.UnitY;
                return new Vector4(
                    (tangent.X + 1) * 0.5f,
                    (tangent.Y + 1) * 0.5f,
                    0.5f,
                    1);
            });
        PrismPremultipliedColor[] straight = PrismNeighborhoodMath.Apply(
            plan,
            source,
            size,
            size,
            PrismColorProfile.LinearSrgb,
            resource: _ => new Vector4(0.5f, 1, 0.5f, 1));

        int anchor = (center * size) + 15;
        Assert.True(curved[anchor].Alpha > 0.01);
        Assert.True(curved[anchor].Alpha > straight[anchor].Alpha + 0.01);
        Assert.Equal(
            curved,
            PrismNeighborhoodMath.Apply(
                plan,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb,
                resource: uv =>
                {
                    Vector2 radial = new(
                        (uv.X * size) - (center + 0.5f),
                        (uv.Y * size) - (center + 0.5f));
                    Vector2 tangent = radial.LengthSquared() > 0.000001f
                        ? Vector2.Normalize(new Vector2(-radial.Y, radial.X))
                        : Vector2.UnitY;
                    return new Vector4(
                        (tangent.X + 1) * 0.5f,
                        (tangent.Y + 1) * 0.5f,
                        0.5f,
                        1);
                }));
        Assert.All(curved, AssertFiniteAssociated);

        PrismNeighborhoodPlan PathPlan(float speed) => CreatePlan(
            PrismFilterId.PathBlur,
            new DrawRect(0, 0, size, size),
            (state, entry) =>
            {
                SetNumber(state, entry, "Speed", speed);
                SetNumber(state, entry, "EndSpeed", speed);
                SetBoolean(state, entry, "CenteredBlur", true);
                SetSymbol(state, entry, "Shape", "Basic");
                SetNumber(state, entry, "Noise", 0);
            });
    }

    [Fact]
    public void PathBlurAnchorsWindowAndUsesEndpointSpeedAndResourceAlpha()
    {
        const int width = 33;
        const int center = width / 2;
        PrismPremultipliedColor white =
            PrismPremultipliedColor.FromStraight(1, 1, 1, 1);
        PrismPremultipliedColor[] behind =
            new PrismPremultipliedColor[width];
        PrismPremultipliedColor[] ahead =
            new PrismPremultipliedColor[width];
        behind[center - 6] = white;
        ahead[center + 6] = white;
        Func<Vector2, Vector4> forward =
            _ => new Vector4(1, 0.5f, 1, 1);

        PrismNeighborhoodPlan rear = PathPlan(
            centered: false,
            flashSync: "Rear",
            speed: 12,
            endSpeed: 12);
        PrismNeighborhoodPlan front = PathPlan(
            centered: false,
            flashSync: "Front",
            speed: 12,
            endSpeed: 12);
        PrismNeighborhoodPlan centered = PathPlan(
            centered: true,
            flashSync: "Rear",
            speed: 12,
            endSpeed: 12);

        Assert.True(Apply(rear, behind, forward)[center].Alpha > 0.01);
        Assert.Equal(0, Apply(rear, ahead, forward)[center].Alpha, precision: 7);
        Assert.True(Apply(front, ahead, forward)[center].Alpha > 0.01);
        Assert.Equal(0, Apply(front, behind, forward)[center].Alpha, precision: 7);
        Assert.True(Apply(centered, behind, forward)[center].Alpha > 0.01);
        Assert.True(Apply(centered, ahead, forward)[center].Alpha > 0.01);

        PrismNeighborhoodPlan variable = PathPlan(
            centered: true,
            flashSync: "Center",
            speed: 2,
            endSpeed: 16);
        PrismPremultipliedColor[] slow =
            Apply(variable, ahead, _ => new Vector4(1, 0.5f, 0, 1));
        PrismPremultipliedColor[] fast =
            Apply(variable, ahead, _ => new Vector4(1, 0.5f, 1, 1));
        Assert.Equal(0, slow[center].Alpha, precision: 7);
        Assert.True(fast[center].Alpha > 0.01);

        PrismPremultipliedColor[] invalid =
            Apply(front, ahead, _ => new Vector4(1, 0.5f, 1, 0));
        Assert.Equal(0, invalid[center].Alpha, precision: 7);

        PrismPremultipliedColor[] Apply(
            PrismNeighborhoodPlan plan,
            PrismPremultipliedColor[] source,
            Func<Vector2, Vector4> resource) =>
            PrismNeighborhoodMath.Apply(
                plan,
                source,
                width,
                1,
                PrismColorProfile.LinearSrgb,
                resource: resource);

        PrismNeighborhoodPlan PathPlan(
            bool centered,
            string flashSync,
            float speed,
            float endSpeed) => CreatePlan(
            PrismFilterId.PathBlur,
            new DrawRect(0, 0, width, 1),
            (state, entry) =>
            {
                SetNumber(state, entry, "Speed", speed);
                SetNumber(state, entry, "EndSpeed", endSpeed);
                SetBoolean(state, entry, "CenteredBlur", centered);
                SetSymbol(state, entry, "Shape", "Basic");
                SetSymbol(state, entry, "FlashSync", flashSync);
                SetNumber(state, entry, "Noise", 0);
            });
    }

    [Fact]
    public void PathBlurTaperAndNoiseAreEffectiveFiniteAndDeterministic()
    {
        const int width = 33;
        const int center = width / 2;
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width];
        source[center + 12] =
            PrismPremultipliedColor.FromStraight(1, 0.5, 0.25, 0.8);
        Func<Vector2, Vector4> path =
            _ => new Vector4(1, 0.5f, 0.5f, 1);
        PrismNeighborhoodPlan basic = PathPlan("Basic", taper: 1, noise: 0);
        PrismNeighborhoodPlan taper = PathPlan("Taper", taper: 1, noise: 0);
        PrismNeighborhoodPlan noisy = PathPlan("Basic", taper: 0, noise: 1);
        PrismNeighborhoodPass pass = Assert.Single(basic.Passes);
        Assert.Equal(17, pass.SampleCount);
        Assert.Equal(12, pass.BoundsRadiusX);
        Assert.Equal(12, pass.BoundsRadiusY);
        Assert.True(
            Assert.Single(
                PathPlan(
                    "Basic",
                    taper: 0,
                    noise: 1,
                    speed: 0).Passes)
                .IsNoOp);

        PrismPremultipliedColor[] basicResult = Apply(basic);
        PrismPremultipliedColor[] taperedResult = Apply(taper);
        PrismPremultipliedColor[] noisyResult = Apply(noisy);

        Assert.True(basicResult[center].Alpha > taperedResult[center].Alpha);
        Assert.NotEqual(basicResult, noisyResult);
        Assert.Equal(noisyResult, Apply(noisy));
        Assert.All(noisyResult, AssertFiniteAssociated);

        PrismPremultipliedColor[] Apply(PrismNeighborhoodPlan plan) =>
            PrismNeighborhoodMath.Apply(
                plan,
                source,
                width,
                1,
                PrismColorProfile.LinearSrgb,
                resource: path);

        PrismNeighborhoodPlan PathPlan(
            string shape,
            float taper,
            float noise,
            float speed = 12) => CreatePlan(
            PrismFilterId.PathBlur,
            new DrawRect(0, 0, width, 1),
            (state, entry) =>
            {
                SetNumber(state, entry, "Speed", speed);
                SetNumber(state, entry, "EndSpeed", speed);
                SetBoolean(state, entry, "CenteredBlur", false);
                SetSymbol(state, entry, "Shape", shape);
                SetSymbol(state, entry, "FlashSync", "Front");
                SetNumber(state, entry, "Taper", taper);
                SetNumber(state, entry, "Noise", noise);
            });
    }

    [Fact]
    public void SpinBlurPlansAnAdaptiveOddTapBudgetFromArcLength()
    {
        PrismNeighborhoodPass small = Assert.Single(
            SpinPlan(16, 16, rotation: 15).Passes);
        PrismNeighborhoodPass large = Assert.Single(
            SpinPlan(256, 128, rotation: 180).Passes);

        Assert.InRange(small.SampleCount, 3, 65);
        Assert.Equal(1, small.SampleCount & 1);
        Assert.True(large.SampleCount > small.SampleCount);
        Assert.Equal(65, large.SampleCount);
        Assert.Equal(0, large.BoundsRadiusX);
        Assert.Equal(0, large.BoundsRadiusY);
        Assert.False(large.IsNoOp);
        Assert.True(
            Assert.Single(
                SpinPlan(16, 16, rotation: 0).Passes)
                .IsNoOp);
    }

    [Fact]
    public void SpinBlurUsesCenteredPixelSpaceArcsAndAssociatedAlpha()
    {
        const int width = 41;
        const int height = 21;
        int centerX = width / 2;
        int centerY = height / 2;
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        source[(centerY * width) + centerX + 8] =
            PrismPremultipliedColor.FromStraight(1, 0.5, 0.25, 0.8);
        PrismNeighborhoodPlan plan = SpinPlan(
            width,
            height,
            rotation: 90,
            radius: new Vector2(0.48f, 0.9f));

        PrismPremultipliedColor[] result =
            PrismNeighborhoodMath.Apply(
                plan,
                source,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        Assert.True(
            result[((centerY - 6) * width) + centerX + 6].Alpha >
            0.001);
        Assert.True(
            result[((centerY + 6) * width) + centerX + 6].Alpha >
            0.001);
        Assert.InRange(
            Math.Abs(
                result[((centerY - 6) * width) + centerX + 6].Alpha -
                result[((centerY + 6) * width) + centerX + 6].Alpha),
            0,
            0.00001);
        Assert.All(result, AssertFiniteAssociated);
    }

    [Fact]
    public void SpinBlurMaskFeatherStrobeAndNoiseAreEffectiveAndDeterministic()
    {
        const int size = 33;
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float alpha = ((x * 7) + (y * 11)) % 17 / 16f;
                source[(y * size) + x] =
                    PrismPremultipliedColor.FromStraight(
                        x / (double)(size - 1),
                        y / (double)(size - 1),
                        ((x + y) & 1) == 0 ? 0.8 : 0.2,
                        alpha);
            }
        }

        PrismNeighborhoodPlan hard = SpinPlan(
            size,
            size,
            rotation: 120,
            radius: new Vector2(0.3f, 0.3f));
        PrismNeighborhoodPlan feathered = SpinPlan(
            size,
            size,
            rotation: 120,
            radius: new Vector2(0.3f, 0.3f),
            feather: 0.75f);
        PrismNeighborhoodPlan strobed = SpinPlan(
            size,
            size,
            rotation: 120,
            radius: new Vector2(0.3f, 0.3f),
            strobeStrength: 1,
            strobeFlashes: 4,
            strobeDuration: 0.25f);
        PrismNeighborhoodPlan noisy = SpinPlan(
            size,
            size,
            rotation: 120,
            radius: new Vector2(0.3f, 0.3f),
            noise: 1);

        PrismPremultipliedColor[] hardResult = Apply(hard);
        PrismPremultipliedColor[] featheredResult = Apply(feathered);
        PrismPremultipliedColor[] strobedResult = Apply(strobed);
        PrismPremultipliedColor[] noisyResult = Apply(noisy);
        int outside = (size / 2 * size) + size - 2;

        AssertColor(hardResult[outside], source[outside], 0.000001);
        Assert.NotEqual(hardResult, featheredResult);
        Assert.NotEqual(hardResult, strobedResult);
        Assert.NotEqual(hardResult, noisyResult);
        Assert.Equal(noisyResult, Apply(noisy));
        Assert.All(noisyResult, AssertFiniteAssociated);

        PrismPremultipliedColor[] Apply(PrismNeighborhoodPlan plan) =>
            PrismNeighborhoodMath.Apply(
                plan,
                source,
                size,
                size,
                PrismColorProfile.LinearSrgb);
    }

    [Fact]
    public void EveryNeighborhoodFilterIsFiniteAndRepeatableOnSmallImages()
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
        Func<Vector2, Vector4> resource =
            uv => new Vector4(
                uv.X,
                uv.Y,
                (uv.X + uv.Y) * 0.5f,
                1);

        foreach (PrismCatalogEntryDescriptor entry in
            NeighborhoodEntries())
        {
            PrismNeighborhoodPlan plan = CreatePlan(
                (PrismFilterId)entry.StableId);
            PrismPremultipliedColor[] first =
                PrismNeighborhoodMath.Apply(
                    plan,
                    source,
                    3,
                    3,
                    PrismColorProfile.LinearSrgb,
                    resource: resource);
            PrismPremultipliedColor[] repeated =
                PrismNeighborhoodMath.Apply(
                    plan,
                    source,
                    3,
                    3,
                    PrismColorProfile.LinearSrgb,
                    resource: resource);

            Assert.Equal(first, repeated);
            Assert.All(first, AssertFiniteAssociated);
        }
    }

    [Fact]
    public void BlurPreservesConstantPremultipliedPixelsAcrossWorkingProfiles()
    {
        PrismNeighborhoodPlan blur =
            CreatePlan(PrismFilterId.Blur);
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
            PrismPremultipliedColor[] result =
                PrismNeighborhoodMath.Apply(
                    blur,
                    source,
                    3,
                    3,
                    profile);

            Assert.All(
                result,
                pixel => AssertColor(
                    pixel,
                    working,
                    tolerance: 0.00001));
        }
    }

    private static PrismPremultipliedColor[] ApplySharpenEdges(
        PrismPremultipliedColor[] source,
        int size,
        float threshold)
    {
        PrismNeighborhoodPlan plan = CreatePlan(
            PrismFilterId.SharpenEdges,
            new DrawRect(0, 0, size, size),
            (state, entry) =>
            {
                SetNumber(state, entry, "Amount", 1);
                SetNumber(state, entry, "Threshold", threshold);
            });

        return PrismNeighborhoodMath.Apply(
            plan,
            source,
            size,
            size,
            PrismColorProfile.LinearSrgb);
    }

    private static PrismNeighborhoodPlan CreatePlan(
        PrismFilterId filter,
        DrawRect? bounds = null,
        Action<PrismFilterState, PrismCatalogEntryDescriptor>?
            configure = null)
    {
        PrismGraph graph = CreateGraph(
            filter,
            bounds ?? new DrawRect(0, 0, 20, 10),
            configure);
        PrismGraphNode node = graph.Nodes.First(candidate =>
            candidate.Kind == PrismGraphNodeKind.Filter);
        return Assert.IsType<PrismNeighborhoodPlan>(
            node.NeighborhoodPlan);
    }

    private static PrismGraph CreateGraph(
        PrismFilterId filter,
        DrawRect bounds,
        Action<PrismFilterState, PrismCatalogEntryDescriptor>?
            configure = null)
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
            scope.Instance.GetLayerState(layer.Id).Filters);
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)filter);
        ConfigureRequiredResources(state, entry);
        configure?.Invoke(state, entry);
        return BuildGraph(scope);
    }

    private static PrismNeighborhoodPlan SpinPlan(
        int width,
        int height,
        float rotation,
        Vector2? radius = null,
        float feather = 0,
        float strobeStrength = 0,
        float strobeFlashes = 0,
        float strobeDuration = 0,
        float noise = 0) =>
        CreatePlan(
            PrismFilterId.SpinBlur,
            new DrawRect(0, 0, width, height),
            (state, entry) =>
            {
                Vector2 ellipse = radius ?? Vector2.One;
                SetVector(
                    state,
                    entry,
                    "Center",
                    new Vector4(0.5f, 0.5f, 0, 0));
                SetVector(
                    state,
                    entry,
                    "Radius",
                    new Vector4(ellipse.X, ellipse.Y, 0, 0));
                SetNumber(state, entry, "Rotation", rotation);
                SetNumber(state, entry, "Feather", feather);
                SetNumber(
                    state,
                    entry,
                    "StrobeStrength",
                    strobeStrength);
                SetInteger(
                    state,
                    entry,
                    "StrobeFlashes",
                    (int)strobeFlashes);
                SetNumber(
                    state,
                    entry,
                    "StrobeDuration",
                    strobeDuration);
                SetNumber(state, entry, "Noise", noise);
            });

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
                    $"neighborhood-{entry.Symbol}-{property.Name}"));
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

    private static PrismCatalogPropertyDescriptor Property(
        PrismCatalogEntryDescriptor entry,
        string name) =>
        entry.Properties.Single(property =>
            property.Name == name);

    private static PrismCatalogEntryDescriptor[]
        NeighborhoodEntries() =>
        PrismCatalogGenerated.Entries
            .Where(entry =>
                entry.Kind == "filter" &&
                entry.Coverage.Test.StartsWith(
                    "PrismNeighborhoodFilterTests/",
                    StringComparison.Ordinal))
            .ToArray();

    private static PrismGraph BuildGraph(
        PrismDrawScope scope)
    {
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(
                new DrawRect(0, 0, 20, 10),
                new Color(255, 255, 255)),
            DrawCommand.EndPrism());
        return new PrismGraphBuilder().Build(
            new PrismFrameAnalyzer().Analyze(commands));
    }

    private static void AssertColor(
        PrismPremultipliedColor actual,
        PrismPremultipliedColor expected,
        double tolerance)
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
}
