using System.Numerics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.SdlGpu;
using Cerneala.UI.Markup;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using SkiaSharp;
using CernealaColor = Cerneala.Drawing.Color;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismStyleConformanceMigrationTests
{
    private const int Width = 96, Height = 64;
    private static readonly Color ClearColor = new(9, 13, 21);
    private static readonly DrawRect ScopeBounds = new(0, 0, Width, Height);

    [SdlNativeFact]
    public void OuterGlowFallsOffContinuouslyInsteadOfDrawingDetachedRectangles()
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "Outer glow continuity",
            styles: [new PrismStyleDefinition(PrismStyleId.OuterGlow)]);
        DrawRect elementBounds = new(24, 18, 48, 28);
        PrismCompositionDefinition composition = new(
            "Outer glow continuity",
            [layer],
            workingColorProfile: PrismColorProfile.LinearSrgb);
        PrismDrawScope scope = PrismTestData.Scope(
            composition,
            ownerToken: 1_370,
            bounds: elementBounds);
        PrismStyleState glow = Assert.Single(
            scope.Instance.GetLayerState(layer.Id).Styles);
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)PrismStyleId.OuterGlow);
        SetNumber("Size", 12);
        SetNumber("Range", 1);
        SetNumber("Opacity", 1);
        PrismScene scene = new(
            "outer-glow-continuity",
            Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(
                    elementBounds,
                    new CernealaColor(7, 12, 22, 208)),
                DrawCommand.EndPrism()));
        using SdlDrawingFixture fixture = new(Width, Height);

        RenderedScene rendered = RenderPixels(fixture.Session, scene);
        using SKBitmap bitmap = Decode(rendered.Pixels, scene.Name);
        int[] intensities = Enumerable
            .Range(1, 12)
            .Select(distance =>
            {
                SKColor pixel = bitmap.GetPixel(48, 18 - distance);
                return Math.Abs(pixel.Red - ClearColor.R) +
                    Math.Abs(pixel.Green - ClearColor.G) +
                    Math.Abs(pixel.Blue - ClearColor.B);
            })
            .ToArray();
        int[] interiorIntensities = Enumerable
            .Range(1, 12)
            .Select(distance =>
            {
                SKColor pixel = bitmap.GetPixel(48, 18 + distance);
                return Math.Abs(pixel.Red - ClearColor.R) +
                    Math.Abs(pixel.Green - ClearColor.G) +
                    Math.Abs(pixel.Blue - ClearColor.B);
            })
            .ToArray();

        Assert.All(intensities, intensity => Assert.True(intensity > 0));
        Assert.True(
            intensities.Max() >= 30,
            $"Expected a visible one-pixel outer glow, but found [{string.Join(", ", intensities)}].");
        Assert.True(
            intensities.Distinct().Count() >= 6,
            $"Expected a continuous glow falloff, but found bands [{string.Join(", ", intensities)}].");
        Assert.Single(interiorIntensities.Distinct());

        void SetNumber(string name, float value)
        {
            PrismCatalogPropertyDescriptor property =
                entry.Properties.Single(candidate => candidate.Name == name);
            GeneratedMarkup.SetPrismStyleNumber(
                glow,
                entry.StableId,
                property.TypeSlot,
                value);
        }
    }


    [SdlNativeFact]
    public void OverlayOuterGlowUsesBackdropAcrossExpandedEffectBounds()
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "Overlay outer glow",
            styles: [new PrismStyleDefinition(PrismStyleId.OuterGlow)],
            blendMode: PrismBlendMode.Overlay);
        DrawRect elementBounds = new(30, 24, 30, 30);
        PrismCompositionDefinition composition = new(
            "Overlay outer glow",
            [layer],
            workingColorProfile: PrismColorProfile.LinearSrgb);
        PrismDrawScope scope = PrismTestData.Scope(
            composition,
            ownerToken: 1_374,
            bounds: elementBounds);
        PrismStyleState glow = Assert.Single(
            scope.Instance.GetLayerState(layer.Id).Styles);
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)PrismStyleId.OuterGlow);
        SetNumber("Size", 10);
        SetNumber("Spread", 0);
        SetNumber("Range", 1);
        SetNumber("Opacity", 1);
        SetColor("Color", new CernealaColor(176, 139, 124));
        PrismScene scene = new(
            "overlay-outer-glow-expanded-backdrop",
            Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(
                    new DrawRect(30, 24, 10, 10),
                    new CernealaColor(255, 32, 64)),
                DrawCommand.FillRectangle(
                    new DrawRect(50, 44, 10, 10),
                    new CernealaColor(255, 32, 64)),
                DrawCommand.EndPrism()));
        using SdlDrawingFixture fixture = new(Width, Height);

        RenderedScene rendered = RenderPixels(fixture.Session, scene);
        using SKBitmap bitmap = Decode(rendered.Pixels, scene.Name);
        SKColor outsideControl = bitmap.GetPixel(27, 29);
        SKColor insideControl = bitmap.GetPixel(42, 29);

        Assert.True(DifferenceFromClear(outsideControl) > 0);
        Assert.True(DifferenceFromClear(insideControl) > 0);
        Assert.InRange(
            Difference(outsideControl, insideControl),
            0,
            12);

        int DifferenceFromClear(SKColor pixel) =>
            Math.Abs(pixel.Red - ClearColor.R) +
            Math.Abs(pixel.Green - ClearColor.G) +
            Math.Abs(pixel.Blue - ClearColor.B);

        int Difference(SKColor left, SKColor right) =>
            Math.Abs(left.Red - right.Red) +
            Math.Abs(left.Green - right.Green) +
            Math.Abs(left.Blue - right.Blue) +
            Math.Abs(left.Alpha - right.Alpha);

        void SetNumber(string name, float value)
        {
            PrismCatalogPropertyDescriptor property =
                entry.Properties.Single(candidate => candidate.Name == name);
            GeneratedMarkup.SetPrismStyleNumber(
                glow,
                entry.StableId,
                property.TypeSlot,
                value);
        }

        void SetColor(string name, CernealaColor value)
        {
            PrismCatalogPropertyDescriptor property =
                entry.Properties.Single(candidate => candidate.Name == name);
            GeneratedMarkup.SetPrismStyleColor(
                glow,
                entry.StableId,
                property.TypeSlot,
                value);
        }
    }


    [SdlNativeFact]
    public void OuterGlowGaussianFalloffDoesNotFavorCardinalDirections()
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "Outer glow isotropy",
            styles: [new PrismStyleDefinition(PrismStyleId.OuterGlow)]);
        DrawRect elementBounds = new(48, 32, 1, 1);
        PrismCompositionDefinition composition = new(
            "Outer glow isotropy",
            [layer],
            workingColorProfile: PrismColorProfile.LinearSrgb);
        PrismDrawScope scope = PrismTestData.Scope(
            composition,
            ownerToken: 1_371,
            bounds: elementBounds);
        PrismStyleState glow = Assert.Single(
            scope.Instance.GetLayerState(layer.Id).Styles);
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)PrismStyleId.OuterGlow);
        SetNumber("Size", 12);
        SetNumber("Spread", 0);
        SetNumber("Range", 1);
        SetNumber("Opacity", 1);
        PrismScene scene = new(
            "outer-glow-isotropy",
            Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(
                    elementBounds,
                    new CernealaColor(255, 255, 255, 255)),
                DrawCommand.EndPrism()));
        using SdlDrawingFixture fixture = new(Width, Height);

        RenderedScene rendered = RenderPixels(fixture.Session, scene);
        using SKBitmap bitmap = Decode(rendered.Pixels, scene.Name);
        int cardinal = Intensity(bitmap.GetPixel(43, 32));
        int diagonal = Intensity(bitmap.GetPixel(45, 28));

        Assert.True(cardinal > 0 && diagonal > 0);
        Assert.InRange(
            Math.Abs(cardinal - diagonal),
            0,
            12);

        int Intensity(SKColor pixel) =>
            Math.Abs(pixel.Red - ClearColor.R) +
            Math.Abs(pixel.Green - ClearColor.G) +
            Math.Abs(pixel.Blue - ClearColor.B);

        void SetNumber(string name, float value)
        {
            PrismCatalogPropertyDescriptor property =
                entry.Properties.Single(candidate => candidate.Name == name);
            GeneratedMarkup.SetPrismStyleNumber(
                glow,
                entry.StableId,
                property.TypeSlot,
                value);
        }
    }


    [SdlNativeFact]
    public void OuterGlowUsesJfaDistanceInsideTransparentHolesAndBeyondThirtyTwoPixels()
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "Outer glow JFA coverage",
            styles: [new PrismStyleDefinition(PrismStyleId.OuterGlow)]);
        PrismCompositionDefinition composition = new(
            "Outer glow JFA coverage",
            [layer],
            workingColorProfile: PrismColorProfile.LinearSrgb);
        PrismDrawScope scope = PrismTestData.Scope(
            composition,
            ownerToken: 1_372,
            bounds: ScopeBounds);
        PrismStyleState glow = Assert.Single(
            scope.Instance.GetLayerState(layer.Id).Styles);
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)PrismStyleId.OuterGlow);
        SetNumber("Size", 48);
        SetNumber("Spread", 0);
        SetNumber("Range", 1);
        SetNumber("Opacity", 1);
        PrismScene scene = new(
            "outer-glow-jfa-coverage",
            Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(
                    new DrawRect(42, 26, 12, 2),
                    new CernealaColor(255, 255, 255)),
                DrawCommand.FillRectangle(
                    new DrawRect(42, 36, 12, 2),
                    new CernealaColor(255, 255, 255)),
                DrawCommand.FillRectangle(
                    new DrawRect(42, 28, 2, 8),
                    new CernealaColor(255, 255, 255)),
                DrawCommand.FillRectangle(
                    new DrawRect(52, 28, 2, 8),
                    new CernealaColor(255, 255, 255)),
                DrawCommand.EndPrism()));
        using SdlDrawingFixture fixture = new(Width, Height);

        RenderedScene first = RenderPixels(fixture.Session, scene);
        RenderedScene second = RenderPixels(fixture.Session, scene);
        Assert.Equal(first.Pixels, second.Pixels);
        using SKBitmap bitmap = Decode(first.Pixels, scene.Name);

        Assert.True(
            Intensity(bitmap.GetPixel(48, 32)) > 20,
            "Expected the transparent hole to receive glow from its alpha boundary.");
        Assert.True(
            Intensity(bitmap.GetPixel(5, 32)) > 0,
            "Expected a size-48 glow beyond the old 32-pixel Gaussian limit.");

        int Intensity(SKColor pixel) =>
            Math.Abs(pixel.Red - ClearColor.R) +
            Math.Abs(pixel.Green - ClearColor.G) +
            Math.Abs(pixel.Blue - ClearColor.B);

        void SetNumber(string name, float value)
        {
            PrismCatalogPropertyDescriptor property =
                entry.Properties.Single(candidate => candidate.Name == name);
            GeneratedMarkup.SetPrismStyleNumber(
                glow,
                entry.StableId,
                property.TypeSlot,
                value);
        }
    }


    [SdlNativeTheory]
    [InlineData("InnerBevel", false)]
    [InlineData("OuterBevel", true)]
    [InlineData("Emboss", false)]
    [InlineData("PillowEmboss", false)]
    [InlineData("StrokeEmboss", false)]
    public void BevelEmbossProfilesRenderReliefAndAreDeterministic(
        string style,
        bool expectsOutsideRelief)
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            $"{style} JFA coverage",
            styles: [new PrismStyleDefinition(PrismStyleId.BevelEmboss)]);
        PrismCompositionDefinition composition = new(
            $"{style} JFA coverage",
            [layer],
            workingColorProfile: PrismColorProfile.LinearSrgb);
        PrismDrawScope scope = PrismTestData.Scope(
            composition,
            ownerToken: 1_373,
            bounds: ScopeBounds);
        PrismStyleState bevel = Assert.Single(
            scope.Instance.GetLayerState(layer.Id).Styles);
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)PrismStyleId.BevelEmboss);
        SetSymbol("Style", style);
        SetNumber("Size", 8);
        SetNumber("Soften", 0);
        SetNumber("Depth", 10);
        SetNumber("Angle", 0);
        SetNumber("Altitude", 30);
        SetNumber("HighlightOpacity", 1);
        SetNumber("ShadowOpacity", 1);
        SetBoolean("UseGlobalLight", false);
        PrismScene scene = new(
            $"bevel-emboss-{style.ToLowerInvariant()}-jfa-coverage",
            Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(
                    new DrawRect(40, 24, 16, 16),
                    new CernealaColor(96, 128, 192)),
                DrawCommand.EndPrism()));
        using SdlDrawingFixture fixture = new(Width, Height);

        RenderedScene first = RenderPixels(fixture.Session, scene);
        RenderedScene second = RenderPixels(fixture.Session, scene);
        Assert.Equal(first.Pixels, second.Pixels);
        using SKBitmap bitmap = Decode(first.Pixels, scene.Name);

        int outsideRelief = Enumerable.Range(32, 32).Sum(x =>
            Enumerable.Range(16, 32)
                .Where(y => x < 40 || x >= 56 || y < 24 || y >= 40)
                .Sum(y => DifferenceFromClear(bitmap.GetPixel(x, y))));
        SKColor center = bitmap.GetPixel(48, 32);
        int insideRelief = Enumerable.Range(40, 16).Sum(x =>
            Enumerable.Range(24, 16)
                .Where(y => x < 44 || x >= 52 || y < 28 || y >= 36)
                .Sum(y => Difference(bitmap.GetPixel(x, y), center)));
        Assert.True(
            outsideRelief + insideRelief > 120,
            $"Expected {style} to produce relief; outside energy was {outsideRelief}, inside energy was {insideRelief}.");
        if (expectsOutsideRelief)
        {
            Assert.True(
                outsideRelief > 120,
                $"Expected {style} outside the source alpha; energy was {outsideRelief}.");
        }

        int DifferenceFromClear(SKColor pixel) =>
            Math.Abs(pixel.Red - ClearColor.R) +
            Math.Abs(pixel.Green - ClearColor.G) +
            Math.Abs(pixel.Blue - ClearColor.B) +
            Math.Abs(pixel.Alpha - ClearColor.A);

        int Difference(SKColor left, SKColor right) =>
            Math.Abs(left.Red - right.Red) +
            Math.Abs(left.Green - right.Green) +
            Math.Abs(left.Blue - right.Blue) +
            Math.Abs(left.Alpha - right.Alpha);

        void SetNumber(string name, float value)
        {
            PrismCatalogPropertyDescriptor property =
                entry.Properties.Single(candidate => candidate.Name == name);
            GeneratedMarkup.SetPrismStyleNumber(
                bevel,
                entry.StableId,
                property.TypeSlot,
                value);
        }

        void SetSymbol(string name, string value)
        {
            PrismCatalogPropertyDescriptor property =
                entry.Properties.Single(candidate => candidate.Name == name);
            GeneratedMarkup.SetPrismStyleInteger(
                bevel,
                entry.StableId,
                property.TypeSlot,
                PrismCatalogRuntime.ResolveSymbol(name, value));
        }

        void SetBoolean(string name, bool value)
        {
            PrismCatalogPropertyDescriptor property =
                entry.Properties.Single(candidate => candidate.Name == name);
            GeneratedMarkup.SetPrismStyleBoolean(
                bevel,
                entry.StableId,
                property.TypeSlot,
                value);
        }
    }


    [SdlNativeFact]
    public void DropShadowGaussianFalloffDoesNotFavorCardinalDirections()
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "Drop shadow isotropy",
            styles: [new PrismStyleDefinition(PrismStyleId.DropShadow)]);
        DrawRect elementBounds = new(48, 32, 1, 1);
        PrismCompositionDefinition composition = new(
            "Drop shadow isotropy",
            [layer],
            workingColorProfile: PrismColorProfile.LinearSrgb);
        PrismDrawScope scope = PrismTestData.Scope(
            composition,
            ownerToken: 1_372,
            bounds: elementBounds);
        PrismStyleState shadow = Assert.Single(
            scope.Instance.GetLayerState(layer.Id).Styles);
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)PrismStyleId.DropShadow);
        SetNumber("Distance", 0);
        SetNumber("Size", 12);
        SetNumber("Spread", 0);
        SetNumber("Opacity", 1);
        SetInteger("BlendMode", (int)PrismBlendMode.Normal);
        SetColor("Color", new CernealaColor(74, 218, 188));
        PrismScene scene = new(
            "drop-shadow-isotropy",
            Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(
                    elementBounds,
                    new CernealaColor(255, 255, 255, 255)),
                DrawCommand.EndPrism()));
        using SdlDrawingFixture fixture = new(Width, Height);

        RenderedScene rendered = RenderPixels(fixture.Session, scene);
        using SKBitmap bitmap = Decode(rendered.Pixels, scene.Name);
        int cardinal = Intensity(bitmap.GetPixel(43, 32));
        int diagonal = Intensity(bitmap.GetPixel(45, 28));

        Assert.True(cardinal > 0 && diagonal > 0);
        Assert.InRange(
            Math.Abs(cardinal - diagonal),
            0,
            12);

        int Intensity(SKColor pixel) =>
            Math.Abs(pixel.Red - ClearColor.R) +
            Math.Abs(pixel.Green - ClearColor.G) +
            Math.Abs(pixel.Blue - ClearColor.B);

        void SetNumber(string name, float value)
        {
            PrismCatalogPropertyDescriptor property =
                entry.Properties.Single(candidate => candidate.Name == name);
            GeneratedMarkup.SetPrismStyleNumber(
                shadow,
                entry.StableId,
                property.TypeSlot,
                value);
        }

        void SetInteger(string name, int value)
        {
            PrismCatalogPropertyDescriptor property =
                entry.Properties.Single(candidate => candidate.Name == name);
            GeneratedMarkup.SetPrismStyleInteger(
                shadow,
                entry.StableId,
                property.TypeSlot,
                value);
        }

        void SetColor(string name, CernealaColor value)
        {
            PrismCatalogPropertyDescriptor property =
                entry.Properties.Single(candidate => candidate.Name == name);
            GeneratedMarkup.SetPrismStyleColor(
                shadow,
                entry.StableId,
                property.TypeSlot,
                value);
        }
    }


    [SdlNativeFact]
    public void ExtrudeRendersProjectedBlockFacesOnSdlGpu()
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "Extrude projected blocks",
            filters:
            [
                new PrismFilterDefinition(PrismFilterId.Extrude)
            ]);
        PrismDrawScope scope = PrismTestData.Scope(
            new PrismCompositionDefinition(
                "Extrude projected blocks",
                [layer],
                workingColorProfile: PrismColorProfile.LinearSrgb),
            ownerToken: 13_016,
            bounds: ScopeBounds);
        PrismFilterState state = Assert.Single(
            scope.Instance.GetLayerState(layer.Id).Filters);
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)PrismFilterId.Extrude);
        SetSymbol("Type", "Blocks");
        SetNumber("Size", 16);
        SetNumber("Depth", 16);
        SetSymbol("DepthMode", "Level");
        SetBoolean("SolidFrontFaces", true);
        SetBoolean("MaskIncompleteBlocks", false);
        PrismScene scene = new(
            "extrude-projected-blocks",
            Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(
                    ScopeBounds,
                    new CernealaColor(0, 0, 255)),
                DrawCommand.FillRectangle(
                    new DrawRect(0, 0, 16, 16),
                    new CernealaColor(255, 0, 0)),
                DrawCommand.FillRectangle(
                    new DrawRect(16, 0, 16, 16),
                    new CernealaColor(0, 255, 0)),
                DrawCommand.EndPrism()));
        using SdlDrawingFixture fixture = new(Width, Height);

        RenderedScene rendered = RenderPixels(fixture.Session, scene);
        using SKBitmap bitmap = Decode(rendered.Pixels, scene.Name);
        SKColor projectedSide = bitmap.GetPixel(17, 8);
        SKColor frontCap = bitmap.GetPixel(30, 4);

        Assert.Equal(0, rendered.Counters.FallbackCount);
        Assert.True(rendered.Counters.PassCount >= 1);
        Assert.True(
            projectedSide.Red > projectedSide.Green + 80 &&
            projectedSide.Red > projectedSide.Blue + 80,
            $"Expected the left red cell to project a shaded side, got {projectedSide}.");
        Assert.True(
            frontCap.Green > frontCap.Red + 80 &&
            frontCap.Green > frontCap.Blue + 80,
            $"Expected the current green cell to keep its solid front cap, got {frontCap}.");

        void SetNumber(string name, float value)
        {
            PrismCatalogPropertyDescriptor property =
                entry.Properties.Single(candidate => candidate.Name == name);
            GeneratedMarkup.SetPrismFilterNumber(
                state,
                entry.StableId,
                property.TypeSlot,
                value);
        }

        void SetBoolean(string name, bool value)
        {
            PrismCatalogPropertyDescriptor property =
                entry.Properties.Single(candidate => candidate.Name == name);
            GeneratedMarkup.SetPrismFilterBoolean(
                state,
                entry.StableId,
                property.TypeSlot,
                value);
        }

        void SetSymbol(string name, string value)
        {
            PrismCatalogPropertyDescriptor property =
                entry.Properties.Single(candidate => candidate.Name == name);
            GeneratedMarkup.SetPrismFilterInteger(
                state,
                entry.StableId,
                property.TypeSlot,
                PrismCatalogRuntime.ResolveSymbol(name, value));
        }
    }


    [SdlNativeFact]
    public void FibersRendersLongitudinallyCoherentPatternOnSdlGpu()
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "Fibers coherence",
            filters:
            [
                new PrismFilterDefinition(PrismFilterId.Fibers)
            ]);
        PrismDrawScope scope = PrismTestData.Scope(
            new PrismCompositionDefinition(
                "Fibers coherence",
                [layer],
                workingColorProfile: PrismColorProfile.LinearSrgb),
            ownerToken: 13_015,
            bounds: ScopeBounds);
        PrismFilterState state = Assert.Single(
            scope.Instance.GetLayerState(layer.Id).Filters);
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)PrismFilterId.Fibers);
        SetNumber("Variance", 12);
        SetNumber("Strength", 1);
        SetInteger("Seed", 17);
        PrismScene scene = new(
            "fibers-coherence",
            Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(
                    ScopeBounds,
                    new CernealaColor(255, 255, 255)),
                DrawCommand.EndPrism()));
        using SdlDrawingFixture fixture = new(Width, Height);

        RenderedScene rendered = RenderPixels(fixture.Session, scene);
        using SKBitmap bitmap = Decode(rendered.Pixels, scene.Name);
        double horizontalDifference =
            AverageAdjacentDifference(bitmap, horizontal: true);
        double verticalDifference =
            AverageAdjacentDifference(bitmap, horizontal: false);

        Assert.Equal(0, rendered.Counters.FallbackCount);
        Assert.True(rendered.Counters.PassCount >= 1);
        Assert.True(horizontalDifference > 0.5);
        Assert.True(
            verticalDifference < horizontalDifference * 0.75,
            $"Expected longitudinal coherence, got horizontal {horizontalDifference:F3} and vertical {verticalDifference:F3}.");

        void SetNumber(string name, float value)
        {
            PrismCatalogPropertyDescriptor property =
                entry.Properties.Single(candidate =>
                    candidate.Name == name);
            GeneratedMarkup.SetPrismFilterNumber(
                state,
                entry.StableId,
                property.TypeSlot,
                value);
        }

        void SetInteger(string name, int value)
        {
            PrismCatalogPropertyDescriptor property =
                entry.Properties.Single(candidate =>
                    candidate.Name == name);
            GeneratedMarkup.SetPrismFilterInteger(
                state,
                entry.StableId,
                property.TypeSlot,
                value);
        }
    }


    [SdlNativeFact]
    public void MosaicPreserveEdgesRendersBilateralCellsOnSdlGpu()
    {
        using SdlDrawingFixture fixture = new();
        SKColor baseline = RenderMosaic(preserveEdges: false, ownerToken: 13_013);
        SKColor preserved = RenderMosaic(preserveEdges: true, ownerToken: 13_014);

        Assert.NotEqual(baseline, preserved);
        Assert.True(
            preserved.Red > preserved.Blue + 80,
            $"Expected the red-side representative to reject blue bleeding, got {preserved}.");

        SKColor RenderMosaic(bool preserveEdges, long ownerToken)
        {
            string name =
                preserveEdges ? "Mosaic bilateral" : "Mosaic center sample";
            PrismLayerDefinition layer = new(
                new PrismNodeId(1),
                name,
                filters:
                [
                    new PrismFilterDefinition(PrismFilterId.Mosaic)
                ]);
            PrismDrawScope scope = PrismTestData.Scope(
                new PrismCompositionDefinition(
                    name,
                    [layer],
                    workingColorProfile: PrismColorProfile.LinearSrgb),
                ownerToken,
                ScopeBounds);
            PrismFilterState state = Assert.Single(
                scope.Instance
                    .GetLayerState(layer.Id)
                    .Filters);
            PrismCatalogEntryDescriptor entry =
                PrismCatalogRuntime.GetEntry((int)PrismFilterId.Mosaic);
            GeneratedMarkup.SetPrismFilterVector(
                state,
                entry.StableId,
                entry.Properties.Single(property =>
                    property.Name == "CellSize").TypeSlot,
                new Vector4(48, 32, 0, 0));
            GeneratedMarkup.SetPrismFilterBoolean(
                state,
                entry.StableId,
                entry.Properties.Single(property =>
                    property.Name == "PreserveEdges").TypeSlot,
                preserveEdges);

            List<DrawCommand> commands =
            [
                DrawCommand.BeginPrism(scope)
            ];
            for (int cellX = 0; cellX < Width; cellX += 48)
            {
                CernealaColor[] colors =
                [
                    new(51, 0, 0),
                    new(102, 0, 0),
                    new(153, 0, 0),
                    new(204, 0, 0),
                    new(0, 0, 255),
                    new(0, 0, 255)
                ];
                for (int strip = 0; strip < colors.Length; strip++)
                {
                    commands.Add(DrawCommand.FillRectangle(
                        new DrawRect(cellX + (strip * 8), 0, 8, Height),
                        colors[strip]));
                }
            }
            commands.Add(DrawCommand.EndPrism());
            PrismScene scene = new(
                preserveEdges ? "mosaic-bilateral" : "mosaic-center-sample",
                Commands([.. commands]));

            RenderedScene rendered = RenderPixels(fixture.Session, scene);
            using SKBitmap bitmap = Decode(rendered.Pixels, scene.Name);
            SKColor representative = bitmap.GetPixel(4, 8);

            Assert.Equal(0, rendered.Counters.FallbackCount);
            Assert.True(rendered.Counters.PassCount >= 1);
            Assert.True(
                IsWithinTolerance(representative, bitmap.GetPixel(40, 8), 2),
                "Mosaic did not emit a uniform representative across the cell.");
            Assert.True(
                IsWithinTolerance(representative, bitmap.GetPixel(4, 24), 2),
                "Mosaic cell output changed vertically.");
            return representative;
        }
    }


    [SdlNativeFact]
    public void TwirlEightTapPathRendersThroughSdlGpuWithoutFallback()
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "Twirl eight tap",
            filters:
            [
                new PrismFilterDefinition(PrismFilterId.Twirl)
            ]);
        PrismDrawScope scope = PrismTestData.Scope(
            new PrismCompositionDefinition(
                "Twirl eight tap",
                [layer],
                workingColorProfile: PrismColorProfile.LinearSrgb),
            ownerToken: 13_012,
            bounds: ScopeBounds);
        PrismFilterState state = Assert.Single(
            scope.Instance
                .GetLayerState(layer.Id)
                .Filters);
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)PrismFilterId.Twirl);
        GeneratedMarkup.SetPrismFilterNumber(
            state,
            entry.StableId,
            entry.Properties.Single(property =>
                property.Name == "Angle").TypeSlot,
            1440);
        List<DrawCommand> commands =
        [
            DrawCommand.BeginPrism(scope)
        ];
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                byte channel = (x + y) % 2 == 0
                    ? byte.MinValue
                    : byte.MaxValue;
                commands.Add(DrawCommand.FillRectangle(
                    new DrawRect(x, y, 1, 1),
                    new CernealaColor(
                        channel,
                        channel,
                        channel)));
            }
        }
        commands.Add(DrawCommand.EndPrism());
        PrismScene scene = new(
            "twirl-eight-tap",
            Commands([.. commands]));
        using SdlDrawingFixture fixture = new(Width, Height);

        RenderedScene rendered =
            RenderPixels(fixture.Session, scene);
        using SKBitmap bitmap =
            Decode(rendered.Pixels, scene.Name);
        int filteredPixels = 0;
        for (int y = 8; y < Height - 8; y++)
        {
            for (int x = 8; x < Width - 8; x++)
            {
                SKColor pixel = bitmap.GetPixel(x, y);
                if (pixel.Red is > 32 and < 223)
                {
                    filteredPixels++;
                }
            }
        }

        Assert.Equal(0, rendered.Counters.FallbackCount);
        Assert.True(rendered.Counters.PassCount >= 1);
        Assert.True(
            filteredPixels > 100,
            $"Expected bounded anisotropic filtering, found {filteredPixels} intermediate pixels.");
    }


    [SdlNativeFact]
    public void ColorLookupSamplesAValidatedCanonicalHaldResourceOnGpu()
    {
        const int level = 2;
        const int cubeSize = level * level;
        const int haldSide = cubeSize * level;
        using SdlDrawingFixture fixture = new();
        byte[] pixels = new byte[haldSide * haldSide * 4];
        for (int blue = 0; blue < cubeSize; blue++)
        {
            for (int green = 0; green < cubeSize; green++)
            {
                for (int red = 0; red < cubeSize; red++)
                {
                    int index = red +
                        (cubeSize * (green + (cubeSize * blue)));
                    pixels[index * 4] = (byte)(blue * 85);
                    pixels[index * 4 + 1] = (byte)(green * 85);
                    pixels[index * 4 + 2] = (byte)(red * 85);
                    pixels[index * 4 + 3] = byte.MaxValue;
                }
            }
        }
        using SdlGpuImage image = new(haldSide, haldSide, pixels);
        PrismResourceId resourceId = new("HaldConformance");
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "Hald lookup",
            filters:
            [
                new PrismFilterDefinition(PrismFilterId.ColorLookup)
            ]);
        PrismDrawScope scope = PrismTestData.Scope(
            new PrismCompositionDefinition(
                "Hald lookup",
                [layer],
                workingColorProfile: PrismColorProfile.LinearSrgb),
            ownerToken: 13_011,
            bounds: ScopeBounds,
            resources: PrismDrawResources.Create(
            [
                new PrismDrawImageResource(
                    resourceId,
                    image,
                    Version: 1,
                    Identity: 13_011)
            ]));
        PrismFilterState state = Assert.Single(
            scope.Instance.GetLayerState(layer.Id).Filters);
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)PrismFilterId.ColorLookup);
        GeneratedMarkup.SetPrismFilterResource(
            state,
            entry.StableId,
            entry.Properties.Single(property =>
                property.Name == "Lookup").TypeSlot,
            resourceId);
        PrismScene scene = new(
            "hald-color-lookup",
            Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(
                    ScopeBounds,
                    new CernealaColor(32, 128, 224)),
                DrawCommand.EndPrism()));

        RenderedScene rendered = RenderPixels(fixture.Session, scene);
        using SKBitmap bitmap = Decode(rendered.Pixels, scene.Name);
        SKColor pixel = bitmap.GetPixel(Width / 2, Height / 2);

        Assert.Equal(0, rendered.Counters.FallbackCount);
        Assert.True(
            pixel.Red > pixel.Green && pixel.Green > pixel.Blue,
            $"Expected the Hald red/blue swap, got ({pixel.Red}, {pixel.Green}, {pixel.Blue}).");
    }

    private static DrawCommandList Commands(params DrawCommand[] commands) => PrismTestData.Commands(commands);

    private static RenderedScene RenderPixels(SdlGpuWindowGraphicsSession session, PrismScene scene)
    {
        Color[] pixels = SdlDrawingFixture.Render(session, scene.Commands, ClearColor);
        SdlGpuDrawingBackend backend = Assert.IsType<SdlGpuDrawingBackend>(session.DrawingBackend);
        Assert.Equal(0, backend.PrismDiagnostics.Count);
        return new(pixels, backend.PrismDiagnostics.Counters);
    }

    private static SKBitmap Decode(Color[] pixels, string sceneName)
    {
        Assert.Equal(Width * Height, pixels.Length);
        SKBitmap bitmap = new(Width, Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
        {
            Color pixel = pixels[y * Width + x];
            bitmap.SetPixel(x, y, new SKColor(pixel.R, pixel.G, pixel.B, pixel.A));
        }
        return bitmap;
    }

    private static bool IsWithinTolerance(SKColor a, SKColor b, int tolerance) =>
        Math.Abs(a.Red - b.Red) <= tolerance && Math.Abs(a.Green - b.Green) <= tolerance &&
        Math.Abs(a.Blue - b.Blue) <= tolerance && Math.Abs(a.Alpha - b.Alpha) <= tolerance;

    private static double AverageAdjacentDifference(SKBitmap bitmap, bool horizontal)
    {
        double total = 0;
        int count = 0;
        for (int y = 0; y < (horizontal ? bitmap.Height : bitmap.Height - 1); y++)
        for (int x = 0; x < (horizontal ? bitmap.Width - 1 : bitmap.Width); x++)
        {
            total += Math.Abs(bitmap.GetPixel(x, y).Red -
                bitmap.GetPixel(horizontal ? x + 1 : x, horizontal ? y : y + 1).Red);
            count++;
        }
        return total / count;
    }

    private sealed record PrismScene(string Name, DrawCommandList Commands);

    private sealed record RenderedScene(Color[] Pixels, PrismExecutionCounters Counters);

}
