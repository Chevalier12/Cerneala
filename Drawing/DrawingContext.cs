using Cerneala.Drawing.Prism;

namespace Cerneala.Drawing;

public sealed class DrawingContext
{
    private readonly DrawCommandList _commands;

    public DrawingContext(DrawCommandList commands)
    {
        _commands = commands ?? throw new ArgumentNullException(nameof(commands));
    }

    public void FillRectangle(DrawRect rect, Color color)
    {
        _commands.Add(DrawCommand.FillRectangle(rect, color));
    }

    public void FillRectangle(DrawRect rect, IDrawBrush brush)
    {
        _commands.Add(DrawCommand.FillRectangle(rect, brush));
    }

    public void DrawRectangle(DrawRect rect, Color color, float thickness)
    {
        _commands.Add(DrawCommand.DrawRectangle(rect, color, thickness));
    }

    public void DrawRectangle(DrawRect rect, IDrawBrush brush, float thickness)
    {
        _commands.Add(DrawCommand.DrawRectangle(rect, brush, thickness));
    }

    public void FillEllipse(DrawRect bounds, Color color)
    {
        _commands.Add(DrawCommand.FillEllipse(bounds, color));
    }

    public void FillEllipse(DrawRect bounds, IDrawBrush brush)
    {
        _commands.Add(DrawCommand.FillEllipse(bounds, brush));
    }

    public void DrawEllipse(DrawRect bounds, Color color, float thickness)
    {
        _commands.Add(DrawCommand.DrawEllipse(bounds, color, thickness));
    }

    public void DrawEllipse(DrawRect bounds, IDrawBrush brush, float thickness)
    {
        _commands.Add(DrawCommand.DrawEllipse(bounds, brush, thickness));
    }

    public void DrawLine(DrawPoint start, DrawPoint end, Color color, float thickness)
    {
        _commands.Add(DrawCommand.DrawLine(start, end, color, thickness));
    }

    public void DrawLine(DrawPoint start, DrawPoint end, IDrawBrush brush, float thickness)
    {
        _commands.Add(DrawCommand.DrawLine(start, end, brush, thickness));
    }

    public void FillPath(string pathData, DrawRect sourceBounds, DrawRect destination, IDrawBrush brush)
    {
        _commands.Add(DrawCommand.FillPath(pathData, sourceBounds, destination, brush));
    }

    public void DrawText(DrawTextRun textRun, DrawPoint position, Color color)
    {
        _commands.Add(DrawCommand.DrawText(textRun, position, color));
    }

    public void DrawText(DrawTextRun textRun, DrawPoint position, IDrawBrush brush)
    {
        _commands.Add(DrawCommand.DrawText(textRun, position, brush));
    }

    public void DrawImage(IDrawImage image, DrawRect destination, Color color)
    {
        DrawImage(
            image,
            destination,
            source: null,
            color,
            rotation: 0,
            origin: default,
            DrawImageFlip.None,
            layerDepth: 0);
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
        if (image is PrismImage prismImage)
        {
            _commands.Add(DrawCommand.BeginPrism(
                prismImage.CreateDrawScope(destination)));
            DrawImage(
                prismImage.Source,
                destination,
                source,
                color,
                rotation,
                origin,
                flip,
                layerDepth);
            _commands.Add(DrawCommand.EndPrism());
            return;
        }

        _commands.Add(DrawCommand.DrawImage(
            image,
            destination,
            source,
            color,
            rotation,
            origin,
            flip,
            layerDepth));
    }

    internal void DrawRenderSurface2D(
        IRenderSurface2DSource surface,
        DrawRect destination,
        Color color)
    {
        _commands.Add(DrawCommand.RenderSurface2D(surface, destination, color));
    }

    public void PushClip(DrawRect rect)
    {
        _commands.Add(DrawCommand.PushClip(rect));
    }

    public void PopClip()
    {
        _commands.Add(DrawCommand.PopClip());
    }
}
