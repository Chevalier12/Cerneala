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

    internal static DrawCommandMetadata Create(DrawCommand command, DrawCommandMetadata? previous = null)
    {
        IReadOnlyList<object> resources = VisitResources(command, trackImage: null, previous?.Resources);
        DrawRect? bounds = ResolveBounds(command);
        bool isContextSensitive = IsContextSensitiveKind(command.Kind);
        // Commands may reference mutable brushes and images. Rediscover their
        // dependencies and bounds before sharing an immutable metadata snapshot.
        if (previous is not null && previous.Bounds == bounds &&
            previous.IsContextSensitive == isContextSensitive &&
            previous.RetainedIdentity.Equals(command) &&
            SameResources(previous.Resources, resources))
        {
            return previous;
        }
        return new DrawCommandMetadata(
            bounds,
            resources,
            isContextSensitive,
            command);
    }

    private static bool SameResources(IReadOnlyList<object> previous, IReadOnlyList<object> current)
    {
        if (ReferenceEquals(previous, current)) { return true; }
        if (previous.Count != current.Count) { return false; }
        for (int index = 0; index < current.Count; index++)
        {
            if (!ReferenceEquals(previous[index], current[index])) { return false; }
        }
        return true;
    }

    private static IReadOnlyList<object> VisitResources(
        DrawCommand command,
        Action<IDrawImage>? trackImage,
        IReadOnlyList<object>? previousResources = null)
    {
        object? firstResource = null;
        object? secondResource = null;
        List<object>? resources = null;
        HashSet<object>? seen = null;

        AddCommandResources(command);

        if (trackImage is not null)
        {
            // Keep discovery eager: a tracking callback must not change which
            // dependencies are discovered later in this same command.
            if (resources is not null)
            {
                foreach (object resource in resources)
                {
                    if (resource is IDrawImage image) { trackImage(image); }
                }
            }
            else
            {
                if (firstResource is IDrawImage firstImage) { trackImage(firstImage); }
                if (secondResource is IDrawImage secondImage) { trackImage(secondImage); }
            }
            return Array.Empty<object>();
        }

        if (previousResources is not null)
        {
            bool same = resources is not null
                ? SameResources(previousResources, resources)
                : previousResources.Count == (secondResource is not null ? 2 : firstResource is not null ? 1 : 0) &&
                  (firstResource is null || ReferenceEquals(previousResources[0], firstResource)) &&
                  (secondResource is null || ReferenceEquals(previousResources[1], secondResource));
            if (same) { return previousResources; }
        }

        return resources is not null
                ? new ReadOnlyCollection<object>(resources)
                : secondResource is not null
                    ? Array.AsReadOnly(new[] { firstResource!, secondResource })
                    : firstResource is not null
                        ? Array.AsReadOnly(new[] { firstResource })
                        : Array.Empty<object>();

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
            if (resource is null || ReferenceEquals(resource, firstResource) || ReferenceEquals(resource, secondResource))
            {
                return;
            }

            if (firstResource is null)
            {
                firstResource = resource;
            }
            else if (secondResource is null)
            {
                secondResource = resource;
            }
            else
            {
                // Most commands have at most two resources. Keep the same eager,
                // reference-deduplicated traversal without allocating collections
                // until a third distinct dependency is actually encountered.
                if (seen is null)
                {
                    seen = new HashSet<object>(ReferenceEqualityComparer.Instance) { firstResource, secondResource };
                    resources = new List<object>(4) { firstResource, secondResource };
                }
                if (!seen.Add(resource))
                {
                    return;
                }
                resources!.Add(resource);
            }
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

    internal static void TrackImageDependencies(DrawCommand command, Action<IDrawImage> track)
    {
        ArgumentNullException.ThrowIfNull(track);
        // Dependency tracking shares resource discovery with state analysis,
        // without materializing an unused resource snapshot or retained identity.
        VisitResources(command, track);
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
        // Only the count is needed, not an allocated array of every boundary.
        // Use the same runtime grapheme segmentation, including malformed UTF-16.
        ReadOnlySpan<char> remaining = run.Text.AsSpan();
        int elements = 0;
        while (!remaining.IsEmpty)
        {
            remaining = remaining[System.Globalization.StringInfo.GetNextTextElementLength(remaining)..];
            elements++;
        }
        float width = elements * run.Size;
        return new DrawRect(command.Position.X, command.Position.Y, width, run.Size * 1.5f);
    }
}
