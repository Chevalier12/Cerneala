using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.UI.Prism.Runtime;

internal readonly struct PrismStateAccess
{
    private readonly PrismInstance owner;
    private readonly PrismParameterStore store;
    private readonly PrismValueSlice slice;
    private readonly int generation;
    private readonly int entryStableId;

    public PrismStateAccess(
        PrismInstance owner,
        PrismParameterStore store,
        PrismValueSlice slice,
        int generation,
        int entryStableId)
    {
        this.owner = owner;
        this.store = store;
        this.slice = slice;
        this.generation = generation;
        this.entryStableId = entryStableId;
    }

    public bool Get(PrismParameterKey<bool> key)
    {
        Validate(key.EntryStableId);
        return store.Get(slice, key);
    }

    public int Get(PrismParameterKey<int> key)
    {
        Validate(key.EntryStableId);
        return store.Get(slice, key);
    }

    public float Get(PrismParameterKey<float> key)
    {
        Validate(key.EntryStableId);
        return store.Get(slice, key);
    }

    public Color Get(PrismParameterKey<Color> key)
    {
        Validate(key.EntryStableId);
        return store.Get(slice, key);
    }

    public Vector4 Get(PrismParameterKey<Vector4> key)
    {
        Validate(key.EntryStableId);
        return store.Get(slice, key);
    }

    public PrismResourceId Get(PrismParameterKey<PrismResourceId> key)
    {
        Validate(key.EntryStableId);
        return store.Get(slice, key);
    }

    public void Set(PrismParameterKey<bool> key, bool value)
    {
        Validate(key.EntryStableId);
        if (store.Set(slice, key, value))
        {
            owner.MarkValueChanged(generation);
        }
    }

    public void Set(PrismParameterKey<int> key, int value)
    {
        Validate(key.EntryStableId);
        if (store.Set(slice, key, value))
        {
            owner.MarkValueChanged(generation);
        }
    }

    public void Set(PrismParameterKey<float> key, float value)
    {
        Validate(key.EntryStableId);
        if (store.Set(slice, key, value))
        {
            owner.MarkValueChanged(generation);
        }
    }

    public void Set(PrismParameterKey<Color> key, Color value)
    {
        Validate(key.EntryStableId);
        if (store.Set(slice, key, value))
        {
            owner.MarkValueChanged(generation);
        }
    }

    public void Set(PrismParameterKey<Vector4> key, Vector4 value)
    {
        Validate(key.EntryStableId);
        if (store.Set(slice, key, value))
        {
            owner.MarkValueChanged(generation);
        }
    }

    public void Set(PrismParameterKey<PrismResourceId> key, PrismResourceId value)
    {
        Validate(key.EntryStableId);
        if (store.Set(slice, key, value))
        {
            owner.MarkValueChanged(generation);
        }
    }

    private void Validate(int keyEntryStableId)
    {
        owner.EnsureCurrent(generation);
        if (keyEntryStableId != entryStableId)
        {
            throw new ArgumentException(
                $"Prism key belongs to catalog entry {keyEntryStableId}, not {entryStableId}.");
        }
    }
}

public abstract class PrismNodeState
{
    protected PrismNodeState(PrismNodeId id, string? name)
    {
        Id = id;
        Name = name;
    }

    public PrismNodeId Id { get; }

    public string? Name { get; }
}

public sealed class PrismCompositionState
{
    private readonly PrismStateAccess values;

    internal PrismCompositionState(PrismStateAccess values)
    {
        this.values = values;
    }

    public PrismColorProfile WorkingColorProfile
    {
        get => (PrismColorProfile)values.Get(
            PrismCatalogGenerated.PrismCompositionPropertyKeys.WorkingColorProfileKey);
        set => values.Set(
            PrismCatalogGenerated.PrismCompositionPropertyKeys.WorkingColorProfileKey,
            (int)value);
    }

    public float GlobalLightAngle
    {
        get => values.Get(PrismCatalogGenerated.PrismCompositionPropertyKeys.GlobalLightAngleKey);
        set => values.Set(
            PrismCatalogGenerated.PrismCompositionPropertyKeys.GlobalLightAngleKey,
            PrismRuntimeValidation.Finite(value, nameof(value)));
    }

