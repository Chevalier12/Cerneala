using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class Sprite2DImageResourceTests
{
    [Fact]
    public void TwoSpritesResolveOneLocalImageResourceThroughOneRootCache()
    {
        ResourceId<ImageResource> id = new("WorldAtlas");
        DisposableImage loaded = new("loaded");
        RecordingImageLoader loader = new();
        loader.SetImage("world.png", loaded);
        RenderSurface2D surface = SurfaceWithSprites(
            Sprite(id, directSource: new TestImage("fallback-a")),
            Sprite(id, directSource: new TestImage("fallback-b")));
        surface.Resources.SetResource(id, new ImageResource("world.png"));
        UIRoot root = new();
        root.SetImageLoader(loader);
        root.VisualChildren.Add(surface);

        DrawCommand[] draws = Record(surface)
            .Where(command => command.Kind == DrawCommandKind.DrawImage)
            .ToArray();

        Assert.Equal(2, draws.Length);
        Assert.All(draws, draw => Assert.Same(loaded, draw.Image));
        Assert.Equal(1, loader.GetLoadCount("world.png"));
        Assert.False(loaded.IsDisposed);
    }

    [Fact]
    public void NonNullResourceIdTakesPrecedenceWithoutFallingBackToSource()
    {
        ResourceId<ImageResource> missing = new("Missing");
        TestImage direct = new("direct");
        Sprite2D sprite = Sprite(missing, direct);
        RenderSurface2D surface = SurfaceWithSprites(sprite);

        Assert.Empty(Record(surface).Where(command => command.Kind == DrawCommandKind.DrawImage));

        sprite.SourceResourceId = null;

        DrawCommand draw = Assert.Single(
            Record(surface).Where(command => command.Kind == DrawCommandKind.DrawImage));
        Assert.Same(direct, draw.Image);
    }

    [Fact]
    public void RootResourceReplacementInvalidatesSurfaceAndResolvesReplacement()
    {
        ResourceId<ImageResource> id = new("WorldAtlas");
        DisposableImage first = new("first");
        DisposableImage second = new("second");
        RecordingImageLoader loader = new();
        loader.SetImage("first.png", first);
        loader.SetImage("second.png", second);
        ResourceStore resources = new();
        resources.SetResource(id, new ImageResource("first.png"));
        RenderSurface2D surface = SurfaceWithSprites(Sprite(id));
        surface.RedrawMode = RenderSurface2DRedrawMode.OnDemand;
        UIRoot root = new();
        root.SetResourceProvider(resources);
        root.SetImageLoader(loader);
        root.VisualChildren.Add(surface);
        Assert.Same(first, Assert.Single(Record(surface).Where(IsImageDraw)).Image);
        long before = ((IRenderSurface2DFrameSource)surface).FrameVersion;

        resources.SetResource(id, new ImageResource("second.png"));

        Assert.True(((IRenderSurface2DFrameSource)surface).FrameVersion > before);
        Assert.Same(second, Assert.Single(Record(surface).Where(IsImageDraw)).Image);
        Assert.False(first.IsDisposed);
        Assert.False(second.IsDisposed);

        root.SetImageLoader(null);
        Assert.Equal(1, first.DisposeCount);
        Assert.Equal(1, second.DisposeCount);
    }

    [Fact]
    public void ReattachingToAnotherRootUsesThatRootsCacheWithoutPrematureDisposal()
    {
        ResourceId<ImageResource> id = new("WorldAtlas");
        DisposableImage first = new("first-root");
        DisposableImage second = new("second-root");
        RecordingImageLoader firstLoader = new();
        RecordingImageLoader secondLoader = new();
        firstLoader.SetImage("first.png", first);
        secondLoader.SetImage("second.png", second);
        ResourceStore firstResources = new();
        ResourceStore secondResources = new();
        firstResources.SetResource(id, new ImageResource("first.png"));
        secondResources.SetResource(id, new ImageResource("second.png"));
        RenderSurface2D surface = SurfaceWithSprites(Sprite(id));
        surface.RedrawMode = RenderSurface2DRedrawMode.OnDemand;
        UIRoot firstRoot = new();
        UIRoot secondRoot = new();
        firstRoot.SetResourceProvider(firstResources);
        secondRoot.SetResourceProvider(secondResources);
        firstRoot.SetImageLoader(firstLoader);
        secondRoot.SetImageLoader(secondLoader);

        firstRoot.VisualChildren.Add(surface);
        Assert.Same(first, Assert.Single(Record(surface).Where(IsImageDraw)).Image);
        firstRoot.VisualChildren.Remove(surface);
        Assert.False(first.IsDisposed);

        secondRoot.VisualChildren.Add(surface);
        Assert.Same(second, Assert.Single(Record(surface).Where(IsImageDraw)).Image);
        Assert.False(first.IsDisposed);
        Assert.False(second.IsDisposed);

        long beforeOldRootChange = ((IRenderSurface2DFrameSource)surface).FrameVersion;
        firstResources.SetResource(id, new ImageResource("unused.png"));
        Assert.Equal(beforeOldRootChange, ((IRenderSurface2DFrameSource)surface).FrameVersion);

        firstRoot.SetImageLoader(null);
        Assert.Equal(1, first.DisposeCount);
        Assert.False(second.IsDisposed);
        secondRoot.SetImageLoader(null);
        Assert.Equal(1, second.DisposeCount);
    }

    [Fact]
    public void RemovingLocalResourceInvalidatesFrameWithoutDisposingCachedImage()
    {
        ResourceId<ImageResource> id = new("WorldAtlas");
        DisposableImage loaded = new("loaded");
        RecordingImageLoader loader = new();
        loader.SetImage("world.png", loaded);
        RenderSurface2D surface = SurfaceWithSprites(Sprite(id));
        surface.RedrawMode = RenderSurface2DRedrawMode.OnDemand;
        surface.Resources.SetResource(id, new ImageResource("world.png"));
        UIRoot root = new();
        root.SetImageLoader(loader);
        root.VisualChildren.Add(surface);
        Assert.Single(Record(surface).Where(IsImageDraw));
        long before = ((IRenderSurface2DFrameSource)surface).FrameVersion;

        Assert.True(surface.Resources.Remove(id.Key));

        Assert.True(((IRenderSurface2DFrameSource)surface).FrameVersion > before);
        Assert.Empty(Record(surface).Where(IsImageDraw));
        Assert.False(loaded.IsDisposed);
        root.SetImageLoader(null);
        Assert.Equal(1, loaded.DisposeCount);
    }

    private static bool IsImageDraw(DrawCommand command)
    {
        return command.Kind == DrawCommandKind.DrawImage;
    }

    private static Sprite2D Sprite(
        ResourceId<ImageResource> id,
        IDrawImage? directSource = null)
    {
        Sprite2D sprite = new()
        {
            Source = directSource,
            Destination = new DrawRect(0, 0, 1, 1)
        };
        sprite.SourceResourceId = id;
        return sprite;
    }

    private static RenderSurface2D SurfaceWithSprites(params Sprite2D[] sprites)
    {
        Scene2D scene = new();
        foreach (Sprite2D sprite in sprites)
        {
            scene.Children.Add(sprite);
        }

        return new RenderSurface2D { Scene = scene };
    }

    private static DrawCommandList Record(RenderSurface2D surface)
    {
        DrawCommandList commands = new();
        ((IRenderSurface2DFrameSource)surface).RecordFrame(
            commands,
            new DrawRect(0, 0, 100, 100));
        return commands;
    }

    private class TestImage(string name) : IDrawImage
    {
        public string Name { get; } = name;

        public int Width => 16;

        public int Height => 16;
    }

    private sealed class DisposableImage(string name) : TestImage(name), IDisposable
    {
        public bool IsDisposed { get; private set; }

        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
            DisposeCount++;
        }
    }

    private sealed class RecordingImageLoader : IImageLoader
    {
        private readonly Dictionary<string, IDrawImage> images = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> counts = new(StringComparer.Ordinal);

        public void SetImage(string path, IDrawImage image)
        {
            images[path] = image;
        }

        public int GetLoadCount(string path)
        {
            return counts.GetValueOrDefault(path);
        }

        public IDrawImage Load(string path)
        {
            counts[path] = GetLoadCount(path) + 1;
            return images[path];
        }
    }
}
