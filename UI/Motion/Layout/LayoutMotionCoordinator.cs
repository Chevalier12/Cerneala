using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Core;

namespace Cerneala.UI.Motion.Layout;

public sealed class LayoutMotionCoordinator
{
    private readonly MotionSystem motion;
    private readonly Dictionary<UIElement, LayoutSnapshot> firstSnapshots = [];
    private readonly Dictionary<UIElement, LayoutRect> firstUncorrectedScrollBounds = [];
    private readonly Dictionary<LayoutMotionId, LayoutSnapshot> previousSnapshotsById = [];
    private readonly Dictionary<UIElement, LayoutMotionBinding> bindings = [];
    private readonly HashSet<UIElement> pendingDetached = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<UIElement> participants = new(ReferenceEqualityComparer.Instance);

    public LayoutMotionCoordinator(MotionSystem motion)
    {
        this.motion = motion ?? throw new ArgumentNullException(nameof(motion));
    }

    public LayoutMotionBinding? GetBinding(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return bindings.TryGetValue(element, out LayoutMotionBinding? binding) ? binding : null;
    }

    public int ActiveBindingCount => bindings.Count;

    internal int ActiveMotionCount => bindings.Values.Count(binding => binding.IsActive);

    internal void MarkAttached(UIElement element)
    {
        motion.VerifyAccess();
        pendingDetached.Remove(element);
        UpdateParticipation(element);
    }

    internal void MarkDetached(UIElement element)
    {
        motion.VerifyAccess();
        firstSnapshots.Remove(element);
        firstUncorrectedScrollBounds.Remove(element);
        participants.Remove(element);
        pendingDetached.Add(element);
    }

    internal void OnPropertyMutated(UiPropertyMutation mutation)
    {
        if ((ReferenceEquals(mutation.Property, UIElement.LayoutMotionIdProperty) ||
            ReferenceEquals(mutation.Property, UIElement.LayoutMotionOptionsProperty)) &&
            mutation.Target is UIElement element)
        {
            motion.VerifyAccess();
            UpdateParticipation(element);
        }
    }

    private void UpdateParticipation(UIElement element)
    {
        if (ReferenceEquals(element.Root, motion.Root) &&
            !pendingDetached.Contains(element) && IsParticipating(element))
        {
            participants.Add(element);
        }
        else
        {
            participants.Remove(element);
        }
    }

    internal void CaptureFirstSnapshots()
    {
        motion.VerifyAccess();
        CleanupDetached();
        firstSnapshots.Clear();
        firstUncorrectedScrollBounds.Clear();
        if (participants.Count == 0 || !motion.Root.LayoutQueue.HasWork ||
            motion.ReducedMotion.Mode == ReducedMotionMode.DisableNonEssential)
        {
            return;
        }

        foreach (UIElement element in ElementTreeWalker.PreOrder(motion.Root))
        {
            if (!IsParticipating(element))
            {
                continue;
            }

            if (!TryGetRootVisualBounds(element, includeOwnLayoutCorrection: true, ignoreScrollOffsets: true,
                out LayoutRect firstBounds))
            {
                continue;
            }

            firstSnapshots[element] = new LayoutSnapshot(
                element,
                firstBounds,
                element.VisualParent,
                element.LayoutMotionId);
            if (HasScrollAncestor(element) &&
                TryGetRootVisualBounds(element, includeOwnLayoutCorrection: false, ignoreScrollOffsets: true,
                    out LayoutRect uncorrectedBounds))
            {
                firstUncorrectedScrollBounds[element] = uncorrectedBounds;
            }
        }
    }

    private void CleanupDetached()
    {
        foreach (UIElement element in pendingDetached.Where(element => !element.IsAttached).ToArray())
        {
            pendingDetached.Remove(element);
            if (element.LayoutMotionId is LayoutMotionId id &&
                previousSnapshotsById.TryGetValue(id, out LayoutSnapshot previous) &&
                ReferenceEquals(previous.Element, element))
            {
                previousSnapshotsById.Remove(id);
            }

            if (bindings.Remove(element, out LayoutMotionBinding? binding))
            {
                binding.Dispose();
            }
        }
    }

