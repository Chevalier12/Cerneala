namespace Cerneala.UI.Controls;

/// <summary>Reports the time a frame advanced to a scene node's <c>Awake</c> event.</summary>
public sealed class FrameTimeEventArgs : EventArgs
{
    internal static FrameTimeEventArgs Zero { get; } = new(TimeSpan.Zero);

    /// <summary>Creates arguments that report <paramref name="elapsed"/> as the time the frame advanced.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="elapsed"/> is negative.</exception>
    public FrameTimeEventArgs(TimeSpan elapsed)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(elapsed, TimeSpan.Zero);
        Elapsed = elapsed;
    }

    /// <summary>
    /// The time the frame advanced. It is <see cref="TimeSpan.Zero"/> for the first event after the node became awake,
    /// and the elapsed time of that frame for every later one.
    /// </summary>
    public TimeSpan Elapsed { get; }
}
