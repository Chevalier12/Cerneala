using System.Collections.ObjectModel;
using Cerneala.Drawing.Prism;

namespace Cerneala.Drawing;

internal sealed class DrawCommandMetadata
{
    private DrawCommandMetadata(
        DrawRect? bounds,
        IReadOnlyList<object> resources,
        bool isContextSensitive,
        DrawCommand retainedIdentity)
    {
        Bounds = bounds;
        Resources = resources;
        IsContextSensitive = isContextSensitive;
        RetainedIdentity = retainedIdentity;
    }

    internal DrawRect? Bounds { get; }

    internal IReadOnlyList<object> Resources { get; }

    internal bool IsContextSensitive { get; }

    internal DrawCommand RetainedIdentity { get; }

    internal static DrawCommandMetadata Create(DrawCommand command)
    {
        List<object> resources = [];
        HashSet<object> seen = new(ReferenceEqualityComparer.Instance);

        AddCommandResources(command);

        return new DrawCommandMetadata(
            ResolveBounds(command),
            new ReadOnlyCollection<object>(resources),
            IsContextSensitiveKind(command.Kind),
            command);

        void AddCommandResources(DrawCommand current)
        {
            Add(current.Image);
            Add(current.Font);
            Add(current.Brush);
            Add(current.Path);
            Add(current.Pen);
            Add(current.Mesh);
            Add(current.PointBatch);
            Add(current.LineBatch);
            Add(current.SpriteBatch);
            Add(current.TextLayout);
            Add(current.RenderSurface);
            if (current.PrismScope is not PrismDrawScope prismScope)
            {
                return;
            }

            Add(prismScope.Instance);
            Add(prismScope.ImageDependency);
            foreach (IDrawImage image in prismScope.Resources.Images)
            {
                Add(image);
            }
        }

        void Add(object? resource)
        {
            if (resource is null || !seen.Add(resource))
            {
                return;
            }

            resources.Add(resource);
            switch (resource)
            {
                case DrawPen pen:
                    Add(pen.Brush);
                    break;
                case DrawMesh2D mesh:
                    Add(mesh.Image);
                    break;
                case DrawSpriteBatch batch:
                    Add(batch.Image);
                    break;
                case DrawTextLayout layout:
                    foreach (DrawTextLayoutLine line in layout.Lines)
                    {
                        foreach (DrawTextLayoutRun run in line.Runs)
                        {
                            Add(run.Font);
                            Add(run.Brush);
                        }
                    }
                    break;
                case IDrawBrush brush:
                    AddBrushResources(brush);
                    break;
            }
        }

        void AddBrushResources(IDrawBrush brush)
        {
            switch (brush.CreateDescriptor())
            {
                case ImageDrawBrushDescriptor image:
                    Add(image.Image);
                    break;
                case DrawingDrawBrushDescriptor drawing:
                    foreach (DrawCommand nested in drawing.Commands)
                    {
                        AddCommandResources(nested);
                    }
                    break;
                case VisualDrawBrushDescriptor visual:
                    foreach (DrawCommand nested in visual.Commands)
                    {
                        AddCommandResources(nested);
                    }
                    break;
            }
        }
    }

    internal void TrackImageDependencies(Action<IDrawImage> track)
    {
        ArgumentNullException.ThrowIfNull(track);
        foreach (object resource in Resources)
        {
            if (resource is IDrawImage image)
            {
                track(image);
            }
        }
    }

    internal static bool IsContextSensitiveKind(DrawCommandKind kind) =>
        kind switch
        {
            DrawCommandKind.FillRectangle or
            DrawCommandKind.DrawRectangle or
            DrawCommandKind.FillRoundedRectangle or
            DrawCommandKind.DrawRoundedRectangle or
            DrawCommandKind.FillEllipse or
            DrawCommandKind.DrawEllipse or
            DrawCommandKind.DrawLine or
            DrawCommandKind.FillPath or
            DrawCommandKind.DrawPath or
            DrawCommandKind.DrawText or
            DrawCommandKind.DrawTextLayout or
            DrawCommandKind.DrawImage or
            DrawCommandKind.DrawImageQuad or
            DrawCommandKind.DrawNineSlice or
            DrawCommandKind.DrawMesh or
            DrawCommandKind.DrawPointBatch or
            DrawCommandKind.DrawLineBatch or
            DrawCommandKind.DrawSpriteBatch or
            DrawCommandKind.RenderSurface2D => false,
            DrawCommandKind.PushClip or
            DrawCommandKind.PopClip or
            DrawCommandKind.BeginPrism or
            DrawCommandKind.EndPrism or
            DrawCommandKind.PushTransform or
            DrawCommandKind.PopTransform or
            DrawCommandKind.PushPathClip or
            DrawCommandKind.PushOpacity or
            DrawCommandKind.PopOpacity or
            DrawCommandKind.PushBlend or
            DrawCommandKind.PopBlend or
            DrawCommandKind.PushLayer or
            DrawCommandKind.PopLayer => true,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown draw command kind.")
        };

