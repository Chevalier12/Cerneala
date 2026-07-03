using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class ElementInputCache
{
    private readonly ElementInputRouteBuilder routeBuilder = new();
    private readonly HitTestService hitTestService = new();
    private bool isDirty = true;

    public ElementInputCache()
    {
        RouteMap = new ElementInputRouteMap();
        LastInvalidationReason = "Initial input cache";
    }

    public ElementInputRouteMap RouteMap { get; private set; }

    public bool IsDirty => isDirty;

    public int RebuildCount { get; private set; }

    public string LastInvalidationReason { get; private set; }

    public void Invalidate(string reason)
    {
        LastInvalidationReason = string.IsNullOrWhiteSpace(reason)
            ? "Input route changed"
            : reason;
        isDirty = true;
    }

    public ElementInputRouteMap EnsureCurrent(UIRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        if (isDirty)
        {
            Rebuild(root);
        }

        return RouteMap;
    }

    private ElementInputRouteMap Rebuild(UIRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        RouteMap = routeBuilder.Build(root);
        RebuildCount++;
        isDirty = false;
        return RouteMap;
    }

    public HitTestResult? HitTest(UIRoot root, float x, float y, HitTestFilter? filter = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        ElementInputRouteMap routeMap = EnsureCurrent(root);
        return hitTestService.HitTest(root, routeMap, x, y, filter);
    }
}
