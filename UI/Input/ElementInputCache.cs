using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class ElementInputCache
{
    private readonly ElementInputRouteBuilder routeBuilder = new();
    private readonly HitTestService hitTestService = new();
    private UIRoot? currentRoot;
    private int currentTreeVersion = -1;
    private bool isDirty = true;
    private ElementInputRouteMap? raisedEventRoutes;
    private ElementInputRouteMap? raisedEventRoutesSource;

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
        if (isDirty ||
            !ReferenceEquals(currentRoot, root) ||
            currentTreeVersion != root.TreeVersion)
        {
            Rebuild(root);
        }

        return RouteMap;
    }

    public ElementInputRouteMap Rebuild(UIRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        RouteMap = routeBuilder.Build(root);
        currentRoot = root;
        currentTreeVersion = root.TreeVersion;
        RebuildCount++;
        isDirty = false;
        return RouteMap;
    }

    // The routes of the events that elements raise themselves (ElementInputRouteBuilder.BuildForRaisedEvents). They
    // change with everything that changes the input routes, so they are built again, when first needed, after each
    // rebuild of RouteMap.
    internal ElementInputRouteMap EnsureRaisedEventRoutesCurrent(UIRoot root)
    {
        ElementInputRouteMap inputRoutes = EnsureCurrent(root);
        if (raisedEventRoutes is null || !ReferenceEquals(raisedEventRoutesSource, inputRoutes))
        {
            raisedEventRoutes = routeBuilder.BuildForRaisedEvents(root);
            raisedEventRoutesSource = inputRoutes;
        }

        return raisedEventRoutes;
    }

    public HitTestResult? HitTest(UIRoot root, float x, float y, HitTestFilter? filter = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        ElementInputRouteMap routeMap = EnsureCurrent(root);
        return hitTestService.HitTest(root, routeMap, x, y, filter);
    }
}
