using System.Collections.Immutable;
using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Filters;

internal static partial class PrismCatalogFilterPlanner
{
    private static ImmutableArray<PrismCatalogFilterPass> CreatePasses(
        PrismFilterId filter,
        PrismCatalogFilterPrimitive primitive,
        PrismFilterParameterReader values,
        float deviceScale,
        float pixelScale,
        DrawRect sourceBounds)
    {
        if (filter == PrismFilterId.MosaicTiles)
        {
            float mosaicRadius =
                MosaicTilesSettings(values, deviceScale).X * 0.5f;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    mosaicRadius,
                    mosaicRadius,
                    0,
                    0,
                    0,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.Patchwork)
        {
            float patchworkRadius =
                PatchworkSettings(values, deviceScale).X * 0.5f;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    patchworkRadius,
                    patchworkRadius,
                    0,
                    0,
                    0,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.StainedGlass)
        {
            Vector3 settings = StainedGlassSettings(
                values,
                deviceScale);
            double scaledWidth = Math.Max(sourceBounds.Width, 0) *
                deviceScale;
            double scaledHeight = Math.Max(sourceBounds.Height, 0) *
                deviceScale;
            double maximumDimension = Math.Max(
                scaledWidth,
                scaledHeight);
            int floodPassCount = maximumDimension <= 1
                ? 0
                : Math.Clamp(
                    (int)Math.Ceiling(Math.Log2(maximumDimension)),
                    0,
                    30);
            ImmutableArray<PrismCatalogFilterPass>.Builder passes =
                ImmutableArray.CreateBuilder<PrismCatalogFilterPass>(
                    floodPassCount + 2);
            passes.Add(new(
                PrismCatalogFilterPassKind.Direct,
                0,
                0,
                0,
                0,
                0,
                IsNoOp: false));
            float jump = floodPassCount == 0
                ? 0
                : MathF.Pow(2, floodPassCount - 1);
            for (int index = 0; index < floodPassCount; index++)
            {
                passes.Add(new(
                    PrismCatalogFilterPassKind.Iteration,
                    jump,
                    jump,
                    0,
                    0,
                    index + 1,
                    IsNoOp: false));
                jump *= 0.5f;
            }
            passes.Add(new(
                PrismCatalogFilterPassKind.Direct,
                settings.Y,
                settings.Y,
                0,
                0,
                floodPassCount + 1,
                IsNoOp: false));
            return passes.MoveToImmutable();
        }

        if (filter == PrismFilterId.Plaster)
        {
            float radius = PlasterSettings(values, deviceScale).Y;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    radius,
                    0,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    radius,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    radius,
                    0,
                    0,
                    0,
                    2,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    radius,
                    0,
                    0,
                    3,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    1,
                    1,
                    0,
                    0,
                    4,
                    IsNoOp: false)
            ];
        }

        if (filter is
            PrismFilterId.Photocopy or
            PrismFilterId.Stamp or
            PrismFilterId.TornEdges)
        {
            float radius = filter switch
            {
                PrismFilterId.Photocopy =>
                    PhotocopyXDogSettings(values, deviceScale).Z,
                PrismFilterId.Stamp =>
                    StampXDogSettings(values, deviceScale).Z,
                _ => TornEdgesXDogSettings(values, deviceScale).Z
            };
            return
            [
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    radius,
                    0,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    radius,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    1,
                    1,
                    0,
                    0,
                    2,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.NotePaper)
        {
            float radius = Math.Clamp(
                2 * MathF.Sqrt(deviceScale),
                1,
                4);
            return
            [
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    radius,
                    0,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    radius,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    1,
                    1,
                    0,
                    0,
                    2,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.Chrome)
        {
            float radius = ChromeSettings(values, deviceScale).Y;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    radius,
                    0,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    radius,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    1,
                    1,
                    0,
                    0,
                    2,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.Charcoal)
        {
            Vector4 fdog = CharcoalFdogSettings(values, deviceScale);
            Vector4 etf = CharcoalEtfSettings(values, deviceScale);
            int refinementCount = (int)etf.Y;
            ImmutableArray<PrismCatalogFilterPass>.Builder passes =
                ImmutableArray.CreateBuilder<PrismCatalogFilterPass>(
                    refinementCount + 4);
            passes.Add(new(
                PrismCatalogFilterPassKind.Direct,
                1,
                1,
                0,
                0,
                0,
                IsNoOp: false));
            for (int iteration = 1; iteration <= refinementCount; iteration++)
            {
                passes.Add(new(
                    PrismCatalogFilterPassKind.Iteration,
                    etf.X,
                    etf.X,
                    0,
                    0,
                    iteration,
                    IsNoOp: false));
            }

            passes.Add(new(
                PrismCatalogFilterPassKind.Direct,
                fdog.Z,
                fdog.Z,
                0,
                0,
                4,
                IsNoOp: false));
            passes.Add(new(
                PrismCatalogFilterPassKind.Iteration,
                fdog.W,
                fdog.W,
                0,
                0,
                5,
                IsNoOp: false));
            passes.Add(new(
                PrismCatalogFilterPassKind.Direct,
                0,
                0,
                0,
                0,
                6,
                IsNoOp: false));
            return passes.MoveToImmutable();
        }

        if (filter == PrismFilterId.ConteCrayon)
        {
            Vector4 xdog = ConteCrayonXDogSettings(deviceScale);
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    1,
                    1,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Iteration,
                    3,
                    3,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    xdog.Z,
                    xdog.Z,
                    0,
                    0,
                    4,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Iteration,
                    xdog.W,
                    xdog.W,
                    0,
                    0,
                    5,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    0,
                    0,
                    0,
                    0,
                    6,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.GraphicPen)
        {
            Vector4 xdog = GraphicPenXDogSettings(values, deviceScale);
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    1,
                    1,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Iteration,
                    3,
                    3,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    xdog.Z,
                    xdog.Z,
                    0,
                    0,
                    4,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Iteration,
                    xdog.W,
                    xdog.W,
                    0,
                    0,
                    5,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    0,
                    0,
                    0,
                    0,
                    6,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.ChalkCharcoal)
        {
            float radius = ChalkCharcoalGaussianSettings(
                values,
                deviceScale).W;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    radius,
                    0,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    radius,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    0,
                    0,
                    0,
                    0,
                    2,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.SumiE)
        {
            float strokeWidth = MathF.Max(
                values.Number("StrokeWidth"),
                0);
            float washRadius = Math.Clamp(
                (1 + (strokeWidth * 0.25f)) * deviceScale,
                1,
                6);
            Vector4 gaussian = SumiEGaussianSettings(
                values,
                deviceScale);
            float gaussianRadius = gaussian.W;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    washRadius,
                    washRadius,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    gaussianRadius,
                    0,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    gaussianRadius,
                    0,
                    0,
                    2,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.Watercolor)
        {
            float detail = Math.Clamp(
                values.Number("BrushDetail"),
                0,
                16);
            float meanShiftRadius = MathF.Max(
                (3 - (2 * detail / 16)) * deviceScale,
                1);
            float morphologyRadius = MathF.Max(
                (detail < 6 ? 2 : 1) * deviceScale,
                1);
            float edgeRadius = MathF.Max(deviceScale, 1);
            return
            [
                new(
                    PrismCatalogFilterPassKind.Iteration,
                    meanShiftRadius,
                    meanShiftRadius,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Iteration,
                    meanShiftRadius,
                    meanShiftRadius,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    morphologyRadius,
                    morphologyRadius,
                    0,
                    0,
                    2,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    morphologyRadius,
                    morphologyRadius,
                    0,
                    0,
                    3,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    morphologyRadius,
                    morphologyRadius,
                    0,
                    0,
                    4,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    morphologyRadius,
                    morphologyRadius,
                    0,
                    0,
                    5,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    edgeRadius,
                    edgeRadius,
                    0,
                    0,
                    6,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.WaterPaper)
        {
            float fiberLength = Math.Clamp(
                MathF.Abs(values.Number("FiberLength")) * deviceScale,
                1,
                96);
            float pigmentRadius = Math.Clamp(
                MathF.Sqrt(fiberLength) * 0.75f,
                1,
                6);
            return
            [
                new(
                    PrismCatalogFilterPassKind.Iteration,
                    pigmentRadius,
                    pigmentRadius,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    1,
                    1,
                    0,
                    0,
                    1,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.Wind)
        {
            int method = values.SymbolCode(
                "Method",
                ("Wind", 0),
                ("Blast", 1),
                ("Stagger", 2));
            float methodScale = PrismWindFilter.MethodLengthScale(method);
            float radius = Math.Clamp(
                MathF.Abs(values.Number("Strength")) *
                    deviceScale *
                    methodScale,
                0,
                64);
            if (radius == 0)
            {
                return
                [
                    new(
                        PrismCatalogFilterPassKind.Direct,
                        0,
                        0,
                        0,
                        0,
                        0,
                        IsNoOp: true)
                ];
            }

            return
            [
                new(
                    PrismCatalogFilterPassKind.Iteration,
                    radius,
                    radius,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    1,
                    1,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Iteration,
                    radius,
                    radius,
                    0,
                    0,
                    2,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.AngledStrokes)
        {
            float radius = Math.Clamp(
                values.Number("StrokeLength"),
                1,
                50) *
                deviceScale;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    radius,
                    radius,
                    0,
                    0,
                    0,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.SprayedStrokes)
        {
            float strokeLength = MathF.Max(
                values.Number("StrokeLength"),
                0);
            float sprayRadius = MathF.Max(
                values.Number("SprayRadius"),
                0);
            float brushRadius = MathF.Max(
                0.75f,
                (strokeLength * 0.08f) +
                    (sprayRadius * 0.2f));
            float radius =
                ((strokeLength * 0.5f) +
                    sprayRadius +
                    brushRadius) *
                deviceScale;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    radius,
                    radius,
                    0,
                    0,
                    0,
                    IsNoOp: strokeLength == 0 && sprayRadius == 0)
            ];
        }

        if (filter == PrismFilterId.RoughPastels)
        {
            float coarseRadius = Math.Clamp(
                MathF.Max(values.Number("StrokeLength"), 1),
                1,
                12) *
                deviceScale;
            float detailMix = Math.Clamp(
                values.Number("StrokeDetail"),
                0,
                16) /
                16;
            float fineRadius = MathF.Max(
                coarseRadius *
                    (0.25f + (0.25f * detailMix)),
                1);
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    coarseRadius,
                    coarseRadius,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    fineRadius,
                    fineRadius,
                    0,
                    0,
                    1,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.SmudgeStick)
        {
            float radius = Math.Clamp(
                MathF.Max(values.Number("StrokeLength"), 0),
                0,
                12) *
                deviceScale;
            float intensity = Math.Clamp(
                values.Number("Intensity"),
                0,
                10);
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    radius,
                    radius,
                    0,
                    0,
                    0,
                    IsNoOp: radius == 0 || intensity == 0)
            ];
        }

        if (filter == PrismFilterId.Sponge)
        {
            float radius = Math.Clamp(
                MathF.Max(values.Number("BrushSize"), 0),
                0,
                12) *
                deviceScale;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    radius,
                    radius,
                    0,
                    0,
                    0,
                    IsNoOp: radius == 0)
            ];
        }

        if (filter == PrismFilterId.Underpainting)
        {
            float radius = Math.Clamp(
                MathF.Max(values.Number("BrushSize"), 0),
                0,
                12) *
                deviceScale;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    radius,
                    radius,
                    0,
                    0,
                    0,
                    IsNoOp: false)
            ];
        }

        if (filter is
            PrismFilterId.BasRelief or
            PrismFilterId.PosterEdges)
        {
            float requestedRadius = filter == PrismFilterId.BasRelief
                ? values.Number("Smoothness")
                : values.Number("EdgeThickness");
            float radius = MathF.Round(Math.Clamp(
                MathF.Max(requestedRadius, 0) *
                    deviceScale,
                1,
                8));
            float finalRadius = filter == PrismFilterId.BasRelief
                ? 1
                : radius;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    radius,
                    0,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    radius,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    0,
                    0,
                    0,
                    0,
                    2,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    radius,
                    0,
                    0,
                    0,
                    3,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    radius,
                    0,
                    0,
                    4,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    finalRadius,
                    finalRadius,
                    0,
                    0,
                    5,
                    IsNoOp: false)
            ];
        }

        if (filter is
            PrismFilterId.AccentedEdges or
            PrismFilterId.DarkStrokes or
            PrismFilterId.InkOutlines)
        {
            Vector4 gaussian = filter switch
            {
                PrismFilterId.AccentedEdges =>
                    AccentedEdgesGaussianSettings(values, deviceScale),
                PrismFilterId.InkOutlines =>
                    InkOutlinesGaussianSettings(values, deviceScale),
                _ => XDogGaussianSettings(deviceScale)
            };
            float radius = gaussian.W;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    radius,
                    0,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    radius,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Direct,
                    0,
                    0,
                    0,
                    0,
                    2,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.GlowingEdges)
        {
            Vector4 settings = GlowingEdgesSettings(values, deviceScale);
            float edgeRadius = settings.X;
            float gaussianRadius = settings.Z;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    edgeRadius,
                    edgeRadius,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    gaussianRadius,
                    0,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    gaussianRadius,
                    0,
                    0,
                    2,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.Cutout)
        {
            float simplicity = Math.Clamp(
                values.Number("EdgeSimplicity"),
                0,
                10);
            int iterations = Math.Clamp(
                (int)MathF.Ceiling(simplicity * 0.5f),
                1,
                4);
            float radius = Math.Clamp(
                1 + (simplicity * 0.75f),
                1,
                8) *
                deviceScale;
            ImmutableArray<PrismCatalogFilterPass>.Builder passes =
                ImmutableArray.CreateBuilder<PrismCatalogFilterPass>(
                    iterations + 1);
            for (int index = 0; index < iterations; index++)
            {
                passes.Add(
                    new(
                        PrismCatalogFilterPassKind.Iteration,
                        radius,
                        radius,
                        0,
                        0,
                        index,
                        IsNoOp: false));
            }
            passes.Add(
                new(
                    PrismCatalogFilterPassKind.Direct,
                    0,
                    0,
                    0,
                    0,
                    iterations,
                    IsNoOp: false));
            return passes.MoveToImmutable();
        }

        if (filter == PrismFilterId.ColoredPencil)
        {
            float pencilWidth = Math.Clamp(
                values.Number("PencilWidth"),
                0,
                12);
            float tensorBlurRadius = Math.Clamp(
                pencilWidth * 0.5f,
                1,
                4) *
                deviceScale;
            float licRadius = pencilWidth * deviceScale;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    deviceScale,
                    deviceScale,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    tensorBlurRadius,
                    0,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    tensorBlurRadius,
                    0,
                    0,
                    2,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Iteration,
                    licRadius,
                    licRadius,
                    0,
                    0,
                    3,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.Fresco)
        {
            float brushRadius = Math.Clamp(
                MathF.Max(
                    values.Number("BrushSize"),
                    1) *
                deviceScale,
                1,
                6);
            float tensorBlurRadius = Math.Clamp(
                brushRadius * 0.5f,
                1,
                4);
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    deviceScale,
                    deviceScale,
                    0,
                    0,
                    0,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Horizontal,
                    tensorBlurRadius,
                    0,
                    0,
                    0,
                    1,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Vertical,
                    0,
                    tensorBlurRadius,
                    0,
                    0,
                    2,
                    IsNoOp: false),
                new(
                    PrismCatalogFilterPassKind.Iteration,
                    brushRadius,
                    brushRadius,
                    0,
                    0,
                    3,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.DryBrush)
        {
            float radius = MathF.Min(
                MathF.Max(
                    values.Number("BrushSize"),
                    1),
                6) *
                deviceScale;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    radius,
                    radius,
                    0,
                    0,
                    0,
                    IsNoOp: false)
            ];
        }

        if (filter == PrismFilterId.PaletteKnife)
        {
            float radius = MathF.Max(
                values.Number("StrokeSize"),
                0) *
                deviceScale;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    radius,
                    radius,
                    0,
                    0,
                    0,
                    IsNoOp: radius == 0)
            ];
        }

        if (filter == PrismFilterId.PaintDaubs)
        {
            float radius = Math.Clamp(
                values.Number("BrushSize"),
                1,
                50) *
                deviceScale;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    radius,
                    radius,
                    0,
                    0,
                    0,
                    IsNoOp: false)
            ];
        }

        if (filter is PrismFilterId.Maximum or PrismFilterId.Minimum)
        {
            int morphologyShape = filter == PrismFilterId.Maximum
                ? values.SymbolCode(
                    "Preserve",
                    ("Roundness", 0))
                : values.SymbolCode(
                    "Preserve",
                    ("Roundness", 0),
                    ("Squareness", 1));

            float radius = MorphologyRadius(
                values.Number("Radius"),
                deviceScale,
                sourceBounds);
            if (radius == 0 ||
                (sourceBounds.Width <= 0 && sourceBounds.Height <= 0))
            {
                return
                [
                    new(
                        PrismCatalogFilterPassKind.Direct,
                        0,
                        0,
                        0,
                        0,
                        0,
                        IsNoOp: true)
                ];
            }

            float radiusX = sourceBounds.Width > 0
                ? radius
                : 0;
            float radiusY = sourceBounds.Height > 0
                ? radius
                : 0;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    radiusX,
                    radiusY,
                    radiusX / pixelScale,
                    radiusY / pixelScale,
                    morphologyShape,
                    IsNoOp: false)
            ];
        }

        if (filter is PrismFilterId.Facet or PrismFilterId.Diffuse)
        {
            int iterations = IterationCount(
                values.Number("Iterations"),
                filter);
            if (iterations == 0)
            {
                return
                [
                    new(
                        PrismCatalogFilterPassKind.Iteration,
                        0,
                        0,
                        0,
                        0,
                        0,
                        IsNoOp: true)
                ];
            }

            ImmutableArray<PrismCatalogFilterPass>.Builder passes =
                ImmutableArray.CreateBuilder<PrismCatalogFilterPass>(
                    iterations);
            float iterationSampleRadius =
                filter == PrismFilterId.Facet
                    ? 6
                    : deviceScale;
            float iterationBoundsRadius =
                filter == PrismFilterId.Facet
                    ? 6 / pixelScale
                    : (2 * deviceScale) / pixelScale;
            for (int index = 0; index < iterations; index++)
            {
                passes.Add(
                    new(
                        PrismCatalogFilterPassKind.Iteration,
                        iterationSampleRadius,
                        iterationSampleRadius,
                        iterationBoundsRadius,
                        iterationBoundsRadius,
                        index,
                        IsNoOp: false));
            }
            return passes.MoveToImmutable();
        }

        if (filter == PrismFilterId.Fragment)
        {
            float offset =
                MathF.Max(0, values.Number("Offset")) *
                deviceScale;
            if (offset == 0)
            {
                return
                [
                    new(
                        PrismCatalogFilterPassKind.Direct,
                        0,
                        0,
                        0,
                        0,
                        0,
                        IsNoOp: true)
                ];
            }

            float boundsRadius = offset / pixelScale;
            return
            [
                new(
                    PrismCatalogFilterPassKind.Direct,
                    offset,
                    offset,
                    boundsRadius,
                    boundsRadius,
                    0,
                    IsNoOp: false)
            ];
        }

        float sampleRadius =
            SampleRadius(filter, primitive, values) * deviceScale;
        return
        [
            new(
                PrismCatalogFilterPassKind.Direct,
                sampleRadius,
                sampleRadius,
                0,
                0,
                0,
                IsNoOp: false)
        ];
    }

    private static float SampleRadius(
        PrismFilterId filter,
        PrismCatalogFilterPrimitive primitive,
        PrismFilterParameterReader values)
    {
        return filter switch
        {
            PrismFilterId.Crosshatch => 0,
            PrismFilterId.Craquelure => 0,
            PrismFilterId.Texturizer => 0,
            PrismFilterId.Reticulation => 0,
            PrismFilterId.Spatter => 0,
            PrismFilterId.ColorHalftone =>
                MathF.Max(0, values.Number("MaxRadius")),
            PrismFilterId.ChromaticAberration =>
                ChromaticAberrationSampleRadius(values),
            PrismFilterId.Deinterlace => 9,
            PrismFilterId.CustomConvolution => 1,
            PrismFilterId.FindEdges => 1,
            PrismFilterId.Emboss =>
                MathF.Max(1, values.Number("Height")),
            PrismFilterId.PlasticWrap =>
                1 +
                (2 * Math.Clamp(
                    values.Number("Smoothness") / 15,
                    0,
                    1)),
            PrismFilterId.OilPaint =>
                1 +
                (2 * Math.Clamp(
                    values.Number("Scale"),
                    0,
                    3)),
            PrismFilterId.TraceContour => 1,
            _ when primitive is
                PrismCatalogFilterPrimitive.Artistic or
                PrismCatalogFilterPrimitive.EdgeDetection or
                PrismCatalogFilterPrimitive.Texture => 1,
            _ => 0
        };
    }

    private static float ChromaticAberrationSampleRadius(
        PrismFilterParameterReader values)
    {
        float amount = MathF.Abs(values.Number("Amount"));
        if (!values.Boolean("Radial"))
        {
            return amount;
        }

        Vector4 center = values.Vector("Center");
        float left = MathF.Max(
            center.X * center.X,
            (1 - center.X) * (1 - center.X));
        float top = MathF.Max(
            center.Y * center.Y,
            (1 - center.Y) * (1 - center.Y));
        return amount * 2 * MathF.Sqrt(left + top);
    }

    private static float MorphologyRadius(
        float radius,
        float deviceScale,
        DrawRect sourceBounds)
    {
        double scaledRadius = (double)radius * deviceScale;
        double width = Math.Max(sourceBounds.Width, 0) *
            deviceScale;
        double height = Math.Max(sourceBounds.Height, 0) *
            deviceScale;
        double maximumRelevantRadius = Math.Sqrt(
            (width * width) + (height * height));
        return (float)Math.Min(
            Math.Min(scaledRadius, maximumRelevantRadius),
            float.MaxValue);
    }

    private static int IterationCount(
        float value,
        PrismFilterId filter)
    {
        if (!float.IsFinite(value) ||
            value < 0 ||
            MathF.Truncate(value) != value ||
            value > int.MaxValue)
        {
            throw new InvalidOperationException(
                $"Filter '{filter}' requires an integral iteration count " +
                "representable by the runtime.");
        }
        return (int)value;
    }
}
