# Blob cache admission fix — 2026-09-09

## Scope and implementation

This fixes the approved contract: recurring new text must become reusable after
the per-typeface blob cache saturates, without increasing its 4,096-entry limit.
It does not fix the separate gradient/ImageBrush rasterization RED, introduce a
glyph atlas, change raster quality, or add prewarming.

The old `SkiaTextBlobCache` kept its first 4,096 entries indefinitely. After that,
every new-key rental built and disposed a temporary blob. Its separate concurrent
dictionary count check and insertion also allowed a capacity race.

The replacement stays inside `Drawing/Text/SkiaTextBlobCache.cs`:

- FIFO eviction follows the existing neighboring shaping-cache policy.
- A per-typeface lock makes lookup, admission, eviction and lease acquisition
  atomic. It also serializes blob creation for that typeface.
- The cache and its active leases own separate references to an entry. Eviction
  releases the cache reference; native disposal waits for the last active lease.
- Each lease is an idempotently disposable reference object, so duplicate returns
  cannot destroy a blob still used by another lease.
- The old per-rental factory closure is removed. Warm rentals still allocate;
  the measured allocation is 24 B, not zero.

The entry limit covers retained cache membership. Evicted blobs borrowed by
active callers must remain alive until those callers release them; this is not
a new global native-byte budget. Keys, native blob construction, shaping,
rasterization, atlas policy and public API signatures are unchanged.

## RED and GREEN

Permanent coverage is in `tests/Cerneala.Tests/Drawing/TextPipelineTests.cs`.

Before the production change, the six focused blob cases produced **2 PASS /
4 RED**:

- Empty-cache admission and concurrent same-key sharing passed.
- Saturated admission reused no blob in 255 repeats after the initial request.
- Both eviction/lifetime cases failed because the old cache never evicted the
  first blob, even after 4,096 replacement keys.
- The bounded concurrency case actually observed **4,097 entries**, exceeding
  4,096. Its workload is 8,192 precomputed shaped labels with parallelism capped
  at eight, while an earlier blob remains borrowed.

After the fix, the complete `TextPipelineTests` group passes **45/45**. Coverage
includes active and already-returned leases, duplicate disposal, disposal after
the last borrower, recreated blob bounds, concurrent same-key sharing, capacity
under concurrent admissions, and the original 256-request admission regression.
Each saturation fixture uses a separately loaded typeface to avoid modifying
other tests' shared font caches.

```powershell
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~TextBlobCache --logger "trx;LogFileName=blob-lifetime-red.trx" --results-directory "$env:LOCALAPPDATA\Temp\CernealaTextCharacterization-20260909" --verbosity minimal
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~TextPipelineTests --logger "trx;LogFileName=blob-admission-green.trx" --results-directory "$env:LOCALAPPDATA\Temp\CernealaTextCharacterization-20260909" --verbosity minimal
```

## Measured comparison

The unchanged characterization runner was repeated for all 13 cases, in three
fresh processes per case: **39 post-fix reports**. The data and raw-file hashes
are in [blob-fix-summary.json](blob-fix-summary.json); the original baseline
remains in [summary.json](summary.json).

```powershell
.\benchmarks\Cerneala.Benchmarks\Measure-TextCharacterization.ps1 -NoBuild -Runs 3 -OutputDirectory "$env:LOCALAPPDATA\Temp\CernealaTextBlobFix-20260909"
```

The blob probe inserts 8,192 unique keys, then replays the latest key 256 times.
Shapes are precomputed before these measurements. Ranges below are the three
processes' medians, not pooled percentiles or frame times.

| Text length | Latest-key P50 before, us | Latest-key P50 after, us | Managed B/call before | Managed B/call after |
|---|---:|---:|---:|---:|
| 32 | 27.6–27.9 | 0.3 | 656 | 24 |
| 512 | 18.8–34.8 | 0.6–0.7 | about 4,496 | 24 |

All six post-fix blob runs retain 4,096 entries after overflow and report reuse
of the late key. The earliest key can now be evicted and subsequently readmitted:
its 256-request replay includes one miss, with mean allocations of 26.25 B for
32 characters and 41.25 B for 512 characters.

This is not a zero-overhead synchronization claim. Earliest-key replay P50 for
512 characters is 0.6–0.8 us after the fix versus 0.5–0.6 us before. The formerly
uncached repeated workload benefits substantially; the new lease and locking
still have a cost.

Native steady-state raster counts are unchanged: zero for identical text, the
64-label set, the 64-string cycle and all 64 baseline phases; 256 for 256 newly
generated whole strings. Native CPU timings vary across runs and are not used
to claim a whole-frame speedup from this blob-cache fix. Every native fixture
reported zero device-owned cached entries/textures after disposal.

