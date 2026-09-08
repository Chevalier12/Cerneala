# Prism architecture remediation — 2026-09-08

## Scope and decisions

- Implement shared, explicit multipass planning, not merely additional SDL counters.
- Keep the SDL configuration internal; add no public configuration facade.
- Preserve capture of the retained visual subtree, including descendants. The user
  confirmed this contract; the proposal, technical design, guide and canonical API
  documentation are synchronized without changing runtime subtree ownership.
- `ControlBounds` limits source capture before effects, including transformed scopes.
  Correct a reference fixture that draws beyond that boundary rather than widening
  the renderer's capture bounds.
- Preserve strict rejection of invalid composition/backdrop profiles. The initial
  audit's public fallback-defect claim was not reproduced and is withdrawn; do not
  add support for malformed internal graphs.

## Reproductions and changes

### Explicit multipass planning

The semantic plan did not describe the auxiliary Otsu, shadow, JFA+1 or bevel
passes executed inside SDL. Regression tests compared execution diagnostics with
actual fragment-uniform submissions and failed before the change.

`Drawing/Prism/Graph/PrismRasterPlanner.cs` now lowers those operations to explicit
nodes, dependencies, surface dimensions/formats and lifetimes. It shares distance
fields by source node and coverage mode. SDL consumes that plan through its normal
execution loop instead of allocating hidden scratch passes. Retained hits prune
the complete auxiliary dependency graph.

Coverage: `PrismRasterPlannerTests`, the multipass accounting/seed tests in
`SdlGpuPrismExecutorTests`, and native Prism conformance.

### Backdrop contribution metadata

The analyzer inferred mode/opacity pairs from names ending in `BlendMode`. It
therefore missed Bevel's `HighlightMode` and `ShadowMode`. Three regressions were
RED: highlight-only, shadow-only, and a reused analyzer after an opacity change.

The catalog now declares eleven `blendOpacity` relationships. The compiler
validates them and generates typed mode/opacity keys. Frame analysis reads those
keys directly, including both Bevel contributions. Six runtime-analysis tests
and seven additional compiler tests cover the contract and validation.

No timing or allocation improvement is claimed without a benchmark.

### Source capture bounds and reference corrections

The original `mask-transform` reference expected pixel `(9,9)` to be
`#ff0e0f17`, while SDL returned background `#ff090d15` (tolerance 2). Its scope
started at host x=12 after translation, but a source rectangle started at x=10.

The confirmed capture contract exposed a separate renderer violation: drawing
an overflowing rectangle versus its intersection with `ControlBounds` produced
different filtered pixels. The native regression failed at pixel index 1842
before the fix and passes afterward. The capture now uses the existing rectangle
and stencil clipping path before filtering. Four additional native cases cover
rotation and coordinate scales 1 and 1.5 against equivalent explicit clips.

The mask fixture's rectangle now begins at x=12. Only `mask-transform.png` and
`style-bevel-emboss.png` were replaced. The latter reference had encoded Bevel's
missing-backdrop behavior. Both replacements were captured through the real
window's `Window.SaveScreenshot` API and compared against the native command-list
fixture over every RGBA channel (maximum allowed difference 2). The remaining
nineteen baseline images and their tolerances were not changed.

`tests/Cerneala.Tests/Golden/Prism/conformance.json` records per-scene SDL
provenance, reasons and SHA-256 hashes instead of mislabeling these captures as
historical WindowsDX output. Temporary capture instrumentation was removed.

### Fallback finding correction

`PrismGraphBuilder.SnapshotCompositionSettings` validates the composition profile.
`PrismBackdropFramePolicy.Prepare` validates backdrop metadata, including a default
or otherwise incomplete struct, before graph construction. No supported public
path to the executor's invalid-profile branch was established. Internal policy
and defensive execution code were left unchanged by explicit user decision.

## Verification ledger

Commands run from the repository root; native runs set `CERNEALA_SDL_NATIVE_TESTS=1`.

| Gate | Result |
| --- | --- |
| Core Prism/architecture/dependency tests after planner and metadata fixes | 712 passed, 1 native test skipped |
| SourceGen Prism tests | 63 passed |
| Native capture-bound regression | RED before fix; GREEN afterward |
| Native rotation/scale capture cases | 4 passed |
| Native Prism and architecture group after reference corrections | 292 passed, 0 failed, 0 skipped |
| Offline SDL shader verification | 6 artifacts verified |
| Documentation manifest | 1,119 entries; no missing files or duplicate paths |
| Full Release solution with native tests | Failed: 4,797 passed, 6 failed, 0 skipped; Visual Studio test project additionally blocked at build |
| Full core test project, native enabled | 3,319 passed, 0 failed, 0 skipped |
| Full SourceGen test project | 524 passed, 0 failed |
| PreviewHost dynamic-compilation recheck | 1 passed after rebuilding the stale Debug generator; no source change |
| Full PreviewHost project after rebuilding the Debug generator | 13 passed, 0 failed |
| Visual Studio project rerun in a separate build invocation | Extension build passed; 47 tests passed, 0 failed |
| Retained subtree contract after documentation synchronization | 16 passed, 0 failed, 0 skipped |
| Native OS-input recheck after the user released the desktop | 1 passed, 0 failed, 0 skipped; unchanged test |
| Generated completeness report (`--write`, then `--check`) | Passed: 178 catalog entries, 31 common properties, 216 public Prism types and 7 extended public types; zero inventory gaps |

The full-suite command is:

