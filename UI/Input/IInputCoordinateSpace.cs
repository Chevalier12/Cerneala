using System.Numerics;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Input;

internal interface IInputCoordinateSpace
{
    bool TryRootToLocal(Vector2 rootPosition, out Vector2 localPosition);

    LayoutRect GetRootBounds();
}
