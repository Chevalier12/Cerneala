# RenderSurface2D scene-foundation API compatibility

Date: 2026-09-04

## Baseline

The strict comparison reuses the detached Servo baseline at commit
`fed724b954bc2823c4799db69c94b92e2790b2b5` and its separately audited
historical suppression file.

- Baseline assembly: `C:\Users\lauri\Desktop\Cerneala-baseline-servo-fed724b\bin\Release\net8.0\Cerneala.dll`
- Baseline size: `5,031,936` bytes
- Baseline SHA-256: `2D20937BFC0783CEC52CB8F8328C4F27625E56C99A808C736C28FD822B8297F1`
- Current assembly: `C:\Users\lauri\Desktop\Cerneala\bin\Release\net8.0\Cerneala.dll`
- Current size: `5,097,984` bytes
- Current SHA-256: `E601DE113AB46C91537FF75F37BDBEAC2764954F279F6FACC4FF801FD7525F5C`

## Approved plan additions

Strict API Compat reports additive API as differences. The local suppression
file contains exactly the 12 additions frozen by this plan:

- the `SceneOrderMode` public enum;
- `SceneNode2D.LayerProperty` and the `Layer` getter/setter;
- `Scene2D.OrderModeProperty` and the `OrderMode` getter/setter;
- `Scene2D.TransformOriginProperty` and the `TransformOrigin` getter/setter;
- the `Sprite2D.SourceResourceId` getter/setter.

No removal, signature change, or unrelated addition is suppressed by the plan
file. `PermitUnnecessarySuppressions` is `false`.

## Strict gate

```powershell
dotnet build .\Cerneala.csproj -c Release
dotnet msbuild .\benchmarks\results\2026-09-04-rendersurface2d-scene-foundation\api-compat.proj -t:Compare -v:minimal
```

Result: both commands exited `0`; the Release build produced zero warnings and
zero errors, and strict API Compat reported no unsuppressed difference.
