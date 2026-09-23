using System.Numerics;
using Cerneala.Drawing;

namespace Cerneala.UI.Controls;

public sealed class RenderSurface3DFrame
{
    private readonly List<DrawPrimitive3D> primitives = [];
    private bool active = true;

    internal RenderSurface3DFrame(
        DrawRect bounds, int pixelWidth, int pixelHeight, float rasterScale,
        TimeSpan frameTime, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
    {
        Bounds = bounds;
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
        RasterScale = rasterScale;
        FrameTime = frameTime;
        ViewMatrix = viewMatrix;
        ProjectionMatrix = projectionMatrix;
    }

    public DrawRect Bounds { get; }
    public int PixelWidth { get; }
    public int PixelHeight { get; }
    public float RasterScale { get; }
    public TimeSpan FrameTime { get; }
    public Matrix4x4 ViewMatrix { get; }
    public Matrix4x4 ProjectionMatrix { get; }

    public void DrawLine(Vector3 start, Vector3 end, Color color, float thickness = 1) =>
        DrawLine(start, end, color, Matrix4x4.Identity, thickness);

    public void DrawLine(Vector3 start, Vector3 end, Color color, Matrix4x4 model, float thickness = 1)
    {
        EnsureActive();
        DrawLineSegment3D line = new(start, end, color, thickness);
        ValidateModel(model);
        primitives.Add(new(DrawPrimitive3DKind.Line, line.Start, line.End, line.Color, line.Thickness, model));
    }

    public void DrawMarker(Vector3 center, Color color, float diameter = 1) =>
        DrawMarker(center, color, Matrix4x4.Identity, diameter);

    public void DrawMarker(Vector3 center, Color color, Matrix4x4 model, float diameter = 1)
    {
        EnsureActive();
        DrawMarker3D marker = new(center, color, diameter);
        ValidateModel(model);
        primitives.Add(new(DrawPrimitive3DKind.Marker, marker.Center, default, marker.Color, marker.Diameter, model));
    }

    public void DrawLineBatch(ReadOnlySpan<DrawLineSegment3D> lines) => DrawLineBatch(lines, Matrix4x4.Identity);

    public void DrawLineBatch(ReadOnlySpan<DrawLineSegment3D> lines, Matrix4x4 model)
    {
        EnsureActive();
        if (lines.IsEmpty) { return; }
        ValidateModel(model);
        ValidateBatchSize<DrawLineSegment3D>(lines.Length);
        foreach (DrawLineSegment3D line in lines)
        {
            DrawLineSegment3D validated = new(line.Start, line.End, line.Color, line.Thickness);
            primitives.Add(new(DrawPrimitive3DKind.Line, validated.Start, validated.End, validated.Color, validated.Thickness, model));
        }
    }

    public void DrawMarkerBatch(ReadOnlySpan<DrawMarker3D> markers) => DrawMarkerBatch(markers, Matrix4x4.Identity);

    public void DrawMarkerBatch(ReadOnlySpan<DrawMarker3D> markers, Matrix4x4 model)
    {
        EnsureActive();
        if (markers.IsEmpty) { return; }
        ValidateModel(model);
        ValidateBatchSize<DrawMarker3D>(markers.Length);
        foreach (DrawMarker3D marker in markers)
        {
            DrawMarker3D validated = new(marker.Center, marker.Color, marker.Diameter);
            primitives.Add(new(DrawPrimitive3DKind.Marker, validated.Center, default, validated.Color, validated.Diameter, model));
        }
    }

    internal IReadOnlyList<DrawPrimitive3D> Complete()
    {
        EnsureActive();
        active = false;
        return primitives.ToArray();
    }

    internal void Abort()
    {
        active = false;
        primitives.Clear();
    }

    internal static int ValidateBatchSize<T>(int count) where T : struct =>
        checked(count * System.Runtime.InteropServices.Marshal.SizeOf<T>());

    private void EnsureActive() => ObjectDisposedException.ThrowIf(!active, this);

    private static void ValidateModel(Matrix4x4 model)
    {
        if (!RenderSurface3DValidation.IsFiniteAffine(model)) throw new ArgumentOutOfRangeException(nameof(model));
    }
}
