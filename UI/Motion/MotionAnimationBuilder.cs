using Cerneala.UI.Core;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Input;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Motion;

public sealed class MotionAnimationBuilder<T>
{
    private readonly MotionElementFacade facade;
    private readonly UiProperty<T> property;
    private bool hasFrom;
    private T from = default!;
    private T to = default!;

    internal MotionAnimationBuilder(MotionElementFacade facade, UiProperty<T> property)
    {
        this.facade = facade ?? throw new ArgumentNullException(nameof(facade));
        this.property = property ?? throw new ArgumentNullException(nameof(property));
    }

    public MotionAnimationBuilder<T> From(T value)
    {
        from = value;
        hasFrom = true;
        return this;
    }

    public MotionAnimationBuilder<T> To(T value)
    {
        to = value;
        return this;
    }

    public MotionHandle With(MotionSpec<T> spec)
    {
        return With(spec, new MotionPropertyStartOptions { HoldOnComplete = true });
    }

    public MotionHandle With(MotionSpec<T> spec, MotionPropertyStartOptions options)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(options);
        MotionSystem motion = facade.ResolveMotion();
        MotionPropertyBinding<T> binding = motion.Properties.GetOrCreateBinding(motion, facade.Element, property);
        if (hasFrom)
        {
            binding.Value.JumpTo(from);
        }

        return binding.AnimateTo(to, spec, options);
    }

    public void Bind(ScrollMotionBinding<T> binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        binding.Bind(facade.Element, property);
    }
}
