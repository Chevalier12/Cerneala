using System.Numerics;

namespace Cerneala.Drawing;

internal interface IRenderSurface3DSource
{
    long FrameVersion { get; }
    bool HasDrawSubscribers { get; }
    long ResourceEpoch { get; }
    RenderSurface3DRecording RecordFrame(DrawRect bounds, float rasterScale);
    void SetBackendState(object owner, IRenderSurface3DBackendState? state);
}

internal interface IRenderSurface3DBackendState : IDisposable;

internal enum DrawPrimitive3DKind { Line, Marker }

internal readonly record struct DrawPrimitive3D(
    DrawPrimitive3DKind Kind, Vector3 Start, Vector3 End, Color Color, float Size, Matrix4x4 Model);

internal sealed record RenderSurface3DRecording(
    long Generation, Color ClearColor, DrawRect Bounds, int PixelWidth, int PixelHeight,
    float RasterScale, TimeSpan FrameTime, Matrix4x4 ViewMatrix, Matrix4x4 ProjectionMatrix,
    IReadOnlyList<DrawPrimitive3D> Primitives);
