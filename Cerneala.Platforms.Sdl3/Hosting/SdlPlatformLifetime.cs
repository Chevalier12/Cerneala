namespace Cerneala.Platforms.Sdl3;

internal sealed class SdlPlatformLifetime : IDisposable
{
    private readonly object sync = new();
    private readonly ISdlApi api;
    private readonly int uiThreadId;
    private bool disposed;

    public SdlPlatformLifetime(ISdlApi api)
    {
        this.api = api ?? throw new ArgumentNullException(nameof(api));
        uiThreadId = Environment.CurrentManagedThreadId;

        if (api.InitializeVideo())
        {
            return;
        }

        InvalidOperationException exception = SdlApiError.Create(api, "SDL video initialization");
        api.Quit();
        throw exception;
    }

    public int UiThreadId => uiThreadId;

    public bool IsDisposed
    {
        get
        {
            lock (sync)
            {
                return disposed;
            }
        }
    }

    public void VerifyUiThread()
    {
        if (Environment.CurrentManagedThreadId != uiThreadId)
        {
            throw new InvalidOperationException(
                $"SDL platform access must remain on UI thread {uiThreadId}.");
        }
    }

    public void Dispose()
    {
        lock (sync)
        {
            if (disposed)
            {
                return;
            }

            VerifyUiThread();
            disposed = true;
            api.Quit();
        }
    }
}
