# SceneItems2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/SceneItems2D.cs`

Acquires spatial payloads and realizes retained scene nodes for the surface viewport and active simulation.

```csharp
public sealed class SceneItems2D : SceneNode2D
```

Inheritance: `object` -> `UiObject` -> `UIElement` -> `SceneNode2D` -> `SceneItems2D`

## Examples

The source declares bounds before the template is instantiated. This in-memory example owns its model strings; releasing an acquisition does not unload those caller-owned strings.

```csharp
var source = new SceneSpatialSource2D<object>(
    [new SceneSpatialEntry2D("npc-17", new DrawRect(120, 80, 32, 48), isSimulated: true)],
    (entry, cancellationToken) =>
        ValueTask.FromResult(new SceneSpatialLease2D<object>(entry.Id)));

var items = new SceneItems2D();
items.Templates.Add(new ContentTemplate<string>("npc", null, 0,
    context => new Sprite2D
    {
        X = 120, Y = 80, Width = 32, Height = 48,
        DataContext = context.Data
    }));
items.ItemsSource = source;

var scene = new Scene2D();
scene.Children.Add(items);
var surface = new RenderSurface2D { Scene = scene, ViewBox = new DrawRect(0, 0, 320, 180) };
```

The sprite in this example has no image. Assign its `Image` and, when required, its collider using the ordinary [Sprite2D](Cerneala.UI.Controls.Sprite2D.md) APIs.

In `.crn` files the template collection still uses `@templates { ... }`. Bind `ItemsSource` to an `ISceneSpatialSource2D<object>`; a template's `DataType` describes its payload model, not the metadata entry. Legacy `SceneItems2D.Templates` property-element syntax remains rejected.

## Remarks

### Identity and ordering

This is a breaking migration from `IEnumerable`. Arbitrary enumerable/template pairs cannot provide bounds before realization and are no longer accepted. Publish immutable catalog snapshots through a spatial source instead of collection delta notifications.

Selected entries are realized and recorded in catalog order. Identity is the ordinal, case-sensitive entry ID plus payload version, within the same source. Inserting, removing, or reordering other entries does not recreate retained nodes. Publishing changed bounds with the same ID/version keeps the payload and node. Incrementing a payload version replaces that entry; replacing the source or changing templates replaces the relevant nodes.

The materializer's logical bounds are the union of the current catalog's visual bounds, including unloaded entries. Camera residency does not shrink those bounds or change the containing scene's `LayerThenY` anchor or Prism capture coordinates. Publishing a new catalog updates the union without acquiring payloads. An empty or null source has empty bounds.

The template match and creation contexts use their default `Index = -1`; they do not receive a positional index. Put application ordering information in the model and bind it explicitly. Ordinary UI content-template index semantics are unchanged. `TryGetRealizedNode` looks up a currently realized node by ID without loading it.

A matching content template must return a `SceneNode2D`. With no matching template, a payload that already is a scene node is used directly. Distinct identities require distinct nodes. Nodes already owned by another parent cannot be adopted.

### Residency and presentation

Preparation requires a [SceneSimulationContext2D](Cerneala.UI.Controls.SceneSimulationContext2D.md). An attached surface supplies that common owner and refreshes spatial interest during frame updates and arrangement, including in `OnDemand` mode. ViewBox, stretch, raster size/DPI, and nested scene transforms determine the conservative local viewport. An independent context has an empty viewport and selects only simulated entries and collision interests; the application's normal owner-thread loop pumps `Update`. It does not attach nodes to UI or start image loading, UI Motion, Aspect, or an animation clock.

Entry bounds use the materializer's local coordinate space. They must conservatively include the templated subtree's visual influence, including its own effects. The source must publish changed bounds as its model moves, or supply an envelope covering that motion. Bounds are not inferred by loading an image or measuring a template.

Pointer picking uses those bounds to select visual candidates before asking a templated sprite for image-dependent geometry. A metadata rectangle is not itself a hit shape: retained input filters and precise node geometry still decide the target. Actual collider hits preserve their ancestor route independently of the visual envelope, including a collider extending beyond its sprite. A pointer miss outside an entry's metadata therefore does not reload that entry's retired image just to measure it. Non-invertible input transforms retain conservative traversal.

The materializer selects viewport intersections, collision-interest intersections, and every entry with `IsSimulated = true`. A simulated entry remains attached outside the viewport: its node identity, bindings, Motion state, sprite-animation registration, and colliders remain present. Camera retirement does not assign `IsVisible`, disable colliders, or detach active simulation. Offscreen entries are omitted from recording and their node-owned render/image acquisitions are released, including static entries retained only for collisions. Other consumers, retained commands, direct/borrowed images, or a caller-owned model can still retain resources.

Changed spatial interest retires unnecessary node-owned render acquisitions independently of successful scene recording. An unrelated source that puts the surface in `Loading` must not prevent an offscreen NPC image from being released. Hidden/transparent scene ancestors also retire these graphical acquisitions without detaching simulation.

The owning surface checks current visual identity/version/template coverage before submitting the scene or making scene input available. Missing required visible payloads withhold the entire scene; a relevant `PreparationError` is also exposed as the surface's `PresentationError`. This does not gate on every source task indiscriminately: an offscreen simulation failure can remain source-visible while an already-ready viewport stays presentable. Worker-published catalogs are checked even before their UI notifications apply. See [RenderSurface2D](Cerneala.UI.Controls.RenderSurface2D.md) for the current integration limits and application-composed loading/error UI.

