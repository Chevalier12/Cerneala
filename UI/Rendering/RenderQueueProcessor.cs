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
        ElementRenderCache elementCache = renderCache.GetElementCache(element);
        if (element.ConsumeRenderScopeOnlyInvalidation())
        {
            renderCache.InvalidateRoot();
            if (!elementCache.IsStale(element))
            {
                return;
            }
        }

        bool forceRebuild = element.DirtyState.Has(InvalidationFlags.Render);
        bool rebuilt = elementCache.Ensure(element, counters, forceRebuild);
        if (rebuilt)
        {
            renderCache.InvalidateRoot();
        }
    }
}
