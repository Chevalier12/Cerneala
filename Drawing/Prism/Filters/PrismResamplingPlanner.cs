using System.Collections.Immutable;
using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Filters;

internal enum PrismResamplingOperation
{
    Transform,
    AdaptiveWideAngle,
    LensCorrection,
    DiffuseGlow,
    Displace,
    Glass,
    OceanRipple,
    Pinch,
    PolarCoordinates,
    Ripple,
    Shear,
    Spherize,
    Twirl,
    Wave,
    ZigZag,
    Liquify,
    Offset
}

internal enum PrismResamplingPassKind
{
    Direct,
    Diffuse,
    Grain
}

internal readonly record struct PrismResamplingPass(
    PrismResamplingPassKind Kind,
    bool IsNoOp);

internal readonly record struct PrismResamplingPlan
{
    public PrismResamplingPlan(
        PrismFilterId filter,
        PrismResamplingOperation operation,
        PrismBlendMode blendMode,
        ImmutableArray<PrismResamplingPass> passes)
    {
        this = default;
        Filter = filter;
        Operation = operation;
        BlendMode = blendMode;
        Passes = passes;
    }

    public PrismFilterId Filter { get; init; }

    public PrismResamplingOperation Operation { get; init; }

    public PrismBlendMode BlendMode { get; init; }

    public ImmutableArray<PrismResamplingPass> Passes { get; init; }

    public Vector4 Options0 { get; init; }

    public Vector4 Options1 { get; init; }

    public Vector4 Options2 { get; init; }

    public Vector4 Options3 { get; init; }

    public Vector4 Options4 { get; init; }

    public Vector4 Options5 { get; init; }

    public PrismResourceId PrimaryResource { get; init; }

    public bool PrimaryResourceRequired { get; init; }

    public PrismResourceId AuxiliaryResource { get; init; }

    public bool AuxiliaryResourceRequired { get; init; }

    public bool TransformsBounds { get; init; }

    public Vector2 BoundsTranslation { get; init; }

    public Vector2 BoundsScale { get; init; }

    public float BoundsRotation { get; init; }

    public Vector2 BoundsSkew { get; init; }

    public Vector2 BoundsOrigin { get; init; }
}

