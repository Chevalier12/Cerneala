# Prism integration and hardening - reference benchmark

## Reference setup

- Date: 2026-07-21.
- Runtime: `net8.0-windows`, Release, WindowsDX HiDef.
- GPU: NVIDIA RTX 2000 Ada Generation.
- CPU: 8 logical processors.
- OS: Microsoft Windows NT 10.0.26200.0.
- Resolutions: 256 x 144 (`preview`) and 640 x 360 (`medium`).
- Window: 96 measured frames after 12 warmup frames and one post-GC priming
  frame for every scenario/cache/resolution combination.
- Command:
  `dotnet run -c Release --no-build --no-restore --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -- --prism-retained-cache`.

The command was run three times. Timings below are medians; all work, cache,
surface, allocation, and eviction counters were identical in all three runs.
`GPU upper` includes submission and a synchronous render-target readback. It is
an intentionally conservative completion upper bound, not a hardware timestamp.

## Medium timing matrix

The build columns measure construction of a fresh immutable graph plan. Execute
allocation is measured only around warmed executor submission. All byte figures
are per measured frame.

| Scenario | CPU build (us) | Build alloc | Submit off (us) | Submit on (us) | GPU upper off (us) | GPU upper on (us) | Execute alloc on |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| static-control | 68.505 | 165,578 B | 41.060 | 16.708 | 2,331.912 | 340.262 | 0 B |
| static-backdrop | 136.996 | 278,278 B | 62.047 | 20.177 | 2,925.338 | 290.125 | 0 B |
| animated-game-backdrop | 158.229 | 278,478 B | 44.569 | 75.667 | 3,157.463 | 2,807.637 | 3,264 B |
| motion-parameter | 88.543 | 173,736 B | 22.042 | 29.534 | 2,363.412 | 2,456.775 | 2,040 B |
| changed-resource | 114.280 | 245,929 B | 34.564 | 39.637 | 2,870.787 | 2,727.188 | 3,264 B |
| many-common-instances | 1,881.855 | 3,611,494 B | 730.680 | 162.388 | 46,502.613 | 991.325 | 0 B |
| small-budget | 60.908 | 165,618 B | 20.882 | 31.433 | 2,424.425 | 2,387.175 | 2,809 B |
| many-layers | 476.438 | 1,039,125 B | 160.686 | 14.361 | 6,351.087 | 262.712 | 0 B |
| filter-chain | 82.582 | 209,068 B | 23.258 | 5.018 | 2,607.750 | 240.650 | 0 B |
| styles | 96.078 | 207,756 B | 23.508 | 4.853 | 1,221.825 | 273.587 | 0 B |
| nested-groups | 133.978 | 319,316 B | 37.464 | 5.714 | 3,182.300 | 242.200 | 0 B |
| shared-backdrop | 217.758 | 525,992 B | 71.112 | 10.821 | 5,700.800 | 314.488 | 0 B |

Static cache hits allocate exactly `0 B`. Dynamic dependency changes allocate
2,040-3,264 B per frame for cache-key/promotion bookkeeping; the hostile
four-entry scenario allocates 2,809 B per frame. Graph construction allocation
is reported separately and is not attributed to retained executor submission.

## Preview scaling

This table records cache-on results at the smaller representative scope. Surface
memory scales by exactly 6.25x from 256 x 144 to 640 x 360.

| Scenario | CPU build (us) | Submit (us) | GPU upper (us) | Execute alloc/frame | Retained MiB | Peak total MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| static-control | 339.972 | 26.549 | 162.662 | 0 B | 1.406 | 1.406 |
| static-backdrop | 567.397 | 39.789 | 180.238 | 0 B | 2.250 | 2.531 |
| animated-game-backdrop | 690.618 | 262.740 | 597.500 | 3,264 B | 2.250 | 2.531 |
| motion-parameter | 445.634 | 131.146 | 421.925 | 2,040 B | 1.406 | 1.688 |
| changed-resource | 677.291 | 189.224 | 561.300 | 3,264 B | 2.250 | 2.531 |
| many-common-instances | 2,223.418 | 983.852 | 1,240.775 | 0 B | 33.750 | 33.750 |
| small-budget | 82.765 | 110.678 | 415.525 | 2,809 B | 1.125 | 1.688 |
| many-layers | 559.951 | 77.427 | 221.613 | 0 B | 4.219 | 7.594 |
| filter-chain | 92.877 | 15.470 | 116.062 | 0 B | 1.688 | 1.688 |
| styles | 116.715 | 16.149 | 170.512 | 0 B | 1.688 | 1.688 |
| nested-groups | 149.430 | 20.614 | 156.762 | 0 B | 1.688 | 1.969 |
| shared-backdrop | 253.931 | 41.905 | 202.300 | 0 B | 4.500 | 4.781 |

