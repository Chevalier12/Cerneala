using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Filters;

internal enum PrismAdjustmentOperation
{
    BrightnessContrast,
    Levels,
    Curves,
    Exposure,
    Vibrance,
    HueSaturation,
    ColorBalance,
    BlackWhite,
    PhotoFilter,
    ChannelMixer,
    ColorLookup,
    Invert,
    Posterize,
    Threshold,
    GradientMap,
    SelectiveColor
}

internal readonly record struct PrismAdjustmentPlan
{
    public PrismAdjustmentPlan(
        PrismFilterId filter,
        PrismAdjustmentOperation operation,
        PrismBlendMode blendMode)
    {
        this = default;
        Filter = filter;
        Operation = operation;
        BlendMode = blendMode;
    }

    public PrismFilterId Filter { get; init; }

    public PrismAdjustmentOperation Operation { get; init; }

    public PrismBlendMode BlendMode { get; init; }

    public Vector4 Parameters0 { get; init; }

    public Vector4 Parameters1 { get; init; }

    public Vector4 Parameters2 { get; init; }

    public Vector4 Parameters3 { get; init; }

    public Vector4 Parameters4 { get; init; }

    public Vector4 Parameters5 { get; init; }

    public Vector4 Parameters6 { get; init; }

    public Vector4 Parameters7 { get; init; }

    public Vector4 Parameters8 { get; init; }

    public Vector4 Parameters9 { get; init; }

    public PrismResourceId Resource { get; init; }

    public bool ResourceRequired { get; init; }
}

internal static class PrismAdjustmentPlanner
{
    private const string AdjustmentCategory =
        "color-and-adjustment";
    private const string AdjustmentPrimitive =
        "matrix-curve-lut";

