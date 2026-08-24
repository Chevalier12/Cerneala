using System.Linq.Expressions;

namespace Cerneala.UI.Motion;

public sealed class ObjectMotionFacade<TTarget>
    where TTarget : class
{
    private readonly ObjectMotionRuntimeState runtime;
    private readonly TTarget target;

    internal ObjectMotionFacade(
        ObjectMotionRuntimeState runtime,
        TTarget target)
    {
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        this.target = target ?? throw new ArgumentNullException(nameof(target));
    }

    public ObjectMotionAnimationBuilder<TTarget, TValue> Animate<TValue>(
        Expression<Func<TTarget, TValue>> property)
    {
        ArgumentNullException.ThrowIfNull(property);
        return new ObjectMotionAnimationBuilder<TTarget, TValue>(
            runtime,
            target,
            ObjectMotionExpressionPropertyCache<TTarget, TValue>.Get(property));
    }

    public ObjectMotionAnimationBuilder<TPropertyTarget, TValue> Animate<TPropertyTarget, TValue>(
        MotionProperty<TPropertyTarget, TValue> property)
        where TPropertyTarget : class
    {
        ArgumentNullException.ThrowIfNull(property);
        if (target is not TPropertyTarget typedTarget)
        {
            throw new InvalidOperationException(
                $"Motion property '{property.Name}' targets '{typeof(TPropertyTarget).Name}', " +
                $"but the receiver is '{target.GetType().Name}'.");
        }

        return new ObjectMotionAnimationBuilder<TPropertyTarget, TValue>(
            runtime,
            typedTarget,
            property);
    }
}

public sealed class ObjectMotionFacade
{
    private readonly ObjectMotionRuntimeState runtime;
    private readonly object target;

    internal ObjectMotionFacade(
        ObjectMotionRuntimeState runtime,
        object target)
    {
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        this.target = target ?? throw new ArgumentNullException(nameof(target));
    }

    public ObjectMotionAnimationBuilder<TTarget, TValue> Animate<TTarget, TValue>(
        MotionProperty<TTarget, TValue> property)
        where TTarget : class
    {
        ArgumentNullException.ThrowIfNull(property);
        if (target is not TTarget typedTarget)
        {
            throw new InvalidOperationException(
                $"Motion property '{property.Name}' targets '{typeof(TTarget).Name}', " +
                $"but the receiver is '{target.GetType().Name}'.");
        }

        return new ObjectMotionAnimationBuilder<TTarget, TValue>(
            runtime,
            typedTarget,
            property);
    }
}