## Deterministic work and memory

The counters below are for 640 x 360. `Hits/misses` are cache-on final hits and
node misses. Static scenarios finish with no live transient surface; animated
cases need only two or three. No measured row created a new surface after
warmup, leaked an active lease, or entered fallback.

| Scenario | Passes off -> on | Captures off -> on | Peak live | Hits/misses | Entries | Capacity evictions | Retained MiB | Peak total MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| static-control | 672 -> 96 | 96 -> 0 | 0 | 96/0 | 5 | 0 | 8.789 | 8.789 |
| static-backdrop | 1,248 -> 96 | 96 -> 0 | 0 | 96/0 | 8 | 0 | 14.062 | 15.820 |
| animated-game-backdrop | 1,248 -> 1,248 | 96 -> 96 | 3 | 0/768 | 8 | 0 | 14.062 | 15.820 |
| motion-parameter | 768 -> 768 | 96 -> 96 | 2 | 0/480 | 5 | 0 | 8.789 | 10.547 |
| changed-resource | 1,056 -> 1,056 | 96 -> 96 | 3 | 0/768 | 8 | 0 | 14.062 | 15.820 |
| many-common-instances | 16,128 -> 2,304 | 2,304 -> 0 | 0 | 2,304/0 | 120 | 0 | 210.938 | 210.938 |
| small-budget | 672 -> 672 | 96 -> 96 | 2 | 0/480 | 4 | 480 | 7.031 | 10.547 |
| many-layers | 4,896 -> 96 | 96 -> 0 | 0 | 96/0 | 15 | 0 | 26.367 | 47.461 |
| filter-chain | 768 -> 96 | 96 -> 0 | 0 | 96/0 | 6 | 0 | 10.547 | 10.547 |
| styles | 768 -> 96 | 96 -> 0 | 0 | 96/0 | 6 | 0 | 10.547 | 10.547 |
| nested-groups | 1,344 -> 96 | 96 -> 0 | 0 | 96/0 | 6 | 0 | 10.547 | 12.305 |
| shared-backdrop | 2,304 -> 192 | 192 -> 0 | 0 | 192/0 | 16 | 0 | 28.125 | 29.883 |

The exact contract tests additionally prove that an identical second frame has
a final retained hit, no capture, and no covered effect pass; every structural,
parameter, resource, transform, clip, mask, style, backdrop, viewport, color,
and shader-package input that can change pixels produces a miss and cache-on
pixels equal a fresh cache-off render. Nonstructural Motion commits reuse the
same `ElementRenderCache` command topology across thousands of updates.

## Default budgets and failure behavior

The measured data ratifies and freezes these defaults:

- 512 MiB hard limit for transient plus retained Prism surfaces;
- 256 MiB retained soft limit;
- 256 retained entries.

At 640 x 360 the 120-entry common-instance workload retains 210.938 MiB, so the
soft byte limit has useful headroom while the independent entry ceiling remains
necessary for smaller surfaces. A full-HD HalfVector4 surface is 15.820 MiB;
sixteen such surfaces occupy 253.125 MiB. The hard limit reserves another
256 MiB for correctness-critical transient work.

When the retained soft byte or entry limit is reached, unpinned least-recently
used entries are evicted deterministically; an oversized or still-unadmittable
promotion is rejected and the current frame continues transiently. The
four-entry scenario proves this with exactly 480 capacity evictions, four final
entries, 7.031 MiB retained, 10.547 MiB peak total, and zero fallback. Transient
pressure evicts retained entries before requesting more memory. If the hard
limit still cannot admit a required transient surface, execution records
`SurfaceAllocationFailed`, restores the host target, releases leases, and draws
the remaining inner content without partial Prism output.

## Presentation dogfood gate

The real WindowsDX Presentation benchmark ran eight cycles with 45 frames for
each of seven chapters. Its stable gate is a 16.6667 ms warm p99 per chapter and
a separately visible 500 ms one-time cold ceiling. `SOLAR MOTION` measured:

- cold maximum: 388.664 ms;
- warm p99: 12.874 ms;
- warm maximum: 49.363 ms;
- two warm samples above 16.6667 ms, both preserved in the JSON and console;
- no adaptive-quality path or hidden feature degradation.

The cold sample exposes 78.228 ms of retained update and 310.422 ms of first
Prism command rendering rather than burying shader/JIT/surface initialization in
the steady-state number. Every chapter passed its cold and warm gate; the raw
report is generated at `benchmarks/artifacts/prism-stage4-frame-budget.json`.

The measurements do not justify adaptive quality, async compute, or a public
third-party plugin API, so none was added. The only recorded opportunity is a
future explicit shader/surface warmup study if startup latency becomes a product
target; it is not required for the current steady-state frame budget.
