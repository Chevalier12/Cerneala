# Prism retained pixel cache - reference benchmark

## Reference setup

- Date: 2026-07-21.
- Runtime: `net8.0-windows`, Release, WindowsDX HiDef.
- GPU: NVIDIA RTX 2000 Ada Generation.
- CPU identifier: AMD64 Family 25 Model 17 Stepping 1, 8 logical processors.
- OS: Microsoft Windows NT 10.0.26200.0.
- Surface: 256 x 144, 96 measured frames after 12 warmup frames and one
  post-GC priming frame.
- Command:
  `dotnet run --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -c Release --no-build --no-restore -- --prism-retained-cache`.

The command was run three times. The table reports the median of the three
runs. `GPU upper` includes submission plus a synchronous render-target readback;
it is deliberately an upper bound rather than a hardware timestamp query.

## Results

| Scenario | CPU cache off (us) | CPU cache on (us) | CPU delta | GPU upper off (us) | GPU upper on (us) | Cache-on allocation/frame |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Static control | 92.607 | 27.738 | -70.0% | 412.375 | 176.762 | 0 B |
| Static backdrop | 149.223 | 38.196 | -74.4% | 499.300 | 187.025 | 0 B |
| Animated game backdrop | 164.546 | 220.191 | +33.8% | 499.025 | 594.962 | 3,264 B |
| Motion parameter | 87.459 | 137.218 | +56.9% | 584.112 | 415.075 | 2,040 B |
| Changed resource version | 136.372 | 158.014 | +15.9% | 444.025 | 648.975 | 3,264 B |
| 24 common instances | 3,120.626 | 595.781 | -80.9% | 2,800.838 | 727.275 | 0 B |
| Four-entry churn budget | 29.061 | 55.231 | +26.170 us | 480.938 | 445.475 | 2,809 B |

The synchronous GPU upper bound is noisier than CPU submission, but its median
still confirms the stable-scene reduction. Dynamic cases produce no cache hits;
their CPU cost remains bounded to 15.9-56.9% for the normal changing workloads.
The deliberately hostile four-entry case adds 26.170 us per frame while doing
five capacity evictions per frame.

## Counter evidence

All figures below are the deterministic counters from each 96-frame measured
window. Cache-off performed zero lookups and zero promotions in every scenario.

| Scenario, cache on | Final hits | Misses | Lookups | Promotions | Evictions | Entries | Retained bytes | Saved captures | Saved passes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Static control | 96 | 0 | 96 | 0 | 0 | 5 | 1,474,560 | 96 | 576 |
| Static backdrop | 96 | 0 | 96 | 0 | 0 | 8 | 2,359,296 | 96 | 1,152 |
| Animated game backdrop | 0 | 768 | 768 | 768 | 768 | 8 | 2,359,296 | 0 | 0 |
| Motion parameter | 0 | 480 | 480 | 480 | 480 | 5 | 1,474,560 | 0 | 0 |
| Changed resource version | 0 | 768 | 768 | 768 | 768 | 8 | 2,359,296 | 0 | 0 |
| 24 common instances | 2,304 | 0 | 2,304 | 0 | 0 | 120 | 35,389,440 | 2,304 | 13,824 |
| Four-entry churn budget | 0 | 480 | 480 | 480 | 480 capacity | 4 | 1,179,648 | 0 | 0 |

Static control, static backdrop, and common-instance hits allocated `0 B` in
all three runs. The focused executor regression
`SimpleCompositionCapturesOnceAndAllocatesNothingAfterWarmup` independently
asserts the same exact `0 B` contract.

## Default budgets

The measured HalfVector4 surface is 294,912 bytes at 256 x 144. At 1920 x
1080 the same surface is 16,588,800 bytes (15.82 MiB). The eight-entry backdrop
working set therefore scales to about 126.6 MiB at full HD, while the
24-instance case demonstrates that entry count, not only byte count, needs a
separate ceiling.

The retained defaults remain:

- 512 MiB hard limit for all Prism surfaces;
- 256 MiB retained soft limit;
- 256 retained entries.

The 256 MiB soft limit holds roughly two measured full-HD backdrop working sets
or sixteen full-HD HalfVector4 surfaces. The 512 MiB hard cap leaves another
256 MiB for transient correctness work before allocation refusal. The entry
limit bounds small-surface workloads, and the four-entry benchmark proves that
the same mechanism produces deterministic capacity eviction without exceeding
its byte or entry budget.