    public float GlobalLightAltitude
    {
        get => values.Get(PrismCatalogGenerated.PrismCompositionPropertyKeys.GlobalLightAltitudeKey);
        set
        {
            if (!float.IsFinite(value) || value is < 0f or > 90f)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Light altitude must be from zero through 90.");
            }
            values.Set(PrismCatalogGenerated.PrismCompositionPropertyKeys.GlobalLightAltitudeKey, value);
        }
    }
}

public sealed class PrismLayerState : PrismNodeState
{
    private readonly PrismStateAccess values;

    internal PrismLayerState(
        PrismStateAccess values,
        PrismNodeId id,
        string? name,
        PrismFilterState[] filters,
        PrismStyleState[] styles,
        PrismMaskState? mask)
        : base(id, name)
    {
        this.values = values;
        Filters = filters;
        Styles = styles;
        Mask = mask;
    }

    public IReadOnlyList<PrismFilterState> Filters { get; }

    public IReadOnlyList<PrismStyleState> Styles { get; }

    public PrismMaskState? Mask { get; }

    public bool Visible
    {
        get => values.Get(PrismCatalogGenerated.PrismLayerPropertyKeys.VisibleKey);
        set => values.Set(PrismCatalogGenerated.PrismLayerPropertyKeys.VisibleKey, value);
    }

    public float Opacity
    {
        get => values.Get(PrismCatalogGenerated.PrismLayerPropertyKeys.OpacityKey);
        set => values.Set(
            PrismCatalogGenerated.PrismLayerPropertyKeys.OpacityKey,
            PrismRuntimeValidation.UnitInterval(value, nameof(value)));
    }

    public float Fill
    {
        get => values.Get(PrismCatalogGenerated.PrismLayerPropertyKeys.FillKey);
        set => values.Set(
            PrismCatalogGenerated.PrismLayerPropertyKeys.FillKey,
            PrismRuntimeValidation.UnitInterval(value, nameof(value)));
    }

    public PrismBlendMode BlendMode
    {
        get => (PrismBlendMode)values.Get(PrismCatalogGenerated.PrismLayerPropertyKeys.BlendModeKey);
        set
        {
            if (value == PrismBlendMode.PassThrough)
            {
                throw new ArgumentException("PassThrough is valid only for Prism groups.", nameof(value));
            }
            values.Set(PrismCatalogGenerated.PrismLayerPropertyKeys.BlendModeKey, (int)value);
        }
    }

    public bool ClipToBelow
    {
        get => values.Get(PrismCatalogGenerated.PrismLayerPropertyKeys.ClipToBelowKey);
        set => values.Set(PrismCatalogGenerated.PrismLayerPropertyKeys.ClipToBelowKey, value);
    }

    public PrismBlendChannels BlendChannels
    {
        get => (PrismBlendChannels)values.Get(
            PrismCatalogGenerated.PrismLayerPropertyKeys.BlendChannelsKey);
        set
        {
            if ((value & ~PrismBlendChannels.Rgba) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown Prism blend channels.");
            }
            values.Set(PrismCatalogGenerated.PrismLayerPropertyKeys.BlendChannelsKey, (int)value);
        }
    }

