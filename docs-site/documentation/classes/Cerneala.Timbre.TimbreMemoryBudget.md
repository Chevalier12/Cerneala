# TimbreMemoryBudget Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/TimbreMemoryBudget.cs`

Cooperative accounting of the live memory that one opening of a [TimbreSource](Cerneala.Timbre.TimbreSource.md) uses for its reader, decoder, and I/O buffers.

```csharp
public sealed class TimbreMemoryBudget
```

Inheritance:
`object` -> `TimbreMemoryBudget`

## Examples

```csharp
using Cerneala.Timbre;

const int TableBytes = 256 * 1024;

TimbreSource source = TimbreSource.FromReader(budget =>
{
    // Reserve first: an oversized request fails here, before allocating.
    budget.Reserve(TableBytes);
    return new WavetableReader(new float[TableBytes / sizeof(float)]);
}, name: "wavetable");
```

`WavetableReader` stands for an application-defined [TimbreReader](Cerneala.Timbre.TimbreReader.md).

## Remarks

The runtime creates one budget each time it opens a source (once per playback, and once per `PrepareAsync` that has to read the source) and passes it to budget-aware `TimbreSource.FromStream` and `TimbreSource.FromReader` factories. Built-in file and stream decoding reserves through the same budget: a file source reserves its 64 KiB read buffer before the file is opened.

All budgets of a runtime share the allowance [TimbreRuntimeOptions.StreamingMemoryLimit](Cerneala.Timbre.TimbreRuntimeOptions.md), together with the runtime's streaming buffers. `Reserve` admits the bytes or throws [TimbreException](Cerneala.Timbre.TimbreException.md) with `TimbreErrorKind.ResourceLimitExceeded`; nothing is allocated by the budget itself. A refusal while a playback opens its source fails that playback with `ResourceLimitExceeded`; `PrepareAsync` throws it. Other playbacks are unaffected.

The default limit, `long.MaxValue`, is accounting headroom, not a cap: reservations always succeed unless a limit is configured explicitly.

The accounting is cooperative. It covers what Timbre's decoders and cooperative factories reserve; memory that a factory or reader allocates without reserving is not intercepted or counted.

Lifetime: reservations stay counted until they are disposed or until the runtime releases the reader. The runtime disposes the reader (and the stream it owns) first and then closes the budget, returning every reservation still held; `Reserve` on a closed budget throws `InvalidOperationException`. A reader may dispose a [TimbreMemoryReservation](Cerneala.Timbre.TimbreMemoryReservation.md) earlier, after freeing the memory it covered.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Limit` | `long` | The runtime's `StreamingMemoryLimit`. |
| `Reserved` | `long` | Bytes currently reserved across the whole runtime, including streaming buffers. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `Reserve(long bytes)` | `TimbreMemoryReservation` | Reserves `bytes` before they are allocated. Throws `ArgumentOutOfRangeException` for a negative count, `TimbreException` (`ResourceLimitExceeded`) when the allowance cannot admit them, and `InvalidOperationException` after the reader was released. |

## See also

- [TimbreMemoryReservation](Cerneala.Timbre.TimbreMemoryReservation.md)
- [TimbreSource](Cerneala.Timbre.TimbreSource.md)
- [TimbreRuntimeOptions](Cerneala.Timbre.TimbreRuntimeOptions.md)
