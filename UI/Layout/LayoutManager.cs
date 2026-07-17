using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Layout;

public sealed class LayoutManager
{
    private readonly UIRoot root;

    public LayoutManager(UIRoot root)
    {
        this.root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public FramePhaseProcessors CreatePhaseProcessors()
    {
        return new FramePhaseProcessors
        {
            Measure = element => Measure(element, GetAvailableSize(element)),
            IncrementalMeasure = MeasureForFrame,
            Arrange = element => Arrange(element, GetFinalRect(element))
        };
    }

    public LayoutResult Measure(UIElement element, LayoutSize availableSize)
    {
        ArgumentNullException.ThrowIfNull(element);
        bool isDirtyMeasure = element.DirtyState.Has(InvalidationFlags.Measure);
        if (!isDirtyMeasure && element.TryUseCachedMeasure(availableSize, out LayoutSize cachedDesiredSize))
        {
            return new LayoutResult(cachedDesiredSize, element.ArrangedBounds, true, false, false);
        }

        if (isDirtyMeasure)
        {
            element.InvalidateMeasureCache();
        }

        LayoutSize desired = element.Measure(new MeasureContext(availableSize, LayoutRounding.ForScale(root.Scale)));
        element.LastMeasureAvailableSize = availableSize;
        element.LastMeasureLayoutVersion = element.LayoutVersion;
        element.LastMeasureViewportVersion = root.ViewportVersion;
        return new LayoutResult(desired, element.ArrangedBounds, false, false, false);
    }

    public LayoutResult Arrange(UIElement element, LayoutRect finalRect)
    {
        ArgumentNullException.ThrowIfNull(element);
        bool isDirtyArrange = element.DirtyState.Has(InvalidationFlags.Arrange);
        if (!isDirtyArrange &&
            element.LastArrangeFinalRect == finalRect &&
            element.LastArrangeLayoutVersion == element.LayoutVersion)
        {
            return new LayoutResult(element.DesiredSize, element.ArrangedBounds, false, true, false);
        }

        if (isDirtyArrange)
        {
            element.LastArrangeFinalRect = null;
        }

        LayoutRect previous = element.ArrangedBounds;
        LayoutRect arranged = element.Arrange(new ArrangeContext(finalRect, LayoutRounding.ForScale(root.Scale)));
        element.LastArrangeFinalRect = finalRect;
        element.LastArrangeLayoutVersion = element.LayoutVersion;
        PruneTranslatedDescendantRenderWork(element);

        bool boundsChanged = previous != arranged;
        if (boundsChanged && element.IsAttached)
        {
            element.Invalidate(InvalidationFlags.Render | InvalidationFlags.HitTest, "Layout bounds changed");
        }

        return new LayoutResult(element.DesiredSize, arranged, false, false, boundsChanged);
    }

    private void PruneTranslatedDescendantRenderWork(UIElement element)
    {
        if (!element.IsAttached)
        {
            return;
        }

        PruneTranslatedChildRenderWork(element);
    }

    private void PruneTranslatedChildRenderWork(UIElement parent)
    {
        for (int index = 0; index < parent.VisualChildren.Count; index++)
        {
            UIElement descendant = parent.VisualChildren[index];
            if (CanReuseTranslatedRenderCache(descendant))
            {
                root.RenderQueue.Remove(descendant);
                root.RetainedRenderCache.InvalidateRoot();
            }

            PruneTranslatedChildRenderWork(descendant);
        }
    }

    private bool CanReuseTranslatedRenderCache(UIElement element)
    {
        if (element.DirtyState.Has(InvalidationFlags.Render))
        {
            return false;
        }

        ElementRenderCache cache = root.RetainedRenderCache.GetElementCache(element);
        return cache.IsValid &&
            cache.Dependencies == element.RenderDependencies &&
            cache.ContentBounds.Width == element.ArrangedBounds.Width &&
            cache.ContentBounds.Height == element.ArrangedBounds.Height &&
            (cache.ContentBounds.X != element.ArrangedBounds.X ||
             cache.ContentBounds.Y != element.ArrangedBounds.Y);
    }

    private LayoutSize GetAvailableSize(UIElement element)
    {
        if (ReferenceEquals(element, root))
        {
            return new LayoutSize(root.ViewportWidth, root.ViewportHeight);
        }

        if (element.LastMeasureAvailableSize is LayoutSize previousAvailableSize &&
            element.LastMeasureViewportVersion == root.ViewportVersion)
        {
            return previousAvailableSize;
        }

        UIElement? parent = element.VisualParent;
        if (parent is UIRoot)
        {
            return new LayoutSize(root.ViewportWidth, root.ViewportHeight);
        }

        if (parent is not null && parent.ArrangedBounds.Width > 0 && parent.ArrangedBounds.Height > 0)
        {
            return parent.ArrangedBounds.Size;
        }

        return new LayoutSize(root.ViewportWidth, root.ViewportHeight);
    }

    private bool MeasureForFrame(UIElement element)
    {
        LayoutSize previousDesiredSize = element.DesiredSize;
        Measure(element, GetAvailableSize(element));
        return element.DesiredSize != previousDesiredSize;
    }

    private LayoutRect GetFinalRect(UIElement element)
    {
        if (ReferenceEquals(element, root))
        {
            return new LayoutRect(0, 0, root.ViewportWidth, root.ViewportHeight);
        }

        UIElement? parent = element.VisualParent;
        if (parent is Canvas)
        {
            return new LayoutRect(
                parent.ArrangedBounds.X + Canvas.GetLeft(element),
                parent.ArrangedBounds.Y + Canvas.GetTop(element),
                element.DesiredSize.Width,
                element.DesiredSize.Height);
        }

        if (parent is not UIRoot && element.LastArrangeFinalRect is LayoutRect currentSlot)
        {
            return currentSlot;
        }

        if (parent is not null && parent.ArrangedBounds.Width > 0 && parent.ArrangedBounds.Height > 0)
        {
            return new LayoutRect(parent.ArrangedBounds.X, parent.ArrangedBounds.Y, parent.ArrangedBounds.Width, parent.ArrangedBounds.Height);
        }

        LayoutSize desired = element.DesiredSize;
        return new LayoutRect(0, 0, desired.Width, desired.Height);
    }
}
