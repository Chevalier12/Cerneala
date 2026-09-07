using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Hosting.Windowing;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuPrismBuiltinTextureAllocationTests
{
    [Fact]
    public void RepeatedBuiltinTextureLookupDoesNotRebuildPixelPayloads()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow(
            "prism-builtin-texture-allocation",
            48,
            32,
            SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(
            api,
            useMultisampling: false);
        using SdlGpuWindowGraphicsSession session =
            Assert.IsType<SdlGpuWindowGraphicsSession>(
                factory.Create(
                    new SdlWindowSurface(window, api.GetWindowId(window)),
                    48,
                    32,
                    coordinateScale: 1));
        SdlGpuPrismDeviceResources resources =
            session.DrawingResources.PrismResources;

        session.BeginFrame(Color.Transparent);
        _ = resources.GetWhiteTexture(session);
        _ = resources.GetGradientDitherTexture(session);
        nint points = resources.GetSpatterPointTexture(session);
        Assert.Equal(points, resources.GetSpatterPointTexture(session));
        SdlGpuTextureCreateInfo pointInfo = api.GpuTextures[points].CreateInfo;
        Assert.Equal((uint)(PrismRecursiveWangBlueNoise.GridSize *
            PrismRecursiveWangBlueNoise.LayerCount), pointInfo.Width);
        Assert.Equal((uint)PrismRecursiveWangBlueNoise.GridSize, pointInfo.Height);
        Assert.Equal(SdlGpuTextureFormat.R16G16B16A16Float, pointInfo.Format);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int pass = 0; pass < 256; pass++)
        {
            _ = resources.GetWhiteTexture(session);
            _ = resources.GetGradientDitherTexture(session);
            _ = resources.GetSpatterPointTexture(session);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }
}