```powershell
$env:CERNEALA_SDL_NATIVE_TESTS='1'
dotnet test Cerneala.slnx -c Release -m:1 --logger trx `
  --results-directory .artifacts/prism-architecture-fixes/full-suite
```

Focused logs and TRX files are under `.artifacts/prism-architecture-fixes/`.
Automated window captures are not human manual validation. Linux/macOS native
runs and comparative performance measurements were not performed here.

### Full-suite failure disposition

- Four `AlphaBlendRenderingTests.OpaqueStrokeOccludesEarlierStrokeAroundTranslucentContent`
  cases failed for inputs 1, 7, 6 and 3 with differences 47, 25, 27 and 46 / 255.
  The same cases and differences are recorded in the
  [2026-09-05 backend retirement audit](2026-09-05-monogame-removal.md).
  The failing variants contain ordinary draw commands, not Prism operations;
  their test source is unchanged. No renderer workaround or tolerance relaxation
  was introduced. The user explicitly accepted these four pre-existing failures
  as an exception for this Prism remediation on September 8. The tests remain
  failing and unchanged; the exception is not a renderer correctness claim.
- `SdlWindowsNativeContractTests.NativeOwnershipInputAndGraphicsLifetimesRemainIndependent`
  refused to inject input because a different application's window occupied the
  target point. The guard failed before input injection. The agent did not close
  the other application; the user later confirmed closing the game.
  This test uses global cursor movement and Windows `SendInput` to verify native
  focus and SDL per-window input isolation. Servo-driven retained UI interaction
  cannot replace that OS-to-SDL contract. After the user released the desktop,
  the unchanged native test passed: 1 passed, 0 failed, 0 skipped. It was not
  replaced, weakened or waived. Evidence: `native-input-final.log` and
  `native-input-final.trx` under `.artifacts/prism-architecture-fixes/`.
- `PreviewHostTests.UnsavedValidMarkupCompilesThroughTheDynamicProject` could not
  resolve generated Prism enum types. `PreviewCompiler.LoadProjectAsync` selects
  Debug even when this test assembly runs in Release. The Debug generator DLL was
  from September 7, whereas the catalog and Release generator had changed on
  September 8. Rebuilding only `Cerneala.SourceGen` in Debug and rerunning the
  unchanged Release test made it pass. No PreviewHost production fix was made.
  The complete PreviewHost project subsequently passed all 13 tests.
- The Visual Studio extension build failed with `NETSDK1047`: the LanguageServer
  assets file lacked `net10.0/win-x64`. Inspection confirmed that the nested
  `PublishBundledServer` target requests that RID while the assets file contained
  only `net10.0`. The extension, LanguageServer and their project files are
  unchanged. Rerunning the Visual Studio test project in a separate build
  invocation completed both bundled publishes, built the VSIX and passed all
  47 tests. No project-file patch or build-target bypass was used. This resolves
  that project's verification, but does not turn the original solution run green.

The original full-solution run is not green. PreviewHost and Visual Studio passed
their separate rechecks; the four alpha-blend cases have an explicit exception.
The native OS-input gate also passed its separate recheck. All non-waived failures
from the original run have now passed their targeted or project rechecks. No new
full-solution run is claimed, and the four accepted alpha-blend failures remain.

The resolving build/test commands were:

```powershell
dotnet build Cerneala.SourceGen/Cerneala.SourceGen.csproj -c Debug --no-restore -m:1
dotnet test tests/Cerneala.Tests.PreviewHost/Cerneala.Tests.PreviewHost.csproj -c Release --no-build
dotnet test tests/Cerneala.Tests.VisualStudio/Cerneala.Tests.VisualStudio.csproj -c Release -m:1
dotnet run --project Tools/PrismAudit/PrismAudit.csproj -c Release -- --write
dotnet run --no-build --project Tools/PrismAudit/PrismAudit.csproj -c Release -- --check
```

The final native-input recheck was:

```powershell
$env:CERNEALA_SDL_NATIVE_TESTS='1'
dotnet test tests/Cerneala.Tests.SdlGpu/Cerneala.Tests.SdlGpu.csproj -c Release --no-build `
  --filter FullyQualifiedName~NativeOwnershipInputAndGraphicsLifetimesRemainIndependent `
  --logger 'trx;LogFileName=native-input-final.trx' `
  --results-directory .artifacts/prism-architecture-fixes
```

## Capture contract resolution

The user confirmed subtree capture limited by `ControlBounds`. The older
local-only statements and diagrams were corrected, including descendant cache
dependencies. Existing `PrismRetainedCommandContractTests` cover owned-subtree
delimiters, nested scopes, Presence-exit children and nested value invalidation.
No runtime subtree-ownership change was needed.

## Commit-scope verification

The session began with uncommitted SDL invalidation, presentation-coordinate and
retained-surface test changes. Those pre-existing hunks are excluded from this
commit, as are the unrelated agent/configuration changes and generated attachment
directory entries. They remain in the user's working tree.

To verify the actual commit scope rather than rely solely on tests of that mixed
working tree, the staged tree was exported to an isolated temporary directory.
The Release SDL test project was rebuilt there with native tests enabled and
`--filter FullyQualifiedName~Prism`: **285 passed, 0 failed, 0 skipped**. Its build
also verified all six SDL shader artifacts. Evidence is in
`.artifacts/prism-architecture-fixes/staged-native-prism.log` and the matching TRX.
`PrismAudit --check` also passed on that isolated tree with zero inventory gaps.
The earlier full-suite and project results above describe the original working
tree and are not relabeled as a clean-checkout full-suite result.
