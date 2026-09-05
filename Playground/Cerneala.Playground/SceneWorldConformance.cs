using System.Text.Json;
using System.Reflection;
using System.Numerics;
using Cerneala.UI.Controls;
using Cerneala.UI.Detective;
using Cerneala.UI.Input;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Playground;

public partial class MainWindow
{
    private bool sceneWorldCaptureStarted;

    private void ObserveSceneWorldFrame()
    {
        if (ShowcaseHost.VisualChildren.FirstOrDefault() is SceneWorldShowcase view)
            view.ObserveFrame();
        string? directory = Environment.GetEnvironmentVariable("CERNEALA_SCENE_WORLD_CAPTURE");
        if (sceneWorldCaptureStarted || string.IsNullOrWhiteSpace(directory)) return;
        sceneWorldCaptureStarted = true;
        _ = CaptureSceneWorldAsync(Path.GetFullPath(directory));
    }

    private async Task CaptureSceneWorldAsync(string directory)
    {
        Directory.CreateDirectory(directory);
        try
        {
            ServoApi servo = new(this, new ServoOptions { DefaultTimeout = TimeSpan.FromSeconds(15) });
            await servo.ClickAsync(ServoTarget.ById("showcase-scene-world"));
            SceneWorldShowcase view = (SceneWorldShowcase)ShowcaseHost.VisualChildren.Single();
            await view.CaptureConformanceAsync(servo, directory);
        }
        catch (Exception error)
        {
            File.WriteAllText(Path.Combine(directory, "failure.txt"), error.ToString());
        }
        finally { Close(); }
    }
}

public partial class SceneWorldShowcase
{
    private int observedFrames;
    private readonly List<TileMapDiagnosticsSnapshot> recordingWindow = [];
    private bool collectRecordings;

    internal void ObserveFrame()
    {
        observedFrames++;
        if (Root is null || Map.Root is null) return;
        TileMapDiagnosticsSnapshot snapshot = Root.Detective.CaptureTileMap(Map);
        string metrics = $"Chunks {snapshot.VisibleChunks}/{snapshot.TotalChunks} | built {snapshot.BatchesBuilt} rebuilt {snapshot.BatchesRebuilt} reused {snapshot.BatchesReused}";
        if (MetricsText.Text != metrics) MetricsText.Text = metrics;
        if (collectRecordings && recordingWindow.Count < 256) recordingWindow.Add(snapshot);
    }

