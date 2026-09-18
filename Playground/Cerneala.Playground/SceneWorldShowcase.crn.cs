using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Relay;
using Cerneala.UI.Resources;

namespace Cerneala.Playground;

public partial class SceneWorldShowcase : UserControl
{
    public SceneWorldState State { get; } = new();
    public MoveCollisionResult2D? LastMove { get; private set; }
    public int PlayerSelections { get; private set; }
    public Task PendingOperation { get; private set; } = Task.CompletedTask;
    public Exception? OperationError { get; private set; }

    private CancellationTokenSource? lifetime;
    private SceneCollisionRegion2D? playerRegion;

    private void OnLoaded(UiElementId sender, RoutedEventArgs args)
    {
        if (lifetime is not null) return;
        lifetime = new();
        State.OwnerRelay = Root!.Relay;
        State.PropertyChanged += OnStatePropertyChanged;
        DataContext = State;
        PendingOperation = Task.CompletedTask;
        Begin(token => LoadWorldAsync(State.IsLdtk, resume: true, Root!.Relay, token));
    }

    private void OnUnloaded(UiElementId sender, RoutedEventArgs args)
    {
        CancellationTokenSource? previous = lifetime;
        lifetime = null;
        State.PropertyChanged -= OnStatePropertyChanged;
        previous?.Cancel();
        playerRegion?.Dispose();
        playerRegion = null;
        Surface.Scene = null;
        State.Suspend();
        State.OwnerRelay = null;
        Surface.Resources.Remove("world-atlas.png");
        previous?.Dispose();
    }

    private void Begin(Func<CancellationToken, Task> operation)
    {
        if (lifetime is null || !PendingOperation.IsCompleted) return;
        State.IsBusy = true;
        OperationError = null;
        PendingOperation = RunAsync(operation, Root!.Relay, lifetime.Token);
    }

    private async Task RunAsync(Func<CancellationToken, Task> operation, UiRelay relay, CancellationToken token)
    {
        try { await operation(token).ConfigureAwait(false); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
        catch (Exception error)
        {
            if (token.IsCancellationRequested) return;
            await relay.InvokeAsync(() =>
            {
                OperationError = error;
                State.Status = $"Scene operation failed: {error.Message}";
            }, token).ConfigureAwait(false);
        }
        if (!token.IsCancellationRequested)
            await relay.InvokeAsync(() => State.IsBusy = false, token).ConfigureAwait(false);
    }

    private async Task LoadWorldAsync(bool ldtk, bool resume, UiRelay relay, CancellationToken token)
    {
        // Initial opening has no world to present. Replacing a package must not
        // detach the simulation or recreate actors whose source has not changed.
        if (!State.IsLoaded) Surface.Scene = null;
        playerRegion?.Dispose();
        playerRegion = null;
        State.Status = "Loading prepared village package...";
        await State.LoadCoreAsync(ldtk, reset: !resume || !State.HasLoaded, token).ConfigureAwait(false);
        await relay.InvokeAsync(() =>
        {
            Surface.Scene = World;
            LastMove = null;
        }, token).ConfigureAwait(false);
        await PreparePlayerAsync(relay, reset: false, token).ConfigureAwait(false);
    }

    private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(SceneWorldState.Atlas) || State.Atlas is not { } atlas) return;
        // Resources and map-source notifications share the same UI publication.
        // There is no frame with new tile data and the previous package's atlas.
        Resources.SetResource(new ResourceId<ImageResource>("WorldAtlas"), atlas);
        Surface.Resources.SetResource(new ResourceId<ImageResource>("world-atlas.png"), atlas);
    }

    private async Task PreparePlayerAsync(UiRelay relay, bool reset, CancellationToken token)
    {
        SceneCollisionRegion2D? acquired = await relay.InvokeAsync(() =>
        {
            DrawPoint point = reset ? State.Spawn : new(State.PlayerX, State.PlayerY);
            // Prepare one movement step in every direction. Missing coverage never
            // becomes empty space, and an input received while busy is not replayed.
            return World.CollisionWorld.PrepareRegionAsync(
                new(point.X - 64, point.Y - 64, 128 + 12, 128 + 14), token).AsTask();
        }, token).ConfigureAwait(false);
        try
        {
            await relay.InvokeAsync(() =>
            {
                SceneCollisionRegion2D? previous = playerRegion;
                playerRegion = acquired;
                acquired = null;
                if (reset) { State.ResetPlayer(); LastMove = null; }
                previous?.Dispose();
            }, token).ConfigureAwait(false);
        }
        finally { acquired?.Dispose(); }
    }

    private void OnSelectPlayer(UiElementId sender, RoutedEventArgs args)
    {
        if (State.IsBusy) return;
        PlayerSelections++;
        State.Status = "Player selected. Arrows: move; Space: attack.";
    }

    private void OnDoor(UiElementId sender, RoutedEventArgs args)
    {
        if (State.IsBusy) return;
        State.DoorClosed = !State.DoorClosed;
        State.Status = State.DoorClosed ? "Door closed: collider active." : "Door open: collider disabled.";
    }

    private void OnPlayerKey(UiElementId sender, RoutedEventArgs args)
    {
        if (args is not KeyEventArgs key) return;
        if (State.IsBusy || playerRegion?.IsReady != true)
        {
            args.Handled = true;
            return;
        }
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
        Begin(token => PreparePlayerAsync(Root!.Relay, reset: false, token));
        args.Handled = true;
    }

    private void OnReset(UiElementId sender, RoutedEventArgs args) =>
        Begin(token => PreparePlayerAsync(Root!.Relay, reset: true, token));
    private void OnPan(UiElementId sender, RoutedEventArgs args)
    { if (!State.IsBusy) State.CameraX = State.CameraX > -500 ? State.CameraX - 128 : 8; }
    private void OnHome(UiElementId sender, RoutedEventArgs args) { if (!State.IsBusy) State.CameraX = 8; }
    private void OnMutate(UiElementId sender, RoutedEventArgs args)
    { if (!State.IsBusy && State.IsLoaded) Begin(token => State.PlantAsync(token)); }
    private void OnAddNpc(UiElementId sender, RoutedEventArgs args)
    { if (!State.IsBusy) State.Npcs.Add(new(300 + State.Npcs.Count * 20, 190)); }
    private void OnDebug(UiElementId sender, RoutedEventArgs args)
    {
        if (!State.IsBusy)
            State.DebugFlags = State.DebugFlags == Scene2DDebugFlags.None ? Scene2DDebugFlags.All : Scene2DDebugFlags.None;
    }
    private void OnFormat(UiElementId sender, RoutedEventArgs args) =>
        Begin(token => LoadWorldAsync(!State.IsLdtk, resume: false, Root!.Relay, token));
}