    private static DrawRect? ResolveBounds(DrawCommand command) =>
        command.Kind switch
        {
            DrawCommandKind.FillRectangle or
            DrawCommandKind.DrawRectangle or
            DrawCommandKind.FillRoundedRectangle or
            DrawCommandKind.DrawRoundedRectangle or
            DrawCommandKind.DrawEllipse or
            DrawCommandKind.FillPath or
            DrawCommandKind.DrawPath or
            DrawCommandKind.RenderSurface2D or
            DrawCommandKind.DrawImageQuad or
            DrawCommandKind.DrawNineSlice or
            DrawCommandKind.DrawMesh or
            DrawCommandKind.DrawPointBatch or
            DrawCommandKind.DrawLineBatch or
            DrawCommandKind.DrawSpriteBatch or
            DrawCommandKind.DrawTextLayout => ExpandStroke(command.Rect, command.Pen),
            DrawCommandKind.FillEllipse => ExpandCoverage(command.Rect),
            DrawCommandKind.DrawImage => ImageBounds(command),
            DrawCommandKind.DrawLine => LineBounds(command),
            DrawCommandKind.DrawText => TextBounds(command),
            DrawCommandKind.PushClip or
            DrawCommandKind.PopClip or
            DrawCommandKind.BeginPrism or
            DrawCommandKind.EndPrism or
            DrawCommandKind.PushTransform or
            DrawCommandKind.PopTransform or
            DrawCommandKind.PushPathClip or
            DrawCommandKind.PushOpacity or
            DrawCommandKind.PopOpacity or
            DrawCommandKind.PushBlend or
            DrawCommandKind.PopBlend or
            DrawCommandKind.PushLayer or
            DrawCommandKind.PopLayer => null,
            _ => throw new InvalidOperationException($"Unsupported draw command: {command.Kind}")
        };

    private static DrawRect ExpandCoverage(DrawRect bounds)
    {
        const float margin = 0.125f;
        return new DrawRect(
            bounds.X - margin,
            bounds.Y - margin,
            bounds.Width + (margin * 2),
            bounds.Height + (margin * 2));
    }

    private static DrawRect ImageBounds(DrawCommand command)
    {
        if (command.Image is null || command.ImageOptions is null)
        {
            return command.Rect;
        }

        DrawPoint[] corners = DrawImageGeometry.GetDestinationCorners(
            command.Image,
            command.Rect,
            command.ImageOptions);
        float left = corners.Min(point => point.X);
        float top = corners.Min(point => point.Y);
        float right = corners.Max(point => point.X);
        float bottom = corners.Max(point => point.Y);
        return new DrawRect(left, top, right - left, bottom - top);
    }

    private static DrawRect ExpandStroke(DrawRect bounds, DrawPen? pen)
    {
        if (pen is null)
        {
            return bounds;
        }

        float extent = pen.Thickness * MathF.Max(1, pen.Style.MiterLimit);
        return new DrawRect(
            bounds.X - extent,
            bounds.Y - extent,
            bounds.Width + (extent * 2),
            bounds.Height + (extent * 2));
    }

    private static DrawRect LineBounds(DrawCommand command)
    {
        float extent = command.Pen is DrawPen pen
            ? pen.Thickness * MathF.Max(1, pen.Style.MiterLimit)
            : command.Thickness / 2;
        float left = MathF.Min(command.Position.X, command.EndPoint.X) - extent;
        float top = MathF.Min(command.Position.Y, command.EndPoint.Y) - extent;
        float right = MathF.Max(command.Position.X, command.EndPoint.X) + extent;
        float bottom = MathF.Max(command.Position.Y, command.EndPoint.Y) + extent;
        return new DrawRect(left, top, right - left, bottom - top);
    }

    private static DrawRect TextBounds(DrawCommand command)
    {
        DrawTextRun run = command.TextRun ??
            throw new InvalidOperationException("DrawText has no text run payload.");
        int elements = System.Globalization.StringInfo.ParseCombiningCharacters(run.Text).Length;
        float width = elements * run.Size;
        return new DrawRect(command.Position.X, command.Position.Y, width, run.Size * 1.5f);
    }
}
