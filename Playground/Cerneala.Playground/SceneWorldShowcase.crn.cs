using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using Cerneala.Drawing;
using Cerneala.Scene2D.Importers;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Resources;

namespace Cerneala.Playground;

public partial class SceneWorldShowcase : UserControl
{
    public SceneWorldState State { get; } = new();
    public MoveCollisionResult2D? LastMove { get; private set; }
    public int PlayerSelections { get; private set; }

    private bool initialized;

    private void OnLoaded(UiElementId sender, RoutedEventArgs args)
    {
        if (initialized) return;
        initialized = true;
        if (!Resources.TryGetResource(new ResourceId<ImageResource>("WorldAtlas"), out ImageResource? atlas))
            throw new InvalidOperationException("The declarative world atlas is missing.");
        // Importers retain root-relative resource IDs. Composition aliases the declared
        // resource, not its decoded pixels, so the root image cache remains the owner.
        Surface.Resources.SetResource(new ResourceId<ImageResource>("world-atlas.png"), atlas);
        DataContext = State;
    }

    private void OnSelectPlayer(UiElementId sender, RoutedEventArgs args)
    {
        PlayerSelections++;
        State.Status = "Player selected. Arrows: move; Space: attack.";
    }

    private void OnDoor(UiElementId sender, RoutedEventArgs args)
    {
        State.DoorClosed = !State.DoorClosed;
        State.Status = State.DoorClosed ? "Door closed: collider active." : "Door open: collider disabled.";
    }

    private void OnPlayerKey(UiElementId sender, RoutedEventArgs args)
    {
        if (args is not KeyEventArgs key) return;
        if (key.Key == InputKey.Space)
        {
            State.PlayerState = "Attack";
            PlayerSprite.RestartAnimation();
            args.Handled = true;
            return;
        }
        Vector2 requested = key.Key switch
        {
            InputKey.Up => new(0, -64), InputKey.Down => new(0, 64),
            InputKey.Left => new(-64, 0), InputKey.Right => new(64, 0), _ => default
        };
        if (requested == default) return;
        LastMove = World.CollisionWorld.MoveAndCollide(PlayerCollider, requested);
        State.PlayerX += LastMove.Travel.X;
        State.PlayerY += LastMove.Travel.Y;
        State.PlayerState = "Walk";
        PlayerSprite.RestartAnimation();
        State.Status = $"Requested {requested}; travel {LastMove.Travel}; contact {LastMove.Collision is not null}";
        args.Handled = true;
    }

    private void OnReset(UiElementId sender, RoutedEventArgs args) { State.ResetPlayer(); LastMove = null; }
    private void OnPan(UiElementId sender, RoutedEventArgs args) { State.CameraX = State.CameraX > -500 ? State.CameraX - 128 : 8; }
    private void OnHome(UiElementId sender, RoutedEventArgs args) { State.CameraX = 8; }
    private void OnMutate(UiElementId sender, RoutedEventArgs args) { State.Plant(); }
    private void OnAddNpc(UiElementId sender, RoutedEventArgs args) { State.Npcs.Add(new(300 + State.Npcs.Count * 20, 190)); }
    private void OnDebug(UiElementId sender, RoutedEventArgs args)
    {
        State.DebugFlags = State.DebugFlags == Scene2DDebugFlags.None ? Scene2DDebugFlags.All : Scene2DDebugFlags.None;
    }
    private void OnFormat(UiElementId sender, RoutedEventArgs args) { State.Load(!State.IsLdtk); }
}

// Authored collision regions are realized as collision-only Sprite2D items.
public sealed record SceneWorldBox(float X, float Y, float Width, float Height, uint Layer, uint Mask);
public sealed record SceneWorldNpc(float X, float Y)
{
    public DrawRect Destination => new(0, 0, 16, 16);
    public float SourceX => 64;
    public float SourceY => 16;
    public float SourceWidth => 16;
    public float SourceHeight => 16;
}

public sealed class SceneWorldState : INotifyPropertyChanged
{
    private float playerX, playerY, cameraX = 8;
    private bool doorClosed = true;
    private string playerState = "Idle", status = "";
    private Scene2DDebugFlags debugFlags;
    private Scene2DLevel level = null!;

    public SceneWorldState() { Load(false); Npcs.Add(new(310, 190)); }
    public event PropertyChangedEventHandler? PropertyChanged;
    public IReadOnlyList<TileMap2DModel> TileMaps { get; private set; } = [];
    public TileMap2DModel GroundModel => TileMaps.Single(map => map.Id == "1");
    public TileMap2DModel BuildingModel => TileMaps.Single(map => map.Id == "2");
    public TileMap2DModel DoorModel => TileMaps.Single(map => map.Id == "4");
    public float DoorX => DoorModel.Offset.X + 14 * DoorModel.TileSize.Width;
    public float DoorY => DoorModel.Offset.Y + 9 * DoorModel.TileSize.Height;
    public float DoorWidth => DoorModel.TileSize.Width;
    public float DoorHeight => DoorModel.TileSize.Height;
    public Color DoorTint => DoorModel.Tint;
    public float DoorOpacity => DoorModel.Opacity;
    public float PlayerX { get => playerX; set { playerX = value; Changed(); } }
    public float PlayerY { get => playerY; set { playerY = value; Changed(); } }
    public float CameraX { get => cameraX; set { cameraX = value; Changed(); } }
    public string PlayerState { get => playerState; set { playerState = value; Changed(); } }
    public bool DoorClosed { get => doorClosed; set { doorClosed = value; Changed(); Changed(nameof(DoorState)); } }
    public string DoorState => DoorClosed ? "Closed" : "Open";
    public Scene2DDebugFlags DebugFlags { get => debugFlags; set { debugFlags = value; Changed(); } }
    public string Status { get => status; set { status = value; Changed(); } }
    public bool IsLdtk { get; private set; }
    public IReadOnlyList<SceneWorldBox> Colliders { get; private set; } = [];
    public ObservableCollection<SceneWorldNpc> Npcs { get; } = [];
    public IScene2DDebugNavigationGrid Navigation { get; } = new VillageNavigation();
    public float PlayerWidth => 16;
    public float PlayerHeight => 16;

