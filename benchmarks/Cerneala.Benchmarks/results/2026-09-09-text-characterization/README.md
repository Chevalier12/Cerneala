# Text characterization — 2026-09-09

## Baseline status

The subsequent bounded blob-admission fix and its verification are recorded in
[blob-fix.md](blob-fix.md). This page preserves the earlier characterization and
pre-fix RED evidence; the original measured baseline has not been overwritten.

13 cases completed in three fresh processes each: 39 reports. This is a measured
baseline, not a new performance acceptance gate. No production code, cache
policy, quality setting, public API or compiler/publishing policy was changed
for that baseline.

The earlier gradient/ImageBrush RED is still intentionally open. The other
observations are classified below rather than manufacturing four more failures.

After this baseline, the user approved bounded admission of recurring new text
after blob-cache saturation. The permanent RED and its control are recorded below;
production was still unchanged when those RED results were captured.

## Reproduction and evidence

```powershell
.\benchmarks\Cerneala.Benchmarks\Measure-TextCharacterization.ps1 -NoBuild -Runs 3 -OutputDirectory "$env:LOCALAPPDATA\Temp\CernealaTextCharacterization-20260909"
```

Use a new output directory when repeating. Build the benchmark in normal Release
first, or omit `-NoBuild`. Base commit: `1f2a14d82ff14777e731e719f46780c2b370faa8`,
with the characterization runner and preceding RED test uncommitted at capture.
The exact dirty state, managed binary hashes, hardware and runtime are retained
in [summary.json](summary.json). All four recorded binary hashes were constant
across all 39 reports.

Raw per-operation/per-frame JSON and logs are in
`C:\Users\lauri\AppData\Local\Temp\CernealaTextCharacterization-20260909`.
The summary retains every reported row's statistics, memory checkpoints and raw
file SHA-256; raw sample arrays remain outside the repository. The earlier
`CernealaTextCharacterization-20260909-smoke` directory contains the separate
one-process-per-case fixture validation, not the three-run baseline.

Environment: Intel Core i5-9300H (4 cores/8 logical), High performance power plan,
Windows, .NET 8 normal Release, actual Arial at 16 logical units. The environment
record lists Intel UHD 630/RTX 2060/Parsec adapters; it is not proof of which GPU
SDL selected. Relevant DOTNET/COMPlus tiering/ReadyToRun overrides, diagnostic
ports and CLR profiling enablement were unset when checked after the matrix.
No build, indexer, profiler or second benchmark ran during measurements.

## 1. Whole-run reuse versus continually new text

Native cases use a real 960x640 SDL_GPU window, scale 1, no MSAA and
`CompleteFrame(present: false)`. They are backend-only: prebuilt commands and
analysis, no UI layout/input, no VSync pacing and no isolated GPU-time query.
CPU wall time includes submission and any native waits.

Ranges below are the minimum/maximum of the three runs' respective statistics,
not pooled percentiles. Each steady block has 256 samples.

| Case | Steady raster requests/run | CPU wall P50, us | CPU wall P95, us | Final atlas pages |
|---|---:|---:|---:|---:|
| Identical ten-character run | 0 | 115.4–142.0 | 446.4–520.4 | 1 |
| Continually new ten-digit run | 256 | 475.5–693.9 | 1442.6–1618.9 | 4 |
| Replay of 64 populated ten-digit runs | 0 | 111.1–147.9 | 339.6–505.5 | 1 |
| Replay of all 64 baseline phases | 0 | 125.2–154.0 | 380.3–547.9 | 1 |
| Replay of 64 distinct labels/frame | 0 | 832.3–1009.0 | 2000.5–2137.8 | 2 |

The continually new run case uses only digits already encountered in the
population block, but each whole string is new. It produces 256 raster requests
in 256 measured frames in all three runs. Rasterization P50 is 189.4–275.5 us.
This demonstrates the whole-run cache's workload cost, not that a particular
replacement algorithm has been selected or proven faster.

Its mean managed allocation is 54,854–55,078 B/frame **including three lazy atlas
page-growth frames**. Each new page adds a 4 MiB CPU pixel array. The other 253
frames average 5,682–5,908 B/frame. All frames remain in the main statistics;
the growth/non-growth split in the summary is attribution, not gate filtering.
Final page payload is 16 MiB CPU plus 16 MiB GPU, excluding metadata/driver cost.

The identical-run backend path measures 616 B/frame, not zero total frame
allocations, even though its text raster requests and rasterized pixels are zero.
The 64-label case measures 2,632 B/frame and 192 actual draw calls/frame.
That draw count is an observation, not permission to reorder RGB draws.

## 2. The 64 phases are bounded and reusable

The phase case visits the full 8x8 canonical grid. First frame plus population
produce exactly 64 raster requests and 192 RGB-channel entries, fitting in one
page. All 768 subsequent replay frames across the three processes have zero
raster requests and zero rasterized pixels.

Conclusion: phase cardinality is a deliberate quality/cache tradeoff, not an
observed unbounded-key or perpetual-rasterization defect in this workload.
This does not establish behavior under a larger working set or another DPI.

## 3. First use is distinct from steady state

For the 32-character CPU pipeline:

- first shaping call: 30.83–45.47 ms;
- first blob call after shaping is warm: 6.36–12.28 ms;
- first raster call after shaping and blob are warm: 24.79–39.47 ms;
- repeated direct rasterization: P50 140.2–254.4 us.

