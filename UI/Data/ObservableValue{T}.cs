namespace Cerneala.UI.Data;

public sealed class ObservableValue<T>
{
    private readonly IEqualityComparer<T> equalityComparer;
    private T value;

    public ObservableValue(T value = default!, IEqualityComparer<T>? equalityComparer = null)
    {
        this.value = value;
        this.equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
    }

    public event EventHandler<ObservableValueChangedEventArgs<T>>? ValueChanged;

    public T Value
    {
        get => value;
        set => SetValue(value);
    }

    public T SetValue(T newValue)
    {
        T oldValue = value;
        if (equalityComparer.Equals(oldValue, newValue))
        {
            return oldValue;
        }

        value = newValue;
        ValueChanged?.Invoke(this, new ObservableValueChangedEventArgs<T>(oldValue, newValue));
        return oldValue;
    }
}

public sealed class ObservableValueChangedEventArgs<T> : EventArgs
{
    public ObservableValueChangedEventArgs(T oldValue, T newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    public T OldValue { get; }

    public T NewValue { get; }
}