    public PrismKnockout Knockout
    {
        get => (PrismKnockout)values.Get(PrismCatalogGenerated.PrismLayerPropertyKeys.KnockoutKey);
        set
        {
            if ((uint)value > (uint)PrismKnockout.Deep)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown Prism knockout mode.");
            }
            values.Set(PrismCatalogGenerated.PrismLayerPropertyKeys.KnockoutKey, (int)value);
        }
    }

    public bool BlendInteriorStylesAsGroup
    {
        get => values.Get(
            PrismCatalogGenerated.PrismLayerPropertyKeys.BlendInteriorStylesAsGroupKey);
        set => values.Set(
            PrismCatalogGenerated.PrismLayerPropertyKeys.BlendInteriorStylesAsGroupKey,
            value);
    }

    public bool BlendClippedLayersAsGroup
    {
        get => values.Get(
            PrismCatalogGenerated.PrismLayerPropertyKeys.BlendClippedLayersAsGroupKey);
        set => values.Set(
            PrismCatalogGenerated.PrismLayerPropertyKeys.BlendClippedLayersAsGroupKey,
            value);
    }

    public bool TransparencyShapesLayer
    {
        get => values.Get(
            PrismCatalogGenerated.PrismLayerPropertyKeys.TransparencyShapesLayerKey);
        set => values.Set(
            PrismCatalogGenerated.PrismLayerPropertyKeys.TransparencyShapesLayerKey,
            value);
    }

    public bool LayerMaskHidesStyles
    {
        get => values.Get(
            PrismCatalogGenerated.PrismLayerPropertyKeys.LayerMaskHidesStylesKey);
        set => values.Set(
            PrismCatalogGenerated.PrismLayerPropertyKeys.LayerMaskHidesStylesKey,
            value);
    }

    public bool VectorMaskHidesStyles
    {
        get => values.Get(
            PrismCatalogGenerated.PrismLayerPropertyKeys.VectorMaskHidesStylesKey);
        set => values.Set(
            PrismCatalogGenerated.PrismLayerPropertyKeys.VectorMaskHidesStylesKey,
            value);
    }

    public PrismBlendIfChannel BlendIfChannel
    {
        get => (PrismBlendIfChannel)values.Get(
            PrismCatalogGenerated.PrismLayerPropertyKeys.BlendIfChannelKey);
        set
        {
            if ((uint)value > (uint)PrismBlendIfChannel.Blue)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown Blend If channel.");
            }
            values.Set(PrismCatalogGenerated.PrismLayerPropertyKeys.BlendIfChannelKey, (int)value);
        }
    }

    public PrismBlendRange ThisLayerRange
    {
        get => PrismBlendRange.FromVector4(
            values.Get(PrismCatalogGenerated.PrismLayerPropertyKeys.ThisLayerRangeKey));
        set => values.Set(
            PrismCatalogGenerated.PrismLayerPropertyKeys.ThisLayerRangeKey,
            value.ToVector4());
    }

    public PrismBlendRange UnderlyingRange
    {
        get => PrismBlendRange.FromVector4(
            values.Get(PrismCatalogGenerated.PrismLayerPropertyKeys.UnderlyingRangeKey));
        set => values.Set(
            PrismCatalogGenerated.PrismLayerPropertyKeys.UnderlyingRangeKey,
            value.ToVector4());
    }

    public int DissolveSeed
    {
        get => values.Get(PrismCatalogGenerated.PrismLayerPropertyKeys.DissolveSeedKey);
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Dissolve seed cannot be negative.");
            }
            values.Set(PrismCatalogGenerated.PrismLayerPropertyKeys.DissolveSeedKey, value);
        }
    }
}

public sealed class PrismGroupState : PrismNodeState
{
    private readonly PrismStateAccess values;

    internal PrismGroupState(
        PrismStateAccess values,
        PrismNodeId id,
        string? name,
        PrismNodeState[] children,
        PrismFilterState[] filters,
        PrismStyleState[] styles,
        PrismMaskState? mask)
        : base(id, name)
    {
        this.values = values;
        Children = children;
        Filters = filters;
        Styles = styles;
        Mask = mask;
    }

    public IReadOnlyList<PrismNodeState> Children { get; }

    public IReadOnlyList<PrismFilterState> Filters { get; }

    public IReadOnlyList<PrismStyleState> Styles { get; }

    public PrismMaskState? Mask { get; }

    public bool Visible
    {
        get => values.Get(PrismCatalogGenerated.PrismGroupPropertyKeys.VisibleKey);
        set => values.Set(PrismCatalogGenerated.PrismGroupPropertyKeys.VisibleKey, value);
    }

    public float Opacity
    {
        get => values.Get(PrismCatalogGenerated.PrismGroupPropertyKeys.OpacityKey);
        set => values.Set(
            PrismCatalogGenerated.PrismGroupPropertyKeys.OpacityKey,
            PrismRuntimeValidation.UnitInterval(value, nameof(value)));
    }

