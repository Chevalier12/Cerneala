using System.Globalization;
using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism;

public abstract class PrismOperation
{
    private readonly Dictionary<string, object> parameterValues =
        new(StringComparer.Ordinal);
    private long version;

    private protected PrismOperation(PrismCatalogOperationInfo catalogInfo)
    {
        CatalogInfo = catalogInfo ?? throw new ArgumentNullException(nameof(catalogInfo));
    }

    public PrismCatalogOperationInfo CatalogInfo { get; }

    public long Version => version;

    internal event EventHandler? Changed;

    protected T GetParameter<T>(string name)
    {
        PrismCatalogParameterInfo parameter = GetParameter(name);
        if (parameterValues.TryGetValue(name, out object? value))
        {
            return (T)value;
        }

        return ParseDefault<T>(parameter);
    }

    protected void SetParameter<T>(string name, T value)
    {
        PrismCatalogParameterInfo parameter = GetParameter(name);
        ValidateValue(parameter, value);
        object boxed = value!;
        if (parameterValues.TryGetValue(name, out object? current) &&
            Equals(current, boxed))
        {
            return;
        }

        parameterValues[name] = boxed;
        MarkChanged();
    }

    internal abstract bool IsFilter { get; }

    internal abstract PrismFilterDefinition? CreateFilterDefinition();

    internal abstract PrismStyleDefinition? CreateStyleDefinition();

    internal abstract void ApplyTo(PrismFilterState state);

    internal abstract void ApplyTo(PrismStyleState state);

    protected void MarkChanged()
    {
        unchecked
        {
            version++;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    protected static float ValidateOpacity(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Prism opacity must be finite and between zero and one.");
        }

        return value;
    }

    private protected void ApplyParameters(PrismFilterState state)
    {
        foreach ((string name, object value) in parameterValues)
        {
            PrismCatalogParameterInfo parameter = GetParameter(name);
            ApplyParameter(state, parameter, value);
        }
    }

    private protected void ApplyParameters(PrismStyleState state)
    {
        foreach ((string name, object value) in parameterValues)
        {
            PrismCatalogParameterInfo parameter = GetParameter(name);
            ApplyParameter(state, parameter, value);
        }
    }

    private PrismCatalogParameterInfo GetParameter(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return CatalogInfo.Parameters.FirstOrDefault(
                parameter => string.Equals(parameter.Name, name, StringComparison.Ordinal))
            ?? throw new ArgumentException(
                $"Prism operation '{CatalogInfo.Symbol}' has no parameter named '{name}'.",
                nameof(name));
    }

    private static void ApplyParameter(
        PrismFilterState state,
        PrismCatalogParameterInfo parameter,
        object value)
    {
        switch (parameter.ValueKind)
        {
            case PrismCatalogValueKind.Boolean:
                state.SetValue(parameter, (bool)value);
                break;
            case PrismCatalogValueKind.Integer:
                state.SetValue(parameter, (int)value);
                break;
            case PrismCatalogValueKind.Number:
                state.SetValue(parameter, (float)value);
                break;
            case PrismCatalogValueKind.Color:
                state.SetValue(parameter, (Color)value);
                break;
            case PrismCatalogValueKind.Vector:
                state.SetValue(parameter, (Vector4)value);
                break;
            case PrismCatalogValueKind.Symbol:
                state.SetValue(parameter, (string)value);
                break;
            case PrismCatalogValueKind.Resource:
                state.SetValue(parameter, (PrismResourceId)value);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unknown Prism value kind '{parameter.ValueKind}'.");
        }
    }

    private static void ApplyParameter(
        PrismStyleState state,
        PrismCatalogParameterInfo parameter,
        object value)
    {
        switch (parameter.ValueKind)
        {
            case PrismCatalogValueKind.Boolean:
                state.SetValue(parameter, (bool)value);
                break;
            case PrismCatalogValueKind.Integer:
                state.SetValue(parameter, (int)value);
                break;
            case PrismCatalogValueKind.Number:
                state.SetValue(parameter, (float)value);
                break;
            case PrismCatalogValueKind.Color:
                state.SetValue(parameter, (Color)value);
                break;
            case PrismCatalogValueKind.Vector:
                state.SetValue(parameter, (Vector4)value);
                break;
            case PrismCatalogValueKind.Symbol:
                state.SetValue(parameter, (string)value);
                break;
            case PrismCatalogValueKind.Resource:
                state.SetValue(parameter, (PrismResourceId)value);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unknown Prism value kind '{parameter.ValueKind}'.");
        }
    }

    private static void ValidateValue<T>(
        PrismCatalogParameterInfo parameter,
        T value)
    {
        Type expectedType = parameter.ValueKind switch
        {
            PrismCatalogValueKind.Boolean => typeof(bool),
            PrismCatalogValueKind.Integer => typeof(int),
            PrismCatalogValueKind.Number => typeof(float),
            PrismCatalogValueKind.Color => typeof(Color),
            PrismCatalogValueKind.Vector => typeof(Vector4),
            PrismCatalogValueKind.Symbol => typeof(string),
            PrismCatalogValueKind.Resource => typeof(PrismResourceId),
            _ => throw new InvalidOperationException(
                $"Unknown Prism value kind '{parameter.ValueKind}'.")
        };
        if (typeof(T) != expectedType)
        {
            throw new ArgumentException(
                $"Prism parameter '{parameter.Name}' requires '{expectedType.Name}'.",
                nameof(value));
        }

        if (value is float number)
        {
            if (!float.IsFinite(number) ||
                parameter.Minimum is double minimum && number < (float)minimum ||
                parameter.Maximum is double maximum && number > (float)maximum)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }
        else if (value is int integer &&
            (parameter.Minimum is double minimum && integer < minimum ||
             parameter.Maximum is double maximum && integer > maximum))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, null);
        }
        else if (value is Vector4 vector &&
            (!float.IsFinite(vector.X) ||
             !float.IsFinite(vector.Y) ||
             !float.IsFinite(vector.Z) ||
             !float.IsFinite(vector.W) ||
             parameter.DomainKind == "positive-xy-components" &&
             (vector.X <= 0 || vector.Y <= 0)))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, null);
        }
        else if (value is string symbol &&
            !parameter.SymbolOptions.Contains(symbol, StringComparer.Ordinal))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                $"'{symbol}' is not valid for Prism parameter '{parameter.Name}'.");
        }
    }

    private static T ParseDefault<T>(PrismCatalogParameterInfo parameter)
    {
        string? text = parameter.DefaultValue;
        if (text is null || string.Equals(text, "null", StringComparison.Ordinal))
        {
            return default!;
        }

        object value = parameter.ValueKind switch
        {
            PrismCatalogValueKind.Boolean => bool.Parse(text),
            PrismCatalogValueKind.Integer => int.Parse(
                text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture),
            PrismCatalogValueKind.Number => float.Parse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture),
            PrismCatalogValueKind.Color => Color.TryParse(text, out Color color)
                ? color
                : throw InvalidDefault(parameter),
            PrismCatalogValueKind.Vector => ParseVector(text, parameter),
            PrismCatalogValueKind.Symbol => text,
            PrismCatalogValueKind.Resource => default(PrismResourceId),
            _ => throw InvalidDefault(parameter)
        };
        return (T)value;
    }

    private static Vector4 ParseVector(
        string text,
        PrismCatalogParameterInfo parameter)
    {
        string[] components = text.Split(',');
        if (components.Length is < 2 or > 4)
        {
            throw InvalidDefault(parameter);
        }

        Span<float> values = stackalloc float[4];
        for (int index = 0; index < components.Length; index++)
        {
            if (!float.TryParse(
                    components[index],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out values[index]))
            {
                throw InvalidDefault(parameter);
            }
        }

        return new Vector4(values[0], values[1], values[2], values[3]);
    }

    private static InvalidOperationException InvalidDefault(
        PrismCatalogParameterInfo parameter) =>
        new($"Catalog default '{parameter.DefaultValue}' is invalid for '{parameter.Name}'.");
}

