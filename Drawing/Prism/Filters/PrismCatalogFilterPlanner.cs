using System.Collections.Immutable;
using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Filters;

internal enum PrismCatalogFilterPrimitive
{
    Morphology,
    Quantization,
    Procedural,
    Video,
    Artistic,
    EdgeDetection,
    Tiling,
    Texture,
    Convolution,
    Color,
    Extrude,
    LineIntegralConvolution
}

internal enum PrismCatalogFilterPassKind
{
    Direct,
    Horizontal,
    Vertical,
    Iteration
}

internal readonly record struct PrismCatalogFilterPass(
    PrismCatalogFilterPassKind Kind,
    float RadiusX,
    float RadiusY,
    float BoundsRadiusX,
    float BoundsRadiusY,
    int Iteration,
    bool IsNoOp);

internal readonly record struct PrismCatalogFilterPlan
{
    public PrismCatalogFilterPlan(
        PrismFilterId filter,
        PrismCatalogFilterPrimitive primitive,
        PrismBlendMode blendMode,
        ImmutableArray<PrismCatalogFilterPass> passes)
    {
        this = default;
        Filter = filter;
        Primitive = primitive;
        BlendMode = blendMode;
        Passes = passes;
    }

    public PrismFilterId Filter { get; init; }

    public PrismCatalogFilterPrimitive Primitive { get; init; }

    public PrismBlendMode BlendMode { get; init; }

    public ImmutableArray<PrismCatalogFilterPass> Passes { get; init; }

    public Vector4 Options0 { get; init; }

    public Vector4 Options1 { get; init; }

    public Vector4 Options2 { get; init; }

    public Vector4 Options3 { get; init; }

    public Vector4 Options4 { get; init; }

    public Vector4 Options5 { get; init; }

    public Vector4 Options6 { get; init; }

    public Vector4 Options7 { get; init; }

    public Vector4 Options8 { get; init; }

    public PrismResourceId PrimaryResource { get; init; }

    public bool PrimaryResourceRequired { get; init; }

    public PrismResourceId AuxiliaryResource { get; init; }

    public bool AuxiliaryResourceRequired { get; init; }

    public PrismWaveNoiseTable WaveNoiseTable { get; init; }

    public uint WaveNoiseSeed { get; init; }

    public uint SpatterSeed { get; init; }

    public Vector4 GetOption(int slot) =>
        slot switch
        {
            0 => Options0,
            1 => Options1,
            2 => Options2,
            3 => Options3,
            4 => Options4,
            5 => Options5,
            6 => Options6,
            7 => Options7,
            8 => Options8,
            _ => throw new ArgumentOutOfRangeException(nameof(slot))
        };

    public Vector4 GetOption(string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        return TryGetOption(propertyName, out Vector4 value)
            ? value
            : throw new InvalidOperationException(
                $"Filter '{Filter}' has no generated property '{propertyName}'.");
    }

    public bool TryGetOption(
        string propertyName,
        out Vector4 value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)Filter);
        foreach (PrismCatalogPropertyDescriptor property in entry.Properties)
        {
            if (string.Equals(
                    property.Name,
                    propertyName,
                    StringComparison.Ordinal))
            {
                value = GetOption(property.Slot);
                return true;
            }
        }

        value = default;
        return false;
    }
}

internal static partial class PrismCatalogFilterPlanner
{
    private const string KernelOwnerPrefix =
        "SdlGpuPrismKernelSelector/";
    private const string TestOwnerPrefix =
        "PrismCatalogFilterTests/";

