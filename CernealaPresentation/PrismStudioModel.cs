using System.Collections.ObjectModel;
using System.Globalization;
using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Presentation;

internal enum PrismStudioTarget
{
    Mascot,
    Typography,
    Badge,
    Card
}

internal sealed class PrismStudioOperation
{
    private readonly Dictionary<string, object> values;

    public PrismStudioOperation(int id, PrismCatalogOperationInfo catalog)
    {
        Id = id;
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        values = catalog.Parameters.ToDictionary(
            parameter => parameter.Id,
            PrismStudioValueCodec.DefaultValue,
            StringComparer.Ordinal);
    }

    public int Id { get; }

    public PrismCatalogOperationInfo Catalog { get; }

    public bool IsVisible { get; set; } = true;

    public float Opacity { get; set; } = 1f;

    public PrismBlendMode BlendMode { get; set; } = PrismBlendMode.Normal;

    public IReadOnlyDictionary<string, object> Values => new ReadOnlyDictionary<string, object>(values);

    public object GetValue(PrismCatalogParameterInfo parameter)
    {
        ValidateParameter(parameter);
        return values[parameter.Id];
    }

    public void SetValue(PrismCatalogParameterInfo parameter, object value)
    {
        ValidateParameter(parameter);
        PrismStudioValueCodec.Validate(parameter, value);
        values[parameter.Id] = value;
    }

    private void ValidateParameter(PrismCatalogParameterInfo parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        if (!Catalog.Parameters.Contains(parameter))
        {
            throw new ArgumentException("The parameter belongs to another Prism operation.", nameof(parameter));
        }
    }
}

internal sealed class PrismStudioLayer
{
    private readonly List<PrismStudioOperation> filters = [];
    private readonly List<PrismStudioOperation> styles = [];

    public PrismStudioLayer(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public int Id { get; }

    public string Name { get; set; }

    public bool IsVisible { get; set; } = true;

    public float Opacity { get; set; } = 1f;

    public float Fill { get; set; } = 1f;

    public PrismBlendMode BlendMode { get; set; } = PrismBlendMode.Normal;

    public IReadOnlyList<PrismStudioOperation> Filters => filters;

    public IReadOnlyList<PrismStudioOperation> Styles => styles;

    public IEnumerable<PrismStudioOperation> Operations => filters.Concat(styles);

    internal List<PrismStudioOperation> MutableFilters => filters;

    internal List<PrismStudioOperation> MutableStyles => styles;
}

internal sealed class PrismStudioModel
{
    private readonly List<PrismStudioLayer> layers = [];
    private int nextLayerId;
    private int nextOperationId;

    public PrismStudioModel()
    {
        Reset();
    }

    public IReadOnlyList<PrismStudioLayer> Layers => layers;

    public PrismStudioTarget Target { get; private set; }

    public int SelectedLayerId { get; private set; }

    public int? SelectedOperationId { get; private set; }

    public int OperationCount => layers.Sum(layer => layer.Filters.Count + layer.Styles.Count);

    public void Reset()
    {
        layers.Clear();
        nextLayerId = 1;
        nextOperationId = 1;
        Target = PrismStudioTarget.Mascot;
        SelectedLayerId = 0;
        SelectedOperationId = null;
    }

    public void SelectTarget(PrismStudioTarget target)
    {
        Target = target;
    }

    public void SelectLayer(int layerId)
    {
        PrismStudioLayer layer = Layer(layerId);
        SelectedLayerId = layer.Id;
        SelectedOperationId = null;
    }

    public void SelectOperation(int operationId)
    {
        (PrismStudioLayer layer, PrismStudioOperation operation) = FindOperation(operationId);
        SelectedLayerId = layer.Id;
        SelectedOperationId = operation.Id;
    }

    public PrismStudioLayer AddLayer(string? name = null)
    {
        int id = nextLayerId++;
        PrismStudioLayer layer = new(
            id,
            string.IsNullOrWhiteSpace(name) ? $"LAYER {id:00}" : name);
        layers.Insert(0, layer);
        SelectedLayerId = layer.Id;
        SelectedOperationId = null;
        return layer;
    }

    public bool RemoveLayer(int layerId)
    {
        int index = layers.FindIndex(layer => layer.Id == layerId);
        if (index < 0)
        {
            return false;
        }

        layers.RemoveAt(index);
        if (layers.Count == 0)
        {
            SelectedLayerId = 0;
            SelectedOperationId = null;
            return true;
        }

        PrismStudioLayer selected = layers[Math.Min(index, layers.Count - 1)];
        SelectedLayerId = selected.Id;
        SelectedOperationId = selected.Operations.FirstOrDefault()?.Id;
        return true;
    }

