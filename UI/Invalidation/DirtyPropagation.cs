using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Invalidation;

public sealed class DirtyPropagation
{
    public static DirtyPropagation Default { get; } = new();

    public void Propagate(
        InvalidationRequest request,
        UIRoot root,
        LayoutQueue layoutQueue,
        RenderQueue renderQueue,
        HitTestQueue hitTestQueue,
        InvalidationTrace trace)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(layoutQueue);
        ArgumentNullException.ThrowIfNull(renderQueue);
        ArgumentNullException.ThrowIfNull(hitTestQueue);
        ArgumentNullException.ThrowIfNull(trace);

        InvalidationFlags effective = GetEffectiveFlags(request);
        if (effective == InvalidationFlags.None)
        {
            return;
        }

        InvalidationFlags propagated = effective & ~InvalidationFlags.Subtree;
        MarkAndQueue(request.Target, propagated, layoutQueue, renderQueue, hitTestQueue, trace, request.Reason);

        if (effective.HasFlag(InvalidationFlags.Measure))
        {
            foreach (UIElement ancestor in ElementTreeWalker.Ancestors(request.Target, ElementChildRole.Visual))
            {
                InvalidationFlags ancestorFlags = InvalidationFlags.Measure | InvalidationFlags.Arrange;
                MarkAndQueue(ancestor, ancestorFlags, layoutQueue, renderQueue, hitTestQueue, trace, "Measure ancestor propagation");
            }
        }

        if (request.Flags.HasFlag(InvalidationFlags.Subtree))
        {
            foreach (UIElement descendant in ElementTreeWalker.Descendants(request.Target, ElementChildRole.Visual))
            {
                MarkAndQueue(descendant, propagated, layoutQueue, renderQueue, hitTestQueue, trace, "Subtree propagation");
            }
        }
    }

    public InvalidationFlags GetEffectiveFlags(InvalidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        InvalidationFlags effective = request.Flags;

        if (request.Flags.HasFlag(InvalidationFlags.Measure))
        {
            effective |= InvalidationFlags.Arrange | InvalidationFlags.Render;
        }

        if (request.Flags.HasFlag(InvalidationFlags.Arrange))
        {
            effective |= InvalidationFlags.Render;
        }

        if (request.Flags.HasFlag(InvalidationFlags.Text))
        {
            effective |= InvalidationFlags.Measure | InvalidationFlags.Arrange | InvalidationFlags.Render;
        }

        if (request.Flags.HasFlag(InvalidationFlags.Image))
        {
            effective |= request.AffectsIntrinsicSize
                ? InvalidationFlags.Measure | InvalidationFlags.Arrange | InvalidationFlags.Render
                : InvalidationFlags.Render;
        }

        if (request.Flags.HasFlag(InvalidationFlags.Resource))
        {
            effective &= ~InvalidationFlags.Resource;
            effective |= request.ResourceEffects ?? InvalidationFlags.Render;
        }

        if (request.Flags.HasFlag(InvalidationFlags.InputVisual))
        {
            effective |= InvalidationFlags.Render;
        }

        return effective;
    }

    private static void MarkAndQueue(
        UIElement element,
        InvalidationFlags flags,
        LayoutQueue layoutQueue,
        RenderQueue renderQueue,
        HitTestQueue hitTestQueue,
        InvalidationTrace trace,
        string reason)
    {
        if (flags == InvalidationFlags.None)
        {
            return;
        }

        element.DirtyState.Mark(flags);
        trace.RecordPropagation(element, flags, reason);

        if (flags.HasFlag(InvalidationFlags.Measure))
        {
            layoutQueue.EnqueueMeasure(element);
            trace.RecordQueue(element, InvalidationFlags.Measure, reason);
        }

        if (flags.HasFlag(InvalidationFlags.Arrange))
        {
            layoutQueue.EnqueueArrange(element);
            trace.RecordQueue(element, InvalidationFlags.Arrange, reason);
        }

        if (flags.HasFlag(InvalidationFlags.Render))
        {
            renderQueue.Enqueue(element);
            trace.RecordQueue(element, InvalidationFlags.Render, reason);
        }

        if (flags.HasFlag(InvalidationFlags.HitTest))
        {
            hitTestQueue.Enqueue(element);
            trace.RecordQueue(element, InvalidationFlags.HitTest, reason);
        }
    }
}