    public static bool IsSupported(PrismFilterId filter)
    {
        if (!TryGetPrimitive(
                filter,
                out PrismCatalogFilterPrimitive _))
        {
            return false;
        }

        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)filter);
        return entry.Kind == "filter" &&
            entry.Execution is not null &&
            string.Equals(
                entry.Coverage.Kernel,
                KernelOwnerPrefix + entry.Symbol,
                StringComparison.Ordinal) &&
            string.Equals(
                entry.Coverage.Test,
                TestOwnerPrefix + entry.Symbol,
                StringComparison.Ordinal);
    }

    public static bool RequiresOriginalInput(
        PrismFilterId filter,
        PrismCatalogFilterPass pass) =>
        ((filter is
                PrismFilterId.ColoredPencil or
                PrismFilterId.Fresco) &&
            pass.Iteration == 3) ||
        (filter == PrismFilterId.Watercolor &&
            pass.Iteration == 6) ||
        (filter == PrismFilterId.WaterPaper &&
            pass.Iteration == 1) ||
        (filter == PrismFilterId.SumiE &&
            pass.Iteration == 2) ||
        (filter == PrismFilterId.Charcoal &&
            pass.Iteration is 4 or 6) ||
        (filter == PrismFilterId.ConteCrayon &&
            pass.Iteration is 4 or 6) ||
        (filter == PrismFilterId.GraphicPen &&
            pass.Iteration is 4 or 6) ||
        (filter == PrismFilterId.ChalkCharcoal &&
            pass.Iteration == 2) ||
        (filter == PrismFilterId.Cutout &&
            pass.Kind == PrismCatalogFilterPassKind.Direct) ||
        (filter is
                PrismFilterId.AccentedEdges or
                PrismFilterId.DarkStrokes or
                PrismFilterId.InkOutlines &&
            pass.Iteration == 2) ||
        (filter is
                PrismFilterId.BasRelief or
                PrismFilterId.PosterEdges &&
            pass.Iteration is 2 or 4 or 5) ||
        (filter == PrismFilterId.GlowingEdges &&
            pass.Iteration == 2) ||
        (filter == PrismFilterId.NotePaper &&
            pass.Iteration == 2) ||
        (filter == PrismFilterId.Plaster &&
            pass.Iteration is 3 or 4) ||
        (filter is
                PrismFilterId.Photocopy or
                PrismFilterId.Stamp or
                PrismFilterId.TornEdges &&
            pass.Iteration == 2) ||
        (filter == PrismFilterId.Chrome &&
            pass.Iteration == 2) ||
        (filter == PrismFilterId.StainedGlass &&
            pass.Kind == PrismCatalogFilterPassKind.Direct &&
            pass.Iteration > 0) ||
        filter == PrismFilterId.Wind;

    public static bool RequiresStableHostCoordinates(PrismFilterId filter) =>
        filter is
            PrismFilterId.Crystallize or
            PrismFilterId.Scanlines;

    public static PrismCatalogFilterPlan Create(
        PrismFilterId filter,
        ImmutableArray<PrismGraphParameter> parameters,
        PrismBlendMode blendMode,
        float pixelScale,
        Matrix3x2 effectiveTransform,
        DrawRect sourceBounds)
    {
        if (!IsSupported(filter) ||
            !TryGetPrimitive(
                filter,
                out PrismCatalogFilterPrimitive primitive))
        {
            throw new InvalidOperationException(
                $"Filter '{filter}' has no catalog filter planner.");
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

        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)filter);
        if (parameters.Length != entry.Properties.Length)
        {
            throw new InvalidOperationException(
                $"Filter '{filter}' has {parameters.Length} graph values " +
                $"for {entry.Properties.Length} generated properties.");
        }
        if (entry.Properties.Length > 9)
        {
            throw new InvalidOperationException(
                $"Filter '{filter}' exceeds the nine generated option slots.");
        }

        PrismFilterParameterReader reader =
            new(filter, parameters);
        Vector4[] options = new Vector4[9];
        PrismResourceId primaryResource = default;
        PrismResourceId auxiliaryResource = default;
        bool primaryRequired = false;
        bool auxiliaryRequired = false;
        int resourceCount = 0;
        for (int index = 0; index < entry.Properties.Length; index++)
        {
            PrismCatalogPropertyDescriptor property =
                entry.Properties[index];
            PrismGraphParameter parameter = parameters[index];
            ValidateSlot(filter, property, parameter, index);
            if (property.ValueType == PrismCatalogValueType.Resource)
            {
                if (resourceCount == 0)
                {
                    primaryResource = parameter.ResourceValue;
                    primaryRequired = property.Required;
                }
                else if (resourceCount == 1)
                {
                    auxiliaryResource = parameter.ResourceValue;
                    auxiliaryRequired = property.Required;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Filter '{filter}' exceeds two auxiliary resources.");
                }
                resourceCount++;
                continue;
            }

            options[property.Slot] = Pack(
                reader,
                property,
                parameter);
        }

        ImmutableArray<PrismCatalogFilterPass> passes =
            CreatePasses(
                filter,
                primitive,
                reader,
                deviceScale,
                pixelScale,
                sourceBounds);
        ApplyFilterOptions(
            filter,
            entry,
            reader,
            deviceScale,
            options);
        PrismWaveNoiseTable waveNoiseTable = default;
        uint waveNoiseSeed = 0;
        uint spatterSeed = 0;
        if (filter is
            PrismFilterId.Clouds or
            PrismFilterId.DifferenceClouds)
        {
            waveNoiseSeed = unchecked(
                (uint)reader.Integer("Seed"));
            PrismWaveSpectrum spectrum = (PrismWaveSpectrum)
                reader.SymbolCode(
                    "Spectrum",
                    ("White", (int)PrismWaveSpectrum.White),
                    ("Blue", (int)PrismWaveSpectrum.Blue),
                    ("Pink", (int)PrismWaveSpectrum.Pink),
                    ("Brown", (int)PrismWaveSpectrum.Brown));
            waveNoiseTable = PrismWaveNoise.Precompute(
                unchecked((int)waveNoiseSeed),
                reader.Vector("FrequencyRange"),
                spectrum);
        }
        else if (filter == PrismFilterId.Spatter)
        {
            spatterSeed = unchecked((uint)reader.Integer("Seed"));
        }
        return new PrismCatalogFilterPlan(
            filter,
            primitive,
            blendMode,
            passes)
        {
            Options0 = options[0],
            Options1 = options[1],
            Options2 = options[2],
            Options3 = options[3],
            Options4 = options[4],
            Options5 = options[5],
            Options6 = options[6],
            Options7 = options[7],
            Options8 = options[8],
            PrimaryResource = primaryResource,
            PrimaryResourceRequired = primaryRequired,
            AuxiliaryResource = auxiliaryResource,
            AuxiliaryResourceRequired = auxiliaryRequired,
            WaveNoiseTable = waveNoiseTable,
            WaveNoiseSeed = waveNoiseSeed,
            SpatterSeed = spatterSeed
        };
    }

    private static void ValidateSlot(
        PrismFilterId filter,
        PrismCatalogPropertyDescriptor property,
        PrismGraphParameter parameter,
        int index)
    {
        PrismGraphParameterValueKind expected =
            property.ValueType switch
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
                    $"Filter '{filter}' has an unknown generated property type.")
            };
        if (property.Slot != index ||
            parameter.Index != index ||
            parameter.Kind != expected)
        {
            throw new InvalidOperationException(
                $"Filter '{filter}' property '{property.Name}' does not " +
                "match its generated slot and value type.");
        }
    }

    private static bool TryGetPrimitive(
        PrismFilterId filter,
        out PrismCatalogFilterPrimitive primitive)
    {
        int stableId = (int)filter;
        primitive = stableId switch
        {
            55 or 56 =>
                PrismCatalogFilterPrimitive.Morphology,
            >= 63 and <= 69 =>
                PrismCatalogFilterPrimitive.Quantization,
            >= 70 and <= 74 or 106 or 114 =>
                PrismCatalogFilterPrimitive.Procedural,
            75 or 76 or 134 =>
                PrismCatalogFilterPrimitive.Video,
            >= 77 and <= 99 or 113 =>
                PrismCatalogFilterPrimitive.Artistic,
            >= 100 and <= 105 or >= 107 and <= 112 or
                115 or 117 or 118 or 121 =>
                PrismCatalogFilterPrimitive.EdgeDetection,
            116 =>
                PrismCatalogFilterPrimitive.Extrude,
            120 or 133 =>
                PrismCatalogFilterPrimitive.Tiling,
            122 =>
                PrismCatalogFilterPrimitive.LineIntegralConvolution,
            >= 123 and <= 129 =>
                PrismCatalogFilterPrimitive.Texture,
            130 =>
                PrismCatalogFilterPrimitive.Convolution,
            119 or 131 or 132 =>
                PrismCatalogFilterPrimitive.Color,
            _ => (PrismCatalogFilterPrimitive)(-1)
        };
        return (int)primitive >= 0;
    }
}
