# Delay Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/SoundModifier.cs`

Feedback echo with linear dry/wet mix.

```csharp
public sealed class Delay : SoundModifier
```

Inheritance:
`object` -> `SoundModifier` -> `Delay`

## Examples

```csharp
using Cerneala.Timbre;

var echoMix = new SoundParameter<float>("EchoMix", 0.15f);
var clip = new SoundClip(
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

Per channel, with `D = round(Time · 48000)` frames: `w[n] = x[n] + Feedback · w[n − D]` and `y[n] = (1 − Mix) · x[n] + Mix · w[n − D]`. `Mix = 0` is fully dry and `Mix = 1` fully wet. Channels are independent; there is no ping-pong. Changing `Time` during playback moves the read position immediately without interpolation; such changes are not guaranteed click-free. The delay line holds `D` frames for a constant time, or two seconds when `Time` is a parameter. Line values below `1e-20` are stored as zero so long silences never run on subnormal floats.

After a non-looping source ends, the chain keeps processing silence. The tail ends once the pre-volume chain output stays below `1e-6` for one full echo period (at least 480 frames), or after [SoundRuntimeOptions.DelayTailCap](Cerneala.Timbre.SoundRuntimeOptions.md) (30 seconds by default); a capped tail completes with [SoundPlaybackResult.TailTruncated](Cerneala.Timbre.SoundPlaybackResult.md) set. Pause freezes the tail, cancel stops it, a successful seek clears the delay line, and natural loop repetition keeps it, so echoes continue across the loop boundary.

## Constructors

| Name | Description |
| --- | --- |
| `Delay(SoundInput<float>? time = null, SoundInput<float>? feedback = null, SoundInput<float>? mix = null)` | `null` selects each default. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Time` | `SoundInput<float>` | Delay time in seconds. |
| `Feedback` | `SoundInput<float>` | Fraction of the delayed signal fed back. |
| `Mix` | `SoundInput<float>` | Wet proportion of the output. |

## See also

- [LowPass](Cerneala.Timbre.LowPass.md)
- [SoundClip](Cerneala.Timbre.SoundClip.md)
