using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.UI.Elements;

internal static class ElementVisualTransform
{
    internal static Matrix3x2 GetElementTransform(UIElement element, bool includeLayoutCorrection = true)
    {
        ArgumentNullException.ThrowIfNull(element);
        return GetElementTransform(element, element.ArrangedBounds, includeLayoutCorrection);
    }

    internal static Matrix3x2 GetElementTransform(UIElement element, LayoutRect bounds, bool includeLayoutCorrection = true)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (!TryGetElementTransform(element, bounds, includeLayoutCorrection, out Matrix3x2 transform))
        {
            throw new ArgumentOutOfRangeException(nameof(element), "Matrix values must be finite.");
        }

        return transform;
    }

    // Returns false when composing the element transform overflows float; such a
    // transform is non-finite, so input treats it like a non-invertible transform.
    internal static bool TryGetElementTransform(UIElement element, out Matrix3x2 transform)
    {
        ArgumentNullException.ThrowIfNull(element);
        return TryGetElementTransform(element, element.ArrangedBounds, includeLayoutCorrection: true, out transform);
    }

    private static bool TryGetElementTransform(
        UIElement element,
        LayoutRect bounds,
        bool includeLayoutCorrection,
        out Matrix3x2 transform)
    {
        transform = Matrix3x2.Identity;
        LayoutPoint origin = element.RenderTransformOrigin;
        float pivotX = bounds.X + (bounds.Width * origin.X);
        float pivotY = bounds.Y + (bounds.Height * origin.Y);
        float scaleX = element.Scale * element.ScaleX * element.PresenceScale;
        float scaleY = element.Scale * element.ScaleY * element.PresenceScale;
        if (!float.IsFinite(scaleX) || !float.IsFinite(scaleY))
        {
            return false;
        }

        Matrix3x2 channelTransform = Matrix3x2.Identity;
        if (!Matrix3x2.TryMultiply(channelTransform, Matrix3x2.CreateScale(scaleX, scaleY), out channelTransform) ||
            !Matrix3x2.TryMultiply(channelTransform, Matrix3x2.CreateSkew(element.SkewX, element.SkewY), out channelTransform) ||
            !Matrix3x2.TryMultiply(channelTransform, Matrix3x2.CreateRotation(element.Rotation), out channelTransform) ||
            !Matrix3x2.TryMultiply(channelTransform, Matrix3x2.CreateTranslation(element.TranslateX, element.TranslateY), out channelTransform) ||
            !Matrix3x2.TryMultiply(channelTransform, element.RenderTransform.Matrix, out channelTransform) ||
            (includeLayoutCorrection &&
                !Matrix3x2.TryMultiply(channelTransform, element.LayoutCorrectionTransform.Matrix, out channelTransform)))
        {
            return false;
        }

        if (channelTransform == Matrix3x2.Identity)
        {
            return true;
        }

        return float.IsFinite(pivotX) && float.IsFinite(pivotY) &&
            Matrix3x2.TryMultiply(Matrix3x2.CreateTranslation(-pivotX, -pivotY), channelTransform, out Matrix3x2 pivoted) &&
            Matrix3x2.TryMultiply(pivoted, Matrix3x2.CreateTranslation(pivotX, pivotY), out transform);
    }

    // Returns false when the inverse cannot be represented with finite float values: a zero determinant, a
    // determinant that overflows, or a denormal determinant whose reciprocal overflows.
    internal static bool TryInvert(Matrix3x2 matrix, out Matrix3x2 inverse)
    {
        inverse = Matrix3x2.Identity;
        float determinant = (matrix.M11 * matrix.M22) - (matrix.M12 * matrix.M21);
        if (!float.IsFinite(determinant) || MathF.Abs(determinant) <= float.Epsilon)
        {
            return false;
        }

        float reciprocal = 1 / determinant;
        return Matrix3x2.TryCreate(
            matrix.M22 * reciprocal,
            -matrix.M12 * reciprocal,
            -matrix.M21 * reciprocal,
            matrix.M11 * reciprocal,
            ((matrix.M32 * matrix.M21) - (matrix.M31 * matrix.M22)) * reciprocal,
            ((matrix.M31 * matrix.M12) - (matrix.M32 * matrix.M11)) * reciprocal,
            out inverse);
    }
}
