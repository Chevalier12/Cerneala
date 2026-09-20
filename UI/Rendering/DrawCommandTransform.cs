using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using NumericsMatrix3x2 = System.Numerics.Matrix3x2;

namespace Cerneala.UI.Rendering;

internal static class DrawCommandTransform
{
    internal static DrawCommand Translate(DrawCommand command, float offsetX, float offsetY)
    {
        if (offsetX == 0 && offsetY == 0)
        {
            return command;
        }

        return command.Kind switch
        {
            DrawCommandKind.FillRectangle when command.Brush is not null => DrawCommand.FillRectangle(Translate(command.Rect, offsetX, offsetY), command.Brush, command.BrushOpacity),
            DrawCommandKind.FillRectangle => DrawCommand.FillRectangle(Translate(command.Rect, offsetX, offsetY), command.Color),
            DrawCommandKind.DrawRectangle when command.Pen is not null => DrawCommand.DrawRectangle(Translate(command.Rect, offsetX, offsetY), command.Pen, command.BrushOpacity),
            DrawCommandKind.DrawRectangle when command.Brush is not null => DrawCommand.DrawRectangle(Translate(command.Rect, offsetX, offsetY), command.Brush, command.Thickness, command.BrushOpacity),
            DrawCommandKind.DrawRectangle => DrawCommand.DrawRectangle(Translate(command.Rect, offsetX, offsetY), command.Color, command.Thickness),
            DrawCommandKind.FillRoundedRectangle when command.Brush is not null => DrawCommand.FillRoundedRectangle(Translate(command.Rect, offsetX, offsetY), command.CornerRadius, command.Brush, command.BrushOpacity),
            DrawCommandKind.FillRoundedRectangle => DrawCommand.FillRoundedRectangle(Translate(command.Rect, offsetX, offsetY), command.CornerRadius, command.Color),
            DrawCommandKind.DrawRoundedRectangle when command.Pen is not null => DrawCommand.DrawRoundedRectangle(Translate(command.Rect, offsetX, offsetY), command.CornerRadius, command.Pen, command.BrushOpacity),
            DrawCommandKind.DrawRoundedRectangle => DrawCommand.DrawRoundedRectangle(Translate(command.Rect, offsetX, offsetY), command.CornerRadius, command.Color, command.Thickness),
            DrawCommandKind.FillEllipse when command.Brush is not null => DrawCommand.FillEllipse(Translate(command.Rect, offsetX, offsetY), command.Brush, command.BrushOpacity),
            DrawCommandKind.FillEllipse => DrawCommand.FillEllipse(Translate(command.Rect, offsetX, offsetY), command.Color),
            DrawCommandKind.DrawEllipse when command.Pen is not null => DrawCommand.DrawEllipse(Translate(command.Rect, offsetX, offsetY), command.Pen),
            DrawCommandKind.DrawEllipse when command.Brush is not null => DrawCommand.DrawEllipse(Translate(command.Rect, offsetX, offsetY), command.Brush, command.Thickness, command.BrushOpacity),
            DrawCommandKind.DrawEllipse => DrawCommand.DrawEllipse(Translate(command.Rect, offsetX, offsetY), command.Color, command.Thickness),
            DrawCommandKind.DrawLine when command.Pen is not null => DrawCommand.DrawLine(
                Translate(command.Position, offsetX, offsetY),
                Translate(command.EndPoint, offsetX, offsetY),
                command.Pen,
                command.BrushOpacity),
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
            DrawCommandKind.FillPath when command.Brush is not null => DrawCommand.FillPath(
                command.Path!,
                command.SourceRect,
                Translate(command.Rect, offsetX, offsetY),
                command.Brush,
                command.FillRule,
                command.BrushOpacity),
            DrawCommandKind.FillPath => DrawCommand.FillPath(
                command.Path!,
                command.SourceRect,
                Translate(command.Rect, offsetX, offsetY),
                command.Color,
                command.FillRule),
            DrawCommandKind.DrawPath => DrawCommand.DrawPath(
                command.Path!,
                command.SourceRect,
                Translate(command.Rect, offsetX, offsetY),
                command.Pen!,
                command.BrushOpacity),
            DrawCommandKind.DrawText when command.Brush is not null => DrawCommand.DrawText(
                command.TextRun!,
                Translate(command.Position, offsetX, offsetY),
                command.Brush,
                command.BrushOpacity),
            DrawCommandKind.DrawText => DrawCommand.DrawText(command.TextRun!, Translate(command.Position, offsetX, offsetY), command.Color),
            DrawCommandKind.DrawTextLayout => DrawCommand.DrawTextLayout(
                command.TextLayout!,
                Translate(command.Position, offsetX, offsetY),
                command.BrushOpacity),
            DrawCommandKind.DrawImage => DrawCommand.DrawImage(
                command.Image!,
                Translate(command.Rect, offsetX, offsetY),
                command.ImageOptions!),
            DrawCommandKind.DrawImageQuad or
            DrawCommandKind.DrawNineSlice or
            DrawCommandKind.DrawMesh or
            DrawCommandKind.DrawPointBatch or
            DrawCommandKind.DrawLineBatch or
            DrawCommandKind.DrawSpriteBatch => TransformMeshCommand(
                command,
                point => Translate(point, offsetX, offsetY),
                opacity: 1),
            DrawCommandKind.RenderSurface2D => DrawCommand.RenderSurface2D(command.RenderSurface!, Translate(command.Rect, offsetX, offsetY), command.Color),
            DrawCommandKind.PushTransform => DrawCommand.PushTransform(
                NumericsMatrix3x2.CreateTranslation(-offsetX, -offsetY) *
                command.Transform *
                NumericsMatrix3x2.CreateTranslation(offsetX, offsetY)),
            DrawCommandKind.PushPathClip => DrawCommand.PushClip(
                command.Path!,
                command.SourceRect,
                Translate(command.Rect, offsetX, offsetY),
                command.FillRule),
            DrawCommandKind.PushClip => DrawCommand.PushClip(Translate(command.Rect, offsetX, offsetY)),
            DrawCommandKind.PopClip => command,
            DrawCommandKind.BeginPrism
                when command.PrismScope is PrismDrawScope scope &&
                    scope.IsLocalDrawingScope =>
                DrawCommand.BeginPrism(scope.TranslateLocal(offsetX, offsetY)),
            DrawCommandKind.BeginPrism => command,
            DrawCommandKind.EndPrism => command,
            _ => command
        };
    }

