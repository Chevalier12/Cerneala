# Sprite animation API compatibility audit

Date: 2026-09-04. Result: strict comparison PASS (exit 0).

Baseline: the preserved Servo baseline assembly at commit `fed724b954`, SHA256
`2D20937BFC0783CEC52CB8F8328C4F27625E56C99A808C736C28FD822B8297F1`.
Compared Release core assembly SHA256:
`8D416F6204BB6FE6D99ADBD3D018995CF7AB6B1D319D884B686F59D197EDCAB3`.

Reproduction:

```powershell
dotnet msbuild benchmarks/Cerneala.Benchmarks/results/2026-09-04-sprite-animation-stage4/api-compat.proj -t:Compare -v:minimal
```

Strict mode and parameter-name validation are enabled. Unnecessary suppressions
are forbidden. The Servo, scene-foundation, tilemap and collision suppressions
remain separate inputs, not copied into this plan's approval list.

The generated inventory includes all cumulative differences from that baseline.
Subtracting those four existing inventories leaves exactly 20 approved entries:

- Four added types: `SpriteAnimationFrame`, `SpriteAnimationClip`,
  `SpriteAnimationSet`, `SpriteAnimationStateChangeMode`.
- Five `Sprite2D` UI-property identifier fields, their ten get/set accessors,
  and `RestartAnimation()`.

All match the stage-0 contract. No new removal or unrelated signature change was
suppressed. `TileInstance2D` is wholly absent from the old baseline and already
covered by the tilemap type-addition suppression: that masks member-level
comparison for the type. Its five analogous fields/properties and restart method
were therefore explicitly reviewed against the frozen contract and canonical
TileInstance2D documentation; it is not claimed that ApiCompat enumerated them.
The remaining scheduler/sampler/Prism-parent additions are internal.

The suppression-generation command is discovery only; the PASS above is the
subsequent ordinary strict run with generation disabled.
