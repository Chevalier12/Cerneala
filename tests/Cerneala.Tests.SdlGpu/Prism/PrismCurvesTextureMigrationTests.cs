using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Tests.SdlGpu;

public sealed class PrismCurvesTextureMigrationTests
{
    [Fact]
    public void CurvesLutUsesHalfPrecision1024SamplesAndVersionedDeferredRetirement()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("curves-cache", 16, 16, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(new SdlWindowSurface(window, api.GetWindowId(window)), 16, 16, 1));
        SdlGpuPrismDeviceResources resources = session.DrawingResources.PrismResources;
        PrismResourceId id = new("curves-cache");
        PrismCurvesResource curve = new(red: [new(0, 0), new(1, .5f)]);
        session.BeginFrame(Color.Transparent);
        nint first = resources.GetCurvesTexture(session, id, curve, 1, 1);
        Assert.Equal(first, resources.GetCurvesTexture(session, id, curve, 1, 1));
        SdlGpuTextureCreateInfo info = api.GpuTextures[first].CreateInfo;
        Assert.Equal((uint)PrismCurveLut.SampleCount, info.Width);
        Assert.Equal(1u, info.Height);
        Assert.Equal(SdlGpuTextureFormat.R16G16B16A16Float, info.Format);
        Assert.Equal(SdlGpuTextureUsage.Sampler, info.Usage);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < 256; index++)
        {
            _ = resources.GetCurvesTexture(session, id, curve, 1, 1);
        }
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);

        nint changedVersion = resources.GetCurvesTexture(session, id, curve, 1, 2);
        nint changedIdentity = resources.GetCurvesTexture(session, id, curve, 2, 2);
        nint changedValue = resources.GetCurvesTexture(session, id, new PrismCurvesResource(), 2, 2);
        Assert.NotEqual(first, changedVersion);
        Assert.NotEqual(changedVersion, changedIdentity);
        Assert.NotEqual(changedIdentity, changedValue);
        Assert.True(api.GpuTextures.ContainsKey(first));
        session.CompleteFrame(present: false);
        Assert.False(api.GpuTextures.ContainsKey(first));
        Assert.False(api.GpuTextures.ContainsKey(changedVersion));
        Assert.False(api.GpuTextures.ContainsKey(changedIdentity));
        Assert.True(api.GpuTextures.ContainsKey(changedValue));
        session.Dispose();
        factory.Dispose();
        Assert.False(api.GpuTextures.ContainsKey(changedValue));
        api.DestroyWindow(window);
    }
}
