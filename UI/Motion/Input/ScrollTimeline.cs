using Cerneala.UI.Controls;
using Cerneala.UI.Motion.Core;

namespace Cerneala.UI.Motion.Input;

public sealed class ScrollTimeline
{
    private readonly ScrollViewer scrollViewer;

    internal ScrollTimeline(ScrollViewer scrollViewer)
    {
        this.scrollViewer = scrollViewer ?? throw new ArgumentNullException(nameof(scrollViewer));
        MotionSystem motion = scrollViewer.Root?.Motion ?? throw new InvalidOperationException("ScrollViewer must be attached before creating a scroll timeline.");
        Progress = new ScrollTimelineProgress(motion.Graph.CreateValue(0f));
        HorizontalProgress = new ScrollTimelineProgress(motion.Graph.CreateValue(0f));
    }

    public ScrollTimelineProgress Progress { get; }

    public ScrollTimelineProgress HorizontalProgress { get; }

    public float VerticalOffset => scrollViewer.ScrollInfo.VerticalOffset;

    public float HorizontalOffset => scrollViewer.ScrollInfo.HorizontalOffset;

    public void Update()
    {
        IScrollInfo info = scrollViewer.ScrollInfo;
        Progress.JumpTo(Normalize(info.VerticalOffset, info.ExtentHeight, info.ViewportHeight));
        HorizontalProgress.JumpTo(Normalize(info.HorizontalOffset, info.ExtentWidth, info.ViewportWidth));
    }

    private static float Normalize(float offset, float extent, float viewport)
    {
        float max = MathF.Max(0, extent - viewport);
        return max <= 0 ? 0 : Math.Clamp(offset / max, 0, 1);
    }
}

public sealed class ScrollTimelineProgress
{
    private readonly MotionValue<float> value;

    internal ScrollTimelineProgress(MotionValue<float> value)
    {
        this.value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public float Current => value.Current;

    internal IDisposable Subscribe(Action<float> listener)
    {
        return value.Subscribe(change => listener(change.NewValue));
    }

    internal void JumpTo(float progress)
    {
        value.JumpTo(progress);
    }

    public ScrollMotionBinding<float> Map(float from, float to)
    {
        return new ScrollMotionBinding<float>(this, new MotionRange(0, 1, from, to));
    }
}
