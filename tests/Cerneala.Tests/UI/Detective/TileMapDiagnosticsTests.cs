using System.Reflection;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Detective;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class TileMapDiagnosticsTests
{
    [Fact]
    public void RootSnapshotExposesLastTileRecordingWithoutChangingItsState()
    {
        MethodInfo? capture = typeof(Cerneala.UI.Detective.Detective)
            .GetMethod("CaptureTileMap", [typeof(TileMap2D)]);
        Assert.NotNull(capture);
        UIRoot root = new();
        ResourceId<ImageResource> atlas = new("atlas");
        TileMap2D map = new()
        {
            Model = new TileMap2DModel(new DrawSize(16, 16),
                [new TileSet2D("atlas", atlas, [new TileDefinition2D(1, new DrawRect(0, 0, 16, 16))])],
                [new TileLayer2DModel("ground", [new TileChunk2D(default, 2, 1, [new TileCell2D(1), new TileCell2D(1)])])])
        };
        map.Resources.SetResource(atlas, new ImageResource(new TestImage()));
        Scene2D scene = new();
        scene.Children.Add(map);
        RenderSurface2D surface = new() { Scene = scene };
        root.VisualChildren.Add(surface);
        DrawCommandList commands = new();
        ((IRenderSurface2DFrameSource)surface).RecordFrame(commands, new DrawRect(0, 0, 32, 16));
        TileMap2DDiagnosticsSnapshot before = map.GetDiagnosticsSnapshot();
        int renderVersion = map.RenderVersion;
        long commandVersion = commands.Version;

        object first = capture.Invoke(root.Detective, [map])!;
        object second = capture.Invoke(root.Detective, [map])!;

        Assert.Equal(first, second);
        foreach (PropertyInfo field in typeof(TileMap2DDiagnosticsSnapshot).GetProperties())
        {
            PropertyInfo? published = first.GetType().GetProperty(field.Name);
            Assert.NotNull(published);
            Assert.Equal(field.GetValue(before), published.GetValue(first));
        }
        Assert.Equal(before, map.GetDiagnosticsSnapshot());
        Assert.Equal(renderVersion, map.RenderVersion);
        Assert.Equal(commandVersion, commands.Version);
    }

    private sealed class TestImage : IDrawImage
    {
        public int Width => 16;
        public int Height => 16;
    }

    [Fact]
    public void CaptureRequiresTheOwningRootAndDoesNotAllocateAfterWarmup()
    {
        UIRoot root = new();
        UIRoot other = new();
        TileMap2D map = new();
        Scene2D scene = new();
        scene.Children.Add(map);
        RenderSurface2D surface = new() { Scene = scene };
        Assert.Throws<ArgumentNullException>(() => root.Detective.CaptureTileMap(null!));
        Assert.Throws<ArgumentException>(() => root.Detective.CaptureTileMap(map));
        root.VisualChildren.Add(surface);
        Assert.Throws<ArgumentException>(() => other.Detective.CaptureTileMap(map));
        for (int i = 0; i < 256; i++) { _ = root.Detective.CaptureTileMap(map); }
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10000; i++) { _ = root.Detective.CaptureTileMap(map); }
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
        Assert.Equal(default, root.Detective.CaptureTileMap(map));
        root.VisualChildren.Remove(surface);
        Assert.Throws<ArgumentException>(() => root.Detective.CaptureTileMap(map));
    }
}
