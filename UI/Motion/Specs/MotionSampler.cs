namespace Cerneala.UI.Motion.Specs;

public abstract class MotionSampler
{
    public abstract object? CurrentUntyped { get; }

    public abstract bool IsComplete { get; }

    public abstract void Advance(TimeSpan delta);

    public abstract void RetargetUntyped(object? to, RetargetMode mode);
}

public abstract class MotionSampler<T> : MotionSampler
{
    public abstract T Current { get; }

    public virtual MotionVelocity<T>? Velocity => null;

    public override object? CurrentUntyped => Current;

    public abstract void Retarget(T to, RetargetMode mode);

    public override void RetargetUntyped(object? to, RetargetMode mode)
    {
        if (to is T typed)
        {
            Retarget(typed, mode);
            return;
        }

        if (to is null && default(T) is null)
        {
            Retarget(default!, mode);
            return;
        }

        throw new ArgumentException($"Expected retarget value of type {typeof(T).Name}.", nameof(to));
    }
}
