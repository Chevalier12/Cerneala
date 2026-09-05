using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

internal interface IGeometricHitTestHost
{
    UIElement? HitTestGeometry(
        ElementInputRouteMap routeMap,
        float rootX,
        float rootY,
        HitTestFilter filter);
}