    public PrismBlendMode BlendMode
    {
        get => (PrismBlendMode)values.Get(PrismCatalogGenerated.PrismGroupPropertyKeys.BlendModeKey);
        set => values.Set(PrismCatalogGenerated.PrismGroupPropertyKeys.BlendModeKey, (int)value);
    }
}

public sealed class PrismBackdropState : PrismNodeState
{
    private readonly PrismStateAccess values;

    internal PrismBackdropState(
        PrismStateAccess values,
        PrismNodeId id,
        string? name,
        PrismFilterState[] filters,
        PrismStyleState[] styles,
        PrismMaskState? mask)
        : base(id, name)
    {
        this.values = values;
        Filters = filters;
        Styles = styles;
        Mask = mask;
    }

    public IReadOnlyList<PrismFilterState> Filters { get; }

    public IReadOnlyList<PrismStyleState> Styles { get; }

    public PrismMaskState? Mask { get; }

    public bool Visible
    {
        get => values.Get(PrismCatalogGenerated.PrismBackdropPropertyKeys.VisibleKey);
        set => values.Set(PrismCatalogGenerated.PrismBackdropPropertyKeys.VisibleKey, value);
    }

    public float Opacity
    {
        get => values.Get(PrismCatalogGenerated.PrismBackdropPropertyKeys.OpacityKey);
        set => values.Set(
            PrismCatalogGenerated.PrismBackdropPropertyKeys.OpacityKey,
            PrismRuntimeValidation.UnitInterval(value, nameof(value)));
    }
}

public sealed class PrismMaskState
{
    private readonly PrismStateAccess values;

    internal PrismMaskState(PrismStateAccess values)
    {
        this.values = values;
    }

    public PrismResourceId Image
    {
        get => values.Get(PrismCatalogGenerated.PrismMaskPropertyKeys.ImageKey);
        set => values.Set(PrismCatalogGenerated.PrismMaskPropertyKeys.ImageKey, value);
    }

    public PrismMaskChannel Channel
    {
        get => (PrismMaskChannel)values.Get(PrismCatalogGenerated.PrismMaskPropertyKeys.ChannelKey);
        set => values.Set(PrismCatalogGenerated.PrismMaskPropertyKeys.ChannelKey, (int)value);
    }

    public float Feather
    {
        get => values.Get(PrismCatalogGenerated.PrismMaskPropertyKeys.FeatherKey);
        set
        {
            if (!float.IsFinite(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Mask feather cannot be negative.");
            }
            values.Set(PrismCatalogGenerated.PrismMaskPropertyKeys.FeatherKey, value);
        }
    }

    public float Density
    {
        get => values.Get(PrismCatalogGenerated.PrismMaskPropertyKeys.DensityKey);
        set => values.Set(
            PrismCatalogGenerated.PrismMaskPropertyKeys.DensityKey,
            PrismRuntimeValidation.UnitInterval(value, nameof(value)));
    }

    public bool Invert
    {
        get => values.Get(PrismCatalogGenerated.PrismMaskPropertyKeys.InvertKey);
        set => values.Set(PrismCatalogGenerated.PrismMaskPropertyKeys.InvertKey, value);
    }
}

public sealed class PrismFilterState
{
    private readonly PrismStateAccess common;
    private readonly PrismStateAccess parameters;

    internal PrismFilterState(
        PrismFilterId filter,
        PrismStateAccess common,
        PrismStateAccess parameters)
    {
        Filter = filter;
        this.common = common;
        this.parameters = parameters;
    }

    public PrismFilterId Filter { get; }

    public bool Visible
    {
        get => common.Get(PrismCatalogGenerated.PrismFilterCommonParameterKeys.VisibleKey);
        set => common.Set(PrismCatalogGenerated.PrismFilterCommonParameterKeys.VisibleKey, value);
    }

    public float Opacity
    {
        get => common.Get(PrismCatalogGenerated.PrismFilterCommonParameterKeys.OpacityKey);
        set => common.Set(
            PrismCatalogGenerated.PrismFilterCommonParameterKeys.OpacityKey,
            PrismRuntimeValidation.UnitInterval(value, nameof(value)));
    }

