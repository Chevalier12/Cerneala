using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Resources;

namespace Cerneala.Backends.SdlGpu;

internal sealed class SdlGpuDeviceOwner : IDisposable
{
    internal const SdlGpuShaderFormats RequestedShaderFormats =
        SdlGpuShaderFormats.SpirV |
        SdlGpuShaderFormats.Dxil |
        SdlGpuShaderFormats.Msl |
        SdlGpuShaderFormats.MetalLib;

    private readonly object sync = new();
    private readonly ISdlApi api;
    private nint device;
    private int activeSessions;
    private bool disposeRequested;

    public SdlGpuDeviceOwner(ISdlApi api)
    {
        this.api = api ?? throw new ArgumentNullException(nameof(api));
        device = api.CreateGpuDevice(RequestedShaderFormats, DefaultDebugMode, preferredDriver: null);
        if (device == 0)
        {
            throw SdlApiError.Create(api, "SDL GPU device creation");
        }

        try
        {
            ShaderFormats = api.GetGpuShaderFormats(device) & RequestedShaderFormats;
            if (ShaderFormats == SdlGpuShaderFormats.None)
            {
                throw new InvalidOperationException(
                    "SDL GPU device reported no supported offline shader format.");
            }

            DebugLabels = new SdlGpuDebugLabels(api, DefaultDebugMode);
            DrawingResources = new SdlGpuDrawingResources(
                api,
                device,
                ShaderFormats);
            ImageLoader = new SdlGpuImageLoader();
            ImageResourceCache = new ImageResourceCache(ImageLoader);
        }
        catch
        {
            api.DestroyGpuDevice(device);
            device = 0;
            throw;
        }
    }

#if DEBUG
    internal const bool DefaultDebugMode = true;
#else
    internal const bool DefaultDebugMode = false;
#endif

    public SdlGpuShaderFormats ShaderFormats { get; }

    public SdlGpuDebugLabels DebugLabels { get; }

    public SdlGpuDrawingResources DrawingResources { get; }

    public SdlGpuImageLoader ImageLoader { get; }

    public ImageResourceCache ImageResourceCache { get; }

    public SdlGpuDeviceLease AcquireSession()
    {
        lock (sync)
        {
            ObjectDisposedException.ThrowIf(disposeRequested, this);
            activeSessions++;
            return new SdlGpuDeviceLease(
                this,
                device,
                ShaderFormats,
                DrawingResources,
                ImageLoader,
                ImageResourceCache);
        }
    }

    public void Dispose()
    {
        lock (sync)
        {
            if (disposeRequested)
            {
                return;
            }

            disposeRequested = true;
            DestroyDeviceIfUnused();
        }
    }

    internal void ReleaseSession()
    {
        lock (sync)
        {
            if (activeSessions <= 0)
            {
                throw new InvalidOperationException("SDL GPU session ownership is unbalanced.");
            }

            activeSessions--;
            DestroyDeviceIfUnused();
        }
    }

    private void DestroyDeviceIfUnused()
    {
        if (!disposeRequested || activeSessions != 0 || device == 0)
        {
            return;
        }

        nint deviceToDestroy = device;
        device = 0;
        ImageResourceCache.Dispose();
        DrawingResources.Dispose();
        api.DestroyGpuDevice(deviceToDestroy);
    }
}

internal sealed class SdlGpuDeviceLease : IDisposable
{
    private SdlGpuDeviceOwner? owner;

    internal SdlGpuDeviceLease(
        SdlGpuDeviceOwner owner,
        nint device,
        SdlGpuShaderFormats shaderFormats,
        SdlGpuDrawingResources drawingResources,
        SdlGpuImageLoader imageLoader,
        ImageResourceCache imageResourceCache)
    {
        this.owner = owner;
        Device = device;
        ShaderFormats = shaderFormats;
        DrawingResources = drawingResources;
        ImageLoader = imageLoader;
        ImageResourceCache = imageResourceCache;
    }

    public nint Device { get; }

    public SdlGpuShaderFormats ShaderFormats { get; }

    public SdlGpuDrawingResources DrawingResources { get; }

    public SdlGpuImageLoader ImageLoader { get; }

    public ImageResourceCache ImageResourceCache { get; }

    public void Dispose() =>
        Interlocked.Exchange(ref owner, null)?.ReleaseSession();
}