// Authored walls are acquired only for spatial interests. Their large authoring
// properties do not become permanent application backing data.
public sealed record SceneWorldBox(float X, float Y, float Width, float Height, uint Layer, uint Mask);
public sealed record SceneWorldNpc(float X, float Y)
{
    internal string Id { get; } = Guid.NewGuid().ToString("N");
    public DrawRect Destination => new(0, 0, 16, 16);
    public DrawRect? PrismInputDomain => Destination;
    public float SourceX => 64;
    public float SourceY => 16;
    public float SourceWidth => 16;
    public float SourceHeight => 16;
}

public sealed class SceneWorldState : INotifyPropertyChanged, IDisposable
{
    private float playerX, playerY, cameraX = 8;
    private bool doorClosed = true, planted, isBusy;
    private string playerState = "Idle", status = "";
    private Scene2DDebugFlags debugFlags;
    private SceneWorldPackage? world;
    private long generation;
    private bool disposed;
    private readonly string packageRoot;
    private readonly Dictionary<string, SceneWorldNpc> npcPayloads = new(StringComparer.Ordinal);

    public SceneWorldState(string? packageRoot = null)
    {
        this.packageRoot = packageRoot ?? Path.Combine(AppContext.BaseDirectory, "SceneWorldPackages");
        NpcSource = new([], (entry, _) => ValueTask.FromResult(new SceneSpatialLease2D<object>(npcPayloads[entry.Id])));
        Npcs.CollectionChanged += (_, _) => PublishNpcs();
        Npcs.Add(new(310, 190));
    }

    internal UiRelay? OwnerRelay { get; set; }
    internal bool HasLoaded { get; private set; }
    internal ImageResource? Atlas => world?.Atlas;
    public bool IsLoaded => world is not null;
    public bool IsBusy { get => isBusy; internal set { isBusy = value; Changed(); } }
    public event PropertyChangedEventHandler? PropertyChanged;
    public IReadOnlyList<TileMapSource2D> TileMaps => world?.TileMaps ?? [];
    public TileMapSource2D? GroundSource => TileMaps.SingleOrDefault(map => map.Catalog.Id == "1");
    public TileMapSource2D? BuildingSource => TileMaps.SingleOrDefault(map => map.Catalog.Id == "2");
    public TileMapSource2D? DoorSource => TileMaps.SingleOrDefault(map => map.Catalog.Id == "4");
    public float DoorX => (DoorSource?.Catalog.Offset.X ?? 0) + 14 * DoorWidth;
    public float DoorY => (DoorSource?.Catalog.Offset.Y ?? 0) + 9 * DoorHeight;
    public float DoorWidth => DoorSource?.Catalog.TileSize.Width ?? 16;
    public float DoorHeight => DoorSource?.Catalog.TileSize.Height ?? 16;
    public DrawRect? DoorPrismInputDomain => new DrawRect(0, 0, DoorWidth, DoorHeight);
    public Color DoorTint => DoorSource?.Catalog.Tint ?? Color.White;
    public float DoorOpacity => DoorSource?.Catalog.Opacity ?? 1;
    public float PlayerX { get => playerX; set { playerX = value; Changed(); } }
    public float PlayerY { get => playerY; set { playerY = value; Changed(); } }
    public float CameraX { get => cameraX; set { cameraX = value; Changed(); } }
    public string PlayerState { get => playerState; set { playerState = value; Changed(); } }
    public bool DoorClosed { get => doorClosed; set { doorClosed = value; Changed(); Changed(nameof(DoorState)); } }
    public string DoorState => DoorClosed ? "Closed" : "Open";
    public Scene2DDebugFlags DebugFlags { get => debugFlags; set { debugFlags = value; Changed(); } }
    public string Status { get => status; set { status = value; Changed(); } }
    public bool IsLdtk { get; private set; }
    public ObservableCollection<SceneWorldNpc> Npcs { get; } = [];
    public SceneSpatialSource2D<object> NpcSource { get; }
    public ISceneSpatialSource2D<object>? ColliderSource => world?.ColliderSource;
    public DrawPoint Spawn => RequireWorld().Spawn;
    public IScene2DDebugNavigationGrid Navigation { get; } = new VillageNavigation();
    public float PlayerWidth => 16;
    public float PlayerHeight => 16;
    public DrawRect? PlayerPrismInputDomain => new DrawRect(0, 0, PlayerWidth, PlayerHeight);

