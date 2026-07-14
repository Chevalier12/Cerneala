using System.Globalization;
using Cerneala.UI.Core;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Relay;

namespace Cerneala.UI.Markup;

public static partial class GeneratedMarkup
{
    public static Binding AttachPropertyBinding<T>(
        UIElement owner,
        UiObject target,
        UiProperty<T> targetProperty,
        MarkupObservation observation,
        BindingMode mode,
        Func<object?, T> projection,
        string description)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(projection);

        MarkupPropertyBindingController<T> controller = new(
            owner,
            target,
            targetProperty,
            [observation],
            () => observation.IsResolved,
            () => projection(observation.Value),
            mode == BindingMode.TwoWay ? observation : null,
            mode,
            UiPropertyValueSource.MarkupBase,
            description);
        controller.Attach();
        owner.AddLifecycleBehavior(controller);
        return controller;
    }

    public static Binding AttachInterpolatedStringBinding(
        UIElement owner,
        UiObject target,
        UiProperty<string> targetProperty,
        IReadOnlyList<MarkupObservation> observations,
        Func<string> compose,
        string description)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(compose);

        MarkupPropertyBindingController<string> controller = new(
            owner,
            target,
            targetProperty,
            observations,
            static () => true,
            compose,
            null,
            BindingMode.OneWay,
            UiPropertyValueSource.MarkupBase,
            description);
        controller.Attach();
        owner.AddLifecycleBehavior(controller);
        return controller;
    }

    public static MarkupConditionalValue CreateConditionalPropertyBinding<T>(
        UiObject target,
        UiProperty<T> targetProperty,
        MarkupObservation observation,
        BindingMode mode,
        Func<object?, T> projection,
        string description)
    {
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(projection);

        MarkupPropertyBindingController<T> controller = new(
            null,
            target,
            targetProperty,
            [observation],
            () => observation.IsResolved,
            () => projection(observation.Value),
            mode == BindingMode.TwoWay ? observation : null,
            mode,
            UiPropertyValueSource.MarkupConditional,
            description);
        return new MarkupConditionalValue(target, targetProperty, controller);
    }

    public static MarkupConditionalValue CreateConditionalInterpolatedStringBinding(
        UiObject target,
        UiProperty<string> targetProperty,
        IReadOnlyList<MarkupObservation> observations,
        Func<string> compose,
        string description)
    {
        ArgumentNullException.ThrowIfNull(compose);

        MarkupPropertyBindingController<string> controller = new(
            null,
            target,
            targetProperty,
            observations,
            static () => true,
            compose,
            null,
            BindingMode.OneWay,
            UiPropertyValueSource.MarkupConditional,
            description);
        return new MarkupConditionalValue(target, targetProperty, controller);
    }

    public static string FormatStringValue(object? value)
    {
        return Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
    }
}

internal interface IMarkupConditionalValueProvider : IDisposable
{
    void Activate(UIElement owner);

    void Deactivate();
}

internal sealed class MarkupPropertyBindingController<T> : Binding, IElementLifecycleBehavior, IMarkupConditionalValueProvider
{
    private UIElement? lifecycleOwner;
    private readonly UiObject target;
    private readonly UiProperty<T> targetProperty;
    private readonly IReadOnlyList<MarkupObservation> observations;
    private readonly Func<bool> canRead;
    private readonly Func<T> readValue;
    private readonly MarkupObservation? writeEndpoint;
    private readonly BindingMode mode;
    private readonly UiPropertyValueSource targetSource;
    private readonly string description;
    private EventHandler? observationChangedHandler;
    private EventHandler<UiPropertyChangedEventArgs>? targetChangedHandler;
    private Func<bool>? callbackGuard;
    private readonly UiRelayRefreshDispatcher refreshDispatcher;
    private int activationVersion;
    private bool active;
    private bool updatingTarget;
    private bool updatingSource;

    public MarkupPropertyBindingController(
        UIElement? lifecycleOwner,
        UiObject target,
        UiProperty<T> targetProperty,
        IReadOnlyList<MarkupObservation> observations,
        Func<bool> canRead,
        Func<T> readValue,
        MarkupObservation? writeEndpoint,
        BindingMode mode,
        UiPropertyValueSource targetSource,
        string description)
    {
        this.lifecycleOwner = lifecycleOwner;
        this.target = target ?? throw new ArgumentNullException(nameof(target));
        this.targetProperty = targetProperty ?? throw new ArgumentNullException(nameof(targetProperty));
        this.observations = observations?.Distinct().ToArray() ?? throw new ArgumentNullException(nameof(observations));
        this.canRead = canRead ?? throw new ArgumentNullException(nameof(canRead));
        this.readValue = readValue ?? throw new ArgumentNullException(nameof(readValue));
        this.writeEndpoint = writeEndpoint;
        this.mode = mode;
        this.targetSource = targetSource;
        this.description = string.IsNullOrWhiteSpace(description)
            ? targetProperty.DiagnosticName
            : description;
        refreshDispatcher = new UiRelayRefreshDispatcher(ResolveRelay, RefreshFromRelay, this.description);

        if (this.observations.Count == 0)
        {
            throw new ArgumentException("At least one markup observation is required.", nameof(observations));
        }

        if (targetProperty.IsReadOnly)
        {
            throw new InvalidOperationException($"UI property '{targetProperty.DiagnosticName}' is read-only.");
        }

        if (mode is not BindingMode.OneWay and not BindingMode.TwoWay)
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        if (mode == BindingMode.TwoWay && (writeEndpoint is null || !writeEndpoint.IsWritable))
        {
            throw new InvalidOperationException($"Markup binding '{this.description}' requires a writable source endpoint.");
        }

        if (targetSource is not UiPropertyValueSource.MarkupBase and not UiPropertyValueSource.MarkupConditional)
        {
            throw new ArgumentOutOfRangeException(nameof(targetSource));
        }
    }

