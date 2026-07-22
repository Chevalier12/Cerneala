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
    Vertical
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
                    sourceWidth,
                    sourceHeight,
                    sampleCount: BestSamples,
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
                RadiusPlan(
                    filter,
                    operation,
                    blendMode,
                    values.Number("Radius") * deviceScale,
                    Quality(values, "Quality"),
                    EdgeMode(values, "EdgeMode"),
                    separable: false),
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
                SeparableRadiusPlan(
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
                    deviceScale),
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
                EdgeAwarePlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    deviceScale,
                    hasMode: true),
            PrismNeighborhoodOperation.SurfaceBlur =>
                EdgeAwarePlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    deviceScale,
                    hasMode: false),
            PrismNeighborhoodOperation.FieldBlur =>
                FieldPlan(
                    filter,
                    operation,
                    blendMode,
                    values),
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
                    values),
            PrismNeighborhoodOperation.Sharpen or
            PrismNeighborhoodOperation.SharpenMore =>
                PointPlan(
                    filter,
                    operation,
                    blendMode,
                    new Vector4(values.Number("Amount"), 0, 0, 0),
                    noOp: values.Number("Amount") == 0),
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
                    noOp: values.Number("Amount") == 0),
            PrismNeighborhoodOperation.UnsharpMask =>
                UnsharpPlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    deviceScale),
            PrismNeighborhoodOperation.SmartSharpen =>
                SmartSharpenPlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    deviceScale),
            PrismNeighborhoodOperation.HighPass =>
                RadiusPlan(
                    filter,
                    operation,
                    blendMode,
                    values.Number("Radius") * deviceScale,
                    GoodSamples,
                    EdgeMode(values, "EdgeMode"),
                    separable: false,
                    noOp: false,
                    expandBounds: false),
            PrismNeighborhoodOperation.AddNoise =>
                NoisePlan(
                    filter,
                    operation,
                    blendMode,
                    values),
            PrismNeighborhoodOperation.Despeckle =>
                PointPlan(
                    filter,
                    operation,
                    blendMode,
                    new Vector4(
                        values.Number("Threshold"),
                        values.Number("Radius") * deviceScale,
                        0,
                        0),
                    radius: values.Number("Radius") * deviceScale,
                    noOp: values.Number("Radius") == 0),
            PrismNeighborhoodOperation.DustScratches =>
                PointPlan(
                    filter,
                    operation,
                    blendMode,
                    new Vector4(
                        values.Number("Radius") * deviceScale,
                        values.Number("Threshold"),
                        0,
                        0),
                    radius: values.Number("Radius") * deviceScale,
                    noOp: values.Number("Radius") == 0),
            PrismNeighborhoodOperation.Median =>
                PointPlan(
                    filter,
                    operation,
                    blendMode,
                    new Vector4(
                        values.Number("Radius") * deviceScale,
                        0,
                        0,
                        0),
                    radius: values.Number("Radius") * deviceScale,
                    noOp: values.Number("Radius") == 0),
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
        float support = radius * MathF.Max(0, iterations);
        return SeparableRadiusPlan(
            filter,
            operation,
            blendMode,
            support,
            GoodSamples,
            edgeMode,
            sourceWidth,
            sourceHeight,
            new Vector4(radius, iterations, edgeMode, 0),
            noOp: radius == 0 || iterations == 0);
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
        float deviceScale)
    {
        float distance = values.Number("Distance") * deviceScale;
        return Plan(
            filter,
            operation,
            blendMode,
            distance,
            distance,
            Quality(values, "Quality"),
            distance,
            distance,
            noOp: distance == 0) with
        {
            Options0 = new Vector4(
                distance,
                Degrees(values.Number("Angle")),
                Quality(values, "Quality"),
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
        Vector4 center = values.Vector("Center");
        return Plan(
            filter,
            operation,
            blendMode,
            MathF.Max(sourceWidth, sourceHeight) * MathF.Abs(amount),
            MathF.Max(sourceWidth, sourceHeight) * MathF.Abs(amount),
            Quality(values, "Quality"),
            boundsRadiusX: 0,
            boundsRadiusY: 0,
            noOp: amount == 0) with
        {
            Options0 = new Vector4(
                RadialMode(values, "Mode"),
                amount,
                center.X,
                center.Y),
            Options1 = new Vector4(
                Quality(values, "Quality"),
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
                EdgeMode(values, "EdgeMode"),
                0,
                0),
            Resource = values.Resource("Kernel"),
            ResourceRequired = true
        };
    }

    private static PrismNeighborhoodPlan EdgeAwarePlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float deviceScale,
        bool hasMode)
    {
        float radius = values.Number("Radius") * deviceScale;
        return Plan(
            filter,
            operation,
            blendMode,
            radius,
            radius,
            Quality(values, "Quality"),
            boundsRadiusX: 0,
            boundsRadiusY: 0,
            noOp: radius == 0) with
        {
            Options0 = new Vector4(
                radius,
                values.Number("Threshold"),
                Quality(values, "Quality"),
                hasMode ? SmartBlurMode(values, "Mode") : 0)
        };
    }

    private static PrismNeighborhoodPlan FieldPlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values)
    {
        return Plan(
            filter,
            operation,
            blendMode,
            radiusX: 1,
            radiusY: 1,
            sampleCount: BestSamples,
            boundsRadiusX: 0,
            boundsRadiusY: 0,
            noOp: false) with
        {
            Options0 = new Vector4(
                values.Number("BokehAmount"),
                values.Number("BokehColor"),
                values.Number("Noise"),
                0),
            Options1 = values.Vector("LightRange"),
            Resource = values.Resource("Pins"),
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
                values.Number("BokehAmount")),
            Options2 = new Vector4(
                values.Number("BokehColor"),
                values.Number("Noise"),
                0,
                0),
            Options3 = values.Vector("LightRange")
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
        float distortion = values.Number("Distortion");
        float noise = values.Number("Noise");
        return Plan(
            filter,
            operation,
            blendMode,
            blur,
            blur,
            BestSamples,
            boundsRadiusX: 0,
            boundsRadiusY: 0,
            noOp: blur == 0 && distortion == 0 && noise == 0) with
        {
            Options0 = new Vector4(
                center.X,
                center.Y,
                Degrees(values.Number("Angle")),
                values.Number("FocusWidth")),
            Options1 = new Vector4(
                values.Number("Feather"),
                blur,
                distortion,
                values.Boolean("SymmetricDistortion") ? 1 : 0),
            Options2 = new Vector4(noise, 0, 0, 0)
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
            boundsRadiusX: 0,
            boundsRadiusY: 0,
            noOp: speed == 0 && endSpeed == 0 && noise == 0) with
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
        PrismFilterParameterReader values)
    {
        Vector4 center = values.Vector("Center");
        Vector4 radius = values.Vector("Radius");
        float rotation = Degrees(values.Number("Rotation"));
        float noise = values.Number("Noise");
        return Plan(
            filter,
            operation,
            blendMode,
            MathF.Abs(rotation),
            MathF.Abs(rotation),
            BestSamples,
            boundsRadiusX: 0,
            boundsRadiusY: 0,
            noOp: rotation == 0 && noise == 0) with
        {
            Options0 = new Vector4(
                center.X,
                center.Y,
                radius.X,
                radius.Y),
            Options1 = new Vector4(
                rotation,
                values.Number("Feather"),
                values.Number("StrobeStrength"),
                values.Number("StrobeFlashes")),
            Options2 = new Vector4(
                values.Number("StrobeDuration"),
                noise,
                0,
                0)
        };
    }

    private static PrismNeighborhoodPlan UnsharpPlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float amount = values.Number("Amount");
        float radius = values.Number("Radius") * deviceScale;
        return PointPlan(
            filter,
            operation,
            blendMode,
            new Vector4(
                amount,
                radius,
                values.Number("Threshold"),
                0),
            radius,
            noOp: amount == 0 || radius == 0);
    }

    private static PrismNeighborhoodPlan SmartSharpenPlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float amount = values.Number("Amount");
        float radius = values.Number("Radius") * deviceScale;
        float reduceNoise = values.Number("ReduceNoise");
        return PointPlan(
            filter,
            operation,
            blendMode,
            new Vector4(
                amount,
                radius,
                reduceNoise,
                SmartSharpenRemove(values, "Remove")),
            radius,
            noOp: amount == 0 && reduceNoise == 0) with
        {
            Options1 = new Vector4(
                Degrees(values.Number("Angle")),
                values.Number("ShadowFade"),
                values.Number("ShadowTonalWidth"),
                values.Number("ShadowRadius") * deviceScale),
            Options2 = new Vector4(
                values.Number("HighlightFade"),
                values.Number("HighlightTonalWidth"),
                values.Number("HighlightRadius") * deviceScale,
                0)
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

    private static PrismNeighborhoodPlan ReduceNoisePlan(
        PrismFilterId filter,
        PrismNeighborhoodOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values)
    {
        float strength = values.Number("Strength");
        float colorNoise = values.Number("ReduceColorNoise");
        float sharpen = values.Number("SharpenDetails");
        return PointPlan(
            filter,
            operation,
            blendMode,
            new Vector4(
                strength,
                values.Number("PreserveDetails"),
                colorNoise,
                sharpen),
            radius: 1,
            noOp:
                strength == 0 &&
                colorNoise == 0 &&
                sharpen == 0) with
        {
            Options1 = new Vector4(
                values.Boolean("RemoveJpegArtifact") ? 1 : 0,
                0,
                0,
                0)
        };
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
