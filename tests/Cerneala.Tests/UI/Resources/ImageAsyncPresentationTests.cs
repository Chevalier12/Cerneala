using System.Runtime.CompilerServices;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Resources;

public sealed class ImageAsyncPresentationTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void SharedPendingAtlasDoesNotBlockTheUiFrame(int iteration)
    {
        _ = iteration;
        TaskCompletionSource<IDrawImage> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ManualResetEventSlim measuring = new();
        using ManualResetEventSlim frameDone = new();
        Loader loader = new(completion.Task);
        TestImage image = new();
        Exception? failure = null;
        Thread ui = new(() =>
        {
            try
            {
                RunSharedFrameOnOwnedUiThread(loader, measuring, frameDone, image);
            }
            catch (Exception error) { failure = error; frameDone.Set(); }
        }) { IsBackground = true };
        ui.Start();
        bool reachedMeasure = measuring.Wait(TimeSpan.FromSeconds(5));
        bool finishedWhilePending = reachedMeasure && frameDone.Wait(TimeSpan.FromMilliseconds(250));
        completion.TrySetResult(image);
        Assert.True(ui.Join(TimeSpan.FromSeconds(5)), "UI worker did not finish after releasing the pending atlas.");
        Assert.Null(failure);
        Assert.True(reachedMeasure, "The real Image.MeasureCore path was not reached.");
        Assert.Equal(1, loader.AsyncLoads);
        Assert.Equal(0, loader.SyncLoads);
        Assert.Equal(1, image.DisposeCount);
        Assert.True(finishedWhilePending, "UI frame waited for the shared asynchronous atlas load.");
    }

    [Fact]
    public async Task ColdImageStartsAsyncPreparationInsteadOfSynchronousDecoding()
    {
        TaskCompletionSource<IDrawImage> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Loader loader = new(completion.Task);
        UIRoot root = new(100, 100);
        root.SetImageLoader(loader);
        ResourceId<ImageResource> id = new("cold-atlas");
        root.Resources.SetResource(id, new ImageResource("atlas.png"));
        Cerneala.UI.Controls.Image image = new() { SourceResourceId = id, Width = 60, Height = 40 };
        root.VisualChildren.Add(image);
        try
        {
            root.ProcessFrame();
            Assert.Equal(0, loader.SyncLoads);
            Assert.Equal(1, loader.AsyncLoads);
            Assert.Equal(new LayoutSize(60, 40), image.DesiredSize);
            Assert.True(image.IsAttached);
            Assert.True(image.IsEnabled);
        }
        finally
        {
            root.VisualChildren.Remove(image);
            completion.TrySetResult(new TestImage());
            await root.ImageResourceCache!.DisposeAsync();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PendingImageOmitsPixelsAndCompletionUpdatesOnlyNaturalDimensions(bool explicitWidth)
    {
        await using Fixture fixture = new();
        if (explicitWidth) { fixture.Image.Width = 60; }
        fixture.Root.ProcessFrame();
        Assert.Equal(ImageLoadingState.Loading, fixture.Image.LoadingState);
        Assert.Null(fixture.Image.LoadingError);
        Assert.Equal(new LayoutSize(explicitWidth ? 60 : 0, 0), fixture.Image.DesiredSize);
        Assert.Empty(fixture.Root.RetainedRenderer.Commit(fixture.Root));
        Assert.Throws<InvalidOperationException>(() => fixture.Image.SetValue(Image.LoadingStateProperty, ImageLoadingState.Ready));
        Assert.Throws<InvalidOperationException>(() => fixture.Image.SetValue(Image.LoadingErrorProperty, new IOException()));

        fixture.Completion.SetResult(fixture.Bitmap);
        fixture.PumpUntil(() => fixture.Image.LoadingState == ImageLoadingState.Ready);

        Assert.Equal(new LayoutSize(explicitWidth ? 60 : 16, 8), fixture.Image.DesiredSize);
        Assert.Same(fixture.Bitmap, Assert.Single(fixture.Root.RetainedRenderer.Commit(fixture.Root)).Image);
        for (int i = 0; i < 16; i++) { fixture.Root.ProcessFrame(); fixture.Root.RetainedRenderer.Commit(fixture.Root); }
        Assert.Equal(1, fixture.Loader.AsyncLoads);
        Assert.Equal(0, fixture.Loader.SyncLoads);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompletionUsesTheCurrentIntrinsicSizePolicy(bool changeWhilePending)
    {
        await using Fixture fixture = new();
        fixture.Image.UseIntrinsicSize = false;
        fixture.Root.ProcessFrame();
        fixture.Root.RetainedRenderer.Commit(fixture.Root);
        if (changeWhilePending) { fixture.Image.UseIntrinsicSize = true; fixture.Root.ProcessFrame(); }
        fixture.Completion.SetResult(fixture.Bitmap);
        Assert.True(SpinWait.SpinUntil(() => fixture.Root.Relay.HasPendingWork, TimeSpan.FromSeconds(5)));
        var stats = fixture.Root.ProcessFrame();
        fixture.Root.RetainedRenderer.Commit(fixture.Root);
        Assert.Equal(ImageLoadingState.Ready, fixture.Image.LoadingState);
        if (changeWhilePending) { Assert.Equal(new LayoutSize(16, 8), fixture.Image.DesiredSize); }
        else { Assert.Equal(0, stats.MeasuredElements); Assert.Equal(LayoutSize.Zero, fixture.Image.DesiredSize); }
    }

    [Fact]
    public async Task FailedLoadIsObservableWithoutRetryStormAndReplacingTheResourceRecovers()
    {
        await using Fixture fixture = new();
        fixture.Root.ProcessFrame();
        IOException failure = new("atlas unavailable");
        fixture.Completion.SetException(failure);
        fixture.PumpUntil(() => fixture.Image.LoadingState == ImageLoadingState.Error);
        Assert.Same(failure, fixture.Image.LoadingError);
        for (int i = 0; i < 16; i++) { fixture.Root.ProcessFrame(); fixture.Root.RetainedRenderer.Commit(fixture.Root); }
        Assert.Same(failure, fixture.Image.LoadingError);
        Assert.Equal(1, fixture.Loader.AsyncLoads);
        Assert.Empty(fixture.Root.RetainedRenderer.Commit(fixture.Root));

        fixture.Root.Resources.SetResource(Fixture.Id, new ImageResource(fixture.Bitmap));
        fixture.Root.ProcessFrame();

        Assert.Equal(ImageLoadingState.Ready, fixture.Image.LoadingState);
        Assert.Null(fixture.Image.LoadingError);
        Assert.Same(fixture.Bitmap, Assert.Single(fixture.Root.RetainedRenderer.Commit(fixture.Root)).Image);
    }

    [Fact]
    public async Task ForgettingTheFailedPathAndInvalidatingPermitsAnExplicitRetry()
    {
        await using Fixture fixture = new();
        fixture.Root.ProcessFrame();
        fixture.Completion.SetException(new IOException("first attempt failed"));
        fixture.PumpUntil(() => fixture.Image.LoadingState == ImageLoadingState.Error);

        fixture.Loader.Completion = Task.FromResult<IDrawImage>(fixture.Bitmap);
        fixture.Cache.Remove(new ImageResource("atlas.png"));
        fixture.Image.Invalidate(InvalidationFlags.Measure | InvalidationFlags.Render, "Retry image loading");
        fixture.PumpUntil(() => fixture.Image.LoadingState == ImageLoadingState.Ready);

        Assert.Null(fixture.Image.LoadingError);
        Assert.Equal(2, fixture.Loader.AsyncLoads);
        Assert.Equal(0, fixture.Loader.SyncLoads);
        Assert.Equal(Fixture.Id, fixture.Image.SourceResourceId);
        Assert.Same(fixture.Bitmap, Assert.Single(fixture.Root.RetainedRenderer.Commit(fixture.Root)).Image);
    }

    [Fact]
    public async Task ReplacingTheLoaderCancelsOldInterestAndRejectsItsLateImage()
    {
        await using Fixture fixture = new();
        fixture.Root.ProcessFrame();
        TestImage replacement = new();
        Loader replacementLoader = new(Task.FromResult<IDrawImage>(replacement));
        fixture.Root.SetImageLoader(replacementLoader);
        fixture.PumpUntil(() => fixture.Image.LoadingState == ImageLoadingState.Ready);
        Assert.True(fixture.Loader.Token.IsCancellationRequested);

        fixture.Completion.SetResult(fixture.Bitmap);
        Assert.True(SpinWait.SpinUntil(() => fixture.Cache.PendingLoadCount == 0, TimeSpan.FromSeconds(5)));
        fixture.Root.ProcessFrame();

        Assert.Equal(0, fixture.Cache.ResidentCount);
        Assert.Equal(1, fixture.Bitmap.DisposeCount);
        Assert.Equal(1, replacementLoader.AsyncLoads);
        Assert.Same(replacement, Assert.Single(fixture.Root.RetainedRenderer.Commit(fixture.Root)).Image);
    }

    [Fact]
    public async Task SynchronousOnlyLoaderIsNotRunOnTheUiThreadOrSilentlyMovedToAWorker()
    {
        await using Fixture fixture = new();
        SyncLoader loader = new();
        fixture.Root.SetImageLoader(loader);
        fixture.Root.ProcessFrame();
        Assert.Equal(ImageLoadingState.Error, fixture.Image.LoadingState);
        Assert.IsType<NotSupportedException>(fixture.Image.LoadingError);
        Assert.Equal(0, loader.Calls);
        Assert.Empty(fixture.Root.RetainedRenderer.Commit(fixture.Root));
    }

    [Fact]
    public async Task MissingAndNullResourcesRemainEmptyReadyImages()
    {
        await using Fixture fixture = new();
        fixture.Image.SourceResourceId = new("missing");
        fixture.Root.ProcessFrame();
        Assert.Equal(ImageLoadingState.Ready, fixture.Image.LoadingState);
        Assert.Null(fixture.Image.LoadingError);
        Assert.Empty(fixture.Root.RetainedRenderer.Commit(fixture.Root));
        fixture.Image.SourceResourceId = null;
        fixture.Root.ProcessFrame();
        Assert.Equal(ImageLoadingState.Ready, fixture.Image.LoadingState);
        Assert.Equal(0, fixture.Loader.AsyncLoads);
    }

    [Fact]
    public async Task DetachCancelsPendingInterestAndLateCompletionCannotPublishIntoTheOldRoot()
    {
        for (int iteration = 0; iteration < 32; iteration++)
        {
            await using Fixture fixture = new();
            fixture.Root.ProcessFrame();
            fixture.Root.VisualChildren.Remove(fixture.Image);
            Assert.True(fixture.Loader.Token.IsCancellationRequested);
            int version = fixture.Image.RenderVersion;
            fixture.Completion.SetResult(fixture.Bitmap);
            Assert.True(SpinWait.SpinUntil(() => fixture.Cache.PendingLoadCount == 0, TimeSpan.FromSeconds(5)));
            fixture.Root.ProcessFrame();
            Assert.Equal(version, fixture.Image.RenderVersion);
            Assert.False(fixture.Image.IsAttached);
            Assert.Equal(0, fixture.Cache.ResidentCount);
            Assert.Equal(1, fixture.Bitmap.DisposeCount);
        }
    }

    [Fact]
    public async Task ASharedPendingLoadDoesNotRetainTheDetachedControlOrItsRoot()
    {
        TaskCompletionSource<IDrawImage> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ImageResourceCache cache = new(new Loader(completion.Task));
        Task<ImageResourceLease> shared = cache.AcquireAsync(new ImageResource("atlas.png")).AsTask();
        bool imageAlive, rootAlive;
        try
        {
            (WeakReference root, WeakReference image) = CreateDetachedImage(cache);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            imageAlive = image.IsAlive;
            rootAlive = root.IsAlive;
        }
        finally { completion.TrySetResult(new TestImage()); }
        using ImageResourceLease lease = await shared.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(!imageAlive && !rootAlive,
            $"The shared pending completion retained a detached image ({imageAlive}) or its root ({rootAlive}).");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference Root, WeakReference Image) CreateDetachedImage(ImageResourceCache cache)
    {
        UIRoot root = new(100, 100);
        root.SetImageResourceCache(null, cache);
        root.Resources.SetResource(Fixture.Id, new ImageResource("atlas.png"));
        Image image = new() { SourceResourceId = Fixture.Id };
        root.VisualChildren.Add(image);
        root.ProcessFrame();
        Assert.Equal(ImageLoadingState.Loading, image.LoadingState);
        root.VisualChildren.Remove(image);
        root.SetImageResourceCache(null, null);
        return (new(root), new(image));
    }

    [Fact]
    public async Task ReadyNotificationCannotReturnAnImageWhoseAcquisitionTheHandlerReleased()
    {
        await using Fixture fixture = new();
        fixture.Root.ProcessFrame();
        fixture.Image.PropertyChanged += (_, args) =>
        {
            if (ReferenceEquals(args.Property, Image.LoadingStateProperty) && fixture.Image.LoadingState == ImageLoadingState.Ready)
            {
                fixture.Image.SourceResourceId = null;
            }
        };
        fixture.Completion.SetResult(fixture.Bitmap);
        fixture.PumpUntil(() => fixture.Image.SourceResourceId is null);
        Assert.Empty(fixture.Root.RetainedRenderer.Commit(fixture.Root));
        Assert.Equal(1, fixture.Bitmap.DisposeCount);
    }

    // This deliberately blocked wait belongs to a dedicated, bounded UI worker,
    // never to an xUnit synchronization-context continuation.
    private static void RunSharedFrameOnOwnedUiThread(Loader loader, ManualResetEventSlim measuring,
        ManualResetEventSlim frameDone, TestImage image)
    {
        UIRoot root = new(100, 100);
        root.SetImageLoader(loader);
        root.ProcessFrame();
        ResourceId<ImageResource> id = new("shared-atlas");
        ImageResource resource = new("atlas.png");
        root.Resources.SetResource(id, resource);
        Task<ImageResourceLease> shared = root.ImageResourceCache!.AcquireAsync(resource).AsTask();
        ProbeImage control = new(measuring) { SourceResourceId = id };
        root.VisualChildren.Add(control);
        try
        {
            root.ProcessFrame();
            frameDone.Set();
            using ImageResourceLease lease = shared.GetAwaiter().GetResult();
            Assert.Same(image, lease.Image);
        }
        finally
        {
            root.VisualChildren.Remove(control);
            root.ImageResourceCache.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        Assert.Equal(0, root.ImageResourceCache.ResidentCount);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        internal static readonly ResourceId<ImageResource> Id = new("atlas");
        internal readonly TaskCompletionSource<IDrawImage> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly UIRoot Root = new(100, 100);
        internal readonly Image Image = new() { SourceResourceId = Id };
        internal readonly TestImage Bitmap = new();
        internal readonly Loader Loader;
        internal readonly ImageResourceCache Cache;
        internal Fixture()
        {
            Loader = new(Completion.Task);
            Root.SetImageLoader(Loader);
            Cache = Root.ImageResourceCache!;
            Root.Resources.SetResource(Id, new ImageResource("atlas.png"));
            Root.VisualChildren.Add(Image);
        }
        internal void PumpUntil(Func<bool> done) => Assert.True(SpinWait.SpinUntil(() =>
        {
            Root.ProcessFrame();
            Root.RetainedRenderer.Commit(Root);
            return done();
        }, TimeSpan.FromSeconds(5)), "Image presentation did not reach the expected state.");
        public async ValueTask DisposeAsync()
        {
            Root.VisualChildren.Remove(Image);
            ImageResourceCache? current = Root.ImageResourceCache;
            Root.SetImageLoader(null);
            Completion.TrySetResult(Bitmap);
            await Cache.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
            if (current is not null && !ReferenceEquals(current, Cache)) { await current.DisposeAsync(); }
        }
    }

    private sealed class ProbeImage(ManualResetEventSlim measuring) : Cerneala.UI.Controls.Image
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            measuring.Set();
            return base.MeasureCore(context);
        }
    }

    private sealed class Loader(Task<IDrawImage> completion) : IAsyncImageLoader
    {
        internal Task<IDrawImage> Completion = completion;
        internal int AsyncLoads;
        internal int SyncLoads;
        internal CancellationToken Token;
        public IDrawImage Load(string path) { Interlocked.Increment(ref SyncLoads); return new TestImage(); }
        public ValueTask<IDrawImage> LoadAsync(string path, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref AsyncLoads);
            Token = cancellationToken;
            return new(Completion);
        }
    }

    private sealed class TestImage : IDrawImage, IDisposable
    {
        internal int DisposeCount;
        public int Width { get { ObjectDisposedException.ThrowIf(DisposeCount != 0, this); return 16; } }
        public int Height { get { ObjectDisposedException.ThrowIf(DisposeCount != 0, this); return 8; } }
        public void Dispose() => Interlocked.Increment(ref DisposeCount);
    }

    private sealed class SyncLoader : IImageLoader
    {
        internal int Calls;
        public IDrawImage Load(string path) { Calls++; return new TestImage(); }
    }
}
