using Cerneala.Drawing;

namespace Cerneala.UI.Controls;

[Flags]
public enum RenderSurface2DSpriteFlip
{
    None = 0,
    Horizontal = 1,
    Vertical = 2
}

public sealed class RenderSurface2DFrame
{
    private readonly DrawingContext drawingContext;
    private bool active = true;

    internal RenderSurface2DFrame(
        DrawCommandList commands,
        DrawRect bounds,
        TimeSpan frameTime)
    {
        drawingContext = new DrawingContext(commands ??
            throw new ArgumentNullException(nameof(commands)));
        Bounds = bounds;
        FrameTime = frameTime;
    }

    public DrawRect Bounds { get; }

    public TimeSpan FrameTime { get; }

    public void FillRectangle(DrawRect rectangle, Color color)
    {
        EnsureActive();
        drawingContext.FillRectangle(rectangle, color);
    }

    public void FillRectangle(DrawRect rectangle, IDrawBrush brush)
    {
        EnsureActive();
        drawingContext.FillRectangle(rectangle, brush);
    }

    public void DrawRectangle(DrawRect rectangle, Color color, float thickness)
    {
        EnsureActive();
        drawingContext.DrawRectangle(rectangle, color, thickness);
    }

    public void DrawRectangle(DrawRect rectangle, IDrawBrush brush, float thickness)
    {
        EnsureActive();
        drawingContext.DrawRectangle(rectangle, brush, thickness);
    }

    public void FillEllipse(DrawRect bounds, Color color)
    {
        EnsureActive();
        drawingContext.FillEllipse(bounds, color);
    }

    public void FillEllipse(DrawRect bounds, IDrawBrush brush)
    {
        EnsureActive();
        drawingContext.FillEllipse(bounds, brush);
    }

    public void DrawEllipse(DrawRect bounds, Color color, float thickness)
    {
        EnsureActive();
        drawingContext.DrawEllipse(bounds, color, thickness);
    }

    public void DrawEllipse(DrawRect bounds, IDrawBrush brush, float thickness)
    {
        EnsureActive();
        drawingContext.DrawEllipse(bounds, brush, thickness);
    }

    public void DrawLine(
        DrawPoint start,
        DrawPoint end,
        Color color,
        float thickness)
    {
        EnsureActive();
        drawingContext.DrawLine(start, end, color, thickness);
    }

    public void DrawLine(
        DrawPoint start,
        DrawPoint end,
        IDrawBrush brush,
        float thickness)
    {
        EnsureActive();
        drawingContext.DrawLine(start, end, brush, thickness);
    }

    public void FillPath(
        string pathData,
        DrawRect sourceBounds,
        DrawRect destination,
        IDrawBrush brush)
    {
        EnsureActive();
        drawingContext.FillPath(pathData, sourceBounds, destination, brush);
    }

    public void DrawText(DrawTextRun textRun, DrawPoint position, Color color)
    {
        EnsureActive();
        drawingContext.DrawText(textRun, position, color);
    }

    public void DrawText(DrawTextRun textRun, DrawPoint position, IDrawBrush brush)
    {
        EnsureActive();
        drawingContext.DrawText(textRun, position, brush);
    }

    public void DrawImage(IDrawImage image, DrawRect destination, Color color)
    {
        EnsureActive();
        drawingContext.DrawImage(image, destination, color);
    }

    public void DrawImage(
        IDrawImage image,
        DrawRect destination,
        DrawRect? source,
        Color color,
        float rotation = 0,
        DrawPoint origin = default,
        DrawImageFlip flip = DrawImageFlip.None,
        float layerDepth = 0)
    {
        EnsureActive();
        drawingContext.DrawImage(
            image,
            destination,
            source,
            color,
            rotation,
            origin,
            flip,
            layerDepth);
    }

    public void DrawSprite(
        IDrawImage image,
        DrawRect destination,
        Color tint)
    {
        DrawSprite(
            image,
            destination,
            source: null,
            tint,
            rotation: 0,
            origin: default,
            RenderSurface2DSpriteFlip.None,
            layerDepth: 0);
    }

    public void DrawSprite(
        IDrawImage image,
        DrawRect destination,
        DrawRect? source,
        Color tint,
        float rotation = 0,
        DrawPoint origin = default,
        RenderSurface2DSpriteFlip flip = RenderSurface2DSpriteFlip.None,
        float layerDepth = 0)
    {
        EnsureActive();
        drawingContext.DrawImage(
            image,
            destination,
            source,
            tint,
            rotation,
            origin,
            MapFlip(flip),
            layerDepth);
    }

    public void PushClip(DrawRect rectangle)
    {
        EnsureActive();
        drawingContext.PushClip(rectangle);
    }

    public void PopClip()
    {
        EnsureActive();
        drawingContext.PopClip();
    }

    internal void Complete()
    {
        active = false;
    }

    private void EnsureActive()
    {
        ObjectDisposedException.ThrowIf(!active, this);
    }

    private static DrawImageFlip MapFlip(RenderSurface2DSpriteFlip flip)
    {
        if ((flip & ~(
                RenderSurface2DSpriteFlip.Horizontal |
                RenderSurface2DSpriteFlip.Vertical)) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(flip));
        }

        DrawImageFlip mapped = DrawImageFlip.None;
        if ((flip & RenderSurface2DSpriteFlip.Horizontal) != 0)
        {
            mapped |= DrawImageFlip.Horizontal;
        }
        if ((flip & RenderSurface2DSpriteFlip.Vertical) != 0)
        {
            mapped |= DrawImageFlip.Vertical;
        }

        return mapped;
    }
}
