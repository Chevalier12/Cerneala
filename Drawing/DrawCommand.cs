using Cerneala.Drawing.Prism;
using System.Numerics;

namespace Cerneala.Drawing;

public readonly partial record struct DrawCommand
{
    private static readonly DrawPath UnitEllipsePath = new DrawPathBuilder()
        .MoveTo(new DrawPoint(1, 0.5f))
        .ArcTo(0.5f, 0.5f, 0, false, true, new DrawPoint(0, 0.5f))
        .ArcTo(0.5f, 0.5f, 0, false, true, new DrawPoint(1, 0.5f))
        .Close()
        .Build();

    private DrawCommand(
        DrawCommandKind kind,
        DrawRect rect,
        Color color,
        float thickness,
        string? text,
        DrawTextRun? textRun,
        DrawPoint position,
        DrawPoint endPoint,
        IDrawImage? image,
        IDrawFont? font,
        IDrawBrush? brush,
        float brushOpacity,
        string? pathData = null,
        DrawRect sourceRect = default,
        PrismDrawScope? prismScope = null,
        IRenderSurface2DSource? renderSurface = null,
        DrawRect? imageSource = null,
        float imageRotation = 0,
        DrawPoint imageOrigin = default,
        DrawImageFlip imageFlip = DrawImageFlip.None,
        float layerDepth = 0,
        DrawPath? path = null,
        DrawFillRule fillRule = DrawFillRule.NonZero,
        DrawPen? pen = null,
        Matrix3x2 transform = default,
        float opacity = 1,
        DrawBlendMode blendMode = DrawBlendMode.Normal,
        DrawLayerOptions? layerOptions = null,
        DrawCornerRadius cornerRadius = default,
        DrawImageOptions? imageOptions = null,
        DrawInsets insets = default,
        DrawMesh2D? mesh = null,
        DrawPointBatch? pointBatch = null,
        DrawLineBatch? lineBatch = null,
        DrawSpriteBatch? spriteBatch = null,
        DrawTextLayout? textLayout = null,
        long retainedVersion = 0)
    {
        Kind = kind;
        Rect = rect;
        Color = color;
        Thickness = thickness;
        Text = text;
        TextRun = textRun;
        Position = position;
        EndPoint = endPoint;
        Image = image;
        Font = font;
        Brush = brush;
        BrushOpacity = brushOpacity;
        PathData = pathData;
        SourceRect = sourceRect;
        PrismScope = prismScope;
        RenderSurface = renderSurface;
        ImageSource = imageSource;
        ImageRotation = imageRotation;
        ImageOrigin = imageOrigin;
        ImageFlip = imageFlip;
        LayerDepth = layerDepth;
        Path = path;
        FillRule = fillRule;
        Pen = pen;
        Transform = transform;
        Opacity = opacity;
        BlendMode = blendMode;
        LayerOptions = layerOptions;
        CornerRadius = cornerRadius;
        ImageOptions = imageOptions;
        Insets = insets;
        Mesh = mesh;
        PointBatch = pointBatch;
        LineBatch = lineBatch;
        SpriteBatch = spriteBatch;
        TextLayout = textLayout;
        RetainedVersion = retainedVersion;
    }

    public DrawCommandKind Kind { get; }

    public DrawRect Rect { get; }

    public Color Color { get; }

    public float Thickness { get; }

    public string? Text { get; }

    public DrawTextRun? TextRun { get; }

    public DrawPoint Position { get; }

    public DrawPoint EndPoint { get; }

    public IDrawImage? Image { get; }

    public DrawRect? ImageSource { get; }

    public float ImageRotation { get; }

    public DrawPoint ImageOrigin { get; }

    public DrawImageFlip ImageFlip { get; }

    public float LayerDepth { get; }

    public IDrawFont? Font { get; }

    public IDrawBrush? Brush { get; }

    public float BrushOpacity { get; }

    public string? PathData { get; }

    public DrawRect SourceRect { get; }

    public DrawPath? Path { get; }

    public DrawFillRule FillRule { get; }

    public DrawPen? Pen { get; }

    public Matrix3x2 Transform { get; }

    public float Opacity { get; }

    public DrawBlendMode BlendMode { get; }

    public DrawLayerOptions? LayerOptions { get; }

    public DrawCornerRadius CornerRadius { get; }

    public DrawImageOptions? ImageOptions { get; }

    public DrawInsets Insets { get; }

    public DrawMesh2D? Mesh { get; }

    public DrawPointBatch? PointBatch { get; }

    public DrawLineBatch? LineBatch { get; }

    public DrawSpriteBatch? SpriteBatch { get; }

    public DrawTextLayout? TextLayout { get; }

    internal long RetainedVersion { get; }

    public PrismDrawScope? PrismScope { get; }

    internal IRenderSurface2DSource? RenderSurface { get; }

    public static DrawCommand FillRectangle(DrawRect rect, Color color)
    {
        return new DrawCommand(DrawCommandKind.FillRectangle, rect, color, 0, null, null, default, default, null, null, null, 1);
    }

    public static DrawCommand FillRectangle(DrawRect rect, IDrawBrush brush, float opacity = 1)
    {
        return CreateBrushCommand(DrawCommandKind.FillRectangle, rect, default, default, brush, 0, opacity);
    }

    public static DrawCommand DrawRectangle(DrawRect rect, Color color, float thickness)
    {
        DrawArgument.ThrowIfNotValidPixelSize(thickness, nameof(thickness));

        return new DrawCommand(DrawCommandKind.DrawRectangle, rect, color, thickness, null, null, default, default, null, null, null, 1);
    }

    public static DrawCommand DrawRectangle(DrawRect rect, IDrawBrush brush, float thickness, float opacity = 1)
    {
        DrawArgument.ThrowIfNotValidPixelSize(thickness, nameof(thickness));
        return CreateBrushCommand(DrawCommandKind.DrawRectangle, rect, default, default, brush, thickness, opacity);
    }

    public static DrawCommand DrawRectangle(DrawRect rect, DrawPen pen)
    {
        return DrawRectangle(rect, pen, 1);
    }

    internal static DrawCommand DrawRectangle(
        DrawRect rect,
        DrawPen pen,
        float opacity)
    {
        ArgumentNullException.ThrowIfNull(pen);
        return CreatePenCommand(
            DrawCommandKind.DrawRectangle,
            rect,
            default,
            default,
            pen,
            opacity);
    }

    public static DrawCommand FillEllipse(DrawRect bounds, Color color)
    {
        return new DrawCommand(
            DrawCommandKind.FillEllipse,
            bounds,
            color,
            0,
            null,
            null,
            default,
            default,
            null,
            null,
            null,
            1,
            sourceRect: UnitEllipsePath.Bounds,
            path: UnitEllipsePath);
    }

    public static DrawCommand FillEllipse(DrawRect bounds, IDrawBrush brush, float opacity = 1)
    {
        ArgumentNullException.ThrowIfNull(brush);
        ThrowIfNotOpacity(opacity);
        return new DrawCommand(
            DrawCommandKind.FillEllipse,
            bounds,
            default,
            0,
            null,
            null,
            default,
            default,
            null,
            null,
            brush,
            opacity,
            sourceRect: UnitEllipsePath.Bounds,
            path: UnitEllipsePath);
    }

    public static DrawCommand DrawEllipse(DrawRect bounds, Color color, float thickness)
    {
        DrawArgument.ThrowIfNotValidPixelSize(thickness, nameof(thickness));

        return new DrawCommand(DrawCommandKind.DrawEllipse, bounds, color, thickness, null, null, default, default, null, null, null, 1);
    }

    public static DrawCommand DrawEllipse(DrawRect bounds, IDrawBrush brush, float thickness, float opacity = 1)
    {
        DrawArgument.ThrowIfNotValidPixelSize(thickness, nameof(thickness));
        return CreateBrushCommand(DrawCommandKind.DrawEllipse, bounds, default, default, brush, thickness, opacity);
    }

    public static DrawCommand DrawEllipse(DrawRect bounds, DrawPen pen)
    {
        return DrawEllipse(bounds, pen, 1);
    }

    internal static DrawCommand DrawEllipse(
        DrawRect bounds,
        DrawPen pen,
        float opacity)
    {
        ArgumentNullException.ThrowIfNull(pen);
        return CreatePenCommand(
            DrawCommandKind.DrawEllipse,
            bounds,
            default,
            default,
            pen,
            opacity);
    }

    public static DrawCommand DrawLine(DrawPoint start, DrawPoint end, Color color, float thickness)
    {
        ThrowIfPointOutsidePixelRange(start, nameof(start));
        ThrowIfPointOutsidePixelRange(end, nameof(end));
        DrawArgument.ThrowIfNotValidPixelSize(thickness, nameof(thickness));

        return new DrawCommand(DrawCommandKind.DrawLine, default, color, thickness, null, null, start, end, null, null, null, 1);
    }

    public static DrawCommand DrawLine(DrawPoint start, DrawPoint end, IDrawBrush brush, float thickness, float opacity = 1)
    {
        ThrowIfPointOutsidePixelRange(start, nameof(start));
        ThrowIfPointOutsidePixelRange(end, nameof(end));
        DrawArgument.ThrowIfNotValidPixelSize(thickness, nameof(thickness));
        return CreateBrushCommand(DrawCommandKind.DrawLine, default, start, end, brush, thickness, opacity);
    }

    public static DrawCommand DrawLine(DrawPoint start, DrawPoint end, DrawPen pen)
    {
        return DrawLine(start, end, pen, 1);
    }

    internal static DrawCommand DrawLine(
        DrawPoint start,
        DrawPoint end,
        DrawPen pen,
        float opacity)
    {
        ThrowIfPointOutsidePixelRange(start, nameof(start));
        ThrowIfPointOutsidePixelRange(end, nameof(end));
        ArgumentNullException.ThrowIfNull(pen);
        return CreatePenCommand(
            DrawCommandKind.DrawLine,
            default,
            start,
            end,
            pen,
            opacity);
    }

    public static DrawCommand FillPath(string pathData, DrawRect sourceBounds, DrawRect destination, IDrawBrush brush, float opacity = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pathData);
        return FillPath(
            DrawPathParser.ParseSvg(pathData),
            sourceBounds,
            destination,
            brush,
            DrawFillRule.NonZero,
            opacity,
            pathData);
    }

    public static DrawCommand FillPath(
        DrawPath path,
        IDrawBrush brush,
        DrawFillRule fillRule = DrawFillRule.NonZero,
        float opacity = 1)
    {
        ArgumentNullException.ThrowIfNull(path);
        return FillPath(path, path.Bounds, path.Bounds, brush, fillRule, opacity);
    }

    public static DrawCommand FillPath(
        DrawPath path,
        Color color,
        DrawFillRule fillRule = DrawFillRule.NonZero)
    {
        ArgumentNullException.ThrowIfNull(path);
        return FillPath(path, path.Bounds, path.Bounds, color, fillRule);
    }

    public static DrawCommand FillPath(
        DrawPath path,
        DrawRect sourceBounds,
        DrawRect destination,
        IDrawBrush brush,
        DrawFillRule fillRule = DrawFillRule.NonZero,
        float opacity = 1) =>
        FillPath(path, sourceBounds, destination, brush, fillRule, opacity, pathData: null);

    public static DrawCommand FillPath(
        DrawPath path,
        DrawRect sourceBounds,
        DrawRect destination,
        Color color,
        DrawFillRule fillRule = DrawFillRule.NonZero)
    {
        ValidatePathFill(path, sourceBounds, fillRule);
        return new DrawCommand(
            DrawCommandKind.FillPath,
            destination,
            color,
            0,
            null,
            null,
            default,
            default,
            null,
            null,
            null,
            1,
            sourceRect: sourceBounds,
            path: path,
            fillRule: fillRule);
    }

    public static DrawCommand DrawPath(DrawPath path, DrawPen pen)
    {
        ArgumentNullException.ThrowIfNull(path);
        return DrawPath(path, path.Bounds, path.Bounds, pen, 1);
    }

    public static DrawCommand DrawPath(
        DrawPath path,
        DrawRect sourceBounds,
        DrawRect destination,
        DrawPen pen)
    {
        return DrawPath(path, sourceBounds, destination, pen, 1);
    }

    internal static DrawCommand DrawPath(
        DrawPath path,
        DrawRect sourceBounds,
        DrawRect destination,
        DrawPen pen,
        float opacity)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(pen);
        ThrowIfNotOpacity(opacity);
        return new DrawCommand(
            DrawCommandKind.DrawPath,
            destination,
            default,
            pen.Thickness,
            null,
            null,
            default,
            default,
            null,
            null,
            pen.Brush,
            opacity,
            sourceRect: sourceBounds,
            path: path,
            pen: pen);
    }

    public static DrawCommand DrawText(DrawTextRun textRun, DrawPoint position, Color color)
    {
        ArgumentNullException.ThrowIfNull(textRun);
        ThrowIfPointOutsidePixelRange(position, nameof(position));

        return new DrawCommand(DrawCommandKind.DrawText, default, color, 0, textRun.Text, textRun, position, default, null, textRun.Font, null, 1);
    }

    public static DrawCommand DrawText(DrawTextRun textRun, DrawPoint position, IDrawBrush brush, float opacity = 1)
    {
        ArgumentNullException.ThrowIfNull(textRun);
        ThrowIfPointOutsidePixelRange(position, nameof(position));
        ArgumentNullException.ThrowIfNull(brush);
        if (!float.IsFinite(opacity) || opacity < 0 || opacity > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(opacity));
        }

        return new DrawCommand(DrawCommandKind.DrawText, default, default, 0, textRun.Text, textRun, position, default, null, textRun.Font, brush, opacity);
    }

    public static DrawCommand DrawImage(IDrawImage image, DrawRect destination, Color color)
    {
        return DrawImage(image, destination, new DrawImageOptions(tint: color));
    }

    public static DrawCommand DrawImage(
        IDrawImage image,
        DrawRect destination,
        DrawRect? source,
        Color color,
        float rotation = 0,
        DrawPoint origin = default,
        DrawImageFlip flip = DrawImageFlip.None,
        float layerDepth = 0)
    {
        return DrawImage(
            image,
            destination,
            new DrawImageOptions(
                source,
                color,
                rotation: rotation,
                origin: origin,
                flip: flip,
                layerDepth: layerDepth));
    }

    internal static DrawCommand RenderSurface2D(
        IRenderSurface2DSource surface,
        DrawRect destination,
        Color color)
    {
        ArgumentNullException.ThrowIfNull(surface);

        return new DrawCommand(
            DrawCommandKind.RenderSurface2D,
            destination,
            color,
            0,
            null,
            null,
            default,
            default,
            null,
            null,
            null,
            1,
            renderSurface: surface);
    }

    public static DrawCommand PushClip(DrawRect rect)
    {
        return new DrawCommand(DrawCommandKind.PushClip, rect, default, 0, null, null, default, default, null, null, null, 1);
    }

    public static DrawCommand PopClip()
    {
        return new DrawCommand(DrawCommandKind.PopClip, default, default, 0, null, null, default, default, null, null, null, 1);
    }

    public static DrawCommand PushTransform(Matrix3x2 transform)
    {
        if (!IsFinite(transform))
        {
            throw new ArgumentOutOfRangeException(nameof(transform));
        }
        return new DrawCommand(
            DrawCommandKind.PushTransform,
            default,
            default,
            0,
            null,
            null,
            default,
            default,
            null,
            null,
            null,
            1,
            transform: transform);
    }

    public static DrawCommand PopTransform() =>
        CreateStateCommand(DrawCommandKind.PopTransform);

    public static DrawCommand PushClip(
        DrawPath path,
        DrawFillRule fillRule = DrawFillRule.NonZero)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (!Enum.IsDefined(fillRule))
        {
            throw new ArgumentOutOfRangeException(nameof(fillRule));
        }
        return new DrawCommand(
            DrawCommandKind.PushPathClip,
            path.Bounds,
            default,
            0,
            null,
            null,
            default,
            default,
            null,
            null,
            null,
            1,
            sourceRect: path.Bounds,
            path: path,
            fillRule: fillRule);
    }

    public static DrawCommand PushOpacity(float opacity)
    {
        ThrowIfNotOpacity(opacity);
        return new DrawCommand(
            DrawCommandKind.PushOpacity,
            default,
            default,
            0,
            null,
            null,
            default,
            default,
            null,
            null,
            null,
            1,
            opacity: opacity);
    }

    public static DrawCommand PopOpacity() =>
        CreateStateCommand(DrawCommandKind.PopOpacity);

    public static DrawCommand PushBlend(DrawBlendMode blendMode)
    {
        if (!Enum.IsDefined(blendMode))
        {
            throw new ArgumentOutOfRangeException(nameof(blendMode));
        }
        return new DrawCommand(
            DrawCommandKind.PushBlend,
            default,
            default,
            0,
            null,
            null,
            default,
            default,
            null,
            null,
            null,
            1,
            blendMode: blendMode);
    }

    public static DrawCommand PopBlend() =>
        CreateStateCommand(DrawCommandKind.PopBlend);

    public static DrawCommand PushLayer(DrawLayerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new DrawCommand(
            DrawCommandKind.PushLayer,
            default,
            default,
            0,
            null,
            null,
            default,
            default,
            null,
            null,
            null,
            1,
            layerOptions: options);
    }

    public static DrawCommand PopLayer() =>
        CreateStateCommand(DrawCommandKind.PopLayer);

    public static DrawCommand BeginPrism(PrismDrawScope scope)
    {
        long retainedVersion = HashCode.Combine(
            scope.StructuralVersion.Value,
            scope.ValueVersion.Value,
            scope.VisualContentVersion,
            scope.LowerUiVersion);
        return new DrawCommand(
            DrawCommandKind.BeginPrism,
            default,
            default,
            0,
            null,
            null,
            default,
            default,
            null,
            null,
            null,
            1,
            prismScope: scope,
            retainedVersion: retainedVersion);
    }

    public static DrawCommand EndPrism()
    {
        return new DrawCommand(
            DrawCommandKind.EndPrism,
            default,
            default,
            0,
            null,
            null,
            default,
            default,
            null,
            null,
            null,
            1);
    }

    private static DrawCommand CreateBrushCommand(
        DrawCommandKind kind,
        DrawRect rect,
        DrawPoint position,
        DrawPoint endPoint,
        IDrawBrush brush,
        float thickness,
        float opacity)
    {
        ArgumentNullException.ThrowIfNull(brush);
        if (!float.IsFinite(opacity) || opacity < 0 || opacity > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(opacity));
        }

        return new DrawCommand(kind, rect, default, thickness, null, null, position, endPoint, null, null, brush, opacity);
    }

    private static DrawCommand CreatePenCommand(
        DrawCommandKind kind,
        DrawRect rect,
        DrawPoint position,
        DrawPoint endPoint,
        DrawPen pen,
        float opacity)
    {
        ThrowIfNotOpacity(opacity);
        return new DrawCommand(
            kind,
            rect,
            default,
            pen.Thickness,
            null,
            null,
            position,
            endPoint,
            null,
            null,
            pen.Brush,
            opacity,
            pen: pen);
    }

    private static void ThrowIfNotOpacity(float opacity)
    {
        if (!float.IsFinite(opacity) || opacity < 0 || opacity > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(opacity));
        }
    }

    private static DrawCommand CreateStateCommand(DrawCommandKind kind) =>
        new(
            kind,
            default,
            default,
            0,
            null,
            null,
            default,
            default,
            null,
            null,
            null,
            1);

    private static bool IsFinite(Matrix3x2 matrix) =>
        float.IsFinite(matrix.M11) &&
        float.IsFinite(matrix.M12) &&
        float.IsFinite(matrix.M21) &&
        float.IsFinite(matrix.M22) &&
        float.IsFinite(matrix.M31) &&
        float.IsFinite(matrix.M32);

    private static DrawCommand FillPath(
        DrawPath path,
        DrawRect sourceBounds,
        DrawRect destination,
        IDrawBrush brush,
        DrawFillRule fillRule,
        float opacity,
        string? pathData)
    {
        ValidatePathFill(path, sourceBounds, fillRule);
        ArgumentNullException.ThrowIfNull(brush);
        if (!float.IsFinite(opacity) || opacity < 0 || opacity > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(opacity));
        }

        return new DrawCommand(
            DrawCommandKind.FillPath,
            destination,
            default,
            0,
            null,
            null,
            default,
            default,
            null,
            null,
            brush,
            opacity,
            pathData,
            sourceBounds,
            path: path,
            fillRule: fillRule);
    }

    private static void ValidatePathFill(
        DrawPath path,
        DrawRect sourceBounds,
        DrawFillRule fillRule)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (sourceBounds.Width <= 0 || sourceBounds.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceBounds));
        }
        if (!Enum.IsDefined(fillRule))
        {
            throw new ArgumentOutOfRangeException(nameof(fillRule));
        }
    }

    private static void ThrowIfPointOutsidePixelRange(DrawPoint point, string parameterName)
    {
        DrawArgument.ThrowIfNotValidPixelCoordinate(point.X, parameterName);
        DrawArgument.ThrowIfNotValidPixelCoordinate(point.Y, parameterName);
    }
}
