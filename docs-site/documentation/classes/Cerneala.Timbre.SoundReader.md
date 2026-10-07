# SoundReader Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/SoundReader.cs`

Per-playback source of canonical PCM: interleaved stereo float32 at 48 kHz.

```csharp
public abstract class SoundReader : IDisposable
```

Inheritance:
`object` -> `SoundReader`

Implements:
`IDisposable`

## Examples

```csharp
using Cerneala.Timbre;

sealed class SilenceReader(long lengthFrames) : SoundReader
{
    private long position;

    public override long? LengthFrames => lengthFrames;

    public override ValueTask<SoundReadResult> ReadAsync(Memory<float> destination, CancellationToken cancellationToken)
    {
        int frames = (int)Math.Min(destination.Length / SoundRuntime.ChannelCount, lengthFrames - position);
        destination.Span[..(frames * SoundRuntime.ChannelCount)].Clear();
        position += frames;
        return ValueTask.FromResult(new SoundReadResult(frames, position == lengthFrames));
    }

    public override ValueTask SeekAsync(long frame, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(frame, lengthFrames);
        position = frame;
        return ValueTask.CompletedTask;
    }
}

var clip = new SoundClip(SoundSource.FromReader(() => new SilenceReader(48000)));
```

## Remarks

The runtime creates one reader per playback and calls it from a background worker, never from an audio callback or the UI thread. Calls on one reader are sequential. The runtime disposes the reader after its playback stops, completes, fails, or is canceled.

Contract:

- Units: a sample is one `float` (4 bytes); a frame is two samples (left, right). `destination.Length` is always a whole number of frames, and a read returns at most `destination.Length / 2` frames.
- Samples must be finite. NaN or infinity fails the playback with `SoundErrorKind.InvalidData`.
- `EndOfSource = true` marks the end of the source; it may accompany the last frames.
- After a result of zero frames with `EndOfSource = false`, the next `ReadAsync` must wait asynchronously for data, end of source, an error, or cancellation. Returning zero frames again synchronously violates the contract and fails the playback with `InvalidData`; the runtime does not poll.
- `SeekAsync` positions the next read exactly at the requested decoded frame. The runtime validates the target against `LengthFrames` when known.
- `LengthFrames` is `null` while the length is unknown; [SoundPlayback.Duration](Cerneala.Timbre.SoundPlayback.md) is `null` in that case until a streaming playback reaches the end of the source. A reader may start reporting a length later; the runtime reads it when the reader is opened.
- A reader that reports a length must deliver exactly that many frames: ending earlier or reading past it fails the playback with `SoundErrorKind.InvalidData`, whether the clip is preloaded or streamed.
- Exceptions other than cancellation fail the playback with `SoundErrorKind.SourceUnavailable`, unless the exception is a [SoundException](Cerneala.Timbre.SoundException.md), whose kind is kept. Errors are never treated as a successful end of source.

## Constructors

| Name | Description |
| --- | --- |
| `SoundReader()` | Initializes the base reader. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `LengthFrames` | `long?` | Total frames, or `null` when unknown. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `ReadAsync(Memory<float> destination, CancellationToken cancellationToken)` | `ValueTask<SoundReadResult>` | Reads the next frames. |
| `SeekAsync(long frame, CancellationToken cancellationToken)` | `ValueTask` | Repositions to an exact frame. |
| `Dispose()` | `void` | Releases the reader. |
| `Dispose(bool disposing)` | `void` | Protected override point for releasing resources. |

## See also

- [SoundReadResult](Cerneala.Timbre.SoundReadResult.md)
- [SoundSource](Cerneala.Timbre.SoundSource.md)
