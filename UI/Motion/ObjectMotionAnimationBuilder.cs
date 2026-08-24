using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Motion;

public sealed class ObjectMotionAnimationBuilder<TTarget, TValue>
    where TTarget : class
{
    private readonly ObjectMotionRuntimeState runtime;
    private readonly TTarget target;
    private readonly MotionProperty<TTarget, TValue> property;
    private bool hasFrom;
    private TValue from = default!;
    private TValue to = default!;

    internal ObjectMotionAnimationBuilder(
        ObjectMotionRuntimeState runtime,
        TTarget target,
        MotionProperty<TTarget, TValue> property)
    {
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        this.target = target ?? throw new ArgumentNullException(nameof(target));
        this.property = property ?? throw new ArgumentNullException(nameof(property));
    }

    public ObjectMotionAnimationBuilder<TTarget, TValue> From(TValue value)
    {
        from = value;
        hasFrom = true;
        return this;
    }

    public ObjectMotionAnimationBuilder<TTarget, TValue> To(TValue value)
    {
        to = value;
        return this;
    }

    public MotionHandle Start(MotionSpec<TValue> spec)
    {
        return Start(
            spec,
            new MotionPropertyStartOptions { HoldOnComplete = true });
    }

    public MotionHandle Start(
        MotionSpec<TValue> spec,
        MotionPropertyStartOptions options)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(options);

        ObjectMotionBinding<TTarget, TValue> binding =
            runtime.Bindings.GetOrCreateBinding(
                runtime,
                target,
                property);
        if (hasFrom)
        {
            binding.JumpTo(from);
        }

        return binding.AnimateTo(to, spec, options);
    }
}