For 512 characters, repeated direct rasterization has P50 1.569–1.798 ms.
These direct raster calls deliberately bypass the backend atlas; they are not
the cost of drawing an already cached label.

The first native frame with 64 labels costs 202.81–216.97 ms, of which the backend
raster timer records 85.13–97.90 ms. This frame includes first-use backend/JIT
work. It is **not** comparable to first entering Motion in an already running
application, nor does it isolate the entire frame's text ownership.

The CPU stage rows are sequential first calls with explicit warm dependencies
and prepared instances/inputs. They are not independent cold processes per stage
and must not be summed into a startup breakdown. No thresholds or prewarm policy
are introduced by this baseline.

Additional measured warm costs: font resolution returns about 40 managed B/call;
shape-cache replay 96 B/call; cached blob replay about 112 B/call; layout replay
480 B/call. The empty observer control records zero managed allocation. These
are explicit method-call costs, not proof those methods run every retained frame.
They do not explain the separate historical 4,160-byte atlas-lookup test anomaly.

## 4. Entry limits do not imply a uniform byte budget

Shape probes insert 8,192 unique keys into a single typeface cache. Both retain
4,096 entries after saturation:

| UTF-16 characters/run | Retained shape entries | Glyph ID/position array payload |
|---|---:|---:|
| 32 | 4,096 | 1,310,720 B = 1.25 MiB |
| 512 | 4,096 | 20,971,520 B = 20 MiB |

That is an exact 16x payload difference at the same entry count, excluding
headers, dictionaries and shared input strings. Read-only reflection counts
the already created shapes outside timed operations. No cache is modified by
the observer.

The layout probe retains 512 layout entries after 1,024 distinct strings, while
the shared shaping cache retains all 1,024 shapes. Thus the layout entry cap is
not a cap on the whole text pipeline. Managed-live and process-private snapshots
in the summary include their caveats: native allocators, runtime reserves,
font data and small benchmark metadata prevent exact cache attribution.

### Saturated blob cache does not admit the later working set

After 8,192 distinct requests, the blob cache still contains its first 4,096
entries. Repeated rentals of the earliest key return the same blob; rentals
of the latest key do not, in every run.

| Text length | Earliest cached key P50, us | Latest uncached key P50, us | Latest-key managed B/call |
|---|---:|---:|---:|
| 32 | 0.2–0.3 | 27.6–27.9 | 656 |
| 512 | 0.5–0.6 | 18.8–34.8 | about 4,496 |

The late key remains uncached over 256 replays. Shapes are precomputed and held
before the blob probe's memory baseline, so this difference does not include
repeated shaping. This is a demonstrated policy cost, not a memory leak or an
already selected replacement policy. The subsequently approved admission
contract is covered by the RED below; no eviction algorithm has been selected.

### Approved admission RED

`TextPipelineTests.TextBlobCacheAdmitsRecurringTextWithoutExceedingCapacity`
uses a separately loaded typeface, either zero or 4,096 earlier entries, and
256 consecutive requests for one new shaped label. It checks the existing
4,096-entry bound after every rental and requires observed blob reuse within
that bounded window. It does not require immediate admission or a particular
replacement policy. Managed object identity is compared after sequential rentals;
native handles are not compared because disposed handles can be recycled.

Results in two test processes:

- Empty-cache control: PASS in both runs.
- Saturated cache: RED in both runs, **0/255 repeat requests reuse a blob**;
  the cache remains at 4,096 entries and every capacity assertion passes.
- The second run includes all `TextPipelineTests`: **40 PASS / 1 intentional RED**.

```powershell
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj -c Release --filter FullyQualifiedName~TextBlobCacheAdmitsRecurringTextWithoutExceedingCapacity --logger "trx;LogFileName=blob-admission-red-1.trx" --results-directory "$env:LOCALAPPDATA\Temp\CernealaTextCharacterization-20260909" --verbosity minimal
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj -c Release --no-build --no-restore --filter FullyQualifiedName~TextPipelineTests --logger "trx;LogFileName=blob-admission-text-pipeline-red.trx" --results-directory "$env:LOCALAPPDATA\Temp\CernealaTextCharacterization-20260909" --verbosity minimal
```

The first command built successfully with no reported warnings/errors before
running the expected failure. TRX files remain in the external evidence directory.
No production change or full-suite run was made for this test-only addition.

## Verification and limits

- Benchmark Release build: zero warnings/errors.
- PowerShell syntax parse passed; all 13 smoke cases and all 39 baseline cases completed.
- Checked sample counts, allocation-summary arithmetic, native raster-counter
  sums, three reports per case and unchanged managed binary hashes.
- Invalid case exits nonzero without producing a report. The collection script
  refuses an existing output directory before writes or builds.
- Every native run disposed its cached text/texture entries; no benchmark
  process remained after completion. This is not an independent native leak audit.
- Existing focused cache contracts: 33 PASS, 2 intentional RED (unchanged
  gradient/ImageBrush text: 16 rasterizations/147,600 layer pixels in 16 frames).
- No full solution suite, visual conformance, human validation or cross-platform
  run was performed for these benchmark-only changes.

The subsequent [remaining text fixes](remaining-text-fixes.md) record the
gradient/ImageBrush RED-to-GREEN change and the separately reproduced allocation
measurement defect. The baseline results above remain unchanged.

Next decisions beyond these fixes: choose representative dynamic-text/first-use
workloads and CPU/memory budgets.
Do not turn whole-run caching or the 64-phase grid into failing
architecture assertions merely to obtain more RED tests.
