# Stage 2 integrated candidate — verification ledger before independent audit

This is a candidate handoff, **not** a checklist checkpoint. The seven Stage 2
tasks and its gate remain unchecked until the shared independent auditor
reviews current source, diff, contract and raw results. Stage 3 native/full
repository/platform gates have not been run or waived.

## Current verification and deliberately open limits

Each raw command, full console log and TRX (where a test was run) is in this
`integration/` directory or the named owner subtree. Release builds/tests use
the Stage 1 evidence-backed command-only exclusion
`-p:DefaultItemExcludesInProjectFolder=artifacts/**`; no project file was
changed to hide source. These are separate commands, not an aggregate sum of
distinct tests; some focused subsets overlap broader runs.

| Scope | Current result / raw evidence | Limit |
| --- | --- | --- |
| Core affected scene/items/map/collision/presentation/Prism/RenderSurface filter | Final post-audit `core-affected-final-repair.command.txt`, `.log`, `.trx`: **1446 pass, 1 skip, 0 fail** using the exact original `Scene|TileMap|Collision|Collider|RenderSurface|Prism|ImageLease` filter. The skip is the separately historical `PrismSdlGpuPixelConformanceTests.EveryResourceFreeCatalogEntryMatchesHistoricalPixelThresholds`. | The Stage 3 native conformance opt-in is not proven by this filtered host run. Earlier 1441/1444-pass runs are historical pre-repair results. |
| Shared scene/Prism/presentation/Playground correction | `sceneitems/frame-discard-shared-classes.*`: **153/153 pass** after a recorded original 17-failure run and one owner-local frame discard RED→GREEN. | Do not interpret the original fixture failures as proof that all were production defects; exact classification is in `shared-scene-focused-classification.md`. |
| Collection/Tetris/map focused owners | `sceneitems/verification.md`: collection-owned **79/79** after both audit repairs; `map/map-owner-verification.md`: broad **232/232**, new **6/6** (overlap). | Owner logs preserve the cache RED and the two later one-shot/reentrant RED→GREEN sequences. |
| SourceGen after final SceneItems and Language changes | `sourcegen-final-repair.command.txt`, `.log`, `.trx`: **610/610 pass**, no skip. | Current rebuilt source/test inputs, not the earlier pre-audit SourceGen run. |
| Language public markup/completion caller | `language/language-source-cutover.md`; final full `language-full-final.*`: **234 pass, 1 skip, 0 fail**. Focused contract GREEN **8/8** after genuine RED. | Explicit existing completion CPU P95 test is skipped by maintainer instruction; no performance gate is claimed. |
| Packages / CPV2 / public payload retention | Final rebuilt `packages-final-repair.command.txt`, `.log`, `.trx`: **104/104 pass**. `package/verification.md` preserves frozen CPV2 fixture and `TileMapChunkData2D` boundary RED→GREEN. | No actual remote server or native SDL run is claimed. |
| Importers | `importers-current.command.txt`, `.log`, `.trx`: **173/173 pass**. | No full solution run yet. |
| Tetris app/tests | Final `tetris-final-repair.command.txt`, `.log`, `.trx`: **31/31 pass** after final SceneItems repair; app compiled in that command. | No human gameplay validation claimed. |
| Canonical docs manifest | Final `manifest-final-repair.command.txt`, `.log`, `.trx`: official VisualStudio project targeted manifest test **1/1 pass** after the docs writer's SceneItems fault/reentrancy clarification. | Static canonical page/link checks by docs owner are separate; no public docs placed under `docs/documentation/`. |
| Native/benchmark caller compile | Final `sdlgpu-build-final-repair.*`: Release **0 warnings, 0 errors** after the package-owned native fixture teardown correction. Earlier `benchmarks-build-first.*`: Release **0 warnings, 0 errors**; its source was unchanged by the audit repairs. | Compile only, not native visual/performance results. The Stage 4 tile benchmark intentionally retains an internal friend-only source-publication workload; README now says so. |

The core API comparison was run strictly against frozen Stage 1 binaries in
`api-compat-core-final-repair.*`. A separate, equivalent strict package
project/command was needed because the original comparison short-circuits
after Core's intentional nonzero. `api-compat-review.md` and
`api-compat-exact-classification.tsv` classify **all 23 Core + 20 Package CP
diagnostics by exact member** under the selected Stage 0 contract. Both raw
commands exit **1** for deliberate breaks; neither is called an exit-zero
ApiCompat pass. The current strict lines are exactly identical (23/23 and
20/20 in order) to the pre-audit reviewed classification despite changed
binary hashes. The unapproved `TileMapChunkData2D` CP0001 from the earlier raw
candidate is absent after public restoration. Current Core and Package DLL
SHA-256 are the refreshed compared candidate hashes:

