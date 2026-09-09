# Remaining text failures — 2026-09-09

Scope: the two warm gradient/ImageBrush rasterization failures and the intermittent
warm atlas-lookup allocation failure. The four preexisting alpha-blending failures
are outside this change. No allocation threshold, pixel tolerance, production GC
policy or compiler policy is relaxed.

## Warm non-solid text

The existing regression reproduced **16 CPU rasterizations / 147,600 layer pixels
in 16 unchanged warm frames**, for both gradient and ImageBrush text. Textures were
already reused; `AddTextRun` rasterized before reaching their resource lookup.

The fix queries the existing device-owned texture cache before rasterization and
stores the tight raster's origin alongside its texture dimensions. Gradient text
reuses its colorized texture; tile text reuses its alpha-coverage texture. Tile
brush capture still records and checks the current brush content, and reapplies
coverage when that content changes. The existing per-backend reference retention,
retirement and device ownership remain in control. No parallel text cache was
introduced. The solid-text atlas path and its page budget are unchanged.

Nine additional native regression cases were compiled against the pre-fix backend
without rebuilding project references: all nine were RED on a repeated raster
request (expected 0, actual 1). The backend SHA-256 was
`F98D1CCA859AF83E7D41D2D95376911930C43D4E445C113F69F13B5AFED1D9E7`.
These cover gradient/image pixels at 1/1.25/1.5/2 scale, whole-pixel baseline
translation, retirement/recreation, and a VisualBrush source changing red to blue.

The first post-fix focused run passed **59/59**, including all nine new cases,
the original 35 cache contracts, brush rendering and brush-texture lifetime tests.
After the measurement fix, the expanded focused run passed **65/65**, including the existing native atlas retention cases and the allocation positive control.

## Allocation failure ownership

The unchanged test performs 1,000 warmed atlas lookups and allows at most 1,024 B.
Its measured path does not shape or rasterize text or rent text blobs.

Eight fresh-process focused runs passed all three variants, and the native SDL
pre-fix project run passed this test as well: **552 PASS / 6 FAIL / 558 total**,
with only the two known brush failures and four known alpha failures. Those
passes did not erase the earlier complete-suite allocation failure.

A temporary .NET 8.0.30 harness called the existing 64-variant test 256 times:
**10/256 failures**, reporting **2,736–6,560 B**. This reproduced the counter
failure without changing the test or its limit. With background GC disabled only
for a diagnostic child process, **0/256 failed**. This switch is not part of the
fix or acceptance commands.

A control removed every Cerneala call from the measured interval. After allocation
pressure outside that interval, 1,000 `Thread.SpinWait(4)` calls produced counter
deltas of **3,584 B and 7,152 B** in two of 256 intervals. With background GC
disabled, all 256 control intervals reported zero. Thus the failing counter delta
is not evidence of allocations by the atlas lookup.

