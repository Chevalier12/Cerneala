using System.Runtime.CompilerServices;
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
using Cerneala.UI.Platform;
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
    public async Task ImportedWallOwnersPreserveTileDrawingAndTheSixAuthoredCollisionRegions(bool ldtk)
    {
        using SceneWorldState state = new();
        await state.LoadAsync(ldtk);
        string path = Path.Combine(AppContext.BaseDirectory, "SceneWorldAuthoring", ldtk ? "village.ldtk" : "village.tmj");
        Scene2DImportResult imported = ldtk ? LdtkScene2DImporter.Import(path) : TiledScene2DImporter.Import(path);
        Assert.True(imported.Success);
        Scene2DLevel level = imported.Document!.Levels.Single();
        Scene2DEntity[] importedWalls = level.Entities.Where(e => e.Role == "Collider").ToArray();
        Assert.Equal(6, importedWalls.Length);
        Assert.Equal(6, state.ColliderSource!.Entries.Count);
        foreach (var entry in state.ColliderSource.Entries)
        {
            using var lease = await state.ColliderSource.LoadAsync(entry);
            SceneWorldBox actual = Assert.IsType<SceneWorldBox>(lease.Value);
            Scene2DEntity expected = importedWalls.Single(wall => wall.Id == entry.Id);
            Assert.Equal((expected.Position.X, expected.Position.Y, expected.Size.Width, expected.Size.Height,
                expected.Collider!.CollisionLayer, expected.Collider.CollisionMask),
                (actual.X, actual.Y, actual.Width, actual.Height, actual.Layer, actual.Mask));
        }
        Assert.Equal(level.TileMaps.Count, state.TileMaps.Count);
        Assert.Equal(ldtk ? 24 : 65, state.TileMaps.Sum(map => map.Catalog.Chunks.Count));
        foreach (TileMap2DModel before in level.TileMaps)
        {
            TileMapSource2D after = state.TileMaps.Single(map => map.Catalog.Id == before.Id);
            TileMapCatalog2D catalog = after.Catalog;
            Assert.Equal((before.Offset, before.Opacity, before.Tint, before.Order, before.IsVisible),
                (catalog.Offset, catalog.Opacity, catalog.Tint, catalog.Order, catalog.IsVisible));
            Assert.Equal(before.Chunks.Sum(chunk => chunk.Tiles.Count), catalog.Chunks.Sum(info => info.TileCount));
            foreach (TileMapChunkInfo2D info in catalog.Chunks)
            {
                Assert.InRange(info.Cells!.Value.Width, 1, 16);
                Assert.InRange(info.Cells.Value.Height, 1, 16);
                using var lease = await after.LoadAsync(info.Spatial);
                TileMapChunkData2D data = lease.Value;
                TileChunk2D chunk = data.Grid!;
                for (int index = 0; index < chunk.Tiles.Count; index++)
                {
                    TileCell2D cell = chunk.Tiles[index];
                    TileCoordinate2D coordinate = new(chunk.Origin.X + index % chunk.Width, chunk.Origin.Y + index / chunk.Width);
                    Assert.True(before.TryGetCell(coordinate, out TileCell2D oldCell));
                    if (before.Id == "4" && coordinate == new TileCoordinate2D(14, 9))
                    {
                        Assert.Equal(7, oldCell.TileId);
                        Assert.Equal(0, cell.TileId);
                        Assert.Equal((224f, 144f), (state.DoorX, state.DoorY));
                        continue;
                    }
                    Assert.Equal(oldCell, cell);
                    if (oldCell.TileId == 0) continue;
                    Assert.True(before.TryResolveTile(oldCell.TileId, out TileSet2D? oldSet, out TileDefinition2D? oldTile));
                    Assert.True(data.TryResolveTile(cell.TileId, out TileSet2D? set, out TileDefinition2D? tile));
                    Assert.Equal(oldSet!.AtlasResourceId, set!.AtlasResourceId);
                    Assert.Equal(oldTile!.SourceRect, tile!.SourceRect);
                    Assert.Null(oldTile.Collider);
                    Assert.Null(tile.Collider);
                }
            }
        }
    }

    [Fact]
    public async Task ConstructionDoesNotOpenAnEditorDocumentAndFailedPackageOpenLeavesStateEmpty()
    {
        using SceneWorldState state = new(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        Assert.False(state.IsLoaded);
        Assert.Empty(state.TileMaps);
        Assert.Null(state.ColliderSource);
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => state.LoadAsync(false));
        Assert.False(state.IsLoaded);
        Assert.Empty(state.TileMaps);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PlantPreservesGrassAndOnlyChangesTheDecorativeChunk(bool ldtk)
    {
        using SceneWorldState state = new();
        await state.LoadAsync(ldtk);
        TileMapCatalog2D ground = state.GroundSource!.Catalog;
        TileMapSource2D decorations = state.BuildingSource!;
        TileMapCatalog2D before = decorations.Catalog;
        TileMapChunkInfo2D target = before.Chunks.Single(info => info.Cells!.Value.Contains(new(11, 10)));
        using (var baseline = await decorations.LoadAsync(target.Spatial))
            Assert.Equal(0, baseline.Value.Grid!.GetCell(new(11, 10)).TileId);

        await state.PlantAsync();

        Assert.Same(ground, state.GroundSource.Catalog);
        Assert.Single(before.Chunks.Zip(decorations.Catalog.Chunks).Where(pair => !ReferenceEquals(pair.First, pair.Second)));
        TileMapChunkInfo2D revision = decorations.Catalog.Chunks.Single(info => info.Spatial.Id == target.Spatial.Id);
        using var planted = await decorations.LoadAsync(revision.Spatial);
        Assert.Equal(15, planted.Value.Grid!.GetCell(new(11, 10)).TileId);
        Assert.Equal(15, planted.Value.Grid.GetCell(new(10, 10)).TileId); // Preserve the authored neighboring flower.
        TileMapChunkInfo2D terrain = ground.Chunks.Single(info => info.Cells!.Value.Contains(new(11, 10)));
        using var grass = await state.GroundSource.LoadAsync(terrain.Spatial);
        Assert.Equal(1, grass.Value.Grid!.GetCell(new(11, 10)).TileId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PlantPreparationSharesOnlyThePublishingAcquisitionAndDoesNotKeepBackingPayloads(bool ldtk)
    {
        using SceneWorldState state = new();
        await state.LoadAsync(ldtk);
        WeakReference[] payloads = await PublishPlantAndRelease(state);
        await Task.Yield();
        for (int collection = 0; collection < 3; collection++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
        Assert.All(payloads, reference => Assert.False(reference.IsAlive));
        GC.KeepAlive(state);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference[]> PublishPlantAndRelease(SceneWorldState state)
    {
        TileMapSource2D source = state.BuildingSource!;
        ValueTask<SceneSpatialLease2D<TileMapChunkData2D>> acquisition = default;
        bool changed = false, immediatelyReady = false;
        void OnChanged(object? sender, EventArgs args)
        {
            changed = true;
            TileMapChunkInfo2D info = source.Catalog.Chunks.Single(info => info.Cells!.Value.Contains(new(11, 10)));
            acquisition = source.LoadAsync(info.Spatial);
            immediatelyReady = acquisition.IsCompletedSuccessfully;
        }
        source.Changed += OnChanged;
        try
        {
            await state.PlantAsync();
            Assert.True(changed);
            using var published = await acquisition;
            Assert.True(immediatelyReady, "The published edit must be available synchronously to current scene interests.");
            Assert.Equal(15, published.Value.Grid!.GetCell(new(11, 10)).TileId);
            TileMapChunkInfo2D info = source.Catalog.Chunks.Single(info => info.Cells!.Value.Contains(new(11, 10)));
            using var reloaded = await source.LoadAsync(info.Spatial);
            Assert.NotSame(published.Value, reloaded.Value); // No persistent staging cache after publication.
            Assert.Equal(published.Value.Grid.Tiles, reloaded.Value.Grid!.Tiles);
            return [new(published.Value), new(published.Value.Grid), new(published.Value.TileSets), new(reloaded.Value)];
        }
        finally { source.Changed -= OnChanged; }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelledPlantDoesNotPublishOrToggleTheNextSuccessfulEdit(bool ldtk)
    {
        using SceneWorldState state = new();
        await state.LoadAsync(ldtk);
        TileMapCatalog2D before = state.BuildingSource!.Catalog;
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => state.PlantAsync(cancellation.Token));
        Assert.Same(before, state.BuildingSource.Catalog);
        await state.PlantAsync();
        TileMapChunkInfo2D info = state.BuildingSource.Catalog.Chunks.Single(info => info.Cells!.Value.Contains(new(11, 10)));
        _ = await AcquirePlantAndRelease(state.BuildingSource, info, 15);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PlantDeltaSurvivesReleaseAndPlayerResetButFormatReloadDiscardsIt(bool ldtk)
    {
        using SceneWorldState state = new();
        await state.LoadAsync(ldtk);
        SceneWorldNpc npc = Assert.Single(state.Npcs);
        TileMapSource2D source = state.BuildingSource!;
        TileMapCatalog2D baseline = source.Catalog;
        TileMapChunkInfo2D edited = baseline.Chunks.Single(info => info.Cells!.Value.Contains(new(11, 10)));
        List<WeakReference> payloads = [];
        for (int iteration = 1; iteration <= 32; iteration++)
        {
            await state.PlantAsync();
            Assert.Same(source, state.BuildingSource);
            TileMapChunkInfo2D revision = source.Catalog.Chunks.Single(info => info.Spatial.Id == edited.Spatial.Id);
            Assert.Equal(edited.Spatial.Version + iteration, revision.Spatial.Version);
            Assert.Null(revision.DataResidencyBytes); // Unknown is not a zero-byte warm-cache bypass.
            foreach (TileMapChunkInfo2D unchanged in baseline.Chunks.Where(info => !ReferenceEquals(info, edited)))
                Assert.Same(unchanged, source.Catalog.Chunks.Single(info => info.Spatial.Id == unchanged.Spatial.Id));
            payloads.AddRange(await AcquirePlantAndRelease(source, revision, iteration % 2 == 1 ? 15 : 0));
            state.PlayerX = 999;
            state.ResetPlayer();
            Assert.Equal(state.Spawn.X, state.PlayerX);
            payloads.AddRange(await AcquirePlantAndRelease(source, revision, iteration % 2 == 1 ? 15 : 0));
        }
        await Task.Yield();
        for (int collection = 0; collection < 3; collection++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
        Assert.All(payloads, reference => Assert.False(reference.IsAlive));
        await state.PlantAsync();
        await state.LoadAsync(!ldtk);
        Assert.Same(npc, Assert.Single(state.Npcs));
        Assert.True(state.DoorClosed);
        Assert.Equal("Idle", state.PlayerState);
        TileMapSource2D reloaded = state.BuildingSource!;
        TileMapChunkInfo2D fresh = reloaded.Catalog.Chunks.Single(info => info.Cells!.Value.Contains(new(11, 10)));
        _ = await AcquirePlantAndRelease(reloaded, fresh, 0);
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await source.LoadAsync(source.Entries[0]));
        GC.KeepAlive(state);
        GC.KeepAlive(source);
    }

    [Fact]
    public async Task DisposingTheStateClosesPackageSourcesWithoutKeepingACompleteBackingMap()
    {
        SceneWorldState state = new();
        await state.LoadAsync(false);
        TileMapSource2D source = state.GroundSource!;
        var walls = state.ColliderSource!;
        state.Dispose();
        state.Dispose();
        Assert.False(state.IsLoaded);
        Assert.Empty(state.TileMaps);
        Assert.Null(state.ColliderSource);
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await source.LoadAsync(source.Entries[0]));
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await walls.LoadAsync(walls.Entries[0]));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => state.LoadAsync(true));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference[]> AcquirePlantAndRelease(TileMapSource2D source, TileMapChunkInfo2D info, int expected)
    {
        using var lease = await source.LoadAsync(info.Spatial);
        TileMapChunkData2D data = lease.Value;
        Assert.Equal(expected, data.Grid!.GetCell(new(11, 10)).TileId);
        Assert.Equal(data.Grid.Tiles.Where(cell => cell.TileId != 0).Select(cell => cell.TileId).Distinct().Order(),
            data.TileSets.SelectMany(set => set.Tiles).Select(tile => tile.Id).Order());
        return [new(data), new(data.Grid), new(data.TileSets)];
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompiledWorldMarkupRunsItsEffectsAnimationsAndInputContracts(bool platformCursor)
    {
        ManualMotionClock clock = new();
        UIRoot root = new(800, 600, motionClock: clock);
        root.SetImageLoader(new AtlasLoader());
        SceneWorldShowcase view = new();
        root.VisualChildren.Add(view);
        TestCursor cursor = new();
        UiHost host = new(new UiHostOptions
        {
            Root = root, Viewport = new UiViewport(800, 600),
            PlatformServices = platformCursor ? new PlatformServices(Cursor: cursor) : null
        });
        ServoApi servo = new(host);
        void Frame(int milliseconds)
        {
            clock.Advance(TimeSpan.FromMilliseconds(milliseconds));
            host.Update(new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty,
                KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []), host.Viewport, TimeSpan.FromMilliseconds(milliseconds));
        }
        Frame(0);
        RenderSurface2D surface = Descendants(view).OfType<RenderSurface2D>().Single();
        void Ready()
        {
            Assert.True(SpinWait.SpinUntil(() =>
            {
                Frame(0);
                Assert.Null(view.OperationError);
                Assert.Null(surface.PresentationError);
                return view.PendingOperation.IsCompleted && view.State.IsLoaded && !view.State.IsBusy &&
                    surface.PresentationState == RenderSurface2DPresentationState.Ready;
            }, TimeSpan.FromSeconds(10)), view.State.Status);
            Assert.True(view.PendingOperation.IsCompletedSuccessfully);
        }
        Ready();
        Scene2D world = surface.Scene!;
        UIElement[] nodes = Descendants(world).Prepend(world).ToArray();
        TileMap2D map = nodes.OfType<TileMap2D>().Single(map => map.Source!.Catalog.Id == "1");
        TileMap2D layer = nodes.OfType<TileMap2D>().Single(map => map.Source!.Catalog.Id == "2");
        Sprite2D door = nodes.OfType<Sprite2D>().Single(sprite => ServoApi.GetId(sprite) == "world-door");
        Scene2D mapGroup = Assert.IsType<Scene2D>(door.LogicalParent);
        Assert.Contains(map, mapGroup.Children);
        Assert.Contains(layer, mapGroup.Children);
        Assert.Equal(3, nodes.OfType<TileMap2D>().Count());
        BoxCollider2D doorCollider = door.LogicalChildren.OfType<BoxCollider2D>().Single();
        SceneItems2D importedColliders = nodes.OfType<SceneItems2D>()
            .Single(n => ReferenceEquals(n.ItemsSource, view.State.ColliderSource));
        Sprite2D[] wallOwners = Descendants(importedColliders).OfType<Sprite2D>().ToArray();
        Assert.Equal(6, importedColliders.RealizedItemCount);
        Assert.Equal(6, wallOwners.Length);
        foreach (var entry in view.State.ColliderSource!.Entries)
        {
            // Payload equality is checked above; the UI test checks composed singular owners.
            Assert.True(importedColliders.TryGetRealizedNode(entry.Id, out SceneNode2D? node));
            Assert.Contains(Assert.IsType<Sprite2D>(node), wallOwners);
        }
        Assert.All(wallOwners, owner =>
            {
                BoxCollider2D collider = Assert.IsType<BoxCollider2D>(owner.Collider);
                Assert.Single(owner.LogicalChildren);
                Assert.Same(collider, owner.LogicalChildren.Single());
                SceneWorldBox wall = Assert.IsType<SceneWorldBox>(owner.DataContext);
                Assert.Equal((wall.X, wall.Y, wall.Width, wall.Height, wall.Layer, wall.Mask),
                    (owner.X, owner.Y, collider.Width, collider.Height, collider.CollisionLayer, collider.CollisionMask));
            });
        Scene2D player = nodes.OfType<Scene2D>().Single(n => ServoApi.GetId(n) == "world-player");
        Sprite2D playerSprite = player.Children.OfType<Sprite2D>().Single();
        SceneItems2D npcs = nodes.OfType<SceneItems2D>().Single(n => ReferenceEquals(n.ItemsSource, view.State.NpcSource));
        Sprite2D npcSprite = Descendants(npcs).OfType<Sprite2D>().Single();
        Scene2DDebugOverlay overlay = nodes.OfType<Scene2DDebugOverlay>().Single();
        UIElement[] visualMatrix = [world, mapGroup, layer, door, player, playerSprite, npcSprite, overlay];
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
        UIElement[] loadedFades = [world, mapGroup, layer, player, playerSprite, npcSprite, overlay];
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
        Assert.Equal(65, nodes.OfType<TileMap2D>().Sum(map => map.Source!.Catalog.Chunks.Count));

        await servo.ClickAsync(ServoTarget.ById("world-player"));
        await servo.PressKeyAsync(InputKey.Up);
        Ready();
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

        TileMapCatalog2D original = map.Source!.Catalog;
        var collisionBefore = world.CollisionWorld.GetDiagnosticsSnapshot();
        ServoElement pickingBefore = await servo.FindAsync(ServoTarget.ById("world-player"));
        await servo.ClickAsync(ServoTarget.ById("world-debug"));
        Assert.Equal(8, Record().Count(c => c.Kind == DrawCommandKind.BeginPrism));
        Assert.True(overlay.GetDiagnosticsSnapshot().Primitives > 0);
        Assert.Same(original, map.Source!.Catalog);
        var collisionAfter = world.CollisionWorld.GetDiagnosticsSnapshot();
        Assert.Equal(collisionBefore.EntryCount, collisionAfter.EntryCount);
        Assert.Equal(collisionBefore.RebuildCount, collisionAfter.RebuildCount);
        Assert.Equal(collisionBefore.IncrementalUpdateCount, collisionAfter.IncrementalUpdateCount);
        Assert.Equal(pickingBefore.Bounds, (await servo.FindAsync(ServoTarget.ById("world-player"))).Bounds);
        UIElement firstNpc = npcs.LogicalChildren[0];
        await servo.ClickAsync(ServoTarget.ById("world-add"));
        Assert.Equal(2, npcs.RealizedItemCount);
        Assert.Same(firstNpc, npcs.LogicalChildren[0]);
        SceneWorldNpc firstModel = view.State.Npcs[0];
        await servo.ClickAsync(ServoTarget.ById("world-format"));
        Ready();
        Assert.True(view.State.IsLdtk);
        Assert.Same(firstModel, view.State.Npcs[0]);
        Assert.Same(firstNpc, npcs.LogicalChildren[0]);
        Assert.Equal(24, view.State.TileMaps.Sum(source => source.Catalog.Chunks.Count));
        TileMapCatalog2D beforePlant = layer.Source!.Catalog;
        await servo.ClickAsync(ServoTarget.ById("world-mutate"));
        Ready();
        Assert.Single(beforePlant.Chunks.Zip(layer.Source.Catalog.Chunks).Where(pair => !ReferenceEquals(pair.First, pair.Second)));
        await servo.ClickAsync(ServoTarget.ById("world-reset"));
        Ready();
        Assert.Equal(view.State.Spawn.X, view.State.PlayerX);
        Assert.Equal(view.State.Spawn.Y, view.State.PlayerY);
        Assert.Null(view.LastMove);
        root.VisualChildren.Remove(view);
        Assert.False(view.State.IsLoaded);
        Assert.Empty(view.State.TileMaps);
        Assert.Null(surface.Scene);
    }

    [Fact]
    public void DetachingDuringAnAsynchronousOpenRetiresTheOperationWithoutPumpingTheOldRootAndAllowsReattach()
    {
        UIRoot first = new(800, 600);
        first.SetImageLoader(new AtlasLoader());
        SceneWorldShowcase view = new();
        first.VisualChildren.Add(view);
        Task abandoned = view.PendingOperation;
        first.VisualChildren.Remove(view);
        Assert.True(SpinWait.SpinUntil(() => abandoned.IsCompleted, TimeSpan.FromSeconds(10)));
        Assert.False(view.State.IsLoaded);
        Assert.Empty(view.State.TileMaps);

        UIRoot second = new(800, 600);
        second.SetImageLoader(new AtlasLoader());
        second.VisualChildren.Add(view);
        UiHost host = new(new UiHostOptions { Root = second, Viewport = new UiViewport(800, 600) });
        RenderSurface2D surface = Descendants(view).OfType<RenderSurface2D>().Single();
        Assert.True(SpinWait.SpinUntil(() =>
        {
            host.Update(new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty,
                KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []), host.Viewport, TimeSpan.Zero);
            Assert.Null(view.OperationError);
            return view.State.IsLoaded && !view.State.IsBusy && view.PendingOperation.IsCompleted &&
                surface.PresentationState == RenderSurface2DPresentationState.Ready;
        }, TimeSpan.FromSeconds(10)));
        Assert.NotNull(surface.Scene);
        second.VisualChildren.Remove(view);
        Assert.False(view.State.IsLoaded);
    }

    private static IEnumerable<UIElement> Descendants(UIElement element)
    {
        foreach (UIElement child in element.VisualChildren.Concat(element.LogicalChildren).Distinct())
        {
            yield return child;
            foreach (UIElement descendant in Descendants(child)) yield return descendant;
        }
    }

    private sealed class TestCursor : ICursorService
    {
        public CursorShape Current { get; private set; }
        public void SetCursor(CursorShape shape) => Current = shape;
    }

    private sealed class AtlasLoader : IAsyncImageLoader
    {
        public IDrawImage Load(string path) => new Atlas();
        public ValueTask<IDrawImage> LoadAsync(string path, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Load(path));
    }
    private sealed class Atlas : IDrawImage
    {
        public int Width => 128;
        public int Height => 32;
    }
}
