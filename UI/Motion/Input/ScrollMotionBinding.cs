using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Properties;

namespace Cerneala.UI.Motion.Input;

public sealed class ScrollMotionBinding<T> : IDisposable
{
    private readonly ScrollTimelineProgress progress;
    private readonly MotionRange range;
    private readonly List<Action<T>> listeners = [];
    private readonly IDisposable progressSubscription;
    private bool allowsLayout;
    private bool disposed;

    internal ScrollMotionBinding(ScrollTimelineProgress progress, MotionRange range)
    {
        this.progress = progress ?? throw new ArgumentNullException(nameof(progress));
        this.range = range;
        progressSubscription = progress.Subscribe(Notify);
    }

    public T Current => Convert(range.Map(progress.Current));

    public ScrollMotionBinding<T> AllowLayout()
    {
        allowsLayout = true;
        return this;
    }

    internal void Bind(UIElement element, UiProperty<T> property)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(property);
        if (!allowsLayout &&
            MotionPropertyInvalidationClassifier.Classify(property).HasFlag(MotionPropertyInvalidationCategory.Layout))
        {
            throw new InvalidOperationException("Scroll-linked layout properties require explicit AllowLayout() opt-in.");
        }

        Action<T> apply = value => element.SetValue(property, value, UiPropertyValueSource.Animation);
        listeners.Add(apply);
        apply(Current);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        progressSubscription.Dispose();
        listeners.Clear();
    }

    private void Notify(float value)
    {
        T mapped = Convert(range.Map(value));
        foreach (Action<T> listener in listeners.ToArray())
        {
            listener(mapped);
        }
    }

    private static T Convert(float value)
    {
        if (typeof(T) == typeof(float))
        {
            return (T)(object)value;
        }

        throw new InvalidOperationException("Scroll motion binding currently supports float properties only.");
    }
}
