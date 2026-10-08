# LowPass Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/TimbreModifier.cs`

Two-pole Butterworth low-pass filter with a single cutoff input.

```csharp
public sealed class LowPass : TimbreModifier
```

Inheritance:
`object` -> `TimbreModifier` -> `LowPass`

## Examples

```csharp
using Cerneala.Timbre;

var cutoff = new TimbreParameter<float>("ToneCutoff", 1200f);
var clip = new TimbreClip("audio/confirm.wav", parameters: [cutoff], modifiers: [new LowPass(cutoff: cutoff)]);
var fixedFilter = new LowPass(cutoff: 800f);
```

## Remarks

`Cutoff` is in hertz, range 20–20000, default 1200. Constants outside the range, NaN, or infinity throw `ArgumentOutOfRangeException`. There are no Q, resonance, slope, or bypass inputs.

The filter is a topology-preserving-transform state-variable filter with damping `k = √2` and `g = tan(π · Cutoff / 48000)`. Its magnitude response equals the bilinear-transformed 2-pole Butterworth: `|H(e^{jω})|² = 1 / (1 + (tan(ω/2) / tan(ωc/2))⁴)`, unity at DC and −3 dB at the cutoff. The structure stays stable while the cutoff is changed between blocks. Each channel has independent state per playback. A successful manual seek resets the state; pause/resume and natural loop repetition preserve it. Filter state whose magnitude falls below `1e-20` is set to zero at the end of each block so long silences never run on subnormal floats; output samples are never limited.

## Constructors

| Name | Description |
| --- | --- |
| `LowPass(TimbreInput<float>? cutoff = null)` | `null` selects the default cutoff of 1200 Hz. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Cutoff` | `TimbreInput<float>` | Cutoff frequency in Hz. |

## See also

- [Delay](Cerneala.Timbre.Delay.md)
- [TimbreClip](Cerneala.Timbre.TimbreClip.md)