    public void Attach()
    {
        if (active)
        {
            DeactivateCore(clearOwnedValue: false);
        }

        ActivateCore();
    }

    public void Detach()
    {
        DeactivateCore(clearOwnedValue: false);
    }

    public void Activate(UIElement owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (lifecycleOwner is not null && !ReferenceEquals(lifecycleOwner, owner))
        {
            throw new InvalidOperationException($"Markup binding '{description}' cannot change its lifecycle owner.");
        }

        lifecycleOwner = owner;
        ActivateCore();
    }

    public void Deactivate()
    {
        DeactivateCore(clearOwnedValue: true);
    }

    protected override void DisposeCore()
    {
        DeactivateCore(clearOwnedValue: true);
        lifecycleOwner?.RemoveLifecycleBehavior(this);
    }

    private void ActivateCore()
    {
        if (active || IsDisposed)
        {
            return;
        }

        active = true;
        int version = ++activationVersion;

        try
        {
            callbackGuard = refreshDispatcher.Activate();
            observationChangedHandler = (_, _) => OnObservationChanged(version);
            targetChangedHandler = (_, args) => OnTargetPropertyChanged(version, args);
            foreach (MarkupObservation observation in observations)
            {
                observation.CallbackGuard = callbackGuard;
                observation.Changed += observationChangedHandler;
                observation.Start();
            }

            if (mode == BindingMode.TwoWay)
            {
                target.PropertyChanged += targetChangedHandler;
            }

            RefreshTarget();
        }
        catch
        {
            DeactivateCore(clearOwnedValue: true);
            throw;
        }
    }

    private void DeactivateCore(bool clearOwnedValue)
    {
        if (active)
        {
            active = false;
            activationVersion++;
            if (targetChangedHandler is not null)
            {
                target.PropertyChanged -= targetChangedHandler;
            }

            foreach (MarkupObservation observation in observations)
            {
                if (observationChangedHandler is not null)
                {
                    observation.Changed -= observationChangedHandler;
                }

                if (ReferenceEquals(observation.CallbackGuard, callbackGuard))
                {
                    observation.CallbackGuard = null;
                }

                observation.Stop();
            }

            observationChangedHandler = null;
            targetChangedHandler = null;
            callbackGuard = null;
            refreshDispatcher.Deactivate();
        }

        if (clearOwnedValue)
        {
            ClearOwnedValue();
        }
    }

    private void OnObservationChanged(int version)
    {
        if (!active || version != activationVersion || updatingSource || IsDisposed)
        {
            return;
        }

        RefreshTarget();
    }

    private void OnTargetPropertyChanged(int version, UiPropertyChangedEventArgs args)
    {
        if (!active || version != activationVersion || IsDisposed || updatingTarget || updatingSource ||
            mode != BindingMode.TwoWay || !ReferenceEquals(args.Property, targetProperty) ||
            args.ValueSource != UiPropertyValueSource.Local)
        {
            return;
        }

        updatingSource = true;
        try
        {
            writeEndpoint!.TryWrite(args.NewValue);
            RefreshTarget();
            target.ClearValue(targetProperty, UiPropertyValueSource.Local);
        }
        finally
        {
            updatingSource = false;
        }
    }

    private void RefreshTarget()
    {
        if (!active)
        {
            return;
        }

        if (!canRead())
        {
            ClearOwnedValue();
            return;
        }

        T value = readValue();
        updatingTarget = true;
        try
        {
            target.SetValue(targetProperty, value, targetSource);
        }
        finally
        {
            updatingTarget = false;
        }
    }

    private void ClearOwnedValue()
    {
        updatingTarget = true;
        try
        {
            target.ClearValue(targetProperty, targetSource);
        }
        finally
        {
            updatingTarget = false;
        }
    }

    private void RefreshFromRelay()
    {
        if (!active || IsDisposed)
        {
            return;
        }

        foreach (MarkupObservation observation in observations)
        {
            observation.RefreshValue();
        }

        RefreshTarget();
    }

    private UiRelay? ResolveRelay()
    {
        UiRelay? ownerRelay = lifecycleOwner?.Root?.Relay;
        UiRelay? targetRelay = (target as UIElement)?.Root?.Relay;
        if (ownerRelay is not null && targetRelay is not null && !ReferenceEquals(ownerRelay, targetRelay))
        {
            throw new InvalidOperationException(
                $"Markup binding '{description}' cannot span UI roots with different Relay instances.");
        }

        return ownerRelay ?? targetRelay;
    }
}
