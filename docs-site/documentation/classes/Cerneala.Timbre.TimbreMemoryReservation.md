# TimbreMemoryReservation Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/TimbreMemoryBudget.cs`

Bytes reserved through a [TimbreMemoryBudget](Cerneala.Timbre.TimbreMemoryBudget.md).

```csharp
public sealed class TimbreMemoryReservation : IDisposable
```

Inheritance:
`object` -> `TimbreMemoryReservation`

Implements:
`IDisposable`

## Examples

```csharp
using Cerneala.Timbre;

TimbreSource source = TimbreSource.FromReader(budget =>
{
    // Temporary scratch memory needed only while the reader is built.
    using (budget.Reserve(64 * 1024))
    {
        float[] scratch = new float[16 * 1024];
        PrecomputeTable(scratch);
    }

    return new TableReader();
});
```

`PrecomputeTable` and `TableReader` stand for application code.

## Remarks

Disposing returns the bytes to the runtime's allowance; repeated disposal has no further effect. Dispose a reservation only after the memory it covers is no longer referenced. Reservations that are not disposed are returned when the runtime releases the reader, after the reader has been disposed.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Bytes` | `long` | Number of bytes reserved. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `Dispose()` | `void` | Returns the bytes to the allowance. Idempotent. |

## See also

- [TimbreMemoryBudget](Cerneala.Timbre.TimbreMemoryBudget.md)
