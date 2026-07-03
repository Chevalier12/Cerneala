using Cerneala.UI.Core;

namespace Cerneala.UI.Animation;

public abstract class Transition
{
    private protected Transition(UiProperty property, TimeSpan duration)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Transition duration must be positive.");
        }

        Duration = duration;
    }

    public UiProperty Property { get; }

    public Type ValueType => Property.ValueType;

    public TimeSpan Duration { get; }

    public abstract Animation CreateUntyped(object? from, object? to);
}
