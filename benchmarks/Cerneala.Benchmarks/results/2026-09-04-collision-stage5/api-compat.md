# Collision and picking API compatibility

Date: 2026-09-04

## Baseline

The strict comparison uses the detached Servo baseline at commit
`fed724b954bc2823c4799db69c94b92e2790b2b5` and composes the previously audited
Servo, RenderSurface2D scene-foundation, and TileMap2D suppression files.

- Baseline assembly: `C:\Users\lauri\Desktop\Cerneala-baseline-servo-fed724b\bin\Release\net8.0\Cerneala.dll`
- Baseline size: 5,031,936 bytes
- Baseline SHA-256: `2D20937BFC0783CEC52CB8F8328C4F27625E56C99A808C736C28FD822B8297F1`
- Current assembly: `C:\Users\lauri\Desktop\Cerneala\bin\Release\net8.0\Cerneala.dll`
- Current size: 5,242,368 bytes
- Current SHA-256: `1D7E8C683E6929BC994774230D77F9B34BDB8D5A73A9838282B0750DA7B0A1E9`

## Approved additions

`api-compat.suppressions.xml` contains only the collision-plan surface:

- nine new public collision types: `Collider2D`, `BoxCollider2D`,
  `CircleCollider2D`, `PolygonCollider2D`, `CollisionWorld2D`,
  `CollisionQuery2D`, `CollisionHit2D`, `MoveCollisionResult2D`, and
  `CollisionWorld2DDiagnosticsSnapshot`;
- two tile-collider descriptor types: `TileColliderDescriptor2D` and
  `TileColliderShape2D`;
- `Scene2D.CollisionWorld`;
- `RenderSurface2D.TryRootToScene`, `RenderSurface2D.SceneToRoot`, and
  `MouseEventArgs.GetPosition`.

Suppressing a newly added type covers its members because that type is absent
from the baseline. The file does not suppress a removal, signature change, or
unrelated API difference. `PermitUnnecessarySuppressions` is `false`.

## Strict gate

```powershell
dotnet msbuild .\benchmarks\Cerneala.Benchmarks\results\2026-09-04-collision-stage5\api-compat.proj -t:Compare -v:minimal
```

Result: exit code 0 with strict mode and parameter-name checks enabled; no
unsuppressed or unnecessary difference was reported.