    internal void CaptureLastSnapshotsAndStartCorrections()
    {
        motion.VerifyAccess();
        if (firstSnapshots.Count == 0)
        {
            return;
        }

        foreach (UIElement element in ElementTreeWalker.PreOrder(motion.Root))
        {
            if (!IsParticipating(element) || !TryResolveFirstSnapshot(element, out LayoutSnapshot first, out bool reparented))
            {
                continue;
            }

            if (!Equals(first.Id, element.LayoutMotionId))
            {
                continue;
            }

            if (!TryGetRootVisualBounds(element, includeOwnLayoutCorrection: false, ignoreScrollOffsets: !reparented,
                out LayoutRect last))
            {
                continue;
            }

            // A scroll-only arrange must not retarget an existing correction either.
            if (!reparented && firstUncorrectedScrollBounds.TryGetValue(element, out LayoutRect uncorrected) &&
                uncorrected == last)
            {
                continue;
            }

            if (!IsValidLayoutRect(first.Bounds) || !IsValidLayoutRect(last))
            {
                continue;
            }

            if (first.Bounds == last)
            {
                continue;
            }

            LayoutRect firstBounds = first.Bounds;
            if (!reparented)
            {
                // Convert the scroll-neutral comparison back to the current render space.
                if (!TryGetRootVisualBounds(element, includeOwnLayoutCorrection: false, ignoreScrollOffsets: false,
                    out LayoutRect visualLast) || !IsValidLayoutRect(visualLast))
                {
                    continue;
                }

                firstBounds = firstBounds with
                {
                    X = firstBounds.X + (visualLast.X - last.X),
                    Y = firstBounds.Y + (visualLast.Y - last.Y)
                };
                last = visualLast;
            }

            Transform inverse = CreateElementSpaceCorrection(element, firstBounds, last);
            if (inverse == Transform.Identity)
            {
                continue;
            }

            GetOrCreateBinding(element).StartCorrection(inverse, element.LayoutMotion!.CorrectionSpec);
        }

        UpdatePreviousSnapshots();
        firstSnapshots.Clear();
        firstUncorrectedScrollBounds.Clear();
    }

    private LayoutMotionBinding GetOrCreateBinding(UIElement element)
    {
        if (bindings.TryGetValue(element, out LayoutMotionBinding? binding))
        {
            return binding;
        }

        binding = new LayoutMotionBinding(motion, element);
        bindings[element] = binding;
        return binding;
    }

    private static bool IsParticipating(UIElement element)
    {
        return element.IsAttached &&
            element.LayoutMotion is not null &&
            element.LayoutMotionId is not null;
    }

    private static bool IsValidLayoutRect(LayoutRect rect)
    {
        return rect.Width > 0 &&
            rect.Height > 0 &&
            float.IsFinite(rect.X) &&
            float.IsFinite(rect.Y) &&
            float.IsFinite(rect.Width) &&
            float.IsFinite(rect.Height);
    }

    private bool TryResolveFirstSnapshot(UIElement element, out LayoutSnapshot snapshot, out bool reparented)
    {
        reparented = false;
        if (!firstSnapshots.TryGetValue(element, out snapshot))
        {
            return false;
        }

        if (snapshot.Id is LayoutMotionId id &&
            previousSnapshotsById.TryGetValue(id, out LayoutSnapshot previous) &&
            ReferenceEquals(previous.Element, element) &&
            !ReferenceEquals(previous.Parent, element.VisualParent))
        {
            snapshot = previous;
            reparented = true;
        }

        return true;
    }

    private void UpdatePreviousSnapshots()
    {
        previousSnapshotsById.Clear();
        foreach (UIElement element in ElementTreeWalker.PreOrder(motion.Root))
        {
            if (!IsParticipating(element) || element.LayoutMotionId is not LayoutMotionId id ||
                !TryGetRootVisualBounds(element, includeOwnLayoutCorrection: true, ignoreScrollOffsets: false,
                    out LayoutRect bounds))
            {
                continue;
            }

            previousSnapshotsById[id] = new LayoutSnapshot(
                element,
                bounds,
                element.VisualParent,
                id);
        }
    }

    private static bool HasScrollAncestor(UIElement element)
    {
        for (UIElement? current = element; current?.VisualParent is UIElement parent; current = parent)
        {
            if (parent is ScrollContentPresenter presenter && ReferenceEquals(presenter.Content, current))
            {
                return true;
            }
        }

        return false;
    }

    // Returns false when composing the visual transform chain overflows float; such an element has no
    // representable visual bounds, so it starts no layout correction.
    private static bool TryGetRootVisualBounds(
        UIElement element,
        bool includeOwnLayoutCorrection,
        bool ignoreScrollOffsets,
        out LayoutRect visualBounds)
    {
        visualBounds = default;
        Matrix3x2 transform = Matrix3x2.Identity;
        Stack<UIElement> stack = new();
        for (UIElement? current = element; current is not null; current = current.VisualParent)
        {
            stack.Push(current);
        }

        // The location the visual parent would have without the undone scroll offsets; null while nothing is undone.
        LayoutPoint? unscrolledParentLocation = null;
        LayoutRect bounds = element.ArrangedBounds;
        while (stack.Count > 0)
        {
            UIElement current = stack.Pop();
            LayoutPoint scrollOffset = LayoutPoint.Zero;
            if (ignoreScrollOffsets && current.VisualParent is ScrollContentPresenter presenter &&
                ReferenceEquals(presenter.Content, current))
            {
                scrollOffset = presenter.ArrangedScrollOffset;
            }

            bounds = current.ArrangedBounds;
            if (current.VisualParent is UIElement parent && (unscrolledParentLocation is not null || scrollOffset != LayoutPoint.Zero))
            {
                // A child is placed from its parent's rounded origin, and scroll offsets are applied before layout
                // rounding. Undo them in that same space, from the unscrolled parent origin, including for each
                // ancestor's render pivot.
                LayoutPoint parentOrigin = parent.ArrangedBounds.Location;
                LayoutPoint unscrolledParentOrigin = unscrolledParentLocation ?? parentOrigin;
                bounds = bounds with
                {
                    X = current.ArrangeRounding.Round(
                        unscrolledParentOrigin.X + (current.UnroundedArrangedLocation.X - parentOrigin.X) + scrollOffset.X),
                    Y = current.ArrangeRounding.Round(
                        unscrolledParentOrigin.Y + (current.UnroundedArrangedLocation.Y - parentOrigin.Y) + scrollOffset.Y)
                };
                unscrolledParentLocation = bounds.Location;
            }

            bool includeLayoutCorrection = includeOwnLayoutCorrection || !ReferenceEquals(current, element);
            if (!ElementVisualTransform.TryGetElementTransform(
                    current, bounds, includeLayoutCorrection, out Matrix3x2 elementTransform) ||
                !Matrix3x2.TryMultiply(elementTransform, transform, out transform))
            {
                return false;
            }
        }

        visualBounds = TransformBounds(bounds, transform);
        return true;
    }

