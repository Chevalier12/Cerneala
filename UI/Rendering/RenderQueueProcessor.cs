using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.UI.Rendering;

public sealed class RenderQueueProcessor
{
    private readonly RetainedRenderCache renderCache;
    private readonly RenderCounters counters;

    public RenderQueueProcessor(RetainedRenderCache renderCache, RenderCounters counters)
    {
        this.renderCache = renderCache ?? throw new ArgumentNullException(nameof(renderCache));
        this.counters = counters ?? throw new ArgumentNullException(nameof(counters));
    }

    public void Process(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (element.ConsumeRenderScopeOnlyInvalidation())
        {
            renderCache.InvalidateRoot();
            return;
        }

        bool forceRebuild = element.DirtyState.Has(InvalidationFlags.Render);
        bool rebuilt = renderCache.GetElementCache(element).Ensure(element, counters, forceRebuild);
        if (rebuilt)
        {
            renderCache.InvalidateRoot();
        }
    }
}
