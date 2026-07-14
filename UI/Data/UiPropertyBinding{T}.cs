using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Relay;

namespace Cerneala.UI.Data;

public sealed class UiPropertyBinding<T> : Binding, IElementLifecycleBehavior
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

    void IElementLifecycleBehavior.ValidateRoot(UIRoot root)
    {
        if (explicitRelay is not null && !ReferenceEquals(explicitRelay, root.Relay))
        {
            throw new InvalidOperationException(
                $"Binding target '{targetProperty.DiagnosticName}' is attaching to a root with a different Relay.");
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
            WriteTarget(source.Value);
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
            WriteTarget(args.NewValue);
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
        UiRelay? rootRelay = targetElement?.Root?.Relay;
        if (explicitRelay is not null && rootRelay is not null && !ReferenceEquals(explicitRelay, rootRelay))
        {
            throw new InvalidOperationException(
                $"Binding target '{targetProperty.DiagnosticName}' belongs to a different Relay.");
        }

        return rootRelay ?? explicitRelay;
    }
}
