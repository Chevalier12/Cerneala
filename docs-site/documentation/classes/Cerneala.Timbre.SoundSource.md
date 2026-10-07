# SoundSource Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/SoundSource.cs`

Describes where a clip's audio comes from: a local file, a seekable stream factory, or a factory of canonical PCM readers.

```csharp
public abstract class SoundSource
```

Inheritance:
`object` -> `SoundSource`

## Examples

```csharp
using Cerneala.Timbre;

SoundSource file = "audio/confirm.wav";
SoundSource stream = SoundSource.FromStream(() => File.OpenRead("audio/music.ogg"), name: "music");
SoundSource generated = SoundSource.FromReader(() => new MyToneReader(), name: "tone");
```

`MyToneReader` stands for an application-defined [SoundReader](Cerneala.Timbre.SoundReader.md).

## Remarks

Creating a source performs no I/O. Each playback opens its own reader on a background worker; readers and positions are never shared between playbacks.

`FromFile` paths that are relative resolve against [SoundRuntimeOptions.BaseDirectory](Cerneala.Timbre.SoundRuntimeOptions.md), or `AppContext.BaseDirectory` when that is `null` — not the process working directory or a markup file's directory. URIs such as `http://` are rejected; non-seekable and network sources are not supported.

File and stream sources are decoded by content, not by extension. The core runtime registers no decoders, so a file or stream source fails its playback with [SoundErrorKind.UnsupportedFormat](Cerneala.Timbre.SoundErrorKind.md) until a decoding package supplies one. `FromStream` requires a stream with `CanRead` and `CanSeek`; the factory is called once per playback and the runtime disposes the stream.

`FromReader` bypasses decoding: the factory returns a reader that already produces canonical interleaved stereo float32 PCM at 48 kHz.

Sources are cache keys for preloaded payloads: file sources by their resolved full path, factory sources by their factory instance.

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `FromFile(string path)` | `SoundSource` | A local file source. |
| `FromStream(Func<Stream> openStream, string? name = null)` | `SoundSource` | A seekable stream opened per playback. |
| `FromReader(Func<SoundReader> openReader, string? name = null)` | `SoundSource` | A canonical PCM reader created per playback. |
| `implicit operator SoundSource(string path)` | `SoundSource` | Same as `FromFile`. |
| `ToString()` | `string` | Returns `Name`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Diagnostic name: the path, or the supplied/derived factory name. |

## See also

- [SoundReader](Cerneala.Timbre.SoundReader.md)
- [SoundClip](Cerneala.Timbre.SoundClip.md)
- [SoundLoading](Cerneala.Timbre.SoundLoading.md)
