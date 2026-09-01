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
    <Scene2D>
        <Sprite2D
            Source="$DataContext.Image:OneWay"
            Destination="$DataContext.Bounds:OneWay" />
    </Scene2D>
</RenderSurface2D.Scene>
```

## Remarks

Scene nodes are logical children, not visual layout children. They reuse inherited `DataContext`, generated bindings, UI properties, attachment, Aspect, Motion, and invalidation, while their drawing is recorded into the owning surface's command stream.

Changing or animating a UI property on an owned node invalidates the surface. A node's local Aspect is processed when the node attaches and when the Aspect is invalidated. `IsVisible` and `Visibility` control participation in scene recording. The built-in concrete nodes are `Scene2D`, `SceneItems2D`, and `Sprite2D`.

`SceneNode2D` is a framework base for the built-in scene node types. Its recording contract is internal, so applications compose the provided nodes rather than implement new node types outside the Cerneala assembly.

## Applies to

Project: `Cerneala`

## See also

- `RenderSurface2D`
- `Scene2D`
- `SceneItems2D`
- `Sprite2D`
