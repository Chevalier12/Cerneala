using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Data;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Relay;

namespace Cerneala.UI.Markup;

public static partial class GeneratedMarkup
{
    public static IDisposable AttachPrism(
        UIElement owner,
        Func<PrismInstance> instanceFactory)
    {
        return AttachPrism(
            owner,
            instanceFactory,
            Array.Empty<Func<PrismInstance, IDisposable>>());
    }

    public static IDisposable AttachPrism(
        UIElement owner,
        Func<PrismInstance> instanceFactory,
        IReadOnlyList<Func<PrismInstance, IDisposable>> bindingFactories)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(instanceFactory);
        ArgumentNullException.ThrowIfNull(bindingFactories);
        if (bindingFactories.Any(factory => factory is null))
        {
            throw new ArgumentException("Prism binding factories cannot contain null.", nameof(bindingFactories));
        }

        return PrismAttachment.Set(owner, instanceFactory, bindingFactories);
    }

    public static IDisposable AttachPrismValueBinding<T>(
        UIElement owner,
        PrismInstance instance,
        MarkupObservation observation,
        Func<PrismInstance, T> getValue,
        Action<PrismInstance, T> setValue,
        BindingMode mode,
        Func<object?, T> projection,
        string description)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(getValue);
        ArgumentNullException.ThrowIfNull(setValue);
        ArgumentNullException.ThrowIfNull(projection);

        PrismValueBindingController<T> controller = new(
            owner,
            instance,
            observation,
            getValue,
            setValue,
            mode,
            projection,
            description);
        controller.Attach();
        return controller;
    }

    public static IDisposable ApplyPrismValueReference<T>(
        PrismInstance instance,
        MarkupObservation observation,
        Action<PrismInstance, T> setValue,
        Func<object?, T> projection)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(setValue);
        ArgumentNullException.ThrowIfNull(projection);

        observation.Start();
        try
        {
            setValue(instance, projection(observation.Value));
        }
        finally
        {
            observation.Stop();
        }

        return EmptyPrismValueReferenceLifetime.Instance;
    }

    public static bool TryGetPrismInstance(UIElement owner, out PrismInstance? instance)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return PrismAttachment.TryGetInstance(owner, out instance);
    }

    public static PrismInstance GetPrismInstance(UIElement owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return PrismAttachment.TryGetInstance(owner, out PrismInstance? instance)
            ? instance!
            : throw new InvalidOperationException("The element has no attached Prism instance.");
    }

    public static void SetPrismMotionProperty<T>(
        UIElement target,
        Func<PrismInstance, T> getValue,
        Action<PrismInstance, T> setValue,
        T value)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(getValue);
        ArgumentNullException.ThrowIfNull(setValue);
        PrismInstance instance = GetPrismInstance(target);
        _ = getValue(instance);
        setValue(instance, value);
    }

    public static bool GetPrismFilterBoolean(
        PrismFilterState state,
        int entryStableId,
        int slot) =>
        state.GetValue(new PrismParameterKey<bool>(entryStableId, slot));

    public static int GetPrismFilterInteger(
        PrismFilterState state,
        int entryStableId,
        int slot) =>
        state.GetValue(new PrismParameterKey<int>(entryStableId, slot));

    public static float GetPrismFilterNumber(
        PrismFilterState state,
        int entryStableId,
        int slot) =>
        state.GetValue(new PrismParameterKey<float>(entryStableId, slot));

    public static Color GetPrismFilterColor(
        PrismFilterState state,
        int entryStableId,
        int slot) =>
        state.GetValue(new PrismParameterKey<Color>(entryStableId, slot));

    public static Vector4 GetPrismFilterVector(
        PrismFilterState state,
        int entryStableId,
        int slot) =>
        state.GetValue(new PrismParameterKey<Vector4>(entryStableId, slot));

    public static PrismResourceId GetPrismFilterResource(
        PrismFilterState state,
        int entryStableId,
        int slot) =>
        state.GetValue(new PrismParameterKey<PrismResourceId>(entryStableId, slot));

    public static void SetPrismFilterBoolean(
        PrismFilterState state,
        int entryStableId,
        int slot,
        bool value) =>
        state.SetValue(new PrismParameterKey<bool>(entryStableId, slot), value);

    public static void SetPrismFilterInteger(
        PrismFilterState state,
        int entryStableId,
        int slot,
        int value) =>
        state.SetValue(new PrismParameterKey<int>(entryStableId, slot), value);

    public static void SetPrismFilterNumber(
        PrismFilterState state,
        int entryStableId,
        int slot,
        float value) =>
        state.SetValue(new PrismParameterKey<float>(entryStableId, slot), value);

    public static void SetPrismFilterColor(
        PrismFilterState state,
        int entryStableId,
        int slot,
        Color value) =>
        state.SetValue(new PrismParameterKey<Color>(entryStableId, slot), value);

    public static void SetPrismFilterVector(
        PrismFilterState state,
        int entryStableId,
        int slot,
        Vector4 value) =>
        state.SetValue(new PrismParameterKey<Vector4>(entryStableId, slot), value);

    public static void SetPrismFilterResource(
        PrismFilterState state,
        int entryStableId,
        int slot,
        PrismResourceId value) =>
        state.SetValue(new PrismParameterKey<PrismResourceId>(entryStableId, slot), value);

    public static bool GetPrismStyleBoolean(
        PrismStyleState state,
        int entryStableId,
        int slot) =>
        state.GetValue(new PrismParameterKey<bool>(entryStableId, slot));

    public static int GetPrismStyleInteger(
        PrismStyleState state,
        int entryStableId,
        int slot) =>
        state.GetValue(new PrismParameterKey<int>(entryStableId, slot));

    public static float GetPrismStyleNumber(
        PrismStyleState state,
        int entryStableId,
        int slot) =>
        state.GetValue(new PrismParameterKey<float>(entryStableId, slot));

    public static Color GetPrismStyleColor(
        PrismStyleState state,
        int entryStableId,
        int slot) =>
        state.GetValue(new PrismParameterKey<Color>(entryStableId, slot));

    public static Vector4 GetPrismStyleVector(
        PrismStyleState state,
        int entryStableId,
        int slot) =>
        state.GetValue(new PrismParameterKey<Vector4>(entryStableId, slot));

    public static PrismResourceId GetPrismStyleResource(
        PrismStyleState state,
        int entryStableId,
        int slot) =>
        state.GetValue(new PrismParameterKey<PrismResourceId>(entryStableId, slot));

    public static void SetPrismStyleBoolean(
        PrismStyleState state,
        int entryStableId,
        int slot,
        bool value) =>
        state.SetValue(new PrismParameterKey<bool>(entryStableId, slot), value);

    public static void SetPrismStyleInteger(
        PrismStyleState state,
        int entryStableId,
        int slot,
        int value) =>
        state.SetValue(new PrismParameterKey<int>(entryStableId, slot), value);

    public static void SetPrismStyleNumber(
        PrismStyleState state,
        int entryStableId,
        int slot,
        float value) =>
        state.SetValue(new PrismParameterKey<float>(entryStableId, slot), value);

    public static void SetPrismStyleColor(
        PrismStyleState state,
        int entryStableId,
        int slot,
        Color value) =>
        state.SetValue(new PrismParameterKey<Color>(entryStableId, slot), value);

    public static void SetPrismStyleVector(
        PrismStyleState state,
        int entryStableId,
        int slot,
        Vector4 value) =>
        state.SetValue(new PrismParameterKey<Vector4>(entryStableId, slot), value);

    public static void SetPrismStyleResource(
        PrismStyleState state,
        int entryStableId,
        int slot,
        PrismResourceId value) =>
        state.SetValue(new PrismParameterKey<PrismResourceId>(entryStableId, slot), value);
}

