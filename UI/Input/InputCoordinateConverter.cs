using System.Numerics;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Input;

internal static class InputCoordinateConverter
{
    internal static LayoutRect GetRootBounds(UIElement element) =>
        element is IInputCoordinateSpace coordinateSpace
            ? coordinateSpace.GetRootBounds()
            : element.ArrangedBounds;

    internal static bool TryRootToElement(
        UIElement element,
        Vector2 rootPosition,
        out Vector2 elementPosition)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (!float.IsFinite(rootPosition.X) || !float.IsFinite(rootPosition.Y))
        {
            elementPosition = default;
            return false;
        }

        if (element is IInputCoordinateSpace coordinateSpace)
        {
            return coordinateSpace.TryRootToLocal(rootPosition, out elementPosition);
        }

        Matrix3x2 elementToRoot = GetElementToRootTransform(element);
        if (!Matrix3x2.Invert(elementToRoot, out Matrix3x2 rootToElement))
        {
            elementPosition = default;
            return false;
        }

        Vector2 layoutPosition = Vector2.Transform(rootPosition, rootToElement);
        elementPosition = new Vector2(
            layoutPosition.X - element.ArrangedBounds.X,
            layoutPosition.Y - element.ArrangedBounds.Y);
        return float.IsFinite(elementPosition.X) && float.IsFinite(elementPosition.Y);
    }

    internal static Matrix3x2 GetElementToRootTransform(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        Matrix3x2 result = Matrix3x2.Identity;
        for (UIElement? current = element;
            current is not null;
            current = current.VisualParent)
        {
            result *= ElementVisualTransform
                .GetElementTransform(current)
                .ToNumerics();
        }

        return result;
    }
}