    internal async Task CaptureConformanceAsync(ServoApi servo, string directory)
    {
        List<object> measurements = [];
        string? backend = typeof(SceneWorldShowcase).Assembly.GetCustomAttribute<ApplicationBackendAttribute>()?.BackendType.FullName;
        Require(backend == "Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend", $"This conformance scenario requires the SDL GPU backend; selected: {backend}.");
        await Frames(30);
        await Snap("01-closed");
        Require(Map.Model is not null && ImportedColliders.RealizedItemCount == 6, "Imported world and six collider entities must attach.");
        Require(Door.X == 14 && Door.Y == 9 && DoorCollider.Enabled, "Door promotion binding must address (14,9), closed.");
        await Click("world-player");
        Require(PlayerSelections == 1, "Player selection must use routed pointer input.");
        await servo.PressKeyAsync(InputKey.Up);
        Require(LastMove?.Collision is not null && Math.Abs(LastMove.Travel.Y + 32) < 0.01f, "Closed door must stop upward travel at -32.");
        measurements.Add(Move("closed-door"));
        await Frames(30);
        await Snap("02-door-contact");
        await Click("world-door");
        Require(!State.DoorClosed && !DoorCollider.Enabled, "Clicking door must disable its collider through binding.");
        await Frames(30);
        await Click("world-player");
        await servo.PressKeyAsync(InputKey.Up);
        Require(LastMove?.Collision is not null && Math.Abs(LastMove.Travel.Y + 44) < 0.01f, "Open door must admit player until the house back wall at y=116.");
        measurements.Add(Move("open-door"));
        await Frames(30);
        await Snap("03-open-house");
        await Click("world-reset");
        await Click("world-player");
        await servo.PressKeyAsync(InputKey.Down);
        Require(LastMove?.Collision is not null && Math.Abs(LastMove.Travel.Y - 36) < 0.01f, "Fence must stop travel at 36, not allow tunneling.");
        measurements.Add(Move("fence"));
        await Frames(30);
        await Snap("04-fence-contact");
        await servo.PressKeyAsync(InputKey.Space);
        Require(State.PlayerState == "Attack", "Keyboard Space must select Attack animation.");
        await Frames(30);
        await Snap("05-attack-completed");

        var existingNpc = Npcs.LogicalChildren[0];
        await Click("world-add");
        Require(Npcs.RealizedItemCount == 2 && ReferenceEquals(existingNpc, Npcs.LogicalChildren[0]), "Appending NPC must preserve the first realized node.");
        measurements.Add(new { scenario = "append-npc", count = Npcs.RealizedItemCount, firstIdentityRetained = true });

        TileMapDiagnosticsSnapshot warm = Root!.Detective.CaptureTileMap(Map);
        Require(warm.VisibleChunks < warm.TotalChunks && warm.BatchesReused > 0, "Warm viewport must cull chunks and reuse static batches.");
        await RecordCommand("pan", "world-pan");
        Require(recordingWindow.Any(s => s.VisibleChunks != warm.VisibleChunks && s.BatchesReused > 0), "Pan must change the culled viewport while reusing retained batches.");
        await Snap("06-pan");
        await RecordCommand("home", "world-home");
        await RecordCommand("local-mutation", "world-mutate");
        Require(recordingWindow.Any(s => s.BatchesRebuilt == 1 && s.BatchesReused > 0), "Plant must rebuild exactly one visible batch and reuse others.");
        await Snap("07-local-mutation");

        MoveCollisionResult2D queryBefore = World.CollisionWorld.MoveAndCollide(PlayerCollider, new Vector2(0, 64));
        CollisionWorld2DDiagnosticsSnapshot indexBefore = World.CollisionWorld.GetDiagnosticsSnapshot();
        await Click("world-player");
        int selectionsBefore = PlayerSelections;
        await Click("world-debug");
        await Frames(30);
        Require(DebugOverlay.GetDiagnosticsSnapshot().Primitives > 0, "Enabled overlay must emit debug commands.");
        MoveCollisionResult2D queryAfter = World.CollisionWorld.MoveAndCollide(PlayerCollider, new Vector2(0, 64));
        CollisionWorld2DDiagnosticsSnapshot indexAfter = World.CollisionWorld.GetDiagnosticsSnapshot();
        static object? Contact(CollisionHit2D? hit) => hit is null ? null :
            (hit.Collider, hit.Entity, hit.Point, hit.Normal, hit.Distance, hit.Fraction, hit.IsTrigger);
        Require(queryBefore.Travel == queryAfter.Travel && Equals(Contact(queryBefore.Collision), Contact(queryAfter.Collision)), "Debug presentation must preserve collision query results.");
        Require(indexBefore.RebuildCount == indexAfter.RebuildCount && indexBefore.IncrementalUpdateCount == indexAfter.IncrementalUpdateCount, "Debug presentation must not mutate the collision index.");
        await Click("world-player");
        Require(PlayerSelections == selectionsBefore + 1, "Debug overlay must not intercept player picking.");
        measurements.Add(new { scenario = "debug-invariance", travelX = queryAfter.Travel.X, travelY = queryAfter.Travel.Y, indexUnchanged = true, pickingPreserved = true });
        await Snap("08-debug");
        await Click("world-debug");
        await Frames(2);
        Require(DebugOverlay.GetDiagnosticsSnapshot().Primitives == 0, "Disabled overlay must emit no debug commands.");
        await Click("world-format");
        await Frames(30);
        Require(State.IsLdtk && Map.Model!.Layers.Sum(l => l.Chunks.Count) == 2, "LDtk must load its two finite tile-layer chunks.");
        await Snap("09-ldtk");
        File.WriteAllText(Path.Combine(directory, "results.json"), JsonSerializer.Serialize(new
        {
            success = true, backend, frames = observedFrames, measurements
        }, new JsonSerializerOptions { WriteIndented = true }));

        Task Click(string id) => servo.ClickAsync(ServoTarget.ById(id));
        Task Frames(int count)
        {
            int until = observedFrames + count;
            return servo.WaitUntilAsync(_ => Task.FromResult(observedFrames >= until));
        }
        async Task Snap(string name)
        {
            await servo.SaveScreenshotAsync(Path.Combine(directory, name + ".png"));
            ServoElement player = await servo.FindAsync(ServoTarget.ById("world-player"));
            ServoElement door = await servo.FindAsync(ServoTarget.ById("world-door"));
            File.WriteAllText(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(new
            {
                player, door, surface = Root!.Detective.CaptureLayout(Surface),
                tilemap = Root.Detective.CaptureTileMap(Map),
                overlay = DebugOverlay.GetDiagnosticsSnapshot(),
                State.PlayerX, State.PlayerY, State.DoorClosed, State.PlayerState
            }, new JsonSerializerOptions { WriteIndented = true }));
            await servo.SaveScreenshotAsync(ServoTarget.ById("world-player"), Path.Combine(directory, name + "-player.png"));
        }
        object Move(string scenario) => new { scenario, x = State.PlayerX, y = State.PlayerY,
            travelX = LastMove!.Travel.X, travelY = LastMove.Travel.Y, contact = LastMove.Collision is not null };
        async Task RecordCommand(string scenario, string id)
        {
            recordingWindow.Clear(); collectRecordings = true;
            try { await Click(id); await Frames(3); }
            finally { collectRecordings = false; }
            measurements.Add(new { scenario, recordings = recordingWindow.ToArray() });
        }
        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
