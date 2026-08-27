using Cerneala.UI.Platform;

namespace Cerneala.Platforms.Sdl3;

internal sealed class SdlCursorService : ICursorService, IDisposable
{
    private readonly ISdlApi api;
    private readonly Dictionary<SdlSystemCursor, nint> handles = [];
    private bool disposed;

    public SdlCursorService(ISdlApi api)
    {
        this.api = api;
    }

    public CursorShape Current { get; private set; } = CursorShape.Arrow;

    public void SetCursor(CursorShape shape)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        nint handle = shape == CursorShape.Hidden ? 0 : GetHandle(Map(shape));
        if (!api.SetCursor(handle))
        {
            throw SdlApiError.Create(api, "SDL cursor selection");
        }

        Current = shape;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        foreach (nint handle in handles.Values)
        {
            api.DestroyCursor(handle);
        }

        handles.Clear();
    }

    private nint GetHandle(SdlSystemCursor cursor)
    {
        if (handles.TryGetValue(cursor, out nint handle))
        {
            return handle;
        }

        handle = api.CreateSystemCursor(cursor);
        if (handle == 0)
        {
            throw SdlApiError.Create(api, "SDL system cursor creation");
        }

        handles.Add(cursor, handle);
        return handle;
    }

    private static SdlSystemCursor Map(CursorShape shape) => shape switch
    {
        CursorShape.Hand => SdlSystemCursor.Pointer,
        CursorShape.IBeam => SdlSystemCursor.Text,
        CursorShape.Crosshair => SdlSystemCursor.Crosshair,
        CursorShape.ResizeHorizontal => SdlSystemCursor.ResizeHorizontal,
        CursorShape.ResizeVertical => SdlSystemCursor.ResizeVertical,
        _ => SdlSystemCursor.Default
    };
}