internal static class PrismResamplingPlanner
{
    private const string ResamplingOwnerPrefix =
        "PrismKernelRegistry/";

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
                ResamplingOwnerPrefix + entry.Symbol,
                StringComparison.Ordinal);
    }

    public static PrismResamplingPlan Create(
        PrismFilterId filter,
        ImmutableArray<PrismGraphParameter> parameters,
        PrismBlendMode blendMode,
        float pixelScale,
        Matrix3x2 effectiveTransform,
        DrawRect sourceBounds)
    {
        if (!IsSupported(filter) ||
            !TryGetOperation(
                filter,
                out PrismResamplingOperation operation))
        {
            throw new InvalidOperationException(
                $"Filter '{filter}' has no resampling planner.");
        }
        if (!float.IsFinite(pixelScale) || pixelScale <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pixelScale),
                pixelScale,
                "Resampling planning requires a finite positive pixel scale.");
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

        Vector2 sourceSize = new(
            MathF.Max(1, sourceBounds.Width * deviceScale),
            MathF.Max(1, sourceBounds.Height * deviceScale));
        PrismFilterParameterReader values =
            new(filter, parameters);
        return operation switch
        {
            PrismResamplingOperation.Transform =>
                TransformPlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    deviceScale,
                    transformScale,
                    sourceSize),
            PrismResamplingOperation.AdaptiveWideAngle =>
                AdaptiveWideAnglePlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    deviceScale),
            PrismResamplingOperation.LensCorrection =>
                LensCorrectionPlan(
                    filter,
                    operation,
                    blendMode,
                    values),
            PrismResamplingOperation.DiffuseGlow =>
                DiffuseGlowPlan(
                    filter,
                    operation,
                    blendMode,
                    values),
            PrismResamplingOperation.Displace =>
                DisplacePlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    deviceScale),
            PrismResamplingOperation.Glass =>
                GlassPlan(
                    filter,
                    operation,
                    blendMode,
                    values),
            PrismResamplingOperation.OceanRipple =>
                OceanRipplePlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    deviceScale),
            PrismResamplingOperation.Pinch =>
                PointPlan(
                    filter,
                    operation,
                    blendMode,
                    new Vector4(
                        values.Number("Amount"),
                        values.Vector("Center").X,
                        values.Vector("Center").Y,
                        0),
                    noOp: values.Number("Amount") == 0),
            PrismResamplingOperation.PolarCoordinates =>
                PointPlan(
                    filter,
                    operation,
                    blendMode,
                    new Vector4(
                        PolarMode(values, "Mode"),
                        values.Vector("Center").X,
                        values.Vector("Center").Y,
                        0),
                    noOp: false),
            PrismResamplingOperation.Ripple =>
                RipplePlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    deviceScale),
            PrismResamplingOperation.Shear =>
                PointPlan(
                    filter,
                    operation,
                    blendMode,
                    new Vector4(
                        ShearCurve(values, "Curve"),
                        UndefinedAreas(
                            values,
                            "UndefinedAreas"),
                        0,
                        0),
                    noOp:
                        ShearCurve(values, "Curve") == 0),
            PrismResamplingOperation.Spherize =>
                PointPlan(
                    filter,
                    operation,
                    blendMode,
                    new Vector4(
                        values.Number("Amount"),
                        SpherizeMode(values, "Mode"),
                        values.Vector("Center").X,
                        values.Vector("Center").Y),
                    noOp: values.Number("Amount") == 0),
            PrismResamplingOperation.Twirl =>
                PointPlan(
                    filter,
                    operation,
                    blendMode,
                    new Vector4(
                        Degrees(values.Number("Angle")),
                        values.Vector("Center").X,
                        values.Vector("Center").Y,
                        0),
                    noOp: values.Number("Angle") == 0),
            PrismResamplingOperation.Wave =>
                WavePlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    deviceScale),
            PrismResamplingOperation.ZigZag =>
                ZigZagPlan(
                    filter,
                    operation,
                    blendMode,
                    values),
            PrismResamplingOperation.Liquify =>
                LiquifyPlan(
                    filter,
                    operation,
                    blendMode,
                    values),
            PrismResamplingOperation.Offset =>
                OffsetPlan(
                    filter,
                    operation,
                    blendMode,
                    values,
                    deviceScale),
            _ => throw new InvalidOperationException(
                $"Resampling operation '{operation}' has no planner.")
        };
    }

    private static PrismResamplingPlan TransformPlan(
        PrismFilterId filter,
        PrismResamplingOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float deviceScale,
        float logicalScale,
        Vector2 sourceSize)
    {
        Vector4 translate = values.Vector("Translate");
        Vector4 scale = values.Vector("Scale");
        Vector4 skew = values.Vector("Skew");
        Vector4 origin = values.Vector("Origin");
        float rotation = Degrees(values.Number("Rotation"));
        Vector2 deviceTranslation =
            new(translate.X, translate.Y);
        deviceTranslation *= deviceScale;
        Vector2 boundsTranslation =
            new(translate.X, translate.Y);
        boundsTranslation *= logicalScale;
        Vector2 scale2 = new(scale.X, scale.Y);
        Vector2 skew2 = new(
            MathF.Tan(Degrees(skew.X)),
            MathF.Tan(Degrees(skew.Y)));
        Vector2 origin2 = new(origin.X, origin.Y);
        bool noOp =
            deviceTranslation == Vector2.Zero &&
            scale2 == Vector2.One &&
            rotation == 0 &&
            skew2 == Vector2.Zero;
        return PointPlan(
            filter,
            operation,
            blendMode,
            new Vector4(
                deviceTranslation,
                scale2.X,
                scale2.Y),
            noOp) with
        {
            Options1 = new Vector4(
                rotation,
                skew2.X,
                skew2.Y,
                Sampling(values, "Sampling")),
            Options2 = new Vector4(
                origin2,
                EdgeMode(values, "EdgeMode"),
                0),
            Options3 = new Vector4(sourceSize, 0, 0),
            TransformsBounds = true,
            BoundsTranslation = boundsTranslation,
            BoundsScale = scale2,
            BoundsRotation = rotation,
            BoundsSkew = skew2,
            BoundsOrigin = origin2
        };
    }

    private static PrismResamplingPlan AdaptiveWideAnglePlan(
        PrismFilterId filter,
        PrismResamplingOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float deviceScale)
    {
        Vector4 translate = values.Vector("Translate");
        return PointPlan(
            filter,
            operation,
            blendMode,
            new Vector4(
                Projection(values, "Projection"),
                FocalLength(values, "FocalLength"),
                values.Number("CropFactor"),
                values.Number("Scale")),
            noOp: false) with
        {
            Options1 = new Vector4(
                Degrees(values.Number("Rotation")),
                translate.X * deviceScale,
                translate.Y * deviceScale,
                0),
            PrimaryResource =
                values.Resource("Constraints"),
            PrimaryResourceRequired = true
        };
    }

    private static PrismResamplingPlan LensCorrectionPlan(
        PrismFilterId filter,
        PrismResamplingOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values)
    {
        float distortion = values.Number("Distortion");
        float redCyan = values.Number("ChromaticRedCyan");
        float blueYellow =
            values.Number("ChromaticBlueYellow");
        float vignette = values.Number("VignetteAmount");
        float vertical =
            values.Number("PerspectiveVertical");
        float horizontal =
            values.Number("PerspectiveHorizontal");
        float angle = Degrees(values.Number("Angle"));
        float scale = values.Number("Scale");
        return PointPlan(
            filter,
            operation,
            blendMode,
            new Vector4(
                distortion,
                redCyan,
                blueYellow,
                vignette),
            noOp:
                distortion == 0 &&
                redCyan == 0 &&
                blueYellow == 0 &&
                vignette == 0 &&
                vertical == 0 &&
                horizontal == 0 &&
                angle == 0 &&
                scale == 1) with
        {
            Options1 = new Vector4(
                values.Number("VignetteMidpoint"),
                vertical,
                horizontal,
                angle),
            Options2 = new Vector4(
                scale,
                EdgeMode(values, "EdgeMode"),
                0,
                0)
        };
    }

    private static PrismResamplingPlan DiffuseGlowPlan(
        PrismFilterId filter,
        PrismResamplingOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values)
    {
        float grain = values.Number("Grain");
        float glow = values.Number("GlowAmount");
        float clear = values.Number("ClearAmount");
        return new PrismResamplingPlan(
            filter,
            operation,
            blendMode,
            [
                new PrismResamplingPass(
                    PrismResamplingPassKind.Diffuse,
                    glow == 0),
                new PrismResamplingPass(
                    PrismResamplingPassKind.Grain,
                    grain == 0 && clear == 0)
            ])
        {
            Options0 = new Vector4(
                grain,
                glow,
                clear,
                0),
            Options1 = values.Color("Color")
        };
    }

    private static PrismResamplingPlan DisplacePlan(
        PrismFilterId filter,
        PrismResamplingOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float deviceScale)
    {
        float horizontal =
            values.Number("HorizontalScale") * deviceScale;
        float vertical =
            values.Number("VerticalScale") * deviceScale;
        return PointPlan(
            filter,
            operation,
            blendMode,
            new Vector4(
                horizontal,
                vertical,
                MapFit(values, "MapFit"),
                UndefinedAreas(
                    values,
                    "UndefinedAreas")),
            noOp: horizontal == 0 && vertical == 0) with
        {
            Options1 = new Vector4(
                Channel(values, "ChannelX"),
                Channel(values, "ChannelY"),
                0,
                0),
            PrimaryResource = values.Resource("Map"),
            PrimaryResourceRequired = true
        };
    }

    private static PrismResamplingPlan GlassPlan(
        PrismFilterId filter,
        PrismResamplingOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values)
    {
        float distortion = values.Number("Distortion");
        return PointPlan(
            filter,
            operation,
            blendMode,
            new Vector4(
                distortion,
                values.Number("Smoothness"),
                GlassTexture(values, "Texture"),
                values.Number("Scaling")),
            noOp: distortion == 0) with
        {
            Options1 = new Vector4(
                values.Boolean("Invert") ? 1 : 0,
                0,
                0,
                0),
            PrimaryResource =
                values.Resource("TextureImage")
        };
    }

    private static PrismResamplingPlan OceanRipplePlan(
        PrismFilterId filter,
        PrismResamplingOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float deviceScale)
    {
        int seed = values.Integer("Seed");
        float magnitude = values.Number("RippleMagnitude");
        return PointPlan(
            filter,
            operation,
            blendMode,
            new Vector4(
                values.Number("RippleSize") * deviceScale,
                magnitude,
                seed & 0xffff,
                (seed >> 16) & 0xffff),
            noOp: magnitude == 0);
    }

    private static PrismResamplingPlan RipplePlan(
        PrismFilterId filter,
        PrismResamplingOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float deviceScale)
    {
        int seed = values.Integer("Seed");
        float amount = values.Number("Amount");
        return PointPlan(
            filter,
            operation,
            blendMode,
            new Vector4(
                amount * deviceScale,
                RippleSize(values, "Size"),
                seed & 0xffff,
                (seed >> 16) & 0xffff),
            noOp: amount == 0) with
        {
            Options1 = new Vector4(
                EdgeMode(values, "EdgeMode"),
                0,
                0,
                0)
        };
    }

    private static PrismResamplingPlan WavePlan(
        PrismFilterId filter,
        PrismResamplingOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float deviceScale)
    {
        Vector4 wavelength = values.Vector("Wavelength");
        Vector4 amplitude = values.Vector("Amplitude");
        Vector4 scale = values.Vector("Scale");
        int seed = values.Integer("Seed");
        float generators = values.Number("Generators");
        return PointPlan(
            filter,
            operation,
            blendMode,
            new Vector4(
                generators,
                wavelength.X * deviceScale,
                wavelength.Y * deviceScale,
                WaveType(values, "Type")),
            noOp:
                generators == 0 ||
                (amplitude.X == 0 && amplitude.Y == 0)) with
        {
            Options1 = new Vector4(
                amplitude.X * deviceScale,
                amplitude.Y * deviceScale,
                scale.X,
                scale.Y),
            Options2 = new Vector4(
                UndefinedAreas(
                    values,
                    "UndefinedAreas"),
                seed & 0xffff,
                (seed >> 16) & 0xffff,
                0)
        };
    }

    private static PrismResamplingPlan ZigZagPlan(
        PrismFilterId filter,
        PrismResamplingOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values)
    {
        float amount = values.Number("Amount");
        float ridges = values.Number("Ridges");
        Vector4 center = values.Vector("Center");
        return PointPlan(
            filter,
            operation,
            blendMode,
            new Vector4(
                amount,
                ridges,
                ZigZagStyle(values, "Style"),
                0),
            noOp: amount == 0 || ridges == 0) with
        {
            Options1 = new Vector4(
                center.X,
                center.Y,
                0,
                0)
        };
    }

    private static PrismResamplingPlan LiquifyPlan(
        PrismFilterId filter,
        PrismResamplingOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values)
    {
        return PointPlan(
            filter,
            operation,
            blendMode,
            new Vector4(
                values.Number("Reconstruct"),
                values.Boolean("MaskInvert") ? 1 : 0,
                EdgeMode(values, "EdgeMode"),
                0),
            noOp: false) with
        {
            PrimaryResource = values.Resource("Mesh"),
            PrimaryResourceRequired = true,
            AuxiliaryResource = values.Resource("Mask")
        };
    }

    private static PrismResamplingPlan OffsetPlan(
        PrismFilterId filter,
        PrismResamplingOperation operation,
        PrismBlendMode blendMode,
        PrismFilterParameterReader values,
        float deviceScale)
    {
        Vector4 offset = values.Vector("Offset");
        Vector2 deviceOffset =
            new(offset.X, offset.Y);
        deviceOffset *= deviceScale;
        return PointPlan(
            filter,
            operation,
            blendMode,
            new Vector4(
                deviceOffset,
                UndefinedAreas(
                    values,
                    "UndefinedAreas"),
                0),
            noOp: deviceOffset == Vector2.Zero) with
        {
            Options1 = values.Color("FillColor")
        };
    }

    private static PrismResamplingPlan PointPlan(
        PrismFilterId filter,
        PrismResamplingOperation operation,
        PrismBlendMode blendMode,
        Vector4 options0,
        bool noOp)
    {
        return new PrismResamplingPlan(
            filter,
            operation,
            blendMode,
            [
                new PrismResamplingPass(
                    PrismResamplingPassKind.Direct,
                    noOp)
            ])
        {
            Options0 = options0
        };
    }

    private static int Sampling(
        PrismFilterParameterReader values,
        string name) =>
        values.SymbolCode(name, ("Linear", 0));

    private static int EdgeMode(
        PrismFilterParameterReader values,
        string name) =>
        values.SymbolCode(
            name,
            ("Clamp", 0),
            ("Transparent", 1),
            ("Wrap", 2),
            ("Repeat", 2),
            ("Mirror", 3),
            ("Reflect", 3));

    private static int UndefinedAreas(
        PrismFilterParameterReader values,
        string name) =>
        values.SymbolCode(
            name,
            ("RepeatEdgePixels", 0),
            ("WrapAround", 2),
            ("SetToBackground", 4),
            ("Transparent", 1));

    private static int Projection(
        PrismFilterParameterReader values,
        string name) =>
        values.SymbolCode(
            name,
            ("Auto", 0),
            ("Fisheye", 1),
            ("Perspective", 2),
            ("FullSpherical", 3));

    private static int FocalLength(
        PrismFilterParameterReader values,
        string name) =>
        values.SymbolCode(
            name,
            ("Auto", 0),
            ("Measured", 1));

    private static int MapFit(
        PrismFilterParameterReader values,
        string name) =>
        values.SymbolCode(
            name,
            ("Stretch", 0),
            ("Tile", 1));

    private static int Channel(
        PrismFilterParameterReader values,
        string name) =>
        values.SymbolCode(
            name,
            ("Red", 0),
            ("Green", 1),
            ("Blue", 2),
            ("Alpha", 3),
            ("Luminance", 4));

    private static int GlassTexture(
        PrismFilterParameterReader values,
        string name) =>
        values.SymbolCode(
            name,
            ("Frosted", 0),
            ("TinyLens", 1),
            ("Blocks", 2),
            ("Canvas", 3),
            ("TextureImage", 4));

    private static int PolarMode(
        PrismFilterParameterReader values,
        string name) =>
        values.SymbolCode(
            name,
            ("RectangularToPolar", 0),
            ("PolarToRectangular", 1));

    private static int RippleSize(
        PrismFilterParameterReader values,
        string name) =>
        values.SymbolCode(
            name,
            ("Small", 0),
            ("Medium", 1),
            ("Large", 2));

    private static int ShearCurve(
        PrismFilterParameterReader values,
        string name) =>
        values.SymbolCode(
            name,
            ("Linear", 0),
            ("EaseIn", 1),
            ("EaseOut", 2),
            ("EaseInOut", 3),
            ("SCurve", 4));

    private static int SpherizeMode(
        PrismFilterParameterReader values,
        string name) =>
        values.SymbolCode(
            name,
            ("Normal", 0),
            ("HorizontalOnly", 1),
            ("VerticalOnly", 2));

    private static int WaveType(
        PrismFilterParameterReader values,
        string name) =>
        values.SymbolCode(
            name,
            ("Sine", 0),
            ("Triangle", 1),
            ("Square", 2));

    private static int ZigZagStyle(
        PrismFilterParameterReader values,
        string name) =>
        values.SymbolCode(
            name,
            ("PondRipples", 0),
            ("OutFromCenter", 1),
            ("AroundCenter", 2));

    private static float Degrees(float value) =>
        value * (MathF.PI / 180f);

    private static bool TryGetOperation(
        PrismFilterId filter,
        out PrismResamplingOperation operation)
    {
        operation = filter switch
        {
            PrismFilterId.Transform =>
                PrismResamplingOperation.Transform,
            PrismFilterId.AdaptiveWideAngle =>
                PrismResamplingOperation.AdaptiveWideAngle,
            PrismFilterId.LensCorrection =>
                PrismResamplingOperation.LensCorrection,
            PrismFilterId.DiffuseGlow =>
                PrismResamplingOperation.DiffuseGlow,
            PrismFilterId.Displace =>
                PrismResamplingOperation.Displace,
            PrismFilterId.Glass =>
                PrismResamplingOperation.Glass,
            PrismFilterId.OceanRipple =>
                PrismResamplingOperation.OceanRipple,
            PrismFilterId.Pinch =>
                PrismResamplingOperation.Pinch,
            PrismFilterId.PolarCoordinates =>
                PrismResamplingOperation.PolarCoordinates,
            PrismFilterId.Ripple =>
                PrismResamplingOperation.Ripple,
            PrismFilterId.Shear =>
                PrismResamplingOperation.Shear,
            PrismFilterId.Spherize =>
                PrismResamplingOperation.Spherize,
            PrismFilterId.Twirl =>
                PrismResamplingOperation.Twirl,
            PrismFilterId.Wave =>
                PrismResamplingOperation.Wave,
            PrismFilterId.ZigZag =>
                PrismResamplingOperation.ZigZag,
            PrismFilterId.Liquify =>
                PrismResamplingOperation.Liquify,
            PrismFilterId.Offset =>
                PrismResamplingOperation.Offset,
            _ => (PrismResamplingOperation)(-1)
        };
        return (int)operation >= 0;
    }
}
