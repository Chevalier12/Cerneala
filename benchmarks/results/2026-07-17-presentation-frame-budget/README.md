# CernealaPresentation frame-budget results

## Environment

- Date: 2026-07-17
- OS: Microsoft Windows 11 Home 10.0.26200, 64-bit
- CPU: AMD EPYC 9354 32-Core Processor
- Memory: 16 GB
- Display adapters: NVIDIA RTX 2000 Ada Generation (driver 32.0.15.7216) and
  Red Hat QXL controller (driver 10.0.0.21000)
- Runtime: .NET 8, native WindowsDX window
- Configuration: Release
- Budget: `16.6667 ms`
- Workload: eight cycles, 45 frames per chapter load

## RED baseline

Exact command, run from the repository root:

```powershell
dotnet run -c Release --no-build --project .\benchmarks\Cerneala.PresentationFrameBudget\Cerneala.PresentationFrameBudget.csproj -- --cycles 8 --frames-per-load 45 --budget-ms 16.6667 --report .\benchmarks\artifacts\stage0-baseline.json
```

The runner completed the real presentation tour in 41.3 seconds, produced all 360
samples for each of the six measured chapters, excluded Welcome, and exited `1`
because 113 frames exceeded the budget.

| Chapter | Cold max | Cold over | Warm max | Warm over |
| --- | ---: | ---: | ---: | ---: |
| Retained Model | 40.997 ms | 2 | 21.259 ms | 45 |
| Build-Time Markup | 52.880 ms | 2 | 21.827 ms | 10 |
| Aspect Design System | 28.145 ms | 4 | 46.998 ms | 21 |
| Motion | 32.417 ms | 1 | 22.504 ms | 9 |
| Frame Pipeline | 19.865 ms | 1 | 16.439 ms | 0 |
| Diagnostics | 20.169 ms | 1 | 19.475 ms | 17 |

This is a behavior RED: the executable built successfully, automation traversed the
expected chapters, the report was complete, and neither the fixture nor the timeout
caused the non-zero exit.

## Allocation and GC profile

A separate two-cycle, 45-frame-per-load run was captured with `dotnet-counters`; the
frame callback did not force GC and did not write or serialize samples while frames
were being measured.

```powershell
$env:CERNEALA_PRESENTATION_FRAME_BUDGET_REPORT='benchmarks\artifacts\stage0-profile-report.json'
$env:CERNEALA_PRESENTATION_FRAME_BUDGET_CYCLES='2'
$env:CERNEALA_PRESENTATION_FRAME_BUDGET_FRAMES_PER_LOAD='45'
dotnet-counters collect --counters System.Runtime --format csv --output .\benchmarks\artifacts\stage0-runtime-counters.csv -- .\CernealaPresentation\bin\Release\net8.0-windows\CernealaPresentation.exe
```

Across 12 one-second samples, the process allocated 3,249,071,112 bytes. Allocation
rate peaked at 426,428,808 B/s. The counters recorded 367 Gen 0, 256 Gen 1, and 255
Gen 2 collections, confirming heavy allocation and GC churn during the native draw
workload without adding public phase-timing APIs.

## Stage 3 local optimization checkpoint

After canonical phases, bounded retention, lazy grayscale masks, and removal of
avoidable `RasterizedText` buffer copies, the same eight-cycle command produced zero
warm overruns for every chapter. Five cold first-load frames remain over budget:

| Chapter | Cold max | Cold over | Warm max | Warm over |
| --- | ---: | ---: | ---: | ---: |
| Retained Model | 31.356 ms | 1 | 8.516 ms | 0 |
| Build-Time Markup | 52.405 ms | 1 | 6.409 ms | 0 |
| Aspect Design System | 25.954 ms | 1 | 3.594 ms | 0 |
| Motion | 28.154 ms | 1 | 5.535 ms | 0 |
| Frame Pipeline | 16.399 ms | 0 | 8.856 ms | 0 |
| Diagnostics | 16.877 ms | 1 | 5.332 ms | 0 |

The run completed in 40.3 seconds with a complete report. This checkpoint isolates
the remaining failure to synchronous cold creation on the first measured frame; the
bounded cache is effective after population. Per the plan's stop condition, prewarm
or asynchronous rasterization requires review before expanding the implementation.

A separate CPU-sampling trace of one cold tour confirmed that the remaining path still
contains native `D3D11.CreateTexture2D`/`DeviceContext.UpdateSubresource` work together
with `SKTextBlobBuilder.Build`, HarfBuzz shaping, `SKCanvas.DrawText`, and subpixel-layer
construction. The optimized two-cycle runtime-counter run reduced total allocation
from 3,249,071,112 bytes to 426,480,320 bytes and Gen 0 collections from 367 to 43,
but cannot move the mandatory synchronous GPU creation out of the first measured
frame. The trace command was:

