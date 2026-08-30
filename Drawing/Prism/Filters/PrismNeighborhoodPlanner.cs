using System.Collections.Immutable;
using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Filters;

internal enum PrismNeighborhoodOperation
{
    Average,
    Blur,
    BlurMore,
    BoxBlur,
    GaussianBlur,
    LensBlur,
    MotionBlur,
    RadialBlur,
    ShapeBlur,
    SmartBlur,
    SurfaceBlur,
    FieldBlur,
    IrisBlur,
    TiltShift,
    PathBlur,
    SpinBlur,
    Sharpen,
    SharpenMore,
    SharpenEdges,
    UnsharpMask,
    SmartSharpen,
    HighPass,
    AddNoise,
    Despeckle,
    DustScratches,
    Median,
    ReduceNoise
}

internal enum PrismNeighborhoodPassKind
{
    Direct,
    Horizontal,
    Vertical,
    RichardsonLucyPsf,
    RichardsonLucyRatio,
    RichardsonLucyBackProject,
    RichardsonLucyUpdate,
    Recombine,
    DespeckleDetect,
    DespeckleFilter,
    DespeckleDecode,
    JpegDeblockHorizontal,
    JpegDeblockVertical
}

internal readonly record struct PrismNeighborhoodPass(
    PrismNeighborhoodPassKind Kind,
    float RadiusX,
    float RadiusY,
    float BoundsRadiusX,
    float BoundsRadiusY,
    int SampleCount,
    bool IsNoOp);

internal readonly record struct PrismNeighborhoodPlan
{
    public PrismNeighborhoodPlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        ImmutableArray<PrismNeighborhoodPass> passes)
    {
        this = default;
        Filter = filter;
        Operation = operation;
        BlendMode = blendMode;
        Passes = passes;
    }

    public PrismFilterId Filter { get; init; }

    public PrismNeighborhoodOperation Operation { get; init; }

    public PrismBlendMode BlendMode { get; init; }

    public ImmutableArray<PrismNeighborhoodPass> Passes { get; init; }

    public Vector4 Options0 { get; init; }

    public Vector4 Options1 { get; init; }

    public Vector4 Options2 { get; init; }

    public Vector4 Options3 { get; init; }

    public PrismResourceId Resource { get; init; }

    public bool ResourceRequired { get; init; }
}

internal static class PrismNeighborhoodPlanner
{
    private const string NeighborhoodOwnerPrefix =
        "PrismKernelRegistry/";
    private const int DraftSamples = 5;
    private const int GoodSamples = 9;
    private const int BestSamples = 17;
    private const int MaximumAdaptiveMedianRadius = 3;
    private const int ReduceNoiseIterationCount = 3;
    private const int MaximumDomainTransformRadius = 8;
    internal const int MaximumSpinSamples = 65;

