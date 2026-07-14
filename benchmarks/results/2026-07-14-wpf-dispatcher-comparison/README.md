# UiRelay versus WPF Dispatcher - 2026-07-14

This directory contains two complete same-machine BenchmarkDotNet runs of the
controlled comparison in `benchmarks/Cerneala.WpfDispatcherBenchmarks`.

## Environment

- OS: Windows 11 25H2 (10.0.26200.8655)
- CPU: AMD EPYC 9354 3.25 GHz, 8 logical / 4 physical cores exposed
- SDK: .NET SDK 10.0.300
- Runtime: .NET 8.0.27, x64 RyuJIT x86-64-v4
- GC: concurrent workstation
- BenchmarkDotNet: 0.15.8
- Job: 1 launch, 2 warmups, 5 measured iterations, 1 invocation per iteration
- Build: `Release`

Raw reports from the first run are under `results/`. The independent
verification run is under `final/results/`.

## Methodology

WPF and Cerneala each own a dedicated STA thread in the same benchmark process.
Enqueue benchmarks keep both owner threads dormant so WPF cannot consume work
while producers are still queueing it. Drain benchmarks prequeue the same number
of callbacks, then explicitly pump one controlled batch. WPF uses normal
priority and a `DispatcherFrame`; Cerneala uses one `UIRoot.ProcessFrame` with a
Relay budget large enough for the complete batch.

The correctness smoke test executes 10,000 callbacks exactly once on each owner
thread and verifies that Cerneala leaves no pending work.

## Headline results

The table uses the independent final run. Lower is better. The speedup is
`WPF / Cerneala`.

| Workload | WPF | Cerneala | Cerneala speedup | WPF allocation | Cerneala allocation |
| --- | ---: | ---: | ---: | ---: | ---: |
| Single enqueue | 19.820 us | 2.560 us | 7.74x | 544 B | 64 B |
| Task-returning enqueue | 19.040 us | 3.540 us | 5.38x | 544 B | 168 B |
| Drain 1 callback | 80.24 us | 33.72 us | 2.38x | 1,392 B | 144 B |
| Drain 100 callbacks | 745.48 us | 42.73 us | 17.45x | 37,008 B | 144 B |
| Drain 1,024 callbacks | 9,361.66 us | 95.74 us | 97.78x | 369,648 B | 144 B |

The producer benchmark reports normalized time and allocation per enqueue in a
10,000-operation batch.

| Producers | WPF/enqueue | Cerneala/enqueue | Cerneala speedup | WPF allocation | Cerneala allocation |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 354.16 ns | 19.99 ns | 17.72x | 360 B | 90 B |
| 2 | 302.20 ns | 49.15 ns | 6.15x | 360 B | 91 B |
| 4 | 321.63 ns | 73.73 ns | 4.36x | 360 B | 91 B |
| 8 | 331.55 ns | 80.76 ns | 4.11x | 360 B | 91 B |

## Reproducibility

The first run produced the same ordering and identical allocation counts:

- single enqueue: 5.94x faster;
- task-returning enqueue: 5.28x faster;
- drain 100: 20.01x faster;
- drain 1,024: 78.23x faster;
- producer batches: 4.01x to 14.13x faster.

The exact timings have broad confidence intervals because the fixed-size jobs
finish well below BenchmarkDotNet's recommended 100 ms iteration duration.
Treat the precise ratios as ranges, not universal constants. The two runs do,
however, reproduce the direction, order of magnitude for large drains, and
allocation differences.

## Interpretation boundary

This benchmark compares only the overlapping queue-and-dispatch contract. Relay
is intentionally smaller and does not implement WPF Dispatcher priorities,
timers, nested message loops, Win32 message integration, or compatibility
semantics. The controlled `DispatcherFrame` also measures an explicit WPF batch
pump, not the latency distribution of an already-running desktop application.

The defensible claim is that Relay is substantially cheaper for Cerneala's
bounded frame-handoff workload. It is not a claim that Relay replaces every
service provided by WPF Dispatcher.