```powershell
$env:CERNEALA_PRESENTATION_FRAME_BUDGET_CYCLES='1'
$env:CERNEALA_PRESENTATION_FRAME_BUDGET_FRAMES_PER_LOAD='5'
dotnet-trace collect --providers Microsoft-DotNETCore-SampleProfiler --output .\benchmarks\artifacts\stage3-cpu.nettrace -- .\CernealaPresentation\bin\Release\net8.0-windows\CernealaPresentation.exe
```

## Stage 3 GREEN

The final Stage 3 implementation batches cold text uploads into a compact shared GPU
atlas, reference-counts that atlas across retained cache entries, returns temporary
CPU layer and atlas buffers only after synchronous upload, and reuses the per-frame
rasterization request dictionary. The last change removed the remaining steady-state
allocation that was periodically triggering warm-frame spikes.

The exact gate command completed in 38.6 seconds, wrote
`benchmarks/artifacts/presentation-frame-budget-20260717-133009.json`, and exited `0`:

```powershell
dotnet run -c Release --project .\benchmarks\Cerneala.PresentationFrameBudget\Cerneala.PresentationFrameBudget.csproj -- --cycles 8 --frames-per-load 45 --budget-ms 16.6667
```

| Chapter | Cold max | Cold over | Warm max | Warm over |
| --- | ---: | ---: | ---: | ---: |
| Retained Model | 11.076 ms | 0 | 4.507 ms | 0 |
| Build-Time Markup | 16.106 ms | 0 | 4.093 ms | 0 |
| Aspect Design System | 6.702 ms | 0 | 2.130 ms | 0 |
| Motion | 11.207 ms | 0 | 6.298 ms | 0 |
| Frame Pipeline | 6.607 ms | 0 | 7.442 ms | 0 |
| Diagnostics | 5.200 ms | 0 | 3.714 ms | 0 |

The report contains 360 samples for each measured chapter and no Welcome samples.
Focused cache, allocation, pixel, and lifecycle coverage passed 68/68, including
A-B-A retention, canonical animated variants, deterministic LRU eviction, dependent
brush cleanup, and delayed disposal of a shared atlas until its final entry is
evicted. No Presentation view content or visibility behavior was changed to obtain
this result.

## Stage 4 three-run gate

The exact gate command was then run three consecutive times in fresh Release
processes on the same machine and with the same parameters:

```powershell
dotnet run -c Release --project .\benchmarks\Cerneala.PresentationFrameBudget\Cerneala.PresentationFrameBudget.csproj -- --cycles 8 --frames-per-load 45 --budget-ms 16.6667
```

| Report | Maximum frame | Frames over | Samples per chapter | Welcome samples |
| --- | ---: | ---: | ---: | ---: |
| `presentation-frame-budget-20260717-151648.json` | 15.9832 ms | 0 | 360 | 0 |
| `presentation-frame-budget-20260717-151735.json` | 16.2858 ms | 0 | 360 | 0 |
| `presentation-frame-budget-20260717-151820.json` | 15.3977 ms | 0 | 360 | 0 |

Every report contains 2,160 samples: 360 for each of Retained Model, Build-Time
Markup, Aspect Design System, Motion, Frame Pipeline, and Diagnostics. The frame
callback records already-computed processing time, phase diagnostics, allocation
deltas, and GC counters only. It neither forces collection nor performs file I/O;
JSON serialization happens after all measured loads finish.

Success, deliberate gate failure, and one-second timeout probes all left zero
`CernealaPresentation` or frame-budget runner processes. The failure probe exited
`1`, while timeout exited `4` after killing the complete process tree.

## Stage 5 final verification

After the final API-compatibility cleanup, the exact gate command produced
`presentation-frame-budget-20260717-153628.json` with 2,160 complete samples,
zero Welcome samples, and zero frames over budget. The global maximum was
`14.763 ms`, compared with `52.880 ms` and 113 over-budget frames in the RED
baseline.

SDK `ApiCompat` found no removals or incompatible changes against `HEAD`. Strict
comparison reported only the additive `UiFrame.ProcessingTime` property and the
opt-in `UIRoot(InvalidationTrace, ...)` diagnostics constructor. Both are documented
on their existing class pages; the original `UIRoot` constructor remains available,
and no manifest entry was added or renamed. All detailed phase timing remains
internal rather than becoming a public profiler API.

The user also completed the required visual pass at scale 1, 1.25, 1.5, and 2 and
confirmed that static and Motion text remained clear and stable, without jitter,
ghosting, blur, or color/gamma changes.
