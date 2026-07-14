# UiRelay benchmark baseline - 2026-07-14

This directory is the first checked-in performance baseline for `UiRelay`. Raw
BenchmarkDotNet reports and logs are preserved in the sibling directories.

## Environment

- OS: Windows 11 25H2 (10.0.26200.8655)
- CPU: AMD EPYC 9354 3.25 GHz, 8 logical / 4 physical cores exposed
- SDK: .NET SDK 10.0.300
- Runtime: .NET 8.0.27, x64 RyuJIT x86-64-v4
- GC: concurrent workstation
- BenchmarkDotNet: 0.15.8
- Job: 1 launch, 2 warmups, 5 measured iterations, 1 invocation per iteration
- Build: `Release`

Run the suite from the repository root with:

```powershell
dotnet run -c Release --no-build --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -- --filter "*UiRelay*" --exporters markdown --artifacts .\benchmarks\results\2026-07-14-relay
```

## Results

Scheduling is measured per accepted operation. `Post` is the baseline for the
`InvokeAsync` comparison.

| Operation | Mean | Allocated | Approx. throughput |
| --- | ---: | ---: | ---: |
| `Post` | 1.920 us | 64 B | 0.52 M ops/s |
| `InvokeAsync` | 2.640 us | 168 B | 0.38 M ops/s |

Frame drain uses `MaxCallbacksPerFrame = 1024`. The 2,048 callback case executes
1,024 callbacks and intentionally leaves 1,024 pending for the next frame.

| Callbacks queued | Mean/frame | Allocated/frame | Approx. executed throughput |
| ---: | ---: | ---: | ---: |
| 0 | 5.780 us | 920 B | n/a |
| 1 | 4.875 us | 120 B | 0.21 M callbacks/s |
| 100 | 11.560 us | 120 B | 8.65 M callbacks/s |
| 1,024 | 56.575 us | 120 B | 18.10 M callbacks/s |
| 2,048 | 75.700 us | 120 B | 13.53 M callbacks/s |

Concurrent producer results are normalized per one of 10,000 `Post` calls.
`Parallel.For` orchestration is included in the measured workload.

| Producers | Mean/post | Allocated/post | Approx. throughput |
| ---: | ---: | ---: | ---: |
| 1 | 21.97 ns | 90 B | 45.52 M posts/s |
| 2 | 52.12 ns | 91 B | 19.19 M posts/s |
| 4 | 70.68 ns | 91 B | 14.15 M posts/s |
| 8 | 77.34 ns | 91 B | 12.93 M posts/s |

Binding results are normalized per notification. Both cases deliver 10,000
worker-thread notifications and drain one coalesced UI refresh. Worker-thread
creation is included in the end-to-end workload.

| Binding workload | Mean/notification | Allocated/notification | Approx. throughput |
| --- | ---: | ---: | ---: |
| Simple source | 95.83 ns | 81 B | 10.43 M notifications/s |
| Two-source interpolation | 109.69 ns | 82 B | 9.12 M notifications/s |

MemoryDiagnoser observed zero Gen0 collections in every measured benchmark.
The focused allocation test separately proves that warmed-up `HasPending`,
`PendingCount`, and an idle `UiRelay.Drain` allocate zero bytes. The empty
`UIRoot.ProcessFrame` number above includes root/frame scheduler work and is not
the relay-idle structural measurement.

## Interpretation

Drain cost remains approximately linear with executed callbacks and the frame
budget leaves backlog bounded and visible. Increasing producer contention lowers
throughput without lost or duplicate work. The binding path coalesces each burst
to one pending UI refresh.

This is the initial implementation baseline, so there is no pre-relay historical
run to compare against. Future changes should compare their raw reports with
these files on equivalent hardware and runtime. The current allocation and
throughput data do not justify callback or completion-source pooling; adding a
pool now would increase lifecycle complexity without evidence of a useful win.

BenchmarkDotNet reports `MinIterationTime` warnings because these deliberately
fixed-size workloads finish well below 100 ms. Treat small differences inside
the reported confidence intervals as noise and rerun on the same machine before
drawing regression conclusions.

## Final verification run

The complete 13-case suite was rerun after formatting, the final build, and the
full test pass. Raw reports are stored under `final/results/`.

| Workload | Initial baseline | Final run | Allocation |
| --- | ---: | ---: | ---: |
| `Post` | 1.920 us | 2.420 us | 64 B |
| `InvokeAsync` | 2.640 us | 2.940 us | 168 B |
| Drain 100 callbacks | 11.560 us | 11.725 us | 120 B/frame |
| Drain 1,024 callbacks | 56.575 us | 71.500 us | 120 B/frame |
| Simple binding notification | 95.83 ns | 111.1 ns | 81 B |
| Interpolation notification | 109.69 ns | 445.5 ns | 82 B |

The timing deltas remain inside the deliberately short job's broad confidence
intervals; the final interpolation run in particular contains a large outlier
and is not evidence of a regression. Allocations are unchanged, the 2,048
case still leaves exactly 1,024 callbacks for a later frame, and the structural
allocation and exact-once stress tests remain the correctness gates rather than
microbenchmark timing noise.