    public bool MoveLayer(int layerId, int offset)
    {
        int index = layers.FindIndex(layer => layer.Id == layerId);
        if (index < 0)
        {
            return false;
        }

        int destination = Math.Clamp(index + offset, 0, layers.Count - 1);
        if (destination == index)
        {
            return false;
        }

        PrismStudioLayer layer = layers[index];
        layers.RemoveAt(index);
        layers.Insert(destination, layer);
        return true;
    }

    public bool AddOperation(int layerId, PrismCatalogOperationInfo catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        if (catalog.RequiresResource)
        {
            return false;
        }

        PrismStudioLayer layer = Layer(layerId);
        PrismStudioOperation operation = AddOperationCore(layer, catalog);
        SelectedLayerId = layer.Id;
        SelectedOperationId = operation.Id;
        return true;
    }

    public bool RemoveOperation(int operationId)
    {
        (PrismStudioLayer layer, PrismStudioOperation operation) = FindOperation(operationId);
        List<PrismStudioOperation> list = OperationList(layer, operation.Catalog.Kind);
        int index = list.IndexOf(operation);
        list.RemoveAt(index);
        PrismStudioOperation? selected = list.Count > 0
            ? list[Math.Min(index, list.Count - 1)]
            : layer.Operations.FirstOrDefault();
        SelectedLayerId = layer.Id;
        SelectedOperationId = selected?.Id;
        return true;
    }

    public bool MoveOperation(int operationId, int offset)
    {
        (PrismStudioLayer layer, PrismStudioOperation operation) = FindOperation(operationId);
        List<PrismStudioOperation> list = OperationList(layer, operation.Catalog.Kind);
        int index = list.IndexOf(operation);
        int destination = Math.Clamp(index + offset, 0, list.Count - 1);
        if (destination == index)
        {
            return false;
        }

        list.RemoveAt(index);
        list.Insert(destination, operation);
        return true;
    }

    public PrismCompositionDefinition BuildDefinition()
    {
        return new PrismCompositionDefinition(
            "PrismStudio",
            layers.Where(layer => layer.Operations.Any()).Select(layer => new PrismLayerDefinition(
                new PrismNodeId(layer.Id),
                $"Layer{layer.Id}",
                layer.Filters.Select(operation => new PrismFilterDefinition(
                    (PrismFilterId)operation.Catalog.StableId,
                    visible: operation.IsVisible,
                    opacity: operation.Opacity,
                    blendMode: operation.BlendMode)),
                layer.Styles.Select(operation => new PrismStyleDefinition(
                    (PrismStyleId)operation.Catalog.StableId,
                    visible: operation.IsVisible)),
                visible: layer.IsVisible,
                opacity: layer.Opacity,
                fill: layer.Fill,
                blendMode: layer.BlendMode)));
    }

    public void ApplyTo(PrismInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        instance.ReplaceDefinition(BuildDefinition());
        foreach (PrismStudioLayer layer in layers)
        {
            if (!layer.Operations.Any())
            {
                continue;
            }

            PrismLayerState layerState = instance.GetLayerState(new PrismNodeId(layer.Id));
            for (int index = 0; index < layer.Filters.Count; index++)
            {
                Apply(layer.Filters[index], layerState.Filters[index]);
            }
            for (int index = 0; index < layer.Styles.Count; index++)
            {
                Apply(layer.Styles[index], layerState.Styles[index]);
            }
        }
    }

    public void SetLayerVisibility(PrismInstance? instance, int layerId, bool value)
    {
        PrismStudioLayer layer = Layer(layerId);
        layer.IsVisible = value;
        if (instance is not null && layer.Operations.Any())
        {
            instance.GetLayerState(new PrismNodeId(layer.Id)).Visible = value;
        }
    }

    public void SetLayerOpacity(PrismInstance? instance, int layerId, float value)
    {
        PrismStudioValueCodec.ValidateUnitInterval(value, nameof(value));
        PrismStudioLayer layer = Layer(layerId);
        layer.Opacity = value;
        if (instance is not null && layer.Operations.Any())
        {
            instance.GetLayerState(new PrismNodeId(layer.Id)).Opacity = value;
        }
    }

    public void SetLayerFill(PrismInstance? instance, int layerId, float value)
    {
        PrismStudioValueCodec.ValidateUnitInterval(value, nameof(value));
        PrismStudioLayer layer = Layer(layerId);
        layer.Fill = value;
        if (instance is not null && layer.Operations.Any())
        {
            instance.GetLayerState(new PrismNodeId(layer.Id)).Fill = value;
        }
    }

    public void SetLayerBlendMode(PrismInstance? instance, int layerId, PrismBlendMode value)
    {
        if (value == PrismBlendMode.PassThrough)
        {
            throw new ArgumentException("PassThrough is valid only for Prism groups.", nameof(value));
        }

        PrismStudioLayer layer = Layer(layerId);
        layer.BlendMode = value;
        if (instance is not null && layer.Operations.Any())
        {
            instance.GetLayerState(new PrismNodeId(layer.Id)).BlendMode = value;
        }
    }

