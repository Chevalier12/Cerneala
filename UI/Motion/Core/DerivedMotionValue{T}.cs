namespace Cerneala.UI.Motion.Core;

public sealed class DerivedMotionValue<T> : MotionValue, IDisposable
{
    private readonly Func<T> compute;
    private readonly List<IDisposable> subscriptions;
    private readonly List<Action<MotionValueChanged<T>>> listeners = [];
    private T current;
    private bool disposed;

    internal DerivedMotionValue(Func<T> compute, Func<Action, List<IDisposable>> subscribe)
    {
        ArgumentNullException.ThrowIfNull(compute);
        ArgumentNullException.ThrowIfNull(subscribe);
        this.compute = compute;
        current = compute();
        subscriptions = subscribe(Recompute);
    }

    public T Current => current;

    internal override Type ValueType => typeof(T);

    public IDisposable Subscribe(Action<MotionValueChanged<T>> listener)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(listener);
        listeners.Add(listener);
        return new Subscription(listeners, listener);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        foreach (IDisposable subscription in subscriptions)
        {
            subscription.Dispose();
        }

        subscriptions.Clear();
        listeners.Clear();
    }

    private void Recompute()
    {
        if (disposed)
        {
            return;
        }

        T next = compute();
        if (EqualityComparer<T>.Default.Equals(current, next))
        {
            return;
        }

        T old = current;
        current = next;
        MotionValueChanged<T> change = new(old, current, current, false);
        foreach (Action<MotionValueChanged<T>> listener in listeners.ToArray())
        {
            listener(change);
        }
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(DerivedMotionValue<T>));
        }
    }

    private sealed class Subscription(
        List<Action<MotionValueChanged<T>>> listeners,
        Action<MotionValueChanged<T>> listener) : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            listeners.Remove(listener);
        }
    }
}
