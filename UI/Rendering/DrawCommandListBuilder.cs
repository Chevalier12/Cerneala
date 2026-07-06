using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
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
        if (!UIElementVisibility.ParticipatesInRendering(element))
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
        DrawCommandList localCommands = GetLocalCommands(element, localCache, out float offsetX, out float offsetY);
        foreach (DrawCommand command in localCommands)
        {
            rootCommands.Add(Translate(command, offsetX, offsetY));
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

    private static DrawCommandList GetLocalCommands(
        UIElement element,
        ElementRenderCache localCache,
        out float offsetX,
        out float offsetY)
    {
        if (CanReuseTranslatedCommands(element, localCache, out offsetX, out offsetY))
        {
            return localCache.Commands;
        }

        offsetX = 0;
        offsetY = 0;
        return localCache.GetValidCommands(element);
    }

    private static bool CanReuseTranslatedCommands(
        UIElement element,
        ElementRenderCache localCache,
        out float offsetX,
        out float offsetY)
    {
        offsetX = 0;
        offsetY = 0;
        if (!localCache.IsValid ||
            element.DirtyState.Has(InvalidationFlags.Render) ||
            localCache.Dependencies != element.RenderDependencies ||
            localCache.ContentBounds.Width != element.ArrangedBounds.Width ||
            localCache.ContentBounds.Height != element.ArrangedBounds.Height)
        {
            return false;
        }

        offsetX = element.ArrangedBounds.X - localCache.ContentBounds.X;
        offsetY = element.ArrangedBounds.Y - localCache.ContentBounds.Y;
        return offsetX != 0 || offsetY != 0;
    }

    private static DrawCommand Translate(DrawCommand command, float offsetX, float offsetY)
    {
        if (offsetX == 0 && offsetY == 0)
        {
            return command;
        }

        return command.Kind switch
        {
            DrawCommandKind.FillRectangle => DrawCommand.FillRectangle(Translate(command.Rect, offsetX, offsetY), command.Color),
            DrawCommandKind.DrawRectangle => DrawCommand.DrawRectangle(Translate(command.Rect, offsetX, offsetY), command.Color, command.Thickness),
            DrawCommandKind.FillEllipse => DrawCommand.FillEllipse(Translate(command.Rect, offsetX, offsetY), command.Color),
            DrawCommandKind.DrawEllipse => DrawCommand.DrawEllipse(Translate(command.Rect, offsetX, offsetY), command.Color, command.Thickness),
            DrawCommandKind.DrawLine => DrawCommand.DrawLine(
                Translate(command.Position, offsetX, offsetY),
                Translate(command.EndPoint, offsetX, offsetY),
                command.Color,
                command.Thickness),
            DrawCommandKind.DrawText => DrawCommand.DrawText(command.TextRun!, Translate(command.Position, offsetX, offsetY), command.Color),
            DrawCommandKind.DrawImage => DrawCommand.DrawImage(command.Image!, Translate(command.Rect, offsetX, offsetY), command.Color),
            DrawCommandKind.PushClip => DrawCommand.PushClip(Translate(command.Rect, offsetX, offsetY)),
            DrawCommandKind.PopClip => command,
            _ => command
        };
    }

    private static DrawRect Translate(DrawRect rect, float offsetX, float offsetY)
    {
        return new DrawRect(rect.X + offsetX, rect.Y + offsetY, rect.Width, rect.Height);
    }

    private static DrawPoint Translate(DrawPoint point, float offsetX, float offsetY)
    {
        return new DrawPoint(point.X + offsetX, point.Y + offsetY);
    }
}
