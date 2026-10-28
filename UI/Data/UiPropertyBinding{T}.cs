using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Relay;

namespace Cerneala.UI.Data;

public sealed class UiPropertyBinding<T> : Binding, IElementDataLifecycleBehavior
{
    private readonly ObservableValue<T> source;
    private readonly UiObject target;
    private readonly UiProperty<T> targetProperty;
    private readonly BindingMode mode;
    private readonly UiRelay? explicitRelay;
    private readonly UIElement? targetElement;
    private readonly UiRelayRefreshDispatcher refreshDispatcher;
    private Func<bool>? sourceCallbackGuard;
    private bool active;
    private bool updating;

    public UiPropertyBinding(
        UiObject target,
        UiProperty<T> targetProperty,
        ObservableValue<T> source,
        BindingMode mode = BindingMode.OneWay)
        : this(target, targetProperty, source, mode, explicitRelay: null)
    {
    }

    internal UiPropertyBinding(
        UiObject target,
        UiProperty<T> targetProperty,
        ObservableValue<T> source,
        BindingMode mode,
        UiRelay? explicitRelay)
    {
        this.target = target ?? throw new ArgumentNullException(nameof(target));
        this.targetProperty = targetProperty ?? throw new ArgumentNullException(nameof(targetProperty));
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        this.mode = mode;
        this.explicitRelay = explicitRelay;
        targetElement = target as UIElement;
        refreshDispatcher = new UiRelayRefreshDispatcher(ResolveRelay, RefreshFromSource, targetProperty.DiagnosticName);

        if (targetProperty.IsReadOnly)
        {
            throw new InvalidOperationException($"UI property '{targetProperty.DiagnosticName}' is read-only.");
        }

        if (mode is not BindingMode.OneWay and not BindingMode.TwoWay)
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        ActivateCore();
        targetElement?.AddLifecycleBehavior(this);
    }

    public BindingMode Mode => mode;

    void IElementLifecycleBehavior.ValidateRoot(UIRoot root) => ValidateRelay(root.Relay);

    void IElementDataLifecycleBehavior.ValidateRelay(UiRelay relay) => ValidateRelay(relay);

    private void ValidateRelay(UiRelay relay)
    {
        if (explicitRelay is not null && !ReferenceEquals(explicitRelay, relay))
        {
            throw new InvalidOperationException(
                $"Binding target '{targetProperty.DiagnosticName}' is attaching to an owner with a different Relay.");
        }
    }

    void IElementLifecycleBehavior.Attach()
    {
        DeactivateCore();
        ActivateCore();
    }

    void IElementLifecycleBehavior.Detach()
    {
        DeactivateCore();
    }

    protected override void DisposeCore()
    {
        DeactivateCore();
        targetElement?.RemoveLifecycleBehavior(this);
    }

    private void ActivateCore()
    {
        if (active || IsDisposed)
        {
            return;
        }

        sourceCallbackGuard = refreshDispatcher.Activate();
        active = true;
        source.ValueChanged -= OnSourceValueChanged;
        source.ValueChanged += OnSourceValueChanged;
        if (mode == BindingMode.TwoWay)
        {
            target.PropertyChanged -= OnTargetPropertyChanged;
            target.PropertyChanged += OnTargetPropertyChanged;
        }

        try
        {
            // Activating again, for example when the target is attached again, transfers no new source value. When the
            // target already holds the source value as its effective local value, the binding writes nothing, because
            // a write of the current value is not a no-op for every property: Selector.SelectedIndex selects it.
            T value = source.Value;
            if (target.GetValueSource(targetProperty) != UiPropertyValueSource.Local ||
                !targetProperty.Metadata.EqualityComparer.Equals(target.GetValue(targetProperty), value))
            {
                WriteTarget(value);
            }
        }
        catch
        {
            DeactivateCore();
            throw;
        }
    }

    private void DeactivateCore()
    {
        if (!active)
        {
            return;
        }

        active = false;
        refreshDispatcher.Deactivate();
        sourceCallbackGuard = null;
        source.ValueChanged -= OnSourceValueChanged;
        if (mode == BindingMode.TwoWay)
        {
            target.PropertyChanged -= OnTargetPropertyChanged;
        }
    }

    private void OnSourceValueChanged(object? sender, ObservableValueChangedEventArgs<T> args)
    {
        if (IsDisposed || !active)
        {
            return;
        }

        if (sourceCallbackGuard?.Invoke() == true && !updating)
        {
            // A ValueChanged handler can change the source again before this binding sees the outer notification, so
            // args.NewValue can be stale; the target always takes the value the source holds now.
            WriteTarget(source.Value);
        }
    }

    private void OnTargetPropertyChanged(object? sender, UiPropertyChangedEventArgs args)
    {
        if (IsDisposed || updating || !ReferenceEquals(args.Property, targetProperty))
        {
            return;
        }

        updating = true;
        try
        {
            source.SetValue((T)args.NewValue!);
        }
        finally
        {
            updating = false;
        }

        // The update flag hid the source notifications raised during that write. When the source kept a different value,
        // for example because a ValueChanged handler normalized the committed one, the target takes that value once.
        if (active && !IsDisposed)
        {
            T value = source.Value;
            if (!targetProperty.Metadata.EqualityComparer.Equals(target.GetValue(targetProperty), value))
            {
                WriteTarget(value);
            }
        }
    }

    private void WriteTarget(T value)
    {
        updating = true;
        try
        {
            target.SetValue(targetProperty, value);
        }
        finally
        {
            updating = false;
        }
    }

    private void RefreshFromSource()
    {
        if (active && !IsDisposed)
        {
            WriteTarget(source.Value);
        }
    }

    private UiRelay? ResolveRelay()
    {
        UiRelay? rootRelay = targetElement?.OwnerRelay;
        if (explicitRelay is not null && rootRelay is not null && !ReferenceEquals(explicitRelay, rootRelay))
        {
            throw new InvalidOperationException(
                $"Binding target '{targetProperty.DiagnosticName}' belongs to a different Relay.");
        }

        return rootRelay ?? explicitRelay;
    }
}
