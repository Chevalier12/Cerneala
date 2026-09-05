using System.Runtime.CompilerServices;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class CursorService
{
    private readonly ConditionalWeakTable<UIElement, CursorBox> cursors = new();
    private readonly HitTestService hitTestService;

    public CursorService(HitTestService? hitTestService = null)
    {
        this.hitTestService = hitTestService ?? new HitTestService();
    }

    public void SetCursor(UIElement element, Cursor cursor)
    {
        ArgumentNullException.ThrowIfNull(element);
        cursors.Remove(element);
        cursors.Add(element, new CursorBox(cursor));
    }

    public Cursor Resolve(UIRoot root, float x, float y)
    {
        ArgumentNullException.ThrowIfNull(root);
        ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
        HitTestResult? hit = hitTestService.HitTest(root, routeMap, x, y);
        IReadOnlyList<UIElement> route = hit is null
            ? []
            : routeMap.GetRouteToRoot(hit.Element);
        foreach (UIElement current in route)
        {
            if (cursors.TryGetValue(current, out CursorBox? box))
            {
                return box.Cursor;
            }

            if (current.Cursor is Cursor cursor)
            {
                return cursor;
            }
        }

        return Cursor.Arrow;
    }

    private sealed class CursorBox(Cursor cursor)
    {
        public Cursor Cursor { get; } = cursor;
    }
}
