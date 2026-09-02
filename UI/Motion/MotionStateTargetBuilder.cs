using Cerneala.UI.Aspect;
using Cerneala.UI.Core;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Motion;

public sealed class MotionStateTargetBuilder
{
    private readonly MotionStateBuilder owner;
    private readonly AspectState state;

    internal MotionStateTargetBuilder(MotionStateBuilder owner, AspectState state)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public MotionStateBuilder Set<T>(UiProperty<T> property, T value, MotionSpec<T> spec)
    {
        return owner.Set(state, property, value, spec);
    }
}
