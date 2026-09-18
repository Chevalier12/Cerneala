using Cerneala.Drawing;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Resources;

public sealed class ImageResourceResidentProbeTests
{
    [Fact]
    public async Task ResidentProbeNeverLoadsWaitsOrRevivesForgottenIdentities()
    {
        Loader loader = new();
        await using ImageResourceCache cache = new(loader);
        ImageResource resource = new("atlas.png");
        Assert.Null(cache.TryAcquireResident(resource));
        Assert.Equal(0, loader.Loads);
        Task<ImageResourceLease> loading = cache.AcquireAsync(resource).AsTask();
        Assert.Null(cache.TryAcquireResident(resource));
        Assert.Equal(1, loader.Loads);
        TestImage image = new();
        loader.Ready.SetResult(image);
        using ImageResourceLease first = await loading;
        using ImageResourceLease resident = Assert.IsType<ImageResourceLease>(cache.TryAcquireResident(resource));
        Assert.Same(image, resident.Image);
        cache.Remove(resource);
        Assert.Null(cache.TryAcquireResident(resource));
        Assert.Same(image, resident.Image);
        cache.Dispose();
        Assert.Null(cache.TryAcquireResident(resource));
        first.Dispose();
        Assert.Equal(0, image.Disposals);
        resident.Dispose();
        Assert.Equal(1, image.Disposals);
    }

    private sealed class Loader : IAsyncImageLoader
    {
        internal int Loads { get; private set; }
        internal TaskCompletionSource<IDrawImage> Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public IDrawImage Load(string path) => throw new InvalidOperationException("The probe must not invoke synchronous loading.");
        public ValueTask<IDrawImage> LoadAsync(string path, CancellationToken cancellationToken = default)
        {
            Loads++;
            return new(Ready.Task);
        }
    }

    private sealed class TestImage : IDrawImage, IDisposable
    {
        public int Width => 10;
        public int Height => 10;
        internal int Disposals { get; private set; }
        public void Dispose() => Disposals++;
    }
}