    public static bool IsSupported(PrismFilterId filter)
    {
        if (!TryGetOperation(filter, out _))
        {
            return false;
        }

        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)filter);
        return entry.Kind == "filter" &&
            string.Equals(
                entry.Coverage.Kernel,
                NeighborhoodOwnerPrefix + entry.Symbol,
                StringComparison.Ordinal);
    }

    public static bool RequiresStableHostCoordinates(PrismFilterId filter) =>
        filter is
            PrismFilterId.IrisBlur or
            PrismFilterId.SpinBlur;

    public static PrismNeighborhoodPlan Create(
        PrismFilterId filter,
        ImmutableArray<PrismGraphParameter> parameters,
        PrismBlendMode blendMode,
        float pixelScale,
        Matrix3x2 effectiveTransform,
        DrawRect sourceBounds)
    {
        if (!IsSupported(filter) ||
            !TryGetOperation(filter, out PrismNeighborhoodOperation operation))
        {
            throw new InvalidOperationException(
                $"Filter '{filter}' has no neighborhood planner.");
        }
        if (!float.IsFinite(pixelScale) || pixelScale <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pixelScale),
                pixelScale,
                "Filter planning requires a finite positive pixel scale.");
        }

        float transformScale = MathF.Max(
            MathF.Sqrt(
                (effectiveTransform.M11 * effectiveTransform.M11) +
                (effectiveTransform.M12 * effectiveTransform.M12)),
            MathF.Sqrt(
                (effectiveTransform.M21 * effectiveTransform.M21) +
                (effectiveTransform.M22 * effectiveTransform.M22)));
        float deviceScale = transformScale * pixelScale;
        if (!float.IsFinite(deviceScale) || deviceScale <= 0)
        {
            throw new InvalidOperationException(
                "The filter transform produced an invalid device scale.");
        }

        float sourceWidth =
            MathF.Max(1, sourceBounds.Width * deviceScale);
        float sourceHeight =
            MathF.Max(1, sourceBounds.Height * deviceScale);
        PrismFilterParameterReader values =
            new(filter, parameters);

        PrismNeighborhoodPlan plan = operation switch
        {
            PrismNeighborhoodOperation.Average =>
                Plan(
                    filter,
                    operation,
                    blendMode,
                    radiusX: 1,
                    radiusY: 1,
                    sampleCount: 9,
                    boundsRadiusX: 0,
                    boundsRadiusY: 0,
                    noOp: sourceWidth <= 1 && sourceHeight <= 1),
            PrismNeighborhoodOperation.Blur =>
                SeparableRadiusPlan(
                    filter,
                    operation,
                    blendMode,
                    values.Number("Radius") * deviceScale,
                    Quality(values, "Quality"),
                    EdgeMode(values, "EdgeMode"),
                    sourceWidth,
                    sourceHeight),
            PrismNeighborhoodOperation.BlurMore =>
                SeparableRadiusPlan(
                    filter,
                    operation,
                    blendMode,
                    values.Number("Radius") * deviceScale,
                    Quality(values, "Quality"),
                    EdgeMode(values, "EdgeMode"),
                    sourceWidth,
                    sourceHeight),
            PrismNeighborhoodOperation.BoxBlur =>
                BoxPlan(
                    filter,
                    operation,
                    blendMode,
                    values.Number("Radius") * deviceScale,
                    values.Number("Iterations"),
                    EdgeMode(values, "EdgeMode"),
                    sourceWidth,
                    sourceHeight),
            PrismNeighborhoodOperation.GaussianBlur =>
                GaussianPlan(
                    filter,
                    operation,
                    blendMode,
                    values.Number("Radius") * deviceScale,
                    Quality(values, "Quality"),
                    EdgeMode(values, "EdgeMode"),
                    sourceWidth,
                    sourceHeight),
            PrismNeighborhoodOperation.LensBlur =>
                LensPlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    deviceScale),
            PrismNeighborhoodOperation.MotionBlur =>
                MotionPlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    pixelScale,
                    effectiveTransform),
            PrismNeighborhoodOperation.RadialBlur =>
                RadialPlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    sourceWidth,
                    sourceHeight),
            PrismNeighborhoodOperation.ShapeBlur =>
                ShapePlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    deviceScale),
            PrismNeighborhoodOperation.SmartBlur =>
                SmartBlurPlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    deviceScale),
            PrismNeighborhoodOperation.SurfaceBlur =>
                SurfaceBlurPlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    deviceScale),
            PrismNeighborhoodOperation.FieldBlur =>
                FieldPlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    deviceScale),
            PrismNeighborhoodOperation.IrisBlur =>
                IrisPlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    deviceScale),
            PrismNeighborhoodOperation.TiltShift =>
                TiltShiftPlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    deviceScale),
            PrismNeighborhoodOperation.PathBlur =>
                PathPlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    deviceScale),
            PrismNeighborhoodOperation.SpinBlur =>
                SpinPlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    sourceWidth,
                    sourceHeight),
            PrismNeighborhoodOperation.Sharpen =>
                ContrastAdaptiveSharpenPlan(
                    filter,
                    operation,
                    blendMode,
                    values),
            PrismNeighborhoodOperation.SharpenMore =>
                BinomialHighBoostPlan(
                    filter,
                    operation,
                    blendMode,
                    values),
            PrismNeighborhoodOperation.SharpenEdges =>
                PointPlan(
                    filter,
                    operation,
                    blendMode,
                    new Vector4(
                        values.Number("Amount"),
                        values.Number("Threshold"),
                        0,
                        0),
                    radius: 1,
                    noOp: values.Number("Amount") == 0),
            PrismNeighborhoodOperation.UnsharpMask =>
                UnsharpPlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    deviceScale,
                    sourceWidth,
                    sourceHeight),
            PrismNeighborhoodOperation.SmartSharpen =>
                SmartSharpenPlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    deviceScale,
                    sourceWidth,
                    sourceHeight),
            PrismNeighborhoodOperation.HighPass =>
                HighPassPlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    deviceScale,
                    sourceWidth,
                    sourceHeight),
            PrismNeighborhoodOperation.AddNoise =>
                NoisePlan(
                    filter,
                    operation,
                    blendMode,
                    values),
            PrismNeighborhoodOperation.Despeckle =>
                DespecklePlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    deviceScale),
            PrismNeighborhoodOperation.DustScratches =>
                DustScratchesPlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    deviceScale),
            PrismNeighborhoodOperation.Median =>
                PointPlan(
                    filter,
                    operation,
                    blendMode,
                    new Vector4(
                        values.Integer("Radius"),
                        0,
                        0,
                        0),
                    radius: values.Integer("Radius"),
                    noOp: values.Integer("Radius") == 0),
            PrismNeighborhoodOperation.ReduceNoise =>
                ReduceNoisePlan(
                    filter,
                    operation,
                    blendMode,
                    values),
            _ => throw new InvalidOperationException(
                $"Neighborhood operation '{operation}' has no planner.")
        };
        if (pixelScale == 1)
        {
            return plan;
        }

        return plan with
        {
            Passes = plan.Passes
                .Select(pass => pass with
                {
                    BoundsRadiusX =
                        pass.BoundsRadiusX / pixelScale,
                    BoundsRadiusY =
                        pass.BoundsRadiusY / pixelScale
                })
                .ToImmutableArray()
        };
    }

    private static PrismNeighborhoodPlan HighPassPlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float deviceScale,
        float sourceWidth,
        float sourceHeight)
    {
        float radius = values.Number("Radius") * deviceScale;
        float sigma = radius / 3f;
        int edgeMode = EdgeMode(values, "EdgeMode");
        Vector4 options = new(radius, BestSamples, edgeMode, sigma);

        ImmutableArray<PrismNeighborhoodPass>.Builder passes =
            ImmutableArray.CreateBuilder<PrismNeighborhoodPass>(3);
        if (radius > 0 && sourceWidth > 1)
        {
            passes.Add(new PrismNeighborhoodPass(
                PrismNeighborhoodPassKind.Horizontal,
                radius,
                0,
                0,
                0,
                BestSamples,
                IsNoOp: false));
        }
        if (radius > 0 && sourceHeight > 1)
        {
            passes.Add(new PrismNeighborhoodPass(
                PrismNeighborhoodPassKind.Vertical,
                0,
                radius,
                0,
                0,
                BestSamples,
                IsNoOp: false));
        }
        passes.Add(new PrismNeighborhoodPass(
            PrismNeighborhoodPassKind.Recombine,
            0,
            0,
            0,
            0,
            1,
            IsNoOp: false));
        return new PrismNeighborhoodPlan(
            filter,
            operation,
            blendMode,
            passes.ToImmutable())
        {
            Options0 = options
        };
    }

    private static PrismNeighborhoodPlan GaussianPlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        float radius,
        int sampleCount,
        int edgeMode,
        float sourceWidth,
        float sourceHeight)
    {


        float sigma = radius / 3f;
        return SeparableRadiusPlan(
            filter,
            operation,
            blendMode,
            radius,
            sampleCount,
            edgeMode,
            sourceWidth,
            sourceHeight,
            new Vector4(radius, sampleCount, edgeMode, sigma));
    }

    private static PrismNeighborhoodPlan BoxPlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        float radius,
        float iterations,
        int edgeMode,
        float sourceWidth,
        float sourceHeight)
    {
        int integerRadius = Math.Max(0, (int)MathF.Round(radius));
        int iterationCount = Math.Max(0, (int)MathF.Round(iterations));
        if (integerRadius == 0 || iterationCount == 0 ||
            (sourceWidth <= 1 && sourceHeight <= 1))
        {
            return Plan(
                filter,
                operation,
                blendMode,
                0,
                0,
                1,
                0,
                0,
                noOp: true) with
            {
                Options0 = new Vector4(
                    integerRadius,
                    iterationCount,
                    edgeMode,
                    0)
            };
        }

        ImmutableArray<PrismNeighborhoodPass>.Builder passes =
            ImmutableArray.CreateBuilder<PrismNeighborhoodPass>(
                checked(iterationCount * 2));
        int sampleCount = checked((integerRadius * 2) + 1);
        for (int index = 0; index < iterationCount; index++)
        {
            passes.Add(new PrismNeighborhoodPass(
                PrismNeighborhoodPassKind.Horizontal,
                integerRadius,
                0,
                integerRadius,
                0,
                sampleCount,
                IsNoOp: false));
            passes.Add(new PrismNeighborhoodPass(
                PrismNeighborhoodPassKind.Vertical,
                0,
                integerRadius,
                0,
                integerRadius,
                sampleCount,
                IsNoOp: false));
        }
        return new PrismNeighborhoodPlan(
            filter,
            operation,
            blendMode,
            passes.ToImmutable())
        {
            Options0 = new Vector4(
                integerRadius,
                iterationCount,
                edgeMode,
                0)
        };
    }

    private static PrismNeighborhoodPlan LensPlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float radius = values.Number("Radius") * deviceScale;
        PrismResourceId depthMap = values.Resource("DepthMap");
        return Plan(
            filter,
            operation,
            blendMode,
            radius,
            radius,
            BestSamples,
            radius,
            radius,
            noOp: radius == 0) with
        {
            Options0 = new Vector4(
                radius,
                values.Number("BladeCount"),
                values.Number("BladeCurvature"),
                Degrees(values.Number("Rotation"))),
            Options1 = new Vector4(
                values.Number("SpecularBrightness"),
                values.Number("SpecularThreshold"),
                DepthChannel(values, "DepthChannel"),
                values.Number("FocalDistance")),
            Options2 = new Vector4(
                values.Boolean("InvertDepth") ? 1 : 0,
                values.Number("Noise"),
                Distribution(values, "NoiseDistribution"),
                values.Boolean("MonochromaticNoise") ? 1 : 0),
            Resource = depthMap
        };
    }

    private static PrismNeighborhoodPlan MotionPlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float pixelScale,
        Matrix3x2 effectiveTransform)
    {
        float distance = values.Number("Distance");
        float angle = Degrees(values.Number("Angle"));
        Vector2 travel = Vector2.TransformNormal(
            new Vector2(
                MathF.Cos(angle) * distance,
                -MathF.Sin(angle) * distance),
            effectiveTransform) * pixelScale;
        Vector2 halfTravel = travel * 0.5f;
        float effectiveDistance = travel.Length();
        float effectiveAngle = effectiveDistance == 0
            ? angle
            : MathF.Atan2(-travel.Y, travel.X);
        int sampleCount = Quality(values, "Quality");
        return Plan(
            filter,
            operation,
            blendMode,
            halfTravel.X,
            halfTravel.Y,
            sampleCount,
            MathF.Abs(halfTravel.X),
            MathF.Abs(halfTravel.Y),
            noOp: effectiveDistance == 0) with
        {
            Options0 = new Vector4(
                effectiveDistance,
                effectiveAngle,
                sampleCount,
                EdgeMode(values, "EdgeMode"))
        };
    }

    private static PrismNeighborhoodPlan RadialPlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float sourceWidth,
        float sourceHeight)
    {
        float amount = values.Number("Amount");
        int mode = RadialMode(values, "Mode");
        int sampleCount = Quality(values, "Quality");
        Vector4 center = values.Vector("Center");
        float maximumTravel;
        if (mode == 0)
        {
            float maximumRadius = MathF.Sqrt(
                (sourceWidth * sourceWidth) +
                (sourceHeight * sourceHeight));
            float boundedSweep = MathF.Min(
                MathF.Abs(amount),
                MathF.PI * 2);
            maximumTravel = maximumRadius *
                MathF.Sin(boundedSweep * 0.25f) * 2;
        }
        else
        {
            maximumTravel = MathF.Max(sourceWidth, sourceHeight) *
                MathF.Abs(amount) * 0.5f;
        }
        return Plan(
            filter,
            operation,
            blendMode,
            maximumTravel,
            maximumTravel,
            sampleCount,
            boundsRadiusX: 0,
            boundsRadiusY: 0,
            noOp: amount == 0) with
        {
            Options0 = new Vector4(
                mode,
                amount,
                center.X,
                center.Y),
            Options1 = new Vector4(
                sampleCount,
                0,
                0,
                0)
        };
    }

    private static PrismNeighborhoodPlan ShapePlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float radius = values.Number("Radius") * deviceScale;
        int sampleCount = Quality(values, "Quality");
        return Plan(
            filter,
            operation,
            blendMode,
            radius,
            radius,
            sampleCount,
            radius,
            radius,
            noOp: radius == 0) with
        {
            Options0 = new Vector4(
                radius,
                EdgeMode(values, "EdgeMode"),
                sampleCount,
                0),
            Resource = values.Resource("Shape"),
            ResourceRequired = true
        };
    }

    private static PrismNeighborhoodPlan SmartBlurPlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float radius = values.Number("Radius") * deviceScale;
        int quality = Quality(values, "Quality");
        return Plan(
            filter,
            operation,
            blendMode,
            radius,
            radius,
            quality,
            boundsRadiusX: 0,
            boundsRadiusY: 0,
            noOp: radius == 0) with
        {
            Options0 = new Vector4(
                radius,
                MathF.Max(0, values.Number("Threshold")),
                quality,
                SmartBlurMode(values, "Mode")),
            Options1 = new Vector4(EdgeMode(values, "EdgeMode"), 0, 0, 0)
        };
    }

    private static PrismNeighborhoodPlan SurfaceBlurPlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float radius = values.Number("Radius") * deviceScale;
        float threshold = MathF.Max(0, values.Number("Threshold"));
        int quality = Quality(values, "Quality");
        return Plan(
            filter,
            operation,
            blendMode,
            radius,
            radius,
            quality,
            boundsRadiusX: 0,
            boundsRadiusY: 0,
            noOp: radius == 0) with
        {
            Options0 = new Vector4(radius, threshold, quality, 0),
            Options1 = new Vector4(EdgeMode(values, "EdgeMode"), 0, 0, 0)
        };
    }

    private static PrismNeighborhoodPlan FieldPlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float blur = values.Number("Blur") * deviceScale;
        int quality = Quality(values, "Quality");
        return Plan(
            filter,
            operation,
            blendMode,
            radiusX: blur,
            radiusY: blur,
            sampleCount: quality,
            boundsRadiusX: blur,
            boundsRadiusY: blur,
            noOp: blur <= 0.000001f) with
        {
            Options0 = new Vector4(
                values.Number("FocalDistance"),
                values.Boolean("Invert") ? 1 : 0,
                values.Number("Highlight"),
                0),
            Resource = values.Resource("BlurField"),
            ResourceRequired = true
        };
    }

    private static PrismNeighborhoodPlan IrisPlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float deviceScale)
    {
        Vector4 center = values.Vector("Center");
        Vector4 radius = values.Vector("Radius");
        float blur = values.Number("Blur") * deviceScale;
        return Plan(
            filter,
            operation,
            blendMode,
            blur,
            blur,
            BestSamples,
            boundsRadiusX: 0,
            boundsRadiusY: 0,
            noOp: blur == 0) with
        {
            Options0 = new Vector4(
                center.X,
                center.Y,
                radius.X,
                radius.Y),
            Options1 = new Vector4(
                values.Number("Feather"),
                Degrees(values.Number("Rotation")),
                blur,
                0)
        };
    }

    private static PrismNeighborhoodPlan TiltShiftPlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float deviceScale)
    {
        Vector4 center = values.Vector("Center");
        float blur = values.Number("Blur") * deviceScale;
        return Plan(
            filter,
            operation,
            blendMode,
            blur,
            blur,
            BestSamples,
            boundsRadiusX: 0,
            boundsRadiusY: 0,
            noOp: blur == 0) with
        {
            Options0 = new Vector4(
                center.X,
                center.Y,
                Degrees(values.Number("Angle")),
                values.Number("FocusWidth")),
            Options1 = new Vector4(
                values.Number("Feather"),
                blur,
                0,
                0)
        };
    }

    private static PrismNeighborhoodPlan PathPlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float speed = values.Number("Speed") * deviceScale;
        float endSpeed = values.Number("EndSpeed") * deviceScale;
        float noise = values.Number("Noise");
        float radius = MathF.Max(MathF.Abs(speed), MathF.Abs(endSpeed));
        return Plan(
            filter,
            operation,
            blendMode,
            radius,
            radius,
            BestSamples,
            boundsRadiusX: radius,
            boundsRadiusY: radius,
            noOp: speed == 0 && endSpeed == 0) with
        {
            Options0 = new Vector4(
                speed,
                values.Number("Taper"),
                values.Boolean("CenteredBlur") ? 1 : 0,
                endSpeed),
            Options1 = new Vector4(
                PathShape(values, "Shape"),
                FlashSync(values, "FlashSync"),
                noise,
                0),
            Resource = values.Resource("Path"),
            ResourceRequired = true
        };
    }

    private static PrismNeighborhoodPlan SpinPlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float sourceWidth,
        float sourceHeight)
    {
        Vector4 center = values.Vector("Center");
        Vector4 radius = values.Vector("Radius");
        float rotation = Degrees(values.Number("Rotation"));
        float radiusX = MathF.Abs(radius.X);
        float radiusY = MathF.Abs(radius.Y);
        float maximumRadius = MathF.Max(
            radiusX * sourceWidth,
            radiusY * sourceHeight);
        int sampleCount = SpinSampleCount(
            MathF.Abs(rotation) * maximumRadius,
            MaximumSpinSamples);
        return Plan(
            filter,
            operation,
            blendMode,
            maximumRadius,
            maximumRadius,
            sampleCount,
            boundsRadiusX: 0,
            boundsRadiusY: 0,
            noOp:
                rotation == 0 ||
                radiusX <= 0.000001f ||
                radiusY <= 0.000001f) with
        {
            Options0 = new Vector4(
                center.X,
                center.Y,
                radiusX,
                radiusY),
            Options1 = new Vector4(
                rotation,
                Math.Clamp(values.Number("Feather"), 0, 1),
                Math.Clamp(values.Number("StrobeStrength"), 0, 1),
                Math.Clamp(
                    values.Integer("StrobeFlashes"),
                    0,
                    MaximumSpinSamples - 1)),
            Options2 = new Vector4(
                Math.Clamp(values.Number("StrobeDuration"), 0, 1),
                Math.Clamp(values.Number("Noise"), 0, 1),
                0,
                0)
        };
    }

    internal static int SpinSampleCount(
        float arcLength,
        int maximumSamples)
    {
        if (!float.IsFinite(arcLength) || arcLength <= 0)
        {
            return 1;
        }

        int maximumIntervals = Math.Max(2, maximumSamples - 1);
        maximumIntervals -= maximumIntervals & 1;
        int intervals = arcLength >= maximumIntervals
            ? maximumIntervals
            : Math.Max(2, (int)MathF.Ceiling(arcLength));
        intervals += intervals & 1;
        return Math.Min(intervals, maximumIntervals) + 1;
    }

    private static PrismNeighborhoodPlan UnsharpPlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float deviceScale,
        float sourceWidth,
        float sourceHeight)
    {
        float amount = values.Number("Amount");
        float radius = values.Number("Radius") * deviceScale;
        float sigma = radius / 3f;
        Vector4 options = new(
            amount,
            radius,
            values.Number("Threshold"),
            sigma);
        if (amount == 0 ||
            radius == 0 ||
            (sourceWidth <= 1 && sourceHeight <= 1))
        {
            return Plan(
                filter,
                operation,
                blendMode,
                0,
                0,
                1,
                0,
                0,
                noOp: true) with
            {
                Options0 = options
            };
        }

        ImmutableArray<PrismNeighborhoodPass>.Builder passes =
            ImmutableArray.CreateBuilder<PrismNeighborhoodPass>(3);
        if (sourceWidth > 1)
        {
            passes.Add(new PrismNeighborhoodPass(
                PrismNeighborhoodPassKind.Horizontal,
                radius,
                0,
                0,
                0,
                BestSamples,
                IsNoOp: false));
        }
        if (sourceHeight > 1)
        {
            passes.Add(new PrismNeighborhoodPass(
                PrismNeighborhoodPassKind.Vertical,
                0,
                radius,
                0,
                0,
                BestSamples,
                IsNoOp: false));
        }
        passes.Add(new PrismNeighborhoodPass(
            PrismNeighborhoodPassKind.Recombine,
            0,
            0,
            0,
            0,
            1,
            IsNoOp: false));
        return new PrismNeighborhoodPlan(
            filter,
            operation,
            blendMode,
            passes.ToImmutable())
        {
            Options0 = options
        };
    }

    private static PrismNeighborhoodPlan SmartSharpenPlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float deviceScale,
        float sourceWidth,
        float sourceHeight)
    {
        const int iterationCount = 4;
        float amount = values.Number("Amount");
        float radius = values.Number("Radius") * deviceScale;
        float reduceNoise = values.Number("ReduceNoise");
        int remove = SmartSharpenRemove(values, "Remove");
        bool noOp =
            amount <= 0 ||
            radius <= 0 ||
            (sourceWidth <= 1 && sourceHeight <= 1);
        if (noOp)
        {
            return PointPlan(
                filter,
                operation,
                blendMode,
                new Vector4(amount, radius, reduceNoise, remove),
                radius: 0,
                noOp: true);
        }

        ImmutableArray<PrismNeighborhoodPass>.Builder passes =
            ImmutableArray.CreateBuilder<PrismNeighborhoodPass>(
                (iterationCount * 4) + 1);
        for (int iteration = 0; iteration < iterationCount; iteration++)
        {
            passes.Add(new PrismNeighborhoodPass(
                PrismNeighborhoodPassKind.RichardsonLucyPsf,
                radius,
                radius,
                0,
                0,
                BestSamples,
                IsNoOp: false));
            passes.Add(new PrismNeighborhoodPass(
                PrismNeighborhoodPassKind.RichardsonLucyRatio,
                0,
                0,
                0,
                0,
                1,
                IsNoOp: false));
            passes.Add(new PrismNeighborhoodPass(
                PrismNeighborhoodPassKind.RichardsonLucyBackProject,
                radius,
                radius,
                0,
                0,
                BestSamples,
                IsNoOp: false));
            passes.Add(new PrismNeighborhoodPass(
                PrismNeighborhoodPassKind.RichardsonLucyUpdate,
                0,
                0,
                0,
                0,
                1,
                IsNoOp: false));
        }
        float shadowRadius =
            values.Number("ShadowRadius") * deviceScale;
        float highlightRadius =
            values.Number("HighlightRadius") * deviceScale;
        passes.Add(new PrismNeighborhoodPass(
            PrismNeighborhoodPassKind.Recombine,
            MathF.Max(shadowRadius, highlightRadius),
            MathF.Max(shadowRadius, highlightRadius),
            0,
            0,
            BestSamples,
            IsNoOp: false));

        return new PrismNeighborhoodPlan(
            filter,
            operation,
            blendMode,
            passes.MoveToImmutable())
        {
            Options0 = new Vector4(
                amount,
                radius,
                reduceNoise,
                remove),
            Options1 = new Vector4(
                Degrees(values.Number("Angle")),
                values.Number("ShadowFade"),
                values.Number("ShadowTonalWidth"),
                shadowRadius),
            Options2 = new Vector4(
                values.Number("HighlightFade"),
                values.Number("HighlightTonalWidth"),
                highlightRadius,
                iterationCount)
        };
    }

    private static PrismNeighborhoodPlan NoisePlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values)
    {
        int seed = values.Integer("Seed");
        return PointPlan(
            filter,
            operation,
            blendMode,
            new Vector4(
                values.Number("Amount"),
                Distribution(values, "Distribution"),
                values.Boolean("Monochromatic") ? 1 : 0,
                seed & 0xffff),
            noOp: values.Number("Amount") == 0) with
        {
            Options1 = new Vector4(
                (seed >> 16) & 0xffff,
                0,
                0,
                0)
        };
    }

    private static PrismNeighborhoodPlan DustScratchesPlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float scaledRadius = MathF.Max(
            0,
            values.Number("Radius") * deviceScale);
        int radius = Math.Clamp(
            (int)MathF.Ceiling(scaledRadius),
            0,
            MaximumAdaptiveMedianRadius);
        int diameter = (radius * 2) + 1;
        return Plan(
            filter,
            operation,
            blendMode,
            radius,
            radius,
            diameter * diameter,
            boundsRadiusX: 0,
            boundsRadiusY: 0,
            noOp: radius == 0) with
        {
            Options0 = new Vector4(
                radius,
                MathF.Max(0, values.Number("Threshold")),
                0,
                0)
        };
    }

    private static PrismNeighborhoodPlan DespecklePlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float deviceScale)
    {
        const int iterationCount = 3;
        float radius = values.Number("Radius") * deviceScale;
        bool noOp = radius <= 0;
        ImmutableArray<PrismNeighborhoodPass>.Builder passes =
            ImmutableArray.CreateBuilder<PrismNeighborhoodPass>(
                (iterationCount * 2) + 1);
        for (int iteration = 0; iteration < iterationCount; iteration++)
        {
            passes.Add(new PrismNeighborhoodPass(
                PrismNeighborhoodPassKind.DespeckleDetect,
                radius,
                radius,
                0,
                0,
                iteration,
                noOp));
        }
        for (int iteration = 0; iteration < iterationCount; iteration++)
        {
            passes.Add(new PrismNeighborhoodPass(
                PrismNeighborhoodPassKind.DespeckleFilter,
                radius,
                radius,
                0,
                0,
                iteration,
                noOp));
        }
        passes.Add(new PrismNeighborhoodPass(
            PrismNeighborhoodPassKind.DespeckleDecode,
            radius,
            radius,
            0,
            0,
            0,
            noOp));

        return new PrismNeighborhoodPlan(
            filter,
            operation,
            blendMode,
            passes.MoveToImmutable())
        {
            Options0 = new Vector4(
                MathF.Max(0, values.Number("Threshold")),
                radius,
                iterationCount,
                0)
        };
    }

    private static PrismNeighborhoodPlan ReduceNoisePlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values)
    {
        float strength = Math.Clamp(values.Number("Strength"), 0, 1);
        float preserveDetails =
            Math.Clamp(values.Number("PreserveDetails"), 0, 1);
        float colorNoise =
            Math.Clamp(values.Number("ReduceColorNoise"), 0, 1);
        float sharpen =
            Math.Clamp(values.Number("SharpenDetails"), 0, 1);
        bool removeJpeg = values.Boolean("RemoveJpegArtifact");
        float domainAmount = MathF.Max(
            strength,
            MathF.Max(colorNoise, sharpen));
        float spatialSigma = 0.75f + (3.25f * domainAmount);
        Vector4 iterationSigmas = new(
            DomainTransformIterationSigma(spatialSigma, 0),
            DomainTransformIterationSigma(spatialSigma, 1),
            DomainTransformIterationSigma(spatialSigma, 2),
            0);
        bool domainNoOp = domainAmount <= 0;
        bool noOp = domainNoOp && !removeJpeg;

        ImmutableArray<PrismNeighborhoodPass>.Builder passes =
            ImmutableArray.CreateBuilder<PrismNeighborhoodPass>(
                (ReduceNoiseIterationCount * 2) +
                (removeJpeg ? 2 : 0) +
                1);
        for (int iteration = 0;
            iteration < ReduceNoiseIterationCount;
            iteration++)
        {
            float iterationSigma = DomainTransformIterationSigma(
                spatialSigma,
                iteration);
            int radius = Math.Clamp(
                (int)MathF.Ceiling(iterationSigma * 3),
                1,
                MaximumDomainTransformRadius);
            passes.Add(new PrismNeighborhoodPass(
                PrismNeighborhoodPassKind.Horizontal,
                radius,
                0,
                0,
                0,
                iteration,
                domainNoOp));
            passes.Add(new PrismNeighborhoodPass(
                PrismNeighborhoodPassKind.Vertical,
                0,
                radius,
                0,
                0,
                iteration,
                domainNoOp));
        }
        if (removeJpeg)
        {
            passes.Add(new PrismNeighborhoodPass(
                PrismNeighborhoodPassKind.JpegDeblockHorizontal,
                2,
                0,
                0,
                0,
                1,
                IsNoOp: false));
            passes.Add(new PrismNeighborhoodPass(
                PrismNeighborhoodPassKind.JpegDeblockVertical,
                0,
                2,
                0,
                0,
                1,
                IsNoOp: false));
        }
        passes.Add(new PrismNeighborhoodPass(
            PrismNeighborhoodPassKind.Recombine,
            0,
            0,
            0,
            0,
            1,
            noOp));

        return new PrismNeighborhoodPlan(
            filter,
            operation,
            blendMode,
            passes.MoveToImmutable())
        {
            Options0 = new Vector4(
                strength,
                preserveDetails,
                colorNoise,
                sharpen),
            Options1 = new Vector4(
                removeJpeg ? 1 : 0,
                spatialSigma,
                ReduceNoiseIterationCount,
                0),
            Options2 = iterationSigmas
        };
    }

    private static float DomainTransformIterationSigma(
        float spatialSigma,
        int iteration)
    {
        float numerator =
            MathF.Sqrt(3) *
            MathF.Pow(
                2,
                ReduceNoiseIterationCount - iteration - 1);
        float denominator = MathF.Sqrt(
            MathF.Pow(4, ReduceNoiseIterationCount) - 1);
        return spatialSigma * numerator / denominator;
    }

    private static PrismNeighborhoodPlan RadiusPlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        float radius,
        int sampleCount,
        int edgeMode,
        bool separable,
        bool? noOp = null,
        bool expandBounds = true)
    {
        Vector4 options =
            new(radius, sampleCount, edgeMode, 0);
        return separable
            ? SeparableRadiusPlan(
                filter,
                operation,
                blendMode,
                radius,
                sampleCount,
                edgeMode,
                sourceWidth: 2,
                sourceHeight: 2,
                options,
                noOp ?? radius == 0,
                expandBounds)
            : Plan(
                filter,
                operation,
                blendMode,
                radius,
                radius,
                sampleCount,
                expandBounds ? radius : 0,
                expandBounds ? radius : 0,
                noOp ?? radius == 0) with
            {
                Options0 = options
            };
    }

    private static PrismNeighborhoodPlan SeparableRadiusPlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        float radius,
        int sampleCount,
        int edgeMode,
        float sourceWidth,
        float sourceHeight,
        Vector4? options = null,
        bool? noOp = null,
        bool expandBounds = true)
    {
        bool isNoOp = noOp ?? radius == 0;
        if (isNoOp)
        {
            return Plan(
                filter,
                operation,
                blendMode,
                0,
                0,
                1,
                0,
                0,
                noOp: true) with
            {
                Options0 =
                    options ?? new Vector4(radius, sampleCount, edgeMode, 0)
            };
        }

        ImmutableArray<PrismNeighborhoodPass>.Builder passes =
            ImmutableArray.CreateBuilder<PrismNeighborhoodPass>(2);
        if (sourceWidth > 1)
        {
            passes.Add(
                new PrismNeighborhoodPass(
                    PrismNeighborhoodPassKind.Horizontal,
                    radius,
                    0,
                    expandBounds ? radius : 0,
                    0,
                    sampleCount,
                    IsNoOp: false));
        }
        if (sourceHeight > 1)
        {
            passes.Add(
                new PrismNeighborhoodPass(
                    PrismNeighborhoodPassKind.Vertical,
                    0,
                    radius,
                    0,
                    expandBounds ? radius : 0,
                    sampleCount,
                    IsNoOp: false));
        }
        if (passes.Count == 0)
        {
            passes.Add(
                new PrismNeighborhoodPass(
                    PrismNeighborhoodPassKind.Direct,
                    0,
                    0,
                    0,
                    0,
                    1,
                    IsNoOp: true));
        }

        return new PrismNeighborhoodPlan(
            filter,
            operation,
            blendMode,
            passes.ToImmutable())
        {
            Options0 =
                options ?? new Vector4(radius, sampleCount, edgeMode, 0)
        };
    }

    private static PrismNeighborhoodPlan ContrastAdaptiveSharpenPlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values)
    {
        float amount = Math.Clamp(values.Number("Amount"), 0, 1);
        return Plan(
            filter,
            operation,
            blendMode,
            radiusX: 1,
            radiusY: 1,
            sampleCount: 5,
            boundsRadiusX: 0,
            boundsRadiusY: 0,
            noOp: amount == 0) with
        {
            Options0 = new Vector4(amount, 0, 0, 0)
        };
    }

    private static PrismNeighborhoodPlan BinomialHighBoostPlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values)
    {
        float amount = Math.Clamp(values.Number("Amount"), 0, 1);
        return Plan(
            filter,
            operation,
            blendMode,
            radiusX: 1,
            radiusY: 1,
            sampleCount: 9,
            boundsRadiusX: 0,
            boundsRadiusY: 0,
            noOp: amount == 0) with
        {
            Options0 = new Vector4(amount, 0, 0, 0)
        };
    }

    private static PrismNeighborhoodPlan PointPlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        Vector4 options0,
        float radius = 0,
        bool noOp = false)
    {
        return Plan(
            filter,
            operation,
            blendMode,
            radius,
            radius,
            GoodSamples,
            boundsRadiusX: 0,
            boundsRadiusY: 0,
            noOp) with
        {
            Options0 = options0
        };
    }

    private static PrismNeighborhoodPlan Plan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        float radiusX,
        float radiusY,
        int sampleCount,
        float boundsRadiusX,
        float boundsRadiusY,
        bool noOp)
    {
        return new PrismNeighborhoodPlan(
            filter,
            operation,
            blendMode,
            [
                new PrismNeighborhoodPass(
                    PrismNeighborhoodPassKind.Direct,
                    radiusX,
                    radiusY,
                    boundsRadiusX,
                    boundsRadiusY,
                    sampleCount,
                    noOp)
            ]);
    }

    private static int Quality(
        PrismFilterParameterReader values,
        string name) =>
        values.SymbolCode(
            name,
            ("Draft", DraftSamples),
            ("Low", DraftSamples),
            ("Good", GoodSamples),
            ("Medium", GoodSamples),
            ("Best", BestSamples),
            ("High", BestSamples));

    private static int EdgeMode(
        PrismFilterParameterReader values,
        string name) =>
        values.SymbolCode(
            name,
            ("Clamp", 0),
            ("Transparent", 1),
            ("Wrap", 2),
            ("Mirror", 3),
            ("Reflect", 3));

    private static int Distribution(
        PrismFilterParameterReader values,
        string name) =>
        values.SymbolCode(
            name,
            ("Uniform", 0),
            ("Gaussian", 1));

    private static int DepthChannel(
        PrismFilterParameterReader values,
        string name) =>
        values.SymbolCode(
            name,
            ("Luminance", 0),
            ("Red", 1),
            ("Green", 2),
            ("Blue", 3),
            ("Alpha", 4));

    private static int RadialMode(
        PrismFilterParameterReader values,
        string name) =>
        values.SymbolCode(
            name,
            ("Spin", 0),
            ("Zoom", 1));

    private static int SmartBlurMode(
        PrismFilterParameterReader values,
        string name) =>
        values.SymbolCode(
            name,
            ("Normal", 0),
            ("EdgeOnly", 1),
            ("OverlayEdge", 2));

    private static int PathShape(
        PrismFilterParameterReader values,
        string name) =>
        values.SymbolCode(
            name,
            ("Basic", 0),
            ("Taper", 1));

    private static int FlashSync(
        PrismFilterParameterReader values,
        string name) =>
        values.SymbolCode(
            name,
            ("Rear", 0),
            ("Center", 1),
            ("Front", 2));

    private static int SmartSharpenRemove(
        PrismFilterParameterReader values,
        string name) =>
        values.SymbolCode(
            name,
            ("GaussianBlur", 0),
            ("LensBlur", 1),
            ("MotionBlur", 2));

    private static float Degrees(float value) =>
        value * (MathF.PI / 180f);

    private static bool TryGetOperation(
        PrismFilterId filter,
        out PrismNeighborhoodOperation operation)
    {
        operation = filter switch
        {
            PrismFilterId.Average => PrismNeighborhoodOperation.Average,
            PrismFilterId.Blur => PrismNeighborhoodOperation.Blur,
            PrismFilterId.BlurMore => PrismNeighborhoodOperation.BlurMore,
            PrismFilterId.BoxBlur => PrismNeighborhoodOperation.BoxBlur,
            PrismFilterId.GaussianBlur => PrismNeighborhoodOperation.GaussianBlur,
            PrismFilterId.LensBlur => PrismNeighborhoodOperation.LensBlur,
            PrismFilterId.MotionBlur => PrismNeighborhoodOperation.MotionBlur,
            PrismFilterId.RadialBlur => PrismNeighborhoodOperation.RadialBlur,
            PrismFilterId.ShapeBlur => PrismNeighborhoodOperation.ShapeBlur,
            PrismFilterId.SmartBlur => PrismNeighborhoodOperation.SmartBlur,
            PrismFilterId.SurfaceBlur => PrismNeighborhoodOperation.SurfaceBlur,
            PrismFilterId.FieldBlur => PrismNeighborhoodOperation.FieldBlur,
            PrismFilterId.IrisBlur => PrismNeighborhoodOperation.IrisBlur,
            PrismFilterId.TiltShift => PrismNeighborhoodOperation.TiltShift,
            PrismFilterId.PathBlur => PrismNeighborhoodOperation.PathBlur,
            PrismFilterId.SpinBlur => PrismNeighborhoodOperation.SpinBlur,
            PrismFilterId.Sharpen => PrismNeighborhoodOperation.Sharpen,
            PrismFilterId.SharpenMore => PrismNeighborhoodOperation.SharpenMore,
            PrismFilterId.SharpenEdges => PrismNeighborhoodOperation.SharpenEdges,
            PrismFilterId.UnsharpMask => PrismNeighborhoodOperation.UnsharpMask,
            PrismFilterId.SmartSharpen => PrismNeighborhoodOperation.SmartSharpen,
            PrismFilterId.HighPass => PrismNeighborhoodOperation.HighPass,
            PrismFilterId.AddNoise => PrismNeighborhoodOperation.AddNoise,
            PrismFilterId.Despeckle => PrismNeighborhoodOperation.Despeckle,
            PrismFilterId.DustScratches => PrismNeighborhoodOperation.DustScratches,
            PrismFilterId.Median => PrismNeighborhoodOperation.Median,
            PrismFilterId.ReduceNoise => PrismNeighborhoodOperation.ReduceNoise,
            _ => (PrismNeighborhoodOperation)(-1)
        };
        return (int)operation >= 0;
    }
}