public abstract class PrismFilter : PrismOperation
{
    private bool visible = true;
    private float opacity = 1;
    private PrismBlendMode blendMode = PrismBlendMode.Normal;

    private protected PrismFilter(PrismFilterId filterId)
        : base(PrismCatalog.GetFilter(filterId))
    {
        FilterId = filterId;
    }

    public PrismFilterId FilterId { get; }

    public bool Visible
    {
        get => visible;
        set
        {
            if (visible != value)
            {
                visible = value;
                MarkChanged();
            }
        }
    }

    public float Opacity
    {
        get => opacity;
        set
        {
            value = ValidateOpacity(value, nameof(value));
            if (!opacity.Equals(value))
            {
                opacity = value;
                MarkChanged();
            }
        }
    }

    public PrismBlendMode BlendMode
    {
        get => blendMode;
        set
        {
            if (value == PrismBlendMode.PassThrough)
            {
                throw new ArgumentException(
                    "PassThrough is valid only for Prism groups.",
                    nameof(value));
            }
            if (blendMode != value)
            {
                blendMode = value;
                MarkChanged();
            }
        }
    }

    internal sealed override bool IsFilter => true;

    internal sealed override PrismFilterDefinition CreateFilterDefinition() =>
        new(FilterId, Visible, Opacity, BlendMode);

    internal sealed override PrismStyleDefinition? CreateStyleDefinition() => null;

    internal sealed override void ApplyTo(PrismFilterState state)
    {
        state.Visible = Visible;
        state.Opacity = Opacity;
        state.BlendMode = BlendMode;
        ApplyParameters(state);
    }

    internal sealed override void ApplyTo(PrismStyleState state) =>
        throw new InvalidOperationException("A Prism filter cannot be applied to a style state.");
}

public abstract class PrismStyle : PrismOperation
{
    private bool visible = true;

    private protected PrismStyle(PrismStyleId styleId)
        : base(PrismCatalog.GetStyle(styleId))
    {
        StyleId = styleId;
    }

    public PrismStyleId StyleId { get; }

    public bool Visible
    {
        get => visible;
        set
        {
            if (visible != value)
            {
                visible = value;
                MarkChanged();
            }
        }
    }

    internal sealed override bool IsFilter => false;

    internal sealed override PrismFilterDefinition? CreateFilterDefinition() => null;

    internal sealed override PrismStyleDefinition CreateStyleDefinition() =>
        new(StyleId, Visible);

    internal sealed override void ApplyTo(PrismFilterState state) =>
        throw new InvalidOperationException("A Prism style cannot be applied to a filter state.");

    internal sealed override void ApplyTo(PrismStyleState state)
    {
        state.Visible = Visible;
        ApplyParameters(state);
    }
}