    public PrismBlendMode BlendMode
    {
        get => (PrismBlendMode)common.Get(
            PrismCatalogGenerated.PrismFilterCommonParameterKeys.BlendModeKey);
        set
        {
            if (value == PrismBlendMode.PassThrough)
            {
                throw new ArgumentException("PassThrough is valid only for Prism groups.", nameof(value));
            }
            common.Set(PrismCatalogGenerated.PrismFilterCommonParameterKeys.BlendModeKey, (int)value);
        }
    }

    public T GetValue<T>(PrismCatalogParameterInfo parameter)
    {
        PrismRuntimeValidation.ValidateParameter<T>(parameter, PrismCatalogOperationKind.Filter, (int)Filter);
        object value = parameter.ValueKind switch
        {
            PrismCatalogValueKind.Boolean => GetValue(new PrismParameterKey<bool>((int)Filter, parameter.TypeSlot)),
            PrismCatalogValueKind.Integer => GetValue(new PrismParameterKey<int>((int)Filter, parameter.TypeSlot)),
            PrismCatalogValueKind.Number => GetValue(new PrismParameterKey<float>((int)Filter, parameter.TypeSlot)),
            PrismCatalogValueKind.Color => GetValue(new PrismParameterKey<Color>((int)Filter, parameter.TypeSlot)),
            PrismCatalogValueKind.Vector => GetValue(new PrismParameterKey<Vector4>((int)Filter, parameter.TypeSlot)),
            PrismCatalogValueKind.Symbol => parameter.ResolveSymbol(
                GetValue(new PrismParameterKey<int>((int)Filter, parameter.TypeSlot))),
            PrismCatalogValueKind.Resource => GetValue(
                new PrismParameterKey<PrismResourceId>((int)Filter, parameter.TypeSlot)),
            _ => throw new InvalidOperationException($"Unknown Prism catalog value kind '{parameter.ValueKind}'.")
        };
        return (T)value;
    }

    public void SetValue<T>(PrismCatalogParameterInfo parameter, T value)
    {
        PrismRuntimeValidation.ValidateParameter<T>(parameter, PrismCatalogOperationKind.Filter, (int)Filter);
        switch (parameter.ValueKind)
        {
            case PrismCatalogValueKind.Boolean:
                SetValue(new PrismParameterKey<bool>((int)Filter, parameter.TypeSlot), (bool)(object)value!);
                break;
            case PrismCatalogValueKind.Integer:
                SetValue(new PrismParameterKey<int>((int)Filter, parameter.TypeSlot), (int)(object)value!);
                break;
            case PrismCatalogValueKind.Number:
                SetValue(new PrismParameterKey<float>((int)Filter, parameter.TypeSlot), (float)(object)value!);
                break;
            case PrismCatalogValueKind.Color:
                SetValue(new PrismParameterKey<Color>((int)Filter, parameter.TypeSlot), (Color)(object)value!);
                break;
            case PrismCatalogValueKind.Vector:
                SetValue(new PrismParameterKey<Vector4>((int)Filter, parameter.TypeSlot), (Vector4)(object)value!);
                break;
            case PrismCatalogValueKind.Symbol:
                SetValue(
                    new PrismParameterKey<int>((int)Filter, parameter.TypeSlot),
                    parameter.ResolveSymbol((string)(object)value!));
                break;
            case PrismCatalogValueKind.Resource:
                SetValue(
                    new PrismParameterKey<PrismResourceId>((int)Filter, parameter.TypeSlot),
                    (PrismResourceId)(object)value!);
                break;
            default:
                throw new InvalidOperationException($"Unknown Prism catalog value kind '{parameter.ValueKind}'.");
        }
    }

    internal bool GetValue(PrismParameterKey<bool> key) => parameters.Get(key);

    internal int GetValue(PrismParameterKey<int> key) => parameters.Get(key);

    internal float GetValue(PrismParameterKey<float> key) => parameters.Get(key);

    internal Color GetValue(PrismParameterKey<Color> key) => parameters.Get(key);

    internal Vector4 GetValue(PrismParameterKey<Vector4> key) => parameters.Get(key);

