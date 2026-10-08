# TimbreParameter&lt;T&gt; Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/TimbreParameter.cs`

Typed descriptor of a value exposed by a clip and fed into one or more modifier inputs.

```csharp
public sealed class TimbreParameter<T> : TimbreParameter where T : struct
```

Inheritance:
`object` -> `TimbreParameter` -> `TimbreParameter<T>`

## Examples

```csharp
using Cerneala.Timbre;

var toneCutoff = new TimbreParameter<float>(name: "ToneCutoff", defaultValue: 1200f);
var clip = new TimbreClip("audio/confirm.wav", parameters: [toneCutoff], modifiers: [new LowPass(cutoff: toneCutoff)]);

TimbrePlayback playback = sounds.Play(clip, start => start.Set(toneCutoff, 800f));
playback.Set(toneCutoff, 6000f);
```

## Remarks

The first release supports `float` only; any other `T` throws `NotSupportedException`. `name` must be a non-empty identifier (letters, digits, underscore; not starting with a digit). `defaultValue` must be finite.

A parameter has no range of its own. When it feeds modifier inputs, its effective range is the intersection of their catalog ranges; the clip constructor validates the default against it, and every start override or runtime [TimbrePlayback.Set](Cerneala.Timbre.TimbrePlayback.md) is validated against it. Out-of-range, NaN, or infinite values are rejected, never clamped.

Values set on one playback never change the clip, the descriptor, or other playbacks.

## Constructors

| Name | Description |
| --- | --- |
| `TimbreParameter(string name, T defaultValue)` | Creates a descriptor. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `DefaultValue` | `T` | Value used when a playback does not override it. |
| `Name` | `string` | Inherited. |
| `ValueType` | `Type` | Inherited; `typeof(T)`. |

## See also

- [TimbreInput&lt;T&gt;](Cerneala.Timbre.TimbreInput_T_.md)
- [TimbreStartOptions](Cerneala.Timbre.TimbreStartOptions.md)
