# Scene Village seams — current integration verification

Date: 2026-09-25. This record covers the user-approved per-sprite Point opt-in, the narrowly approved private Point+Clamp image-domain renderer repair, and the separately authorized PreviewCompiler fix needed by the full-solution gate. It does not change the completed Scene2D plan or the earlier Village feature ledger. The shared Astra accepted the renderer candidate and the PreviewCompiler repair **before** this final build-enabled run, then accepted the current integrated automated Windows SDL checkpoint after reviewing the source, hashes, and raw verification evidence. That acceptance does not imply human validation, GPU timing, or universal atlas isolation.

## Current source and contract

- `Sprite2D.Sampling` and `SamplingProperty` use existing `DrawSamplingMode`, default to Linear, and affect rendering. Village Tiny Town sprites opt into Point. Canonical `docs-site/documentation/classes/Cerneala.UI.Controls.Sprite2D.md` documents the API and SDL_GPU Point+Clamp edge behavior; the canonical manifest and links were inspected, and the manifest test passed.
- The private selected image domain applies only to Point+Clamp image draws. At a covered pixel whose center is **on or outside an external selected-image boundary**, sample from a covered position; strictly interior centers and shared internal triangle/nine-slice seams retain ordinary evaluation. Fractional crops keep nearest-neighbor meaning without a blanket half-texel inset. Linear, Wrap, arbitrary meshes, and surface composites are unchanged by this rule.
- The selected path uses a **64-byte** private image-domain vertex layout and selected shader pair. Ordinary drawing retains its 32-byte layout and original shader artifacts. The selected CPU vertex array is lazy rather than an eager 64 KiB construction payload; one GPU upload arena remains. The Core owner's 34-entry source/artifact/binary SHA-256 inventory at `../core/diagnostic/flat64-final-source-artifact.sha256.txt` matched all 34 current files after the final build (`final-core34-validation.txt`). The explicit shader verifier exited 0 and verified all **10** artifacts (`final-shader-verify/`). Tracked ordinary shader source/compiled artifacts had no Git diff; the new selected pair and updated manifests were inspected separately.

## Regression chain and affected gates

The original app-owned Windows SDL tile-seam RED and captured phase matrix are in `../app/`; the initial Point-only GREEN was not accepted as visual proof. A generic-centroid experiment passed the crop matrix but failed 47 SDL conformance cases, so it was rejected and restored. The final selected-domain candidate passed the Core owner's focused boundary/topology/one-ULP tests and the app-owned native reproduction. Do not interpret the rejected experiment as the final implementation.

| Current evidence | Observed result |
| --- | --- |
| `final-core133-retry01/` Core Drawing + Prism native filter | 133 passed, 0 failed/skipped, exit 0. |
| `final-village-native/` current app-owned native suite | 17 passed, 0 failed/skipped, exit 0. Captures use the app-owned screenshot path under `artifacts/ci/scene-village/screenshots/`; the final full run produced `native-20260925-073328235` and phase capture directories adjacent to it. |
| `final-shader-verify/` explicit `--verify` | Exit 0; ten SDL shader artifacts verified against the current manifest. |
| `final-manifest/` canonical manifest test | 1 passed, 0 failed/skipped, exit 0. The same FQN passed in `final-full-slnx-02/` VisualStudio TRX. |
| `final-api-compat/` strict current-Core comparison | Tool exit **1**, exactly three `CP0002` additive diagnostics, all for approved `Sprite2D.SamplingProperty`, `Sampling.get`, and `Sampling.set`; no other `CP` diagnostic. See exact-member `classification.md`. This is a reviewed intentional API addition, **not** a claim that the raw strict command passed. |

The first Core133 attempt, `final-core133/`, failed **before test execution** because the Playground ldtk package compiler reported access denied on a GUID-named staging directory. The exact same input/compiled tool and absent generated destination succeeded in the bounded direct `diagnostic-ldtk-direct/` run, then the original build/test path passed 133/133 in `final-core133-retry01/`. The denial's root cause remains unproven; the later success does not retroactively prove it was environmental or unrelated.