For camera/interest changes, still-valid static entries leaving the selected region are detached and their spatial acquisitions released after the replacement selection is prepared. This does not postpone catalog edits: when a published catalog is applied on the owning UI thread, removed IDs and obsolete payload versions are detached and released immediately, even if unrelated replacements are still loading. Unchanged IDs/versions retain their nodes and acquisitions. The authoritative world is not an old complete snapshot waiting for a future all-at-once commit.

Each realized node owns its payload acquisition independently of the temporary preparation snapshot. Shared ID/version interests are acquired before obsolete requests are cancelled. A deleted object's old collider and image ownership leave with its normal node lifecycle; the materializer does not assign `Collider.Enabled` to hide stale geometry. New unavailable collision data still causes the collision world's readiness exception. Prepare application data before publishing it when a visual transition must not expose an unprepared replacement. The source controls actual payload allocation and release; this control cannot unload data still retained by application code.

An ancestor Prism composition containing only supported pointwise adjustments or paint styles does not expand camera interest. Image masks retain their separate resource dependency, including feathering, without selecting extra scene payloads. Covered local neighborhood filters automatically include the off-camera sampling input they need, without selecting the full catalog. Offscreen payload and image retirement outside required input still applies, while logical capture bounds remain catalog-based. Every active composition without supported automatic input selection requires a finite `PrismInputDomain` on its owner; this includes global filters, the seven remaining styles, wrapped edge modes and unclassified filters. Required off-camera entries within that domain are prepared and recorded before the final camera clip. A missing required declaration reports a surface presentation error instead of starting whole-world presentation I/O. The materializer itself has no Prism scope; place the effect and domain on the containing scene or templated node. See [Scene2D's Prism input rules](Cerneala.UI.Controls.Scene2D.md#prism-input-and-streamed-children) for the exact operation/edge-mode coverage and live-state handling. Non-invertible/non-finite transforms can still require conservative selection; a finite domain can still exceed raster or memory limits. This is not a blanket bounded-residency guarantee for those cases.

The root [CollisionWorld2D](Cerneala.UI.Controls.CollisionWorld2D.md) combines explicit prepared-region leases and the `CollisionBounds` of simulated entries into terrain interest. Those rectangles are separate from visual bounds and are transformed into each materializer's local space. Distant NPCs do not implicitly retain all terrain between them. A query requiring missing collision data throws `SceneCollisionRegionNotReadyException`; prepare its complete envelope explicitly instead of relying on camera residency. Explicitly hidden sources do not contribute collision coverage or simulation terrain interest.

### Asynchronous work and errors

The residency owner allows up to four concurrent payload loads. Application loaders explicitly provide asynchronous work; a synchronous loader is not moved to a worker implicitly. Payload completion and source notifications are marshaled through the simulation context's relay before creating templates or changing the scene tree. Keep the normal UI frame loop or independent simulation `Update` loop running while awaiting preparation; do not synchronously block its thread on an unfinished `Preparation`.

`Preparation` represents the most recently requested preparation, not an immutable guarantee for a later camera/source revision. An error is available through that task and `PreparationError`. Failed requests do not retry every frame. `Refresh()` explicitly retries or republishes interest; source/catalog and viewport changes also create new requests. A superseded request may be cancelled. Late results cannot attach to a replacement source.

Detach unsubscribes, cancels pending interest, removes realized nodes, and releases acquisitions. Reattach subscribes once and prepares the then-current source. Node detach uses the normal binding, Aspect, Motion, Prism, image-resource, and surface lifecycle. The materializer adds no separate Prism effect layer; declare effects on the templated nodes.

In an independent context, leaving the simulation owner performs source/data-binding retirement without pretending to run UI detach events. Cancellation and late-result retirement do not require a final pump of a disposed context. A new context is a new acquisition lifecycle; it never revives old region leases or pending publications.

## Constructors

| Name | Description |
| --- | --- |
| `SceneItems2D()` | Creates an empty spatial materializer and template collection. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `ItemsSourceProperty` | `UiProperty<ISceneSpatialSource2D<object>?>` | Identifies the source property. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ItemsSource` | `ISceneSpatialSource2D<object>?` | Source catalog and payload-acquisition provider; initially null. |
| `Templates` | `Collection<ContentTemplate>` | Templates resolved against acquired payload models. |
| `RealizedItemCount` | `int` | Number of currently realized entries, including simulated entries; independent-context nodes are not UI-attached. |
| `Preparation` | `Task` | Most recently requested preparation; initially completed. |
| `PreparationError` | `Exception?` | Last reported preparation error; cleared when a new request starts. |

## Methods

| Name | Description |
| --- | --- |
| `Refresh()` | Re-evaluates interest and explicitly retries preparation using the current source and viewport. Requires the current owner's thread. |
| `TryGetRealizedNode(string id, out SceneNode2D? node)` | Returns a currently realized node without loading; false/null when absent. Requires the current owner's thread. |

## Property Information

| Property | Identifier field | Default | Metadata/options |
| --- | --- | --- | --- |
| `ItemsSource` | `ItemsSourceProperty` | `null` | `AffectsRender` |

## See also

- [SceneSpatialEntry2D](Cerneala.UI.Controls.SceneSpatialEntry2D.md)
- [SceneSpatialSource2D&lt;T&gt;](Cerneala.UI.Controls.SceneSpatialSource2D_T_.md)
- [SceneSpatialResidency2D&lt;T&gt;](Cerneala.UI.Controls.SceneSpatialResidency2D_T_.md)
- [RenderSurface2D](Cerneala.UI.Controls.RenderSurface2D.md)
- [Scene2D](Cerneala.UI.Controls.Scene2D.md)