- Core `EC8BEE87C67F0558E85B4D637903AA087C980931EDDE9DD4922B58498774D87B`.
- Packages `DD8790173B27740D1B3819528AE0BA9C5088E3D70B32523D1C247CACC4002DC8`.

The later Language correction touched only internal Language implementation
and Language tests. It introduced no public/protected Language signature; the
full Language and SourceGen project tests were rerun. The post-implementation caller audit also
corrected two inventory interpretations in the Stage 0 addendum: Language
semantic/completion rules were initially missed, and the Stage 4 benchmark's
internal source path was incorrectly grouped with app-facing factory edits.

## Shared-audit repairs on this current candidate

The first independent Stage 2 audit rejected two concrete cases. The
collection owner then captured a one-shot failed-rebind/template recovery RED
and a separate reentrant template edit RED. The owner-local fix retains the
last successfully enumerated pending value while rebuilding a template,
without re-enumerating a one-shot source; an actual source/event/Refresh still
requests a new enumeration. `sceneitems/reentrant-template-red.*`,
`reentrant-template-green-focused.*`, `reentrant-template-owned-green.*` and
the owner's ledger preserve the RED, focused GREEN and **79/79** owned result.
The current broad Core run above includes those added cases. The canonical
SceneItems page was clarified only after these semantics were source/test
grounded; the final manifest test above used the final page.

The package owner corrected
`tests/Cerneala.Tests.SdlGpu/NativePackageWarmStreamingTests.cs` teardown:
detach still-parented maps before terminal disposal and keep draining later
maps rather than stopping cleanup at the first one. The current SDL project
build proves source/fixture compilation. Native execution remains the Stage 3
gate; this ledger does **not** claim the teardown ran in the native harness.

Final repair input/output SHA-256 for audit correlation:

| Path (source rows from repository root; `integration/` rows from the Stage 2 evidence root) | SHA-256 |
| --- | --- |
| `UI/Controls/SceneItems2D.cs` | `D4E5E394BDEC10E5693FB381AAFBD8CE7ABA85CBD79DB63CB64509E6314BF1AE` |
| `tests/Cerneala.Tests/Controls/SceneItems2DSimpleCollectionContractTests.cs` | `DE9E07DE5587429D682D2D81B539C4DD6AC84DC6484CC59F47D2A4BD6EB8A3C4` |
| `tests/Cerneala.Tests.SdlGpu/NativePackageWarmStreamingTests.cs` | `D9A96DFE1F38975C151D0B933A7D822503B5A8C47A00606568A9797FD8A730C7` |
| `docs-site/documentation/classes/Cerneala.UI.Controls.SceneItems2D.md` | `9BE4421801CC7FB0A3B79BC009FBFB539E11C71733FF6A285C1A55A7F0434FD5` |
| `docs-site/documentation/manifest.json` | `5E1DADD6044A20309C8B58EF5E4E976AE2FF928A07F24E3C4165659203B7C80F` |
| `integration/core-affected-final-repair.trx` | `9391E825855470422A45E564EC007B9FD0485AE0AE228290C4B80800AC4C9618` |
| `integration/sourcegen-final-repair.trx` | `650BE73F268B601FF9B86A19AFC7DD689E971EDB2A0400C2FD0B30EE17E65515` |
| `integration/packages-final-repair.trx` | `47E31AFA42D83C91D4CBDCFA944812F8CDE6E6D2A6D798A950E52D93B9A69708` |

## Public/documentation candidate and audit boundary

The intended external surface has `IEnumerable?` SceneItems, public static
`TileMap2D.FromModel`, private map source/catalog/header/residency mechanics,
`Collider2D.IsSimulated`, prepared-package range transport and direct-value
package APIs. `TileMapChunkData2D` remains public domain payload data. The
canonical docs writer removed the **nine** no-longer-public spatial/source/
catalog/header pages, added pages for `IScene2DPackageRangeReader` and
`Scene2DPackagePart`, restored the payload page, updated affected existing
class pages, and synchronized the manifest. The independent auditor must
verify actual source, manifest and current docs rather than accept this
summary or the manifest test alone.

The surviving public `Scene2D.Children`, direct `Sprite2D`, and static
`FromModel` are not renamed equivalents of the removed spatial provider.
Stage 0 selected capability reductions are recorded in the companion, not
silently represented as preserved custom-format chunk streaming or incremental
public cell-publishing equivalence. No additional unrelated cleanup, Git
publication, CI dispatch, platform waiver or manual validation occurred.

Before checkpoint the shared auditor still needs to review every integrated
writer cone, cross-worker seams, caller mapping, current diff and raw
verification, plus stage conformity. Stage 3 remains separate: real native
SDL opt-in/reproduction, idle/delta/conformance, full solution tests and
WindowsDX/Linux/macOS requirements are **open**; WindowsDX currently lacks a
source-identified executable harness and the non-Windows platforms have not
been exercised on this Windows host. No skip or environment blocker is marked
green by this Stage 2 candidate ledger.
