# SceneSimulationContext2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/SceneSimulationContext2D.cs`

Owns map-data preparation, collision interests, and scene mutation on one thread, independently of rendering.

```csharp
public sealed class SceneSimulationContext2D : IDisposable
```

## Examples

Create an independent owner for an unowned scene. The application's existing simulation loop calls `Update` on the constructing thread; no UI root or surface is created.

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Relay;

Scene2D scene = new();
using SceneSimulationContext2D simulation = new(scene,
    new UiRelayOptions { MaxCallbacksPerUpdate = 128 });

Task<SceneCollisionRegion2D> preparing = scene.CollisionWorld
    .PrepareRegionAsync(new DrawRect(2000, 0, 100, 100)).AsTask();

// In each ordinary simulation tick, on the owner thread:
simulation.Update();
if (preparing.IsCompletedSuccessfully)
{
    using SceneCollisionRegion2D region = preparing.GetAwaiter().GetResult();
    // Query only the covered scene-space envelope while this interest is alive.
}
```

For package-backed asynchronous map data, keep the normal loop running until preparation finishes and observe cancellation or failure as well as success. Do not replace that loop with a blocking wait on an unfinished task. In a game, retain the region for as long as its terrain is needed rather than reacquiring it on every tick.

## Remarks

### Ownership and updates

The public constructor requires a root `Scene2D` with no UI root, surface, parent, or existing simulation context. Its existing scene-node subtree adopts the context. Descendants added later adopt the same owner, and removing a subtree retires its spatial acquisitions and data observation. A scene cannot belong to two contexts. Dispose its independent context before reparenting it or attaching it to UI.

The context reuses [UiRelay](Cerneala.UI.Relay.UiRelay.md), capturing the constructing thread. `Update` drains one bounded queue snapshot and refreshes map/collision interests. Work posted during the drain waits for another update. Nested updates are rejected. Scene-items collection notifications and template/tree changes belong on this owner; direct off-thread property, child/template collection, and collision-query operations are rejected. Asynchronous map completions return through the owner relay before changing scene state.

An attached [RenderSurface2D](Cerneala.UI.Controls.RenderSurface2D.md) creates the same kind of context using the existing `UIRoot.Relay`. Its normal frame/arrangement path supplies the viewport and refreshes spatial interest. The host, not application calls to `Update` or `Dispose`, owns that attached context; detach or replace the surface's scene to retire it. [SceneNode2D.SimulationContext](Cerneala.UI.Controls.SceneNode2D.md) identifies the current owner without changing `UIElement.Root` or `IsAttached`.

### Without rendering

An independent context has no visual viewport. [SceneItems2D](Cerneala.UI.Controls.SceneItems2D.md) realizes every occurrence in its current enumerable snapshot; it does not choose entries from pre-realization spatial metadata. A marked, already-realized [Collider2D](Cerneala.UI.Controls.Collider2D.md) can retain nearby tile collision data around its own active geometry. Static terrain is also prepared through [CollisionWorld2D.PrepareRegionAsync](Cerneala.UI.Controls.CollisionWorld2D.md). Queries never perform hidden I/O or treat missing collision data as empty.

Typed `UiPropertyBinding<T>`, generated property bindings, and generated conditional data values use the same owner relay and retire observation when their subtree leaves the context. Ordinary permanent node properties and caller-supplied game logic can update collider transforms without a renderer.

This is not a headless UI engine or an automatic physics loop. It does not create a thread, timer, `UIRoot`, render surface, layout/input service, image decoder, Aspect processor, UI Motion system, or sprite-animation clock. It does not raise UI Loaded/Unloaded events or mark nodes UI-attached. UI-dependent Motion/Aspect activation and resource-provider services still require their existing UI host. Attached offscreen NPCs retain their existing UI animation/simulation behavior.

### Retirement

Disposal must run on the owner thread and is idempotent. It invalidates prepared regions, cancels pending map preparation, unsubscribes scene-items collection observation, removes materialized nodes, and releases their acquisitions. Permanent authored children remain in the scene for explicit reuse. Late map results cannot publish into a disposed or replacement context; uncooperative internal loaders may still finish and their results are retired without requiring another update of the old context.

Disposal does not synchronously wait for arbitrary application work or prove collection of data still retained by the application or a model. It does not shut down the general-purpose relay or execute user-posted work as an implicit cleanup step. Observe map-data failures through `TileMap2D.Preparation`/`PreparationError` and explicit region-preparation tasks; `SceneItems2D` has no asynchronous preparation property.

## Constructors

| Name | Description |
| --- | --- |
| `SceneSimulationContext2D(Scene2D scene, UiRelayOptions? relayOptions = null)` | Creates an independent owner on the calling thread. The relay defaults to 1,024 callbacks per update. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Scene` | `Scene2D` | Root scene owned by this context. |
| `Relay` | `UiRelay` | Queue and access checks for the owner thread; does not create a thread. |
| `IsDisposed` | `bool` | Whether retirement has begun. |

## Methods

| Name | Description |
| --- | --- |
| `Update()` | Drains a bounded relay snapshot and refreshes independent scene interests. Throws after disposal, off-thread, for nested updates, or on a UI-hosted context. |
| `Dispose()` | Retires an independent context on its owner thread. A live UI-hosted context must instead be retired by detaching/replacing its surface scene. |

## See also

- [Scene2D](Cerneala.UI.Controls.Scene2D.md)
- [SceneItems2D](Cerneala.UI.Controls.SceneItems2D.md)
- [SceneCollisionRegion2D](Cerneala.UI.Controls.SceneCollisionRegion2D.md)
- [UiRelayOptions](Cerneala.UI.Relay.UiRelayOptions.md)
