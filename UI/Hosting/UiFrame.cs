using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;

namespace Cerneala.UI.Hosting;

public sealed class UiFrame
{
    public UiFrame(TimeSpan elapsedTime, UiViewport viewport, InputFrame input, FrameStats stats)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(stats);

        ElapsedTime = elapsedTime;
        Viewport = viewport;
        Input = input;
        Stats = stats;
    }

    public TimeSpan ElapsedTime { get; }

    public UiViewport Viewport { get; }

    public InputFrame Input { get; }

    public FrameStats Stats { get; }
}
