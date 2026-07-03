namespace Cerneala.UI.Data;

public sealed class Binding<T> : Binding
{
    private readonly ObservableValue<T> source;
    private readonly Action<T> targetSetter;
    private readonly Action<T>? sourceWriter;

    public Binding(
        ObservableValue<T> source,
        Action<T> targetSetter,
        BindingMode mode = BindingMode.OneWay,
        Action<T>? sourceWriter = null,
        bool updateTargetImmediately = true)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        this.targetSetter = targetSetter ?? throw new ArgumentNullException(nameof(targetSetter));
        Mode = mode;
        this.sourceWriter = sourceWriter;
        if (this.sourceWriter is null && mode == BindingMode.TwoWay)
        {
            this.sourceWriter = value => source.SetValue(value);
        }
        source.ValueChanged += OnSourceValueChanged;
        try
        {
            if (updateTargetImmediately)
            {
                targetSetter(source.Value);
            }
        }
        catch
        {
            source.ValueChanged -= OnSourceValueChanged;
            throw;
        }
    }

    public BindingMode Mode { get; }

    public void CommitTargetValue(T value)
    {
        ThrowIfDisposed();
        if (Mode != BindingMode.TwoWay)
        {
            throw new InvalidOperationException("Only two-way bindings can commit target values.");
        }

        if (sourceWriter is null)
        {
            throw new InvalidOperationException("Two-way binding has no source writer.");
        }

        sourceWriter(value);
    }

    public static Binding<TIn> OneWayConverted<TIn, TOut>(
        ObservableValue<TIn> source,
        Action<TOut> targetSetter,
        IValueConverter<TIn, TOut> converter,
        bool updateTargetImmediately = true)
    {
        ArgumentNullException.ThrowIfNull(converter);
        ArgumentNullException.ThrowIfNull(targetSetter);
        return new Binding<TIn>(
            source,
            value => targetSetter(converter.Convert(value)),
            BindingMode.OneWay,
            updateTargetImmediately: updateTargetImmediately);
    }

    protected override void DisposeCore()
    {
        source.ValueChanged -= OnSourceValueChanged;
    }

    private void OnSourceValueChanged(object? sender, ObservableValueChangedEventArgs<T> args)
    {
        if (!IsDisposed)
        {
            targetSetter(args.NewValue);
        }
    }
}
