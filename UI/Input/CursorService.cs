using System.Runtime.CompilerServices;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class CursorService
{
    private readonly ConditionalWeakTable<UIElement, CursorBox> cursors = new();
    private readonly ElementInputRouteBuilder routeBuilder;
    private readonly HitTestService hitTestService;

    public CursorService(ElementInputRouteBuilder? routeBuilder = null, HitTestService? hitTestService = null)
    {
        this.routeBuilder = routeBuilder ?? new ElementInputRouteBuilder();
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
        ElementInputRouteMap routeMap = routeBuilder.Build(root);
        HitTestResult? hit = hitTestService.HitTest(root, routeMap, x, y);
        for (UIElement? current = hit?.Element; current is not null; current = current.VisualParent)
        {
            if (cursors.TryGetValue(current, out CursorBox? box))
            {
                return box.Cursor;
            }
        }

        return Cursor.Arrow;
    }

    private sealed class CursorBox(Cursor cursor)
    {
        public Cursor Cursor { get; } = cursor;
    }
}
