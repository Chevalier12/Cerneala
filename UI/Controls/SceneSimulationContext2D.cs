using Cerneala.UI.Relay;

namespace Cerneala.UI.Controls;

/// <summary>Owns spatial preparation and scene mutation on one thread, independently of rendering.</summary>
public sealed class SceneSimulationContext2D : IDisposable
{
    private readonly List<ISceneSpatialParticipant2D> spatialItems = [];
    private bool disposed;
    private bool refreshing;
    private bool refreshPending;
    private bool updating;
    private int treeChanges;

    public SceneSimulationContext2D(Scene2D scene, UiRelayOptions? relayOptions = null)
        : this(scene, new UiRelay(relayOptions), null)
    {
    }

    internal SceneSimulationContext2D(Scene2D scene, RenderSurface2D surface)
        : this(scene, surface.Root!.Relay, surface)
    {
    }

    private SceneSimulationContext2D(Scene2D scene, UiRelay relay, RenderSurface2D? surface)
    {
        ArgumentNullException.ThrowIfNull(scene);
        relay.VerifyAccess();
        if (scene.SimulationContext is not null ||
            (surface is null && (scene.Root is not null || scene.Surface is not null ||
                scene.LogicalParent is not null || scene.VisualParent is not null)))
        {
            throw new InvalidOperationException("A simulation context requires an unowned root scene. Dispose its previous context before transferring it.");
        }

        Scene = scene;
        Relay = relay;
        Surface = surface;
        scene.ValidateSimulationAttachment(this);
        try { scene.AttachSimulationContext(this); }
        catch (Exception failure)
        {
            try { Retire(); }
            catch (Exception cleanup) { throw new AggregateException(failure, cleanup); }
            throw;
        }
    }

    public Scene2D Scene { get; }
    public UiRelay Relay { get; }
    public bool IsDisposed => Volatile.Read(ref disposed);
    internal RenderSurface2D? Surface { get; }
    internal bool IsHeadless => Surface is null;
    internal IReadOnlyList<ISceneSpatialParticipant2D> SpatialItems => spatialItems;

    /// <summary>Drains one bounded Relay snapshot and refreshes spatial interests. Does not render or advance UI Motion.</summary>
    public void Update()
    {
        Relay.VerifyAccess();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (!IsHeadless) { throw new InvalidOperationException("The UI host updates an attached scene's context."); }
        if (updating) { throw new InvalidOperationException("Simulation updates cannot be nested."); }
        updating = true;
        try
        {
            Relay.Drain();
            RefreshSpatialItems();
        }
        finally { updating = false; }
    }

    internal void BeginTreeChange() => treeChanges++;
    internal void EndTreeChange() => treeChanges--;

    internal void Register(ISceneSpatialParticipant2D items)
    {
        if (!spatialItems.Contains(items)) { spatialItems.Add(items); refreshPending = true; }
    }

    internal void Unregister(ISceneSpatialParticipant2D items)
    {
        if (spatialItems.Remove(items)) { refreshPending = true; }
    }

    internal SceneBounds2D GetVisibleBounds(SceneNode2D node) =>
        Surface?.GetSpatialViewport(node) ?? SceneBounds2D.Empty;

    internal void RefreshSpatialItems(float? width = null, float? height = null)
    {
        Relay.VerifyAccess();
        if (IsDisposed) { return; }
        if (refreshing || treeChanges != 0) { refreshPending = true; return; }
        refreshing = true;
        try
        {
            do
            {
                refreshPending = false;
                IReadOnlyList<SceneBounds2D> collisionInterest = Scene.CollisionWorld.GetSpatialCollisionInterest();
                // Publishing may add or retire nested materializers in this same context.
                for (int index = 0; !IsDisposed && index < spatialItems.Count;)
                {
                    ISceneSpatialParticipant2D items = spatialItems[index];
                    SceneBounds2D visible = Surface is null ? SceneBounds2D.Empty
                        : width is float w && height is float h
                            ? Surface.GetSpatialViewport(items.Node, w, h) : Surface.GetSpatialViewport(items.Node);
                    // ArrangeCore supplies the incoming size before ArrangedBounds
                    // is committed. Footprint projection must use that same frame,
                    // not the previous (possibly zero-sized) surface geometry.
                    items.UpdateSpatialInterest(visible, collisionInterest, Surface?.GetPresentationBounds(width, height));
                    if (index < spatialItems.Count && ReferenceEquals(items, spatialItems[index])) { index++; }
                }
            } while (refreshPending && !IsDisposed);
        }
        finally { refreshing = false; }
    }

    public void Dispose()
    {
        Relay.VerifyAccess();
        if (!IsHeadless && !IsDisposed)
        {
            throw new InvalidOperationException("The UI host owns this context. Detach or replace its surface scene instead.");
        }
        Retire();
    }

    internal void Retire()
    {
        Relay.VerifyAccess();
        if (IsDisposed) { return; }
        Volatile.Write(ref disposed, true);
        List<Exception>? failures = null;
        try { Scene.CollisionWorld.InvalidateSpatialRegions(); }
        catch (Exception failure) { (failures ??= []).Add(failure); }
        try { Scene.AttachSimulationContext(null); }
        catch (Exception failure) { (failures ??= []).Add(failure); }
        spatialItems.Clear();
        if (failures is not null) { throw new AggregateException(failures); }
    }
}
