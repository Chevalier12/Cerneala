using System.Collections.Immutable;
using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Filters;

internal readonly ref struct PrismFilterParameterReader
{
    private readonly PrismFilterId filter;
    private readonly ImmutableArray<PrismGraphParameter> parameters;
    private readonly PrismCatalogPropertyDescriptor[] properties;

    public PrismFilterParameterReader(
        PrismFilterId filter,
        ImmutableArray<PrismGraphParameter> parameters)
    {
        this.filter = filter;
        this.parameters = parameters;
        properties =
            PrismCatalogRuntime.GetEntry((int)filter).Properties;
        if (parameters.Length != properties.Length)
        {
            throw new InvalidOperationException(
                $"Filter '{filter}' has {parameters.Length} graph values " +
                $"for {properties.Length} generated properties.");
        }
    }

    public bool Boolean(string name) =>
        Value(name, PrismGraphParameterValueKind.Boolean)
            .BooleanValue;

    public int Integer(string name) =>
        Value(name, PrismGraphParameterValueKind.Integer)
            .IntegerValue;

    public float Number(string name) =>
        Value(name, PrismGraphParameterValueKind.Number)
            .NumberValue;

    public Vector4 Vector(string name) =>
        Value(name, PrismGraphParameterValueKind.Vector)
            .VectorValue;

    public PrismResourceId Resource(string name) =>
        Value(name, PrismGraphParameterValueKind.Resource)
            .ResourceValue;

    public Vector4 Color(string name)
    {
        Color color =
            Value(name, PrismGraphParameterValueKind.Color)
                .ColorValue;
        double alpha = color.A / 255d;
        PrismPremultipliedColor converted =
            PrismColorPipeline.ConvertInputToWorking(
                PrismPremultipliedColor.FromStraight(
                    color.R / 255d,
                    color.G / 255d,
                    color.B / 255d,
                    alpha),
                PrismColorProfile.LinearSrgb);
        return alpha == 0
            ? Vector4.Zero
            : new Vector4(
                (float)(converted.Red / alpha),
                (float)(converted.Green / alpha),
                (float)(converted.Blue / alpha),
                (float)alpha);
    }

    public int SymbolCode(
        string name,
        params (string Symbol, int Code)[] mappings)
    {
        int value =
            Value(name, PrismGraphParameterValueKind.Symbol)
                .IntegerValue;
        foreach ((string symbol, int code) in mappings)
        {
            if (value ==
                PrismCatalogRuntime.ResolveSymbol(name, symbol))
            {
                return code;
            }
        }

        throw new InvalidOperationException(
            $"Filter property '{name}' has unsupported symbol '{value}'.");
    }

    private PrismGraphParameter Value(
        string name,
        PrismGraphParameterValueKind kind)
    {
        for (int index = 0; index < properties.Length; index++)
        {
            if (!string.Equals(
                    properties[index].Name,
                    name,
                    StringComparison.Ordinal))
            {
                continue;
            }

            PrismGraphParameter parameter = parameters[index];
            if (parameter.Index != index ||
                parameter.Kind != kind)
            {
                throw new InvalidOperationException(
                    $"Filter property '{name}' does not match its generated slot.");
            }
            return parameter;
        }

        throw new InvalidOperationException(
            $"Filter '{filter}' has no generated property '{name}'.");
    }
}