    public void Load(bool ldtk)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "SceneWorldAssets", ldtk ? "village.ldtk" : "village.tmj");
        Scene2DImportResult result = ldtk ? LdtkScene2DImporter.Import(path) : TiledScene2DImporter.Import(path);
        if (!result.Success) throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics));
        level = result.Document!.Levels.Single();
        if (level.Promotions.Single().Cell != new TileCellKey2D("4", 14, 9))
            throw new InvalidOperationException("The authored door declaration requires cell (4,14,9).");
        IsLdtk = ldtk;
        Colliders = level.Entities.Where(e => e.Role == "Collider").Select(e =>
        {
            if (e.Shape != "Box" || e.Rotation != 0 || e.Collider is null)
                throw new InvalidOperationException("This sample composes the six declared axis-aligned boxes only.");
            TileColliderDescriptor2D collider = e.Collider;
            return new SceneWorldBox(e.Position.X, e.Position.Y, e.Size.Width, e.Size.Height, collider.CollisionLayer, collider.CollisionMask);
        }).ToArray();
        // The door is an ordinary Sprite2D. Its source cell remains in the
        // imported document, but not in the immutable map used for rendering.
        SetTileMaps(level.TileMaps.Select(map => map.Id == "4" ? ReplaceCell(map, new(14, 9), default) : map));
        Changed(nameof(Colliders));
        DoorClosed = Equals(level.Promotions.Single().Properties["InitialState"], "Closed");
        CameraX = 8;
        ResetPlayer();
        Status = $"{(ldtk ? "LDtk" : "Tiled")} | {TileMaps.Sum(map => map.Chunks.Count)} chunks | 1 door sprite | {result.Diagnostics.Count} diagnostics";
    }

    public void ResetPlayer()
    {
        Scene2DEntity spawn = level.Entities.Single(e => e.Role == "Spawn");
        PlayerX = spawn.Position.X; PlayerY = spawn.Position.Y;
        PlayerState = (string)spawn.Properties["InitialState"]!;
    }

    public void Plant()
    {
        TileCoordinate2D location = new(10, 10);
        TileMap2DModel map = GroundModel;
        if (!map.TryGetCell(location, out TileCell2D current))
            throw new InvalidOperationException("The plant location must address an existing cell.");
        TileMap2DModel updated = ReplaceCell(map, location, new(current.TileId == 15 ? 1 : 15));
        SetTileMaps(TileMaps.Select(item => ReferenceEquals(item, map) ? updated : item));
        Status = "Plant: one immutable chunk replaced; all other chunk objects retained.";
    }

    private void SetTileMaps(IEnumerable<TileMap2DModel> maps)
    {
        TileMaps = Array.AsReadOnly(maps.ToArray());
        Changed(nameof(TileMaps));
        Changed(nameof(GroundModel));
        Changed(nameof(BuildingModel));
        Changed(nameof(DoorModel));
        Changed(nameof(DoorX));
        Changed(nameof(DoorY));
        Changed(nameof(DoorWidth));
        Changed(nameof(DoorHeight));
        Changed(nameof(DoorTint));
        Changed(nameof(DoorOpacity));
    }

    private static TileMap2DModel ReplaceCell(TileMap2DModel map, TileCoordinate2D location, TileCell2D cell)
    {
        TileChunk2D changed = map.Chunks.Single(chunk => chunk.Contains(location));
        TileCell2D[] cells = changed.Tiles.ToArray();
        int index = (location.Y - changed.Origin.Y) * changed.Width + location.X - changed.Origin.X;
        cells[index] = cell;
        TileChunk2D replacement = new(changed.Origin, changed.Width, changed.Height, cells, changed.Version + 1, changed.Properties);
        return new(map.Id, map.TileSize, map.TileSets, map.Chunks.Select(chunk => ReferenceEquals(chunk, changed) ? replacement : chunk),
            map.Bounds, map.Order, map.IsVisible, map.Offset, map.Opacity, map.Tint, map.Version + 1, map.Properties);
    }

    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));

    private sealed class VillageNavigation : IScene2DDebugNavigationGrid
    {
        public TileMapBounds2D Bounds => new(8, 9, 18, 8);
        public DrawPoint Origin => default;
        public DrawSize CellSize => new(16, 16);
        public bool TryGetCell(int x, int y, out bool blocked)
        {
            blocked = y == 15;
            return Bounds.Contains(new(x, y));
        }
    }
}
