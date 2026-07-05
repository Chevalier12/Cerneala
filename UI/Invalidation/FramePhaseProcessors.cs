using Cerneala.UI.Elements;

namespace Cerneala.UI.Invalidation;

public sealed class FramePhaseProcessors
{
    public static FramePhaseProcessors Empty { get; } = new();

    public Action<UIElement>? InheritedProperties { get; init; }

    public Action<UIElement>? Style { get; init; }

    public Action<UIElement>? Measure { get; init; }

    public Action<UIElement>? Arrange { get; init; }

    public Action<UIElement>? RenderCache { get; init; }

    public Action<UIElement>? HitTest { get; init; }

    internal void Process(FramePhase phase, UIElement element)
    {
        switch (phase)
        {
            case FramePhase.InheritedProperties:
                InheritedProperties?.Invoke(element);
                break;
            case FramePhase.Style:
                Style?.Invoke(element);
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
