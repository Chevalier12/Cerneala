# SceneCollisionRegion2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/CollisionWorld2D.Streaming.cs`

Owns one prepared collision-region interest in a root scene.

```csharp
public sealed class SceneCollisionRegion2D : IDisposable
```

## Examples

```csharp
using SceneCollisionRegion2D region = await scene.CollisionWorld.PrepareRegionAsync(
    new DrawRect(2000, 0, 100, 100), cancellationToken);
bool ready = region.IsReady;
```

## Remarks

Obtain instances from [CollisionWorld2D.PrepareRegionAsync](Cerneala.UI.Controls.CollisionWorld2D.md). There is no public constructor. `Bounds` is the requested rectangle in collision-world scene coordinates; it is not transformed into a source's local coordinates in this property.

The region keeps collision interest alive, not an immutable snapshot of geometry. Map replacement, chunk loading and scene-items publication failures can change `IsReady`. Collision queries always check their actual envelope again and can throw even when a different or previously prepared rectangle was ready. Reading live readiness requires the simulation owner's thread, whether UI-hosted or independent. Readiness is false after disposal or loss of that context; reattachment does not revive this region.

Disposal is idempotent. Other prepared regions, marked active colliders, and camera interest can keep map chunks resident. Worker-thread disposal invalidates this region immediately and relays tree retirement to the same simulation owner; its UI frame loop or independent `Update` loop must continue draining that work. Context disposal also retires all interests, so late completion does not require pumping an already-disposed owner.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Bounds` | `DrawRect` | Original finite scene-coordinate rectangle, with nonnegative dimensions. |
| `IsReady` | `bool` | Whether required collision chunks and scene-items publication are ready for this live region. False after disposal or detach. |

## Methods

| Name | Description |
| --- | --- |
| `Dispose()` | Releases this interest once; permits retirement when no other interest needs its payloads. |

## See also

- [CollisionWorld2D](Cerneala.UI.Controls.CollisionWorld2D.md)
- [SceneCollisionRegionNotReadyException](Cerneala.UI.Controls.SceneCollisionRegionNotReadyException.md)
- [Collider2D](Cerneala.UI.Controls.Collider2D.md)
