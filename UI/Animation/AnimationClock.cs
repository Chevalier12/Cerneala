namespace Cerneala.UI.Animation;

public sealed class AnimationClock
{
    public AnimationClock(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Animation duration must be positive.");
        }

        Duration = duration;
    }

    public TimeSpan Duration { get; }

    public TimeSpan Elapsed { get; private set; }

    public float Progress => Duration.Ticks == 0
        ? 1
        : Math.Clamp((float)(Elapsed.TotalSeconds / Duration.TotalSeconds), 0, 1);

    public bool IsComplete => Elapsed >= Duration;

    public void Tick(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed), "Elapsed time cannot be negative.");
        }

        TimeSpan remaining = Duration - Elapsed;
        if (elapsed >= remaining)
        {
            Elapsed = Duration;
            return;
        }

        Elapsed += elapsed;
    }
}
