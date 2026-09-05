using Cerneala.Drawing;
using Cerneala.Playground;
using Cerneala.Tests.UI.Motion.Core;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Resources;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.Playground;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class SceneWorldShowcaseTests
{
    [Fact]
    public async Task CompiledWorldMarkupRunsItsEffectsAnimationsAndInputContracts()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(800, 600, motionClock: clock);
        root.SetImageLoader(new AtlasLoader());
        SceneWorldShowcase view = new();
        root.VisualChildren.Add(view);
        UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(800, 600) });
        ServoApi servo = new(host);
        void Frame(int milliseconds)
        {
            clock.Advance(TimeSpan.FromMilliseconds(milliseconds));
            host.Update(new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty,
                KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []), host.Viewport, TimeSpan.FromMilliseconds(milliseconds));
        }
        Frame(0);
        RenderSurface2D surface = Descendants(view).OfType<RenderSurface2D>().Single();
        Scene2D world = surface.Scene!;
        UIElement[] nodes = Descendants(world).Prepend(world).ToArray();
        TileMap2D map = nodes.OfType<TileMap2D>().Single();
        TileLayer2D layer = nodes.OfType<TileLayer2D>().Single(l => l.LayerId == "2");
        TileInstance2D door = nodes.OfType<TileInstance2D>().Single();
        BoxCollider2D doorCollider = door.LogicalChildren.OfType<BoxCollider2D>().Single();
        Scene2D player = nodes.OfType<Scene2D>().Single(n => ServoApi.GetId(n) == "world-player");
        Sprite2D playerSprite = player.Children.OfType<Sprite2D>().Single();
        SceneItems2D npcs = nodes.OfType<SceneItems2D>().Single(n => ReferenceEquals(n.ItemsSource, view.State.Npcs));
        Sprite2D npcSprite = Descendants(npcs).OfType<Sprite2D>().Single();
        Scene2DDebugOverlay overlay = nodes.OfType<Scene2DDebugOverlay>().Single();
        UIElement[] visualMatrix = [world, map, layer, door, player, playerSprite, npcSprite, overlay];
        Assert.All(visualMatrix, node =>
        {
            Assert.NotNull(node.Aspect);
            Assert.True(PrismAttachment.TryGetInstance(node, out _));
        });
        Assert.All(nodes.OfType<SceneItems2D>(), node =>
        {
            Assert.Null(node.Aspect);
            Assert.False(PrismAttachment.TryGetInstance(node, out _));
        });
        Assert.NotNull(doorCollider.Aspect);
        Assert.False(PrismAttachment.TryGetInstance(doorCollider, out _));
        UIElement[] loadedFades = [world, map, layer, player, playerSprite, npcSprite, overlay];
        float[] starts = loadedFades.Select(n => n.Opacity).ToArray();
        Assert.All(starts, value => Assert.InRange(value, 0.39f, 0.71f));
        Assert.InRange(doorCollider.OffsetX, 0.99f, 1.01f);
        Frame(75);
        for (int i = 0; i < loadedFades.Length; i++)
        {
            Assert.InRange(loadedFades[i].Opacity, starts[i] + 0.01f, 0.99f);
            Assert.Equal(UiPropertyValueSource.Animation, loadedFades[i].GetValueSource(UIElement.OpacityProperty));
        }
        Assert.InRange(doorCollider.OffsetX, 0.01f, 0.99f);
        Frame(75);
        Assert.Equal(0, doorCollider.OffsetX);
        Assert.Equal(0.9f, overlay.Opacity);
        DrawCommandList Record()
        {
            DrawCommandList commands = new();
            ((IRenderSurface2DFrameSource)surface).RecordFrame(commands,
                new DrawRect(0, 0, surface.ArrangedBounds.Width, surface.ArrangedBounds.Height));
            return commands;
        }
        DrawCommand PlayerDraw() => Record().Single(c => c.Kind == DrawCommandKind.DrawImage &&
            c.ImageSource is DrawRect r && r.Y == 16 && r.X < 64);
        Assert.Equal(7, Record().Count(c => c.Kind == DrawCommandKind.BeginPrism));
        Assert.Equal(1, root.ImageResourceCache!.LoadCount);
        Record(); // The first recording builds the retained tile batches.
        Assert.True(map.GetDiagnosticsSnapshot().BatchesReused > 0);
        Assert.Equal(64, map.Model!.Layers.Sum(l => l.Chunks.Count));

        await servo.ClickAsync(ServoTarget.ById("world-player"));
        await servo.PressKeyAsync(InputKey.Up);
        Assert.Equal(-32, view.LastMove!.Travel.Y);
        Assert.Equal("Walk", view.State.PlayerState);
        Frame(200);
        Assert.Equal(new DrawRect(48, 16, 16, 16), PlayerDraw().ImageSource);
        await servo.PressKeyAsync(InputKey.Space);
        Assert.Equal(new DrawRect(48, 16, 16, 16), PlayerDraw().ImageSource);
        Assert.Equal(DrawImageFlip.Horizontal, PlayerDraw().ImageFlip);
        Frame(240);
        Assert.Equal(new DrawRect(0, 16, 16, 16), PlayerDraw().ImageSource);
        Assert.Equal(DrawImageFlip.None, PlayerDraw().ImageFlip);

        await servo.ClickAsync(ServoTarget.ById("world-door"));
        Frame(90);
        Assert.InRange(door.Opacity, 0.66f, 0.99f);
        Assert.False(doorCollider.Enabled);
        Frame(150);
        Assert.Equal(1, door.Opacity);
        Assert.Contains(Record(), c => c.Kind == DrawCommandKind.DrawImage && c.ImageSource == new DrawRect(112, 0, 16, 16));

        TileMap2DModel original = map.Model;
        var collisionBefore = world.CollisionWorld.GetDiagnosticsSnapshot();
        ServoElement pickingBefore = await servo.FindAsync(ServoTarget.ById("world-player"));
        await servo.ClickAsync(ServoTarget.ById("world-debug"));
        Assert.Equal(8, Record().Count(c => c.Kind == DrawCommandKind.BeginPrism));
        Assert.True(overlay.GetDiagnosticsSnapshot().Primitives > 0);
        Assert.Same(original, map.Model);
        var collisionAfter = world.CollisionWorld.GetDiagnosticsSnapshot();
        Assert.Equal(collisionBefore.EntryCount, collisionAfter.EntryCount);
        Assert.Equal(collisionBefore.RebuildCount, collisionAfter.RebuildCount);
        Assert.Equal(collisionBefore.IncrementalUpdateCount, collisionAfter.IncrementalUpdateCount);
        Assert.Equal(pickingBefore.Bounds, (await servo.FindAsync(ServoTarget.ById("world-player"))).Bounds);
        UIElement firstNpc = npcs.LogicalChildren[0];
        await servo.ClickAsync(ServoTarget.ById("world-add"));
        Assert.Equal(2, npcs.RealizedItemCount);
        Assert.Same(firstNpc, npcs.LogicalChildren[0]);
        root.VisualChildren.Remove(view);
    }

    private static IEnumerable<UIElement> Descendants(UIElement element)
    {
        foreach (UIElement child in element.VisualChildren.Concat(element.LogicalChildren).Distinct())
        {
            yield return child;
            foreach (UIElement descendant in Descendants(child)) yield return descendant;
        }
    }

    private sealed class AtlasLoader : IImageLoader
    {
        public IDrawImage Load(string path) => new Atlas();
    }
    private sealed class Atlas : IDrawImage
    {
        public int Width => 128;
        public int Height => 32;
    }
}