The pinned [.NET 8.0.30 counter implementation](https://github.com/dotnet/runtime/blob/v8.0.30/src/coreclr/vm/comutilnative.cpp#L868-L877)
subtracts unused allocation-context space. The runtime's
[background-GC end-mark path](https://github.com/dotnet/runtime/blob/v8.0.30/src/coreclr/gc/gc.cpp#L35593-L35595)
voids those contexts; its
[`void_allocation` implementation](https://github.com/dotnet/runtime/blob/v8.0.30/src/coreclr/gc/gc.cpp#L7346-L7355)
clears their pointers without the counter adjustment made by normal GC context
cleanup. This source mechanism explains the control experiment's false-positive
deltas. No CLR patch or runtime upgrade is included here.

Two all-object EventPipe allocation traces altered reproduction timing and passed
256/256. The first trace contained 359,054 allocation events, with only five
lookup-stack allocation events; neither trace reproduced the failing delta. These
traced passes are diagnostic observations, not substitute acceptance results.

The fixture fix isolates only the measurement interval in a checked, bounded
no-GC region and restores it in `finally`. Region acquisition or invalidation
fails the test rather than silently bypassing it. The test shares SDL's existing
nonparallel collection because the reservation is process-wide. The 1,000 lookups,
existing cache warmup and 1,024 B limit are unchanged. A positive-control test
requires the same measurement helper to detect a real 2,048-byte array allocation;
it also checks a no-op interval.

## Raw evidence and commands

Raw TRX files, control outputs and traces are outside the repository under
`%LOCALAPPDATA%/Temp/CernealaRemainingTextFix-20260909/`:

- `text-before.trx`: original 33 PASS / 2 RED.
- `allocation-before-1.trx` through `allocation-before-8.trx`: isolated runs.
- `sdl-before.trx`: the pre-fix native SDL project run.
- `brush-native-before.trx`: nine additional native RED cases.
- `brush-focused-after.trx`: first 59-case GREEN.
- `allocation-stress-before.txt`: 256-call reproduction.
- `allocation-stress-no-background-gc.txt`: diagnostic GC-mode control.
- `allocation-counter-only.txt` and `allocation-counter-no-background-gc.txt`:
  no-Cerneala counter controls.
- `allocation-stress.nettrace`, `allocation-stress-2.nettrace`,
  `allocation-stress-analysis.txt`, `allocation-stress-traced.txt`: diagnostics.
- `full-suite/`, `full-suite.txt`, `full-suite-summary.json`: complete solution run.
- `alpha-baseline-comparison.json`: exact failure-name/message comparison with
  `%LOCALAPPDATA%/Temp/CernealaMotion120Hz-20260908/alpha-baseline-head.trx`.
- `pixel-conformance.trx`, `pixel-conformance.txt`: complete 133-case pixel corpus.
- `smoke.txt`, `smoke/`: multi-window runtime result and application-owned captures.

Normal focused acceptance uses the default runtime configuration:

```powershell
$env:CERNEALA_SDL_NATIVE_TESTS = '1'
dotnet test tests/Cerneala.Tests.SdlGpu/Cerneala.Tests.SdlGpu.csproj -c Release --no-restore --filter 'FullyQualifiedName~SdlGpuTextCacheContractTests|FullyQualifiedName~SdlGpuTextCacheTests|FullyQualifiedName~SdlGpuBrushTextureLifetimeTests|FullyQualifiedName~BrushRenderingTests' --logger 'trx;LogFileName=text-focused-after.trx' --results-directory "$env:LOCALAPPDATA/Temp/CernealaRemainingTextFix-20260909"
```

## Final verification

- Release solution build: **0 warnings / 0 errors**.
- Focused native-enabled text/brush group: **65/65 PASS** (`text-focused-after.trx`).
- Original 256-call allocation stress after the fixture correction: **0 failures**,
  default .NET 8.0.30 GC configuration (`allocation-stress-after.txt`).
- Prism audit: **PASS**, 178 catalog entries / 31 common properties /
  216 public Prism types / 7 extended public types / zero gaps.
- `git diff --check`: PASS.
- Offline shader verification: **6/6 PASS** (`shader-verify.txt`).
- Full native-enabled solution suite: **4,886 PASS / 4 FAIL / 4,890 total**,
  zero skipped. Core passed **3,327/3,327**; SDL passed **564/568**.
  The only failures are the four preexisting alpha cases: content 1/3/6/7,
  delta 47/46/27/25 respectively. Their names and error messages match the
  preexisting baseline exactly. The full-suite gate remains RED; it is not waived.
- Native pixel-conformance corpus: **133/133 PASS**.
- Multi-window runtime smoke: **PASS**, 2 main-window frames / 1 secondary-window
  frame, with `Window.SaveScreenshot` captures. Its `inputObserved=False` result
  does not establish input coverage or human validation.
- No human or cross-platform validation is claimed.

Verification boundary: these results apply to the Release assemblies built for
this change (Core test assembly timestamp: 13:52:12 UTC). Two externally added
test files appeared at 14:17:59 UTC, after the complete suite finished:
`tests/Cerneala.Tests/UI/Controls/Shapes/ShapeRenderingContractTests.cs` and
`tests/Cerneala.Tests/UI/Rendering/RetainedTransformContractTests.cs`. They were
left untouched and are not included in the counts or verification claims above.

The temporary harness source and project were removed. The user removed the
remaining ignored `bin/obj` outputs; the harness directory's absence was verified.
The temporary CSI analyzer and its derived ETLX were removed; raw test evidence
and the two small diagnostic traces remain outside the repository.

Full-suite command (exit 1, solely the four alpha failures above):

```powershell
$env:CERNEALA_SDL_NATIVE_TESTS = '1'
dotnet test Cerneala.slnx -c Release --no-build --no-restore -m:1 --logger trx --results-directory "$env:LOCALAPPDATA/Temp/CernealaRemainingTextFix-20260909/full-suite" --verbosity minimal
```

Final rendering gates (both exit 0):

```powershell
$env:CERNEALA_SDL_NATIVE_TESTS = '1'
dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~PrismSdlGpuPixelConformanceTests|FullyQualifiedName~SdlGpuDrawingConformanceTests' --logger 'trx;LogFileName=pixel-conformance.trx' --results-directory "$env:LOCALAPPDATA/Temp/CernealaRemainingTextFix-20260909" --verbosity minimal
dotnet run --project tests/Cerneala.SdlGpuSmoke/Cerneala.SdlGpuSmoke.csproj -c Release --no-build --no-restore -- --mode multi-window --artifacts "$env:LOCALAPPDATA/Temp/CernealaRemainingTextFix-20260909/smoke"
```

Human validation remains unperformed: inspect the application's gradient/image
text through ordinary navigation and resizing; it should retain its appearance
without stale brush content. The automated regressions additionally assert exact
cold/warm RGBA equality and zero repeated CPU rasterization for unchanged frames.
