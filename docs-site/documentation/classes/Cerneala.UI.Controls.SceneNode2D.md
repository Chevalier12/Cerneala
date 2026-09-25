# SceneNode2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/SceneNode2D.cs`

Provides the logical and invalidation base for retained nodes recorded by a `RenderSurface2D`.

```csharp
public abstract class SceneNode2D : UIElement
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `SceneNode2D`

## Examples

```xml
<RenderSurface2D.Scene>
    <Scene2D OrderMode="Layer">
        <Sprite2D Layer="10" />
    </Scene2D>
</RenderSurface2D.Scene>
```

## Remarks

Scene nodes are logical children, not visual layout children. They reuse inherited `DataContext`, generated bindings, UI properties, attachment, Aspect, Motion, and invalidation, while their drawing is recorded into the owning surface's command stream.

`SimulationContext` identifies their current spatial/simulation owner. An attached surface supplies a context backed by its existing root relay. An independent [SceneSimulationContext2D](Cerneala.UI.Controls.SceneSimulationContext2D.md) supports source preparation, data bindings, and collisions without setting `Root`, raising UI lifecycle events, or enabling UI Motion/Aspect/rendering. Direct mutations use the current owner's thread; dispose an independent root context before UI attachment or reparenting.

Owned scene nodes also participate in the same retained input route as visual UI elements: `UIRoot` -> visual ancestors -> `RenderSurface2D` -> scene groups -> routed target. They reuse inherited mouse, wheel, capture, focus, keyboard, text, command, `Handled`, and `handledEventsToo` behavior. This logical input subtree does not add the nodes to `VisualChildren` or UI layout.

Changing or animating a UI property on an owned node invalidates the surface. A node's local Aspect is processed when the node attaches and when the Aspect is invalidated. `IsVisible` and `Visibility` control participation in scene recording. Built-in concrete nodes include `Scene2D`, `SceneItems2D`, `Sprite2D`, and `TileMap2D`.

`SceneNode2D` is a framework base for the built-in scene node types. Its recording contract is internal, so applications compose the provided nodes rather than implement new recording primitives outside the Cerneala assembly. Applications can derive from `Scene2D` to package that composition, including a paired `.crn` + `.crn.cs` component used as `<local:HouseView />`; see [Scene2D](Cerneala.UI.Controls.Scene2D.md).

`Layer` is an integer scene-order key interpreted by the containing `Scene2D`. It has no effect while that parent uses `SceneOrderMode.Source`. In `Layer` and `LayerThenY` modes, smaller values are recorded first. Equal keys retain the source collection order. `Layer` is distinct from `Sprite2D.LayerDepth`, which is only forwarded to the drawing backend.

Geometric picking walks the effective draw order in reverse. A `Sprite2D` can own one optional collider whose enabled geometry targets that owner. Known visual bounds can also participate in picking; attaching a collider does not remove the visual-bounds fallback. Unknown bounds are not replaced with an invented rectangle. `IsHitTestVisible`, UI `IsEnabled`, visibility, transforms, and the owning surface clip apply before the node can become a target. Opacity and Prism remain presentation-only for hit testing. Scene groups cannot own live collider nodes directly.

Required cold sprite images and live path-backed images used by scene Prism scopes are prepared through the root's asynchronous image cache before scene submission. The owning [RenderSurface2D](Cerneala.UI.Controls.RenderSurface2D.md#scene-preparation-and-input-availability) withholds the entire scene and its input routes while required presentation is loading or failed, without detaching nodes, freezing animation, or disabling collision participation. This presentation availability guard is separate from the geometric picking rules.

### Finite Prism input domains

`PrismInputDomain` declares the complete input rectangle for an active non-pointwise
Prism composition owned by this node. Its default is `null`. A non-null rectangle
must have finite edges and positive width and height; invalid values are rejected.
It uses the owning node's local content coordinates, before its scene transform
(including a sprite's anchor translation and rotation). This is not a camera
rectangle, an output clip, a collision region or a memory budget.

```csharp
// scene owns the global effect; these coordinates are local to that scene.
scene.PrismInputDomain = new DrawRect(0, 0, 2048, 1024);
```

In a hosted scene containing spatial materializers, each active composition
without supported automatic input selection requires a domain on its own Prism
owner. This includes `Threshold`, `Levels` with live `Auto = true`, the seven
non-pointwise styles, `Wrap`/`Mirror` edge modes and other unclassified filters.
See [the exact automatic coverage](Cerneala.UI.Controls.Scene2D.md#prism-input-and-streamed-children).
An ancestor's declaration does not satisfy a separate nested composition that
requires its own domain, even when the nested owner is a single sprite.
A missing domain sets the surface's `PresentationState` to `Error` with an
`InvalidOperationException` naming `PrismInputDomain`; it does not silently load
an entire prepared map. Declaring a valid domain allows ordinary preparation and
readiness checks to resume. Hidden layers, groups, filters and styles do not
require it; zero-opacity layers, groups and filters are also inactive. A style's
own effect-specific opacity parameter does not remove the domain requirement.
An independent, nonvisual simulation context does not prepare Prism presentation.

The complete declared input is prepared and recorded, including required
off-camera map payloads and images. Scene input selection transforms that domain into
each descendant node's coordinates. Backend input capture is independent
of the final surface/camera clip: clipping happens after the effect, so narrowing
the camera does not change an input-wide distribution. Nested scopes retain their
own logical coordinate basis. Changing the domain re-evaluates presentation
interest; clearing a required domain reports the error again. Simulation and
explicit collision interests remain independent and can retain data outside it.

An ancestor's declaration selects necessary descendant content; it does not
replace each descendant composition's own input boundary. A nested scope with
known content bounds captures only their intersection with the required input.
A declaration on that nested scope's own non-pointwise owner defines its complete
input instead, including any deliberately declared transparent area.

The property can also declare the complete input of a supported local
composition instead of automatic camera-neighborhood selection. Pointwise-only
compositions keep their ordinary logical bounds and
viewport interest; declaring this property alone does not expand them. See
[Scene2D's input rules](Cerneala.UI.Controls.Scene2D.md#prism-input-and-streamed-children)
for the current classification and remaining automatic-neighborhood limits.
`SceneItems2D` has no composition scope of its own: put the effect and its domain
on the containing scene or on a templated node, not on the materializer.

A finite domain need not fit a texture or the backend's hard allocation budget.
Unrepresentable raster extents are rejected, and allocation failure in this path
is surfaced rather than removing an effect, reducing resolution or increasing
the budget. This does not provide tiled/out-of-core Prism execution. Scenes with
no spatial materializers and no declared domain retain their prior capture rules.

### Prism allocation failures

When a recorded frame includes node-owned Prism scopes in a scene containing
`SceneItems2D` or `TileMap2D`, SDL_GPU surface-allocation failure propagates as an
`InvalidOperationException` instead of drawing the remaining commands without
their composition. The failure includes requested, current and hard-limit byte
counts; it occurs during backend rendering, not asynchronous map/image preparation.
Nested scopes retain this policy, which applies to Prism execution for that
frame. Frames without these spatial-scene scopes keep their existing allocation
fallback. The policy does not cover an effect attached only to an ordinary UI
ancestor. A finite declaration is not a memory budget: its complete input still
has to fit the backend's limits.

Aspect can assign `Layer` as structural state. Motion cannot animate it because Cerneala has no interpolation contract for structural scene order; generated markup reports a diagnostic instead of silently applying a fallback.

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `LayerProperty` | `UiProperty<int>` | Identifies the `Layer` UI property. Changes affect rendering and ordering. |
| `PrismInputDomainProperty` | `UiProperty<DrawRect?>` | Identifies the optional finite Prism input domain. Changes affect rendering. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Layer` | `int` | Gets or sets the parent-scene ordering layer. The default is `0`. |
| `PrismInputDomain` | `DrawRect?` | Complete node-local input of an active non-pointwise composition; initially null. Required on each owner whose active composition lacks supported automatic input selection in a hosted spatial scene. |
| `SimulationContext` | `SceneSimulationContext2D?` | Current common spatial owner, or null outside a hosted/independent simulation lifecycle. Read-only; inherited by concrete scene nodes. |

## Applies to

Project: `Cerneala`

## See also

- `RenderSurface2D`
- `Scene2D`
- `SceneItems2D`
- `SceneOrderMode`
- `Sprite2D`
- `MouseEventArgs`
