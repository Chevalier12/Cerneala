using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;

namespace Cerneala.UI.Servo;

internal sealed class ServoCaptureEngine
{
    private readonly ServoQueryEngine queryEngine;

    internal ServoCaptureEngine(ServoQueryEngine queryEngine)
    {
        this.queryEngine = queryEngine ?? throw new ArgumentNullException(nameof(queryEngine));
    }

    internal WindowScreenshotRegion ResolveRegion(UIRoot root, ServoTarget target)
    {
        ServoResolvedElement match = queryEngine.Resolve(root, target);
        UIElement element = match.Element;
        if (!ReferenceEquals(element.Root, root))
        {
            throw new ServoTargetNotActionableException(
                "The Servo target detached before its screenshot could be captured.");
        }

        if (!UIElementVisibility.IsEffectivelyVisible(element))
        {
            throw new ServoTargetNotActionableException(
                "The Servo target is not effectively visible for capture.");
        }

        UiViewport viewport = new(root.ViewportWidth, root.ViewportHeight, root.Scale);
        if (!WindowScreenshotRegion.TryCreate(
                InputCoordinateConverter.GetRootBounds(element),
                viewport,
                out WindowScreenshotRegion region))
        {
            throw new ServoTargetNotActionableException(
                "The Servo target has no visible framebuffer region to capture.");
        }

        return region;
    }
}
