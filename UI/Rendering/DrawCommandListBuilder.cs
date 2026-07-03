using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Rendering;

public sealed class DrawCommandListBuilder
{
    public void Build(UIElement root, RetainedRenderCache renderCache, RenderCounters counters)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(renderCache);
        ArgumentNullException.ThrowIfNull(counters);

        DrawCommandList rootCommands = renderCache.RootCommands;
        rootCommands.Clear();
        AppendElement(root, renderCache, counters, rootCommands);
        renderCache.MarkRootBuilt();
    }

    private static void AppendElement(
        UIElement element,
        RetainedRenderCache renderCache,
        RenderCounters counters,
        DrawCommandList rootCommands)
    {
        if (element.Visibility != Visibility.Visible || !element.IsVisible)
        {
            return;
        }

        counters.CountComposedElement();
        bool hasClip = ClipNode.TryGetClip(element, out ClipNode clip);
        if (hasClip)
        {
            rootCommands.Add(DrawCommand.PushClip(ToDrawRect(clip.Bounds)));
            counters.CountEmittedCommands(1);
        }

        ElementRenderCache localCache = renderCache.GetElementCache(element);
        localCache.Ensure(element, counters);
        foreach (DrawCommand command in localCache.Commands)
        {
            rootCommands.Add(command);
            counters.CountEmittedCommands(1);
        }

        foreach (UIElement child in element.VisualChildren)
        {
            AppendElement(child, renderCache, counters, rootCommands);
        }

        if (hasClip)
        {
            rootCommands.Add(DrawCommand.PopClip());
            counters.CountEmittedCommands(1);
        }
    }

    private static DrawRect ToDrawRect(LayoutRect rect)
    {
        return new DrawRect(rect.X, rect.Y, rect.Width, rect.Height);
    }
}
