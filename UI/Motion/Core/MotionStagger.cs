namespace Cerneala.UI.Motion.Core;

public sealed class MotionStagger(TimeSpan offset)
{
    public TimeSpan Offset { get; } = offset < TimeSpan.Zero
        ? throw new ArgumentOutOfRangeException(nameof(offset), "Stagger offset cannot be negative.")
        : offset;

    public TimeSpan GetDelay(int index)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index cannot be negative.");
        }

        return Offset * index;
    }
}
