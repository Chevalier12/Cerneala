using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Rendering;

public sealed class ElementRenderCache
{
    private readonly DrawCommandList commands = new();

    public DrawCommandList Commands => commands;

    public bool IsValid { get; private set; }

    public int RenderVersion { get; private set; } = -1;

    public RenderDependency Dependencies { get; private set; }

    public LayoutRect ContentBounds { get; private set; }

    public bool IsStale(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return !IsValid ||
            RenderVersion != element.RenderVersion ||
            Dependencies != element.RenderDependencies;
    }

    public bool Ensure(UIElement element, RenderCounters counters, bool forceRebuild = false)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(counters);

        if (!forceRebuild && !IsStale(element))
        {
            counters.CountCacheHit();
            return false;
        }

        counters.CountCacheMiss();
        counters.CountLocalRebuild();
        IsValid = false;
        commands.Clear();

        if (element.Visibility == Visibility.Visible && element.IsVisible)
        {
            DrawingContext drawingContext = new(commands);
            RenderContext context = new(element, drawingContext, element.ArrangedBounds, RenderLayer.Default, counters);
            element.Render(context);
        }

        RenderVersion = element.RenderVersion;
        Dependencies = element.RenderDependencies;
        ContentBounds = element.ArrangedBounds;
        IsValid = true;
        return true;
    }

    public void Invalidate()
    {
        IsValid = false;
    }
}