    public void SetOperationVisibility(PrismInstance instance, int operationId, bool value)
    {
        (PrismStudioLayer layer, PrismStudioOperation operation) = FindOperation(operationId);
        operation.IsVisible = value;
        GetOperationState(instance, layer, operation).SetVisibility(value);
    }

    public void SetFilterOpacity(PrismInstance instance, int operationId, float value)
    {
        PrismStudioValueCodec.ValidateUnitInterval(value, nameof(value));
        (PrismStudioLayer layer, PrismStudioOperation operation) = FindOperation(operationId);
        if (operation.Catalog.Kind != PrismCatalogOperationKind.Filter)
        {
            throw new ArgumentException("Opacity is available only for Prism filters.", nameof(operationId));
        }

        operation.Opacity = value;
        instance.GetLayerState(new PrismNodeId(layer.Id))
            .Filters[layer.MutableFilters.IndexOf(operation)]
            .Opacity = value;
    }

    public void SetFilterBlendMode(PrismInstance instance, int operationId, PrismBlendMode value)
    {
        if (value == PrismBlendMode.PassThrough)
        {
            throw new ArgumentException("PassThrough is valid only for Prism groups.", nameof(value));
        }

        (PrismStudioLayer layer, PrismStudioOperation operation) = FindOperation(operationId);
        if (operation.Catalog.Kind != PrismCatalogOperationKind.Filter)
        {
            throw new ArgumentException("Blend mode is available only for Prism filters.", nameof(operationId));
        }

        operation.BlendMode = value;
        instance.GetLayerState(new PrismNodeId(layer.Id))
            .Filters[layer.MutableFilters.IndexOf(operation)]
            .BlendMode = value;
    }

    public void SetOperationValue(
        PrismInstance instance,
        int operationId,
        PrismCatalogParameterInfo parameter,
        object value)
    {
        (PrismStudioLayer layer, PrismStudioOperation operation) = FindOperation(operationId);
        operation.SetValue(parameter, value);
        OperationState state = GetOperationState(instance, layer, operation);
        if (state.Filter is not null)
        {
            Set(state.Filter, parameter, value);
        }
        else
        {
            Set(state.Style!, parameter, value);
        }
    }

    public PrismStudioLayer Layer(int layerId) =>
        layers.Single(layer => layer.Id == layerId);

    public PrismStudioOperation Operation(int operationId) => FindOperation(operationId).Operation;

    private PrismStudioOperation AddOperationCore(
        PrismStudioLayer layer,
        PrismCatalogOperationInfo catalog)
    {
        PrismStudioOperation operation = new(nextOperationId++, catalog);
        OperationList(layer, catalog.Kind).Add(operation);
        return operation;
    }

    private (PrismStudioLayer Layer, PrismStudioOperation Operation) FindOperation(int operationId)
    {
        foreach (PrismStudioLayer layer in layers)
        {
            PrismStudioOperation? operation = layer.Operations.FirstOrDefault(candidate => candidate.Id == operationId);
            if (operation is not null)
            {
                return (layer, operation);
            }
        }

        throw new KeyNotFoundException($"Prism Studio operation '{operationId}' does not exist.");
    }

    private static List<PrismStudioOperation> OperationList(
        PrismStudioLayer layer,
        PrismCatalogOperationKind kind) =>
        kind == PrismCatalogOperationKind.Filter ? layer.MutableFilters : layer.MutableStyles;

    private static OperationState GetOperationState(
        PrismInstance instance,
        PrismStudioLayer layer,
        PrismStudioOperation operation)
    {
        PrismLayerState state = instance.GetLayerState(new PrismNodeId(layer.Id));
        return operation.Catalog.Kind == PrismCatalogOperationKind.Filter
            ? new OperationState(state.Filters[layer.MutableFilters.IndexOf(operation)], null)
            : new OperationState(null, state.Styles[layer.MutableStyles.IndexOf(operation)]);
    }

    private static void Apply(PrismStudioOperation operation, PrismFilterState state)
    {
        foreach (PrismCatalogParameterInfo parameter in operation.Catalog.Parameters)
        {
            Set(state, parameter, operation.GetValue(parameter));
        }
    }

    private static void Apply(PrismStudioOperation operation, PrismStyleState state)
    {
        foreach (PrismCatalogParameterInfo parameter in operation.Catalog.Parameters)
        {
            Set(state, parameter, operation.GetValue(parameter));
        }
    }

