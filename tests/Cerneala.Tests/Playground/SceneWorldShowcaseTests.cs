using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Playground;
using Cerneala.Scene2D.Importers;
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
    public async Task ShowcaseNavigationSelectsSceneWorldThroughServoClick()
    {
        UIRoot root = new(300, 400);
        ShowcaseNavigation navigation = new();
        root.VisualChildren.Add(navigation);
        string? selected = null;
        navigation.ShowcaseSelected += (_, args) => selected = args.ShowcaseName;
        UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(300, 400) });
        host.Update(new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty,
            KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []), host.Viewport, TimeSpan.Zero);

        await new ServoApi(host).ClickAsync(ServoTarget.ById("showcase-scene-world"));

        Assert.Equal("Scene World", selected);
        root.VisualChildren.Remove(navigation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WallMigrationPreservesTileDrawingAndTheSixAuthoredCollisionRegions(bool ldtk)
    {
        SceneWorldState state = new();
        state.Load(ldtk);
        string path = Path.Combine(AppContext.BaseDirectory, "SceneWorldAssets", ldtk ? "village.ldtk" : "village.tmj");
        Scene2DImportResult imported = ldtk ? LdtkScene2DImporter.Import(path) : TiledScene2DImporter.Import(path);
        Assert.True(imported.Success);
        TileMap2DModel original = imported.Document!.Levels.Single().TileMap;
        if (ldtk)
        {
            Assert.Equal(3, original.Layers.Sum(l => l.Chunks.Count));
            Assert.Equal(["1", "2", "4"], original.Layers.Where(l => l.Chunks.Count > 0).Select(l => l.Id).Order());
        }
        List<DrawRect> collisionRegions = [];
        Assert.Equal(6, state.Colliders.Count);
        Assert.Equal(original.Layers.Count, state.Model.Layers.Count);
        foreach (TileLayer2DModel before in original.Layers)
        {
            TileLayer2DModel after = state.Model.Layers.Single(l => l.Id == before.Id);
            Assert.Equal((before.Offset, before.Opacity, before.Tint, before.Order, before.IsVisible),
                (after.Offset, after.Opacity, after.Tint, after.Order, after.IsVisible));
            Assert.Equal(before.Chunks.Count, after.Chunks.Count);
            for (int chunkIndex = 0; chunkIndex < before.Chunks.Count; chunkIndex++)
            {
                TileChunk2D oldChunk = before.Chunks[chunkIndex], chunk = after.Chunks[chunkIndex];
                Assert.Equal((oldChunk.Origin, oldChunk.Width, oldChunk.Height), (chunk.Origin, chunk.Width, chunk.Height));
                for (int index = 0; index < chunk.Tiles.Count; index++)
                {
                    TileCell2D oldCell = oldChunk.Tiles[index], cell = chunk.Tiles[index];
                    Assert.Equal(oldCell.Flip, cell.Flip);
                    if (oldCell.TileId == 0) { Assert.Equal(0, cell.TileId); continue; }
                    Assert.True(original.TryResolveTile(oldCell.TileId, out TileSet2D? oldSet, out TileDefinition2D? oldTile));
                    Assert.True(state.Model.TryResolveTile(cell.TileId, out TileSet2D? set, out TileDefinition2D? tile));
                    Assert.Equal(oldSet!.AtlasResourceId, set!.AtlasResourceId);
                    Assert.Equal(oldTile!.SourceRect, tile!.SourceRect);
                    foreach (TileColliderDescriptor2D collider in tile.Colliders)
                    {
                        Assert.Equal("2", after.Id);
                        Assert.Equal(TileColliderShape2D.Box, collider.Shape);
                        Assert.Equal((2u, 1u), (collider.CollisionLayer, collider.CollisionMask));
                        float x = (chunk.Origin.X + index % chunk.Width) * original.TileSize.Width + after.Offset.X + collider.OffsetX;
                        float y = (chunk.Origin.Y + index / chunk.Width) * original.TileSize.Height + after.Offset.Y + collider.OffsetY;
                        collisionRegions.Add(new(x, y, collider.Width, collider.Height));
                    }
                }
            }
        }
        Assert.NotEmpty(collisionRegions);
        // Half-pixel samples cover every unit cell, including the uncollidable second house.
        for (float y = 0.5f; y < 288; y++)
        for (float x = 0.5f; x < 512; x++)
            Assert.Equal(state.Colliders.Any(r => x >= r.X && x < r.X + r.Width && y >= r.Y && y < r.Y + r.Height),
                collisionRegions.Any(r => x >= r.X && x < r.X + r.Width && y >= r.Y && y < r.Y + r.Height));
    }

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
        Assert.Contains(door, nodes.OfType<TileLayer2D>().Single(l => l.LayerId == "4").LogicalChildren);
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
        Assert.True(PrismAttachment.TryGetInstance(layer, out PrismInstance? buildingPrism));
        PrismFilterState buildingBlur = buildingPrism!.GetLayerState(buildingPrism.Definition.Nodes.Single().Id).Filters.Single();
        Assert.Equal(2f, buildingBlur.GetValue<float>(PrismCatalog.GetFilter(PrismFilterId.Blur).Parameters.Single(p => p.Name == "Radius")));
        Assert.True(PrismAttachment.TryGetInstance(door, out PrismInstance? doorPrism));
        PrismStyleState doorGlow = doorPrism!.GetLayerState(doorPrism.Definition.Nodes.Single().Id).Styles.Single();
        var glowParameters = PrismCatalog.GetStyle(PrismStyleId.OuterGlow).Parameters;
        Assert.Equal(4f, doorGlow.GetValue<float>(glowParameters.Single(p => p.Name == "Size")));
        Assert.Equal(0.8f, doorGlow.GetValue<float>(glowParameters.Single(p => p.Name == "Opacity")));
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
        Assert.Equal(65, map.Model!.Layers.Sum(l => l.Chunks.Count));

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
