using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
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

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int pass = 0; pass < 256; pass++)
        {
            _ = resources.GetWhiteTexture(session);
            _ = resources.GetGradientDitherTexture(session);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }
}
