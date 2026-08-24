namespace Cerneala.UI.Motion;

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
