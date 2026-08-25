namespace Cerneala.Drawing;

internal interface IRenderSurface2DSource
{
}

internal interface IRenderSurface2DFrameSource : IRenderSurface2DSource
{
    Color ClearColor { get; }

    long FrameVersion { get; }

    void RecordFrame(
        DrawCommandList commands,
        DrawRect bounds);

    IRenderSurface2DBackendState? GetBackendState(object owner);

    void SetBackendState(
        object owner,
        IRenderSurface2DBackendState? state);
}

internal interface IRenderSurface2DBackendState : IDisposable
{
}
