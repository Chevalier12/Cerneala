using Cerneala.UI.Controls;

namespace Cerneala.UI.Elements;

public sealed partial class UIRoot
{
    // Simulation contexts of the render surfaces under this root, in attachment order.
    private readonly List<SceneSimulationContext2D> sceneContactContexts = [];
    private readonly List<PendingContact2D> sceneContactEvents = [];
    private bool deliveringSceneContacts;

    internal void RegisterSceneContacts(SceneSimulationContext2D context) => sceneContactContexts.Add(context);

    internal void UnregisterSceneContacts(SceneSimulationContext2D context) => sceneContactContexts.Remove(context);

    // True while a scene that DeliverSceneContacts prepares has a change waiting for the next delivery.
    // The window runtime and Servo must not treat the root as idle until it was delivered.
    internal bool HasPendingSceneContacts
    {
        get
        {
            for (int index = 0; index < sceneContactContexts.Count; index++)
            {
                if (sceneContactContexts[index].Scene.CollisionWorld.HasPendingContacts) { return true; }
            }

            return false;
        }
    }

    // Collider2D.Entered and Exited for every scene shown by a render surface under this root. The host
    // calls it once per update, after input dispatch and before the frame is processed. Every scene computes its
    // contact difference first, then the events of all scenes are raised in one fixed order.
    internal void DeliverSceneContacts()
    {
        if (sceneContactContexts.Count == 0) { return; }
        if (deliveringSceneContacts)
        {
            throw new InvalidOperationException("Scene contact delivery cannot be nested.");
        }

        deliveringSceneContacts = true;
        try
        {
            for (int index = 0; index < sceneContactContexts.Count; index++)
            {
                sceneContactContexts[index].Scene.CollisionWorld.PrepareContacts(sceneContactEvents, index);
            }

            CollisionWorld2D.RaiseContacts(sceneContactEvents);
        }
        finally { deliveringSceneContacts = false; }
    }
}
