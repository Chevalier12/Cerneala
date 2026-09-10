using System.Numerics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Platforms.Sdl3;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuSampledTextureReuseTests
{
    [Fact]
    public void ReuseRequiresMatchingDimensionsAndFormatAndPublishesNewOrigin()
    {
        using ResourceFixture fixture = new();
        SdlGpuTextureResource half = fixture.Create("half", 8, 4, half: true);
        fixture.Resources.ReleaseTexture("half");
        SdlGpuTextureResource rgba = fixture.Create("rgba", 8, 4, origin: new(2, 3));
        Assert.NotEqual(half.Handle, rgba.Handle);
        fixture.Resources.ReleaseTexture("rgba");
        SdlGpuTextureResource resized = fixture.Create("resized", 9, 4);
        Assert.NotEqual(rgba.Handle, resized.Handle);
        fixture.Resources.ReleaseTexture("resized");

        SdlGpuTextureResource reusedHalf = fixture.Create("new-half", 8, 4, half: true);
        SdlGpuTextureResource reusedRgba = fixture.Create("new-rgba", 8, 4, origin: new(5, 6));
        Assert.NotEqual(half.Handle, reusedHalf.Handle);
        Assert.Equal(rgba.Handle, reusedRgba.Handle);
        Assert.Equal(new DrawPoint(5, 6), reusedRgba.OriginOffset);
        Assert.Equal(SdlGpuTextureFormat.R16G16B16A16Float,
            fixture.Api.GpuTextures[reusedHalf.Handle].CreateInfo.Format);
        Assert.Equal(SdlGpuTextureFormat.R8G8B8A8Unorm,
            fixture.Api.GpuTextures[reusedRgba.Handle].CreateInfo.Format);
        Assert.All(fixture.Api.GpuTextureUploads, upload => Assert.True(upload.Cycle));
    }

    [Fact]
    public void StorageCannotBeReusedWhileAnotherLeaseRemainsOrAfterGeneralInvalidation()
    {
        using ResourceFixture fixture = new();
        SdlGpuTextureResource shared = fixture.Create("shared", 8, 4);
        fixture.Resources.RetainTexture("shared");
        fixture.Resources.ReleaseTexture("shared");
        SdlGpuTextureResource other = fixture.Create("other", 8, 4);
        Assert.NotEqual(shared.Handle, other.Handle);
        Assert.Same(shared, fixture.Resources.FindTexture("shared"));
        fixture.Resources.ReleaseTexture("shared");
        Assert.Null(fixture.Resources.FindTexture("shared"));
        Assert.Equal(shared.Handle, fixture.Create("reused", 8, 4).Handle);

        fixture.Resources.InvalidateTexture("other");
        SdlGpuTextureResource afterInvalidation = fixture.Create("after-invalidation", 8, 4);
        Assert.NotEqual(other.Handle, afterInvalidation.Handle);
        fixture.Resources.ReleaseTexture("other");
    }

    [Fact]
    public void IdleStorageHasBothCountAndByteBoundsAndDrainsWithTheLastLease()
    {
        using ResourceFixture fixture = new();
        for (int index = 0; index < 48; index++)
        {
            object key = new();
            fixture.Create(key, index + 2, 4);
            fixture.Resources.ReleaseTexture(key);
            fixture.Resources.FlushRetired();
            AssertBounds();
        }
        for (int index = 0; index < 8; index++)
        {
            object key = new();
            fixture.Create(key, index == 7 ? 1024 : 128 + index, 1024);
            fixture.Resources.ReleaseTexture(key);
            fixture.Resources.FlushRetired();
            AssertBounds();
        }
        fixture.Resources.ReleaseTexture(fixture.Anchor);
        fixture.Resources.FlushRetired();
        Assert.Equal(0, fixture.Resources.CachedTextureCount);
        Assert.Empty(SampledTextures());
        fixture.Dispose();
        Assert.Empty(fixture.Api.GpuTextures);
        Assert.Equal(fixture.Api.ReleasedGpuTextures.Count,
            fixture.Api.ReleasedGpuTextures.Distinct().Count());

        IEnumerable<FakeSdlApi.FakeGpuTexture> SampledTextures() => fixture.Api.GpuTextures.Values
            .Where(texture => texture.CreateInfo.Usage == SdlGpuTextureUsage.Sampler);

        void AssertBounds()
        {
            // One live 1x1 RGBA anchor is excluded from the idle budget.
            var textures = SampledTextures().ToArray();
            Assert.InRange(textures.Length, 1, 17);
            long bytes = textures.Sum(texture => (long)texture.CreateInfo.Width * texture.CreateInfo.Height *
                (texture.CreateInfo.Format == SdlGpuTextureFormat.R16G16B16A16Float ? 8 : 4));
            Assert.InRange(bytes - 4, 0, 1024 * 1024);
        }
    }

    [Fact]
    public void FailedUploadReleasesTakenStorageWithoutPublishingTheNewKey()
    {
        using ResourceFixture fixture = new();
        SdlGpuTextureResource first = fixture.Create("first", 8, 4);
        fixture.Resources.ReleaseTexture("first");
        // Uploading without an active session command buffer fails after storage
        // is taken. The failure path must release that handle, not recache it.
        Assert.Throws<InvalidOperationException>(() => fixture.Resources.GetOrCreateTexture(
            fixture.Session, "failed", 8, 4, new byte[8 * 4 * 4], recycleStorage: true));
        Assert.Null(fixture.Resources.FindTexture("failed"));
        Assert.DoesNotContain(first.Handle, fixture.Api.GpuTextures.Keys);
        SdlGpuTextureResource recovered = fixture.Create("recovered", 8, 4);
        Assert.NotEqual(first.Handle, recovered.Handle);
    }

    private sealed class ResourceFixture : IDisposable
    {
        private readonly SdlGpuWindowGraphicsSessionFactory factory;
        public FakeSdlApi Api { get; } = new();
        public SdlGpuWindowGraphicsSession Session { get; }
        public SdlGpuDrawingResources Resources => Session.DrawingResources;
        public object Anchor { get; } = new();

        public ResourceFixture()
        {
            factory = new(Api);
            nint window = Api.CreateWindow("sampled-reuse", 16, 16, SdlWindowOptions.Hidden);
            Session = Assert.IsType<SdlGpuWindowGraphicsSession>(factory.Create(
                new SdlWindowSurface(window, Api.GetWindowId(window)), 16, 16, 1));
            Create(Anchor, 1, 1);
        }

        public SdlGpuTextureResource Create(object key, int width, int height, bool half = false, DrawPoint origin = default)
        {
            Session.BeginFrame(Color.Transparent);
            try
            {
                SdlGpuTextureResource texture = half
                    ? Resources.GetOrCreateHalfVector4Texture(Session, key, width, height, new Vector4[width * height])
                    : Resources.GetOrCreateTexture(Session, key, width, height, new byte[width * height * 4], origin,
                        recycleStorage: true);
                Resources.RetainTexture(key);
                return texture;
            }
            finally { Session.CompleteFrame(present: false); }
        }

        public void Dispose()
        {
            Session.Dispose();
            factory.Dispose();
        }
    }
}
