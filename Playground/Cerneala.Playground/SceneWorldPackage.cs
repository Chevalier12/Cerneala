using Cerneala.Drawing;
using Cerneala.Scene2D.Packages;
using Cerneala.UI.Controls;
using Cerneala.UI.Resources;

namespace Cerneala.Playground;

// Application composition, not a general mutable-map API. The package owns
// streaming Ground/Buildings nodes; only the two visible village edits use a
// complete application-owned model and a replacement node.
internal sealed class SceneWorldPackage : IAsyncDisposable
{
    private static readonly TileCoordinate2D PlantCell = new(11, 10);
    private static readonly TileCoordinate2D DoorCell = new(14, 9);

    private readonly Scene2DPackage package;
    private readonly Scene2DPackageLevel level;
    private readonly IReadOnlyDictionary<string, DrawSize> imageSizes;
    private readonly List<Task> retiredMapDrains = [];
    private TileMap2DModel? buildingModel;
    private Task? disposal;

    private SceneWorldPackage(Scene2DPackage package, Scene2DPackageLevel level,
        TileMap2DModel doorModel, IReadOnlyDictionary<string, DrawSize> imageSizes,
        SceneWorldBox[] walls, DrawPoint spawn, string spawnState, bool doorClosed)
    {
        this.package = package;
        this.level = level;
        this.imageSizes = imageSizes;
        DoorModel = doorModel;
        Walls = Array.AsReadOnly(walls);
        Spawn = spawn;
        SpawnState = spawnState;
        DoorClosed = doorClosed;
        Atlas = new(package.GetFilePath(package.Assets.Single(asset => asset.ResourceId.Key == "world-atlas.png").Path));
    }

    internal IReadOnlyList<string> TileMapIds => level.TileMapIds;
    internal TileMap2D? GroundMap { get; private set; }
    internal TileMap2D? BuildingMap { get; private set; }
    internal TileMap2D? DoorMap { get; private set; }
    internal bool HasMaps => GroundMap is not null || BuildingMap is not null || DoorMap is not null;
    internal TileMap2DModel DoorModel { get; }
    internal IReadOnlyList<SceneWorldBox> Walls { get; }
    internal ImageResource Atlas { get; }
    internal DrawPoint Spawn { get; }
    internal string SpawnState { get; }
    internal bool DoorClosed { get; }

