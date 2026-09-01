using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.UI.Elements;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Rendering;
using System.Numerics;

namespace Cerneala.UI.Controls;

[Flags]
public enum RenderSurface2DSpriteFlip
{
    None = 0,
    Horizontal = 1,
    Vertical = 2
}

public sealed partial class RenderSurface2DFrame
{
    private readonly DrawingContext drawingContext;
    private readonly DrawCommandList commands;
    private readonly Action<IDrawImage>? trackImageDependency;
    private readonly long contentVersion;
    private bool active = true;

    internal RenderSurface2DFrame(
        DrawCommandList commands,
        DrawRect bounds,
        TimeSpan frameTime,
        Action<IDrawImage>? trackImageDependency = null)
        : this(
            commands,
            bounds,
            frameTime,
            contentVersion: 1,
            trackImageDependency)
    {
    }

    internal RenderSurface2DFrame(
        DrawCommandList commands,
        DrawRect bounds,
        TimeSpan frameTime,
        long contentVersion,
        Action<IDrawImage>? trackImageDependency = null)
    {
        drawingContext = new DrawingContext(
            commands ?? throw new ArgumentNullException(nameof(commands)),
            EnsureActive);
        this.commands = commands;
        Bounds = bounds;
        FrameTime = frameTime;
        this.contentVersion = contentVersion;
        this.trackImageDependency = trackImageDependency;
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

    public void DrawRectangle(DrawRect rectangle, DrawPen pen)
    {
        EnsureActive();
        drawingContext.DrawRectangle(rectangle, pen);
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

    public void DrawEllipse(DrawRect bounds, DrawPen pen)
    {
        EnsureActive();
        drawingContext.DrawEllipse(bounds, pen);
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

    public void DrawLine(DrawPoint start, DrawPoint end, DrawPen pen)
    {
        EnsureActive();
        drawingContext.DrawLine(start, end, pen);
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

    public void FillPath(
        DrawPath path,
        IDrawBrush brush,
        DrawFillRule fillRule = DrawFillRule.NonZero)
    {
        EnsureActive();
        drawingContext.FillPath(path, brush, fillRule);
    }

    public void FillPath(
        DrawPath path,
        Color color,
        DrawFillRule fillRule = DrawFillRule.NonZero)
    {
        EnsureActive();
        drawingContext.FillPath(path, color, fillRule);
    }

    public void FillPath(
        DrawPath path,
        DrawRect sourceBounds,
        DrawRect destination,
        IDrawBrush brush,
        DrawFillRule fillRule = DrawFillRule.NonZero)
    {
        EnsureActive();
        drawingContext.FillPath(path, sourceBounds, destination, brush, fillRule);
    }

    public void FillPath(
        DrawPath path,
        DrawRect sourceBounds,
        DrawRect destination,
        Color color,
        DrawFillRule fillRule = DrawFillRule.NonZero)
    {
        EnsureActive();
        drawingContext.FillPath(path, sourceBounds, destination, color, fillRule);
    }

    public void DrawPath(DrawPath path, DrawPen pen)
    {
        EnsureActive();
        drawingContext.DrawPath(path, pen);
    }

    public void DrawPath(
        DrawPath path,
        DrawRect sourceBounds,
        DrawRect destination,
        DrawPen pen)
    {
        EnsureActive();
        drawingContext.DrawPath(path, sourceBounds, destination, pen);
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

    public void PushClip(
        DrawPath path,
        DrawFillRule fillRule = DrawFillRule.NonZero)
    {
        EnsureActive();
        drawingContext.PushClip(path, fillRule);
    }

    public void PopClip()
    {
        EnsureActive();
        drawingContext.PopClip();
    }

    public void PushTransform(Matrix3x2 transform)
    {
        EnsureActive();
        drawingContext.PushTransform(transform);
    }

    public void PopTransform()
    {
        EnsureActive();
        drawingContext.PopTransform();
    }

    public void PushOpacity(float opacity)
    {
        EnsureActive();
        drawingContext.PushOpacity(opacity);
    }

    public void PopOpacity()
    {
        EnsureActive();
        drawingContext.PopOpacity();
    }

    public void PushBlend(DrawBlendMode blendMode)
    {
        EnsureActive();
        drawingContext.PushBlend(blendMode);
    }

    public void PopBlend()
    {
        EnsureActive();
        drawingContext.PopBlend();
    }

    public void PushLayer(DrawLayerOptions options)
    {
        EnsureActive();
        drawingContext.PushLayer(options);
    }

    public void PopLayer()
    {
        EnsureActive();
        drawingContext.PopLayer();
    }

    public DrawTransformScope Transform(Matrix3x2 transform)
    {
        EnsureActive();
        return drawingContext.Transform(transform);
    }

    public DrawClipScope Clip(DrawRect rectangle)
    {
        EnsureActive();
        return drawingContext.Clip(rectangle);
    }

    public DrawClipScope Clip(
        DrawPath path,
        DrawFillRule fillRule = DrawFillRule.NonZero)
    {
        EnsureActive();
        return drawingContext.Clip(path, fillRule);
    }

    public DrawOpacityScope Opacity(float opacity)
    {
        EnsureActive();
        return drawingContext.Opacity(opacity);
    }

    public DrawBlendScope Blend(DrawBlendMode blendMode)
    {
        EnsureActive();
        return drawingContext.Blend(blendMode);
    }

    public DrawLayerScope Layer(DrawLayerOptions options)
    {
        EnsureActive();
        return drawingContext.Layer(options);
    }

    internal void Complete()
    {
        active = false;
        if (trackImageDependency is null)
        {
            return;
        }

        foreach (DrawCommand command in commands)
        {
            DrawCommandMetadata.Create(command).TrackImageDependencies(
                trackImageDependency);
        }
    }

    internal bool BeginPrism(UIElement owner, DrawRect bounds)
    {
        EnsureActive();
        if (!PrismAttachment.TryGetRenderState(
                owner,
                out PrismInstance? instance,
                out PrismCacheOwnerToken cacheOwnerToken))
        {
            return false;
        }

        PrismDrawScope scope = DrawCommandListBuilder.CreatePrismScope(
            owner,
            instance!,
            cacheOwnerToken,
            bounds,
            Cerneala.UI.Media.Matrix3x2.Identity,
            owner.PrismVisualVersion,
            contentVersion,
            DrawCommandListBuilder.ResolvePrismResources(owner, instance!));
        commands.Add(DrawCommand.BeginPrism(scope));
        return true;
    }

    internal void EndPrism()
    {
        EnsureActive();
        commands.Add(DrawCommand.EndPrism());
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
