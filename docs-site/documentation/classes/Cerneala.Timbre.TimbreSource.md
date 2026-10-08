# TimbreSource Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/TimbreSource.cs`

Describes where a clip's audio comes from: a local file, a seekable stream factory, or a factory of canonical PCM readers.

```csharp
public abstract class TimbreSource
```

Inheritance:
`object` -> `TimbreSource`

## Examples

```csharp
using Cerneala.Timbre;

TimbreSource file = "audio/confirm.wav";
TimbreSource stream = TimbreSource.FromStream(() => File.OpenRead("audio/music.ogg"), name: "music");
TimbreSource generated = TimbreSource.FromReader(() => new MyToneReader(), name: "tone");
TimbreSource budgeted = TimbreSource.FromReader(budget =>
{
    budget.Reserve(MyToneReader.TableBytes);
    return new MyToneReader();
}, name: "budgeted tone");
```

`MyToneReader` (with a `TableBytes` constant) stands for an application-defined [TimbreReader](Cerneala.Timbre.TimbreReader.md).

## Remarks

Creating a source performs no I/O. Each playback opens its own reader on a background worker; readers and positions are never shared between playbacks.

`FromFile` paths that are relative resolve against [TimbreRuntimeOptions.BaseDirectory](Cerneala.Timbre.TimbreRuntimeOptions.md), or `AppContext.BaseDirectory` when that is `null` — not the process working directory or a markup file's directory. URIs such as `http://` are rejected; non-seekable and network sources are not supported.

File and stream sources are decoded by content, not by extension, into the runtime's canonical PCM (stereo float32 at 48 kHz; mono is copied to both channels, other rates are resampled). `FromStream` requires a stream with `CanRead` and `CanSeek`; the factory is called once per playback and the runtime disposes the stream.

| Format | Supported | Not supported |
| --- | --- | --- |
| WAV | RIFF little-endian PCM 8/16/24/32-bit and IEEE float32, including `WAVE_FORMAT_EXTENSIBLE`; mono or stereo; 8–192 kHz | RIFX, RF64/BW64, compressed or float64 formats, more than two channels |
| MP3 | MPEG-1/2/2.5 Layer III, CBR and VBR, mono or stereo; ID3v2/ID3v1/APEv2 tags are skipped; a Xing/Info (LAME) tag supplies the length and gapless trimming | MPEG Layer I/II, free-format bitrates, streams that change rate or channels |
| Ogg Vorbis | one logical stream, mono or stereo, 8–192 kHz | chained or multiplexed Ogg, more than two channels |
| Ogg Opus | one logical stream, channel mapping family 0 (mono or stereo); pre-skip, end trimming and output gain are applied | other channel mappings (surround), chained or multiplexed Ogg |

Content no decoder recognizes, and the variants in the last column, fail the playback with [TimbreErrorKind.UnsupportedFormat](Cerneala.Timbre.TimbreErrorKind.md); corrupt or truncated data fails it with `InvalidData`, never as a successful end. An MP3 without a Xing/Info tag is not trimmed (it keeps the encoder and decoder delay) and its length is known only once it has been decoded to the end; the other formats report their length when opened, which `TimbreLoading.Auto` uses. The decoders are managed (no native libraries); their licenses are in `Cerneala.Timbre.THIRD-PARTY-NOTICES.txt`, which is copied next to the Cerneala assembly.

`FromReader` bypasses decoding: the factory returns a reader that already produces canonical interleaved stereo float32 PCM at 48 kHz.

The overloads taking `Func<TimbreMemoryBudget, ...>` pass the factory the [TimbreMemoryBudget](Cerneala.Timbre.TimbreMemoryBudget.md) of that opening, so it can reserve the live memory of its stream or reader before allocating it. A reservation the runtime cannot admit fails the opening with `TimbreErrorKind.ResourceLimitExceeded`. The parameterless overloads remain valid; their memory is simply not accounted. Passing a `null` literal is ambiguous between the two overloads: cast it to the intended delegate type.

Sources are cache keys for preloaded payloads: file sources by their resolved full path, factory sources by their factory instance.

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `FromFile(string path)` | `TimbreSource` | A local file source. |
| `FromStream(Func<Stream> openStream, string? name = null)` | `TimbreSource` | A seekable stream opened per playback. |
| `FromStream(Func<TimbreMemoryBudget, Stream> openStream, string? name = null)` | `TimbreSource` | A seekable stream opened per playback by a budget-aware factory. |
| `FromReader(Func<TimbreReader> openReader, string? name = null)` | `TimbreSource` | A canonical PCM reader created per playback. |
| `FromReader(Func<TimbreMemoryBudget, TimbreReader> openReader, string? name = null)` | `TimbreSource` | A canonical PCM reader created per playback by a budget-aware factory. |
| `implicit operator TimbreSource(string path)` | `TimbreSource` | Same as `FromFile`. |
| `ToString()` | `string` | Returns `Name`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Diagnostic name: the path, or the supplied/derived factory name. |

## See also

- [TimbreReader](Cerneala.Timbre.TimbreReader.md)
- [TimbreMemoryBudget](Cerneala.Timbre.TimbreMemoryBudget.md)
- [TimbreClip](Cerneala.Timbre.TimbreClip.md)
- [TimbreLoading](Cerneala.Timbre.TimbreLoading.md)
