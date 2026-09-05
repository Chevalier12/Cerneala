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

// Immutable template data uses initial-value references, not live bindings.
public sealed record SceneWorldBox(float X, float Y, float Width, float Height, uint Layer, uint Mask);
public sealed record SceneWorldNpc(float X, float Y)
{
    public DrawRect Destination => new(0, 0, 16, 16);
    public DrawRect? SourceRect => new DrawRect(64, 16, 16, 16);
}

public sealed class SceneWorldState : INotifyPropertyChanged
{
    private TileMap2DModel model = null!;
    private float playerX, playerY, cameraX = 8;
    private bool doorClosed = true;
    private string playerState = "Idle", status = "";
    private Scene2DDebugFlags debugFlags;
    private Scene2DLevel level = null!;

    public SceneWorldState() { Load(false); Npcs.Add(new(310, 190)); }
    public event PropertyChangedEventHandler? PropertyChanged;
    public TileMap2DModel Model { get => model; private set { model = value; Changed(); } }
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
    public DrawRect PlayerDestination => new(0, 0, 16, 16);

    public void Load(bool ldtk)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "SceneWorldAssets", ldtk ? "village.ldtk" : "village.tmj");
        Scene2DImportResult result = ldtk ? LdtkScene2DImporter.Import(path) : TiledScene2DImporter.Import(path);
        if (!result.Success) throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics));
        level = result.Document!.Levels.Single();
        if (level.Promotions.Single().Cell != new TileCellKey2D("2", 14, 9))
            throw new InvalidOperationException("The authored door declaration requires promotion (2,14,9).");
        IsLdtk = ldtk;
        Colliders = level.Entities.Where(e => e.Role == "Collider").Select(e =>
        {
            if (e.Shape != "Box" || e.Rotation != 0 || e.Colliders.Count != 1)
                throw new InvalidOperationException("This sample composes the six declared axis-aligned boxes only.");
            TileColliderDescriptor2D collider = e.Colliders[0];
            return new SceneWorldBox(e.Position.X, e.Position.Y, e.Size.Width, e.Size.Height, collider.CollisionLayer, collider.CollisionMask);
        }).ToArray();
        Model = level.TileMap;
        Changed(nameof(Colliders));
        DoorClosed = Equals(level.Promotions.Single().Properties["InitialState"], "Closed");
        CameraX = 8;
        ResetPlayer();
        Status = $"{(ldtk ? "LDtk" : "Tiled")} | {Model.Layers.Sum(l => l.Chunks.Count)} chunks | 1 promoted door | {result.Diagnostics.Count} diagnostics";
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
        TileLayer2DModel layer = Model.Layers.Single(l => l.Id == "1");
        TileChunk2D changed = layer.Chunks.Single(c => c.Contains(location));
        TileCell2D[] cells = changed.Tiles.ToArray();
        int index = (location.Y - changed.Origin.Y) * changed.Width + location.X - changed.Origin.X;
        cells[index] = new(cells[index].TileId == 15 ? 1 : 15);
        TileChunk2D replacement = new(changed.Origin, changed.Width, changed.Height, cells, changed.Version + 1, changed.Properties);
        TileLayer2DModel updated = new(layer.Id, layer.Chunks.Select(c => ReferenceEquals(c, changed) ? replacement : c),
            layer.Order, layer.IsVisible, layer.Offset, layer.Opacity, layer.Tint, layer.Version, layer.Properties);
        Model = new(Model.TileSize, Model.TileSets, Model.Layers.Select(l => ReferenceEquals(l, layer) ? updated : l),
            Model.Bounds, Model.Version + 1, Model.Properties);
        Status = "Plant: one immutable chunk replaced; all other chunk objects retained.";
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