    public static bool IsSupported(PrismFilterId filter)
    {
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)filter);
        return entry.Kind == "filter" &&
            entry.Category == AdjustmentCategory &&
            entry.Execution is PrismCatalogExecutionDescriptor execution &&
            execution.Primitive == AdjustmentPrimitive;
    }

    public static PrismAdjustmentPlan Create(
        PrismGraphNode node,
        PrismGraphScope scope)
    {
        if (node.Filter is not PrismFilterId filter ||
            !IsSupported(filter))
        {
            throw new InvalidOperationException(
                $"Graph node '{node.Id}' is not a supported adjustment filter.");
        }

        PrismFilterParameterReader values =
            new(filter, node.Parameters);
        PrismBlendMode blendMode =
            node.BlendMode ?? PrismBlendMode.Normal;
        return filter switch
        {
            PrismFilterId.BrightnessContrast => new(
                filter,
                PrismAdjustmentOperation.BrightnessContrast,
                blendMode)
            {
                Parameters0 = new Vector4(
                    values.Number("Brightness"),
                    values.Number("Contrast"),
                    values.Boolean("UseLegacy") ? 1 : 0,
                    0)
            },
            PrismFilterId.Levels => new(
                filter,
                PrismAdjustmentOperation.Levels,
                blendMode)
            {
                Parameters0 = new Vector4(
                    values.SymbolCode(
                        "Channel",
                        ("Composite", 0),
                        ("Red", 1),
                        ("Green", 2),
                        ("Blue", 3)),
                    values.Number("InputBlack"),
                    values.Number("InputWhite"),
                    values.Number("Gamma")),
                Parameters1 = new Vector4(
                    values.Number("OutputBlack"),
                    values.Number("OutputWhite"),
                    0,
                    0)
            },
            PrismFilterId.Curves => new(
                filter,
                PrismAdjustmentOperation.Curves,
                blendMode)
            {
                Parameters0 = new Vector4(
                    values.SymbolCode(
                        "Channel",
                        ("Composite", 0),
                        ("Red", 1),
                        ("Green", 2),
                        ("Blue", 3)),
                    values.SymbolCode(
                        "Curve",
                        ("Linear", 0),
                        ("Lighten", 1),
                        ("Darken", 2),
                        ("Contrast", 3)),
                    values.SymbolCode(
                        "Interpolation",
                        ("Smooth", 0),
                        ("Tetrahedral", 0),
                        ("Linear", 1),
                        ("Trilinear", 1)),
                    0)
            },
            PrismFilterId.Exposure => new(
                filter,
                PrismAdjustmentOperation.Exposure,
                blendMode)
            {
                Parameters0 = new Vector4(
                    values.Number("Exposure"),
                    values.Number("Offset"),
                    values.Number("Gamma"),
                    0)
            },
            PrismFilterId.Vibrance => new(
                filter,
                PrismAdjustmentOperation.Vibrance,
                blendMode)
            {
                Parameters0 = new Vector4(
                    values.Number("Amount"),
                    values.Number("Saturation"),
                    0,
                    0)
            },
            PrismFilterId.HueSaturation => new(
                filter,
                PrismAdjustmentOperation.HueSaturation,
                blendMode)
            {
                Parameters0 = new Vector4(
                    values.SymbolCode(
                        "Channel",
                        ("Master", 0),
                        ("Reds", 1),
                        ("Yellows", 2),
                        ("Greens", 3),
                        ("Cyans", 4),
                        ("Blues", 5),
                        ("Magentas", 6)),
                    values.Number("Hue"),
                    values.Number("Saturation"),
                    values.Number("Lightness")),
                Parameters1 = new Vector4(
                    values.Boolean("Colorize") ? 1 : 0,
                    0,
                    0,
                    0)
            },
            PrismFilterId.ColorBalance => new(
                filter,
                PrismAdjustmentOperation.ColorBalance,
                blendMode)
            {
                Parameters0 = values.Vector("Shadows"),
                Parameters1 = values.Vector("Midtones"),
                Parameters2 = values.Vector("Highlights"),
                Parameters3 = new Vector4(
                    values.Boolean("PreserveLuminosity") ? 1 : 0,
                    0,
                    0,
                    0)
            },
            PrismFilterId.BlackWhite => new(
                filter,
                PrismAdjustmentOperation.BlackWhite,
                blendMode)
            {
                Parameters0 = new Vector4(
                    values.Number("Reds"),
                    values.Number("Yellows"),
                    values.Number("Greens"),
                    values.Number("Cyans")),
                Parameters1 = new Vector4(
                    values.Number("Blues"),
                    values.Number("Magentas"),
                    values.Boolean("Tint") ? 1 : 0,
                    0),
                Parameters2 = values.Color("TintColor")
            },
            PrismFilterId.PhotoFilter => new(
                filter,
                PrismAdjustmentOperation.PhotoFilter,
                blendMode)
            {
                Parameters0 = values.Color("Color"),
                Parameters1 = new Vector4(
                    values.Number("Density"),
                    values.Boolean("PreserveLuminosity") ? 1 : 0,
                    0,
                    0)
            },
            PrismFilterId.ChannelMixer => new(
                filter,
                PrismAdjustmentOperation.ChannelMixer,
                blendMode)
            {
                Parameters0 = values.Vector("Red"),
                Parameters1 = values.Vector("Green"),
                Parameters2 = values.Vector("Blue"),
                Parameters3 = values.Vector("Constant"),
                Parameters4 = new Vector4(
                    values.Boolean("Monochrome") ? 1 : 0,
                    0,
                    0,
                    0)
            },
            PrismFilterId.ColorLookup => new(
                filter,
                PrismAdjustmentOperation.ColorLookup,
                blendMode)
            {
                Parameters0 = new Vector4(
                    values.Number("Intensity"),
                    values.SymbolCode(
                        "Interpolation",
                        ("Smooth", 0),
                        ("Tetrahedral", 0),
                        ("Linear", 1),
                        ("Trilinear", 1)),
                    0,
                    0),
                Resource = values.Resource("Lookup"),
                ResourceRequired = true
            },
            PrismFilterId.Invert => new(
                filter,
                PrismAdjustmentOperation.Invert,
                blendMode),
            PrismFilterId.Posterize => new(
                filter,
                PrismAdjustmentOperation.Posterize,
                blendMode)
            {
                Parameters0 = new Vector4(
                    values.Number("Levels"),
                    0,
                    0,
                    0)
            },
            PrismFilterId.Threshold => new(
                filter,
                PrismAdjustmentOperation.Threshold,
                blendMode)
            {
                Parameters0 = new Vector4(
                    values.Number("Level"),
                    0,
                    0,
                    0)
            },
            PrismFilterId.GradientMap => new(
                filter,
                PrismAdjustmentOperation.GradientMap,
                blendMode)
            {
                Parameters0 = new Vector4(
                    values.SymbolCode(
                        "Gradient",
                        ("BlackToWhite", 0),
                        ("BlueRedYellow", 1)),
                    values.Boolean("Reverse") ? 1 : 0,
                    values.Boolean("Dither") ? 1 : 0,
                    values.SymbolCode(
                        "Method",
                        ("Perceptual", 0),
                        ("Relative", 0),
                        ("Absolute", 1)))
            },
            PrismFilterId.SelectiveColor => new(
                filter,
                PrismAdjustmentOperation.SelectiveColor,
                blendMode)
            {
                Parameters0 = values.Vector("Reds"),
                Parameters1 = values.Vector("Yellows"),
                Parameters2 = values.Vector("Greens"),
                Parameters3 = values.Vector("Cyans"),
                Parameters4 = values.Vector("Blues"),
                Parameters5 = values.Vector("Magentas"),
                Parameters6 = values.Vector("Whites"),
                Parameters7 = values.Vector("Neutrals"),
                Parameters8 = values.Vector("Blacks"),
                Parameters9 = new Vector4(
                    values.SymbolCode(
                        "Method",
                        ("Perceptual", 0),
                        ("Relative", 0),
                        ("Absolute", 1)),
                    0,
                    0,
                    0)
            },
            _ => throw new InvalidOperationException(
                $"Adjustment filter '{filter}' has no planner.")
        };
    }

    public static bool IsNoOp(PrismAdjustmentPlan plan)
    {
        return plan.Operation switch
        {
            PrismAdjustmentOperation.BrightnessContrast =>
                plan.Parameters0.X == 0 &&
                plan.Parameters0.Y == 0,
            PrismAdjustmentOperation.Levels =>
                plan.Parameters0.Y == 0 &&
                plan.Parameters0.Z == 1 &&
                plan.Parameters0.W == 1 &&
                plan.Parameters1.X == 0 &&
                plan.Parameters1.Y == 1,
            PrismAdjustmentOperation.Curves =>
                plan.Parameters0.Y == 0,
            PrismAdjustmentOperation.Exposure =>
                plan.Parameters0.X == 0 &&
                plan.Parameters0.Y == 0 &&
                plan.Parameters0.Z == 1,
            PrismAdjustmentOperation.Vibrance =>
                plan.Parameters0.X == 0 &&
                plan.Parameters0.Y == 0,
            PrismAdjustmentOperation.HueSaturation =>
                plan.Parameters0.Y == 0 &&
                plan.Parameters0.Z == 0 &&
                plan.Parameters0.W == 0 &&
                plan.Parameters1.X == 0,
            PrismAdjustmentOperation.ColorBalance =>
                IsZero(plan.Parameters0) &&
                IsZero(plan.Parameters1) &&
                IsZero(plan.Parameters2),
            PrismAdjustmentOperation.PhotoFilter =>
                plan.Parameters1.X == 0,
            PrismAdjustmentOperation.ChannelMixer =>
                plan.Parameters0 == Vector4.UnitX &&
                plan.Parameters1 == Vector4.UnitY &&
                plan.Parameters2 == Vector4.UnitZ &&
                IsZero(plan.Parameters3) &&
                plan.Parameters4.X == 0,
            PrismAdjustmentOperation.SelectiveColor =>
                IsZero(plan.Parameters0) &&
                IsZero(plan.Parameters1) &&
                IsZero(plan.Parameters2) &&
                IsZero(plan.Parameters3) &&
                IsZero(plan.Parameters4) &&
                IsZero(plan.Parameters5) &&
                IsZero(plan.Parameters6) &&
                IsZero(plan.Parameters7) &&
                IsZero(plan.Parameters8),
            _ => false
        };
    }

    private static bool IsZero(Vector4 value) =>
        value == Vector4.Zero;
}
