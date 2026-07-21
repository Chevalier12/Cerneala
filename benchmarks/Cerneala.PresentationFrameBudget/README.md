# Cerneala Presentation frame-budget benchmark

This Windows-only benchmark launches the real WindowsDX presentation window, navigates
through the seven measured chapters with automation peers, including the animated
Solar Motion Prism dogfood scene, and captures every native
`UiFrame.ProcessingTime` sample. Welcome is navigation-only and is never measured.
It requires Windows with a working Direct3D 11 adapter and the WindowsDX Presentation
build; it is not a headless or cross-platform benchmark.

Run from the repository root:

```powershell
dotnet run -c Release --project .\benchmarks\Cerneala.PresentationFrameBudget\Cerneala.PresentationFrameBudget.csproj -- --cycles 8 --frames-per-load 45 --budget-ms 16.6667 --cold-budget-ms 500
```

The defaults are eight cycles, 45 frames per chapter load, a `16.6667 ms` warm
p99 budget, a `500 ms` one-time cold-frame ceiling, and a four-minute timeout.
Use `--report <path>` to select the JSON report location or
`--timeout-seconds <seconds>` to override the timeout. The separate cold ceiling
keeps shader, JIT, text-atlas, and first-surface initialization visible without
pretending that an operating-system scheduling spike is steady-state frame cost.

The schema-versioned JSON report contains the run configuration plus one sample per
measured frame. Each sample identifies the cycle, chapter, frame index, cold/warm
state, `ProcessingTimeMs`, elapsed time, retained-frame statistics, internal phase
timings, text request/pixel counts, allocation delta, and Gen 0/1/2 collection deltas.
Serialization happens only after every measured load has completed.

The command exits non-zero when the presentation fails, hangs, reports an
asynchronous automation error, omits a chapter or frame, includes Welcome, lets
any cold sample exceed the cold ceiling, or lets any chapter's warm p99 exceed
the frame target. The console summary and JSON still preserve every sample and
print every individual frame above the warm target, including cold samples, so
the percentile gate cannot hide outliers. Results are environment-specific and
should be compared only on the same WindowsDX machine and Release configuration.
