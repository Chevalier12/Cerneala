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

The lease keeps collision interest alive, not an immutable snapshot of geometry. Source, template, and payload revisions can make `IsReady` false while replacements load. Collision queries always check their actual envelope again and can throw even when a different or previously prepared rectangle was ready. Reading live readiness requires the simulation owner's thread, whether UI-hosted or independent. Readiness is false after disposal or loss of that context; reattachment does not revive this lease.

Disposal is idempotent. Other prepared regions, active simulation, and camera interest can keep shared payloads resident. Worker-thread disposal invalidates this lease immediately and relays tree retirement to the same simulation owner; its UI frame loop or independent `Update` loop must continue draining that work. Context disposal also retires all interests, so late completion does not require pumping an already-disposed owner.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Bounds` | `DrawRect` | Original finite scene-coordinate rectangle, with nonnegative dimensions. |
| `IsReady` | `bool` | Whether the live region currently has all collision payloads described by participating sources. False after disposal or detach. |

## Methods

| Name | Description |
| --- | --- |
| `Dispose()` | Releases this interest once; permits retirement when no other interest needs its payloads. |

## See also

- [CollisionWorld2D](Cerneala.UI.Controls.CollisionWorld2D.md)
- [SceneCollisionRegionNotReadyException](Cerneala.UI.Controls.SceneCollisionRegionNotReadyException.md)
- [SceneSpatialEntry2D](Cerneala.UI.Controls.SceneSpatialEntry2D.md)