    private static Transform CreateElementSpaceCorrection(UIElement element, LayoutRect first, LayoutRect last)
    {
        Matrix3x2 rootCorrection = CreateInverseCorrection(first, last).Matrix;
        // An ancestor chain that overflows float or cannot be inverted, or a converted correction that overflows,
        // cannot be represented in element space; no correction starts.
        if (!TryGetAncestorTransform(element, out Matrix3x2 ancestorTransform) ||
            !ElementVisualTransform.TryInvert(ancestorTransform, out Matrix3x2 inverseAncestorTransform) ||
            !Matrix3x2.TryMultiply(ancestorTransform, rootCorrection, out Matrix3x2 correction) ||
            !Matrix3x2.TryMultiply(correction, inverseAncestorTransform, out correction) ||
            !TryCompensateForRenderPivot(element, correction, out correction))
        {
            return Transform.Identity;
        }

        return new Transform(correction);
    }

    private static bool TryCompensateForRenderPivot(UIElement element, Matrix3x2 correction, out Matrix3x2 compensated)
    {
        LayoutRect bounds = element.ArrangedBounds;
        LayoutPoint origin = element.RenderTransformOrigin;
        float pivotX = bounds.X + (bounds.Width * origin.X);
        float pivotY = bounds.Y + (bounds.Height * origin.Y);
        compensated = Matrix3x2.Identity;
        return float.IsFinite(pivotX) && float.IsFinite(pivotY) &&
            Matrix3x2.TryMultiply(Matrix3x2.CreateTranslation(pivotX, pivotY), correction, out Matrix3x2 pivoted) &&
            Matrix3x2.TryMultiply(pivoted, Matrix3x2.CreateTranslation(-pivotX, -pivotY), out compensated);
    }

    private static bool TryGetAncestorTransform(UIElement element, out Matrix3x2 transform)
    {
        transform = Matrix3x2.Identity;
        Stack<UIElement> stack = new();
        for (UIElement? current = element.VisualParent; current is not null; current = current.VisualParent)
        {
            stack.Push(current);
        }

        while (stack.Count > 0)
        {
            if (!ElementVisualTransform.TryGetElementTransform(stack.Pop(), out Matrix3x2 elementTransform) ||
                !Matrix3x2.TryMultiply(elementTransform, transform, out transform))
            {
                return false;
            }
        }

        return true;
    }

    private static Transform CreateInverseCorrection(LayoutRect first, LayoutRect last)
    {
        Matrix3x2 correction = Matrix3x2.CreateTranslation(-last.X, -last.Y);
        if (last.Width != 0 && last.Height != 0)
        {
            correction = Matrix3x2.Multiply(
                correction,
                Matrix3x2.CreateScale(first.Width / last.Width, first.Height / last.Height));
        }

        correction = Matrix3x2.Multiply(correction, Matrix3x2.CreateTranslation(first.X, first.Y));
        return new Transform(correction);
    }

    private static LayoutRect TransformBounds(LayoutRect rect, Matrix3x2 transform)
    {
        DrawPoint topLeft = transform.Transform(new DrawPoint(rect.X, rect.Y));
        DrawPoint topRight = transform.Transform(new DrawPoint(rect.X + rect.Width, rect.Y));
        DrawPoint bottomLeft = transform.Transform(new DrawPoint(rect.X, rect.Y + rect.Height));
        DrawPoint bottomRight = transform.Transform(new DrawPoint(rect.X + rect.Width, rect.Y + rect.Height));

        float minX = MathF.Min(MathF.Min(topLeft.X, topRight.X), MathF.Min(bottomLeft.X, bottomRight.X));
        float minY = MathF.Min(MathF.Min(topLeft.Y, topRight.Y), MathF.Min(bottomLeft.Y, bottomRight.Y));
        float maxX = MathF.Max(MathF.Max(topLeft.X, topRight.X), MathF.Max(bottomLeft.X, bottomRight.X));
        float maxY = MathF.Max(MathF.Max(topLeft.Y, topRight.Y), MathF.Max(bottomLeft.Y, bottomRight.Y));
        return new LayoutRect(minX, minY, maxX - minX, maxY - minY);
    }
}