    internal PrismResourceId GetValue(PrismParameterKey<PrismResourceId> key) =>
        parameters.Get(key);

    internal void SetValue(PrismParameterKey<bool> key, bool value)
    {
        PrismCatalogParameterValidation.Validate(key, value);
        parameters.Set(key, value);
    }

    internal void SetValue(PrismParameterKey<int> key, int value)
    {
        PrismCatalogParameterValidation.Validate(key, value);
        parameters.Set(key, value);
    }

    internal void SetValue(PrismParameterKey<float> key, float value)
    {
        PrismCatalogParameterValidation.Validate(key, value);
        parameters.Set(key, value);
    }

    internal void SetValue(PrismParameterKey<Color> key, Color value)
    {
        PrismCatalogParameterValidation.Validate(key, value);
        parameters.Set(key, value);
    }

    internal void SetValue(PrismParameterKey<Vector4> key, Vector4 value)
    {
        PrismCatalogParameterValidation.Validate(key, value);
        parameters.Set(key, value);
    }

    internal void SetValue(PrismParameterKey<PrismResourceId> key, PrismResourceId value)
    {
        PrismCatalogParameterValidation.Validate(key, value);
        parameters.Set(key, value);
    }
}

public sealed class PrismStyleState
{
    private readonly PrismStateAccess common;
    private readonly PrismStateAccess parameters;

    internal PrismStyleState(
        PrismStyleId style,
        PrismStateAccess common,
        PrismStateAccess parameters)
    {
        Style = style;
        this.common = common;
        this.parameters = parameters;
    }

    public PrismStyleId Style { get; }

    public bool Visible
    {
        get => common.Get(PrismCatalogGenerated.PrismStyleCommonParameterKeys.VisibleKey);
        set => common.Set(PrismCatalogGenerated.PrismStyleCommonParameterKeys.VisibleKey, value);
    }

    public T GetValue<T>(PrismCatalogParameterInfo parameter)
    {
        PrismRuntimeValidation.ValidateParameter<T>(parameter, PrismCatalogOperationKind.Style, (int)Style);
        object value = parameter.ValueKind switch
        {
            PrismCatalogValueKind.Boolean => GetValue(new PrismParameterKey<bool>((int)Style, parameter.TypeSlot)),
            PrismCatalogValueKind.Integer => GetValue(new PrismParameterKey<int>((int)Style, parameter.TypeSlot)),
            PrismCatalogValueKind.Number => GetValue(new PrismParameterKey<float>((int)Style, parameter.TypeSlot)),
            PrismCatalogValueKind.Color => GetValue(new PrismParameterKey<Color>((int)Style, parameter.TypeSlot)),
            PrismCatalogValueKind.Vector => GetValue(new PrismParameterKey<Vector4>((int)Style, parameter.TypeSlot)),
            PrismCatalogValueKind.Symbol => parameter.ResolveSymbol(
                GetValue(new PrismParameterKey<int>((int)Style, parameter.TypeSlot))),
            PrismCatalogValueKind.Resource => GetValue(
                new PrismParameterKey<PrismResourceId>((int)Style, parameter.TypeSlot)),
            _ => throw new InvalidOperationException($"Unknown Prism catalog value kind '{parameter.ValueKind}'.")
        };
        return (T)value;
    }