    internal static async Task<SceneWorldPackage> OpenAsync(string path, CancellationToken token)
    {
        Scene2DPackage package = await Scene2DPackage.OpenAsync(path, cancellationToken: token).ConfigureAwait(false);
        try
        {
            Scene2DPackageLevel level = package.Levels.Single();
            // The package preserves the writer's map order, which also includes
            // the empty Objects layer (3). Composition selects the three maps
            // it renders by identity rather than imposing a sorted ID sequence.
            if (level.WorldOffset != default ||
                new[] { "1", "2", "4" }.Any(id => !level.TileMapIds.Contains(id, StringComparer.Ordinal)))
            { throw new InvalidDataException("The village requires zero level offset and authored Ground, Buildings and Doors maps."); }
            if (level.PromotionCells.Single() != new TileCellKey2D("4", DoorCell))
            { throw new InvalidDataException("The authored door declaration requires cell (4,14,9)."); }

            // Door promotion is visible from the first presented frame. Unlike
            // Ground and Buildings, this one authored map must be loaded whole
            // immediately so the underlying tile can be hidden by an app edit.
            TileMap2DModel authoredDoor = await level.LoadMapModelAsync("4", token).ConfigureAwait(false);
            if (!authoredDoor.TryGetCell(DoorCell, out TileCell2D doorCell) || doorCell.TileId != 7 ||
                !authoredDoor.TryResolveTile(doorCell.TileId, out _, out TileDefinition2D? doorTile) ||
                doorTile!.Collider is not null)
            { throw new InvalidDataException("The authored door cell must be a decorative tile 7."); }
            TileMap2DModel doorModel = ReplaceCell(authoredDoor, DoorCell, 0);

            Scene2DEntity spawn = await level.LoadEntityAsync(level.Entities.Single(info => info.Role == "Spawn").Id, token)
                .ConfigureAwait(false);
            TilePromotion2D promotion = await level.LoadPromotionAsync(level.PromotionCells[0], token).ConfigureAwait(false);
            Scene2DPackageEntityInfo[] wallHeaders = level.Entities.Where(entity => entity.Role == "Collider").ToArray();
            if (wallHeaders.Length != 6) { throw new InvalidDataException("The village requires its six authored wall regions."); }
            SceneWorldBox[] walls = new SceneWorldBox[wallHeaders.Length];
            for (int index = 0; index < wallHeaders.Length; index++)
            {
                Scene2DEntity entity = await level.LoadEntityAsync(wallHeaders[index].Id, token).ConfigureAwait(false);
                if (entity.Shape != "Box" || entity.Rotation != 0 ||
                    entity.Collider is not { Shape: TileColliderShape2D.Box } collider)
                { throw new InvalidDataException("This sample composes axis-aligned box walls only."); }
                // Copy gameplay fields, not the authored Properties/vertices.
                walls[index] = new(entity.Position.X, entity.Position.Y, entity.Size.Width, entity.Size.Height,
                    collider.CollisionLayer, collider.CollisionMask);
            }
            token.ThrowIfCancellationRequested();
            return new(package, level, doorModel,
                package.Assets.ToDictionary(asset => asset.ResourceId.Key, asset => asset.Size, StringComparer.Ordinal),
                walls, spawn.Position, (string)spawn.Properties["InitialState"]!,
                Equals(promotion.Properties["InitialState"], "Closed"));
        }
        catch
        {
            await package.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    // Called on the scene owner thread, immediately before the world is
    // published. A plain state with no UI does not need any map nodes.
    internal void CreateMaps()
    {
        ObjectDisposedException.ThrowIf(disposal is not null, this);
        if (GroundMap is not null) { throw new InvalidOperationException("Village maps are already active."); }
        GroundMap = level.CreateTileMap("1");
        BuildingMap = buildingModel is null
            ? level.CreateTileMap("2")
            : TileMap2D.FromModel(buildingModel, imageSizes);
        DoorMap = TileMap2D.FromModel(DoorModel, imageSizes);
    }

    internal async Task<TileMap2DModel> PreparePlantAsync(bool plant, CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(disposal is not null, this);
        TileMap2DModel before = buildingModel ?? await level.LoadMapModelAsync("2", token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        if (!before.TryGetCell(PlantCell, out TileCell2D cell) || cell.TileId != (plant ? 0 : 15) ||
            !before.TryResolveTile(15, out _, out TileDefinition2D? flower) || flower!.Collider is not null)
        { throw new InvalidDataException("The authored Plant cell or its noncolliding flower definition changed."); }
        return ReplaceCell(before, PlantCell, plant ? 15 : 0);
    }

    internal TileMap2D ReplaceBuildingMap(TileMap2DModel edited)
    {
        ObjectDisposedException.ThrowIf(disposal is not null, this);
        TileMap2D replacement = TileMap2D.FromModel(edited, imageSizes);
        TileMap2D previous = BuildingMap ?? throw new InvalidOperationException("Village maps are not active.");
        BuildingMap = replacement;
        buildingModel = edited;
        return previous;
    }

    internal Task RetireMapAfterDetachAsync(TileMap2D map)
    {
        Task drain = map.DisposeAsync().AsTask();
        retiredMapDrains.Add(drain);
        return drain;
    }

    internal void SetBuildingModelBeforeMaps(TileMap2DModel edited)
    {
        if (BuildingMap is not null) { throw new InvalidOperationException("Building map is already active."); }
        buildingModel = edited;
    }

    internal Task DisposeAfterDetachAsync()
    {
        if (disposal is not null) { return disposal; }
        TileMap2D[] maps = new[] { GroundMap, BuildingMap, DoorMap }.OfType<TileMap2D>().ToArray();
        if (maps.Any(map => map.LogicalParent is not null || map.VisualParent is not null || map.Root is not null))
        { throw new InvalidOperationException("Detach village maps before disposing their package."); }
        // Invoke every map's terminal operation on its owner thread before the
        // first await. The package reader closes only after every map drain.
        Task[] drains = retiredMapDrains.Concat(maps.Select(map => map.DisposeAsync().AsTask())).ToArray();
        disposal = CompleteDisposalAsync(drains);
        return disposal;
    }

    public ValueTask DisposeAsync() => new(DisposeAfterDetachAsync());

    private async Task CompleteDisposalAsync(Task[] drains)
    {
        List<Exception> failures = [];
        foreach (Task drain in drains)
        {
            try { await drain.ConfigureAwait(false); }
            catch (Exception error) { failures.Add(error); }
        }
        try { await package.DisposeAsync().ConfigureAwait(false); }
        catch (Exception error) { failures.Add(error); }
        if (failures.Count != 0) { throw new AggregateException(failures); }
    }

    private static TileMap2DModel ReplaceCell(TileMap2DModel before, TileCoordinate2D coordinate, int tileId)
    {
        TileChunk2D target = before.Chunks.Single(chunk => chunk.Contains(coordinate));
        TileCell2D[] cells = target.Tiles.ToArray();
        int index = (coordinate.Y - target.Origin.Y) * target.Width + coordinate.X - target.Origin.X;
        cells[index] = new(tileId, cells[index].Flip);
        TileChunk2D replacement = new(target.Origin, target.Width, target.Height, cells,
            checked(target.Version + 1), target.Properties);
        return new(before.Id, before.TileSize, before.TileSets,
            before.Chunks.Select(chunk => ReferenceEquals(chunk, target) ? replacement : chunk),
            before.Bounds, before.Order, before.IsVisible, before.Offset, before.Opacity, before.Tint,
            checked(before.Version + 1), before.Properties);
    }
}