    public Task LoadAsync(bool ldtk, CancellationToken cancellationToken = default) =>
        LoadCoreAsync(ldtk, reset: true, cancellationToken);

    internal async Task LoadCoreAsync(bool ldtk, bool reset, CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        OwnerRelay?.VerifyAccess();
        long request = ++generation;
        UiRelay? relay = OwnerRelay;
        bool restorePlant = !reset && planted;
        SceneWorldPackage? next = await SceneWorldPackage.OpenAsync(
            Path.Combine(packageRoot, ldtk ? "ldtk" : "tiled"), token).ConfigureAwait(false);
        try
        {
            if (restorePlant)
            {
                using var edit = await next.PreparePlantAsync(token).ConfigureAwait(false);
                edit.Publish(); // This unopened composition has no scene subscribers yet.
            }
            void Publish()
            {
                token.ThrowIfCancellationRequested();
                if (disposed || generation != request) throw new OperationCanceledException("The village load was superseded.");
                SceneWorldPackage? previous = world;
                world = next;
                next = null;
                try
                {
                    IsLdtk = ldtk;
                    HasLoaded = true;
                    if (reset)
                    {
                        planted = false;
                        DoorClosed = world.DoorClosed;
                        CameraX = 8;
                        ResetPlayer();
                    }
                    SourcesChanged();
                    Status = $"{(ldtk ? "LDtk" : "Tiled")} package | {TileMaps.Sum(map => map.Catalog.Chunks.Count)} chunks | 1 door sprite";
                }
                finally { previous?.Dispose(); }
            }
            if (relay is null) Publish();
            else await relay.InvokeAsync(Publish, token).ConfigureAwait(false);
        }
        finally { next?.Dispose(); }
    }

    public void ResetPlayer()
    {
        SceneWorldPackage current = RequireWorld();
        PlayerX = current.Spawn.X; PlayerY = current.Spawn.Y;
        PlayerState = current.SpawnState;
    }

    public async Task PlantAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        OwnerRelay?.VerifyAccess();
        cancellationToken.ThrowIfCancellationRequested();
        SceneWorldPackage current = RequireWorld();
        UiRelay? relay = OwnerRelay;
        long request = ++generation;
        using var edit = await current.PreparePlantAsync(cancellationToken).ConfigureAwait(false);
        void Publish()
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (disposed || generation != request || !ReferenceEquals(world, current))
                throw new OperationCanceledException("The Plant operation was superseded.");
            edit.Publish();
            planted = !planted;
            Status = "Plant: one prepared decorative chunk published; grass and unchanged chunks retained.";
        }
        if (relay is null) Publish();
        else await relay.InvokeAsync(Publish, cancellationToken).ConfigureAwait(false);
    }

    internal void Suspend()
    {
        generation++;
        SceneWorldPackage? previous = world;
        world = null;
        SourcesChanged();
        previous?.Dispose();
    }

    public void Dispose()
    {
        if (disposed) return;
        OwnerRelay?.VerifyAccess();
        disposed = true;
        Suspend();
    }

    private SceneWorldPackage RequireWorld() => world ?? throw new InvalidOperationException("The village package is not loaded.");

    private void SourcesChanged()
    {
        foreach (string name in new[] { nameof(Atlas), nameof(IsLoaded), nameof(IsLdtk), nameof(TileMaps),
            nameof(GroundSource), nameof(BuildingSource), nameof(DoorSource), nameof(ColliderSource),
            nameof(DoorX), nameof(DoorY), nameof(DoorWidth), nameof(DoorHeight), nameof(DoorPrismInputDomain),
            nameof(DoorTint), nameof(DoorOpacity) })
            Changed(name);
    }

    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));

    private void PublishNpcs()
    {
        npcPayloads.Clear();
        foreach (SceneWorldNpc npc in Npcs) npcPayloads.Add(npc.Id, npc);
        NpcSource.SetEntries(Npcs.Select(npc => new SceneSpatialEntry2D(
            npc.Id, new(npc.X, npc.Y, 16, 16), isSimulated: true)));
    }

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
