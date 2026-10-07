# SoundMemoryBudget Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/SoundMemoryBudget.cs`

Cooperative accounting of the live memory that one opening of a [SoundSource](Cerneala.Timbre.SoundSource.md) uses for its reader, decoder, and I/O buffers.

```csharp
public sealed class SoundMemoryBudget
```

Inheritance:
`object` -> `SoundMemoryBudget`

## Examples

```csharp
using Cerneala.Timbre;

const int TableBytes = 256 * 1024;

SoundSource source = SoundSource.FromReader(budget =>
{
    // Reserve first: an oversized request fails here, before allocating.
    budget.Reserve(TableBytes);
    return new WavetableReader(new float[TableBytes / sizeof(float)]);
}, name: "wavetable");
```

`WavetableReader` stands for an application-defined [SoundReader](Cerneala.Timbre.SoundReader.md).

## Remarks

The runtime creates one budget each time it opens a source (once per playback, and once per `PrepareAsync` that has to read the source) and passes it to budget-aware `SoundSource.FromStream` and `SoundSource.FromReader` factories. Built-in file and stream decoding reserves through the same budget: a file source reserves its 64 KiB read buffer before the file is opened.

All budgets of a runtime share the allowance [SoundRuntimeOptions.StreamingMemoryLimit](Cerneala.Timbre.SoundRuntimeOptions.md), together with the runtime's streaming buffers. `Reserve` admits the bytes or throws [SoundException](Cerneala.Timbre.SoundException.md) with `SoundErrorKind.ResourceLimitExceeded`; nothing is allocated by the budget itself. A refusal while a playback opens its source fails that playback with `ResourceLimitExceeded`; `PrepareAsync` throws it. Other playbacks are unaffected.

The default limit, `long.MaxValue`, is accounting headroom, not a cap: reservations always succeed unless a limit is configured explicitly.

The accounting is cooperative. It covers what Timbre's decoders and cooperative factories reserve; memory that a factory or reader allocates without reserving is not intercepted or counted.

Lifetime: reservations stay counted until they are disposed or until the runtime releases the reader. The runtime disposes the reader (and the stream it owns) first and then closes the budget, returning every reservation still held; `Reserve` on a closed budget throws `InvalidOperationException`. A reader may dispose a [SoundMemoryReservation](Cerneala.Timbre.SoundMemoryReservation.md) earlier, after freeing the memory it covered.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Limit` | `long` | The runtime's `StreamingMemoryLimit`. |
| `Reserved` | `long` | Bytes currently reserved across the whole runtime, including streaming buffers. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `Reserve(long bytes)` | `SoundMemoryReservation` | Reserves `bytes` before they are allocated. Throws `ArgumentOutOfRangeException` for a negative count, `SoundException` (`ResourceLimitExceeded`) when the allowance cannot admit them, and `InvalidOperationException` after the reader was released. |

## See also

- [SoundMemoryReservation](Cerneala.Timbre.SoundMemoryReservation.md)
- [SoundSource](Cerneala.Timbre.SoundSource.md)
- [SoundRuntimeOptions](Cerneala.Timbre.SoundRuntimeOptions.md)
