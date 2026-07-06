using Cerneala.UI.Core;

namespace Cerneala.UI.Data;

public sealed class UiPropertyBinding<T> : Binding
{
    private readonly ObservableValue<T> source;
    private readonly UiObject target;
    private readonly UiProperty<T> targetProperty;
    private readonly BindingMode mode;
    private bool updating;

    public UiPropertyBinding(
        UiObject target,
        UiProperty<T> targetProperty,
        ObservableValue<T> source,
        BindingMode mode = BindingMode.OneWay)
    {
        this.target = target ?? throw new ArgumentNullException(nameof(target));
        this.targetProperty = targetProperty ?? throw new ArgumentNullException(nameof(targetProperty));
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        this.mode = mode;

        if (targetProperty.IsReadOnly)
        {
            throw new InvalidOperationException($"UI property '{targetProperty.DiagnosticName}' is read-only.");
        }

        source.ValueChanged += OnSourceValueChanged;
        if (mode == BindingMode.TwoWay)
        {
            target.PropertyChanged += OnTargetPropertyChanged;
        }

        try
        {
            WriteTarget(source.Value);
        }
        catch
        {
            source.ValueChanged -= OnSourceValueChanged;
            if (mode == BindingMode.TwoWay)
            {
                target.PropertyChanged -= OnTargetPropertyChanged;
            }

            throw;
        }
    }

    public BindingMode Mode => mode;

    protected override void DisposeCore()
    {
        source.ValueChanged -= OnSourceValueChanged;
        if (mode == BindingMode.TwoWay)
        {
            target.PropertyChanged -= OnTargetPropertyChanged;
        }
    }

    private void OnSourceValueChanged(object? sender, ObservableValueChangedEventArgs<T> args)
    {
        if (IsDisposed || updating)
        {
            return;
        }

        WriteTarget(args.NewValue);
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
}
