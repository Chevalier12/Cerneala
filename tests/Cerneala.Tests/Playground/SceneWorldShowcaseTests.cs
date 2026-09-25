using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Playground;
using Cerneala.Scene2D.Importers;
using Cerneala.Scene2D.Packages;
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
        await using SceneWorldPackage world = await SceneWorldPackage.OpenAsync(
            Path.Combine(AppContext.BaseDirectory, "SceneWorldPackages", ldtk ? "ldtk" : "tiled"),
            CancellationToken.None);
        string path = Path.Combine(AppContext.BaseDirectory, "SceneWorldAuthoring", ldtk ? "village.ldtk" : "village.tmj");
        Scene2DImportResult imported = ldtk ? LdtkScene2DImporter.Import(path) : TiledScene2DImporter.Import(path);
        Assert.True(imported.Success);
        Scene2DLevel level = imported.Document!.Levels.Single();
        Scene2DEntity[] importedWalls = level.Entities.Where(e => e.Role == "Collider").ToArray();
        Assert.Equal(6, importedWalls.Length);
        Assert.Equal(6, world.Walls.Count);
        for (int index = 0; index < world.Walls.Count; index++)
        {
            SceneWorldBox actual = world.Walls[index];
            Scene2DEntity expected = importedWalls[index];
            Assert.Equal((expected.Position.X, expected.Position.Y, expected.Size.Width, expected.Size.Height,
                expected.Collider!.CollisionLayer, expected.Collider.CollisionMask),
                (actual.X, actual.Y, actual.Width, actual.Height, actual.Layer, actual.Mask));
        }
        Assert.Equal(level.TileMaps.Select(map => map.Id), world.TileMapIds);
        await using Scene2DPackage package = await Scene2DPackage.OpenAsync(
            Path.Combine(AppContext.BaseDirectory, "SceneWorldPackages", ldtk ? "ldtk" : "tiled"));
        Scene2DPackageLevel packedLevel = Assert.Single(package.Levels);
        foreach (TileMap2DModel before in level.TileMaps)
        {
            TileMap2DModel after = await packedLevel.LoadMapModelAsync(before.Id);
            Assert.Equal((before.Offset, before.Opacity, before.Tint, before.Order, before.IsVisible),
                (after.Offset, after.Opacity, after.Tint, after.Order, after.IsVisible));
            Assert.Equal(before.TileSize, after.TileSize);
            Assert.Equal(before.Bounds, after.Bounds);
            Assert.Equal(before.Chunks.Sum(chunk => chunk.Tiles.Count), after.Chunks.Sum(chunk => chunk.Tiles.Count));
            Assert.Equal(before.TileSets.Select(set => set.Id), after.TileSets.Select(set => set.Id));
            foreach (TileChunk2D chunk in before.Chunks)
            {
                for (int index = 0; index < chunk.Tiles.Count; index++)
                {
                    TileCoordinate2D coordinate = new(chunk.Origin.X + index % chunk.Width, chunk.Origin.Y + index / chunk.Width);
                    Assert.True(after.TryGetCell(coordinate, out TileCell2D cell));
                    Assert.Equal(chunk.Tiles[index], cell);
                }
            }
        }
        Assert.True(level.TileMaps.Single(map => map.Id == "4").TryGetCell(new(14, 9), out TileCell2D authoredDoor));
        Assert.Equal(7, authoredDoor.TileId);
        TileMap2DModel authoredDoorMap = level.TileMaps.Single(map => map.Id == "4");
        Assert.Equal((authoredDoorMap.TileSize, authoredDoorMap.Bounds, authoredDoorMap.Order,
            authoredDoorMap.IsVisible, authoredDoorMap.Offset, authoredDoorMap.Opacity, authoredDoorMap.Tint),
            (world.DoorModel.TileSize, world.DoorModel.Bounds, world.DoorModel.Order,
                world.DoorModel.IsVisible, world.DoorModel.Offset, world.DoorModel.Opacity, world.DoorModel.Tint));
        Assert.Equal(authoredDoorMap.TileSets.Select(set => set.Id), world.DoorModel.TileSets.Select(set => set.Id));
        Assert.True(world.DoorModel.TryGetCell(new(14, 9), out TileCell2D editedDoor));
        Assert.Equal(0, editedDoor.TileId);
        Assert.Equal((224f, 144f),
            (world.DoorModel.Offset.X + 14 * world.DoorModel.TileSize.Width,
                world.DoorModel.Offset.Y + 9 * world.DoorModel.TileSize.Height));
    }

    [Fact]
    public async Task ConstructionDoesNotOpenAnEditorDocumentAndFailedPackageOpenLeavesStateEmpty()
    {
        await using SceneWorldState state = new(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        Assert.False(state.IsLoaded);
        Assert.Empty(state.TileMapIds);
        Assert.Empty(state.Walls);
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => state.LoadAsync(false));
        Assert.False(state.IsLoaded);
        Assert.Empty(state.TileMapIds);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PlantUsesACompleteEditedModelWithoutChangingGrassOrAuthoredNeighbors(bool ldtk)
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "SceneWorldPackages", ldtk ? "ldtk" : "tiled");
        await using SceneWorldPackage world = await SceneWorldPackage.OpenAsync(directory, CancellationToken.None);
        await using Scene2DPackage package = await Scene2DPackage.OpenAsync(directory);
        Scene2DPackageLevel level = Assert.Single(package.Levels);
        TileMap2DModel ground = await level.LoadMapModelAsync("1");
        TileMap2DModel authored = await level.LoadMapModelAsync("2");
        Assert.True(authored.TryGetCell(new(11, 10), out TileCell2D empty));
        Assert.Equal(0, empty.TileId);

        TileMap2DModel planted = await world.PreparePlantAsync(plant: true, CancellationToken.None);
        Assert.Equal(authored.Version + 1, planted.Version);
        Assert.Equal((authored.TileSize, authored.Bounds, authored.Order, authored.IsVisible,
            authored.Offset, authored.Opacity, authored.Tint),
            (planted.TileSize, planted.Bounds, planted.Order, planted.IsVisible,
                planted.Offset, planted.Opacity, planted.Tint));
        Assert.Equal(authored.TileSets.Select(set => set.Id), planted.TileSets.Select(set => set.Id));
        Assert.True(planted.TryGetCell(new(11, 10), out TileCell2D flower));
        Assert.Equal(15, flower.TileId);
        Assert.True(planted.TryGetCell(new(10, 10), out TileCell2D neighbor));
        Assert.Equal(15, neighbor.TileId);
        Assert.True(ground.TryGetCell(new(11, 10), out TileCell2D grass));
        Assert.Equal(1, grass.TileId);
        world.SetBuildingModelBeforeMaps(planted);
        TileMap2DModel cleared = await world.PreparePlantAsync(plant: false, CancellationToken.None);
        Assert.Equal(planted.Version + 1, cleared.Version);
        Assert.Single(planted.Chunks.Zip(cleared.Chunks).Where(pair => !ReferenceEquals(pair.First, pair.Second)));
        Assert.True(cleared.TryGetCell(new(11, 10), out TileCell2D restored));
        Assert.Equal(0, restored.TileId);
        Assert.True(cleared.TryGetCell(new(10, 10), out neighbor));
        Assert.Equal(15, neighbor.TileId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelledPlantDoesNotAdvanceTheNextSuccessfulModelEdit(bool ldtk)
    {
        await using SceneWorldPackage world = await SceneWorldPackage.OpenAsync(
            Path.Combine(AppContext.BaseDirectory, "SceneWorldPackages", ldtk ? "ldtk" : "tiled"),
            CancellationToken.None);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => world.PreparePlantAsync(plant: true, cancellation.Token));
        TileMap2DModel first = await world.PreparePlantAsync(plant: true, CancellationToken.None);
        Assert.True(first.TryGetCell(new(11, 10), out TileCell2D cell));
        Assert.Equal(15, cell.TileId);
        world.SetBuildingModelBeforeMaps(first);
        for (int iteration = 2; iteration <= 32; iteration++)
        {
            TileMap2DModel next = await world.PreparePlantAsync(plant: iteration % 2 == 1, CancellationToken.None);
            Assert.Equal(first.Version + iteration - 1, next.Version);
            Assert.True(next.TryGetCell(new(11, 10), out cell));
            Assert.Equal(iteration % 2 == 1 ? 15 : 0, cell.TileId);
            Assert.Equal(first.TileSets.Select(set => set.Id), next.TileSets.Select(set => set.Id));
            world.SetBuildingModelBeforeMaps(next);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PlantStateSurvivesPlayerResetButFormatReloadDiscardsIt(bool ldtk)
    {
        await using SceneWorldState state = new();
        await state.LoadAsync(ldtk);
        SceneWorldNpc npc = Assert.Single(state.Npcs);
        await state.PlantAsync();
        Assert.Contains("complete decorative map", state.Status);
        state.PlayerX = 999;
        state.ResetPlayer();
        Assert.Equal(state.Spawn.X, state.PlayerX);
        await state.LoadAsync(!ldtk);
        Assert.Same(npc, Assert.Single(state.Npcs));
        Assert.True(state.DoorClosed);
        Assert.Equal("Idle", state.PlayerState);
        Assert.Equal(new[] { "1", "2", "4", "3" }, state.TileMapIds);
    }

    [Fact]
    public async Task DisposingTheStateClosesItsPackageAndRejectsFutureLoads()
    {
        SceneWorldState state = new();
        await state.LoadAsync(false);
        Assert.Equal(new[] { "1", "2", "4", "3" }, state.TileMapIds);
        await state.DisposeAsync();
        await state.DisposeAsync();
        Assert.False(state.IsLoaded);
        Assert.Empty(state.TileMapIds);
        Assert.Empty(state.Walls);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => state.LoadAsync(true));
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
        TileMap2D map = view.State.GroundMap!;
        TileMap2D layer = view.State.BuildingMap!;
        Scene2D buildingLayer = Assert.IsType<Scene2D>(layer.LogicalParent);
        Sprite2D door = nodes.OfType<Sprite2D>().Single(sprite => ServoApi.GetId(sprite) == "world-door");
        Scene2D mapGroup = Assert.IsType<Scene2D>(door.LogicalParent);
        Assert.Contains(Assert.IsType<Scene2D>(map.LogicalParent), mapGroup.Children);
        Assert.Contains(buildingLayer, mapGroup.Children);
        Assert.Contains(Assert.IsType<Scene2D>(view.State.DoorMap!.LogicalParent), mapGroup.Children);
        Assert.Equal(3, nodes.OfType<TileMap2D>().Count());
        BoxCollider2D doorCollider = door.LogicalChildren.OfType<BoxCollider2D>().Single();
        SceneItems2D importedColliders = nodes.OfType<SceneItems2D>()
            .Single(n => ReferenceEquals(n.ItemsSource, view.State.Walls));
        Sprite2D[] wallOwners = Descendants(importedColliders).OfType<Sprite2D>().ToArray();
        Assert.Equal(6, importedColliders.RealizedItemCount);
        Assert.Equal(6, wallOwners.Length);
        foreach (SceneWorldBox wall in view.State.Walls)
        {
            // Value equality is checked above; the UI checks normal collection composition.
            Assert.Contains(wallOwners, owner => ReferenceEquals(owner.DataContext, wall));
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
        SceneItems2D npcs = nodes.OfType<SceneItems2D>().Single(n => ReferenceEquals(n.ItemsSource, view.State.Npcs));
        Sprite2D npcSprite = Descendants(npcs).OfType<Sprite2D>().Single();
        Scene2DDebugOverlay overlay = nodes.OfType<Scene2DDebugOverlay>().Single();
        UIElement[] visualMatrix = [world, mapGroup, buildingLayer, door, player, playerSprite, npcSprite, overlay];
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
        Assert.True(PrismAttachment.TryGetInstance(buildingLayer, out PrismInstance? buildingPrism));
        PrismFilterState buildingBlur = buildingPrism!.GetLayerState(buildingPrism.Definition.Nodes.Single().Id).Filters.Single();
        Assert.Equal(2f, buildingBlur.GetValue<float>(PrismCatalog.GetFilter(PrismFilterId.Blur).Parameters.Single(p => p.Name == "Radius")));
        Assert.True(PrismAttachment.TryGetInstance(door, out PrismInstance? doorPrism));
        PrismStyleState doorGlow = doorPrism!.GetLayerState(doorPrism.Definition.Nodes.Single().Id).Styles.Single();
        var glowParameters = PrismCatalog.GetStyle(PrismStyleId.OuterGlow).Parameters;
        Assert.Equal(4f, doorGlow.GetValue<float>(glowParameters.Single(p => p.Name == "Size")));
        Assert.Equal(0.8f, doorGlow.GetValue<float>(glowParameters.Single(p => p.Name == "Opacity")));
        UIElement[] loadedFades = [world, mapGroup, buildingLayer, player, playerSprite, npcSprite, overlay];
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
        // The edited Door is FromModel rather than the package's subdivided
        // source, so the old aggregate 65-chunk identity is no longer a contract.
        Assert.True(root.Detective.CaptureTileMap(map).TotalChunks > 0);
        Assert.True(root.Detective.CaptureTileMap(layer).TotalChunks > 0);
        Assert.True(root.Detective.CaptureTileMap(view.State.DoorMap!).TotalChunks > 0);

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

        TileMap2D original = view.State.GroundMap!;
        var collisionBefore = world.CollisionWorld.GetDiagnosticsSnapshot();
        ServoElement pickingBefore = await servo.FindAsync(ServoTarget.ById("world-player"));
        await servo.ClickAsync(ServoTarget.ById("world-debug"));
        Assert.Equal(8, Record().Count(c => c.Kind == DrawCommandKind.BeginPrism));
        Assert.True(overlay.GetDiagnosticsSnapshot().Primitives > 0);
        Assert.Same(original, view.State.GroundMap);
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
        Assert.Equal(new[] { "1", "2", "4", "3" }, view.State.TileMapIds);
        TileMap2D beforePlant = view.State.BuildingMap!;
        TileMap2D groundBeforePlant = view.State.GroundMap!;
        await servo.ClickAsync(ServoTarget.ById("world-mutate"));
        Ready();
        Assert.NotSame(beforePlant, view.State.BuildingMap);
        Assert.Same(groundBeforePlant, view.State.GroundMap);
        Assert.Same(view.State.BuildingMap, Assert.Single(buildingLayer.Children));
        Assert.Null(beforePlant.LogicalParent);
        await servo.ClickAsync(ServoTarget.ById("world-reset"));
        Ready();
        Assert.Equal(view.State.Spawn.X, view.State.PlayerX);
        Assert.Equal(view.State.Spawn.Y, view.State.PlayerY);
        Assert.Null(view.LastMove);
        root.VisualChildren.Remove(view);
        Assert.False(view.State.IsLoaded);
        Assert.Empty(view.State.TileMapIds);
        Assert.Null(surface.Scene);
        await view.State.DisposeAsync();
    }

    [Fact]
    public async Task DetachingDuringAnAsynchronousOpenRetiresTheOperationWithoutPumpingTheOldRootAndAllowsReattach()
    {
        UIRoot first = new(800, 600);
        first.SetImageLoader(new AtlasLoader());
        SceneWorldShowcase view = new();
        first.VisualChildren.Add(view);
        Task abandoned = view.PendingOperation;
        first.VisualChildren.Remove(view);
        Assert.True(SpinWait.SpinUntil(() => abandoned.IsCompleted, TimeSpan.FromSeconds(10)));
        Assert.False(view.State.IsLoaded);
        Assert.Empty(view.State.TileMapIds);

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
        await view.State.DisposeAsync();
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
