namespace Cerneala.Drawing;

internal interface IDrawingBackendFrameTimingSource
{
    DrawingBackendFrameTiming LastFrameTiming { get; }
}

internal readonly record struct DrawingBackendFrameTiming(
    TimeSpan Preparation,
    TimeSpan TextRequestCollection,
    TimeSpan TextRasterization,
    TimeSpan TextAtlasUpload,
    TimeSpan CommandRendering,
    TimeSpan Cleanup,
    int TextRequestCount,
    long RasterizedPixelCount);