    public void SetValue<T>(PrismCatalogParameterInfo parameter, T value)
    {
        PrismRuntimeValidation.ValidateParameter<T>(parameter, PrismCatalogOperationKind.Style, (int)Style);
        switch (parameter.ValueKind)
        {
            case PrismCatalogValueKind.Boolean:
                SetValue(new PrismParameterKey<bool>((int)Style, parameter.TypeSlot), (bool)(object)value!);
                break;
            case PrismCatalogValueKind.Integer:
                SetValue(new PrismParameterKey<int>((int)Style, parameter.TypeSlot), (int)(object)value!);
                break;
            case PrismCatalogValueKind.Number:
                SetValue(new PrismParameterKey<float>((int)Style, parameter.TypeSlot), (float)(object)value!);
                break;
            case PrismCatalogValueKind.Color:
                SetValue(new PrismParameterKey<Color>((int)Style, parameter.TypeSlot), (Color)(object)value!);
                break;
            case PrismCatalogValueKind.Vector:
                SetValue(new PrismParameterKey<Vector4>((int)Style, parameter.TypeSlot), (Vector4)(object)value!);
                break;
            case PrismCatalogValueKind.Symbol:
                SetValue(
                    new PrismParameterKey<int>((int)Style, parameter.TypeSlot),
                    parameter.ResolveSymbol((string)(object)value!));
                break;
            case PrismCatalogValueKind.Resource:
                SetValue(
                    new PrismParameterKey<PrismResourceId>((int)Style, parameter.TypeSlot),
                    (PrismResourceId)(object)value!);
                break;
            default:
                throw new InvalidOperationException($"Unknown Prism catalog value kind '{parameter.ValueKind}'.");
        }
    }

    internal bool GetValue(PrismParameterKey<bool> key) => parameters.Get(key);

    internal int GetValue(PrismParameterKey<int> key) => parameters.Get(key);

    internal float GetValue(PrismParameterKey<float> key) => parameters.Get(key);

    internal Color GetValue(PrismParameterKey<Color> key) => parameters.Get(key);

    internal Vector4 GetValue(PrismParameterKey<Vector4> key) => parameters.Get(key);

    internal PrismResourceId GetValue(PrismParameterKey<PrismResourceId> key) =>
        parameters.Get(key);

    internal void SetValue(PrismParameterKey<bool> key, bool value)
    {
        PrismCatalogParameterValidation.Validate(key, value);
        parameters.Set(key, value);
    }

    internal void SetValue(PrismParameterKey<int> key, int value)
    {
        PrismCatalogParameterValidation.Validate(key, value);
        parameters.Set(key, value);
    }

    internal void SetValue(PrismParameterKey<float> key, float value)
    {
        PrismCatalogParameterValidation.Validate(key, value);
        parameters.Set(key, value);
    }

    internal void SetValue(PrismParameterKey<Color> key, Color value)
    {
        PrismCatalogParameterValidation.Validate(key, value);
        parameters.Set(key, value);
    }

    internal void SetValue(PrismParameterKey<Vector4> key, Vector4 value)
    {
        PrismCatalogParameterValidation.Validate(key, value);
        parameters.Set(key, value);
    }

    internal void SetValue(PrismParameterKey<PrismResourceId> key, PrismResourceId value)
    {
        PrismCatalogParameterValidation.Validate(key, value);
        parameters.Set(key, value);
    }
}

internal static class PrismRuntimeValidation
{
    public static void ValidateParameter<T>(
        PrismCatalogParameterInfo parameter,
        PrismCatalogOperationKind operationKind,
        int operationStableId)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        if (parameter.OperationKind != operationKind || parameter.OperationStableId != operationStableId)
        {
            throw new ArgumentException("The Prism catalog parameter belongs to a different operation.", nameof(parameter));
        }

        Type expected = parameter.ValueKind switch
        {
            PrismCatalogValueKind.Boolean => typeof(bool),
            PrismCatalogValueKind.Integer => typeof(int),
            PrismCatalogValueKind.Number => typeof(float),
            PrismCatalogValueKind.Color => typeof(Color),
            PrismCatalogValueKind.Vector => typeof(Vector4),
            PrismCatalogValueKind.Symbol => typeof(string),
            PrismCatalogValueKind.Resource => typeof(PrismResourceId),
            _ => throw new InvalidOperationException($"Unknown Prism catalog value kind '{parameter.ValueKind}'.")
        };
        if (typeof(T) != expected)
        {
            throw new ArgumentException(
                $"Prism parameter '{parameter.Name}' requires values of type '{expected.Name}', not '{typeof(T).Name}'.",
                nameof(parameter));
        }
    }

    public static float UnitInterval(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value is < 0f or > 1f)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Prism values must be finite values from zero through one.");
        }

        return value;
    }

    public static float Finite(float value, string parameterName)
    {
        if (!float.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Prism values must be finite.");
        }

        return value;
    }
}
