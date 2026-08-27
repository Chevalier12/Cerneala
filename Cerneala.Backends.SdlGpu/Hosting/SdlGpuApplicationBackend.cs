using Cerneala.Backends.SdlGpu;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Hosting.Windowing;

namespace Cerneala.UI.Hosting.Sdl;

/// <summary>
/// Registers the SDL3 window platform and SDL_GPU graphics backend used by Cerneala applications.
/// </summary>
public static class SdlGpuApplicationBackend
{
    /// <summary>
    /// Ensures that the composed SDL3 and SDL_GPU backend is available to the window host.
    /// </summary>
    public static void EnsureRegistered() =>
        WindowingBackendRegistry.Register(SdlGpuWindowingBackend.Instance);

    private sealed class SdlGpuWindowingBackend : IWindowingBackend
    {
        public static SdlGpuWindowingBackend Instance { get; } = new();

        public IWindowPlatform CreatePlatform(
            bool useMultisampling,
            float? coordinateScaleOverride)
        {
            NativeSdlApi api = new();
            return new SdlWindowPlatform(
                api,
                new SdlGpuWindowGraphicsSessionFactory(api, useMultisampling),
                coordinateScaleOverride);
        }
    }
}
