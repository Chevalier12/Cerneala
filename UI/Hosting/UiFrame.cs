using Cerneala.Drawing;
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

    public TimeSpan ProcessingTime { get; internal set; }

    internal UiFrameTiming DiagnosticsTiming { get; set; }

    public UiViewport Viewport { get; }

    public InputFrame Input { get; }

    public FrameStats Stats { get; }
}

internal readonly record struct UiFrameTiming(
    TimeSpan InputCollection,
    TimeSpan RetainedUpdate,
    TimeSpan BeginFrame,
    TimeSpan Drawing,
    DrawingBackendFrameTiming DrawingBackend,
    TimeSpan UpdatePreparation,
    TimeSpan ScheduledProcessing,
    TimeSpan InputDispatch,
    TimeSpan InputProcessing,
    TimeSpan RetainedCommit,
    TimeSpan CursorPublication,
    FramePhaseTiming ScheduledPhases,
    FramePhaseTiming InputPhases);