The 39-report check verified case/repetition counts, row sample counts,
allocation-summary arithmetic, native raster-counter sums and four constant
managed binary hashes across the matrix. Raw arrays and logs remain under
`C:\Users\lauri\AppData\Local\Temp\CernealaTextBlobFix-20260909`.

## Repository verification

- Complete Release build: **0 warnings / 0 errors**.
- Focused `TextPipelineTests`: **45 PASS / 0 FAIL**.
- Complete principal `Cerneala.Tests` project: **3,327 PASS / 0 FAIL**.
- Roslyn index refreshed after the production change.
- Native-enabled complete solution suite: **4,873 PASS / 7 FAIL / 0 skipped**
  across 4,880 tests. **The full-suite gate remains RED and is not waived.**
- Historical-reference native pixel conformance: **133 PASS / 0 FAIL**.
- Native multi-window smoke: PASS, two main frames and one secondary frame.
  Its `inputObserved=False` is not input validation; the separate native
  ownership/input test passed in the complete suite.
- PrismAudit: PASS, zero gaps. Offline SDL shader verification: **6 artifacts PASS**.
- The four benchmark binary hashes were unchanged after verification.
- `git diff --check`: PASS.

The seven complete-suite failures were investigated separately:

1. Gradient and ImageBrush replay still perform **16 rasterizations / 147,600
   layer pixels in 16 warm frames**, identical to their pre-fix RED. Blob reuse
   does not remove the renderer's unconditional rasterization of those brushes.
2. `OpaqueStrokeOccludesEarlierStrokeAroundTranslucentContent`, cases 1/3/6/7,
   still reports deltas **47/46/27/25**. All four test names and error messages
   were matched exactly against the preexisting
   `CernealaMotion120Hz-20260908/alpha-baseline-head.trx` evidence.
3. `WarmStaticAnimatedAndABALookupsDoNotAllocateOrMiss(64)` reports **1,664 B**
   against its **1,024 B** limit. The same test previously failed with 4,160 B;
   its allocation owner remains unestablished. Source inspection confirms that
   the measured loop calls `Fixture.TryGet` and
   `SdlGpuDrawingResources.TryGetTextAtlasEntries` over already supplied dummy
   raster data. It does not call the rasterizer or `SkiaTextBlobCache.Rent`.
   A fresh-process recheck of all 35 text-cache contracts produced **33 PASS /
   2 RED** (only gradient/ImageBrush); the allocation failure did not recur.
   That recheck does not erase the complete-suite failure or prove its cause.

No tolerance, allocation limit, warmup count, compiler policy or existing RED
expectation was weakened. No production code outside the blob cache was changed
for this fix. No human validation or cross-platform validation has been claimed.

The complete-suite TRX files and console log are under the post-fix raw directory's
`full-suite/` and `full-suite.log`; the pixel and focused-recheck results are
`pixel-conformance.trx` and `text-contracts-recheck.trx` there. Smoke artifacts are
under `smoke/` and were produced by the application-owned capture path.

Commands for the broader gates, from the repository root:

```powershell
dotnet build .\Cerneala.slnx -c Release --no-restore --verbosity minimal
$env:CERNEALA_SDL_NATIVE_TESTS = '1'
dotnet test .\Cerneala.slnx -c Release --no-build --no-restore -m:1 --logger trx --results-directory "$env:LOCALAPPDATA\Temp\CernealaTextBlobFix-20260909\full-suite" --verbosity minimal
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj -c Release --no-build --no-restore --filter "FullyQualifiedName~PrismSdlGpuPixelConformanceTests|FullyQualifiedName~SdlGpuDrawingConformanceTests" --logger "trx;LogFileName=pixel-conformance.trx" --results-directory "$env:LOCALAPPDATA\Temp\CernealaTextBlobFix-20260909" --verbosity minimal
dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj -c Release --no-build --no-restore --filter FullyQualifiedName~SdlGpuTextCacheContractTests --logger "trx;LogFileName=text-contracts-recheck.trx" --results-directory "$env:LOCALAPPDATA\Temp\CernealaTextBlobFix-20260909" --verbosity minimal
dotnet run --project .\tests\Cerneala.SdlGpuSmoke\Cerneala.SdlGpuSmoke.csproj -c Release --no-build --no-restore -- --mode multi-window --artifacts "$env:LOCALAPPDATA\Temp\CernealaTextBlobFix-20260909\smoke"
dotnet run --project .\Tools\PrismAudit\PrismAudit.csproj -c Release --no-build --no-restore -- --check
dotnet run --project .\Tools\Cerneala.SdlShaderCompiler\Cerneala.SdlShaderCompiler.csproj -c Release --no-build --no-restore -- --verify
```

The executed test commands restored the previous `CERNEALA_SDL_NATIVE_TESTS`
value in `finally`. Use a separate evidence directory for another run.