The first build-enabled full solution, `final-full-slnx-01/`, exited 1 with 6,368 passed, one failed, six skipped. The sole failure was the existing 3D preview characterization: paired markup current-build reuse returned bare `RenderSurface3DPreview` rather than `Cerneala.SdlGpuSmoke.RenderSurface3DPreview`. The user authorized a narrow PreviewCompiler fix. `preview-repair/verification.md` preserves the focused RED, the existing unpaired compatibility catch, the final semantic/PE-identity repair, and 5/5 focused, 17/17 PreviewHost, and 47/47 dependent VisualStudio GREEN. The unpaired built-PE reuse behavior remains unchanged. The shared Astra accepted that repair; the first full run is historical RED, not final evidence.

## Final build-enabled solution gate

`final-full-slnx-02/command.txt`, `output.log`, `exit.txt`, and 11 raw TRXs preserve the exact current Release command. It used `CERNEALA_SDL_NATIVE_TESTS=1`, process **and** command `DefaultItemExcludesInProjectFolder=artifacts/**`, `-m:1`, build enabled, and a unique results directory. Exit code was **0**. `trx-summary.csv`, `trx-skips.csv`, `trx-failures.txt`, and `parse-note.txt` independently reconcile the result rows:

| Scope | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Entire solution, 11 TRXs | **6,372** | **0** | **6** |
| SDL native test project | 935 | 0 | 5 |
| Village native test project | 17 | 0 | 0 |
| Core tests | 4,163 | 0 | 0 |
| SourceGen tests | 611 | 0 | 0 |
| PreviewHost tests | 17 | 0 | 0 |
| VisualStudio tests | 47 | 0 | 0 |

The six skipped result rows are one Language warm-completion P95 performance case, one `SdlWindowsNativeContractTests.NativeOwnershipInputAndGraphicsLifetimesRemainIndependent`, and four disabled `AlphaBlendRenderingTests.OpaqueStrokeOccludesEarlierStrokeAroundTranslucentContent` data rows for content 1, 3, 6, and 7. Their exact FQNs and TRX filenames are in `final-full-slnx-02/trx-skips.csv`; none is counted GREEN. The original failing 3D preview FQN and the canonical manifest FQN both passed in current full-run TRXs. We did not manually play the app or run hosted CI.

`final-full-slnx-02-inputs.txt` and `final-full-slnx-02-inputs-after.txt` show identical SHA-256 for all 13 sampled source, renderer, PreviewCompiler, assembly, and immutable API-baseline inputs before/after the full run. In particular, current Core Release DLL is `99BB2CA9FF02B6FBC6EC99872D0BD093672242FD5FC60F71B6EB51D09DC8AA1E`; the frozen post-Scene2D/pre-sampling baseline is `EC8BEE87C67F0558E85B4D637903AA087C980931EDDE9DD4922B58498774D87B`. This is not a claim that every dirty worktree file belongs to this task.

## Remaining limits and handoff

- The Core native warm workload recorded 1,024 pre-recorded quads, 32 warmup frames, and 20 measured frames of CPU BeginFrame/backend render/CompleteFrame without present, plus measured-thread allocation. It has **no matched pre-change native baseline and no GPU timing**. Do not infer unchanged/improved performance, zero allocations, or universal cost from it. Structural mixed-layout draw splits are noted in the Core owner ledger.
- Windows SDL is the user-selected executable scope. Linux/macOS, WindowsDX, hosted CI, human interaction, and the six skipped cases were not validated here.
- The ldtk staging denial was not reproduced by the direct discriminator or later build; cause remains uncertain. No compiler/ACL/project workaround was installed.
- Existing dirty work and unrelated concurrent edits, including map_impl/environment files, were preserved. Task-scoped whitespace, the regenerated and completely read FileTree, and current worktree status were checked; the shared independent auditor accepted the integrated automated Windows SDL checkpoint. No Git publish or commit was requested.
