using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Core;

namespace Cerneala.UI.Motion.Layout;

public sealed class LayoutMotionCoordinator
{
    private readonly MotionSystem motion;
    private readonly Dictionary<UIElement, LayoutSnapshot> firstSnapshots = [];
    private readonly Dictionary<LayoutMotionId, LayoutSnapshot> previousSnapshotsById = [];
    private readonly Dictionary<UIElement, LayoutMotionBinding> bindings = [];

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

    internal void CaptureFirstSnapshots()
    {
        motion.VerifyAccess();
        firstSnapshots.Clear();
        if (!motion.Root.LayoutQueue.HasWork ||
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

            firstSnapshots[element] = new LayoutSnapshot(
                element,
                GetRootVisualBounds(element, includeOwnLayoutCorrection: true),
                element.VisualParent,
                element.LayoutMotionId);
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
            if (!IsParticipating(element) || !TryResolveFirstSnapshot(element, out LayoutSnapshot first))
            {
                continue;
            }

            if (!Equals(first.Id, element.LayoutMotionId))
            {
                continue;
            }

            LayoutRect last = GetRootVisualBounds(element, includeOwnLayoutCorrection: false);
            if (!IsValidLayoutRect(first.Bounds) || !IsValidLayoutRect(last))
            {
                continue;
            }

            if (first.Bounds == last)
            {
                continue;
            }

            Transform inverse = CreateElementSpaceCorrection(element, first.Bounds, last);
            if (inverse == Transform.Identity)
            {
                continue;
            }

            GetOrCreateBinding(element).StartCorrection(inverse, element.LayoutMotion!.CorrectionSpec);
        }

        UpdatePreviousSnapshots();
        firstSnapshots.Clear();
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

    private bool TryResolveFirstSnapshot(UIElement element, out LayoutSnapshot snapshot)
    {
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
        }

        return true;
    }

    private void UpdatePreviousSnapshots()
    {
        previousSnapshotsById.Clear();
        foreach (UIElement element in ElementTreeWalker.PreOrder(motion.Root))
        {
            if (!IsParticipating(element) || element.LayoutMotionId is not LayoutMotionId id)
            {
                continue;
            }

            previousSnapshotsById[id] = new LayoutSnapshot(
                element,
                GetRootVisualBounds(element, includeOwnLayoutCorrection: true),
                element.VisualParent,
                id);
        }
    }

    private static LayoutRect GetRootVisualBounds(UIElement element, bool includeOwnLayoutCorrection)
    {
        Matrix3x2 transform = Matrix3x2.Identity;
        Stack<UIElement> stack = new();
        for (UIElement? current = element; current is not null; current = current.VisualParent)
        {
            stack.Push(current);
        }

        while (stack.Count > 0)
        {
            UIElement current = stack.Pop();
            bool includeLayoutCorrection = includeOwnLayoutCorrection || !ReferenceEquals(current, element);
            transform = Matrix3x2.Multiply(GetElementTransform(current, includeLayoutCorrection), transform);
        }

        return TransformBounds(element.ArrangedBounds, transform);
    }

    private static Transform CreateElementSpaceCorrection(UIElement element, LayoutRect first, LayoutRect last)
    {
        Matrix3x2 rootCorrection = CreateInverseCorrection(first, last).Matrix;
        Matrix3x2 ancestorTransform = GetAncestorTransform(element);
        if (!TryInvert(ancestorTransform, out Matrix3x2 inverseAncestorTransform))
        {
            return Transform.Identity;
        }

        Matrix3x2 correction = Matrix3x2.Multiply(
            Matrix3x2.Multiply(ancestorTransform, rootCorrection),
            inverseAncestorTransform);
        correction = CompensateForRenderPivot(element, correction);
        return new Transform(correction);
    }

    private static Matrix3x2 CompensateForRenderPivot(UIElement element, Matrix3x2 correction)
    {
        LayoutRect bounds = element.ArrangedBounds;
        LayoutPoint origin = element.RenderTransformOrigin;
        float pivotX = bounds.X + (bounds.Width * origin.X);
        float pivotY = bounds.Y + (bounds.Height * origin.Y);
        return Matrix3x2.Multiply(
            Matrix3x2.Multiply(Matrix3x2.CreateTranslation(pivotX, pivotY), correction),
            Matrix3x2.CreateTranslation(-pivotX, -pivotY));
    }

    private static bool TryInvert(Matrix3x2 matrix, out Matrix3x2 inverse)
    {
        float determinant = (matrix.M11 * matrix.M22) - (matrix.M12 * matrix.M21);
        if (MathF.Abs(determinant) <= float.Epsilon)
        {
            inverse = Matrix3x2.Identity;
            return false;
        }

        float reciprocal = 1 / determinant;
        inverse = new Matrix3x2(
            matrix.M22 * reciprocal,
            -matrix.M12 * reciprocal,
            -matrix.M21 * reciprocal,
            matrix.M11 * reciprocal,
            ((matrix.M32 * matrix.M21) - (matrix.M31 * matrix.M22)) * reciprocal,
            ((matrix.M31 * matrix.M12) - (matrix.M32 * matrix.M11)) * reciprocal);
        return true;
    }

    private static Matrix3x2 GetAncestorTransform(UIElement element)
    {
        Matrix3x2 transform = Matrix3x2.Identity;
        Stack<UIElement> stack = new();
        for (UIElement? current = element.VisualParent; current is not null; current = current.VisualParent)
        {
            stack.Push(current);
        }

        while (stack.Count > 0)
        {
            transform = Matrix3x2.Multiply(GetElementTransform(stack.Pop(), includeLayoutCorrection: true), transform);
        }

        return transform;
    }

    private static Matrix3x2 GetElementTransform(UIElement element, bool includeLayoutCorrection)
    {
        LayoutRect bounds = element.ArrangedBounds;
        LayoutPoint origin = element.RenderTransformOrigin;
        float pivotX = bounds.X + (bounds.Width * origin.X);
        float pivotY = bounds.Y + (bounds.Height * origin.Y);

        Matrix3x2 channelTransform = Matrix3x2.Identity;
        channelTransform = Matrix3x2.Multiply(channelTransform, Matrix3x2.CreateScale(
            element.Scale * element.ScaleX * element.PresenceScale,
            element.Scale * element.ScaleY * element.PresenceScale));
        channelTransform = Matrix3x2.Multiply(channelTransform, Matrix3x2.CreateSkew(element.SkewX, element.SkewY));
        channelTransform = Matrix3x2.Multiply(channelTransform, Matrix3x2.CreateRotation(element.Rotation));
        channelTransform = Matrix3x2.Multiply(channelTransform, Matrix3x2.CreateTranslation(element.TranslateX, element.TranslateY));
        channelTransform = Matrix3x2.Multiply(channelTransform, element.RenderTransform.Matrix);
        if (includeLayoutCorrection)
        {
            channelTransform = Matrix3x2.Multiply(channelTransform, element.LayoutCorrectionTransform.Matrix);
        }

        if (channelTransform == Matrix3x2.Identity)
        {
            return Matrix3x2.Identity;
        }

        return Matrix3x2.Multiply(
            Matrix3x2.Multiply(Matrix3x2.CreateTranslation(-pivotX, -pivotY), channelTransform),
            Matrix3x2.CreateTranslation(pivotX, pivotY));
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
