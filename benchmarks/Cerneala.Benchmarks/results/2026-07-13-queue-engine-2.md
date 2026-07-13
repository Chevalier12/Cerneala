# Queue Engine 2.0 Benchmark Results

Date: 2026-07-13

Environment:

- Windows 11 25H2 (`10.0.26200.8655`)
- AMD EPYC 9354 3.25 GHz, 4 physical / 8 logical cores available
- .NET SDK 10.0.300
- .NET runtime 8.0.27, x64 RyuJIT
- BenchmarkDotNet 0.15.8
- Release configuration

The baseline and final runs used the same machine, runtime, build configuration, benchmark parameters, and BenchmarkDotNet jobs. Short-run confidence intervals are intentionally not used as CI thresholds; unit tests enforce the structural performance invariants.

## Idle HasWork

| Tree nodes | Baseline mean | Final mean | Baseline allocation | Final allocation |
| ---: | ---: | ---: | ---: | ---: |
| 100 | 43.69 us | 5.168 ns | 152.16 KB | 0 B |
| 1,000 | 454.01 us | 5.193 ns | 1,470.30 KB | 0 B |
| 10,000 | 8,775.06 us | 5.216 ns | 14,101.72 KB | 0 B |

Ten repeated idle queries on 10,000 nodes fell from 87.28 ms and 141 MB allocated to 43.63 ns and zero allocation. The final time is independent of tree size within measurement noise.

## Sparse Snapshot

| Tree nodes | Queued | Baseline mean | Final mean | Baseline allocation | Final allocation |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 10,000 | 1 | 1,257.86 us | 7.288 us | 2,014.93 KB | 112 B |
| 10,000 | 100 | 1,318.51 us | 10.965 us | 2,022.04 KB | 3,280 B |
| 10,000 | 1,000 | 1,515.51 us | 51.989 us | 2,074.84 KB | 32,080 B |

The final snapshot cost follows the queued element count after the shared index has been built, instead of rebuilding visual order for each queue.

## Shared Order

Three successive snapshots with 100 scheduled elements per queue:

| Tree nodes | Baseline mean | Final mean | Baseline allocation | Final allocation |
| ---: | ---: | ---: | ---: | ---: |
| 1,000 | 212.8 us | 11.25 us | 652.57 KB | 9.61 KB |
| 10,000 | 3,907.8 us | 31.57 us | 6,066.16 KB | 9.61 KB |

Unit tests additionally verify exactly one order-index build for a `TreeVersion`, including snapshots from different queue wrappers.

## Drain Comparison

The legacy comparison embeds the removed `HashSet + List + RemoveAll` behavior in the benchmark project and runs beside the current queue on the same attached elements.

| Entries | Legacy mean | Current mean | Time ratio | Legacy allocation | Current allocation |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 100 | 121.72 us | 32.00 us | 0.26 | 37.69 KB | 3.23 KB |
| 1,000 | 3,132.85 us | 348.95 us | 0.11 | 356.14 KB | 31.36 KB |
| 10,000 | 243,190.28 us | 3,566.80 us | 0.01 | 3,710.18 KB | 312.61 KB |

The old curve is visibly quadratic; the current drain grows approximately linearly across the tested sizes.

## Rebuild And Detach

| Scenario | Size | Mean | Allocation |
| --- | ---: | ---: | ---: |
| Rebuild plus 100-item snapshot | 1,000 nodes | 317.8 us | 212.51 KB |
| Rebuild plus 100-item snapshot | 10,000 nodes | 3,435.1 us | 2,016.99 KB |
| Detach scheduled subtree | 10 nodes | 29.10 us | 6.35 KB |
| Detach scheduled subtree | 100 nodes | 132.30 us | 45.70 KB |
| Detach scheduled subtree | 1,000 nodes | 1,388.42 us | 438.87 KB |

The rebuild visits the visual tree once for the new version. Detach cleanup scales with the detached subtree and performs direct dictionary removals from every queue; it does not traverse the remaining tree.

## Functional Gates

- `HasWork` performs zero visual traversals and allocates zero bytes after warmup.
- One `TreeVersion` produces at most one shared index build.
- `Remove` uses direct dictionary removal; no queue drain contains `List.RemoveAll`.
- A snapshot sorts only its queued, attached elements.
- Active detach removes every detached element from all root queues, with defensive snapshot pruning retained.
- The Playground wrapped-diagnostics regression scenario passes with exactly one useful `Measure` call.
- Baseline suite: 1,763 passing tests in 38.57 s.
- Final suite: 1,786 passing tests in 21.95 s.