    internal static DrawCommand ApplyOpacity(DrawCommand command, float opacity)
    {
        if (opacity >= 1)
        {
            return command;
        }

        return command.Kind switch
        {
            DrawCommandKind.FillRectangle when command.Brush is not null => DrawCommand.FillRectangle(command.Rect, command.Brush, command.BrushOpacity * opacity),
            DrawCommandKind.FillRectangle => DrawCommand.FillRectangle(command.Rect, ApplyOpacity(command.Color, opacity)),
            DrawCommandKind.DrawRectangle when command.Pen is not null => DrawCommand.DrawRectangle(command.Rect, command.Pen, command.BrushOpacity * opacity),
            DrawCommandKind.DrawRectangle when command.Brush is not null => DrawCommand.DrawRectangle(command.Rect, command.Brush, command.Thickness, command.BrushOpacity * opacity),
            DrawCommandKind.DrawRectangle => DrawCommand.DrawRectangle(command.Rect, ApplyOpacity(command.Color, opacity), command.Thickness),
            DrawCommandKind.FillRoundedRectangle when command.Brush is not null => DrawCommand.FillRoundedRectangle(command.Rect, command.CornerRadius, command.Brush, command.BrushOpacity * opacity),
            DrawCommandKind.FillRoundedRectangle => DrawCommand.FillRoundedRectangle(command.Rect, command.CornerRadius, ApplyOpacity(command.Color, opacity)),
            DrawCommandKind.DrawRoundedRectangle when command.Pen is not null => DrawCommand.DrawRoundedRectangle(command.Rect, command.CornerRadius, command.Pen, command.BrushOpacity * opacity),
            DrawCommandKind.DrawRoundedRectangle => DrawCommand.DrawRoundedRectangle(command.Rect, command.CornerRadius, ApplyOpacity(command.Color, opacity), command.Thickness),
            DrawCommandKind.FillEllipse when command.Brush is not null => DrawCommand.FillEllipse(command.Rect, command.Brush, command.BrushOpacity * opacity),
            DrawCommandKind.FillEllipse => DrawCommand.FillEllipse(command.Rect, ApplyOpacity(command.Color, opacity)),
            DrawCommandKind.DrawEllipse when command.Pen is not null => DrawCommand.DrawEllipse(command.Rect, command.Pen, command.BrushOpacity * opacity),
            DrawCommandKind.DrawEllipse when command.Brush is not null => DrawCommand.DrawEllipse(command.Rect, command.Brush, command.Thickness, command.BrushOpacity * opacity),
            DrawCommandKind.DrawEllipse => DrawCommand.DrawEllipse(command.Rect, ApplyOpacity(command.Color, opacity), command.Thickness),
            DrawCommandKind.DrawLine when command.Pen is not null => DrawCommand.DrawLine(
                command.Position,
                command.EndPoint,
                command.Pen,
                command.BrushOpacity * opacity),
            DrawCommandKind.DrawLine when command.Brush is not null => DrawCommand.DrawLine(
                command.Position,
                command.EndPoint,
                command.Brush,
                command.Thickness,
                command.BrushOpacity * opacity),
            DrawCommandKind.DrawLine => DrawCommand.DrawLine(
                command.Position,
                command.EndPoint,
                ApplyOpacity(command.Color, opacity),
                command.Thickness),
            DrawCommandKind.FillPath when command.Brush is not null => DrawCommand.FillPath(
                command.Path!,
                command.SourceRect,
                command.Rect,
                command.Brush,
                command.FillRule,
                command.BrushOpacity * opacity),
            DrawCommandKind.FillPath => DrawCommand.FillPath(
                command.Path!,
                command.SourceRect,
                command.Rect,
                ApplyOpacity(command.Color, opacity),
                command.FillRule),
            DrawCommandKind.DrawPath => DrawCommand.DrawPath(
                command.Path!,
                command.SourceRect,
                command.Rect,
                command.Pen!,
                command.BrushOpacity * opacity),
            DrawCommandKind.DrawText when command.Brush is not null => DrawCommand.DrawText(
                command.TextRun!,
                command.Position,
                command.Brush,
                command.BrushOpacity * opacity),
            DrawCommandKind.DrawText => DrawCommand.DrawText(command.TextRun!, command.Position, ApplyOpacity(command.Color, opacity)),
            DrawCommandKind.DrawTextLayout => DrawCommand.DrawTextLayout(
                command.TextLayout!,
                command.Position,
                command.BrushOpacity * opacity),
            DrawCommandKind.DrawImage => DrawCommand.DrawImage(
                command.Image!,
                command.Rect,
                ApplyOpacity(command.ImageOptions!, opacity)),
            DrawCommandKind.DrawImageQuad or
            DrawCommandKind.DrawNineSlice or
            DrawCommandKind.DrawMesh or
            DrawCommandKind.DrawPointBatch or
            DrawCommandKind.DrawLineBatch or
            DrawCommandKind.DrawSpriteBatch => TransformMeshCommand(
                command,
                static point => point,
                opacity),
            DrawCommandKind.RenderSurface2D => DrawCommand.RenderSurface2D(command.RenderSurface!, command.Rect, ApplyOpacity(command.Color, opacity)),
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

    private static Color ApplyOpacity(Color color, float opacity)
    {
        if (opacity >= 1)
        {
            return color;
        }

        return new Color(color.R, color.G, color.B, (byte)Math.Clamp((int)MathF.Round(color.A * opacity), 0, 255));
    }

    private static DrawImageOptions ApplyOpacity(
        DrawImageOptions options,
        float opacity) =>
        new(
            options.Source,
            options.Tint,
            options.Opacity * opacity,
            options.Rotation,
            options.Origin,
            options.Flip,
            options.LayerDepth,
            options.Sampling,
            options.AddressMode);

    private static DrawCommand TransformMeshCommand(
        DrawCommand command,
        Func<DrawPoint, DrawPoint> transform,
        float opacity)
    {
        if (command.PointBatch is DrawPointBatch pointBatch)
        {
            DrawPointBatch transformed = pointBatch.Transform(
                transform,
                opacity);
            return DrawCommand.WithMesh(
                command,
                transformed.Mesh,
                pointBatch: transformed);
        }
        if (command.LineBatch is DrawLineBatch lineBatch)
        {
            DrawLineBatch transformed = lineBatch.Transform(
                transform,
                opacity);
            return DrawCommand.WithMesh(
                command,
                transformed.Mesh,
                lineBatch: transformed);
        }
        if (command.SpriteBatch is DrawSpriteBatch spriteBatch)
        {
            DrawSpriteBatch transformed = spriteBatch.Transform(
                transform,
                opacity);
            return DrawCommand.WithMesh(
                command,
                transformed.Mesh,
                spriteBatch: transformed);
        }

        return DrawCommand.WithMesh(
            command,
            command.Mesh!.Transform(transform, opacity));
    }
}
