# RenderSurface2DPresentationState Enum

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/RenderSurface2D.Presentation.cs`

Describes the observed preparation state of a retained scene's required visible coverage.

```csharp
public enum RenderSurface2DPresentationState
```

## Fields

| Name | Value | Description |
| --- | --- | --- |
| `Loading` | `0` | Required visible spatial coverage or sprite/Prism images are not ready. Scene submission and scene input are withheld. |
| `Ready` | `1` | The current coverage check found no missing required spatial identities or sprite/Prism images. |
| `Error` | `2` | A preparation failure prevents required visible coverage; see the surface's `PresentationError`. |

## Remarks

[RenderSurface2D](Cerneala.UI.Controls.RenderSurface2D.md) exposes the state through a read-only UI property, initially `Ready` for an empty surface. State observation occurs in the normal surface update, recording, and input availability paths. Application composition provides loading/error visuals; this enum does not select a built-in overlay.

Loading or failure withholds the entire retained scene rather than progressively drawing ready siblings or retaining a stale scene image. Imperative surface drawing, ordinary UI, and attached animation/simulation remain independent. Existing scene pointer capture and keyboard focus become unroutable; they are not automatically restored on recovery.

The current implementation checks `SceneItems2D` payload/template coverage, asynchronous `Sprite2D` images, and live scene Prism image resources. Source-backed tile presentation and asynchronous tile atlas preparation are not yet integrated. `Ready` is not a promise about these unfinished paths, all offscreen simulation data, a later catalog/camera/image revision, or collision-query coverage. Use the collision world's explicit region-preparation contract for required distant queries. A pending or failed offscreen load alone does not turn a ready visible scene into `Loading` or `Error`; unknown natural bounds and Prism effect influence can conservatively require off-camera input.

## See also

- [RenderSurface2D](Cerneala.UI.Controls.RenderSurface2D.md)
- [SceneItems2D](Cerneala.UI.Controls.SceneItems2D.md)
- [CollisionWorld2D](Cerneala.UI.Controls.CollisionWorld2D.md)
