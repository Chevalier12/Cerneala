using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

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
        AppendElement(root, renderCache, counters, rootCommands, Matrix3x2.Identity, 1);
        renderCache.MarkRootBuilt();
    }

    private static void AppendElement(
        UIElement element,
        RetainedRenderCache renderCache,
        RenderCounters counters,
        DrawCommandList rootCommands,
        Matrix3x2 ancestorTransform,
        float ancestorOpacity)
    {
        if (!UIElementVisibility.ParticipatesInRendering(element))
        {
            return;
        }

        Matrix3x2 elementTransform = Matrix3x2.Multiply(GetElementTransform(element), ancestorTransform);
        float elementOpacity = ancestorOpacity * element.Opacity * element.PresenceOpacity;
        if (elementOpacity <= 0)
        {
            return;
        }

        counters.CountComposedElement();
        bool hasClip = TryGetClip(element, out LayoutRect clipBounds);
        if (hasClip)
        {
            rootCommands.Add(ApplyRenderScope(DrawCommand.PushClip(ToDrawRect(clipBounds)), elementTransform, 1));
            counters.CountEmittedCommands(1);
        }

        ElementRenderCache localCache = renderCache.GetElementCache(element);
        DrawCommandList localCommands = GetLocalCommands(element, localCache, out float offsetX, out float offsetY);
        foreach (DrawCommand command in localCommands)
        {
            rootCommands.Add(ApplyRenderScope(Translate(command, offsetX, offsetY), elementTransform, elementOpacity));
            counters.CountEmittedCommands(1);
        }

        foreach (UIElement child in element.VisualChildren)
        {
            AppendElement(child, renderCache, counters, rootCommands, elementTransform, elementOpacity);
        }

        if (element.Root is UIRoot root)
        {
            foreach (UIElement child in root.Motion.Presence.GetExitingVisualChildren(element))
            {
                AppendElement(child, renderCache, counters, rootCommands, elementTransform, elementOpacity);
            }
        }

        if (hasClip)
        {
            rootCommands.Add(DrawCommand.PopClip());
            counters.CountEmittedCommands(1);
        }
    }

    private static bool TryGetClip(UIElement element, out LayoutRect bounds)
    {
        if (ClipNode.TryGetClip(element, out ClipNode clip))
        {
            bounds = clip.Bounds;
            return true;
        }

        if (element.ClipToBounds)
        {
            bounds = element.ArrangedBounds;
            return true;
        }

        bounds = default;
        return false;
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
            DrawCommandKind.FillRectangle when command.Brush is not null => DrawCommand.FillRectangle(Translate(command.Rect, offsetX, offsetY), command.Brush, command.BrushOpacity),
            DrawCommandKind.FillRectangle => DrawCommand.FillRectangle(Translate(command.Rect, offsetX, offsetY), command.Color),
            DrawCommandKind.DrawRectangle when command.Brush is not null => DrawCommand.DrawRectangle(Translate(command.Rect, offsetX, offsetY), command.Brush, command.Thickness, command.BrushOpacity),
            DrawCommandKind.DrawRectangle => DrawCommand.DrawRectangle(Translate(command.Rect, offsetX, offsetY), command.Color, command.Thickness),
            DrawCommandKind.FillEllipse when command.Brush is not null => DrawCommand.FillEllipse(Translate(command.Rect, offsetX, offsetY), command.Brush, command.BrushOpacity),
            DrawCommandKind.FillEllipse => DrawCommand.FillEllipse(Translate(command.Rect, offsetX, offsetY), command.Color),
            DrawCommandKind.DrawEllipse when command.Brush is not null => DrawCommand.DrawEllipse(Translate(command.Rect, offsetX, offsetY), command.Brush, command.Thickness, command.BrushOpacity),
            DrawCommandKind.DrawEllipse => DrawCommand.DrawEllipse(Translate(command.Rect, offsetX, offsetY), command.Color, command.Thickness),
            DrawCommandKind.DrawLine when command.Brush is not null => DrawCommand.DrawLine(
                Translate(command.Position, offsetX, offsetY),
                Translate(command.EndPoint, offsetX, offsetY),
                command.Brush,
                command.Thickness,
                command.BrushOpacity),
            DrawCommandKind.DrawLine => DrawCommand.DrawLine(
                Translate(command.Position, offsetX, offsetY),
                Translate(command.EndPoint, offsetX, offsetY),
                command.Color,
                command.Thickness),
            DrawCommandKind.DrawText when command.Brush is not null => DrawCommand.DrawText(
                command.TextRun!,
                Translate(command.Position, offsetX, offsetY),
                command.Brush,
                command.BrushOpacity),
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

    private static DrawCommand ApplyRenderScope(DrawCommand command, Matrix3x2 transform, float opacity)
    {
        return command.Kind switch
        {
            DrawCommandKind.FillRectangle when command.Brush is not null => DrawCommand.FillRectangle(Transform(command.Rect, transform), command.Brush, command.BrushOpacity * opacity),
            DrawCommandKind.FillRectangle => DrawCommand.FillRectangle(Transform(command.Rect, transform), ApplyOpacity(command.Color, opacity)),
            DrawCommandKind.DrawRectangle when command.Brush is not null => DrawCommand.DrawRectangle(Transform(command.Rect, transform), command.Brush, command.Thickness, command.BrushOpacity * opacity),
            DrawCommandKind.DrawRectangle => DrawCommand.DrawRectangle(Transform(command.Rect, transform), ApplyOpacity(command.Color, opacity), command.Thickness),
            DrawCommandKind.FillEllipse when command.Brush is not null => DrawCommand.FillEllipse(Transform(command.Rect, transform), command.Brush, command.BrushOpacity * opacity),
            DrawCommandKind.FillEllipse => DrawCommand.FillEllipse(Transform(command.Rect, transform), ApplyOpacity(command.Color, opacity)),
            DrawCommandKind.DrawEllipse when command.Brush is not null => DrawCommand.DrawEllipse(Transform(command.Rect, transform), command.Brush, command.Thickness, command.BrushOpacity * opacity),
            DrawCommandKind.DrawEllipse => DrawCommand.DrawEllipse(Transform(command.Rect, transform), ApplyOpacity(command.Color, opacity), command.Thickness),
            DrawCommandKind.DrawLine when command.Brush is not null => DrawCommand.DrawLine(
                transform.Transform(command.Position),
                transform.Transform(command.EndPoint),
                command.Brush,
                command.Thickness,
                command.BrushOpacity * opacity),
            DrawCommandKind.DrawLine => DrawCommand.DrawLine(
                transform.Transform(command.Position),
                transform.Transform(command.EndPoint),
                ApplyOpacity(command.Color, opacity),
                command.Thickness),
            DrawCommandKind.DrawText when command.Brush is not null => DrawCommand.DrawText(
                command.TextRun!,
                transform.Transform(command.Position),
                command.Brush,
                command.BrushOpacity * opacity),
            DrawCommandKind.DrawText => DrawCommand.DrawText(command.TextRun!, transform.Transform(command.Position), ApplyOpacity(command.Color, opacity)),
            DrawCommandKind.DrawImage => DrawCommand.DrawImage(command.Image!, Transform(command.Rect, transform), ApplyOpacity(command.Color, opacity)),
            DrawCommandKind.PushClip => DrawCommand.PushClip(Transform(command.Rect, transform)),
            DrawCommandKind.PopClip => command,
            _ => command
        };
    }

    private static Matrix3x2 GetElementTransform(UIElement element)
    {
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
        channelTransform = Matrix3x2.Multiply(channelTransform, element.LayoutCorrectionTransform.Matrix);

        if (channelTransform == Matrix3x2.Identity)
        {
            return Matrix3x2.Identity;
        }

        return Matrix3x2.Multiply(
            Matrix3x2.Multiply(Matrix3x2.CreateTranslation(-pivotX, -pivotY), channelTransform),
            Matrix3x2.CreateTranslation(pivotX, pivotY));
    }

    private static DrawRect Transform(DrawRect rect, Matrix3x2 transform)
    {
        DrawPoint topLeft = transform.Transform(new DrawPoint(rect.X, rect.Y));
        DrawPoint topRight = transform.Transform(new DrawPoint(rect.Right, rect.Y));
        DrawPoint bottomLeft = transform.Transform(new DrawPoint(rect.X, rect.Bottom));
        DrawPoint bottomRight = transform.Transform(new DrawPoint(rect.Right, rect.Bottom));

        float minX = MathF.Min(MathF.Min(topLeft.X, topRight.X), MathF.Min(bottomLeft.X, bottomRight.X));
        float minY = MathF.Min(MathF.Min(topLeft.Y, topRight.Y), MathF.Min(bottomLeft.Y, bottomRight.Y));
        float maxX = MathF.Max(MathF.Max(topLeft.X, topRight.X), MathF.Max(bottomLeft.X, bottomRight.X));
        float maxY = MathF.Max(MathF.Max(topLeft.Y, topRight.Y), MathF.Max(bottomLeft.Y, bottomRight.Y));

        return new DrawRect(minX, minY, maxX - minX, maxY - minY);
    }

    private static Color ApplyOpacity(Color color, float opacity)
    {
        if (opacity >= 1)
        {
            return color;
        }

        return new Color(color.R, color.G, color.B, (byte)Math.Clamp((int)MathF.Round(color.A * opacity), 0, 255));
    }
}
