namespace Cerneala.UI.Data;

public abstract class Binding : IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;
        DisposeCore();
        GC.SuppressFinalize(this);
    }

    protected abstract void DisposeCore();

    protected void ThrowIfDisposed()
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    public static Binding<T> OneWay<T>(ObservableValue<T> source, Action<T> targetSetter, bool updateTargetImmediately = true)
    {
        return new Binding<T>(source, targetSetter, BindingMode.OneWay, updateTargetImmediately: updateTargetImmediately);
    }

    public static Binding<T> TwoWay<T>(ObservableValue<T> source, Action<T> targetSetter, bool updateTargetImmediately = true)
    {
        return new Binding<T>(source, targetSetter, BindingMode.TwoWay, value => source.SetValue(value), updateTargetImmediately);
    }
}
