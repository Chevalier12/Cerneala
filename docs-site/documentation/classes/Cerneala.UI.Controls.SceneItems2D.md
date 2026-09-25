# SceneItems2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/SceneItems2D.cs`

Materializes an ordinary collection of models as retained scene nodes under a `Scene2D`.

```csharp
public sealed class SceneItems2D : SceneNode2D
```

Inheritance: `object` -> `UiObject` -> `UIElement` -> `SceneNode2D` -> `SceneItems2D`

## Examples

```csharp
var models = new ObservableCollection<Scene2DEntity>();
var items = new SceneItems2D();
items.Templates.Add(new ContentTemplate<Scene2DEntity>("actor", null, 0,
    context => new Sprite2D
    {
        X = context.Data!.Position.X,
        Y = context.Data.Position.Y,
        Width = 16,
        Height = 16
    }));
items.ItemsSource = models;
var scene = new Scene2D();
scene.Children.Add(items);
```

Populate `models` with separately loaded `Scene2DEntity` values. The template must return a distinct `SceneNode2D` for each occurrence. A sprite still needs an `Image` to draw. In `.crn` markup, declare templates with `@templates { ... }` and bind `ItemsSource` to an ordinary enumerable, including an observable collection.

## Remarks

### Snapshot, identity and templates

`ItemsSource` accepts `IEnumerable`, including `ObservableCollection<T>`. The control enumerates it once when assigned, even before attachment, and does not re-enumerate a plain enumerable on every frame or on detach/reattach. A silent mutation is not visible until `Refresh()` or rebind. `Refresh()` synchronously re-enumerates and resets identity; a one-shot source must support another enumeration if refreshed. An observable source is re-enumerated once after each collection notification and on reattach to recover edits made while detached. Only one observable subscription is active while the control has a simulation context.

The sequence contains ordered **occurrences**, not unique model identities. Repeated references and `null` are not silently discarded. For valid `Add`, `Remove`, `Move` and `Replace` notifications, unaffected occurrences keep their nodes; `Move` moves the same nodes. `Reset`, `Refresh()`, source replacement and template changes rebuild the realizations. Invalid or inconsistent delta indices fall back to a full reset. This is eager model realization, not viewport-only creation or lazy loading for an arbitrary enumerable.

Each realized node receives its occurrence value as `DataContext`. Template matching and creation use `ContentTemplateContext.Index = -1`; index is not a reactive collection position. A matching template must return a `SceneNode2D`. Without a match, a value that already is a `SceneNode2D` can be used directly. `null` needs a template that accepts it. No matching template, a null/wrong-type result, a node returned for two occurrences, or a node already owned elsewhere raises `InvalidOperationException` from control validation. The exact message is not contractual. An application template-factory exception normally propagates unchanged; if retiring an uncommitted candidate also fails, the failures are aggregated without losing the primary error. The control does not create a second input tree or transfer image/data ownership from the application.

The materializer's bounds come from its realized children, not unpublished model metadata. Children remain logical scene nodes even when off camera; their normal scene/input/collision lifecycle applies. Recording may skip off-camera child work and retire unused render/image acquisitions. No automatic bounds, collision envelope, or pre-load terrain interest can be inferred from an arbitrary model before its node exists. For an already-realized actor that needs terrain kept near its own collider geometry, set [Collider2D.IsSimulated](Cerneala.UI.Controls.Collider2D.md) on that collider. For a still-unloaded actor, manage an explicit collision region instead.

### Threading, failure and lifecycle

Assign `ItemsSource`, edit templates, call `Refresh()`, and raise attached observable notifications on the control's owner thread. Before first attachment, assignment and enumeration use the constructing thread. After the first attachment, the owner rule continues while detached. Use `UiRelay` to make a collection mutation on the UI owner; the control does not auto-marshal an already-mutated arbitrary collection from a worker thread.

Enumeration and template creation are staged before changing `LogicalChildren`. If preflight fails, the exception reaches the caller and the previously committed nodes remain; the requested property value may already have changed. The latest successfully enumerated requested snapshot is cached separately from the committed tree. When a complete enumeration of the current request is available, a template-only edit rebuilds from it without re-enumeration, including when deferred from a reentrant child-tree notification; source replacement, collection notifications and `Refresh()` still request enumeration. If template creation fails, a later template edit can retry the requested snapshot without consuming a one-shot enumerable again. If enumeration itself fails, there is no complete requested snapshot to retry: a template edit does not enumerate again or mark the old committed tree ready. `Refresh()` explicitly retries enumeration. Collision preparation does not report an uncommitted new snapshot as ready. Reentrant changes during preflight invalidate the older candidate, so it cannot publish late.

Once structural attach/detach or collection callbacks have begun, an exception can leave external lifecycle side effects that this control cannot roll back. The instance then enters a terminal structural fault: it stops publishing new generations, aligns `RealizedItemCount` with observable logical membership, retires local candidates best-effort, and propagates the primary failure together with any cleanup failures. Replace the control; `Refresh()` and rebind do not repair that terminal state. Normal detach removes realized nodes and the observable subscription. A plain source keeps its snapshot for reattach; an observable source is re-enumerated on reattach.

`RealizedItemCount` counts the nodes this instance currently owns in `LogicalChildren`, including after an interrupted structural commit. It is not a viewport count. The control exposes no asynchronous `Preparation` or `PreparationError`: collection enumeration and `Refresh()` are synchronous, not package I/O retries.

## Constructors

| Name | Description |
| --- | --- |
| `SceneItems2D()` | Creates an empty materializer and template collection. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `ItemsSourceProperty` | `UiProperty<IEnumerable?>` | Identifies the items-source UI property. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ItemsSource` | `IEnumerable?` | Source sequence; initially `null`. |
| `Templates` | `Collection<ContentTemplate>` | Templates resolved against collection values. |
| `RealizedItemCount` | `int` | Current logical-child count for this materializer. |

## Methods

| Name | Description |
| --- | --- |
| `Refresh()` | Synchronously re-enumerates the current source and resets occurrence identity on the owner thread. |

## Property Information

| Property | Identifier field | Default | Metadata/options |
| --- | --- | --- | --- |
| `ItemsSource` | `ItemsSourceProperty` | `null` | `AffectsRender` |

## See also

- [Scene2D](Cerneala.UI.Controls.Scene2D.md)
- [Sprite2D](Cerneala.UI.Controls.Sprite2D.md)
- [Collider2D](Cerneala.UI.Controls.Collider2D.md)
- [SceneCollisionRegion2D](Cerneala.UI.Controls.SceneCollisionRegion2D.md)
