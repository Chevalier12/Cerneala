using System.Numerics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Styles;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Prism.Definitions;

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

    [Fact]
    public void RepeatedGradientOverlayTextureLookupDoesNotRebuildPixelPayloads()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow(
            "prism-gradient-texture-allocation",
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
        PrismResourceId id = new("UnchangedGradient");
        PrismGradientMapResource gradient = new(
            [new(0, Vector3.Zero), new(1, Vector3.One)]);

        session.BeginFrame(Color.Transparent);
        nint texture = Lookup();
        for (int pass = 0; pass < 32; pass++)
        {
            Assert.Equal(texture, Lookup());
        }
        int textureCount = api.GpuTextures.Count;
        nint lastTexture = texture;

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int pass = 0; pass < 256; pass++)
        {
            lastTexture = Lookup();
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(texture, lastTexture);
        Assert.Equal(textureCount, api.GpuTextures.Count);
        Assert.Equal(0, allocated);

        nint Lookup() => resources.GetGradientOverlayTexture(
            session,
            id,
            gradient,
            identity: 1,
            version: 1,
            PrismGradientInterpolation.PerceptualOklab,
            PrismColorProfile.LinearSrgb);
    }
}
