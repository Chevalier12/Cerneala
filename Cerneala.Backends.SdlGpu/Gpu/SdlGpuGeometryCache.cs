using Cerneala.Drawing;
using Cerneala.Drawing.Paths;

namespace Cerneala.Backends.SdlGpu;

// Retains the backend's derived geometry, not paint, transforms or GPU resources.
// Source paths and stroke styles are immutable. A frame's unused entries are
// released at its end, and admission limits also bound continuously changing scenes.
internal sealed class SdlGpuGeometryCache
{
    private readonly int maximumEntries;
    private readonly long maximumArrayBytes;
    private readonly Dictionary<GeometryKey, Entry> entries = [];
    private readonly List<GeometryKey> unused = [];
    private long frame;

    internal SdlGpuGeometryCache(int maximumEntries = 1_024, long maximumArrayBytes = 16 * 1024 * 1024)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximumEntries);
        ArgumentOutOfRangeException.ThrowIfNegative(maximumArrayBytes);
        this.maximumEntries = maximumEntries;
        this.maximumArrayBytes = maximumArrayBytes;
    }

    internal int Count => entries.Count;

    // Array payload only; entry count independently bounds dictionary/object overhead.
    internal long RetainedArrayBytes { get; private set; }

    internal void BeginFrame() => frame++;

    internal SdlGpuGeometry GetFill(DrawCommand command, float coordinateScale) =>
        Get(command, coordinateScale, stroke: false);

    internal SdlGpuGeometry GetStroke(DrawCommand command, float coordinateScale) =>
        Get(command, coordinateScale, stroke: true);

    internal void EndFrame()
    {
        foreach ((GeometryKey key, Entry entry) in entries)
        {
            if (entry.LastFrame != frame) unused.Add(key);
        }
        foreach (GeometryKey key in unused)
        {
            RetainedArrayBytes -= entries[key].Geometry.ArrayBytes;
            entries.Remove(key);
        }
        unused.Clear();
    }

    internal void Clear()
    {
        entries.Clear();
        unused.Clear();
        RetainedArrayBytes = 0;
    }

    private SdlGpuGeometry Get(DrawCommand command, float coordinateScale, bool stroke)
    {
        GeometryKey key = new(
            command.Kind,
            command.Rect,
            command.SourceRect,
            command.Position,
            command.EndPoint,
            command.Path?.StableId ?? 0,
            command.FillRule,
            stroke ? command.Pen?.Thickness ?? command.Thickness : 0,
            stroke ? command.Pen?.Style ?? DrawStrokeStyle.Default : null,
            coordinateScale);
        if (entries.TryGetValue(key, out Entry? cached))
        {
            cached.LastFrame = frame;
            return cached.Geometry;
        }

        SdlGpuGeometry geometry = stroke
            ? BuildStroke(command, coordinateScale)
            : BuildFill(command, coordinateScale);
        if (entries.Count < maximumEntries && geometry.ArrayBytes <= maximumArrayBytes - RetainedArrayBytes)
        {
            entries.Add(key, new Entry(geometry, frame));
            RetainedArrayBytes += geometry.ArrayBytes;
        }
        return geometry;
    }

    private static SdlGpuGeometry BuildFill(DrawCommand command, float coordinateScale)
    {
        DrawPath path = command.Path ??
            throw new InvalidOperationException($"{command.Kind} requires path geometry.");
        DrawRect destination = command.Kind == DrawCommandKind.FillEllipse
            ? DrawEllipseCoverage.AdjustBounds(command.Rect, coordinateScale)
            : command.Rect;
        DrawTriangleMesh mesh = DrawPathMeshBuilder.Build(
            path,
            command.SourceRect,
            destination.Width * coordinateScale,
            destination.Height * coordinateScale,
            destination.X * coordinateScale,
            destination.Y * coordinateScale,
            command.FillRule);
        DrawPoint[] logical = new DrawPoint[mesh.Vertices.Length];
        for (int i = 0; i < logical.Length; i++)
        {
            logical[i] = new DrawPoint(mesh.Vertices[i].X / coordinateScale, mesh.Vertices[i].Y / coordinateScale);
        }
        return new SdlGpuGeometry(logical, logical, mesh.Indices, command.Rect);
    }

    private static SdlGpuGeometry BuildStroke(DrawCommand command, float coordinateScale)
    {
        DrawStrokeRenderMesh stroke = DrawStrokeMeshBuilder.Build(
            command,
            command.Pen?.Thickness ?? command.Thickness,
            command.Pen?.Style ?? DrawStrokeStyle.Default,
            coordinateScale);
        DrawPoint[] logical = new DrawPoint[stroke.Mesh.Vertices.Length];
        for (int i = 0; i < logical.Length; i++)
        {
            // Keep the existing physical-origin round trip exactly; changing it
            // can change coverage at fractional coordinates.
            logical[i] = new DrawPoint(
                (stroke.Mesh.Vertices[i].X + stroke.Left) / coordinateScale,
                (stroke.Mesh.Vertices[i].Y + stroke.Top) / coordinateScale);
        }
        DrawRect bounds = command.Kind == DrawCommandKind.DrawLine
            ? BoundsOf(stroke.BrushPoints)
            : command.Rect;
        return new SdlGpuGeometry(logical, stroke.BrushPoints, stroke.Mesh.Indices, bounds);
    }

    private static DrawRect BoundsOf(IReadOnlyList<DrawPoint> points)
    {
        if (points.Count == 0) return default;
        float left = points[0].X;
        float top = points[0].Y;
        float right = left;
        float bottom = top;
        for (int i = 1; i < points.Count; i++)
        {
            left = MathF.Min(left, points[i].X);
            top = MathF.Min(top, points[i].Y);
            right = MathF.Max(right, points[i].X);
            bottom = MathF.Max(bottom, points[i].Y);
        }
        return new DrawRect(left, top, right - left, bottom - top);
    }

    private readonly record struct GeometryKey(
        DrawCommandKind Kind,
        DrawRect Rect,
        DrawRect SourceRect,
        DrawPoint Position,
        DrawPoint EndPoint,
        long PathId,
        DrawFillRule FillRule,
        float Thickness,
        DrawStrokeStyle? Style,
        float CoordinateScale);

    private sealed class Entry(SdlGpuGeometry geometry, long lastFrame)
    {
        internal SdlGpuGeometry Geometry { get; } = geometry;
        internal long LastFrame { get; set; } = lastFrame;
    }
}

internal sealed class SdlGpuGeometry(DrawPoint[] positions, DrawPoint[] brushPoints, int[] indices, DrawRect brushBounds)
{
    internal DrawPoint[] Positions { get; } = positions;
    internal DrawPoint[] BrushPoints { get; } = brushPoints;
    internal int[] Indices { get; } = indices;
    internal DrawRect BrushBounds { get; } = brushBounds;
    internal bool IsEmpty => Positions.Length == 0 || Indices.Length == 0;
    internal long ArrayBytes { get; } =
        ((long)positions.Length * 2 * sizeof(float)) + ((long)indices.Length * sizeof(int)) +
        (ReferenceEquals(positions, brushPoints) ? 0 : (long)brushPoints.Length * 2 * sizeof(float));
}
