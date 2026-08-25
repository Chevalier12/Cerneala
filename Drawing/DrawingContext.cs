using Cerneala.Drawing.Prism;
using System.Numerics;

namespace Cerneala.Drawing;

public sealed partial class DrawingContext
{
    private readonly DrawCommandList _commands;
    private readonly Action? ensureActive;
    private readonly List<ScopeFrame> scopeStack = [];
    private long nextScopeToken;

    public DrawingContext(DrawCommandList commands)
        : this(commands, ensureActive: null)
    {
    }

    internal DrawingContext(DrawCommandList commands, Action? ensureActive)
    {
        _commands = commands ?? throw new ArgumentNullException(nameof(commands));
        this.ensureActive = ensureActive;
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

    public void DrawRectangle(DrawRect rect, DrawPen pen)
    {
        _commands.Add(DrawCommand.DrawRectangle(rect, pen));
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

    public void DrawEllipse(DrawRect bounds, DrawPen pen)
    {
        _commands.Add(DrawCommand.DrawEllipse(bounds, pen));
    }

    public void DrawLine(DrawPoint start, DrawPoint end, Color color, float thickness)
    {
        _commands.Add(DrawCommand.DrawLine(start, end, color, thickness));
    }

    public void DrawLine(DrawPoint start, DrawPoint end, IDrawBrush brush, float thickness)
    {
        _commands.Add(DrawCommand.DrawLine(start, end, brush, thickness));
    }

    public void DrawLine(DrawPoint start, DrawPoint end, DrawPen pen)
    {
        _commands.Add(DrawCommand.DrawLine(start, end, pen));
    }

    public void FillPath(string pathData, DrawRect sourceBounds, DrawRect destination, IDrawBrush brush)
    {
        _commands.Add(DrawCommand.FillPath(pathData, sourceBounds, destination, brush));
    }

    public void FillPath(
        DrawPath path,
        IDrawBrush brush,
        DrawFillRule fillRule = DrawFillRule.NonZero)
    {
        _commands.Add(DrawCommand.FillPath(path, brush, fillRule));
    }

    public void FillPath(
        DrawPath path,
        Color color,
        DrawFillRule fillRule = DrawFillRule.NonZero)
    {
        _commands.Add(DrawCommand.FillPath(path, color, fillRule));
    }

    public void FillPath(
        DrawPath path,
        DrawRect sourceBounds,
        DrawRect destination,
        IDrawBrush brush,
        DrawFillRule fillRule = DrawFillRule.NonZero)
    {
        _commands.Add(DrawCommand.FillPath(path, sourceBounds, destination, brush, fillRule));
    }

    public void FillPath(
        DrawPath path,
        DrawRect sourceBounds,
        DrawRect destination,
        Color color,
        DrawFillRule fillRule = DrawFillRule.NonZero)
    {
        _commands.Add(DrawCommand.FillPath(path, sourceBounds, destination, color, fillRule));
    }

    public void DrawPath(DrawPath path, DrawPen pen)
    {
        _commands.Add(DrawCommand.DrawPath(path, pen));
    }

    public void DrawPath(
        DrawPath path,
        DrawRect sourceBounds,
        DrawRect destination,
        DrawPen pen)
    {
        _commands.Add(DrawCommand.DrawPath(path, sourceBounds, destination, pen));
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
        PushRaw(DrawStateScopeKind.Clip, DrawCommand.PushClip(rect));
    }

    public void PushClip(
        DrawPath path,
        DrawFillRule fillRule = DrawFillRule.NonZero)
    {
        PushRaw(DrawStateScopeKind.Clip, DrawCommand.PushClip(path, fillRule));
    }

    public void PopClip()
    {
        PopRaw(DrawStateScopeKind.Clip, DrawCommand.PopClip());
    }

    public void PushTransform(Matrix3x2 transform)
    {
        PushRaw(DrawStateScopeKind.Transform, DrawCommand.PushTransform(transform));
    }

    public void PopTransform()
    {
        PopRaw(DrawStateScopeKind.Transform, DrawCommand.PopTransform());
    }

    public void PushOpacity(float opacity)
    {
        PushRaw(DrawStateScopeKind.Opacity, DrawCommand.PushOpacity(opacity));
    }

    public void PopOpacity()
    {
        PopRaw(DrawStateScopeKind.Opacity, DrawCommand.PopOpacity());
    }

    public void PushBlend(DrawBlendMode blendMode)
    {
        PushRaw(DrawStateScopeKind.Blend, DrawCommand.PushBlend(blendMode));
    }

    public void PopBlend()
    {
        PopRaw(DrawStateScopeKind.Blend, DrawCommand.PopBlend());
    }

    public void PushLayer(DrawLayerOptions options)
    {
        PushRaw(DrawStateScopeKind.Layer, DrawCommand.PushLayer(options));
    }

    public void PopLayer()
    {
        PopRaw(DrawStateScopeKind.Layer, DrawCommand.PopLayer());
    }

    public DrawTransformScope Transform(Matrix3x2 transform)
    {
        long token = PushScoped(
            DrawStateScopeKind.Transform,
            DrawCommand.PushTransform(transform));
        return new DrawTransformScope(this, token);
    }

    public DrawClipScope Clip(DrawRect rect)
    {
        long token = PushScoped(
            DrawStateScopeKind.Clip,
            DrawCommand.PushClip(rect));
        return new DrawClipScope(this, token);
    }

    public DrawClipScope Clip(
        DrawPath path,
        DrawFillRule fillRule = DrawFillRule.NonZero)
    {
        long token = PushScoped(
            DrawStateScopeKind.Clip,
            DrawCommand.PushClip(path, fillRule));
        return new DrawClipScope(this, token);
    }

    public DrawOpacityScope Opacity(float opacity)
    {
        long token = PushScoped(
            DrawStateScopeKind.Opacity,
            DrawCommand.PushOpacity(opacity));
        return new DrawOpacityScope(this, token);
    }

    public DrawBlendScope Blend(DrawBlendMode blendMode)
    {
        long token = PushScoped(
            DrawStateScopeKind.Blend,
            DrawCommand.PushBlend(blendMode));
        return new DrawBlendScope(this, token);
    }

    public DrawLayerScope Layer(DrawLayerOptions options)
    {
        long token = PushScoped(
            DrawStateScopeKind.Layer,
            DrawCommand.PushLayer(options));
        return new DrawLayerScope(this, token);
    }

    internal void PopScoped(DrawStateScopeKind kind, long token)
    {
        ensureActive?.Invoke();
        if (scopeStack.Count == 0 ||
            scopeStack[^1] != new ScopeFrame(kind, token))
        {
            throw new InvalidOperationException(
                $"The {kind} drawing scope must be disposed exactly once in LIFO order.");
        }

        scopeStack.RemoveAt(scopeStack.Count - 1);
        _commands.Add(CreatePopCommand(kind));
    }

    private void PushRaw(DrawStateScopeKind kind, DrawCommand command)
    {
        ensureActive?.Invoke();
        scopeStack.Add(new ScopeFrame(kind, Token: 0));
        _commands.Add(command);
    }

    private long PushScoped(DrawStateScopeKind kind, DrawCommand command)
    {
        ensureActive?.Invoke();
        long token = checked(++nextScopeToken);
        scopeStack.Add(new ScopeFrame(kind, token));
        _commands.Add(command);
        return token;
    }

    private void PopRaw(DrawStateScopeKind kind, DrawCommand command)
    {
        ensureActive?.Invoke();
        if (scopeStack.Count == 0 || scopeStack[^1].Kind != kind)
        {
            throw new InvalidOperationException(
                $"Pop{kind} does not match the current drawing state scope.");
        }

        scopeStack.RemoveAt(scopeStack.Count - 1);
        _commands.Add(command);
    }

    private static DrawCommand CreatePopCommand(DrawStateScopeKind kind) =>
        kind switch
        {
            DrawStateScopeKind.Transform => DrawCommand.PopTransform(),
            DrawStateScopeKind.Clip => DrawCommand.PopClip(),
            DrawStateScopeKind.Opacity => DrawCommand.PopOpacity(),
            DrawStateScopeKind.Blend => DrawCommand.PopBlend(),
            DrawStateScopeKind.Layer => DrawCommand.PopLayer(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

    private readonly record struct ScopeFrame(
        DrawStateScopeKind Kind,
        long Token);
}
