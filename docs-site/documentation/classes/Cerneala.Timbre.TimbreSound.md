# TimbreSound Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/TimbreSound.cs`

Immutable sound definition: a source, default volume and loop, a loading policy, declared parameters, and an ordered chain of modifiers.

```csharp
public sealed class TimbreSound
```

Inheritance:
`object` -> `TimbreSound`

## Examples

```csharp
using Cerneala.Timbre;

var plainTimbre = new TimbreSound("audio/confirm.wav");

var toneCutoff = new TimbreParameter<float>(name: "ToneCutoff", defaultValue: 1200f);
var filteredTimbre = new TimbreSound(
    source: "audio/confirm.wav",
    parameters: [toneCutoff],
    modifiers: [new LowPass(cutoff: toneCutoff)]);

var echo = new TimbreSound(
    "audio/confirm.wav",
    modifiers: [new LowPass(cutoff: 1200f), new Delay(time: 0.12f, feedback: 0.2f, mix: 0.15f)]);
```

## Remarks

Creating or referencing a clip does not play it, open an output device, open the source, or perform I/O. Playback starts only through [TimbreScope.Play](Cerneala.Timbre.TimbreScope.md).

A clip can be shared by any number of playbacks and scopes. It holds no playhead, reader, or DSP state; each [TimbrePlayback](Cerneala.Timbre.TimbrePlayback.md) owns its own reader, position, parameter values, and modifier state. The `parameters` and `modifiers` sequences are copied at construction, so later changes to the caller's collections do not alter the clip.

Modifiers run in declaration order, followed by `Volume` as a linear post-chain gain. A clip without modifiers uses the same playback and mix path without a DSP chain.

Validation happens in the constructor:

- `source` must not be `null`.
- `volume` must be finite and within 0–1; `loading` must be a defined value.
- Parameters must be non-null, distinct instances with distinct names.
- `parameters` cannot contain [TimbrePlayback.VolumeParameter](Cerneala.Timbre.TimbrePlayback.md): Volume is intrinsic to every playback (`ArgumentException`).
- Every parameter used by a modifier must be declared in `parameters`. A parameter's default must lie inside the range of every modifier input it feeds.
- Modifier constants are validated by the modifier constructors.

`Loop` is the immutable default; a start can override it through [TimbreStartOptions.Loop](Cerneala.Timbre.TimbreStartOptions.md). A looping playback repeats the whole source until it is canceled, replaced, detached, or fails.

## Constructors

| Name | Description |
| --- | --- |
| `TimbreSound(TimbreSource source, float volume = 1f, bool loop = false, TimbreLoading loading = TimbreLoading.Auto, IEnumerable<TimbreParameter>? parameters = null, IEnumerable<TimbreModifier>? modifiers = null)` | Creates a validated, immutable definition. A `string` converts implicitly to a file [TimbreSource](Cerneala.Timbre.TimbreSource.md). |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Source` | `TimbreSource` | The audio source opened once per playback. |
| `Volume` | `float` | Default linear gain, 0–1, applied after the modifier chain. |
| `Loop` | `bool` | Default loop option captured by each start. |
| `Loading` | `TimbreLoading` | Preload or streaming policy. |
| `Parameters` | `IReadOnlyList<TimbreParameter>` | Declared typed parameters. |
| `Modifiers` | `IReadOnlyList<TimbreModifier>` | Ordered modifier chain. |

## Applies to

`Cerneala` core (`net8.0`). The engine is backend-neutral and does not depend on UI, markup, Aspect, or SDL.

## See also

- [TimbreParameter&lt;T&gt;](Cerneala.Timbre.TimbreParameter_T_.md)
- [LowPass](Cerneala.Timbre.LowPass.md)
- [Delay](Cerneala.Timbre.Delay.md)
- [TimbreScope](Cerneala.Timbre.TimbreScope.md)
