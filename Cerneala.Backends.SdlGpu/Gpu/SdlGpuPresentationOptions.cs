using Cerneala.Platforms.Sdl3;

namespace Cerneala.Backends.SdlGpu;

internal readonly record struct SdlGpuPresentationOptions(
    SdlGpuSwapchainComposition Composition,
    SdlGpuPresentMode PresentMode,
    SdlGpuSampleCount SampleCount)
{
    public static SdlGpuPresentationOptions CreateDefault(bool useMultisampling) =>
        new(
            SdlGpuSwapchainComposition.Sdr,
            SdlGpuPresentMode.VSync,
            useMultisampling ? SdlGpuSampleCount.Eight : SdlGpuSampleCount.One);
}

internal sealed record SdlGpuPresentationDiagnostics(
    SdlGpuSwapchainComposition Composition,
    SdlGpuPresentMode PresentMode,
    SdlGpuTextureFormat TextureFormat,
    SdlGpuSampleCount SampleCount,
    IReadOnlyList<string> Fallbacks);