internal sealed class PrismValueBindingController<T> : IDisposable
{
    private readonly UIElement owner;
    private readonly PrismInstance instance;
    private readonly MarkupObservation observation;
    private readonly Func<PrismInstance, T> getValue;
    private readonly Action<PrismInstance, T> setValue;
    private readonly BindingMode mode;
    private readonly Func<object?, T> projection;
    private readonly UiRelayRefreshDispatcher refreshDispatcher;
    private EventHandler? observationChangedHandler;
    private EventHandler? valueChangedHandler;
    private Func<bool>? callbackGuard;
    private bool updatingTarget;
    private bool updatingSource;
    private bool disposed;

    public PrismValueBindingController(
        UIElement owner,
        PrismInstance instance,
        MarkupObservation observation,
        Func<PrismInstance, T> getValue,
        Action<PrismInstance, T> setValue,
        BindingMode mode,
        Func<object?, T> projection,
        string description)
    {
        this.owner = owner;
        this.instance = instance;
        this.observation = observation;
        this.getValue = getValue;
        this.setValue = setValue;
        this.mode = mode;
        this.projection = projection;
        refreshDispatcher = new UiRelayRefreshDispatcher(
            () => owner.Root?.Relay,
            RefreshFromRelay,
            string.IsNullOrWhiteSpace(description) ? "Prism markup binding" : description);

        if (mode is not BindingMode.OneWay and not BindingMode.TwoWay)
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        if (mode == BindingMode.TwoWay && !observation.IsWritable)
        {
            throw new InvalidOperationException("A TwoWay Prism binding requires a writable source endpoint.");
        }
    }

    public void Attach()
    {
        callbackGuard = refreshDispatcher.Activate();
        observationChangedHandler = (_, _) => RefreshTarget();
        observation.CallbackGuard = callbackGuard;
        observation.Changed += observationChangedHandler;
        observation.Start();

        if (mode == BindingMode.TwoWay)
        {
            valueChangedHandler = (_, _) => RefreshSource();
            instance.ValueChanged += valueChangedHandler;
        }

        RefreshTarget();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (observationChangedHandler is not null)
        {
            observation.Changed -= observationChangedHandler;
        }

        if (valueChangedHandler is not null)
        {
            instance.ValueChanged -= valueChangedHandler;
        }

        if (ReferenceEquals(observation.CallbackGuard, callbackGuard))
        {
            observation.CallbackGuard = null;
        }

        observation.Stop();
        refreshDispatcher.Deactivate();
    }

    private void RefreshTarget()
    {
        if (disposed || updatingSource || !observation.IsResolved)
        {
            return;
        }

        T value = projection(observation.Value);
        if (EqualityComparer<T>.Default.Equals(getValue(instance), value))
        {
            return;
        }

        updatingTarget = true;
        try
        {
            setValue(instance, value);
        }
        finally
        {
            updatingTarget = false;
        }
    }

    private void RefreshSource()
    {
        if (disposed || updatingTarget || updatingSource || mode != BindingMode.TwoWay)
        {
            return;
        }

        T value = getValue(instance);
        if (observation.IsResolved &&
            EqualityComparer<T>.Default.Equals(projection(observation.Value), value))
        {
            return;
        }

        updatingSource = true;
        try
        {
            observation.TryWrite(value);
        }
        finally
        {
            updatingSource = false;
        }
    }

    private void RefreshFromRelay()
    {
        if (disposed)
        {
            return;
        }

        observation.RefreshValue();
        RefreshTarget();
    }
}

internal sealed class EmptyPrismValueReferenceLifetime : IDisposable
{
    public static EmptyPrismValueReferenceLifetime Instance { get; } = new();

    public void Dispose()
    {
    }
}
