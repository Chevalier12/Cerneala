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

Owned scene nodes also participate in the same retained input route as visual UI elements: `UIRoot` -> visual ancestors -> `RenderSurface2D` -> scene groups -> routed target. They reuse inherited mouse, wheel, capture, focus, keyboard, text, command, `Handled`, and `handledEventsToo` behavior. This logical input subtree does not add the nodes to `VisualChildren` or UI layout.

Changing or animating a UI property on an owned node invalidates the surface. A node's local Aspect is processed when the node attaches and when the Aspect is invalidated. `IsVisible` and `Visibility` control participation in scene recording. The built-in concrete nodes are `Scene2D`, `SceneItems2D`, and `Sprite2D`.

`SceneNode2D` is a framework base for the built-in scene node types. Its recording contract is internal, so applications compose the provided nodes rather than implement new node types outside the Cerneala assembly.

`Layer` is an integer scene-order key interpreted by the containing `Scene2D`. It has no effect while that parent uses `SceneOrderMode.Source`. In `Layer` and `LayerThenY` modes, smaller values are recorded first. Equal keys retain the source collection order. `Layer` is distinct from `Sprite2D.LayerDepth`, which is only forwarded to the drawing backend.

Geometric picking walks the effective draw order in reverse. A node with direct collider children uses their union; otherwise a visual node can use its known exact local bounds. Unknown bounds are not replaced with an invented rectangle. `IsHitTestVisible`, UI `IsEnabled`, visibility, transforms, and the owning surface clip apply before the node can become a target. Opacity and Prism remain presentation-only for hit testing.

Aspect can assign `Layer` as structural state. Motion cannot animate it because Cerneala has no interpolation contract for structural scene order; generated markup reports a diagnostic instead of silently applying a fallback.

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `LayerProperty` | `UiProperty<int>` | Identifies the `Layer` UI property. Changes affect rendering and ordering. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Layer` | `int` | Gets or sets the parent-scene ordering layer. The default is `0`. |

## Applies to

Project: `Cerneala`

## See also

- `RenderSurface2D`
- `Scene2D`
- `SceneItems2D`
- `SceneOrderMode`
- `Sprite2D`
- `MouseEventArgs`
