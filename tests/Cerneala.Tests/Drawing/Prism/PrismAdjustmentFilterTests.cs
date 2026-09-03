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

public sealed class PrismAdjustmentFilterTests
{
    [Fact]
    public void CatalogDrivesEveryAdjustmentPlannerKernelAndTestBinding()
    {
        PrismCatalogEntryDescriptor[] entries =
            AdjustmentEntries();
        PrismFilterId[] filters = entries
            .Select(entry => (PrismFilterId)entry.StableId)
            .ToArray();
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "All adjustments",
            filters: filters.Select(
                filter => new PrismFilterDefinition(filter)));
        PrismDrawScope drawScope = PrismTestData.Scope(
            PrismTestData.Composition(
                "Adjustment defaults",
                layer));
        PrismGraph graph = BuildGraph(drawScope);
        PrismGraphScope graphScope = Assert.Single(graph.Scopes);
        PrismGraphNode[] nodes = graph.Nodes
            .Where(node =>
                node.Kind == PrismGraphNodeKind.Filter)
            .ToArray();

        Assert.Equal(entries.Length, nodes.Length);
        Assert.Equal(
            entries.Length,
            nodes
                .Select(node =>
                    PrismAdjustmentPlanner
                        .Create(node, graphScope)
                        .Operation)
                .Distinct()
                .Count());
        foreach (PrismCatalogEntryDescriptor entry in entries)
        {
            PrismFilterId filter =
                (PrismFilterId)entry.StableId;
            PrismGraphNode node = nodes.Single(
                candidate => candidate.Filter == filter);
            Assert.True(
                PrismAdjustmentPlanner.IsSupported(filter));
            Assert.Equal(
                $"PrismKernelRegistry/{entry.Symbol}",
                entry.Coverage.Kernel);
            Assert.Equal(
                $"PrismAdjustmentFilterTests/{entry.Symbol}",
                entry.Coverage.Test);
            Assert.StartsWith(
                "generated:",
                entry.Coverage.Documentation,
                StringComparison.Ordinal);
            Assert.Equal(
                entry.Properties.Length,
                node.Parameters.Length);
            Assert.Equal(
                Enumerable.Range(0, entry.Properties.Length),
                node.Parameters.Select(
                    parameter => parameter.Index));
        }
    }

    [Fact]
    public void RuntimeDomainValidationUsesGeneratedCatalogRanges()
    {
        (PrismFilterState brightness, PrismCatalogEntryDescriptor
            brightnessEntry) = CreateState(
                PrismFilterId.BrightnessContrast);
        PrismCatalogPropertyDescriptor brightnessProperty =
            Property(brightnessEntry, "Brightness");

        Assert.Throws<ArgumentOutOfRangeException>(
            () => GeneratedMarkup.SetPrismFilterNumber(
                brightness,
                brightnessEntry.StableId,
                brightnessProperty.TypeSlot,
                float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => GeneratedMarkup.SetPrismFilterNumber(
                brightness,
                brightnessEntry.StableId,
                brightnessProperty.TypeSlot,
                1.0001f));
        GeneratedMarkup.SetPrismFilterNumber(
            brightness,
            brightnessEntry.StableId,
            brightnessProperty.TypeSlot,
            -1);
        GeneratedMarkup.SetPrismFilterNumber(
            brightness,
            brightnessEntry.StableId,
            brightnessProperty.TypeSlot,
            1);

        (PrismFilterState posterize, PrismCatalogEntryDescriptor
            posterizeEntry) = CreateState(
                PrismFilterId.Posterize);
        PrismCatalogPropertyDescriptor levels =
            Property(posterizeEntry, "Levels");
        Assert.Throws<ArgumentOutOfRangeException>(
            () => GeneratedMarkup.SetPrismFilterNumber(
                posterize,
                posterizeEntry.StableId,
                levels.TypeSlot,
                1));
        GeneratedMarkup.SetPrismFilterNumber(
            posterize,
            posterizeEntry.StableId,
            levels.TypeSlot,
            2);

        (PrismFilterState lookup, PrismCatalogEntryDescriptor
            lookupEntry) = CreateState(
                PrismFilterId.ColorLookup);
        PrismCatalogPropertyDescriptor lookupResource =
            Property(lookupEntry, "Lookup");
        Assert.Throws<ArgumentOutOfRangeException>(
            () => GeneratedMarkup.SetPrismFilterResource(
                lookup,
                lookupEntry.StableId,
                lookupResource.TypeSlot,
                default));

        (PrismFilterState balance, PrismCatalogEntryDescriptor
            balanceEntry) = CreateState(
                PrismFilterId.ColorBalance);
        PrismCatalogPropertyDescriptor shadows =
            Property(balanceEntry, "Shadows");
        Assert.Throws<ArgumentOutOfRangeException>(
            () => GeneratedMarkup.SetPrismFilterVector(
                balance,
                balanceEntry.StableId,
                shadows.TypeSlot,
                new Vector4(float.PositiveInfinity)));
    }

    [Fact]
    public void AdvertisedLevelsGammaMinimumIsAssignableThroughFloatApis()
    {
        PrismCatalogOperationInfo operation =
            PrismCatalog.GetFilter(PrismFilterId.Levels);
        PrismCatalogParameterInfo parameter = operation.Parameters.Single(
            candidate => candidate.Name == "Gamma");
        float advertisedMinimum = checked(
            (float)Assert.IsType<double>(parameter.Minimum));
        (PrismFilterState state, _) = CreateState(PrismFilterId.Levels);
        LevelsFilter typedFilter = new();

        Exception? stateFailure = Record.Exception(
            () => state.SetValue(parameter, advertisedMinimum));
        Exception? typedFailure = Record.Exception(
            () => typedFilter.Gamma = advertisedMinimum);

        Assert.True(
            stateFailure is null && typedFailure is null,
            $"The catalog minimum {parameter.Minimum:R} becomes " +
            $"{advertisedMinimum:R} in the public float APIs. " +
            $"Runtime state: {stateFailure?.Message ?? "accepted"}; " +
            $"typed API: {typedFailure?.Message ?? "accepted"}.");
    }

    [Fact]
    public void BrightnessContrastUsesLinearExposureAndPivotedContrast()
    {
        PrismAdjustmentPlan exposure =
            CreatePlan(
                PrismFilterId.BrightnessContrast,
                (state, entry) => SetNumber(
                    state,
                    entry,
                    "Brightness",
                    1));
        AssertStraight(
            PrismAdjustmentMath.Apply(
                exposure,
                PrismPremultipliedColor.FromStraight(
                    0.09,
                    0.18,
                    0.3,
                    1),
                PrismColorProfile.LinearSrgb),
            0.18,
            0.36,
            0.6);

        PrismAdjustmentPlan contrast =
            CreatePlan(
                PrismFilterId.BrightnessContrast,
                (state, entry) => SetNumber(
                    state,
                    entry,
                    "Contrast",
                    0.5f));
        AssertStraight(
            PrismAdjustmentMath.Apply(
                contrast,
                PrismPremultipliedColor.FromStraight(
                    0.09,
                    0.18,
                    0.36,
                    1),
                PrismColorProfile.LinearSrgb),
            0.045,
            0.18,
            0.72);
    }

    [Fact]
    public void BrightnessContrastLegacyRetainsLinearMidpointAdjustment()
    {
        PrismAdjustmentPlan legacy =
            CreatePlan(
                PrismFilterId.BrightnessContrast,
                (state, entry) =>
                {
                    SetNumber(
                        state,
                        entry,
                        "Brightness",
                        0.1f);
                    SetNumber(
                        state,
                        entry,
                        "Contrast",
                        0.5f);
                    SetBoolean(
                        state,
                        entry,
                        "UseLegacy",
                        true);
                });
        AssertStraight(
            PrismAdjustmentMath.Apply(
                legacy,
                PrismPremultipliedColor.FromStraight(
                    0.2,
                    0.4,
                    0.6,
                    1),
                PrismColorProfile.LinearSrgb),
            0.15,
            0.45,
            0.75);
    }

    [Fact]
    public void ExposureCatalogDefinesCompleteTransformContract()
    {
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry(
                (int)PrismFilterId.Exposure);

        Assert.Equal(
            [
                "Contrast",
                "Direction",
                "Exposure",
                "Gamma",
                "LogExposureStep",
                "LogMidGray",
                "Pivot",
                "Style"
            ],
            entry.Properties.Select(
                property => property.Name));
        Assert.DoesNotContain(
            entry.Properties,
            property => property.Name == "Offset");

        PrismCatalogOperationInfo operation =
            PrismCatalog.GetFilter(PrismFilterId.Exposure);
        Assert.Equal(
            ["Linear", "Logarithmic", "Video"],
            operation.Parameters.Single(
                parameter => parameter.Name == "Style").SymbolOptions);
        Assert.Equal(
            ["Forward", "Inverse"],
            operation.Parameters.Single(
                parameter => parameter.Name == "Direction").SymbolOptions);

        PrismAdjustmentPlan plan =
            CreatePlan(PrismFilterId.Exposure);
        Assert.Equal(0, plan.Parameters0.X);
        Assert.Equal(1, plan.Parameters0.Y);
        Assert.Equal(1, plan.Parameters0.Z);
        Assert.Equal(0.18f, plan.Parameters0.W);
        Assert.Equal(0, plan.Parameters1.X);
        Assert.Equal(0, plan.Parameters1.Y);
        Assert.Equal(0.088f, plan.Parameters1.Z);
        Assert.Equal(0.435f, plan.Parameters1.W);
        Assert.True(PrismAdjustmentPlanner.IsNoOp(plan));
    }

    [Fact]
    public void VibranceCatalogDefinesSkinAwarePerceptualContract()
    {
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry(
                (int)PrismFilterId.Vibrance);

        Assert.Equal(
            [
                "Amount",
                "AvoidSaturatingSkinTones",
                "GrayColorTransform",
                "Saturation"
            ],
            entry.Properties.Select(
                property => property.Name));

        PrismAdjustmentPlan plan =
            CreatePlan(PrismFilterId.Vibrance);
        Assert.Equal(1, plan.Parameters0.Z);
        Assert.Equal(
            new Vector4(
                0.2126f,
                0.7152f,
                0.0722f,
                0),
            plan.Parameters1);
    }

    [Fact]
    public void VibranceBoostsMutedColorsAndProtectsSkinTones()
    {
        PrismPremultipliedColor muted =
            PrismPremultipliedColor.FromStraight(
                0.18,
                0.12,
                0.08,
                0.4);
        PrismAdjustmentPlan protectedPlan =
            CreateVibrancePlan(
                amount: 0.75f,
                avoidsSaturatingSkinTones: true);
        PrismAdjustmentPlan unprotectedPlan =
            CreateVibrancePlan(
                amount: 0.75f,
                avoidsSaturatingSkinTones: false);

        PrismPremultipliedColor protectedResult =
            PrismAdjustmentMath.Apply(
                protectedPlan,
                muted,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor unprotectedResult =
            PrismAdjustmentMath.Apply(
                unprotectedPlan,
                muted,
                PrismColorProfile.LinearSrgb);

        Assert.Equal(0.4, protectedResult.Alpha, 6);
        Assert.Equal(0.4, unprotectedResult.Alpha, 6);
        Assert.True(
            StraightChroma(protectedResult) >
                StraightChroma(muted));
        Assert.True(
            StraightChroma(protectedResult) <
                StraightChroma(unprotectedResult));

        PrismPremultipliedColor saturated =
            PrismPremultipliedColor.FromStraight(
                0.18,
                0.02,
                0.005,
                0.4);
        PrismPremultipliedColor saturatedResult =
            PrismAdjustmentMath.Apply(
                unprotectedPlan,
                saturated,
                PrismColorProfile.LinearSrgb);
        double mutedGain =
            StraightChroma(unprotectedResult) /
            StraightChroma(muted);
        double saturatedGain =
            StraightChroma(saturatedResult) /
            StraightChroma(saturated);
        Assert.True(mutedGain > saturatedGain);
    }

    [Fact]
    public void NegativeVibranceUsesGlobalDesaturationAndGrayTransform()
    {
        PrismPremultipliedColor source =
            PrismPremultipliedColor.FromStraight(
                0.6,
                0.25,
                0.1,
                1);
        PrismAdjustmentPlan protectedPlan =
            CreateVibrancePlan(
                amount: -0.5f,
                avoidsSaturatingSkinTones: true);
        PrismAdjustmentPlan unprotectedPlan =
            CreateVibrancePlan(
                amount: -0.5f,
                avoidsSaturatingSkinTones: false);

        PrismPremultipliedColor protectedResult =
            PrismAdjustmentMath.Apply(
                protectedPlan,
                source,
                PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor unprotectedResult =
            PrismAdjustmentMath.Apply(
                unprotectedPlan,
                source,
                PrismColorProfile.LinearSrgb);

        AssertColor(
            protectedResult,
            unprotectedResult.Red,
            unprotectedResult.Green,
            unprotectedResult.Blue,
            unprotectedResult.Alpha);
        Assert.True(
            StraightChroma(protectedResult) <
                StraightChroma(source));

        PrismPremultipliedColor customGray =
            PrismAdjustmentMath.Apply(
                CreateVibrancePlan(
                    amount: -0.5f,
                    avoidsSaturatingSkinTones: true,
                    grayTransform:
                        new Vector4(1, 0, 0, 0)),
                source,
                PrismColorProfile.LinearSrgb);
        Assert.NotEqual(
            protectedResult,
            customGray);
    }

    [Fact]
    public void HueSaturationUsesGamutAwareOkhslCoordinates()
    {
        Vector3 sourceStraight = new(0.58f, 0.12f, 0.03f);
        Vector3 sourceHsl = PrismOkhsl.FromLinearSrgb(
            sourceStraight);
        Vector3 restored = PrismOkhsl.ToLinearSrgb(sourceHsl);
        Assert.Equal(sourceStraight.X, restored.X, precision: 4);
        Assert.Equal(sourceStraight.Y, restored.Y, precision: 4);
        Assert.Equal(sourceStraight.Z, restored.Z, precision: 4);

        PrismAdjustmentPlan saturated = CreatePlan(
            PrismFilterId.HueSaturation,
            (state, entry) => SetNumber(
                state,
                entry,
                "Saturation",
                1));
        PrismPremultipliedColor source =
            PrismPremultipliedColor.FromStraight(
                sourceStraight.X,
                sourceStraight.Y,
                sourceStraight.Z,
                0.4);
        PrismPremultipliedColor result =
            PrismAdjustmentMath.Apply(
                saturated,
                source,
                PrismColorProfile.LinearSrgb);
        Assert.Equal(source.Alpha, result.Alpha, precision: 8);
        Assert.InRange(result.Red, 0, source.Alpha);
        Assert.InRange(result.Green, 0, source.Alpha);
        Assert.InRange(result.Blue, 0, source.Alpha);

        PrismAdjustmentPlan greenOnly = CreatePlan(
            PrismFilterId.HueSaturation,
            (state, entry) =>
            {
                SetSymbol(state, entry, "Channel", "Greens");
                SetNumber(state, entry, "Hue", 90);
                SetNumber(state, entry, "Saturation", 1);
                SetNumber(state, entry, "Lightness", 1);
            });
        PrismPremultipliedColor unaffected =
            PrismAdjustmentMath.Apply(
                greenOnly,
                source,
                PrismColorProfile.LinearSrgb);
        AssertColor(
            unaffected,
            source.Red,
            source.Green,
            source.Blue,
            source.Alpha,
            tolerance: 0.00001);

        PrismAdjustmentPlan lighter = CreatePlan(
            PrismFilterId.HueSaturation,
            (state, entry) => SetNumber(
                state,
                entry,
                "Lightness",
                0.2f));
        PrismPremultipliedColor lighterResult =
            PrismAdjustmentMath.Apply(
                lighter,
                source,
                PrismColorProfile.LinearSrgb);
        Vector3 lighterHsl = PrismOkhsl.FromLinearSrgb(
            new Vector3(
                (float)(lighterResult.Red / lighterResult.Alpha),
                (float)(lighterResult.Green / lighterResult.Alpha),
                (float)(lighterResult.Blue / lighterResult.Alpha)));
        Assert.InRange(
            lighterHsl.Z - sourceHsl.Z,
            0.19f,
            0.21f);

        PrismAdjustmentPlan colorize = CreatePlan(
            PrismFilterId.HueSaturation,
            (state, entry) =>
            {
                SetNumber(state, entry, "Hue", 120);
                SetNumber(state, entry, "Saturation", 1);
                SetBoolean(state, entry, "Colorize", true);
            });
        PrismPremultipliedColor colorized =
            PrismAdjustmentMath.Apply(
                colorize,
                source,
                PrismColorProfile.LinearSrgb);
        Vector3 colorizedHsl = PrismOkhsl.FromLinearSrgb(
            new Vector3(
                (float)(colorized.Red / colorized.Alpha),
                (float)(colorized.Green / colorized.Alpha),
                (float)(colorized.Blue / colorized.Alpha)));
        Assert.Equal(source.Alpha, colorized.Alpha, precision: 8);
        Assert.InRange(colorizedHsl.X, 0.32f, 0.35f);
        Assert.InRange(colorizedHsl.Y, 0.99f, 1.0f);
    }

    [Fact]
    public void ColorBalanceUsesSmoothTonalRangesAndPreservesOkhslLightness()
    {
        PrismAdjustmentPlan tonal = CreatePlan(
            PrismFilterId.ColorBalance,
            (state, entry) =>
            {
                SetVector(
                    state,
                    entry,
                    "Shadows",
                    new Vector4(0.2f, 0, 0, 0));
                SetVector(
                    state,
                    entry,
                    "Midtones",
                    new Vector4(0, 0.2f, 0, 0));
                SetVector(
                    state,
                    entry,
                    "Highlights",
                    new Vector4(0, 0, 0.2f, 0));
                SetBoolean(
                    state,
                    entry,
                    "PreserveLuminosity",
                    false);
            });

        PrismPremultipliedColor shadows = ApplyStraight(
            tonal,
            new Vector3(0.1f));
        PrismPremultipliedColor midtones = ApplyStraight(
            tonal,
            new Vector3(0.44f));
        PrismPremultipliedColor highlights = ApplyStraight(
            tonal,
            new Vector3(0.85f));

        Assert.True(shadows.Red > shadows.Green);
        Assert.True(midtones.Green > midtones.Red);
        Assert.True(midtones.Green > midtones.Blue);
        Assert.True(highlights.Blue > highlights.Red);

        PrismPremultipliedColor beforeShadowBoundary =
            ApplyStraight(tonal, new Vector3(0.332f));
        PrismPremultipliedColor afterShadowBoundary =
            ApplyStraight(tonal, new Vector3(0.334f));
        Assert.InRange(
            Math.Abs(afterShadowBoundary.Red -
                beforeShadowBoundary.Red),
            0,
            0.01);

        PrismAdjustmentPlan preservesLightness = CreatePlan(
            PrismFilterId.ColorBalance,
            (state, entry) =>
            {
                SetVector(
                    state,
                    entry,
                    "Shadows",
                    new Vector4(-0.4f, 0.4f, 0.4f, 0));
                SetBoolean(
                    state,
                    entry,
                    "PreserveLuminosity",
                    true);
            });
        Vector3 sourceStraight = new(0.22f, 0.03f, 0.01f);
        PrismPremultipliedColor preserved = ApplyStraight(
            preservesLightness,
            sourceStraight,
            alpha: 0.4f);
        Vector3 sourceHsl = PrismOkhsl.FromLinearSrgb(sourceStraight);
        Vector3 preservedHsl = PrismOkhsl.FromLinearSrgb(
            new Vector3(
                (float)(preserved.Red / preserved.Alpha),
                (float)(preserved.Green / preserved.Alpha),
                (float)(preserved.Blue / preserved.Alpha)));

        Assert.Equal((double)0.4f, preserved.Alpha, precision: 8);
        Assert.InRange(
            Math.Abs(preservedHsl.Z - sourceHsl.Z),
            0,
            0.001f);
        Assert.InRange(preserved.Red, 0, preserved.Alpha);
        Assert.InRange(preserved.Green, 0, preserved.Alpha);
        Assert.InRange(preserved.Blue, 0, preserved.Alpha);
    }

    [Fact]
    public void BlackWhiteMatchesMonoMixerWeightsNormalizationAndAlpha()
    {
        PrismAdjustmentPlan defaults =
            CreatePlan(PrismFilterId.BlackWhite);
        PrismPremultipliedColor result = ApplyStraight(
            defaults,
            new Vector3(0.2f, 0.4f, 0.8f),
            alpha: 0.5f);
        const float defaultGray = 0.333f * (0.2f + 0.4f + 0.8f);
        AssertColor(
            result,
            defaultGray * 0.5f,
            defaultGray * 0.5f,
            defaultGray * 0.5f,
            0.5f);

        PrismAdjustmentPlan normalized = CreatePlan(
            PrismFilterId.BlackWhite,
            (state, entry) =>
            {
                SetNumber(state, entry, "Red", 2);
                SetNumber(state, entry, "Green", -1);
                SetNumber(state, entry, "Blue", 1);
                SetBoolean(
                    state,
                    entry,
                    "PreserveLuminosity",
                    true);
            });
        AssertStraight(
            ApplyStraight(
                normalized,
                new Vector3(0.2f, 0.4f, 0.8f)),
            0.4,
            0.4,
            0.4);

        PrismAdjustmentPlan zeroSum = normalized with
        {
            Parameters0 = new Vector4(1, -1, 0, 1)
        };
        AssertStraight(
            ApplyStraight(
                zeroSum,
                new Vector3(0.8f, 0.2f, 0.4f)),
            0.6,
            0.6,
            0.6);
    }

    [Fact]
    public void PhotoFilterLinearlyMixesTintByDensityAndPreservesAlpha()
    {
        PrismAdjustmentPlan plan =
            CreatePlan(
                PrismFilterId.PhotoFilter,
                (state, entry) =>
                {
                    SetColor(
                        state,
                        entry,
                        "Color",
                        new Color(204, 51, 26));
                    SetNumber(
                        state,
                        entry,
                        "Density",
                        0.25f);
                });
        Vector3 sourceStraight = new(0.2f, 0.4f, 0.6f);
        const float alpha = 0.5f;
        Vector3 tint = new(
            plan.Parameters0.X,
            plan.Parameters0.Y,
            plan.Parameters0.Z);
        Vector3 mixed = Vector3.Lerp(
            sourceStraight,
            tint,
            plan.Parameters1.X);

        PrismPremultipliedColor result = ApplyStraight(
            plan,
            sourceStraight,
            alpha);
        AssertColor(
            result,
            mixed.X * alpha,
            mixed.Y * alpha,
            mixed.Z * alpha,
            alpha);
        Assert.False(PrismAdjustmentPlanner.IsNoOp(plan));

        AssertColor(
            ApplyStraight(
                plan with
                {
                    Parameters1 = new Vector4(0, 0, 0, 0)
                },
                sourceStraight,
                alpha),
            sourceStraight.X * alpha,
            sourceStraight.Y * alpha,
            sourceStraight.Z * alpha,
            alpha);
        AssertColor(
            ApplyStraight(
                plan with
                {
                    Parameters1 = new Vector4(1, 0, 0, 0)
                },
                sourceStraight,
                alpha),
            tint.X * alpha,
            tint.Y * alpha,
            tint.Z * alpha,
            alpha);

        Assert.Equal(
            default,
            PrismAdjustmentMath.Apply(
                plan,
                default,
                PrismColorProfile.LinearSrgb));
    }

    [Fact]
    public void ExposureStylesMatchAnalyticReferenceVectors()
    {
        PrismAdjustmentPlan linear =
            CreateExposurePlan(
                style: "Linear",
                direction: "Forward",
                exposure: 0,
                contrast: 2,
                gamma: 1,
                pivot: 0.18f);
        AssertStraight(
            PrismAdjustmentMath.Apply(
                linear,
                PrismPremultipliedColor.FromStraight(
                    0.09,
                    0.18,
                    0.36,
                    1),
                PrismColorProfile.LinearSrgb),
            0.045,
            0.18,
            0.72);

        PrismAdjustmentPlan video =
            CreateExposurePlan(
                style: "Video",
                direction: "Forward",
                exposure: 0,
                contrast: 2,
                gamma: 1,
                pivot: 0.18f);
        AssertStraight(
            PrismAdjustmentMath.Apply(
                video,
                PrismPremultipliedColor.FromStraight(
                    0.0979456366,
                    0.1958912733,
                    0.3917825465,
                    1),
                PrismColorProfile.LinearSrgb),
            0.0244864092,
            0.0979456366,
            0.3917825465);

        PrismAdjustmentPlan logarithmic =
            CreateExposurePlan(
                style: "Logarithmic",
                direction: "Forward",
                exposure: 1,
                contrast: 2,
                gamma: 1,
                pivot: 0.18f);
        AssertStraight(
            PrismAdjustmentMath.Apply(
                logarithmic,
                PrismPremultipliedColor.FromStraight(
                    0.3,
                    0.435,
                    0.6,
                    1),
                PrismColorProfile.LinearSrgb),
            0.341,
            0.611,
            0.941);
    }

    [Fact]
    public void ExposureInverseReversesEveryStyle()
    {
        foreach (string style in
            new[] { "Linear", "Video", "Logarithmic" })
        {
            PrismAdjustmentPlan forward =
                CreateExposurePlan(
                    style,
                    direction: "Forward",
                    exposure: 0.25f,
                    contrast: 1.1f,
                    gamma: 0.9f,
                    pivot: 0.2f,
                    logExposureStep: 0.075f,
                    logMidGray: 0.41f);
            PrismAdjustmentPlan inverse =
                CreateExposurePlan(
                    style,
                    direction: "Inverse",
                    exposure: 0.25f,
                    contrast: 1.1f,
                    gamma: 0.9f,
                    pivot: 0.2f,
                    logExposureStep: 0.075f,
                    logMidGray: 0.41f);
            PrismPremultipliedColor source =
                PrismPremultipliedColor.FromStraight(
                    0.25,
                    0.4,
                    0.55,
                    1);
            PrismPremultipliedColor transformed =
                PrismAdjustmentMath.Apply(
                    forward,
                    source,
                    PrismColorProfile.LinearSrgb);
            PrismPremultipliedColor restored =
                PrismAdjustmentMath.Apply(
                    inverse,
                    transformed,
                    PrismColorProfile.LinearSrgb);

            AssertColor(
                restored,
                source.Red,
                source.Green,
                source.Blue,
                source.Alpha,
                tolerance: 0.00001);
        }
    }

    [Fact]
    public void InvertComplementsStraightLinearRgbBeforeAmountAndRepremultiplication()
    {
        PrismAdjustmentPlan invert =
            CreatePlan(PrismFilterId.Invert);
        PrismPremultipliedColor source =
            PrismPremultipliedColor.FromStraight(
                0.5,
                0.5,
                0.5,
                0.25);

        PrismPremultipliedColor full =
            PrismAdjustmentMath.Apply(
                invert,
                source,
                PrismColorProfile.Srgb);
        PrismPremultipliedColor partial =
            PrismAdjustmentMath.Apply(
                invert,
                source,
                PrismColorProfile.Srgb,
                opacity: 0.25f);

        AssertColor(
            full,
            red: 0.224817,
            green: 0.224817,
            blue: 0.224817,
            alpha: 0.25,
            tolerance: 0.00001);
        AssertColor(
            partial,
            red: 0.157967,
            green: 0.157967,
            blue: 0.157967,
            alpha: 0.25,
            tolerance: 0.00001);
    }

    [Fact]
    public void AnalyticVectorsPreserveAlphaAndHandleTransparentPixels()
    {
        PrismAdjustmentPlan invert =
            CreatePlan(PrismFilterId.Invert);
        PrismPremultipliedColor source =
            PrismPremultipliedColor.FromStraight(
                0.2,
                0.4,
                0.8,
                0.5);
        PrismPremultipliedColor result =
            PrismAdjustmentMath.Apply(
                invert,
                source,
                PrismColorProfile.LinearSrgb);

        AssertColor(
            result,
            red: 0.4,
            green: 0.3,
            blue: 0.1,
            alpha: 0.5);
        Assert.Equal(
            default,
            PrismAdjustmentMath.Apply(
                invert,
                default,
                PrismColorProfile.LinearSrgb));

        foreach (PrismCatalogEntryDescriptor entry in
            AdjustmentEntries())
        {
            PrismAdjustmentPlan plan =
                CreatePlan(
                    (PrismFilterId)entry.StableId);
            PrismPremultipliedColor adjusted =
                PrismAdjustmentMath.Apply(
                    plan,
                    source,
                    PrismColorProfile.LinearSrgb,
                    lookup: color => color);
            Assert.Equal(
                source.Alpha,
                adjusted.Alpha,
                precision: 8);
            AssertFiniteAssociated(adjusted);
        }
    }

    [Fact]
    public void ThresholdPosterizeAndLevelsHaveAnalyticBoundariesAndChannels()
    {
        PrismAdjustmentPlan threshold =
            CreatePlan(
                PrismFilterId.Threshold,
                (state, entry) => SetNumber(
                    state,
                    entry,
                    "Level",
                    0.5f));
        AssertStraight(
            PrismAdjustmentMath.Apply(
                threshold,
                PrismPremultipliedColor.FromStraight(
                    0.49,
                    0.49,
                    0.49,
                    1),
                PrismColorProfile.LinearSrgb),
            0,
            0,
            0);
        AssertStraight(
            PrismAdjustmentMath.Apply(
                threshold,
                PrismPremultipliedColor.FromStraight(
                    0.5,
                    0.5,
                    0.5,
                    1),
                PrismColorProfile.LinearSrgb),
            0,
            0,
            0);

        PrismAdjustmentPlan posterize =
            CreatePlan(
                PrismFilterId.Posterize,
                (state, entry) => SetNumber(
                    state,
                    entry,
                    "Levels",
                    2));
        AssertStraight(
            PrismAdjustmentMath.Apply(
                posterize,
                PrismPremultipliedColor.FromStraight(
                    0.49,
                    0.5,
                    0.51,
                    1),
                PrismColorProfile.LinearSrgb),
            0,
            1,
            1);

        PrismAdjustmentPlan levels =
            CreatePlan(
                PrismFilterId.Levels,
                (state, entry) =>
                {
                    SetSymbol(
                        state,
                        entry,
                        "Channel",
                        "Red");
                    SetNumber(
                        state,
                        entry,
                        "InputBlack",
                        0.25f);
                });
        AssertStraight(
            PrismAdjustmentMath.Apply(
                levels,
                PrismPremultipliedColor.FromStraight(
                    0.25,
                    0.4,
                    0.8,
                    1),
                PrismColorProfile.LinearSrgb),
            0,
            0.4,
            0.8);
    }

    [Fact]
    public void ThresholdOtsuSeparatesBimodalHistogramAndAppliesTheGlobalBoundary()
    {
        PrismPremultipliedColor[] pixels =
        [
            PrismPremultipliedColor.FromStraight(0.2, 0.2, 0.2, 1),
            PrismPremultipliedColor.FromStraight(0.2, 0.2, 0.2, 0.5),
            PrismPremultipliedColor.FromStraight(0.8, 0.8, 0.8, 1),
            PrismPremultipliedColor.FromStraight(0.8, 0.8, 0.8, 0.25)
        ];

        float level = PrismThresholdAnalysis.Calculate(
            pixels,
            PrismColorProfile.LinearSrgb);
        Assert.InRange(level, 0.19f, 0.21f);

        PrismAdjustmentPlan threshold =
            CreatePlan(
                PrismFilterId.Threshold,
                (state, entry) => SetNumber(
                    state,
                    entry,
                    "Level",
                    level));
        AssertStraight(
            PrismAdjustmentMath.Apply(
                threshold,
                pixels[0],
                PrismColorProfile.LinearSrgb),
            0,
            0,
            0);
        AssertStraight(
            PrismAdjustmentMath.Apply(
                threshold,
                pixels[2],
                PrismColorProfile.LinearSrgb),
            1,
            1,
            1);
    }

    [Fact]
    public void ThresholdOtsuReturnsUniformIntensityAndUsesFallbackForTransparentHistograms()
    {
        PrismPremultipliedColor[] uniform =
        [
            PrismPremultipliedColor.FromStraight(0.3, 0.3, 0.3, 1),
            PrismPremultipliedColor.FromStraight(0.3, 0.3, 0.3, 0.2)
        ];
        Assert.InRange(
            PrismThresholdAnalysis.Calculate(
                uniform,
                PrismColorProfile.LinearSrgb,
                fallback: 0.42f),
            0.297f,
            0.301f);
        Assert.Equal(
            0.6f,
            PrismThresholdAnalysis.Calculate(
                [default, default],
                PrismColorProfile.DisplayP3,
                fallback: 0.6f));
    }

    [Fact]
    public void ThresholdOtsuIsProfileAndAlphaInvariantAndMatchesHistogramAnalysis()
    {
        PrismPremultipliedColor[] linear =
        [
            PrismPremultipliedColor.FromStraight(0.1, 0.1, 0.1, 0.2),
            PrismPremultipliedColor.FromStraight(0.1, 0.1, 0.1, 1),
            PrismPremultipliedColor.FromStraight(0.9, 0.9, 0.9, 0.4),
            PrismPremultipliedColor.FromStraight(0.9, 0.9, 0.9, 1),
            default
        ];
        PrismPremultipliedColor[] srgb = linear
            .Select(pixel => PrismAdjustmentMath.ConvertProfile(
                pixel,
                PrismColorProfile.LinearSrgb,
                PrismColorProfile.Srgb))
            .ToArray();

        float linearLevel = PrismThresholdAnalysis.Calculate(
            linear,
            PrismColorProfile.LinearSrgb);
        float srgbLevel = PrismThresholdAnalysis.Calculate(
            srgb,
            PrismColorProfile.Srgb);
        Assert.Equal(linearLevel, srgbLevel, precision: 6);

        int[] histogram = new int[PrismThresholdAnalysis.BinCount];
        histogram[(int)MathF.Round(0.1f * 255)] = 2;
        histogram[(int)MathF.Round(0.9f * 255)] = 2;
        Assert.Equal(
            linearLevel,
            PrismThresholdAnalysis.Calculate(histogram, 4),
            precision: 6);
    }

    [Fact]
    public void PosterizeUniformlyQuantizesLinearRgbEndpointsAndPreservesAlphaAndAmount()
    {
        PrismAdjustmentPlan posterize =
            CreatePlan(
                PrismFilterId.Posterize,
                (state, entry) => SetNumber(
                    state,
                    entry,
                    "Levels",
                    5));
        PrismPremultipliedColor source =
            PrismPremultipliedColor.FromStraight(
                0,
                0.375,
                1,
                0.4);

        AssertColor(
            PrismAdjustmentMath.Apply(
                posterize,
                source,
                PrismColorProfile.LinearSrgb),
            red: 0,
            green: 0.2,
            blue: 0.4,
            alpha: 0.4);
        AssertColor(
            PrismAdjustmentMath.Apply(
                posterize,
                source,
                PrismColorProfile.LinearSrgb,
                opacity: 0.5f),
            red: 0,
            green: 0.175,
            blue: 0.4,
            alpha: 0.4);
        Assert.Equal(
            default,
            PrismAdjustmentMath.Apply(
                posterize,
                default,
                PrismColorProfile.LinearSrgb));
    }

    [Fact]
    public void LevelsAutoUsesSynchronizedClippedHistogramBoundaries()
    {
        Vector3[] samples = new Vector3[1002];
        samples[0] = Vector3.Zero;
        samples[^1] = Vector3.One;
        for (int index = 1; index < samples.Length - 1; index++)
        {
            float value = 0.2f +
                ((index - 1) / 999f * 0.6f);
            samples[index] = new Vector3(value);
        }

        PrismLevelsRange range =
            PrismLevelsAnalysis.Calculate(
                samples,
                channel: 0,
                clippedFraction: 0.001f);

        Assert.InRange(range.InputBlack, 0.19f, 0.21f);
        Assert.InRange(range.InputWhite, 0.79f, 0.81f);

        PrismAdjustmentPlan automatic =
            CreatePlan(
                PrismFilterId.Levels,
                (state, entry) => SetBoolean(
                    state,
                    entry,
                    "Auto",
                    true));
        Assert.Equal(1, automatic.Parameters1.Z);
        Assert.False(PrismAdjustmentPlanner.IsNoOp(automatic));
    }

    [Fact]
    public void CurvesCompilesShapePreservingRgbLutAndComposesChannelFirst()
    {
        PrismCurvesResource curves = new(
            composite:
            [
                new PrismCurvePoint(0, 1),
                new PrismCurvePoint(1, 0)
            ],
            red:
            [
                new PrismCurvePoint(0, 0),
                new PrismCurvePoint(0.5f, 0.25f),
                new PrismCurvePoint(1, 1)
            ]);
        PrismCurveLut lut = PrismCurveLut.Create(curves);

        Assert.Equal(
            PrismCurveLut.SampleCount,
            lut.Values.Length);
        Vector3 mapped = lut.Sample(new Vector3(0.5f));
        Assert.InRange(mapped.X, 0.748f, 0.752f);
        Assert.InRange(mapped.Y, 0.498f, 0.502f);
        Assert.InRange(mapped.Z, 0.498f, 0.502f);

        PrismCurvesResource alternating = new(
            red:
            [
                new PrismCurvePoint(0, 0),
                new PrismCurvePoint(0.25f, 0.8f),
                new PrismCurvePoint(0.5f, 0.2f),
                new PrismCurvePoint(0.75f, 0.7f),
                new PrismCurvePoint(1, 1)
            ]);
        PrismCurveLut alternatingLut =
            PrismCurveLut.Create(alternating);
        float[] outputs = [0, 0.8f, 0.2f, 0.7f, 1];
        for (int index = 0;
            index < PrismCurveLut.SampleCount;
            index++)
        {
            float input =
                index / (PrismCurveLut.SampleCount - 1f);
            int segment = Math.Min((int)(input * 4), 3);
            float minimum = Math.Min(
                outputs[segment],
                outputs[segment + 1]);
            float maximum = Math.Max(
                outputs[segment],
                outputs[segment + 1]);
            Assert.InRange(
                alternatingLut.Values[index].X,
                minimum - 0.00001f,
                maximum + 0.00001f);
        }
    }

    [Fact]
    public void CurvesResourceValidatesPointsAndParticipatesInDependencies()
    {
        Assert.Throws<ArgumentException>(
            () => new PrismCurvesResource(
                red:
                [
                    new PrismCurvePoint(0, 0),
                    new PrismCurvePoint(0, 1),
                    new PrismCurvePoint(1, 1)
                ]));
        Assert.Throws<ArgumentException>(
            () => new PrismCurvesResource(
                blue:
                [
                    new PrismCurvePoint(0.1f, 0),
                    new PrismCurvePoint(1, 1)
                ]));

        PrismResourceId id = new("curves-test");
        PrismCurvesResource resource = new();
        PrismDrawResources resources = PrismDrawResources.Create(
            [],
            [
                new PrismDrawCurvesResource(
                    id,
                    resource,
                    Version: 7,
                    Identity: 11)
            ]);

        Assert.True(resources.HasStableVersions);
        Assert.True(resources.TryGetCurves(
            id,
            out PrismCurvesResource resolved,
            out long identity,
            out long version));
        Assert.Same(resource, resolved);
        Assert.Equal(11, identity);
        Assert.Equal(7, version);
        Assert.True(resources.TryGetDependency(
            id,
            out identity,
            out version));
        Assert.Equal(11, identity);
        Assert.Equal(7, version);
    }

    [Fact]
    public void CurvesPlannerRequiresTypedResourceAndCpuPathPreservesAlpha()
    {
        PrismAdjustmentPlan plan =
            CreatePlan(
                PrismFilterId.Curves,
                (state, entry) =>
                    GeneratedMarkup.SetPrismFilterResource(
                        state,
                        entry.StableId,
                        Property(entry, "Curves").TypeSlot,
                        new PrismResourceId("curve-lut")));
        PrismCurveLut lut = PrismCurveLut.Create(
            new PrismCurvesResource(
                red:
                [
                    new PrismCurvePoint(0, 0),
                    new PrismCurvePoint(1, 0.5f)
                ]));

        Assert.True(plan.ResourceRequired);
        Assert.Equal(
            new PrismResourceId("curve-lut"),
            plan.Resource);
        PrismPremultipliedColor result =
            PrismAdjustmentMath.Apply(
                plan,
                PrismPremultipliedColor.FromStraight(
                    0.8,
                    0.4,
                    0.2,
                    0.5),
                PrismColorProfile.LinearSrgb,
                lookup: lut.Sample);
        AssertColor(
            result,
            red: 0.2,
            green: 0.2,
            blue: 0.1,
            alpha: 0.5,
            tolerance: 0.001);
    }

    [Fact]
    public void ChannelMixerUsesUnpremultipliedRgbMatrixConstantsAndAlpha()
    {
        PrismAdjustmentPlan matrix =
            CreatePlan(
                PrismFilterId.ChannelMixer,
                (state, entry) =>
                {
                    SetVector(
                        state,
                        entry,
                        "Red",
                        new Vector4(0.5f, 0.25f, 0.1f, 0));
                    SetVector(
                        state,
                        entry,
                        "Green",
                        new Vector4(0.2f, 0.6f, 0.1f, 0));
                    SetVector(
                        state,
                        entry,
                        "Blue",
                        new Vector4(0.1f, 0.3f, 0.5f, 0));
                    SetVector(
                        state,
                        entry,
                        "Constant",
                        new Vector4(0.05f, 0.1f, 0, 0));
                });
        PrismPremultipliedColor source =
            PrismPremultipliedColor.FromStraight(
                0.2,
                0.4,
                0.6,
                0.5);

        AssertColor(
            PrismAdjustmentMath.Apply(
                matrix,
                source,
                PrismColorProfile.LinearSrgb),
            red: 0.155,
            green: 0.22,
            blue: 0.22,
            alpha: 0.5);
    }

    [Fact]
    public void ChannelMixerAndLookupUseSharedMatrixAndLutPrimitives()
    {
        PrismAdjustmentPlan mixer =
            CreatePlan(
                PrismFilterId.ChannelMixer,
                (state, entry) =>
                {
                    SetVector(
                        state,
                        entry,
                        "Red",
                        new Vector4(0, 1, 0, 0));
                    SetVector(
                        state,
                        entry,
                        "Green",
                        new Vector4(0, 0, 1, 0));
                    SetVector(
                        state,
                        entry,
                        "Blue",
                        new Vector4(1, 0, 0, 0));
                });
        AssertStraight(
            PrismAdjustmentMath.Apply(
                mixer,
                PrismPremultipliedColor.FromStraight(
                    0.2,
                    0.4,
                    0.8,
                    1),
                PrismColorProfile.LinearSrgb),
            0.4,
            0.8,
            0.2);

        PrismAdjustmentPlan lookup =
            CreatePlan(
                PrismFilterId.ColorLookup,
                (state, entry) =>
                    GeneratedMarkup.SetPrismFilterResource(
                        state,
                        entry.StableId,
                        Property(entry, "Lookup").TypeSlot,
                        new PrismResourceId("analytic-lut")));
        AssertStraight(
            PrismAdjustmentMath.Apply(
                lookup,
                PrismPremultipliedColor.FromStraight(
                    0.2,
                    0.4,
                    0.8,
                    1),
                PrismColorProfile.LinearSrgb,
                lookup: color => Vector3.One - color),
            0.8,
            0.6,
            0.2);
    }

    [Fact]
    public void ColorLookupMatchesCanonicalHaldTrilinearMappingProfilesAndAlpha()
    {
        const int level = 2;
        const int cubeSize = level * level;
        const int haldSide = cubeSize * level;
        Vector3[] values = new Vector3[haldSide * haldSide];
        values[PrismHaldLut.GetHaldIndex(
            cubeSize,
            red: 1,
            green: 0,
            blue: 0)] = Vector3.One;
        PrismHaldLut lut = new(level, values);
        Vector3 coordinate = new(
            0.75f / (cubeSize - 1),
            0.5f / (cubeSize - 1),
            0.25f / (cubeSize - 1));

        Assert.Equal(1, PrismHaldLut.GetHaldIndex(
            cubeSize,
            red: 1,
            green: 0,
            blue: 0));
        Assert.Equal(63, PrismHaldLut.GetHaldIndex(
            cubeSize,
            red: 3,
            green: 3,
            blue: 3));
        Assert.Equal(
            new Vector3(0.28125f),
            lut.Sample(
                coordinate,
                PrismHaldInterpolation.Trilinear));

        PrismAdjustmentPlan plan = CreatePlan(
            PrismFilterId.ColorLookup,
            (state, entry) =>
            {
                GeneratedMarkup.SetPrismFilterResource(
                    state,
                    entry.StableId,
                    Property(entry, "Lookup").TypeSlot,
                    new PrismResourceId("hald-lut"));
            });
        PrismPremultipliedColor source =
            PrismPremultipliedColor.FromStraight(
                coordinate.X,
                coordinate.Y,
                coordinate.Z,
                0.4);
        AssertColor(
            PrismAdjustmentMath.Apply(
                plan,
                source,
                PrismColorProfile.LinearSrgb,
                haldLookup: lut),
            red: 0.1125,
            green: 0.1125,
            blue: 0.1125,
            alpha: 0.4,
            tolerance: 0.00001);
        Assert.Equal(
            default,
            PrismAdjustmentMath.Apply(
                plan,
                default,
                PrismColorProfile.LinearSrgb,
                haldLookup: lut));

        Vector3[] identityValues = new Vector3[values.Length];
        for (int blue = 0; blue < cubeSize; blue++)
        {
            for (int green = 0; green < cubeSize; green++)
            {
                for (int red = 0; red < cubeSize; red++)
                {
                    identityValues[PrismHaldLut.GetHaldIndex(
                        cubeSize,
                        red,
                        green,
                        blue)] = new Vector3(
                        red / (cubeSize - 1f),
                        green / (cubeSize - 1f),
                        blue / (cubeSize - 1f));
                }
            }
        }
        PrismHaldLut identity = new(level, identityValues);
        foreach (PrismColorProfile profile in
            Enum.GetValues<PrismColorProfile>())
        {
            PrismPremultipliedColor working =
                PrismColorPipeline.ConvertInputToWorking(
                    PrismPremultipliedColor.FromStraight(
                        0.31,
                        0.57,
                        0.83,
                        0.4),
                    profile);
            AssertColor(
                PrismAdjustmentMath.Apply(
                    plan with
                    {
                        Parameters0 = new Vector4(1, 0, 0, 0)
                    },
                    working,
                    profile,
                    haldLookup: identity),
                working.Red,
                working.Green,
                working.Blue,
                working.Alpha,
                tolerance: 0.00001);
        }
    }

    [Fact]
    public void NeutralAdjustmentIsStableInEverySelectableColorProfile()
    {
        PrismAdjustmentPlan neutral =
            CreatePlan(
                PrismFilterId.BrightnessContrast);
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
            PrismPremultipliedColor result =
                PrismAdjustmentMath.Apply(
                    neutral,
                    working,
                    profile);
            AssertColor(
                result,
                working.Red,
                working.Green,
                working.Blue,
                working.Alpha,
                tolerance: 0.00001);
        }
    }

    [Fact]
    public void SelectiveColorUsesFfmpegRangesMethodsProfilesAndStraightAlpha()
    {
        string[] ranges =
        [
            "Reds", "Yellows", "Greens", "Cyans", "Blues",
            "Magentas", "Whites", "Neutrals", "Blacks"
        ];
        Vector3[] references =
        [
            new(1, 0, 0), new(1, 1, 0), new(0, 1, 0),
            new(0, 1, 1), new(0, 0, 1), new(1, 0, 1),
            Vector3.One, new(0.5f), Vector3.Zero
        ];

        for (int index = 0; index < ranges.Length; index++)
        {
            string range = ranges[index];
            PrismAdjustmentPlan absolute = CreatePlan(
                PrismFilterId.SelectiveColor,
                (state, entry) =>
                {
                    SetVector(
                        state,
                        entry,
                        range,
                        new Vector4(0, 0, 0, 0.25f));
                    SetSymbol(state, entry, "Method", "Absolute");
                });
            PrismPremultipliedColor result = ApplyStraight(
                absolute,
                references[index],
                alpha: 0.4f);
            Vector3 expected = Vector3.Max(
                references[index] - new Vector3(0.25f),
                Vector3.Zero);
            AssertColor(
                result,
                expected.X * 0.4,
                expected.Y * 0.4,
                expected.Z * 0.4,
                0.4,
                tolerance: 0.00001);
        }

        PrismAdjustmentPlan relative = CreatePlan(
            PrismFilterId.SelectiveColor,
            (state, entry) => SetVector(
                state,
                entry,
                "Reds",
                new Vector4(0.5f, 0, 0, 0)));
        Assert.Equal(0, relative.Parameters9.X);
        AssertStraight(
            ApplyStraight(relative, new Vector3(0.8f, 0.2f, 0.2f)),
            0.74,
            0.2,
            0.2);

        PrismAdjustmentPlan combined = CreatePlan(
            PrismFilterId.SelectiveColor,
            (state, entry) =>
            {
                SetVector(
                    state,
                    entry,
                    "Reds",
                    new Vector4(0.5f, 0, 0, 0.25f));
                SetSymbol(state, entry, "Method", "Absolute");
            });
        Assert.Equal(1, combined.Parameters9.X);
        Assert.Equal(
            new Vector4(0.5f, 0, 0, 0.25f),
            combined.Parameters0);
        PrismPremultipliedColor combinedResult = ApplyStraight(
            combined,
            new Vector3(0.8f, 0.2f, 0.2f));
        Assert.Equal(0.32, combinedResult.Red, precision: 5);
        Assert.Equal(0.08, combinedResult.Green, precision: 5);
        Assert.Equal(0.08, combinedResult.Blue, precision: 5);
        Assert.Equal(1, combinedResult.Alpha);

        PrismPremultipliedColor linear =
            PrismPremultipliedColor.FromStraight(0.8, 0.2, 0.2, 0.4);
        PrismPremultipliedColor srgb = PrismAdjustmentMath.ConvertProfile(
            linear,
            PrismColorProfile.LinearSrgb,
            PrismColorProfile.Srgb);
        PrismPremultipliedColor profiled = PrismAdjustmentMath.Apply(
            relative,
            srgb,
            PrismColorProfile.Srgb);
        AssertColor(
            PrismAdjustmentMath.ConvertProfile(
                profiled,
                PrismColorProfile.Srgb,
                PrismColorProfile.LinearSrgb),
            0.296,
            0.08,
            0.08,
            0.4,
            tolerance: 0.00001);
    }

    [Fact]
    public void OptimizerKeepsExactSourceBoundsForEveryAdjustment()
    {
        foreach (PrismCatalogEntryDescriptor entry in
            AdjustmentEntries())
        {
            PrismFilterId filter =
                (PrismFilterId)entry.StableId;
            PrismLayerDefinition layer = new(
                new PrismNodeId(1),
                filter.ToString(),
                filters:
                [
                    new PrismFilterDefinition(filter)
                ]);
            DrawRect bounds =
                new(10, 20, 40, 30);
            PrismDrawScope scope = PrismTestData.Scope(
                PrismTestData.Composition(
                    $"Bounds {filter}",
                    layer),
                bounds: bounds);
            Assert.Single(
                scope.Instance.GetLayerState(layer.Id).Filters)
                .Opacity = 0.5f;
            PrismGraph graph = BuildGraph(scope);
            PrismGraphExecutionPlan plan =
                new PrismGraphOptimizer().Optimize(graph);
            PrismGraphNode node = Assert.Single(
                plan.OptimizedGraph.Nodes.Where(candidate =>
                    candidate.Kind ==
                        PrismGraphNodeKind.Filter));
            PrismGraphNodePlan nodePlan =
                plan.GetNodePlan(node.Id);

            Assert.Equal(bounds, nodePlan.Bounds);
            Assert.Equal(
                PrismGraphBoundsStatus.Exact,
                nodePlan.BoundsStatus);
        }
    }

    [Fact]
    public void GradientMapUsesVersionedInterpolatedLinearLutReverseDitherProfilesAndAlpha()
    {
        PrismGradientMapResource resource = new(
        [
            new PrismGradientMapPoint(0, new Vector3(0, 0, 1)),
            new PrismGradientMapPoint(0.5f, new Vector3(1, 0, 0)),
            new PrismGradientMapPoint(1, new Vector3(1, 1, 0))
        ]);
        PrismGradientMapLut lut = PrismGradientMapLut.Create(resource);
        Assert.Equal(new Vector3(1, 0.5f, 0), lut.Sample(0.75f));

        PrismAdjustmentPlan plan = CreatePlan(
            PrismFilterId.GradientMap,
            (state, entry) =>
            {
                GeneratedMarkup.SetPrismFilterResource(
                    state,
                    entry.StableId,
                    Property(entry, "Gradient").TypeSlot,
                    new PrismResourceId("gradient-lut"));
                SetBoolean(state, entry, "Reverse", true);
            });
        Assert.True(plan.ResourceRequired);
        Assert.Equal(new PrismResourceId("gradient-lut"), plan.Resource);

        PrismPremultipliedColor source =
            PrismPremultipliedColor.FromStraight(0.25, 0.25, 0.25, 0.4);
        PrismPremultipliedColor result = PrismAdjustmentMath.Apply(
            plan,
            source,
            PrismColorProfile.LinearSrgb,
            gradientLookup: lut);
        AssertColor(result, 0.4, 0.2, 0, 0.4, tolerance: 0.00001);

        PrismAdjustmentPlan dithered = plan with
        {
            Parameters0 = new Vector4(0, 0, 1, 0)
        };
        PrismPremultipliedColor low = PrismAdjustmentMath.Apply(
            dithered,
            source,
            PrismColorProfile.LinearSrgb,
            pixelPosition: Vector2.Zero,
            gradientLookup: lut);
        PrismPremultipliedColor high = PrismAdjustmentMath.Apply(
            dithered,
            source,
            PrismColorProfile.LinearSrgb,
            pixelPosition: new Vector2(3, 3),
            gradientLookup: lut);
        Assert.NotEqual(low, high);

        PrismPremultipliedColor srgbSource =
            PrismAdjustmentMath.ConvertProfile(
                source,
                PrismColorProfile.LinearSrgb,
                PrismColorProfile.Srgb);
        PrismPremultipliedColor srgbResult = PrismAdjustmentMath.Apply(
            plan,
            srgbSource,
            PrismColorProfile.Srgb,
            gradientLookup: lut);
        AssertColor(
            PrismAdjustmentMath.ConvertProfile(
                srgbResult,
                PrismColorProfile.Srgb,
                PrismColorProfile.LinearSrgb),
            result.Red,
            result.Green,
            result.Blue,
            result.Alpha,
            tolerance: 0.00001);
    }

    [Fact]
    public void GradientMapResourceVersionsParticipateInDependencies()
    {
        PrismResourceId id = new("gradient-version");
        PrismGradientMapResource resource = new(
        [
            new PrismGradientMapPoint(0, Vector3.Zero),
            new PrismGradientMapPoint(1, Vector3.One)
        ]);
        PrismDrawResources resources = PrismDrawResources.Create(
            [],
            [],
            [new PrismDrawGradientMapResource(id, resource, 7, 11)]);
        Assert.True(resources.HasStableVersions);
        Assert.True(resources.TryGetGradientMap(
            id,
            out PrismGradientMapResource resolved,
            out long identity,
            out long version));
        Assert.Same(resource, resolved);
        Assert.Equal(11, identity);
        Assert.Equal(7, version);
    }

    private static PrismAdjustmentPlan CreatePlan(
        PrismFilterId filter,
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
                layer));
        PrismFilterState state = Assert.Single(
            scope.Instance.GetLayerState(layer.Id).Filters);
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)filter);
        configure?.Invoke(state, entry);

        PrismGraph graph = BuildGraph(scope);
        PrismGraphNode node = Assert.Single(
            graph.Nodes.Where(candidate =>
                candidate.Kind ==
                    PrismGraphNodeKind.Filter));
        return PrismAdjustmentPlanner.Create(
            node,
            Assert.Single(graph.Scopes));
    }

    private static PrismAdjustmentPlan CreateExposurePlan(
        string style,
        string direction,
        float exposure,
        float contrast,
        float gamma,
        float pivot,
        float logExposureStep = 0.088f,
        float logMidGray = 0.435f) =>
        CreatePlan(
            PrismFilterId.Exposure,
            (state, entry) =>
            {
                SetSymbol(state, entry, "Style", style);
                SetSymbol(
                    state,
                    entry,
                    "Direction",
                    direction);
                SetNumber(
                    state,
                    entry,
                    "Exposure",
                    exposure);
                SetNumber(
                    state,
                    entry,
                    "Contrast",
                    contrast);
                SetNumber(
                    state,
                    entry,
                    "Gamma",
                    gamma);
                SetNumber(
                    state,
                    entry,
                    "Pivot",
                    pivot);
                SetNumber(
                    state,
                    entry,
                    "LogExposureStep",
                    logExposureStep);
                SetNumber(
                    state,
                    entry,
                    "LogMidGray",
                    logMidGray);
            });

    private static PrismAdjustmentPlan CreateVibrancePlan(
        float amount,
        bool avoidsSaturatingSkinTones,
        float saturation = 0,
        Vector4? grayTransform = null) =>
        CreatePlan(
            PrismFilterId.Vibrance,
            (state, entry) =>
            {
                SetNumber(
                    state,
                    entry,
                    "Amount",
                    amount);
                SetNumber(
                    state,
                    entry,
                    "Saturation",
                    saturation);
                SetBoolean(
                    state,
                    entry,
                    "AvoidSaturatingSkinTones",
                    avoidsSaturatingSkinTones);
                if (grayTransform is Vector4 transform)
                {
                    SetVector(
                        state,
                        entry,
                        "GrayColorTransform",
                        transform);
                }
            });

    private static (
        PrismFilterState State,
        PrismCatalogEntryDescriptor Entry) CreateState(
        PrismFilterId filter)
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
                $"State {filter}",
                layer));
        return (
            Assert.Single(
                scope.Instance.GetLayerState(layer.Id).Filters),
            PrismCatalogRuntime.GetEntry((int)filter));
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
        AdjustmentEntries() =>
        PrismCatalogGenerated.Entries
            .Where(entry =>
                entry.Kind == "filter" &&
                entry.Category ==
                    "color-and-adjustment" &&
                entry.Execution is
                    PrismCatalogExecutionDescriptor execution &&
                execution.Primitive ==
                    "matrix-curve-lut")
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

    private static void AssertStraight(
        PrismPremultipliedColor color,
        double red,
        double green,
        double blue,
        double tolerance = 0.00001) =>
        AssertColor(
            color,
            red,
            green,
            blue,
            1,
            tolerance);

    private static PrismPremultipliedColor ApplyStraight(
        PrismAdjustmentPlan plan,
        Vector3 color,
        float alpha = 1) =>
        PrismAdjustmentMath.Apply(
            plan,
            PrismPremultipliedColor.FromStraight(
                color.X,
                color.Y,
                color.Z,
                alpha),
            PrismColorProfile.LinearSrgb);

    private static void AssertColor(
        PrismPremultipliedColor color,
        double red,
        double green,
        double blue,
        double alpha,
        double tolerance = 0.00001)
    {
        Assert.InRange(
            Math.Abs(color.Red - red),
            0,
            tolerance);
        Assert.InRange(
            Math.Abs(color.Green - green),
            0,
            tolerance);
        Assert.InRange(
            Math.Abs(color.Blue - blue),
            0,
            tolerance);
        Assert.InRange(
            Math.Abs(color.Alpha - alpha),
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

    private static double StraightChroma(
        PrismPremultipliedColor color)
    {
        double maximum = Math.Max(
            color.Red,
            Math.Max(color.Green, color.Blue));
        double minimum = Math.Min(
            color.Red,
            Math.Min(color.Green, color.Blue));
        return (maximum - minimum) / color.Alpha;
    }
}