    private static void Set(PrismFilterState state, PrismCatalogParameterInfo parameter, object value)
    {
        switch (parameter.ValueKind)
        {
            case PrismCatalogValueKind.Boolean: state.SetValue(parameter, (bool)value); break;
            case PrismCatalogValueKind.Integer: state.SetValue(parameter, (int)value); break;
            case PrismCatalogValueKind.Number: state.SetValue(parameter, (float)value); break;
            case PrismCatalogValueKind.Color: state.SetValue(parameter, (Color)value); break;
            case PrismCatalogValueKind.Vector: state.SetValue(parameter, (Vector4)value); break;
            case PrismCatalogValueKind.Symbol: state.SetValue(parameter, (string)value); break;
            case PrismCatalogValueKind.Resource: state.SetValue(parameter, (PrismResourceId)value); break;
        }
    }

    private static void Set(PrismStyleState state, PrismCatalogParameterInfo parameter, object value)
    {
        switch (parameter.ValueKind)
        {
            case PrismCatalogValueKind.Boolean: state.SetValue(parameter, (bool)value); break;
            case PrismCatalogValueKind.Integer: state.SetValue(parameter, (int)value); break;
            case PrismCatalogValueKind.Number: state.SetValue(parameter, (float)value); break;
            case PrismCatalogValueKind.Color: state.SetValue(parameter, (Color)value); break;
            case PrismCatalogValueKind.Vector: state.SetValue(parameter, (Vector4)value); break;
            case PrismCatalogValueKind.Symbol: state.SetValue(parameter, (string)value); break;
            case PrismCatalogValueKind.Resource: state.SetValue(parameter, (PrismResourceId)value); break;
        }
    }

    private readonly record struct OperationState(PrismFilterState? Filter, PrismStyleState? Style)
    {
        public void SetVisibility(bool value)
        {
            if (Filter is not null)
            {
                Filter.Visible = value;
            }
            else
            {
                Style!.Visible = value;
            }
        }
    }
}

internal static class PrismStudioValueCodec
{
    public static void ValidateUnitInterval(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value is < 0f or > 1f)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be between zero and one.");
        }
    }

    public static object DefaultValue(PrismCatalogParameterInfo parameter)
    {
        string? value = parameter.DefaultValue;
        return parameter.ValueKind switch
        {
            PrismCatalogValueKind.Boolean => value is not null && bool.Parse(value),
            PrismCatalogValueKind.Integer => value is null ? 0 : int.Parse(value, CultureInfo.InvariantCulture),
            PrismCatalogValueKind.Number => value is null ? 0f : float.Parse(value, CultureInfo.InvariantCulture),
            PrismCatalogValueKind.Color => ParseColor(value),
            PrismCatalogValueKind.Vector => ParseVector(value),
            PrismCatalogValueKind.Symbol => value ?? parameter.SymbolOptions.First(),
            PrismCatalogValueKind.Resource => default(PrismResourceId),
            _ => throw new InvalidOperationException($"Unknown Prism value kind '{parameter.ValueKind}'.")
        };
    }

    public static void Validate(PrismCatalogParameterInfo parameter, object value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Type expected = parameter.ValueKind switch
        {
            PrismCatalogValueKind.Boolean => typeof(bool),
            PrismCatalogValueKind.Integer => typeof(int),
            PrismCatalogValueKind.Number => typeof(float),
            PrismCatalogValueKind.Color => typeof(Color),
            PrismCatalogValueKind.Vector => typeof(Vector4),
            PrismCatalogValueKind.Symbol => typeof(string),
            PrismCatalogValueKind.Resource => typeof(PrismResourceId),
            _ => throw new InvalidOperationException($"Unknown Prism value kind '{parameter.ValueKind}'.")
        };
        if (value.GetType() != expected)
        {
            throw new ArgumentException($"Parameter '{parameter.Name}' requires '{expected.Name}'.", nameof(value));
        }

        if (value is float number)
        {
            ValidateRange(parameter, number);
        }
        else if (value is int integer)
        {
            ValidateRange(parameter, integer);
        }
        else if (value is string symbol && !parameter.SymbolOptions.Contains(symbol, StringComparer.Ordinal))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown Prism catalog symbol.");
        }
    }

    private static void ValidateRange(PrismCatalogParameterInfo parameter, double value)
    {
        if (!double.IsFinite(value) ||
            parameter.Minimum is double minimum && value < minimum ||
            parameter.Maximum is double maximum && value > maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"Value is outside the domain for '{parameter.Name}'.");
        }
    }

    private static Color ParseColor(string? value)
    {
        if (value is null || !Color.TryParse(value, out Color color))
        {
            return default;
        }
        return color;
    }

    private static Vector4 ParseVector(string? value)
    {
        if (value is null)
        {
            return default;
        }

        float[] components = value.Split(',')
            .Select(component => float.Parse(component, CultureInfo.InvariantCulture))
            .ToArray();
        return new Vector4(
            components.ElementAtOrDefault(0),
            components.ElementAtOrDefault(1),
            components.ElementAtOrDefault(2),
            components.ElementAtOrDefault(3));
    }
}
