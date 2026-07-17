using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.UI.Elements;

internal static class ElementVisualTransform
{
    internal static Matrix3x2 GetElementTransform(UIElement element, bool includeLayoutCorrection = true)
    {
        ArgumentNullException.ThrowIfNull(element);

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

    internal static bool TryInvert(Matrix3x2 matrix, out Matrix3x2 inverse)
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
}
