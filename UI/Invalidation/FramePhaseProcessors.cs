using Cerneala.UI.Elements;

namespace Cerneala.UI.Invalidation;

public sealed class FramePhaseProcessors
{
    public static FramePhaseProcessors Empty { get; } = new();

    public Action<UIElement>? InheritedProperties { get; init; }

    public Action<UIElement>? CommandState { get; init; }

    public Action<UIElement>? Aspect { get; init; }

    public Action<UIElement>? Measure { get; init; }

    public Action<UIElement>? Arrange { get; init; }

    public Action<UIElement>? RenderCache { get; init; }

    public Action<UIElement>? HitTest { get; init; }

    internal Func<UIElement, bool>? IncrementalMeasure { get; init; }

    internal bool SupportsIncrementalMeasure => IncrementalMeasure is not null;

    internal bool ProcessMeasure(UIElement element)
    {
        if (IncrementalMeasure is not null)
        {
            return IncrementalMeasure(element);
        }

        Measure?.Invoke(element);
        return true;
    }

    internal void Process(FramePhase phase, UIElement element)
    {
        switch (phase)
        {
            case FramePhase.InheritedProperties:
                InheritedProperties?.Invoke(element);
                break;
            case FramePhase.CommandState:
                CommandState?.Invoke(element);
                break;
            case FramePhase.Aspect:
                Aspect?.Invoke(element);
                break;
            case FramePhase.Measure:
                Measure?.Invoke(element);
                break;
            case FramePhase.Arrange:
                Arrange?.Invoke(element);
                break;
            case FramePhase.RenderCache:
                RenderCache?.Invoke(element);
                break;
            case FramePhase.HitTest:
                HitTest?.Invoke(element);
                break;
        }
    }
}
