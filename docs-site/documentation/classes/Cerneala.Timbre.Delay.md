# Delay Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/TimbreModifier.cs`

Feedback echo with linear dry/wet mix.

```csharp
public sealed class Delay : TimbreModifier
```

Inheritance:
`object` -> `TimbreModifier` -> `Delay`

## Examples

```csharp
using Cerneala.Timbre;

var echoMix = new TimbreParameter<float>("EchoMix", 0.15f);
var clip = new TimbreSound(
    "audio/confirm.wav",
    parameters: [echoMix],
    modifiers: [new Delay(time: 0.12f, feedback: 0.20f, mix: echoMix)]);
```

## Remarks

| Input | Unit | Range | Default |
| --- | --- | --- | --- |
| `Time` | seconds | 0.001–2 | 0.12 |
| `Feedback` | ratio | 0–0.95 | 0.20 |
| `Mix` | linear dry/wet | 0–1 | 0.15 |

Constants outside a range, NaN, or infinity throw `ArgumentOutOfRangeException`.

Per channel, with `D = round(Time · 48000)` frames: `w[n] = x[n] + Feedback · w[n − D]` and `y[n] = (1 − Mix) · x[n] + Mix · w[n − D]`. `Mix = 0` is fully dry and `Mix = 1` fully wet. Channels are independent; there is no ping-pong. Changing `Time` during playback moves the read position immediately without interpolation; such changes are not guaranteed click-free. The delay line holds `D` frames for a constant time, or two seconds when `Time` is a parameter. Line values below `1e-20` are stored as zero so long silences never run on subnormal floats. Line values are not limited: the line can reach `1 / (1 − Feedback)` times the input peak (20 times at `Feedback = 0.95`), so finite input above about `1.7e37` can overflow it. A playback whose chain output becomes NaN or infinity fails with `TimbreErrorKind.InvalidData` (see [TimbreRuntime](Cerneala.Timbre.TimbreRuntime.md)).

After a non-looping source ends, the chain keeps processing silence. The tail ends once the pre-volume chain output stays below `1e-6` for one full echo period (at least 480 frames), or after [TimbreRuntimeOptions.DelayTailCap](Cerneala.Timbre.TimbreRuntimeOptions.md) (30 seconds by default); a capped tail completes with [TimbrePlaybackResult.TailTruncated](Cerneala.Timbre.TimbrePlaybackResult.md) set. Pause and cancel continue processing the tail through a 5 ms post-chain fade-out, then respectively freeze or release it. Resume fades in over 5 ms from the preserved state. A successful seek fades out the old position, clears the delay line, and fades in the new position; no old DSP tail crosses the seek. Natural loop repetition keeps the delay line, so echoes continue across the loop boundary.

## Constructors

| Name | Description |
| --- | --- |
| `Delay(TimbreInput<float>? time = null, TimbreInput<float>? feedback = null, TimbreInput<float>? mix = null)` | `null` selects each default. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Time` | `TimbreInput<float>` | Delay time in seconds. |
| `Feedback` | `TimbreInput<float>` | Fraction of the delayed signal fed back. |
| `Mix` | `TimbreInput<float>` | Wet proportion of the output. |

## See also

- [LowPass](Cerneala.Timbre.LowPass.md)
- [TimbreSound](Cerneala.Timbre.TimbreSound.md)
