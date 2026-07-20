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
    Color
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

internal static class PrismCatalogFilterPlanner
{
    private const string KernelOwnerPrefix =
        "PrismKernelRegistry/";
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
            AuxiliaryResourceRequired = auxiliaryRequired
        };
    }

    private static ImmutableArray<PrismCatalogFilterPass> CreatePasses(
        PrismFilterId filter,
        PrismCatalogFilterPrimitive primitive,
        PrismFilterParameterReader values,
        float deviceScale,
        float pixelScale,
        DrawRect sourceBounds)
    {
        if (filter is PrismFilterId.Maximum or PrismFilterId.Minimum)
        {
            float radius = values.Number("Radius") * deviceScale;
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

            ImmutableArray<PrismCatalogFilterPass>.Builder passes =
                ImmutableArray.CreateBuilder<PrismCatalogFilterPass>(2);
            if (sourceBounds.Width > 0)
            {
                passes.Add(
                    new(
                        PrismCatalogFilterPassKind.Horizontal,
                        radius,
                        0,
                        radius / pixelScale,
                        0,
                        0,
                        IsNoOp: false));
            }
            if (sourceBounds.Height > 0)
            {
                passes.Add(
                    new(
                        PrismCatalogFilterPassKind.Vertical,
                        0,
                        radius,
                        0,
                        radius / pixelScale,
                        1,
                        IsNoOp: false));
            }
            return passes.ToImmutable();
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
            for (int index = 0; index < iterations; index++)
            {
                passes.Add(
                    new(
                        PrismCatalogFilterPassKind.Iteration,
                        deviceScale,
                        deviceScale,
                        0,
                        0,
                        index,
                        IsNoOp: false));
            }
            return passes.MoveToImmutable();
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
            PrismFilterId.ChromaticAberration =>
                MathF.Abs(values.Number("Amount")),
            PrismFilterId.CustomConvolution => 1,
            PrismFilterId.FindEdges => 1,
            PrismFilterId.Emboss =>
                MathF.Max(1, values.Number("Height")),
            PrismFilterId.GlowingEdges =>
                MathF.Max(1, values.Number("EdgeWidth")),
            PrismFilterId.TraceContour => 1,
            _ when primitive is
                PrismCatalogFilterPrimitive.Artistic or
                PrismCatalogFilterPrimitive.EdgeDetection or
                PrismCatalogFilterPrimitive.Texture => 1,
            _ => 0
        };
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

    private static Vector4 Pack(
        PrismFilterParameterReader reader,
        PrismCatalogPropertyDescriptor property,
        PrismGraphParameter parameter)
    {
        return property.ValueType switch
        {
            PrismCatalogValueType.Boolean =>
                new Vector4(parameter.BooleanValue ? 1 : 0, 0, 0, 0),
            PrismCatalogValueType.Integer =>
                PackInteger(parameter.IntegerValue),
            PrismCatalogValueType.Number =>
                new Vector4(parameter.NumberValue, 0, 0, 0),
            PrismCatalogValueType.Color =>
                reader.Color(property.Name),
            PrismCatalogValueType.Vector =>
                parameter.VectorValue,
            PrismCatalogValueType.Symbol =>
                PackInteger(parameter.IntegerValue),
            PrismCatalogValueType.Resource =>
                Vector4.Zero,
            _ => throw new InvalidOperationException(
                $"Property '{property.Name}' has an unknown catalog value type.")
        };
    }

    private static Vector4 PackInteger(int value)
    {
        uint bits = unchecked((uint)value);
        return new Vector4(
            bits & 0xffffu,
            bits >> 16,
            0,
            0);
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
            >= 70 and <= 74 or 114 =>
                PrismCatalogFilterPrimitive.Procedural,
            75 or 76 or 134 =>
                PrismCatalogFilterPrimitive.Video,
            >= 77 and <= 99 =>
                PrismCatalogFilterPrimitive.Artistic,
            >= 100 and <= 113 or 115 or 117 or 118 or 121 =>
                PrismCatalogFilterPrimitive.EdgeDetection,
            116 or 120 or 122 or 133 =>
                PrismCatalogFilterPrimitive.Tiling,
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
