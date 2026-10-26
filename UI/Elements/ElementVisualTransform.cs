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
        float m11 = matrix.M22 * reciprocal;
        float m12 = -matrix.M12 * reciprocal;
        float m21 = -matrix.M21 * reciprocal;
        float m22 = matrix.M11 * reciprocal;
        float m31 = ((matrix.M32 * matrix.M21) - (matrix.M31 * matrix.M22)) * reciprocal;
        float m32 = ((matrix.M31 * matrix.M12) - (matrix.M32 * matrix.M11)) * reciprocal;
        if (!float.IsFinite(m11) || !float.IsFinite(m12) || !float.IsFinite(m21) ||
            !float.IsFinite(m22) || !float.IsFinite(m31) || !float.IsFinite(m32))
        {
            return false;
        }

        inverse = new Matrix3x2(m11, m12, m21, m22, m31, m32);
        return true;
    }
}
